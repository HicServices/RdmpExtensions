using CatalogueLibrary.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using MapsDirectlyToDatabaseTable.Versioning;
using NUnit.Framework;
using ReusableLibraryCode.Checks;
using Tests.Common;

namespace LoadModules.Extensions.AutomationPlugins.Tests
{
    public class TestsRequiringAnAutomationPluginRepository:DatabaseTests
    {
        protected AutomateExtractionRepository _repo;

        [SetUp]
        public void CreateAutomationDatabase()
        {
            var db = DiscoveredServerICanCreateRandomDatabasesAndTablesOn.ExpectDatabase("TEST_AutomationPluginsTests");
            if (db.Exists())
                db.ForceDrop();

            var assembly = typeof(Database.Class1).Assembly;

            MasterDatabaseScriptExecutor executor = new MasterDatabaseScriptExecutor(db.Server.Builder.ConnectionString);
            executor.CreateAndPatchDatabaseWithDotDatabaseAssembly(assembly, new AcceptAllCheckNotifier());

            var server = new ExternalDatabaseServer(RepositoryLocator.CatalogueRepository, "Automation Server", assembly);
            _repo = new AutomateExtractionRepository(RepositoryLocator, db.Server.Builder);
        }

    }
}