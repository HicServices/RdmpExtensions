using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CachingEngine.Requests.FetchRequestProvider;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.Data.Pipelines;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.Repositories;
using DataExportLibrary.CohortCreationPipeline;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.DataRelease;
using DataExportLibrary.DataRelease.ReleasePipeline;
using DataExportLibrary.ExtractionTime;
using DataExportLibrary.ExtractionTime.Commands;
using DataExportLibrary.ExtractionTime.ExtractionPipeline;
using DataExportLibrary.ExtractionTime.UserPicks;
using DataExportLibrary.Interfaces.Data.DataTables;
using FluentNHibernate.Conventions;
using HIC.Logging;
using HIC.Logging.Listeners;
using LoadModules.Extensions.AutomationPlugins.Data;
using RDMPAutomationService;
using RDMPAutomationService.Interfaces;
using ReusableLibraryCode.Progress;
using roundhouse.infrastructure.logging;

namespace LoadModules.Extensions.AutomationPlugins.Execution.AutomationPipeline
{
    public class RoutineExtractionRun : IAutomateable
    {
        public const string LoggingTaskName = "Automated Extracts";
        private readonly IRDMPPlatformRepositoryServiceLocator _repositoryLocator;
        private AutomationServiceSlot _serviceSlot;

        private Pipeline _pipeline;
        public IExtractionConfiguration ExtractionConfiguration;
        private string _jobName;
        private QueuedExtraction _que;
        private AutomateExtraction _automate;
        private LogManager _logManager;
        private IDataLoadInfo _dlinfo;
        private ToLoggingDatabaseDataLoadEventListener _toLogging;

        public AutomationJob AutomationJob { get; private set; }

        public const string RoutineExtractionJobsPrefix = "RE:";

        public RoutineExtractionRun(IRDMPPlatformRepositoryServiceLocator repositoryLocator, AutomationServiceSlot serviceSlot, AutomateExtraction automateExtractionConfigurationToRun)
        {
            _repositoryLocator = repositoryLocator;
            _serviceSlot = serviceSlot;
            ExtractionConfiguration = automateExtractionConfigurationToRun.ExtractionConfiguration;
            _pipeline = automateExtractionConfigurationToRun.AutomateExtractionSchedule.Pipeline;

            _jobName = RoutineExtractionJobsPrefix + automateExtractionConfigurationToRun;
            _automate = automateExtractionConfigurationToRun;
        }
        public RoutineExtractionRun(IRDMPPlatformRepositoryServiceLocator repositoryLocator, AutomationServiceSlot serviceSlot, QueuedExtraction que)
        {
            _repositoryLocator = repositoryLocator;
            _serviceSlot = serviceSlot;
            ExtractionConfiguration = que.ExtractionConfiguration;
            _pipeline = que.Pipeline;

            _jobName = RoutineExtractionJobsPrefix + "QUE " + ExtractionConfiguration;
            _que = que;
        }

        public void CreateJob()
        {
            AutomationJob = _serviceSlot.AddNewJob(AutomationJobType.UserCustomPipeline,_jobName);

            //we have created the job so now clear the que so it doesn't get executed again endlessly
            if(_que != null)
                _que.DeleteInDatabase();
        }

        
        public OnGoingAutomationTask GetTask()
        {
            return new OnGoingAutomationTask(AutomationJob, this);
        }

        public void RunTask(OnGoingAutomationTask task)
        {
            try
            {
                var startDate = DateTime.Now;

                task.Job.SetLastKnownStatus(AutomationJobStatus.Running);

                if (_automate != null && _automate.RefreshCohort)
                    RefreshCohort();

                RunExtraction();

                if (_automate != null && _automate.Release)
                    ReleaseExtract();

                //it worked!
                task.Job.SetLastKnownStatus(AutomationJobStatus.Finished);
                task.Job.DeleteInDatabase();

                //if it all worked out ok and we have an automated extraction schedule we can now say that the schedule has been succesfully executed up this date for all datasets
                if (_automate != null)
                {
                    _automate.BaselineDate = startDate;
                    _automate.SaveToDatabase();
                }
            }
            catch (Exception e)
            {
                task.Job.SetLastKnownStatus(AutomationJobStatus.Crashed);
                new AutomationServiceException(_repositoryLocator.CatalogueRepository, e);
            }
            finally
            {
                if (_toLogging != null)
                    _toLogging.FinalizeTableLoadInfos();

                if(_dlinfo != null && !_dlinfo.IsClosed)
                    _dlinfo.CloseAndMarkComplete();
            }
        }

