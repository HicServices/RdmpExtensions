using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary.Data.Dashboarding;
using CatalogueManager.DashboardTabs.Construction;
using CatalogueManager.ItemActivation;
using CatalogueManager.Refreshing;

namespace LoadModules.Extensions.AutomationPlugins.DashboardComponents
{
    public partial class AutomateExtractionDashboard : UserControl, IDashboardableControl
    {
        public AutomateExtractionDashboard()
        {
            InitializeComponent();
        }

        public void RefreshBus_RefreshObject(object sender, RefreshObjectEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void SetCollection(IActivateItems activator, IPersistableObjectCollection collection)
        {
            throw new NotImplementedException();
        }

        public IPersistableObjectCollection GetCollection()
        {
            throw new NotImplementedException();
        }

        public string GetTabName()
        {
            throw new NotImplementedException();
        }

        public void NotifyEditModeChange(bool isEditModeOn)
        {
            throw new NotImplementedException();
        }

        public IPersistableObjectCollection ConstructEmptyCollection(DashboardControl databaseRecord)
        {
            throw new NotImplementedException();
        }
    }
}
