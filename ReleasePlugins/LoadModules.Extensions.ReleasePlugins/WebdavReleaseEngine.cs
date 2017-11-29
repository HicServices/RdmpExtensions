using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.DataRelease;
using DataExportLibrary.Interfaces.Data.DataTables;
using Ionic.Zip;
using Ticketing;
using WebDAVClient;

namespace LoadModules.Extensions.ReleasePlugins
{
    public class WebdavReleaseEngine : ReleaseEngine
    {
        public WebdavReleaseEngineSettings WebdavSettings { get; set; }

        public WebdavReleaseEngine(Project project, WebdavReleaseEngineSettings releaseSettings) : base(project, new ReleaseEngineSettings())
        {
            base.ReleaseSettings.CreateReleaseDirectoryIfNotFound = true;
            
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

            var releaseFileName = GetArchiveNameForProject() + ".zip";
            var zipOutput = Path.Combine(ReleaseFolder.FullName, releaseFileName);
            ZipReleaseFolder(ReleaseFolder, WebdavSettings.ZipPassword.GetDecryptedValue(), zipOutput);
                
            UploadToServer(zipOutput, releaseFileName);
            
            ReleaseSuccessful = true;
        }

        private void UploadToServer(string zipOutput, string releaseFileName)
        {
            var client = new Client(new NetworkCredential { UserName = WebdavSettings.Username, Password = WebdavSettings.Password.GetDecryptedValue() });
            client.Server = WebdavSettings.Endpoint;
            client.BasePath = WebdavSettings.BasePath;

            var remoteFolder = client.GetFolder(WebdavSettings.RemoteFolder).Result;

            using (var file = File.Open(zipOutput, FileMode.Open))
            {
                var fileUploaded = client.Upload(remoteFolder.Href, file, releaseFileName).Result;

                if (!fileUploaded)
                {
                    throw new Exception("Failed to upload file to remote location");
                }
            }
        }

        private void ZipReleaseFolder(DirectoryInfo customExtractionDirectory, string zipPassword, string zipOutput)
        {
            var zip = new ZipFile();
            if (!String.IsNullOrWhiteSpace(zipPassword))
                zip.Password = zipPassword;
                
            zip.AddDirectory(customExtractionDirectory.FullName);
            zip.Save(zipOutput);
        }

        private string GetArchiveNameForProject()
        {
            var prefix = DateTime.UtcNow.ToString("yyyy-MM-dd_");
            var nameToUse = "";
            if (String.IsNullOrWhiteSpace(Project.MasterTicket))
                nameToUse = Project.ID + "_" + Project.Name + "_Proj-" + Project.ProjectNumber;
            else
                nameToUse = Project.MasterTicket + "_Proj-" + Project.ProjectNumber + " (" + GetSafeHavenFolder(Project.MasterTicket) + ")";

            return prefix + "Release-" + nameToUse;
        }

        private string GetSafeHavenFolder(string masterTicket)
        {
            var catalogueRepository = Project.DataExportRepository.CatalogueRepository;
            var factory = new TicketingSystemFactory(catalogueRepository);
            var system = factory.CreateIfExists(catalogueRepository.GetTicketingSystem());

            if (system == null)
                return String.Empty;

            return system.GetProjectFolderName(masterTicket).Replace("/", "");
        }
    }
}