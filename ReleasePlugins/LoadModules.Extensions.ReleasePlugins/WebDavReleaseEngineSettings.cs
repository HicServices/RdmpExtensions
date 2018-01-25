using System;
using System.Net;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Remoting;
using DataExportLibrary.DataRelease;
using LoadModules.Generic.DataProvider;
using ReusableLibraryCode.Checks;
using WebDAVClient;

namespace LoadModules.Extensions.ReleasePlugins
{
    public class WebdavReleaseEngineSettings : ICheckable
    {
        [DemandsInitialization("Password for ZIP package")]
        public EncryptedString ZipPassword { get; set; }

        [DemandsInitialization("Delete the released files from the origin location if release is succesful", DefaultValue = true)]
        public bool DeleteFilesOnSuccess { get; set; }

        [DemandsInitialization("Remote RDMP instance")]
        public RemoteRDMP RemoteRDMP { get; set; }

        public WebdavReleaseEngineSettings()
        {
            DeleteFilesOnSuccess = true;
        }

        public void Check(ICheckNotifier notifier)
        {
            var client = new WebClient();// (new NetworkCredential { UserName = this.RemoteRDMP.Username, Password = this.RemoteRDMP.GetDecryptedPassword() });
            client.Credentials = new NetworkCredential
            {
                UserName = this.RemoteRDMP.Username,
                Password = this.RemoteRDMP.GetDecryptedPassword()
            };
            try
            {
                var check = client.DownloadString(this.RemoteRDMP.URL);
                notifier.OnCheckPerformed(new CheckEventArgs("Checks passed " + check, CheckResult.Success));
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Checks failed", CheckResult.Fail, e));
            }
        }
    }
}