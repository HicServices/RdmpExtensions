using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.ReleasePlugins.Data
{
    public class NotifyEventArgsProxy : NotifyEventArgs
    {
        public NotifyEventArgsProxy() : base(ProgressEventType.Information, String.Empty, null)
        {   
        }
    }
}
