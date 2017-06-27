using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.Repositories;

namespace LoadModules.Extensions.AutomationPlugins.DataExport
{
    [Export(typeof(ICustomUI<AutomatedExtractionConfiguration>))]
    public partial class AutomatedExtractionConfigurationUI : Form,ICustomUI<AutomatedExtractionConfiguration>
    {
        public ICatalogueRepository CatalogueRepository { get; set; }

        private AutomatedExtractionConfiguration _instance;

        public AutomatedExtractionConfigurationUI()
        {
            InitializeComponent();
        }

        public void SetGenericUnderlyingObjectTo(ICustomUIDrivenClass value, DataTable previewIfAvailable)
        {
            SetUnderlyingObjectTo((AutomatedExtractionConfiguration) value,previewIfAvailable);
        }

        public ICustomUIDrivenClass GetGenericFinalStateOfUnderlyingObject()
        {
            return _instance;
        }

        
        public void SetUnderlyingObjectTo(AutomatedExtractionConfiguration value, DataTable previewIfAvailable)
        {
            _instance = value;

        }

        public AutomatedExtractionConfiguration GetFinalStateOfUnderlyingObject()
        {
            return _instance;
        }
    }
}
