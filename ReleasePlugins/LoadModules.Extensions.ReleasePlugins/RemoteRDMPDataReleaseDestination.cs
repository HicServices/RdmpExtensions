using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataFlowPipeline.Requirements;
using CatalogueLibrary.Ticketing;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.DataRelease.Audit;
using DataExportLibrary.DataRelease.ReleasePipeline;
using DataExportLibrary.ExtractionTime;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.ReleasePlugins
{
    public class RemoteRDMPDataReleaseDestination : IPluginDataFlowComponent<ReleaseAudit>, IDataFlowDestination<ReleaseAudit>, IPipelineRequirement<Project>, IPipelineRequirement<ReleaseData>
    {
        [DemandsNestedInitialization()]
        public RemoteRDMPReleaseEngineSettings RDMPReleaseSettings { get; set; }

        private RemoteRDMPReleaseEngine _remoteRDMPReleaseEngineengine;
        private Project _project;
        private ReleaseData _releaseData;
        private List<IExtractionConfiguration> _configurationReleased;

        public ReleaseAudit ProcessPipelineData(ReleaseAudit releaseAudit, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            if (releaseAudit == null)
                return null;

            if (releaseAudit.ReleaseFolder == null)
            {
                releaseAudit.ReleaseFolder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
                if (!releaseAudit.ReleaseFolder.Exists)
                    releaseAudit.ReleaseFolder.Create();
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "No destination folder specified! Did you forget to introduce and initialize the ReleaseFolderProvider in the pipeline? " +
                                                                                       "The release output will be located in " + releaseAudit.ReleaseFolder.FullName));
            }

            if (_releaseData.ReleaseState == ReleaseState.DoingPatch)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "CumulativeExtractionResults for datasets not included in the Patch will now be erased."));

                int recordsDeleted = 0;

                foreach (var configuration in _releaseData.ConfigurationsForRelease.Keys)
                {
                    IExtractionConfiguration current = configuration;
                    var currentResults = configuration.CumulativeExtractionResults;

                    //foreach existing CumulativeExtractionResults if it is not included in the patch then it should be deleted
                    foreach (var redundantResult in currentResults.Where(r => _releaseData.ConfigurationsForRelease[current].All(rp => rp.DataSet.ID != r.ExtractableDataSet_ID)))
                    {
                        redundantResult.DeleteInDatabase();
                        recordsDeleted++;
                    }
                }

                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Deleted " + recordsDeleted + " old CumulativeExtractionResults (That were not included in the final Patch you are preparing)"));
            }

            _remoteRDMPReleaseEngineengine = new RemoteRDMPReleaseEngine(_project, RDMPReleaseSettings, listener, releaseAudit.ReleaseFolder);

            _remoteRDMPReleaseEngineengine.DoRelease(_releaseData.ConfigurationsForRelease, _releaseData.EnvironmentPotentials, isPatch: _releaseData.ReleaseState == ReleaseState.DoingPatch);

            _configurationReleased = _remoteRDMPReleaseEngineengine.ConfigurationsReleased;
            return null;
        }

        public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {
            if (pipelineFailureExceptionIfAny != null && _releaseData != null)
            {
                try
                {
                    int remnantsDeleted = 0;

                    foreach (ExtractionConfiguration configuration in _releaseData.ConfigurationsForRelease.Keys)
                        foreach (IReleaseLog remnant in configuration.ReleaseLog)
                        {
                            remnant.DeleteInDatabase();
                            remnantsDeleted++;
                        }

                    if (remnantsDeleted > 0)
                        listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Because release failed we are deleting ReleaseLogEntries, this resulted in " + remnantsDeleted + " deleted records, you will likely need to re-extract these datasets"));
                }
                catch (Exception e1)
                {
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Error, "Error occurred when trying to clean up remnant ReleaseLogEntries", e1));
                }
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Pipeline completed..."));
            }

            if (pipelineFailureExceptionIfAny == null)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Data release succeded into: " + RDMPReleaseSettings.RemoteRDMP.Name));
                
                // we can freeze the configuration now:
                foreach (var config in _configurationReleased)
                {
                    config.IsReleased = true;
                    config.SaveToDatabase();
                }
                if (RDMPReleaseSettings.DeleteFilesOnSuccess)
                {
                    listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Cleaning up..."));
                    ExtractionDirectory.CleanupExtractionDirectory(this, _project.ExtractionDirectory, _configurationReleased, listener);
                }

                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "All done!"));
            }
        }

        public void Abort(IDataLoadEventListener listener)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "This component cannot Abort!"));
        }

        public void Check(ICheckNotifier notifier)
        {
            var projectSafeHavenFolder = GetSafeHavenFolder(_project.MasterTicket);
            if (string.IsNullOrWhiteSpace(projectSafeHavenFolder))
                notifier.OnCheckPerformed(new CheckEventArgs("No Safe Haven folder specified in the Project Master Ticket", CheckResult.Fail));
            else
                notifier.OnCheckPerformed(new CheckEventArgs("Project Master Ticket contains Safe Haven folder", CheckResult.Success));
            
            ((ICheckable)RDMPReleaseSettings).Check(notifier);
        }

        private string GetSafeHavenFolder(string masterTicket)
        {
            if (String.IsNullOrWhiteSpace(masterTicket))
                return "Proj-" + _project.ProjectNumber;

            var catalogueRepository = _project.DataExportRepository.CatalogueRepository;
            var factory = new TicketingSystemFactory(catalogueRepository);
            var system = factory.CreateIfExists(catalogueRepository.GetTicketingSystem());

            if (system == null)
                return String.Empty;

            return system.GetProjectFolderName(masterTicket).Replace("/", "");
        }

        public void PreInitialize(Project value, IDataLoadEventListener listener)
        {
            _project = value;
        }

        public void PreInitialize(ReleaseData value, IDataLoadEventListener listener)
        {
            _releaseData = value;
        }
    }
}
