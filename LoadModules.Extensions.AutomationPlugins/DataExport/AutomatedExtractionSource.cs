using System;
using System.Linq;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.DataFlowPipeline;
using RDMPAutomationService;
using RDMPAutomationService.Interfaces;
using ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.AutomationPlugins.DataExport
{
    public class AutomatedExtractionSource : IPluginAutomationSource
    {
        private AutomationServiceSlot _serviceSlot;

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
    }
}
