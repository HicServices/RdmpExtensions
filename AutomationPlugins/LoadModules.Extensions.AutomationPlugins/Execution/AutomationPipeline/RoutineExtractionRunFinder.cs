﻿using System;
using System.Linq;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.Repositories;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.AutomationPlugins.Execution.AutomationPipeline
{
    public class RoutineExtractionRunFinder
    {
        private readonly AutomateExtractionRepository _automateExtractionRepository;
        private readonly IDataLoadEventListener _listener;

        public RoutineExtractionRunFinder(AutomateExtractionRepository automateExtractionRepository, IDataLoadEventListener listener = null)
        {
            _automateExtractionRepository = automateExtractionRepository;
            this._listener = listener;
        }

        public RoutineExtractionRun GetAutomateExtractionToRunIfAny(IRDMPPlatformRepositoryServiceLocator repositoryLocator, AutomationServiceSlot serviceSlot)
        {
            //only allow one execution at once (this means no parallel execution of automated extract schedules - although the datasets in them might stilll be executed in parallel)
            if (serviceSlot.AutomationJobs.Any(j => j.Description.StartsWith(RoutineExtractionRun.RoutineExtractionJobsPrefix)))
            {
                _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Debug, "One extraction is already running, skipping for now."));
                return null;
            }

            var next = _automateExtractionRepository.GetAllObjects<QueuedExtraction>().FirstOrDefault(q => q.IsDue());

            if (next != null)
            {
                _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Debug, String.Format("Found queued extraction {0}, running it", next.ExtractionConfiguration.Name)));
                return new RoutineExtractionRun(repositoryLocator,serviceSlot,next);
            }
            
            var schedules = _automateExtractionRepository.GetAllObjects<AutomateExtractionSchedule>();
            
            //for each schedule
            foreach (AutomateExtractionSchedule schedule in schedules)
            {
                //is the schedule runnable?
                string reason;
                if (!IsRunnable(schedule, out reason))
                {
                    _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Debug, String.Format("Schedule {0} not runnable: {1}", schedule.Name, reason)));
                    continue; //no
                }

                //find the first runnable Extraction in the schedule
                var toRun = schedule.AutomateExtractions.Where(a => !a.Disabled).ToArray();

                foreach (AutomateExtraction runnable in toRun)
                    if (IsRunnable(schedule, runnable))
                        return new RoutineExtractionRun(repositoryLocator,serviceSlot,runnable);
            }
            return null;
        }

        private bool IsRunnable(AutomateExtractionSchedule schedule, AutomateExtraction runnable)
        {
            if (runnable.BaselineDate == null)
                return true;

            //it's not yet that time of the day
            if (DateTime.Now.TimeOfDay <= schedule.ExecutionTimeOfDay)
                return false;

            var now = DateTime.Now.Date;
            var then = runnable.BaselineDate.Value.Date;

            switch (schedule.ExecutionTimescale)
            {
                case AutomationTimeScale.Never:
                    return false;
                case AutomationTimeScale.Daily:
                    return now.Subtract(then).TotalSeconds >= 86400;
                case AutomationTimeScale.Weekly:
                    return now.Subtract(then).TotalSeconds >= 604800;
                case AutomationTimeScale.BiWeekly:
                    return now.Subtract(then).TotalSeconds >= 1209600;
                case AutomationTimeScale.Monthly:
                    return now.Subtract(then).TotalSeconds >= 2629746;
                case AutomationTimeScale.Yearly:
                    return now.Subtract(then).TotalSeconds >= 31556952;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool IsRunnable(AutomateExtractionSchedule arg)
        {
            string whoCares;
            return IsRunnable(arg, out whoCares);
        }

        private bool IsRunnable(AutomateExtractionSchedule arg,out string reason)
        {
            //check the schedule to see if it is of a runnable timescale
            if (arg.ExecutionTimescale == AutomationTimeScale.Never)
            {
                reason = "ExecutionTimescale of Schedule is Never";
                return false;
            }

            //See if the ticketing system is in a state where 
            var mem = new ToMemoryCheckNotifier();
            arg.CheckTicketing(mem);

            if (mem.GetWorst() == CheckResult.Fail)
            {
                reason = "Ticketing System Refused Extraction";
                return false;
            }

            var pipelineChecker = new AutomatedExtractionPipelineChecker(arg.Pipeline);
            pipelineChecker.Check(mem);

            if (mem.GetWorst() == CheckResult.Fail)
            {
                reason = "Extraction Pipeline Failed Checks";
                return false;
            }

            //sure do it
            reason = null;
            return true;
        }

        public bool CanRun(AutomateExtraction automateExtraction, out string reason)
        {
            var schedule = automateExtraction.AutomateExtractionSchedule;
            bool scheduleRunnable = IsRunnable(schedule,out reason);

            if (scheduleRunnable)
                if (IsRunnable(schedule, automateExtraction))
                    return true;
                else
                {
                    reason = "Elapsed time since baseline has not exceeded the Schedule ExecutionTimescale (i.e. no extraction is due)";
                    return false;
                }


            return false;
        }
    }
}
