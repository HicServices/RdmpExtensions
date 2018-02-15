using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.DataRelease;
using DataExportLibrary.Interfaces.Data.DataTables;
using Ionic.Zip;
using LoadModules.Extensions.ReleasePlugins.Data;
using Newtonsoft.Json;
using ReusableLibraryCode.Progress;
using Ticketing;

namespace LoadModules.Extensions.ReleasePlugins
{
    public class RemoteRDMPReleaseEngine : ReleaseEngine
    {
        public RemoteRDMPReleaseEngineSettings RemoteRDMPSettings { get; set; }

        public RemoteRDMPReleaseEngine(Project project, RemoteRDMPReleaseEngineSettings releaseSettings, IDataLoadEventListener listener, DirectoryInfo releaseFolder) : base(project, new ReleaseEngineSettings(), listener, releaseFolder)
        {
            RemoteRDMPSettings = releaseSettings;
            if (RemoteRDMPSettings == null)
                RemoteRDMPSettings = new RemoteRDMPReleaseEngineSettings();

            base.ReleaseSettings.DeleteFilesOnSuccess = RemoteRDMPSettings.DeleteFilesOnSuccess;
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
            ZipReleaseFolder(ReleaseFolder, RemoteRDMPSettings.ZipPassword.GetDecryptedValue(), zipOutput);
                
            UploadToRemote(zipOutput, releaseFileName, projectSafeHavenFolder);
            
            ReleaseSuccessful = true;

            if (RemoteRDMPSettings.DeleteFilesOnSuccess)
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
            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            using (var content = new MultipartFormDataContent())
            {
                handler.Credentials = new NetworkCredential
                {
                    UserName = RemoteRDMPSettings.RemoteRDMP.Username,
                    Password = RemoteRDMPSettings.RemoteRDMP.GetDecryptedPassword()
                };

                content.Add(new StreamContent(File.OpenRead(zipOutput)), "file", Path.GetFileName(releaseFileName));
                var settings = new
                {
                    ProjectFolder = projectSafeHavenFolder,
                    ZipPassword = RemoteRDMPSettings.ZipPassword.GetDecryptedValue()
                };
                content.Add(new StringContent(JsonConvert.SerializeObject(settings)), "settings");
                
                try
                {
                    var result = client.PostAsync(RemoteRDMPSettings.RemoteRDMP.GetUrlForRelease(), content).Result;
                    string resultStream;
                    List<NotifyEventArgsProxy> messages;
                    if (!result.IsSuccessStatusCode)
                    {
                        resultStream = result.Content.ReadAsStringAsync().Result;
                        messages = JsonConvert.DeserializeObject<List<NotifyEventArgsProxy>>(resultStream);
                        foreach (var eventArg in messages)
                        {
                            _listener.OnNotify(this, eventArg);
                        }
                        throw new Exception("Upload failed");
                    }
                    else
                    {
                        resultStream = result.Content.ReadAsStringAsync().Result;
                        messages = JsonConvert.DeserializeObject<List<NotifyEventArgsProxy>>(resultStream);
                        foreach (var eventArg in messages)
                        {
                            _listener.OnNotify(this, eventArg);
                        }
                        _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Upload succeeded"));
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