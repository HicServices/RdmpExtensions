using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueManager.ItemActivation;
using CatalogueManager.TestsAndSetup.ServicePropogation;
using LoadModules.Extensions.AutomationPlugins.Data;
using ReusableUIComponents;
using ReusableUIComponents.ScintillaHelper;
using ScintillaNET;

namespace LoadModules.Extensions.AutomationPlugins.UserInterfaceComponents.Tabs
{
    public partial class SuccessfullyExtractedResultsUI : SuccessfullyExtractedResultsUI_Design
    {
        private Scintilla _scintilla;

        public SuccessfullyExtractedResultsUI()
        {
            InitializeComponent();

            var factory = new ScintillaTextEditorFactory();
            _scintilla = factory.Create();
            splitContainer1.Panel1.Controls.Add(_scintilla);
        }

        public override void SetDatabaseObject(IActivateItems activator, SuccessfullyExtractedResults databaseObject)
        {
            base.SetDatabaseObject(activator, databaseObject);

            _scintilla.Text = databaseObject.SQL;

        }
    }

    [TypeDescriptionProvider(typeof(AbstractControlDescriptionProvider<SuccessfullyExtractedResultsUI_Design, UserControl>))]
    public abstract class SuccessfullyExtractedResultsUI_Design : RDMPSingleDatabaseObjectControl<SuccessfullyExtractedResults>
    {
    }
}
