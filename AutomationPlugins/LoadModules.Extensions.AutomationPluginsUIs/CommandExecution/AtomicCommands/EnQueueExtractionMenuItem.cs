using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CatalogueManager.CommandExecution.AtomicCommands;
using CatalogueManager.Icons.IconOverlays;
using CatalogueManager.ItemActivation;
using DataExportLibrary.Data.DataTables;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using LoadModules.Extensions.AutomationPluginsUIs.Dialogs;
using ReusableUIComponents.CommandExecution.AtomicCommands;
using ReusableUIComponents.Icons.IconProvision;

namespace LoadModules.Extensions.AutomationPluginsUIs.CommandExecution.AtomicCommands
{
    [System.ComponentModel.DesignerCategory("")]
    public class ExecuteCommandEnqueueExtractionMenuItem : BasicUICommandExecution, IAtomicCommand
    {
        private readonly AutomateExtractionRepository _automationRepository;
        private readonly ExtractionConfiguration _extractionConfiguration;

        public ExecuteCommandEnqueueExtractionMenuItem(AutomateExtractionRepository automationRepository, ExtractionConfiguration extractionConfiguration, IActivateItems activator) : base(activator)
        {
            _automationRepository = automationRepository;
            _extractionConfiguration = extractionConfiguration;
        }

        public override string GetCommandName()
        {
            return "Queue One Off Extraction For Specific Time";
        }

        public override void Execute()
        {
         	base.Execute();

            var alreadyQueued = _automationRepository.GetAllObjects<QueuedExtraction>("WHERE ExtractionConfiguration_ID = " + _extractionConfiguration.ID).SingleOrDefault();

            if (alreadyQueued != null)
            {
                if(alreadyQueued.DueDate < DateTime.Now)
                    MessageBox.Show("ExtractionConfiguration '" + _extractionConfiguration +
                                "' was already queued for extraction and is due to be executed right now.  The Automation Server may not be running or it may be busy with other tasks.");
                else if (MessageBox.Show(
                    "ExtractionConfiguration '" + _extractionConfiguration + "' is already queued for execution at:" +
                    alreadyQueued.DueDate + ". Would you like to delete this from the Queue?",
                    "Clear Existing Queued Execution", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    alreadyQueued.DeleteInDatabase();
                    var f = new EnqueueExtractionConfigurationUI(_extractionConfiguration, _automationRepository);
                    f.ShowDialog();
                }
            }
            else
            {
                var f = new EnqueueExtractionConfigurationUI(_extractionConfiguration,_automationRepository);
                f.ShowDialog();
            }
        }

        public Image GetImage(IIconProvider iconProvider)
        {
            return new IconOverlayProvider().GetOverlayNoCache(AutomationIcons.ExecutionSchedule, OverlayKind.Execute);
        }
    }
}
