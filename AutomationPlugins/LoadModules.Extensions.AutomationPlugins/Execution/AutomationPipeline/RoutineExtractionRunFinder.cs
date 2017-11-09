using System;
using System.Linq;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.Repositories;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;
using NHibernate.Cfg.Loquacious;
using ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.AutomationPlugins.Execution.AutomationPipeline
{
    public class RoutineExtractionRunFinder
    {
        private readonly AutomateExtractionRepository _automateExtractionRepository;

        public RoutineExtractionRunFinder(AutomateExtractionRepository automateExtractionRepository)
        {
            _automateExtractionRepository = automateExtractionRepository;
        }

        public RoutineExtractionRun GetAutomateExtractionToRunIfAny(IRDMPPlatformRepositoryServiceLocator repositoryLocator, AutomationServiceSlot serviceSlot)
        {
            //only allow one execution at once (this means no parallel execution of automated extract schedules - although the datasets in them might stilll be executed in parallel)
            if (serviceSlot.AutomationJobs.Any(j => j.Description.StartsWith(RoutineExtractionRun.RoutineExtractionJobsPrefix)))
                return null;

            var next = _automateExtractionRepository.GetAllObjects<QueuedExtraction>().FirstOrDefault(q => q.IsDue());

            if(next != null)
                return new RoutineExtractionRun(repositoryLocator,serviceSlot,next);
            
            var schedules = _automateExtractionRepository.GetAllObjects<AutomateExtractionSchedule>();
            
            //for each schedule
            foreach (AutomateExtractionSchedule schedule in schedules)
            {
                //is the schedule runnable?
                if (!IsRunnable(schedule))
                    continue; //no

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

            switch (schedule.ExecutionTimescale)
            {
                case AutomationTimeScale.Never:
                    return false;
                case AutomationTimeScale.Daily:
                    return DateTime.Now.Subtract(runnable.BaselineDate.Value).TotalSeconds > 86400;
                case AutomationTimeScale.Weekly:
                    return DateTime.Now.Subtract(runnable.BaselineDate.Value).TotalSeconds > 604800;
                case AutomationTimeScale.BiWeekly:
                    return DateTime.Now.Subtract(runnable.BaselineDate.Value).TotalSeconds > 1209600;
                case AutomationTimeScale.Monthly:
                    return DateTime.Now.Subtract(runnable.BaselineDate.Value).TotalSeconds > 2629746;
                case AutomationTimeScale.Yearly:
                    return DateTime.Now.Subtract(runnable.BaselineDate.Value).TotalSeconds> 31556952;
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
