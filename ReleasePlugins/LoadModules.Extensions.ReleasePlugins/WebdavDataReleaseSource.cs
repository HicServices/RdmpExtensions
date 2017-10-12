using System;
using System.IO;
using System.Linq;
using System.Net;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataFlowPipeline.Requirements;
using CatalogueLibrary.Repositories;
using RDMPAutomationService;
using RDMPAutomationService.Interfaces;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using WebDAVClient;
using WebDAVClient.Model;

namespace LoadModules.Extensions.ReleasePlugins
{
    public class WebdavDataReleaseAutomationSource : IPluginAutomationSource, IPipelineRequirement<IRDMPPlatformRepositoryServiceLocator>, ICheckable
    {
        private AutomationServiceSlot _serviceSlot;
        private IRDMPPlatformRepositoryServiceLocator _repositoryLocator;

        [DemandsNestedInitialization()]
        public WebdavAutomationSettings ReleaseSettings { get; set; }

        public OnGoingAutomationTask GetChunk(IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            // we don't have our slot yet
            if(_serviceSlot == null)
                return null;

            var allJobs = _serviceSlot.AutomationJobs;

            // we want to run one at a time
            if (allJobs.Any(aj => (aj.LastKnownStatus == AutomationJobStatus.NotYetStarted || aj.LastKnownStatus == AutomationJobStatus.Running) && aj.Description == this.GetType().Name))
                return null;

            // throttle failures (do not start if 5 or more crashes in the last 24 hours)
            if (allJobs.Where(aj => aj.Lifeline.HasValue && aj.Lifeline > DateTime.UtcNow.AddDays(-1))
                       .Count(aj => (aj.LastKnownStatus == AutomationJobStatus.Crashed)) >= 5)
                return null;

            var file = GetFirstUnprocessed();
            if (file == null)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "No new files to process..."));
                return null;
            }

            var job = new AutomationJob(_repositoryLocator.CatalogueRepository, _serviceSlot, AutomationJobType.UserCustomPipeline, this.GetType().Name);

            var task = new WebdavAutoDownloader(ReleaseSettings, file);

            return new OnGoingAutomationTask(job, task);
        }

        private Item GetFirstUnprocessed()
        {
            var client = new Client(new NetworkCredential { UserName = ReleaseSettings.Username, Password = ReleaseSettings.Password.GetDecryptedValue() });
            client.Server = ReleaseSettings.Endpoint;
            client.BasePath = ReleaseSettings.BasePath;

            var remoteFolder = client.GetFolder(ReleaseSettings.RemoteFolder).Result;

            if (remoteFolder == null)
                return null;

            var files = client.List(remoteFolder.Href).Result;
            var enumerable = files as Item[] ?? files.ToArray();

            // TODO: Get from Logged Jobs!
            if (!File.Exists(@"C:\temp\processed.txt"))
                File.Create(@"C:\temp\processed.txt").Dispose();
            var alreadyProcessed = File.ReadAllLines(@"C:\temp\processed.txt");

            var latest = enumerable.Where(f => f.DisplayName.Contains("Release") && !alreadyProcessed.Contains(f.Href)).OrderBy(f => f.LastModified).FirstOrDefault();

            return latest;
        }
        
        #region IPluginAutomationSource implementation useless methods
        public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {
        }

        public void Abort(IDataLoadEventListener listener)
        {
        }

        public OnGoingAutomationTask TryGetPreview()
        {
            return null;
        }

        public void PreInitialize(AutomationServiceSlot value, IDataLoadEventListener listener)
        {
            _serviceSlot = value;
        }

        public void PreInitialize(IRDMPPlatformRepositoryServiceLocator value, IDataLoadEventListener listener)
        {
            _repositoryLocator = value;
        }

        public void Check(ICheckNotifier notifier)
        {
            ((ICheckable)ReleaseSettings).Check(notifier);
        }
        #endregion

    }
}