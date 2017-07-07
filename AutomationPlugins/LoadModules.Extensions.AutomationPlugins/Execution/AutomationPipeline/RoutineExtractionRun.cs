using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.Repositories;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.ExtractionTime.Commands;
using DataExportLibrary.ExtractionTime.ExtractionPipeline;
using DataExportLibrary.ExtractionTime.UserPicks;
using DataExportLibrary.Interfaces.Data.DataTables;
using DataLoadEngineTests.Integration;
using FluentNHibernate.Conventions;
using HIC.Logging;
using LoadModules.Extensions.AutomationPlugins.Data;
using RDMPAutomationService;
using RDMPAutomationService.Interfaces;
using roundhouse.infrastructure.logging;

namespace LoadModules.Extensions.AutomationPlugins.Execution.AutomationPipeline
{
    public class RoutineExtractionRun : IAutomateable
    {
        public const string LoggingTaskName = "Automated Extracts";
        private readonly IRDMPPlatformRepositoryServiceLocator _repositoryLocator;
        private AutomationServiceSlot _serviceSlot;
        private readonly AutomateExtraction _automateExtractionConfigurationToRun;

        public AutomationJob AutomationJob { get; private set; }

        public const string RoutineExtractionJobsPrefix = "RE:";

        public RoutineExtractionRun(IRDMPPlatformRepositoryServiceLocator repositoryLocator, AutomationServiceSlot serviceSlot, AutomateExtraction automateExtractionConfigurationToRun)
        {
            _repositoryLocator = repositoryLocator;
            _serviceSlot = serviceSlot;
            _automateExtractionConfigurationToRun = automateExtractionConfigurationToRun;

            AutomationJob = _serviceSlot.AddNewJob(AutomationJobType.UserCustomPipeline, RoutineExtractionJobsPrefix + automateExtractionConfigurationToRun);
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
                
                var extractionConfiguration = _automateExtractionConfigurationToRun.ExtractionConfiguration;
                var datasets = extractionConfiguration.GetAllExtractableDataSets();

                if(!datasets.Any())
                    throw new Exception("There are no ExtractableDatasets configured for ExtractionConfiguration '" + extractionConfiguration + "' in AutomateExtraction");

                var logManager = ((ExtractionConfiguration) extractionConfiguration).GetExplicitLoggingDatabaseServerOrDefault();
                logManager.CreateNewLoggingTaskIfNotExists(LoggingTaskName);

                var dlinfo = logManager.CreateDataLoadInfo(LoggingTaskName, GetType().Name, extractionConfiguration.ToString(), "",false);
                
                foreach (IExtractableDataSet ds in datasets)
                {
                    var bundle = new ExtractableDatasetBundle(ds);
                    var cmd = new ExtractDatasetCommand(_repositoryLocator, extractionConfiguration, bundle);

                    var schedule = _automateExtractionConfigurationToRun.AutomateExtractionSchedule;

                    var host = new ExtractionPipelineHost(cmd, _repositoryLocator.CatalogueRepository.MEF,schedule.Pipeline,(DataLoadInfo) dlinfo);
                    host.Execute(new ThrowImmediatelyDataLoadJob());
                }
                
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
