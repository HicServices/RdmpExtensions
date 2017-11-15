using System;
using System.Globalization;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataFlowPipeline.Requirements;
using CatalogueLibrary.Repositories;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using RDMPAutomationService;
using RDMPAutomationService.Interfaces;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.AutomationPlugins.Execution.AutomationPipeline
{
    public class AutomatedExtractionSource : IPluginAutomationSource, IPipelineRequirement<IRDMPPlatformRepositoryServiceLocator>, ICheckable
    {
        private AutomationServiceSlot _serviceSlot;
        private IRDMPPlatformRepositoryServiceLocator _repositoryLocator;
        private AutomateExtractionRepository _automateExtractionRepository;

        [DemandsInitialization("The start time of day when jobs can run e.g. 18:00 to start jobs from 6pm.  Leave blank for no limit")]
        public string StartTimeWindow { get; set; }

        [DemandsInitialization("The end time of day when jobs can run e.g. 9:00 to stop jobs running before 9am.  Leave blank for no limit")]
        public string EndTimeWindow { get; set; }
        
        public OnGoingAutomationTask GetChunk(IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            //we don't have our slot yet
            if(_serviceSlot == null)
                return null;

            //do not start new jobs if we are not within the service execution window
            if (!AreWithinExecutionWindow())
                return null;

            //this finder is used in the UI by people who might not have access to the server 
            AutomateExtractionRepositoryFinder.Timeout = ReusableLibraryCode.DatabaseCommandHelper.GlobalTimeout;

            var repoFinder = new AutomateExtractionRepositoryFinder(_repositoryLocator);
            _automateExtractionRepository = repoFinder.GetRepositoryIfAny() as AutomateExtractionRepository;

            //there is no automate extractions server (records baselines, when to extract etc)
            if (_automateExtractionRepository == null)
                return null;

            //ask the run finder to find a run
            RoutineExtractionRunFinder runFinder = new RoutineExtractionRunFinder(_automateExtractionRepository);
            var run = runFinder.GetAutomateExtractionToRunIfAny(_repositoryLocator,_serviceSlot);

            
            //there are no new available extractions to run
            if (run == null)
                return null;

            run.CreateJob();

            return new OnGoingAutomationTask(run.AutomationJob, run);
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

        bool AreWithinExecutionWindow()
        {

            var timeNow = DateTime.Now.TimeOfDay;

            var windowStart = StringToTime(StartTimeWindow);
            var windowEnd = StringToTime(EndTimeWindow);

            if (windowStart == null && windowEnd == null)
                return true;

            //start time but no end time
            if(windowEnd == null)
                windowEnd = new TimeSpan(23,59,59);

            if(windowStart == null)
                windowStart = new TimeSpan(0,0,0);

            //time is something like 5pm to 8am the next day
            if (windowStart > windowEnd)
                return timeNow > windowStart.Value || timeNow < windowEnd.Value;

            //time is something like 9am to 5pm (the same day)
            return timeNow > windowStart.Value && timeNow < windowEnd.Value;

        }
        TimeSpan? StringToTime(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            return DateTime.ParseExact(s, "HH:mm", CultureInfo.InvariantCulture).TimeOfDay;
        }

        public void Check(ICheckNotifier notifier)
        {
            try
            {
                StringToTime(StartTimeWindow);
                StringToTime(EndTimeWindow);
            }
            catch (Exception)
            {

                notifier.OnCheckPerformed(new CheckEventArgs("Failed to parse start/end times", CheckResult.Fail));
            }

        }
    }
}
