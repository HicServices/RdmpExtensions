using System;
using System.IO;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataFlowPipeline.Requirements;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.DataRelease.Audit;
using DataExportLibrary.DataRelease.ReleasePipeline;
using DataExportLibrary.Interfaces.Data.DataTables;
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

        public ReleaseAudit ProcessPipelineData(ReleaseAudit releaseAudit, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            if (releaseAudit == null)
                return null;

            if (releaseAudit.ReleaseFolder == null)
            {
                releaseAudit.ReleaseFolder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
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

            _remoteRDMPReleaseEngineengine.DoRelease(_releaseData.ConfigurationsForRelease, _releaseData.EnvironmentPotential, isPatch: _releaseData.ReleaseState == ReleaseState.DoingPatch);

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
                        foreach (ReleaseLogEntry remnant in configuration.ReleaseLogEntries)
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
            }

            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Pipeline completed..."));
        }

        public void Abort(IDataLoadEventListener listener)
        {
            listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "This component cannot Abort!"));
        }

        public void Check(ICheckNotifier notifier)
        {
            ((ICheckable)RDMPReleaseSettings).Check(notifier);
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
