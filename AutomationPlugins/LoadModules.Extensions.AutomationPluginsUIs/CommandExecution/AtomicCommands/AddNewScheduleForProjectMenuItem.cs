using System.Drawing;
using CatalogueManager.CommandExecution.AtomicCommands;
using CatalogueManager.Icons.IconOverlays;
using CatalogueManager.ItemActivation;
using CatalogueManager.Refreshing;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.Interfaces.Data.DataTables;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using ReusableUIComponents.CommandExecution.AtomicCommands;
using ReusableUIComponents.Icons.IconProvision;

namespace LoadModules.Extensions.AutomationPluginsUIs.CommandExecution.AtomicCommands
{
    [System.ComponentModel.DesignerCategory("")]
    public class AddNewScheduleForProjectMenuItem : BasicUICommandExecution, IAtomicCommand
    {
        private readonly AutomationPluginInterface _plugin;
        private readonly AutomateExtractionRepository _automationRepository;
        private readonly IActivateItems _itemActivator;
        private readonly Project _project;

        public AddNewScheduleForProjectMenuItem(AutomationPluginInterface plugin, AutomateExtractionRepository automationRepository, IActivateItems itemActivator, Project project) : base(itemActivator) {
            _plugin = plugin;
            _automationRepository = automationRepository;
            _itemActivator = itemActivator;
            _project = project;
        }

        public override string GetCommandName()
        {
            return "Add New Schedule For Project";
        }

        public override void Execute()
        {
            var schedule = new AutomateExtractionSchedule(_automationRepository, _project);
            _plugin.RefreshPluginUserInterfaceRepoAndObjects();
            _itemActivator.RefreshBus.Publish(this,new RefreshObjectEventArgs(_project));

            //start out by making every AutomateExtraction part of the config
            foreach (IExtractionConfiguration configuration in _project.ExtractionConfigurations)
                new AutomateExtraction(_automationRepository,schedule, configuration);
        }

        public Image GetImage(IIconProvider iconProvider)
        {
            return new IconOverlayProvider().GetOverlayNoCache(AutomationIcons.ExecutionSchedule, OverlayKind.Add);
        }
    }
}