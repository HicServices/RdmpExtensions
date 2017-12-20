using System.Drawing;
using CatalogueLibrary.Data;
using CatalogueManager.CommandExecution.AtomicCommands;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using MapsDirectlyToDatabaseTableUI;
using ReusableLibraryCode.CommandExecution.AtomicCommands;
using ReusableLibraryCode.Icons.IconProvision;

namespace LoadModules.Extensions.AutomationPluginsUIs.CommandExecution.AtomicCommands
{
    [System.ComponentModel.DesignerCategory("")]
    public class ExecuteCommandCreateNewAutomationPluginsDatabase : BasicUICommandExecution, IAtomicCommand
    {
        private readonly AutomationPluginInterface _plugin;
        
        public ExecuteCommandCreateNewAutomationPluginsDatabase(AutomationPluginInterface plugin, IActivateItems activator) : base(activator)
        {
            _plugin = plugin;
        }

        public override string GetCommandName()
        {
            return "Create new Automation Plugin Database";
        }

        public Image GetImage(IIconProvider iconProvider)
        {
            return Activator.CoreIconProvider.GetImage(RDMPConcept.ExternalDatabaseServer, OverlayKind.Add);
        }

        public override void Execute()
        {
            base.Execute();
            CreatePlatformDatabase.CreateNewExternalServer(Activator.RepositoryLocator.CatalogueRepository, ServerDefaults.PermissableDefaults.None,
                typeof(LoadModules.Extensions.AutomationPlugins.Database.Class1).Assembly);
            _plugin.RefreshPluginUserInterfaceRepoAndObjects();
        }
    }
}