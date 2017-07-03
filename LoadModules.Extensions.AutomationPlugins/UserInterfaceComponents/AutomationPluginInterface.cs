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
using LoadModules.Extensions.AutomationPlugins.UserInterfaceComponents.MenuItems;
using LoadModules.Extensions.AutomationPlugins.UserInterfaceComponents.Tabs;

namespace LoadModules.Extensions.AutomationPlugins.UserInterfaceComponents
{
    public class AutomationPluginInterface:PluginUserInterface, IRefreshBusSubscriber
    {
        private AutomateExtractionRepository _automationRepository;
        private Bitmap _executionScheduleIcon;

        public AutomateExtractionSchedule[] AllSchedules { get; set; }

        public AutomationPluginInterface(IActivateItems itemActivator) : base(itemActivator)
        {
            _executionScheduleIcon = AutomationIcons.ExecutionSchedule;
            itemActivator.RefreshBus.Subscribe(this);
            RefreshPluginUserInterfaceRepoAndObjects();
        }

        public void RefreshPluginUserInterfaceRepoAndObjects()
        {
            var repoLocator = new AutomateExtractionRepositoryFinder(ItemActivator.RepositoryLocator);
            _automationRepository = repoLocator.GetRepositoryIfAny();

            AllSchedules = _automationRepository != null ? _automationRepository.GetAllObjects<AutomateExtractionSchedule>() : new AutomateExtractionSchedule[0];
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

            RefreshPluginUserInterfaceRepoAndObjects();

            if (_automationRepository == null)
                return new[]{new CreateNewAutomationPluginsDatabase(this,ItemActivator)};
            
            return new[] {new AddNewScheduleForProjectMenuItem(this,_automationRepository,ItemActivator, p)};
        }

        public override void Activate(object sender, object model)
        {

            var schedule = model as AutomateExtractionSchedule;

            //no control because activation isn't for us (could be a Catalogue or anything)
            if (schedule != null)
            {
                var tab = new AutomateExtractionScheduleTab();
                ItemActivator.ShowRDMPSingleDatabaseObjectControl(tab, schedule);
            }
        }

        public override Bitmap GetImage(object concept, OverlayKind kind = OverlayKind.None)
        {
            if (concept is AutomateExtractionSchedule)
                return _executionScheduleIcon;

            return null;
        }

        public void RefreshBus_RefreshObject(object sender, RefreshObjectEventArgs e)
        {
            //if the published event relates to our repository
            if(e.Object.Repository is AutomateExtractionRepository)
                RefreshPluginUserInterfaceRepoAndObjects();//update our list of objects

            if(e.Object is ExternalDatabaseServer)
                RefreshPluginUserInterfaceRepoAndObjects();//update our list of objects
        }
    }
}