        private void ReleaseExtract()
        {
            var schedule = _automate.AutomateExtractionSchedule;
            var releasePipeline = schedule.ReleasePipeline;

            if(releasePipeline == null)
                throw new Exception("Release Pipeline has not been set for AutomateExtractionSchedule '" + schedule +"'");
            
            //the data we are trying to extract
            var source = new FixedDataReleaseSource();

            var releasePotentialList = new List<ReleasePotential>();

            foreach (var ds in ExtractionConfiguration.GetAllExtractableDataSets())
                releasePotentialList.Add(new ReleasePotential(_repositoryLocator, ExtractionConfiguration, ds));

            source.CurrentRelease = new ReleaseData
            {
                ConfigurationsForRelease = new Dictionary<IExtractionConfiguration, List<ReleasePotential>>()
                {
                    {ExtractionConfiguration, releasePotentialList}
                },
                EnvironmentPotential = new ReleaseEnvironmentPotential(ExtractionConfiguration),
                ReleaseState = ReleaseState.DoingProperRelease
            };

            //the release context for the project
            var context = new ReleaseUseCase((Project) ExtractionConfiguration.Project, source);

            StartLoggingIfNotStartedYet();
            var fork = new ForkDataLoadEventListener(_toLogging, new ThrowImmediatelyDataLoadEventListener());

            //translated into an engine
            var engine = context.GetEngine(releasePipeline, fork);
            
            //and executed
            engine.ExecutePipeline(new GracefulCancellationToken());
        }

        private void RefreshCohort()
        {
            int? before = ExtractionConfiguration.Cohort_ID;

            StartLoggingIfNotStartedYet();
            var fork = new ForkDataLoadEventListener(_toLogging, new ThrowImmediatelyDataLoadEventListener());

            var engine = new CohortRefreshEngine(fork, (ExtractionConfiguration)ExtractionConfiguration);
            engine.Execute();

            ExtractionConfiguration.RevertToDatabaseState();

            if(before == ExtractionConfiguration.Cohort_ID)
                throw new Exception("Despite running the CohortRefreshEngine the Cohort_ID of the ExtractionConfiguration did not change!");
        }

        private void RunExtraction()
        {
            if (ExtractionConfiguration.IsReleased)
                ((ExtractionConfiguration) ExtractionConfiguration).Unfreeze();

            var datasets = ExtractionConfiguration.GetAllExtractableDataSets();

            if (!datasets.Any())
                throw new Exception("There are no ExtractableDatasets configured for ExtractionConfiguration '" +
                                    ExtractionConfiguration + "' in AutomateExtraction");

            StartLoggingIfNotStartedYet();

            var toMemory = new ToMemoryDataLoadEventListener(false);
            
            foreach (IExtractableDataSet ds in datasets)
            {
                var bundle = new ExtractableDatasetBundle(ds);
                var cmd = new ExtractDatasetCommand(_repositoryLocator, ExtractionConfiguration, bundle);

                var host = new ExtractionPipelineHost(cmd, _pipeline, (DataLoadInfo) _dlinfo);

                host.Execute(toMemory);
                if (toMemory.GetWorst() == ProgressEventType.Error)
                    throw new Exception(
                        "Failed executing ExtractionConfiguration '" + ExtractionConfiguration + "' DataSet '" + ds +
                        "'",
                        new AggregateException(GetExceptions(toMemory))
                        );

                var wordDataWritter = new WordDataWritter(host);

                if (wordDataWritter.RequirementsMet()) //if Microsoft Word is installed
                {
                    wordDataWritter.GenerateWordFile(); //run the report

                    //if there were any exceptions
                    if (wordDataWritter.ExceptionsGeneratingWordFile.Any())
                        throw new AggregateException(wordDataWritter.ExceptionsGeneratingWordFile);
                }
            }
        }

        private void StartLoggingIfNotStartedYet()
        {
            if(_logManager != null)
                return;

            _logManager = ((ExtractionConfiguration)ExtractionConfiguration).GetExplicitLoggingDatabaseServerOrDefault();
            _logManager.CreateNewLoggingTaskIfNotExists(LoggingTaskName);

            _dlinfo = _logManager.CreateDataLoadInfo(LoggingTaskName, GetType().Name, ExtractionConfiguration.ToString(), "", false);
            _toLogging = new ToLoggingDatabaseDataLoadEventListener(_logManager, _dlinfo);

        }

        private Exception[] GetExceptions(ToMemoryDataLoadEventListener toMemory)
        {
            List<Exception> exes = new List<Exception>();

            foreach (KeyValuePair<object, List<NotifyEventArgs>> kvp in toMemory.EventsReceivedBySender)
                foreach (NotifyEventArgs arg in kvp.Value)
                    if (arg.Exception != null)
                        exes.Add(arg.Exception);
                    else if (arg.ProgressEventType == ProgressEventType.Error)
                        exes.Add(new Exception(arg.Message));

            return exes.ToArray();
        }
    }
}
