using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueManager.CommandExecution.AtomicCommands.UIFactory;
using CatalogueManager.ItemActivation;
using CatalogueManager.PluginChildProvision;
using CatalogueManager.Refreshing;
using DataExportLibrary.Data.DataTables;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using LoadModules.Extensions.AutomationPluginsUIs.CommandExecution.AtomicCommands;
using ReusableLibraryCode.Icons.IconProvision;

namespace LoadModules.Extensions.AutomationPluginsUIs
{
    public class AutomationPluginInterface : PluginUserInterface, IRefreshBusSubscriber
    {
        private AutomateExtractionRepository _automationRepository;
        private Bitmap _executionScheduleIcon;
        private AtomicCommandUIFactory _atomicCommandFactory;

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
            _automationRepository = repoLocator.GetRepositoryIfAny() as AutomateExtractionRepository;

            AllSchedules = _automationRepository != null ? _automationRepository.GetAllObjects<AutomateExtractionSchedule>() : new AutomateExtractionSchedule[0];
        }

        public override object[] GetChildren(object model)
        {
            var p = model as Project;

            if (p == null)
                return null;

            return AllSchedules.Where(s => s.Project_ID == p.ID).ToArray();
        }
        
        public override ToolStripMenuItem[] GetAdditionalRightClickMenuItems(object databaseEntity)
        {
            var p = databaseEntity as Project;
            var c = databaseEntity as ExtractionConfiguration;
            
            if (p == null && c == null)
                return null;
            
            _atomicCommandFactory = new AtomicCommandUIFactory(ItemActivator.CoreIconProvider);

            RefreshPluginUserInterfaceRepoAndObjects();

            if (_automationRepository == null)
                return new[]
                {
                    _atomicCommandFactory.CreateMenuItem(new ExecuteCommandCreateNewAutomationPluginsDatabase(this, ItemActivator))
                };

            if (c != null)
                return new[]
                {
                    _atomicCommandFactory.CreateMenuItem(new ExecuteCommandEnqueueExtractionMenuItem(_automationRepository, c, ItemActivator))
                };

            return new[]
            {
                _atomicCommandFactory.CreateMenuItem(new AddNewScheduleForProjectMenuItem(this, _automationRepository, ItemActivator, p))
            };
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
