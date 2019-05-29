using System;
using System.ComponentModel.Composition;
using Rdmp.Core.Ticketing;
using ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.AutomationPlugins.Tests
{
    [Export(typeof(ITicketingSystem))]
    public class AllowAnythingTicketing:ITicketingSystem
    {
        public void Check(ICheckNotifier notifier)
        {
            
        }

        public bool IsValidTicketName(string ticketName)
        {
            return true;
        }

        public void NavigateToTicket(string ticketName)
        {
            
        }

        public TicketingReleaseabilityEvaluation GetDataReleaseabilityOfTicket(string masterTicket, string requestTicket,
            string releaseTicket, out string reason, out Exception exception)
        {
            reason = null;
            exception = null;
            return TicketingReleaseabilityEvaluation.Releaseable;
        }

        public string GetProjectFolderName(string masterTicket)
        {
            return "Project " + masterTicket;
        }
    }
}