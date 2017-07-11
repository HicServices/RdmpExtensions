using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CachingEngine.Requests.FetchRequestProvider;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.Data.Pipelines;
using CatalogueLibrary.Repositories;
using DataExportLibrary.Data.DataTables;
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
        private AutomateExtraction _automate
            ;

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
                
                var datasets = ExtractionConfiguration.GetAllExtractableDataSets();

                if(!datasets.Any())
                    throw new Exception("There are no ExtractableDatasets configured for ExtractionConfiguration '" + ExtractionConfiguration + "' in AutomateExtraction");

                var logManager = ((ExtractionConfiguration) ExtractionConfiguration).GetExplicitLoggingDatabaseServerOrDefault();
                logManager.CreateNewLoggingTaskIfNotExists(LoggingTaskName);

                var dlinfo = logManager.CreateDataLoadInfo(LoggingTaskName, GetType().Name, ExtractionConfiguration.ToString(), "",false);
                
                foreach (IExtractableDataSet ds in datasets)
                {
                    var bundle = new ExtractableDatasetBundle(ds);
                    var cmd = new ExtractDatasetCommand(_repositoryLocator, ExtractionConfiguration, bundle);

                    var host = new ExtractionPipelineHost(cmd, _repositoryLocator.CatalogueRepository.MEF,_pipeline,(DataLoadInfo) dlinfo);

                    var toMemory = new ToMemoryDataLoadEventReceiver(false);
                    host.Execute(toMemory);
                    if (toMemory.GetWorst() == ProgressEventType.Error)
                        throw new Exception(
                            "Failed executing ExtractionConfiguration '" + ExtractionConfiguration + "' DataSet '" + ds +
                            "'",
                            new AggregateException(GetExceptions(toMemory))
                            );
                }
                
                dlinfo.CloseAndMarkComplete();

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
        }

        private Exception[] GetExceptions(ToMemoryDataLoadEventReceiver toMemory)
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
