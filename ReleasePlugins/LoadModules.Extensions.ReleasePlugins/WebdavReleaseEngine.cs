using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
                var zipFile = ZipReleaseFolder(ReleaseSettings.CustomExtractionDirectory, WebdavSettings.ZipPassword.GetDecryptedValue(), zipOutput);

                UploadToServer(zipOutput);
            }

            ReleaseSuccessful = true;
        }

        private void UploadToServer(MemoryStream zipOutput)
        {
            var client = new Client(new NetworkCredential { UserName = WebdavSettings.Username, Password = WebdavSettings.Password.GetDecryptedValue() });
            client.Server = WebdavSettings.Endpoint;
            client.BasePath = WebdavSettings.BasePath;

            var remoteFolder = client.GetFolder(WebdavSettings.RemoteFolder).Result;

            zipOutput.Seek(0, SeekOrigin.Begin);
            var fileUploaded = client.Upload(remoteFolder.Href, zipOutput, GetArchiveNameForProject() + ".zip").Result;

            if (!fileUploaded)
            {
                throw new Exception("Failed to upload file to remote location");
            }
        }

        private ZipFile ZipReleaseFolder(DirectoryInfo customExtractionDirectory, string zipPassword, MemoryStream zipOutput)
        {
            var zip = new ZipFile();
            if (!String.IsNullOrWhiteSpace(zipPassword))
                zip.Password = zipPassword;
                
            zip.AddDirectory(customExtractionDirectory.FullName);
            zip.Save(zipOutput);

            return zip;
        }

        private string GetArchiveNameForProject()
        {
            var prefix = DateTime.UtcNow.ToString("yyyy-MM-dd_");
            var nameToUse = "";
            if (String.IsNullOrWhiteSpace(Project.MasterTicket))
                nameToUse = Project.ID + "_" + Project.Name + "_Proj-" + Project.ProjectNumber;
            else
                nameToUse = Project.MasterTicket + "_Proj-" + Project.ProjectNumber;

            return prefix + "Release-" + nameToUse;
        }
    }
}