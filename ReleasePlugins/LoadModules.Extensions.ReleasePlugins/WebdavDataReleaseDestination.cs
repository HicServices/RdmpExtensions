using System;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataFlowPipeline.Requirements;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.DataRelease.ReleasePipeline;
using DataExportLibrary.Interfaces.Data.DataTables;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.ReleasePlugins
{
    public class WebdavDataReleaseDestination : IPluginDataFlowComponent<ReleaseData>, IDataFlowDestination<ReleaseData>, IPipelineRequirement<Project>
    {
        [DemandsNestedInitialization()]
        public WebdavReleaseEngineSettings ReleaseSettings { get; set; }

        public ReleaseData CurrentRelease { get; set; }
        private Project _project;

        public ReleaseData ProcessPipelineData(ReleaseData currentRelease, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            this.CurrentRelease = currentRelease;

            WebdavReleaseEngine engine = new WebdavReleaseEngine(_project, ReleaseSettings);

            if (CurrentRelease.ReleaseState == ReleaseState.DoingPatch)
            {
                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "CumulativeExtractionResults for datasets not included in the Patch will now be erased."));

                int recordsDeleted = 0;

                foreach (var configuration in this.CurrentRelease.ConfigurationsForRelease.Keys)
                {
                    IExtractionConfiguration current = configuration;
                    var currentResults = configuration.CumulativeExtractionResults;

                    //foreach existing CumulativeExtractionResults if it is not included in the patch then it should be deleted
                    foreach (var redundantResult in currentResults.Where(r => CurrentRelease.ConfigurationsForRelease[current].All(rp => rp.DataSet.ID != r.ExtractableDataSet_ID)))
                    {
                        redundantResult.DeleteInDatabase();
                        recordsDeleted++;
                    }
                }

                listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, "Deleted " + recordsDeleted + " old CumulativeExtractionResults (That were not included in the final Patch you are preparing)"));
            }

            engine.DoRelease(CurrentRelease.ConfigurationsForRelease, CurrentRelease.EnvironmentPotential, isPatch: CurrentRelease.ReleaseState == ReleaseState.DoingPatch);

            return CurrentRelease;
        }

        public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {
        }

        public void Abort(IDataLoadEventListener listener)
        {
        }

        public void Check(ICheckNotifier notifier)
        {
            ((ICheckable) ReleaseSettings).Check(notifier);
        }

        public void PreInitialize(Project value, IDataLoadEventListener listener)
        {
            _project = value;
        }
    }
}
