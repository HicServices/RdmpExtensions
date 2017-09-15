using CatalogueLibrary.Data;
using DataExportLibrary.DataRelease;
using LoadModules.Generic.DataProvider;
using ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.ReleasePlugins
{
    public class WebdavReleaseEngineSettings : ICheckable
    {
        [DemandsInitialization("Password for ZIP package")]
        public string ZipPassword { get; set; }

        [DemandsInitialization("Delete the released files from the origin location if release is succesful", DefaultValue = true)]
        public bool DeleteFilesOnSuccess { get; set; }

        [DemandsInitialization("Webdav endpoint")]
        public string Endpoint { get; set; }

        [DemandsInitialization("Webdav remote folder")]
        public string RemoteFolder { get; set; }

        [DemandsInitialization("Webdav username")]
        public string Username { get; set; }

        [DemandsInitialization("Webdav password")]
        public string Password { get; set; }

        public WebdavReleaseEngineSettings()
        {
            DeleteFilesOnSuccess = true;
        }

        public void Check(ICheckNotifier notifier)
        {
            // test if release is a valid folder;
            //                                  ^- IMPORTANT semicolon or test will fail!  
        }
    }
}