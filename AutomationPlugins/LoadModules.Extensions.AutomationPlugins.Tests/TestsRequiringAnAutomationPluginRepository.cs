using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Pipelines;
using CatalogueLibrary.Repositories;
using DataExportLibrary.ExtractionTime.ExtractionPipeline.Destinations;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;
using MapsDirectlyToDatabaseTable.Versioning;
using NUnit.Framework;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
using Tests.Common;

namespace LoadModules.Extensions.AutomationPlugins.Tests
{
    public class TestsRequiringAnAutomationPluginRepository:DatabaseTests
    {
        public AutomateExtractionRepository Repo;

        [SetUp]
        public void CreateAutomationDatabase()
        {
            Repo = CreateAutomationDatabaseStatic(DiscoveredServerICanCreateRandomDatabasesAndTablesOn,RepositoryLocator);
        }

        public static AutomateExtractionRepository CreateAutomationDatabaseStatic(DiscoveredServer discoveredServerICanCreateRandomDatabasesAndTablesOn, IRDMPPlatformRepositoryServiceLocator repositoryLocator)
        {
            var db = discoveredServerICanCreateRandomDatabasesAndTablesOn.ExpectDatabase("TEST_AutomationPluginsTests");
            if (db.Exists())
                db.ForceDrop();

            var assembly = typeof(Database.Class1).Assembly;

            MasterDatabaseScriptExecutor executor = new MasterDatabaseScriptExecutor(db.Server.Builder.ConnectionString);
            executor.CreateAndPatchDatabaseWithDotDatabaseAssembly(assembly, new AcceptAllCheckNotifier());

            var server = new ExternalDatabaseServer(repositoryLocator.CatalogueRepository, "Automation Server", assembly);
            server.Server = db.Server.Name;
            server.Database = db.GetRuntimeName();
            server.SaveToDatabase();

            return new AutomateExtractionRepository(repositoryLocator, db.Server.Builder);
        }

        public Pipeline GetValidExtractionPipeline()
        {
            return GetValidExtractionPipelineStatic(CatalogueRepository);
        }

        public static Pipeline GetValidExtractionPipelineStatic(CatalogueRepository catalogueRepository)
        {
            var validPipeline = new Pipeline(catalogueRepository);

            var source = new PipelineComponent(catalogueRepository, validPipeline, typeof(BaselineHackerExecuteDatasetExtractionSource), 0);
            source.CreateArgumentsForClassIfNotExists<BaselineHackerExecuteDatasetExtractionSource>();

            var broadcaster = new PipelineComponent(catalogueRepository, validPipeline, typeof(SuccessfullyExtractedResultsDocumenter), 1);
            
            var destination = new PipelineComponent(catalogueRepository, validPipeline, typeof(ExecuteDatasetExtractionFlatFileDestination), 2);
            destination.CreateArgumentsForClassIfNotExists<ExecuteDatasetExtractionFlatFileDestination>();

            validPipeline.SourcePipelineComponent_ID = source.ID;
            validPipeline.DestinationPipelineComponent_ID = destination.ID;
            validPipeline.SaveToDatabase();

            return validPipeline;
        }
    }
}