using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataFlowPipeline.Requirements;
using CatalogueLibrary.Repositories;
using DataExportLibrary.ExtractionTime;
using DataExportLibrary.ExtractionTime.Commands;
using DataExportLibrary.Interfaces.ExtractionTime.Commands;
using DataExportLibrary.Repositories;
using HIC.Logging;
using LoadModules.Extensions.AutomationPlugins.Data;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.AutomationPlugins.Execution
{
    public class SuccessfullyExtractedResultsDocumenter : IPluginDataFlowComponent<DataTable>, IPipelineRequirement<IExtractCommand>
    {

        private IExtractCommand _extractDatasetCommand;
        private string _sql = null;

        HashSet<string>  _releaseIdentifiersSeen = new HashSet<string>();
        private AutomateExtractionRepository _repo;
        private AutomateExtraction _automateExtraction;
        private DataLoadInfo _dataLoadInfo;

        public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener,GracefulCancellationToken cancellationToken)
        {
            //it is a request for custom data
            var ds = _extractDatasetCommand as ExtractDatasetCommand;
            var customData = _extractDatasetCommand as ExtractCohortCustomTableCommand;

            if (ds != null)
                return ProcessPipelineData(ds, toProcess, listener, cancellationToken);

            if(customData != null)
                return ProcessPipelineData(customData, toProcess, listener, cancellationToken);

            throw new NotSupportedException("Expected IExtractCommand to be ExtractDatasetCommand or ExtractCohortCustomTableCommand");
        }

        private DataTable ProcessPipelineData(ExtractDatasetCommand ds, DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            if(_sql == null)
            {

                _sql = ds.QueryBuilder.SQL;


                var finder = new AutomateExtractionRepositoryFinder(ds.RepositoryLocator);
                _repo = finder.GetRepositoryIfAny() as AutomateExtractionRepository;

                if(_repo == null)
                    throw new Exception("Could not create AutomateExtractionRepository, are you missing an AutomationPluginsDatabase?");

                var matches = _repo.GetAllObjects<AutomateExtraction>("WHERE ExtractionConfiguration_ID = " + ds.Configuration.ID);

                if(matches.Length == 0)
                    throw new Exception("ExtractionConfiguration '" + ds.Configuration + "' does not have an entry in the AutomateExtractionRepository");

                //index ensure you can't have multiple so this shouldn't blow up
                _automateExtraction = matches.Single();

                _automateExtraction.LastAttempt = DateTime.Now;
                _automateExtraction.LastAttemptDataLoadRunID = _dataLoadInfo.ID;
                
                _automateExtraction.SaveToDatabase();

            }
            
            foreach (ReleaseIdentifierSubstitution substitution in ds.ReleaseIdentifierSubstitutions)
            {
                foreach (DataRow dr in toProcess.Rows)
                {
                    var value = dr[substitution.GetRuntimeName()];

                    if(value == null || value == DBNull.Value)
                        continue;

                    _releaseIdentifiersSeen.Add(value.ToString());
                }
            }

            return toProcess;
        }
        
        private DataTable ProcessPipelineData(ExtractCohortCustomTableCommand customData, DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Warning, "Custom Data is not audited and supported by " + GetType().Name));

            //we don't do these yet
            return toProcess;
        }


        public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {
            //it completed succesfully right?
            if (pipelineFailureExceptionIfAny == null)
            {
                var successRecord = new SuccessfullyExtractedResults(_repo, _sql);
                successRecord.SetExtractionIdentifiers(_releaseIdentifiersSeen);
            }
        }

        public void Abort(IDataLoadEventListener listener)
        {
            
        }

        public void Check(ICheckNotifier notifier)
        {
            
        }

        public void PreInitialize(IExtractCommand value, IDataLoadEventListener listener)
        {
            _extractDatasetCommand = value;
        }

        public void PreInitialize(DataLoadInfo value, IDataLoadEventListener listener)
        {
            _dataLoadInfo = value;
        }
    }
}
