using System;
using System.Net;
using System.Net.Http;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Curation.Data.Remoting;
using ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.ReleasePlugins
{
    public class RemoteRDMPReleaseEngineSettings : ICheckable
    {
        [DemandsInitialization("Password for ZIP package")]
        public EncryptedString ZipPassword { get; set; }

        [DemandsInitialization("Delete the released files from the origin location if release is succesful", DefaultValue = true)]
        public bool DeleteFilesOnSuccess { get; set; }

        [DemandsInitialization("Remote RDMP instance")]
        public RemoteRDMP RemoteRDMP { get; set; }

        public RemoteRDMPReleaseEngineSettings()
        {
            DeleteFilesOnSuccess = true;
        }

        public void Check(ICheckNotifier notifier)
        {
            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential
                {
                    UserName = this.RemoteRDMP.Username,
                    Password = this.RemoteRDMP.GetDecryptedPassword()
                }
            };
            var client = new HttpClient(handler);
            try
            {
                var baseUri = new UriBuilder(new Uri(this.RemoteRDMP.URL));
                baseUri.Path += "/api/plugin/";
                var message = new HttpRequestMessage(HttpMethod.Head, baseUri.ToString());
                var check = client.SendAsync(message).Result;
                check.EnsureSuccessStatusCode();
                notifier.OnCheckPerformed(new CheckEventArgs("Checks passed " + check.Content.ReadAsStringAsync().Result, CheckResult.Success));
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Checks failed", CheckResult.Fail, e));
            }
        }
    }
}