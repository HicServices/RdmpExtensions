using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data.Dashboarding;
using CatalogueLibrary.Data.DataLoad;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.AutomationPlugins.DataExport
{
    [Export(typeof(ICustomUIDrivenClass))]
    [Export(typeof(ICheckable))]
    public class AutomatedExtractionConfiguration : ICustomUIDrivenClass
    {
        public PersistStringHelper Helper { get; private set; }

        public AutomatedExtractionConfiguration()
        {
            Helper = new PersistStringHelper();
            
        }

        public void RestoreStateFrom(string value)
        {
            
        }

        public string SaveStateToString()
        {
            return "";
        }
    }
}
