using System;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.ReleasePlugins.Data;

public class NotifyEventArgsProxy : NotifyEventArgs
{
    public NotifyEventArgsProxy() : base(ProgressEventType.Information, String.Empty, null)
    {   
    }
}