using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BrightIdeasSoftware;
using CatalogueManager.Icons.IconOverlays;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using CatalogueManager.SimpleControls;
using CatalogueManager.TestsAndSetup.ServicePropogation;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.ExtractionTime.Commands;
using DataExportLibrary.ExtractionTime.ExtractionPipeline;
using DataExportLibrary.Interfaces.Data.DataTables;
using HIC.Logging;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using LoadModules.Extensions.AutomationPlugins.Execution.AutomationPipeline;
using LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;
using MapsDirectlyToDatabaseTable;
using MapsDirectlyToDatabaseTableUI;
using RDMPObjectVisualisation.Pipelines;
using RDMPObjectVisualisation.Pipelines.PluginPipelineUsers;
using ReusableUIComponents;
using ReusableUIComponents.Icons.IconProvision;

namespace LoadModules.Extensions.AutomationPluginsUIs.Tabs
{
    public partial class AutomateExtractionScheduleTab : AutomateExtractionSchedule_Design,ISaveableUI
    {
        private AutomateExtractionSchedule _schedule;
        IPipelineSelectionUI _selectionUI;

        private Bitmap _extractionConfiguration;
        private Bitmap _extractionConfigurationIconAdd;

        public AutomateExtractionScheduleTab()
        {
            InitializeComponent();
            ticketingControl1.TicketTextChanged += ticketingControl1_TicketTextChanged;
            ticketingControl1.Title = "Ticket";
            
            ddExecutionTimescale.DataSource = Enum.GetValues(typeof (AutomationTimeScale));
            olvConfigurations.BooleanCheckStateGetter = BooleanCheckStateGetter;
            olvConfigurations.BooleanCheckStatePutter = BooleanCheckStatePutter;
            olvConfigurations.RowHeight = 19;

            _extractionConfiguration = CatalogueIcons.ExtractionConfiguration;
            olvName.ImageGetter = e => _extractionConfiguration;

            _extractionConfigurationIconAdd = new IconOverlayProvider().GetOverlayNoCache(_extractionConfiguration, OverlayKind.Add);
            btnAddExtractionConfigurations.Image = _extractionConfigurationIconAdd;

            olvBaselineDate.AspectGetter = LastAttemptAspectGetter;
            olvDeleteBaselineAudit.AspectGetter = DeleteAspectGetter;
            olvCheckAutomation.AspectGetter =(m)=> "Check";
            olvCheckAutomation.ButtonSizing = OLVColumn.ButtonSizingMode.CellBounds;

            olvConfigurations.ButtonClick += olvConfigurations_ButtonClick;

        }

        private object DeleteAspectGetter(object rowObject)
        {
            var a = (AutomateExtraction)rowObject;
            if (a.BaselineDate == null)
                return null;

            return "Clear";
        }

        void olvConfigurations_ButtonClick(object sender, BrightIdeasSoftware.CellClickEventArgs e)
        {
            var a = (AutomateExtraction)e.Model;
            if (e.ColumnIndex == olvDeleteBaselineAudit.Index)
            {
                a.ClearBaselines();
            }

            if (e.ColumnIndex == olvCheckAutomation.Index)
            {
                var finder = new RoutineExtractionRunFinder((AutomateExtractionRepository) _schedule.Repository);

                string reason;
                if (finder.CanRun(a, out reason))
                    MessageBox.Show("Good to go");
                else
                    MessageBox.Show("Cannot run because:" + reason);
            }
        }

        private object LastAttemptAspectGetter(object rowObject)
        {
            var a = (AutomateExtraction) rowObject;
            if (a.BaselineDate == null)
                return "Never";

            return a.BaselineDate;
        }


        private bool BooleanCheckStatePutter(object rowObject, bool newValue)
        {
            var e = ((AutomateExtraction)rowObject);
            e.Disabled = !newValue;
            e.SaveToDatabase();

            return newValue;
        }

        private bool BooleanCheckStateGetter(object rowObject)
        {
            return !((AutomateExtraction) rowObject).Disabled;
        }


