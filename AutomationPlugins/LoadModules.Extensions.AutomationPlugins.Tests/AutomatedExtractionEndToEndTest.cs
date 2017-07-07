using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.Data.Pipelines;
using DataExportLibrary.Tests.DataExtraction;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using LoadModules.Extensions.AutomationPlugins.Execution.AutomationPipeline;
using NUnit.Framework;
using RDMPAutomationService;
using roundhouse.infrastructure.extensions;

namespace LoadModules.Extensions.AutomationPlugins.Tests
{
    public class AutomatedExtractionEndToEndTest:TestsRequiringAnExtractionConfiguration
    {
        private AutomateExtractionRepository _automateExtractionRepository;

        [Test]
        public void EndToEnd()
        {
            _automateExtractionRepository = TestsRequiringAnAutomationPluginRepository.CreateAutomationDatabaseStatic(DiscoveredServerICanCreateRandomDatabasesAndTablesOn, RepositoryLocator);
            
            var schedule = new AutomateExtractionSchedule(_automateExtractionRepository, _project);
            var executeConfiguration = new AutomateExtraction(_automateExtractionRepository, schedule, _configuration);
            
            schedule.ExecutionTimescale = AutomationTimeScale.Weekly;
            schedule.Pipeline_ID = TestsRequiringAnAutomationPluginRepository.GetValidExtractionPipelineStatic(CatalogueRepository).ID;
            schedule.SaveToDatabase();
            
            var slot = new AutomationServiceSlot(RepositoryLocator.CatalogueRepository);

            Pipeline automationPipeline = new Pipeline(RepositoryLocator.CatalogueRepository,"AutomationPipelineTest");
            PipelineComponent automationSource = new PipelineComponent(RepositoryLocator.CatalogueRepository,automationPipeline,typeof(AutomatedExtractionSource),0);
            automationSource.CreateArgumentsForClassIfNotExists<AutomatedExtractionSource>();
            

            automationPipeline.SourcePipelineComponent_ID = automationSource.ID;
            automationPipeline.SaveToDatabase();
            
            var customPipe = new AutomateablePipeline(RepositoryLocator.CatalogueRepository, slot,automationPipeline);

            _project.ExtractionDirectory = Path.Combine(Environment.CurrentDirectory, "AutomatedExtractionEndToEndTest");
            _project.SaveToDatabase();

            DirectoryInfo d = new DirectoryInfo(_project.ExtractionDirectory);

            if (!d.Exists)
                d.Create();
            else
                foreach (var oldExtractionDir in d.GetDirectories())//clear out any remnants
                    oldExtractionDir.Delete(true);
            
            RDMPAutomationLoop loop = new RDMPAutomationLoop(RepositoryLocator, slot);
            loop.Start();

            int timeout = 500000;
            while(loop.StillRunning && (timeout -= 100) > 0)
            {
                var exceptions = RepositoryLocator.CatalogueRepository.GetAllObjects<AutomationServiceException>();

                if(exceptions.Any())
                    Assert.Fail(exceptions.First().Exception);

                if(d.GetDirectories().Any())
                {
                    Console.WriteLine("Found Directories:"+  d + "(" + string.Join(",",d.GetFiles().Select(f=>f.ToString()))+")");
                    loop.Stop = true;
                }

                Thread.Sleep(100);
            }

            if(timeout <= 0)
                Assert.Fail("Never executed the pipe, never created any files, Never generated any exceptions in the automation server exceptions area");

            var logManager = _configuration.GetExplicitLoggingDatabaseServerOrDefault();

            var archive = logManager.GetArchivalLoadInfoFor(RoutineExtractionRun.LoggingTaskName, new CancellationToken());

            var log =archive.OrderByDescending(a => a.StartTime).FirstOrDefault();

            if(log == null)
                Assert.Fail("No Log was created");

            Console.WriteLine("Log was created called:" + log.Description);
            
            var tableLog = log.TableLoadInfos.Single();
            Assert.AreEqual(tableLog.Inserts,1);

        }
    }
}
