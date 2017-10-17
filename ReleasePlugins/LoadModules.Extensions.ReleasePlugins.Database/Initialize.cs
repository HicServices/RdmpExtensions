using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using MapsDirectlyToDatabaseTableUI;

namespace LoadModules.Extensions.ReleasePlugins.Database
{
    public class Initialize : IPluginDbInitialize
    {
        public bool Init(CatalogueRepository catalogueRepository)
        {
            CreatePlatformDatabase.CreateNewExternalServer(catalogueRepository, ServerDefaults.PermissableDefaults.None,
                typeof(Database.Class1).Assembly);
            return true;

            //new ToolStripMenuItem("Create New '" + type.Name + "' Server...", addIcon, type.GetMethod("Init"))
        }
    }
}