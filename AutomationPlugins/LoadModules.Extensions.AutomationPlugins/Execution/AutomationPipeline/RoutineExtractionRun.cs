using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        public AutomationJob AutomationJob { get; private set; }

        public const string RoutineExtractionJobsPrefix = "RE:";

        public RoutineExtractionRun(IRDMPPlatformRepositoryServiceLocator repositoryLocator, AutomationServiceSlot serviceSlot, AutomateExtraction automateExtractionConfigurationToRun)
        {
            _repositoryLocator = repositoryLocator;
            _serviceSlot = serviceSlot;
            ExtractionConfiguration = automateExtractionConfigurationToRun.ExtractionConfiguration;
            _pipeline = automateExtractionConfigurationToRun.AutomateExtractionSchedule.Pipeline;

            _jobName = RoutineExtractionJobsPrefix + automateExtractionConfigurationToRun;
        }
        public RoutineExtractionRun(IRDMPPlatformRepositoryServiceLocator repositoryLocator, AutomationServiceSlot serviceSlot, QueuedExtraction que)
        {
            _repositoryLocator = repositoryLocator;
            _serviceSlot = serviceSlot;
            ExtractionConfiguration = que.ExtractionConfiguration;
            _pipeline = que.Pipeline;

            _jobName = RoutineExtractionJobsPrefix + "QUE " + ExtractionConfiguration;
        }

        public void CreateJob()
        {
            AutomationJob = _serviceSlot.AddNewJob(AutomationJobType.UserCustomPipeline,_jobName);
        }

        
        public OnGoingAutomationTask GetTask()
        {
            return new OnGoingAutomationTask(AutomationJob, this);
        }

        public void RunTask(OnGoingAutomationTask task)
        {
            try
            {
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
                    host.Execute(new ToConsoleDataLoadEventReceiver());
                }
                
                dlinfo.CloseAndMarkComplete();

                //it worked!
                task.Job.SetLastKnownStatus(AutomationJobStatus.Finished);
                task.Job.DeleteInDatabase();
            }
            catch (Exception e)
            {
                task.Job.SetLastKnownStatus(AutomationJobStatus.Crashed);
                new AutomationServiceException(_repositoryLocator.CatalogueRepository, e);
            }
        }
    }
}
