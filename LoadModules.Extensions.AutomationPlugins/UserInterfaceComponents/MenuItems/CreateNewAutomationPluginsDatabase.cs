using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueManager.Icons.IconOverlays;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using MapsDirectlyToDatabaseTableUI;

namespace LoadModules.Extensions.AutomationPlugins.UserInterfaceComponents.MenuItems
{
    [System.ComponentModel.DesignerCategory("")]
    public class CreateNewAutomationPluginsDatabase:ToolStripMenuItem
    {
        private readonly AutomationPluginInterface _plugin;
        private readonly IActivateItems _itemActivator;

        public CreateNewAutomationPluginsDatabase(AutomationPluginInterface plugin, IActivateItems itemActivator):base(
            "Create new Automation Plugin Database", 
            itemActivator.CoreIconProvider.GetImage(RDMPConcept.ExternalDatabaseServer, OverlayKind.Add))
        {
            _plugin = plugin;
            _itemActivator = itemActivator;
        }

        protected override void OnClick(System.EventArgs e)
        {
            CreatePlatformDatabase.CreateNewExternalServer(_itemActivator.RepositoryLocator.CatalogueRepository,ServerDefaults.PermissableDefaults.None, typeof (Database.Class1).Assembly);
            _plugin.RefreshPluginUserInterfaceRepoAndObjects();
        }
    }
}