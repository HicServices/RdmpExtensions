using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.DataRelease;
using DataExportLibrary.Interfaces.Data.DataTables;
using Ionic.Zip;
using WebDAVClient;

namespace LoadModules.Extensions.ReleasePlugins
{
    public class WebdavReleaseEngine : ReleaseEngine
    {
        public WebdavReleaseEngineSettings WebdavSettings { get; set; }

        public WebdavReleaseEngine(Project project, WebdavReleaseEngineSettings releaseSettings) : base(project, new ReleaseEngineSettings())
        {
            base.ReleaseSettings.CreateReleaseDirectoryIfNotFound = true;
            base.ReleaseSettings.UseProjectExtractionFolder = false;
            base.ReleaseSettings.CustomExtractionDirectory = 
                new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            
            WebdavSettings = releaseSettings;
            if (WebdavSettings == null)
                WebdavSettings = new WebdavReleaseEngineSettings();

            base.ReleaseSettings.DeleteFilesOnSuccess = WebdavSettings.DeleteFilesOnSuccess;
        }

        public override void DoRelease(Dictionary<IExtractionConfiguration, List<ReleasePotential>> toRelease, ReleaseEnvironmentPotential environment, bool isPatch)
        {
            base.DoRelease(toRelease, environment, isPatch);

            if (!ReleaseSuccessful)
                throw new Exception("Something horrible happened during Release... cannot progress!");

            ReleaseSuccessful = false;

            using (var zipOutput = new MemoryStream())
            {
                var zipFile = ZipReleaseFolder(ReleaseSettings.CustomExtractionDirectory, WebdavSettings.ZipPassword, zipOutput);

                UploadToServer(zipOutput);
            }

            ReleaseSuccessful = true;
        }

        private void UploadToServer(MemoryStream zipOutput)
        {
            var client = new Client(new NetworkCredential { UserName = WebdavSettings.Username, Password = WebdavSettings.Password });
            client.Server = WebdavSettings.Endpoint;
            client.BasePath = WebdavSettings.BasePath;

            var remoteFolder = client.GetFolder(WebdavSettings.RemoteFolder).Result;

            zipOutput.Seek(0, SeekOrigin.Begin);
            var fileUploaded = client.Upload(remoteFolder.Href, zipOutput, GetArchiveNameForProject() + ".zip").Result;
        }

        private ZipFile ZipReleaseFolder(DirectoryInfo customExtractionDirectory, string zipPassword, MemoryStream zipOutput)
        {
            //var destination = Path.Combine(customExtractionDirectory.FullName, GetArchiveNameForProject() + ".zip");

            var zip = new ZipFile();
            if (!String.IsNullOrWhiteSpace(zipPassword))
                zip.Password = zipPassword;
                
            zip.AddDirectory(customExtractionDirectory.FullName);
            zip.Save(zipOutput);

            return zip;
        }

        private string GetArchiveNameForProject()
        {
            var nameToUse = "";
            if (String.IsNullOrWhiteSpace(Project.MasterTicket))
                nameToUse = Project.ID + "_" + Project.Name;
            else
                nameToUse = Project.MasterTicket;

            return "Release-" + nameToUse;
        }
    }
}