        void ticketingControl1_TicketTextChanged(object sender, System.EventArgs e)
        {
            if (_schedule == null)
                return;

            _schedule.Ticket = ticketingControl1.TicketText;
            ragSmileyTicketing.Reset();
            _schedule.CheckTicketing(ragSmileyTicketing);
        }

        public override void SetDatabaseObject(IActivateItems activator, AutomateExtractionSchedule databaseObject)
        {
            _schedule = databaseObject;
            base.SetDatabaseObject(activator, databaseObject);

            if (_selectionUI == null)
            {
                var pipelineHost = new ExtractionPipelineHost();
                var factory = new PipelineSelectionUIFactory(activator.RepositoryLocator.CatalogueRepository, null, pipelineHost);

                _selectionUI = factory.Create();

                _selectionUI = new PipelineSelectionUI<DataTable>(null,null,activator.RepositoryLocator.CatalogueRepository);

                var selectionUIControl = (Control)_selectionUI;
                selectionUIControl.Dock = DockStyle.Fill;
                _selectionUI.PipelineChanged += _selectionUI_PipelineChanged;
                pPipeline.Controls.Add(selectionUIControl);

                saverButton.SetupFor(_schedule,activator.RefreshBus);
            }

            ticketingControl1.TicketText = _schedule.Ticket;
            cbDisabled.Checked = _schedule.Disabled;
            _selectionUI.Pipeline = _schedule.Pipeline;
            ddExecutionTimescale.SelectedItem = _schedule.ExecutionTimescale;
            
            ticketingControl1.ReCheckTicketingSystemInCatalogue();
            lblName.Text = "Name:"+_schedule.Name;

            RefreshObjects();
        }

        private void RefreshObjects()
        {
            olvConfigurations.ClearObjects();
            olvConfigurations.AddObjects(_schedule.AutomateExtractions);
        }

        void _selectionUI_PipelineChanged(object sender, EventArgs e)
        {
            _schedule.Pipeline_ID = _selectionUI.Pipeline != null ? _selectionUI.Pipeline.ID : (int?) null;
            CheckPipeline();
        }

        private void CheckPipeline()
        {
            ragSmileyPipeline.Reset();
            var p = _schedule.Pipeline;
            new AutomatedExtractionPipelineChecker(p).Check(ragSmileyPipeline);
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
            if (_schedule == null)
                return;

            _schedule.ExecutionTimescale = (AutomationTimeScale)ddExecutionTimescale.SelectedItem;
        }

        private void tbComment_TextChanged(object sender, EventArgs e)
        {
            _schedule.Comment = tbComment.Text;
        }

        private void olvConfigurations_KeyUp(object sender, KeyEventArgs e)
        {
            
            if (e.KeyCode == Keys.Delete)
            {
                var del = olvConfigurations.SelectedObject as IDeleteable;

                if (del != null)
                {
                    _activator.DeleteWithConfirmation(this, del);
                    RefreshObjects();
                }
            }
        }

        private void btnAddExtractionConfigurations_Click(object sender, EventArgs e)
        {
            var available = _schedule.GetImportableExtractionConfigurations();

            if (!available.Any())
            {
                MessageBox.Show("There are no new available ExtractionConfigurations that are not already part of this Automate Extraction Schedule");
                return;
            }

            var dialog = new SelectIMapsDirectlyToDatabaseTableDialog(available.Cast<ExtractionConfiguration>(),false, false);
            dialog.AllowMultiSelect = true;

            bool addedAtLeast1 = false;

            if(dialog.ShowDialog() == DialogResult.OK)
            {
                foreach (var config in dialog.MultiSelected)
                {
                    new AutomateExtraction((AutomateExtractionRepository) _schedule.Repository, _schedule,(IExtractionConfiguration) config);
                    addedAtLeast1 = true;
                }
                
            }

            if(addedAtLeast1)
                RefreshObjects();
        }
    }

    [TypeDescriptionProvider(typeof(AbstractControlDescriptionProvider<AutomateExtractionSchedule_Design, UserControl>))]
    public abstract class AutomateExtractionSchedule_Design : RDMPSingleDatabaseObjectControl<AutomateExtractionSchedule>
    {
    }
}
