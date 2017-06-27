using System;
using System.Data;
using System.Windows.Forms;
using CatalogueManager.TestsAndSetup.ServicePropogation;
using DataExportLibrary.Interfaces.Data.DataTables;
using DataExportLibrary.Interfaces.Repositories;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.Repositories;
using MapsDirectlyToDatabaseTableUI;
using RDMPStartup;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableUIComponents;

namespace LoadModules.Extensions.Interactive.DeAnonymise
{
    public partial class DeAnonymiseAgainstCohortUI : RDMPForm, IDeAnonymiseAgainstCohortConfigurationFulfiller
    {
        private readonly DataTable _toProcess;
        private IDataExportRepository _dataExportRepository;
        public IExtractableCohort ChosenCohort { get; set; }
        public string OverrideReleaseIdentifier { get; set; }
        

        public DeAnonymiseAgainstCohortUI(DataTable toProcess)
        {
            _toProcess = toProcess;
            InitializeComponent();

            try
            {
                var finder = new RegistryRepositoryFinder();
                _dataExportRepository = finder.DataExportRepository;
            }
            catch (Exception e)
            {
                throw new Exception("Error occurred when consulting Registry about the location of DataExport database",e);
            }

            if (_dataExportRepository == null)
                throw new Exception("DataExportRepository was not set so cannot fetch list of cohorts to advertise to user at runtime");
         
            foreach (DataColumn column in toProcess.Columns)
            {
                Button b = new Button();
                b.Text = column.ColumnName;
                flowLayoutPanel1.Controls.Add(b);
                b.Click += b_Click;
            }
        }
        
        void b_Click(object sender, EventArgs e)
        {
            OverrideReleaseIdentifier = ((Button) sender).Text;
            CheckCohortHasCorrectColumns();
        }

       
        private void btnChooseCohort_Click(object sender, EventArgs e)
        {
            SelectIMapsDirectlyToDatabaseTableDialog dialog = new SelectIMapsDirectlyToDatabaseTableDialog(_dataExportRepository.GetAllObjects<ExtractableCohort>(), false, false);
            if(dialog.ShowDialog() == DialogResult.OK)
                if (dialog.Selected != null)
                {
                    ChosenCohort = (ExtractableCohort)dialog.Selected;
                    CheckCohortHasCorrectColumns();
                }

        }

        private void CheckCohortHasCorrectColumns()
        {
            string release = OverrideReleaseIdentifier ?? SqlSyntaxHelper.GetRuntimeName(ChosenCohort.GetReleaseIdentifier());

            if (!_toProcess.Columns.Contains(release))
                checksUI1.OnCheckPerformed(
                    new CheckEventArgs(
                        "Cannot deanonymise table because it contains no release identifier field (should be called " +
                        release + ")", CheckResult.Fail));
            else
                checksUI1.OnCheckPerformed(new CheckEventArgs("Found column " + release + " in your DataTable",
                    CheckResult.Success));
            
            lblExpectedReleaseIdentifierColumn.Text = release;
        }

        private void cbOverrideReleaseIdentifierColumn_CheckedChanged(object sender, EventArgs e)
        {
            if (cbOverrideReleaseIdentifierColumn.Checked == false)
                OverrideReleaseIdentifier = null;

            flowLayoutPanel1.Enabled = cbOverrideReleaseIdentifierColumn.Checked;


        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ChosenCohort = null;
            OverrideReleaseIdentifier = null;
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

    }

    public interface IDeAnonymiseAgainstCohortConfigurationFulfiller
    {
        IExtractableCohort ChosenCohort { get; set; }
        string OverrideReleaseIdentifier { get; set; }
    }
}
