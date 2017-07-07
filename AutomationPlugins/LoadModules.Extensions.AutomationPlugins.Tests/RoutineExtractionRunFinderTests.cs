using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.Data.Pipelines;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.ExtractionTime.ExtractionPipeline.Destinations;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Execution.AutomationPipeline;
using LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;
using NUnit.Framework;

namespace LoadModules.Extensions.AutomationPlugins.Tests
{
    public class RoutineExtractionRunFinderTests:TestsRequiringAnAutomationPluginRepository
    {
        private AutomationServiceSlot _slot;
        private RoutineExtractionRunFinder _finder;
        private Project _proj;
        private ExtractionConfiguration _extractionConfiguration;
        private Pipeline _validPipeline;
        private Pipeline _invalidPipeline;
        private AutomateExtractionSchedule _schedule;
        private AutomateExtraction _config;

        [SetUp]
        public void SetupSlot()
        {
            _slot = new AutomationServiceSlot(_repo.CatalogueRepository);
            _finder = new RoutineExtractionRunFinder(_repo);

            _proj = new Project(_repo.DataExportRepository, "MyProject");
            _extractionConfiguration = new ExtractionConfiguration(DataExportRepository,_proj);

            _validPipeline = new Pipeline(CatalogueRepository);

            var source = new PipelineComponent(CatalogueRepository, _validPipeline,typeof (BaselineHackerExecuteDatasetExtractionSource), 0);
            var broadcaster = new PipelineComponent(CatalogueRepository, _validPipeline, typeof(SuccessfullyExtractedResultsDocumenter), 1);
            var destination = new PipelineComponent(CatalogueRepository, _validPipeline, typeof(ExecuteFullExtractionToDatabaseMSSql), 2);

            _validPipeline.SourcePipelineComponent_ID = source.ID;
            _validPipeline.DestinationPipelineComponent_ID = destination.ID;
            _validPipeline.SaveToDatabase();

            //a pipeline that is missing the broadcaster
            _invalidPipeline = new Pipeline(CatalogueRepository);

            var sourceInvalid = new PipelineComponent(CatalogueRepository, _invalidPipeline, typeof(BaselineHackerExecuteDatasetExtractionSource), 0);
            var destinationInvalid = new PipelineComponent(CatalogueRepository, _invalidPipeline, typeof(ExecuteFullExtractionToDatabaseMSSql), 2);

            _invalidPipeline.SourcePipelineComponent_ID = sourceInvalid.ID;
            _invalidPipeline.DestinationPipelineComponent_ID = destinationInvalid.ID;
            _invalidPipeline.SaveToDatabase();
            
            _schedule = new AutomateExtractionSchedule(_repo, _proj);
            _config = new AutomateExtraction(_repo, _schedule, _extractionConfiguration);

        }

        [TearDown]
        public void DeleteSlot()
        {
            _invalidPipeline.DeleteInDatabase();
            _validPipeline.DeleteInDatabase();


            _slot.DeleteInDatabase();
            _proj.DeleteInDatabase();
        }

        [Test]
        public void FindRunnableSchedule_NoneExist()
        {
            //nothing available to find
            Assert.IsNull(_finder.GetAutomateExtractionToRunIfAny(_slot));
        }

        [Test]
        public void FindRunnableSchedule_RunNever()
        {   
            _schedule.Pipeline_ID = _validPipeline.ID;
            _schedule.SaveToDatabase();

            try
            {
                _schedule.ExecutionTimescale = AutomationTimeScale.Never;
                _schedule.SaveToDatabase();

                Assert.IsNull(_finder.GetAutomateExtractionToRunIfAny(_slot));

                _schedule.ExecutionTimescale = AutomationTimeScale.Monthly;
                _schedule.SaveToDatabase();
                
                Assert.AreEqual(_config,_finder.GetAutomateExtractionToRunIfAny(_slot));
            }
            finally
            {
                _schedule.DeleteInDatabase();
            }
        }


