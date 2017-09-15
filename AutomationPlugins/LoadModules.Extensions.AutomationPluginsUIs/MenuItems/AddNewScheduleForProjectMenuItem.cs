using System;
using System.Windows.Forms;
using CatalogueManager.Icons.IconOverlays;
using CatalogueManager.ItemActivation;
using CatalogueManager.Refreshing;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.Interfaces.Data.DataTables;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using ReusableUIComponents.Icons.IconProvision;

namespace LoadModules.Extensions.AutomationPluginsUIs.MenuItems
{
    [System.ComponentModel.DesignerCategory("")]
    public class AddNewScheduleForProjectMenuItem : ToolStripMenuItem
    {
        private readonly AutomationPluginInterface _plugin;
        private readonly AutomateExtractionRepository _automationRepository;
        private readonly IActivateItems _itemActivator;
        private readonly Project _project;

        public AddNewScheduleForProjectMenuItem(AutomationPluginInterface plugin, AutomateExtractionRepository automationRepository, IActivateItems itemActivator, Project project)
            : base(
                "Add New Schedule For Project",
                new IconOverlayProvider().GetOverlayNoCache(AutomationIcons.ExecutionSchedule, OverlayKind.Add)
                )
        {
            _plugin = plugin;
            _automationRepository = automationRepository;
            _itemActivator = itemActivator;
            _project = project;
        }

        protected override void OnClick(EventArgs e)
        {
            var schedule = new AutomateExtractionSchedule(_automationRepository, _project);
            _plugin.RefreshPluginUserInterfaceRepoAndObjects();
            _itemActivator.RefreshBus.Publish(this,new RefreshObjectEventArgs(_project));

            //start out by making every AutomateExtraction part of the config
            foreach (IExtractionConfiguration configuration in _project.ExtractionConfigurations)
                new AutomateExtraction(_automationRepository,schedule, configuration);
        }
    }
}