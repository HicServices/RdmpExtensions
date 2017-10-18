using System.Linq;
using CatalogueLibrary.CommandExecution.AtomicCommands.PluginCommands;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using MapsDirectlyToDatabaseTableUI;

namespace LoadModules.Extensions.ReleasePlugins.Database
{
    public class Initialize : PluginDatabaseAtomicCommand
    {
        public Initialize(IRDMPPlatformRepositoryServiceLocator repositoryLocator) : base(repositoryLocator)
        {
            if (repositoryLocator.CatalogueRepository
                    .GetAllObjects<ExternalDatabaseServer>()
                    .Any(s => s.CreatedByAssembly == typeof(Database.Class1).Assembly.GetName().Name))
                SetImpossible("Webdav Audit DB already exists");
        }

        public override string GetCommandName()
        {
            return "Initialize Webdav Audit DB";
        }

        public override void Execute()
        {
            base.Execute();
            CreatePlatformDatabase.CreateNewExternalServer(RepositoryLocator.CatalogueRepository, ServerDefaults.PermissableDefaults.None,
                typeof(Database.Class1).Assembly);
        }
    }
}