using System;
using System.Windows.Forms;
using CatalogueManager.Icons.IconOverlays;
using CatalogueManager.ItemActivation;
using CatalogueManager.Refreshing;
using DataExportLibrary.Data.DataTables;
using LoadModules.Extensions.AutomationPlugins.Data;

namespace LoadModules.Extensions.AutomationPlugins.UserInterfaceComponents
{
    [System.ComponentModel.DesignerCategory("")]
    public class AddNewScheduleForProjectMenuItem : ToolStripMenuItem
    {
        private readonly AutomateExtractionRepository _automationRepository;
        private readonly IActivateItems _itemActivator;
        private readonly Project _project;

        public AddNewScheduleForProjectMenuItem(AutomateExtractionRepository automationRepository, IActivateItems itemActivator, Project project)
            : base(
                "Add New Schedule For Project"
                )
        {
            Image = new IconOverlayProvider().GetOverlayNoCache(AutomationIcons.ExecutionSchedule, OverlayKind.Add);
            _automationRepository = automationRepository;
            _itemActivator = itemActivator;
            _project = project;
        }

        protected override void OnClick(EventArgs e)
        {
            var schedule = new ExecutionSchedule(_automationRepository, _project);
            _itemActivator.RefreshBus.Publish(this,new RefreshObjectEventArgs(schedule));
        }
    }
}