        [Test]
        public void FindRunnableSchedule_TicketStatusIsNo()
        {
            _repo.CatalogueRepository.MEF.AddTypeToCatalogForTesting(typeof(NeverAllowAnythingTicketing));

            var ticketingSystem = new TicketingSystemConfiguration(_repo.CatalogueRepository, "NeverAllowAnythingTicketing");
            ticketingSystem.Type = typeof (NeverAllowAnythingTicketing).FullName;
            ticketingSystem.SaveToDatabase();
            
            _schedule.ExecutionTimescale = AutomationTimeScale.Monthly;
            _schedule.Ticket = "FISH";
            _schedule.Pipeline_ID = _validPipeline.ID;
            _schedule.SaveToDatabase();

            try
            {
                Assert.Null( _finder.GetAutomateExtractionToRunIfAny(_slot));
                ticketingSystem.DeleteInDatabase();

                Assert.AreEqual(_config,_finder.GetAutomateExtractionToRunIfAny(_slot));
            }
            finally
            {
                _schedule.DeleteInDatabase();
            }
        }

        [Test]
        public void FindRunnableSchedule_AnotherScheduleIsAlreadyRunning()
        {
            
            _schedule.ExecutionTimescale = AutomationTimeScale.Monthly;
            _schedule.Pipeline_ID = _validPipeline.ID;
            _schedule.SaveToDatabase();

            var job = _slot.AddNewJob(AutomationJobType.UserCustomPipeline, "RE:1");
            
            try
            {
                //other job is in there so it prevents the schedule running
                Assert.IsNull(_finder.GetAutomateExtractionToRunIfAny(_slot));
                
                //job completes itself
                job.DeleteInDatabase();

                //schedule now found
                Assert.AreEqual(_config, _finder.GetAutomateExtractionToRunIfAny(_slot));
            }
            finally
            {
                _schedule.DeleteInDatabase();
            }
        }
        [Test]
        public void FindRunnableSchedule_PipelineMissing()
        {
            
            _schedule.ExecutionTimescale = AutomationTimeScale.Monthly;
            _schedule.Pipeline_ID = null;
            _schedule.SaveToDatabase();

            try
            {
                Assert.IsNull(_finder.GetAutomateExtractionToRunIfAny(_slot));

                _schedule.Pipeline_ID = _validPipeline.ID;
                _schedule.SaveToDatabase();

                //schedule now found
                Assert.AreEqual(_config, _finder.GetAutomateExtractionToRunIfAny(_slot));

            }
            finally
            {
                _schedule.DeleteInDatabase();
            }
        }
    

        [Test]
        public void FindRunnableSchedule_PipelineInvalid()
        {
            _schedule.ExecutionTimescale = AutomationTimeScale.Monthly;
            _schedule.Pipeline_ID = _invalidPipeline.ID;
            _schedule.SaveToDatabase();
            
            try
            {
                //pipeline is invalid so should not be found
                Assert.IsNull(_finder.GetAutomateExtractionToRunIfAny(_slot));

                _schedule.Pipeline_ID = _validPipeline.ID;
                _schedule.SaveToDatabase();

                //schedule now found
                Assert.AreEqual(_config, _finder.GetAutomateExtractionToRunIfAny(_slot));
            }
            finally
            {
                _schedule.DeleteInDatabase();
            }
        }

        [Test]
        public void FindRunnableSchedule_ScheduleWasRunRecently()
        {
            
            //runs daily
            _schedule.ExecutionTimescale = AutomationTimeScale.Daily;
            _schedule.Pipeline_ID = _validPipeline.ID;
            _schedule.SaveToDatabase();

            try
            {
                //configuration was run recently
                _config.BaselineDate = DateTime.Now;
                _config.SaveToDatabase();
                
                Assert.IsNull(_finder.GetAutomateExtractionToRunIfAny(_slot));

                //reset the configuration, they will get everything again
                _config.BaselineDate = null;
                _config.SaveToDatabase();

                //schedule now found
                Assert.AreEqual(_config, _finder.GetAutomateExtractionToRunIfAny(_slot));
            }
            finally
            {
                _schedule.DeleteInDatabase();
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

            _schedule.ExecutionTimescale = AutomationTimeScale.Monthly;
            _schedule.Ticket = "FISH";
            _schedule.Pipeline_ID = _validPipeline.ID;
            _schedule.SaveToDatabase();

            
            try
            {
                //schedule now found
                Assert.AreEqual(_config,_finder.GetAutomateExtractionToRunIfAny(_slot));
            }
            finally
            {
                if(createATicketingSystem)
                    ticketingSystem.DeleteInDatabase();

                _schedule.DeleteInDatabase();
            }
        }
    }
}
