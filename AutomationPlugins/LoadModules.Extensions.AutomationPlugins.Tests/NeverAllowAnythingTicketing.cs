using System;
using System.ComponentModel.Composition;
using ReusableLibraryCode.Checks;
using Ticketing;

namespace LoadModules.Extensions.AutomationPlugins.Tests
{
    [Export(typeof(ITicketingSystem))]
    public class NeverAllowAnythingTicketing:ITicketingSystem
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
            reason = "Nothing is ever releasable through us sorry!";
            exception = null;
            return TicketingReleaseabilityEvaluation.NotReleaseable;
        }
    }
}