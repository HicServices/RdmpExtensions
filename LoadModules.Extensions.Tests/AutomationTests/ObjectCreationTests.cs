using System;
using Microsoft.Data.SqlClient;
using System.Linq;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;
using NUnit.Framework;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Pipelines;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.Logging;

namespace LoadModules.Extensions.AutomationPlugins.Tests
{
    public class ObjectCreationTests : TestsRequiringAnAutomationPluginRepository
    {
        [Test]
        public void CreateAllObjects()
        {
            //Schedule
            var proj = new Project(Repo.DataExportRepository, "My cool project");
            var schedule = new AutomateExtractionSchedule(Repo, proj);

            Assert.IsTrue(schedule.Exists());
            Assert.AreEqual(schedule.Project_ID , proj.ID);

            //Configurations
            var config = new ExtractionConfiguration(Repo.DataExportRepository, proj);
            config.Name = "Configuration1";
            config.SaveToDatabase();
            
            //Permission to use a given configuration
            AutomateExtraction automate = new AutomateExtraction(Repo,schedule,config);
            Assert.AreEqual(automate.ExtractionConfiguration_ID,config.ID);
            Assert.AreEqual(automate.Disabled ,false);
            Assert.IsNull(automate.BaselineDate);

            //Baseline (for when an extraction executes and a baseline is created for the datasets)
            
            //should cascade delete everything
            schedule.DeleteInDatabase();
            
            config.DeleteInDatabase();
            proj.DeleteInDatabase();

            Assert.IsFalse(schedule.Exists());
            Assert.IsFalse(proj.Exists());

        }

        [Test]
        public void CreateQueuedExecution()
        {
            Project project = new Project(Repo.DataExportRepository,"proj");
            ExtractionConfiguration configuration= new ExtractionConfiguration(Repo.DataExportRepository,project);
            
            Pipeline p = new Pipeline(Repo.CatalogueRepository);

            var que = new QueuedExtraction(Repo, configuration, p, DateTime.Now.AddHours(1));
            Assert.IsTrue(que.Exists());
            Assert.Greater(que.DueDate, DateTime.Now);

            que.DeleteInDatabase();

            p.DeleteInDatabase();

            project.DeleteInDatabase();

        }

        [Test]
        public void SimulateExtractExecutionOfANewBaseline()
        {
            //Schedule
            var proj = new Project(Repo.DataExportRepository, "My cool project");
            var config = new ExtractionConfiguration(Repo.DataExportRepository, proj);

            var cata = new Catalogue(Repo.CatalogueRepository, "MyDataset");
            var ds = new ExtractableDataSet(Repo.DataExportRepository,cata);

            var schedule = new AutomateExtractionSchedule(Repo, proj);
            var automateConfig = new AutomateExtraction(Repo, schedule, config);

            var results = new SuccessfullyExtractedResults(Repo, "Select * from CannonballLand", automateConfig,ds);

            //shouldn't be able to create a second audit of the same ds
            Assert.Throws<SqlException>(() => new SuccessfullyExtractedResults(Repo, "Select * from FantasyLand", automateConfig, ds));

            Assert.IsTrue(results.Exists());

            //ensures accumulator only has the lifetime of a single data load execution
            var acc = IdentifierAccumulator.GetInstance(DataLoadInfo.Empty);
            acc.AddIdentifierIfNotSee("123");
            acc.AddIdentifierIfNotSee("12");
            acc.AddIdentifierIfNotSee("123");

            acc.CommitCurrentState(Repo,automateConfig);

            var dt = automateConfig.GetIdentifiersTable();
            Assert.AreEqual(dt.Rows.Count,2);

            //next dataset executes in parallel race conditions galore!
            acc = IdentifierAccumulator.GetInstance(DataLoadInfo.Empty);
            acc.AddIdentifierIfNotSee("22");
            acc.CommitCurrentState(Repo, automateConfig);

            dt = automateConfig.GetIdentifiersTable();
            Assert.AreEqual(dt.Rows.Count, 3);

            automateConfig.ClearBaselines();
            dt = automateConfig.GetIdentifiersTable();
            Assert.AreEqual(dt.Rows.Count, 0);
        }
    }
}