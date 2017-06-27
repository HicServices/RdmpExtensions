using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary.Data.Dashboarding;
using MapsDirectlyToDatabaseTable;

namespace LoadModules.Extensions.AutomationPlugins.DashboardComponents
{
    public class AutomateExtractionDashboardObjectCollection:IPersistableObjectCollection
    {
        public PersistStringHelper Helper { get; private set; }

        public List<IMapsDirectlyToDatabaseTable> DatabaseObjects { get; set; }

        public AutomateExtractionDashboardObjectCollection()
        {
            DatabaseObjects = new List<IMapsDirectlyToDatabaseTable>();
            Helper = new PersistStringHelper();
        }
        public string SaveExtraText()
        {
            return "";
        }

        public void LoadExtraText(string s)
        {
            
        }
    }
}
