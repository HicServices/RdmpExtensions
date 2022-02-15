using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using Rdmp.Core;
using Rdmp.Core.CommandExecution;
using Rdmp.Core.CommandExecution.AtomicCommands;
using Rdmp.Core.Curation.Data.Defaults;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.Providers.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LoadModules.Extensions.AutomationPlugins
{
    public class AutomationUserInterface : PluginUserInterface
    {
        public AutomateExtractionRepository AutomationRepository { get; private set; }

        public AutomationUserInterface(IBasicActivateItems itemActivator) : base(itemActivator)
        {
        }

        public override object[] GetChildren(object model)
        {
            if (model is IProject p)
            {
                var schedule = GetScheduleIfAny(p);
                
                if(schedule != null)
                    return new[] { schedule };
            }

            if(model is IExtractionConfiguration ec)
            {
                var automate = GetAutomateExtractionIfAny(ec);
                if (automate != null)
                    return new[] { automate };
            }

            return base.GetChildren(model);
        }

        private AutomateExtractionSchedule GetScheduleIfAny(IProject p)
        {
            TryGettingAutomationRepository();

            if (AutomationRepository == null)
            {
                return null;    
            }

            return AutomationRepository.GetAllObjects<AutomateExtractionSchedule>().FirstOrDefault(aes => aes.Project_ID == p.ID);
        }

        private AutomateExtraction GetAutomateExtractionIfAny(IExtractionConfiguration ec)
        {
            TryGettingAutomationRepository();

            if (AutomationRepository == null)
            {
                return null;
            }

            return AutomationRepository.GetAllObjects<AutomateExtraction>().FirstOrDefault(ae => ae.ExtractionConfiguration_ID == ec.ID);
        }

        DateTime lastLook = DateTime.MinValue;

        private void TryGettingAutomationRepository()
        {
            // we looked recently already dont spam that thing
            if (DateTime.Now - lastLook < TimeSpan.FromSeconds(5))
                return;

            if (AutomationRepository != null)
                return;

            try
            {
                var repo = new AutomateExtractionRepositoryFinder(BasicActivator.RepositoryLocator);
                AutomationRepository = (AutomateExtractionRepository)repo.GetRepositoryIfAny();
                lastLook = DateTime.Now;
            }
            catch (Exception)
            {
                AutomationRepository = null;
                lastLook = DateTime.Now;
            }
        }

        public override IEnumerable<IAtomicCommand> GetAdditionalRightClickMenuItems(object o)
        {
            if (o is AllExternalServersNode)
            {
                yield return new ExecuteCommandCreateNewExternalDatabaseServer(BasicActivator, new AutomateExtractionPluginPatcher(), PermissableDefaults.None);
            }


            if(o is IProject p)
            {
                yield return new ExecuteCommandCreateNewAutomateExtractionSchedule(BasicActivator, p);
            }
            if(o is IExtractionConfiguration ec)
            {
                yield return new ExecuteCommandCreateNewAutomateExtraction(BasicActivator, ec);
            }    
            
        }
    }
}
