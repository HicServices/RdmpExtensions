using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.DataRelease;
using DataExportLibrary.Interfaces.Data.DataTables;
using Ionic.Zip;
using Newtonsoft.Json;
using ReusableLibraryCode.Progress;
using Ticketing;
using WebDAVClient;

namespace LoadModules.Extensions.ReleasePlugins
{
    public class WebdavReleaseEngine : ReleaseEngine
    {
        public WebdavReleaseEngineSettings WebdavSettings { get; set; }

        public WebdavReleaseEngine(Project project, WebdavReleaseEngineSettings releaseSettings, IDataLoadEventListener listener) : base(project, new ReleaseEngineSettings(), listener)
        {
            base.ReleaseSettings.CreateReleaseDirectoryIfNotFound = true;
            
            WebdavSettings = releaseSettings;
            if (WebdavSettings == null)
                WebdavSettings = new WebdavReleaseEngineSettings();

            base.ReleaseSettings.DeleteFilesOnSuccess = WebdavSettings.DeleteFilesOnSuccess;
        }

        public override void DoRelease(Dictionary<IExtractionConfiguration, List<ReleasePotential>> toRelease, ReleaseEnvironmentPotential environment, bool isPatch)
        {
            base.ReleaseSettings.DeleteFilesOnSuccess = false;
            base.ReleaseSettings.FreezeReleasedConfigurations = false;

            base.DoRelease(toRelease, environment, isPatch);

            if (!ReleaseSuccessful)
                throw new Exception("Something horrible happened during Release... cannot progress!");

            ReleaseSuccessful = false;

            var releaseFileName = GetArchiveNameForProject() + ".zip";
            var projectSafeHavenFolder = GetSafeHavenFolder(Project.MasterTicket);
            var zipOutput = Path.Combine(ReleaseFolder.FullName, releaseFileName);
            ZipReleaseFolder(ReleaseFolder, WebdavSettings.ZipPassword.GetDecryptedValue(), zipOutput);
                
            UploadToRemote(zipOutput, releaseFileName, projectSafeHavenFolder);
            
            ReleaseSuccessful = true;

            if (WebdavSettings.DeleteFilesOnSuccess)
            {
                _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Cleaning up..."));
                CleanupExtractionFolders(this.Project.ExtractionDirectory);
            }
            
            // we can freeze the configuration now:
            foreach (KeyValuePair<IExtractionConfiguration, List<ReleasePotential>> kvp in toRelease)
            {
                kvp.Key.IsReleased = true;
                kvp.Key.SaveToDatabase();
            }
        }

        private void UploadToRemote(string zipOutput, string releaseFileName, string projectSafeHavenFolder)
        {
            //var client = new WebClient();// (new NetworkCredential { UserName = WebdavSettings.Username, Password = WebdavSettings.Password.GetDecryptedValue() });
            //client.Credentials = new NetworkCredential(WebdavSettings.RemoteRDMP.Username, WebdavSettings.RemoteRDMP.GetDecryptedPassword());
            
            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                content.Add(new StreamContent(File.OpenRead(zipOutput)), "file", Path.GetFileName(releaseFileName));
                var settings = new
                {
                    Destination = projectSafeHavenFolder,
                    Password = WebdavSettings.ZipPassword
                };
                content.Add(new StringContent(JsonConvert.SerializeObject(settings)), "settings");

                try
                {
                    var result = client.PostAsync(WebdavSettings.RemoteRDMP.GetUrlFor<ReleaseEngine>(), content).Result;
                    var resultStream = result.Content.ReadAsStringAsync().Result;
                    var messages = JsonConvert.DeserializeObject<List<NotifyEventArgs>>(resultStream);
                    foreach (var eventArg in messages)
                    {
                        _listener.OnNotify(this, eventArg);
                    }
                    if (result.IsSuccessStatusCode)
                    {
                        _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Upload succeeded"));
                    }
                    else
                    {
                        _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Upload failed: " + result.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
                    _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Failed to upload data", ex));
                    throw;
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
            var nameToUse = "Proj-" + Project.ProjectNumber;
            return prefix + "Release-" + nameToUse;
        }

        private string GetSafeHavenFolder(string masterTicket)
        {
            if (String.IsNullOrWhiteSpace(masterTicket))
                return "Proj-" + Project.ProjectNumber;
            
            var catalogueRepository = Project.DataExportRepository.CatalogueRepository;
            var factory = new TicketingSystemFactory(catalogueRepository);
            var system = factory.CreateIfExists(catalogueRepository.GetTicketingSystem());

            if (system == null)
                return String.Empty;

            return system.GetProjectFolderName(masterTicket).Replace("/", "");
        }
    }
}