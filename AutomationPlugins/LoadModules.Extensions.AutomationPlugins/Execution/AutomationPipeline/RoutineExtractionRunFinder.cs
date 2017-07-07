using System.Linq;
using CatalogueLibrary.Data.Automation;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
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

        public AutomateExtractionSchedule GetScheduleToRunIfAny(AutomationServiceSlot serviceSlot)
        {
            var schedules = _automateExtractionRepository.GetAllObjects<AutomateExtractionSchedule>();

            //only allow one execution at once (this means no parallel execution of automated extract schedules - although the datasets in them might stilll be executed in parallel)
            if (serviceSlot.AutomationJobs.Any(j => j.Description.StartsWith(RoutineExtractionRun.RoutineExtractionJobsPrefix)))
                return null;
            
            return schedules.FirstOrDefault(IsRunnable);
        }

        private bool IsRunnable(AutomateExtractionSchedule arg)
        {
            //check the schedule to see if it is of a runnable timescale
            if (arg.ExecutionTimescale == AutomationTimeScale.Never)
                return false;

            //See if the ticketing system is in a state where 
            var mem = new ToMemoryCheckNotifier();
            arg.CheckTicketing(mem);

            if (mem.GetWorst() == CheckResult.Fail)
                return false;

            //sure do it
            return true;
        }
    }
}
