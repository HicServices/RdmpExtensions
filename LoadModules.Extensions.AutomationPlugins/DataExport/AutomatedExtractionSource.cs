using System;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.Data.Dashboarding;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataFlowPipeline.Requirements;
using DataExportLibrary.Data.DataTables;
using RDMPAutomationService;
using RDMPAutomationService.Interfaces;
using RDMPStartup;
using ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.AutomationPlugins.DataExport
{
    public class AutomatedExtractionSource : IPluginAutomationSource, IPipelineRequirement<IRDMPPlatformRepositoryServiceLocator>
    {
        private AutomationServiceSlot _serviceSlot;
        private IRDMPPlatformRepositoryServiceLocator _repositoryLocator;

        [DemandsInitialization("The Extraction Configuration to execute")]
        public AutomatedExtractionConfiguration ConfigurationToRunRoutinely { get; set; }
        
        public OnGoingAutomationTask GetChunk(IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            //we don't have our slot yet
            if(_serviceSlot == null)
                return null;

            //only allow one execution at once
            if (_serviceSlot.AutomationJobs.Any(j => j.Description.StartsWith(RoutineExtractionRun.RoutineExtractionJobsPrefix)))
                return null;

            
            var routineExtractionRun = new RoutineExtractionRun(_serviceSlot);
            return new OnGoingAutomationTask(routineExtractionRun.AutomationJob, routineExtractionRun);
        }

        public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {
            
        }

        public void Abort(IDataLoadEventListener listener)
        {
            
        }

        public OnGoingAutomationTask TryGetPreview()
        {
            return null;
        }

        public void PreInitialize(AutomationServiceSlot value, IDataLoadEventListener listener)
        {
            _serviceSlot = value;
        }

        public void PreInitialize(IRDMPPlatformRepositoryServiceLocator value, IDataLoadEventListener listener)
        {
            _repositoryLocator = value;
        }
    }
}
