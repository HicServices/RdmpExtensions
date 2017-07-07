using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using DataExportLibrary.Data.DataTables;
using HIC.Logging;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;
using NUnit.Framework;

namespace LoadModules.Extensions.AutomationPlugins.Tests
{
    public class ObjectCreationTests : TestsRequiringAnAutomationPluginRepository
    {
        [Test]
        public void CreateAllObjects()
        {
            //Schedule
            var proj = new Project(_repo.DataExportRepository, "My cool project");
            var schedule = new AutomateExtractionSchedule(_repo, proj);

            Assert.IsTrue(schedule.Exists());
            Assert.AreEqual(schedule.Project_ID , proj.ID);

            //Configurations
            var config = new ExtractionConfiguration(_repo.DataExportRepository, proj);
            config.Name = "Configuration1";
            config.SaveToDatabase();
            
            //Permission to use a given configuration
            AutomateExtraction automate = new AutomateExtraction(_repo,schedule,config);
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
        public void SimulateExtractExecutionOfANewBaseline()
        {
            //Schedule
            var proj = new Project(_repo.DataExportRepository, "My cool project");
            var config = new ExtractionConfiguration(_repo.DataExportRepository, proj);

            var cata = new Catalogue(_repo.CatalogueRepository, "MyDataset");
            var ds = new ExtractableDataSet(_repo.DataExportRepository,cata);

            var schedule = new AutomateExtractionSchedule(_repo, proj);
            var automateConfig = new AutomateExtraction(_repo, schedule, config);

            var results = new SuccessfullyExtractedResults(_repo, "Select * from CannonballLand", automateConfig,ds);

            //shouldn't be able to create a second audit of the same ds
            Assert.Throws<SqlException>(() => new SuccessfullyExtractedResults(_repo, "Select * from FantasyLand", automateConfig, ds));

            Assert.IsTrue(results.Exists());

            //ensures accumulator only has the lifetime of a single data load execution
            var acc = IdentifierAccumulator.GetInstance(DataLoadInfo.Empty);
            acc.AddIdentifierIfNotSee("123");
            acc.AddIdentifierIfNotSee("12");
            acc.AddIdentifierIfNotSee("123");

            acc.CommitCurrentState(_repo,automateConfig);

            var dt = automateConfig.GetIdentifiersTable();
            Assert.AreEqual(dt.Rows.Count,2);

            //next dataset executes in parallel race conditions galore!
            acc = IdentifierAccumulator.GetInstance(DataLoadInfo.Empty);
            acc.AddIdentifierIfNotSee("22");
            acc.CommitCurrentState(_repo, automateConfig);

            dt = automateConfig.GetIdentifiersTable();
            Assert.AreEqual(dt.Rows.Count, 3);

            automateConfig.ClearBaselines();
            dt = automateConfig.GetIdentifiersTable();
            Assert.AreEqual(dt.Rows.Count, 0);

        }
    }
}