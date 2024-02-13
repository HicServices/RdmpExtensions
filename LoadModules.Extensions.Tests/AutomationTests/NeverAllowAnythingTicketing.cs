using System;
using Rdmp.Core.Ticketing;
using Rdmp.Core.ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.AutomationPlugins.Tests;

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

    public string GetProjectFolderName(string masterTicket) => $"Project {masterTicket}";
}