using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Dashboarding;
using CatalogueManager.DashboardTabs.Construction;
using CatalogueManager.ItemActivation;
using CatalogueManager.Refreshing;
using LoadModules.Extensions.AutomationPlugins.Data;
using MapsDirectlyToDatabaseTable.Versioning;
using MapsDirectlyToDatabaseTableUI;
using ReusableLibraryCode.DataAccess;
using ReusableUIComponents;
using roundhouse.infrastructure.extensions;

namespace LoadModules.Extensions.AutomationPlugins.DashboardComponents
{
    public partial class AutomateExtractionDashboard : UserControl, IDashboardableControl
    {
        private IActivateItems _activator;
        private IPersistableObjectCollection _collection;
        private AutomateExtractionRepository _automationRepository;
        private AutomateExtractionRepositoryFinder _locator;

        public AutomateExtractionDashboard()
        {
            InitializeComponent();
        }

        public void RefreshBus_RefreshObject(object sender, RefreshObjectEventArgs e)
        {
            
        }

        public void SetCollection(IActivateItems activator, IPersistableObjectCollection collection)
        {
            _activator = activator;
            _collection = collection;

            _locator = new AutomateExtractionRepositoryFinder(_activator.RepositoryLocator);
            _automationRepository = _locator.GetRepositoryIfAny();
            btnCreateAutomationDatabase.Visible = _automationRepository == null;
        }


        public IPersistableObjectCollection GetCollection()
        {
            return _collection;
        }

        public string GetTabName()
        {
            return "";
        }

        public void NotifyEditModeChange(bool isEditModeOn)
        {
            
        }

        public IPersistableObjectCollection ConstructEmptyCollection(DashboardControl databaseRecord)
        {
            return new AutomateExtractionDashboardObjectCollection();
        }

        private void btnCreateAutomationDatabase_Click(object sender, EventArgs e)
        {
            var server = CreatePlatformDatabase.CreateNewExternalServer(_activator.RepositoryLocator.CatalogueRepository, ServerDefaults.PermissableDefaults.None, typeof(Database.Class1).Assembly);

            if (server != null)
            {
                _automationRepository = _locator.GetRepositoryIfAny();
                btnCreateAutomationDatabase.Visible = _automationRepository == null;
            }
        }
    }
}
