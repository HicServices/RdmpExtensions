using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.Repositories;
using HIC.Logging;
using HIC.Logging.Listeners;
using Ionic.Zip;
using LoadModules.Extensions.ReleasePlugins.Data;
using MapsDirectlyToDatabaseTable;
using RDMPAutomationService;
using RDMPAutomationService.EventHandlers;
using RDMPAutomationService.Interfaces;
using ReusableLibraryCode;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DataAccess;
using ReusableLibraryCode.Progress;
using roundhouse.infrastructure.commandline.options;
using WebDAVClient;
using WebDAVClient.Model;

namespace LoadModules.Extensions.ReleasePlugins.Automation
{
    public class WebdavAutoDownloader : IAutomateable
    {
        private readonly WebdavAutomationSettings options;
        private readonly Item file;
        private readonly WebdavAutomationAudit audit;

        private IDataLoadEventListener listener;
        private const string TASK_NAME = "Webdav Auto Release";

        public WebdavAutoDownloader(WebdavAutomationSettings options, Item file, WebdavAutomationAudit audit)
        {
            this.options = options;
            this.file = file;
            this.audit = audit;
        }

        public OnGoingAutomationTask GetTask()
        {
            throw new NotImplementedException("Cannot do this...");
        }

        public void RunTask(OnGoingAutomationTask task)
        {
            task.Job.SetLastKnownStatus(AutomationJobStatus.Running);
            task.Job.TickLifeline();
            
            var sd = new ServerDefaults((CatalogueRepository) task.Repository);
            var loggingServer = sd.GetDefaultFor(ServerDefaults.PermissableDefaults.LiveLoggingServer_ID);
            if (loggingServer != null)
            {
                var lm = new LogManager(loggingServer);
                lm.CreateNewLoggingTaskIfNotExists(TASK_NAME);
                var dli = lm.CreateDataLoadInfo(TASK_NAME, GetType().Name, task.Job.Description, String.Empty, false);

                listener = new ToLoggingDatabaseDataLoadEventListener(lm, dli);

                task.Job.SetLoggingInfo(loggingServer, dli.ID);
            }
            else
            {
                // TODO: See if we can log anyway somewhere... or bomb out?
                listener = new FromCheckNotifierToDataLoadEventListener(new IgnoreAllErrorsCheckNotifier());
            }

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Ready to download and unzip: " + file.DisplayName));

            WebDavDataRepository tableRepo = GetAuditRepo(task);
            if (tableRepo == null)
            {
                task.Job.SetLastKnownStatus(AutomationJobStatus.Cancelled);
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Unable to access the Audit Repository"));
                return;
            }

            try
            {
                var zipFilePath = DownloadToDestination(file);

                // Will bomb if it tries to overwrite existing files!
                UnzipToReleaseFolder(zipFilePath);
                task.Job.TickLifeline();

                ArchiveFile(file, "Done");

                audit.FileResult = FileResult.Done;
                audit.Updated = DateTime.UtcNow;
                audit.Message = "RELEASED!";
                audit.SaveToDatabase();

                task.Job.TickLifeline();

                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Job done: " + file.DisplayName + " RELEASED!"));

                task.Job.SetLastKnownStatus(AutomationJobStatus.Finished);
                task.Job.DeleteInDatabase();
            }
            catch (Exception e)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Fatal crash", e));

                ArchiveFile(file, "Errored");

                audit.FileResult = FileResult.Errored;
                audit.Message = ExceptionHelper.ExceptionToListOfInnerMessages(e);
                audit.Updated = DateTime.UtcNow;
                audit.SaveToDatabase();

                task.Job.SetLastKnownStatus(AutomationJobStatus.Crashed);
            }
            finally
            {
                var dbLog = (listener as ToLoggingDatabaseDataLoadEventListener);
                if (dbLog != null)
                    dbLog.FinalizeTableLoadInfos();
            }
        }

        private WebDavDataRepository GetAuditRepo(OnGoingAutomationTask task)
        {
            WebDavDataRepository tableRepo;
            var repoServer = task.Repository.GetAllObjects<ExternalDatabaseServer>()
                    .SingleOrDefault(s => s.CreatedByAssembly == typeof (Database.Class1).Assembly.GetName().Name);

            if (repoServer == null)
                return null;

            var discoveredServer = DataAccessPortal.GetInstance().ExpectServer(repoServer, DataAccessContext.DataExport);

            tableRepo = new WebDavDataRepository(discoveredServer.Builder);
            return tableRepo;
        }

        private string DownloadToDestination(Item file)
        {
            var client = new Client(new NetworkCredential { UserName = options.Username, Password = options.Password.GetDecryptedValue() });
            client.Server = options.Endpoint;
            client.BasePath = options.BasePath;

            using (var fileStream = File.Create(Path.Combine(options.LocalDestination, file.DisplayName)))
            {
                var content = client.Download(file.Href).Result;
                content.CopyTo(fileStream);
            }

            Console.WriteLine("Downloaded to {0}", Path.Combine(options.LocalDestination, file.DisplayName));

            return Path.Combine(options.LocalDestination, file.DisplayName);
        }

        private void UnzipToReleaseFolder(string zipFilePath)
        {
            var filename = Path.GetFileNameWithoutExtension(zipFilePath);
            Debug.Assert(filename != null, "filename != null");
            var projFolder = Regex.Match(filename, @"\((.*)\)").Groups[1].Value;

            var outputFolder = projFolder;
            if (String.IsNullOrWhiteSpace(projFolder))
            {
                var linkProj = Regex.Match(filename, "Proj-(\\d+)").Groups[1].Value;
                if (String.IsNullOrWhiteSpace(linkProj))
                    outputFolder = "Project " + Guid.NewGuid().ToString("N");
                else
                    outputFolder = "Project " + linkProj;
            }

            var destination = Path.Combine(options.LocalDestination, outputFolder, filename);

            using (var zip = ZipFile.Read(zipFilePath))
            {
                zip.Password = options.ZipPassword.GetDecryptedValue();
                zip.ExtractAll(destination);
            }

            Console.WriteLine("Unzipped all to {0}", destination);
        }

        private void ArchiveFile(Item file, string archiveLocation)
        {
            var client = new Client(new NetworkCredential { UserName = options.Username, Password = options.Password.GetDecryptedValue() });
            client.Server = options.Endpoint;
            client.BasePath = options.BasePath;

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Archived: " + file.DisplayName + " to " + Path.Combine(options.RemoteFolder, archiveLocation, file.DisplayName).Replace("\\", "/")));

            if(!client.MoveFile(file.Href, Path.Combine(options.RemoteFolder, archiveLocation, file.DisplayName).Replace("\\","/")).Result)
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Error archiving file!"));
        }
    }
}