using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using CatalogueManager.ItemActivation;
using CatalogueManager.SimpleControls;
using CatalogueManager.TestsAndSetup.ServicePropogation;
using DataExportLibrary.ExtractionTime.ExtractionPipeline;
using LoadModules.Extensions.AutomationPlugins.Data;
using RDMPAutomationService;
using RDMPObjectVisualisation.Pipelines;
using ReusableUIComponents;

namespace LoadModules.Extensions.AutomationPlugins.UserInterfaceComponents.Tabs
{
    public partial class AutomateExtractionScheduleTab : AutomateExtractionSchedule_Design,ISaveableUI
    {
        private AutomateExtractionSchedule _schedule;
        PipelineSelectionUI<DataTable> _selectionUI;

        public AutomateExtractionScheduleTab()
        {
            InitializeComponent();
            ticketingControl1.TicketTextChanged += ticketingControl1_TicketTextChanged;
            ticketingControl1.Title = "Ticket";
            ddExecutionTimescale.DataSource = Enum.GetValues(typeof (AutomationTimeScale));
            
        }

        void ticketingControl1_TicketTextChanged(object sender, System.EventArgs e)
        {
            if (_schedule == null)
                return;

            _schedule.Ticket = ticketingControl1.TicketText;
            ragSmiley1.Reset();
            _schedule.CheckTicketing(ragSmiley1);
        }

        public override void SetDatabaseObject(IActivateItems activator, AutomateExtractionSchedule databaseObject)
        {
            _schedule = databaseObject;
            base.SetDatabaseObject(activator, databaseObject);

            if (_selectionUI == null)
            {
                _selectionUI = new PipelineSelectionUI<DataTable>(null,null,activator.RepositoryLocator.CatalogueRepository);
                _selectionUI.Context = ExtractionPipelineHost.Context;
                _selectionUI.Dock = DockStyle.Fill;
                _selectionUI.PipelineChanged += _selectionUI_PipelineChanged;
                pPipeline.Controls.Add(_selectionUI);

                saverButton.SetupFor(_schedule,activator.RefreshBus);
            }

            ticketingControl1.TicketText = _schedule.Ticket;
            cbDisabled.Checked = _schedule.Disabled;
            _selectionUI.Pipeline = _schedule.Pipeline;
            ddExecutionTimescale.SelectedItem = _schedule.ExecutionTimescale;
            
            ticketingControl1.ReCheckTicketingSystemInCatalogue();
            lblName.Text = "Name:"+_schedule.Name;
        }

        void _selectionUI_PipelineChanged(object sender, EventArgs e)
        {
            _schedule.Pipeline_ID = _selectionUI.Pipeline != null ? _selectionUI.Pipeline.ID : (int?) null;
        }

        private void cbDisabled_CheckedChanged(object sender, System.EventArgs e)
        {
            _schedule.Disabled = cbDisabled.Checked;
        }

        public ObjectSaverButton GetObjectSaverButton()
        {
            return saverButton;
        }

        private void ddExecutionTimescale_SelectedIndexChanged(object sender, EventArgs e)
        {
            _schedule.ExecutionTimescale = (AutomationTimeScale)ddExecutionTimescale.SelectedItem;
        }

        private void tbComment_TextChanged(object sender, EventArgs e)
        {
            _schedule.Comment = tbComment.Text;
        }
    }

    [TypeDescriptionProvider(typeof(AbstractControlDescriptionProvider<AutomateExtractionSchedule_Design, UserControl>))]
    public abstract class AutomateExtractionSchedule_Design : RDMPSingleDatabaseObjectControl<AutomateExtractionSchedule>
    {
    }
}
