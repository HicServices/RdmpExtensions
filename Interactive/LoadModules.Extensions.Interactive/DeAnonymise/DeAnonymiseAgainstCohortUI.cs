using System;
using System.Data;
using System.Windows.Forms;
using FAnsi.Implementations.MicrosoftSQL;
using MapsDirectlyToDatabaseTable;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.Repositories;
using Rdmp.Core.Startup;
using Rdmp.UI.SimpleDialogs;
using ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.Interactive.DeAnonymise
{
    public partial class DeAnonymiseAgainstCohortUI : Form, IDeAnonymiseAgainstCohortConfigurationFulfiller
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
                var finder = new UserSettingsRepositoryFinder();
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
            var dialog = new SelectDialog<IMapsDirectlyToDatabaseTable>(null, _dataExportRepository.GetAllObjects<ExtractableCohort>(), false, false);
            if(dialog.ShowDialog() == DialogResult.OK)
                if (dialog.Selected != null)
                {
                    ChosenCohort = (ExtractableCohort)dialog.Selected;
                    CheckCohortHasCorrectColumns();
                }

        }

        private void CheckCohortHasCorrectColumns()
        {
            string release = OverrideReleaseIdentifier ?? new MicrosoftQuerySyntaxHelper().GetRuntimeName(ChosenCohort.GetReleaseIdentifier());

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
