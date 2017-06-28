using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueManager.Icons.IconOverlays;
using CatalogueManager.ItemActivation;
using CatalogueManager.PluginChildProvision;
using CatalogueManager.Refreshing;
using DataExportLibrary.Data.DataTables;
using LoadModules.Extensions.AutomationPlugins.Data;

namespace LoadModules.Extensions.AutomationPlugins.UserInterfaceComponents
{
    public class AutomationPluginInterface:PluginUserInterface, IRefreshBusSubscriber
    {
        private AutomateExtractionRepository _automationRepository;
        private Bitmap _executionScheduleIcon;

        public ExecutionSchedule[] AllSchedules { get; set; }

        public AutomationPluginInterface(IActivateItems itemActivator) : base(itemActivator)
        {
            var repoLocator = new AutomateExtractionRepositoryFinder(itemActivator.RepositoryLocator);
            _automationRepository = repoLocator.GetRepositoryIfAny();

            if(_automationRepository == null)
            {
                Exceptions.Add(new Exception("No AutomateExtractionRepository exists"));
                return;
            }
            _executionScheduleIcon = AutomationIcons.ExecutionSchedule;
            itemActivator.RefreshBus.Subscribe(this);
            RefreshListOfSchedules();
        }

        private void RefreshListOfSchedules()
        {
            AllSchedules = _automationRepository.GetAllObjects<ExecutionSchedule>();
        }

        public override object[] GetChildren(object model)
        {
            var p = model as Project;

            if (p == null)
                return null;

            return AllSchedules.Where(s => s.Project_ID == p.ID).ToArray();
        }

        public override ToolStripMenuItem[] GetAdditionalRightClickMenuItems(DatabaseEntity databaseEntity)
        {
            var p = databaseEntity as Project;

            if (p == null)
                return null;

            return new[] {new AddNewScheduleForProjectMenuItem(_automationRepository,ItemActivator, p)};
        }

        public override Control Activate(object sender, object model)
        {
            var lbl = new Label();
            lbl.Text = "Placeholder";
            return lbl;
        }

        public override Bitmap GetImage(object concept, OverlayKind kind = OverlayKind.None)
        {
            if (concept is ExecutionSchedule)
                return _executionScheduleIcon;

            return null;
        }

        public void RefreshBus_RefreshObject(object sender, RefreshObjectEventArgs e)
        {
            //if the published event relates to our repository
            if(e.Object.Repository is AutomateExtractionRepository)
                RefreshListOfSchedules();//update our list of objects
        }
    }
}
