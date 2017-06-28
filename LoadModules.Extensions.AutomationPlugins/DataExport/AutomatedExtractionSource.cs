﻿using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.AutomationPlugins.DataExport
{
    public class AutomatedExtractionSource : IPluginAutomationSource, IPipelineRequirement<IRDMPPlatformRepositoryServiceLocator>, ICheckable
    {
        private AutomationServiceSlot _serviceSlot;
        private IRDMPPlatformRepositoryServiceLocator _repositoryLocator;

        [DemandsInitialization("The start time of day when jobs can run e.g. 18:00 to start jobs from 6pm.  Leave blank for no limit")]
        public string StartTimeWindow { get; set; }

        [DemandsInitialization("The end time of day when jobs can run e.g. 9:00 to stop jobs running before 9am.  Leave blank for no limit")]
        public string EndTimeWindow { get; set; }
        
        public OnGoingAutomationTask GetChunk(IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            //we don't have our slot yet
            if(_serviceSlot == null)
                return null;

            //only allow one execution at once
            if (_serviceSlot.AutomationJobs.Any(j => j.Description.StartsWith(RoutineExtractionRun.RoutineExtractionJobsPrefix)))
                return null;

            //do not start new jobs if we are not within the execution window
            if (!AreWithinExecutionWindow())
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
                return timeNow > windowStart.Value && timeNow < windowEnd.Value;

            //time is something like 9am to 5pm (the same day)
            return timeNow > windowEnd.Value && timeNow < windowStart.Value;

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
