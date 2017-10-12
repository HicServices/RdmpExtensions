using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using CatalogueLibrary.Data.Automation;
using Ionic.Zip;
using RDMPAutomationService;
using RDMPAutomationService.Interfaces;
using WebDAVClient;
using WebDAVClient.Model;

namespace LoadModules.Extensions.ReleasePlugins
{
    public class WebdavAutoDownloader : IAutomateable
    {
        private readonly WebdavAutomationSettings options;
        private readonly Item file;

        public WebdavAutoDownloader(WebdavAutomationSettings options, Item file)
        {
            this.options = options;
            this.file = file;
        }

        public OnGoingAutomationTask GetTask()
        {
            throw new NotImplementedException("Cannot do this...");
        }

        public void RunTask(OnGoingAutomationTask task)
        {
            task.Job.SetLastKnownStatus(AutomationJobStatus.Running);
            task.Job.TickLifeline();

            var zipFilePath = DownloadToDestination(file);
            
            // TODO: Verify I can overwrite existing files (or not?)
            UnzipToReleaseFolder(zipFilePath);
            task.Job.TickLifeline();

            // TODO: Use an alternate method for logging...
            File.AppendAllText(@"C:\temp\processed.txt", file.Href + "\r\n");
            
            task.Job.TickLifeline();
            task.Job.SetLastKnownStatus(AutomationJobStatus.Finished);

            task.Job.DeleteInDatabase();
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
            var linkProj = Regex.Match(filename, "Proj-(\\d+)").Groups[1].Value;

            var destination = Path.Combine(options.LocalDestination, "Project " + linkProj, filename);

            using (var zip = ZipFile.Read(zipFilePath))
            {
                zip.Password = options.ZipPassword.GetDecryptedValue();
                zip.ExtractAll(destination);
            }

            Console.WriteLine("Unzipped all to {0}", destination);
        }
    }
}