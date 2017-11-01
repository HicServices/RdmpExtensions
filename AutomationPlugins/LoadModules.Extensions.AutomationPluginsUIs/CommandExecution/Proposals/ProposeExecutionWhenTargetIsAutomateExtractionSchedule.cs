using CatalogueManager.CommandExecution.Proposals;
using CatalogueManager.ItemActivation;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPluginsUIs.Tabs;
using ReusableLibraryCode.CommandExecution;
using ReusableUIComponents.CommandExecution;

namespace LoadModules.Extensions.AutomationPluginsUIs.CommandExecution.Proposals
{
    public class ProposeExecutionWhenTargetIsAutomateExtractionSchedule : RDMPCommandExecutionProposal<AutomateExtractionSchedule>
    {
        public ProposeExecutionWhenTargetIsAutomateExtractionSchedule(IActivateItems itemActivator) : base(itemActivator)
        {
        }

        public override bool CanActivate(AutomateExtractionSchedule target)
        {
            return true;
        }

        public override void Activate(AutomateExtractionSchedule target)
        {
            ItemActivator.Activate<AutomateExtractionScheduleTab, AutomateExtractionSchedule>(target);
        }

        public override ICommandExecution ProposeExecution(ICommand cmd, AutomateExtractionSchedule target, InsertOption insertOption = InsertOption.Default)
        {
            return null;
        }
    }
}