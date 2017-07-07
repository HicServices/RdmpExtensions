using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Automation;
using DataExportLibrary.Data.DataTables;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Execution.AutomationPipeline;
using NUnit.Framework;

namespace LoadModules.Extensions.AutomationPlugins.Tests
{
    public class RoutineExtractionRunFinderTests:TestsRequiringAnAutomationPluginRepository
    {
        private AutomationServiceSlot _slot;
        private RoutineExtractionRunFinder _finder;
        private Project _proj;

        [SetUp]
        public void SetupSlot()
        {
            _slot = new AutomationServiceSlot(_repo.CatalogueRepository);
            _finder = new RoutineExtractionRunFinder(_repo);

            _proj = new Project(_repo.DataExportRepository, "MyProject");

        }

        [TearDown]
        public void DeleteSlot()
        {
            _slot.DeleteInDatabase();
            _proj.DeleteInDatabase();
        }

        [Test]
        public void FindRunnableSchedule_NoneExist()
        {
            //nothing available to find
            Assert.IsNull(_finder.GetScheduleToRunIfAny(_slot));
        }

        [Test]
        public void FindRunnableSchedule_RunNever()
        {
            AutomateExtractionSchedule schedule = new AutomateExtractionSchedule(_repo, _proj);
            
            try
            {
                schedule.ExecutionTimescale = AutomationTimeScale.Never;
                schedule.SaveToDatabase();

                //nothing available to find
                Assert.IsNull(_finder.GetScheduleToRunIfAny(_slot));
            }
            finally
            {
                schedule.DeleteInDatabase();
            }
        }


        [Test]
        public void FindRunnableSchedule_TicketStatusIsNo()
        {
            _repo.CatalogueRepository.MEF.AddTypeToCatalogForTesting(typeof(NeverAllowAnythingTicketing));

            var ticketingSystem = new TicketingSystemConfiguration(_repo.CatalogueRepository, "NeverAllowAnythingTicketing");
            ticketingSystem.Type = typeof (NeverAllowAnythingTicketing).FullName;
            ticketingSystem.SaveToDatabase();
            
            AutomateExtractionSchedule schedule = new AutomateExtractionSchedule(_repo, _proj);
            schedule.ExecutionTimescale = AutomationTimeScale.Monthly;
            schedule.Ticket = "FISH";
            schedule.SaveToDatabase();

            try
            {
                //nothing available to find
                Assert.Null( _finder.GetScheduleToRunIfAny(_slot));
            }
            finally
            {
                ticketingSystem.DeleteInDatabase();
                schedule.DeleteInDatabase();
            }
        }


        [TestCase(false)]
        [TestCase(true)]
        public void FindRunnableSchedule(bool createATicketingSystem)
        {
            TicketingSystemConfiguration ticketingSystem = null;
            if (createATicketingSystem)
            {
                _repo.CatalogueRepository.MEF.AddTypeToCatalogForTesting(typeof(AllowAnythingTicketing));

                ticketingSystem = new TicketingSystemConfiguration(_repo.CatalogueRepository, "AllowAnythingTicketing");
                ticketingSystem.Type = typeof(AllowAnythingTicketing).FullName;
                ticketingSystem.SaveToDatabase();
            }

            AutomateExtractionSchedule schedule = new AutomateExtractionSchedule(_repo,_proj);
            schedule.ExecutionTimescale = AutomationTimeScale.Monthly;
            schedule.Ticket = "FISH";
            schedule.SaveToDatabase();

            try
            {
                //nothing available to find
                Assert.AreEqual(schedule,_finder.GetScheduleToRunIfAny(_slot));
            }
            finally
            {
                if(createATicketingSystem)
                    ticketingSystem.DeleteInDatabase();

                schedule.DeleteInDatabase();
            }
        }
    }
}
