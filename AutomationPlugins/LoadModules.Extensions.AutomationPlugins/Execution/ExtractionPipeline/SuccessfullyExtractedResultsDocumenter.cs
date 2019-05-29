using System;
using System.Data;
using System.Linq;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataExport.DataExtraction.Commands;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataFlowPipeline.Requirements;
using Rdmp.Core.Logging;
using Rdmp.Core.QueryBuilding;
using Rdmp.Core.Repositories;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline
{
    public class SuccessfullyExtractedResultsDocumenter : IPluginDataFlowComponent<DataTable>, IPipelineRequirement<IExtractCommand>, IPipelineRequirement<DataLoadInfo>
    {

        private IExtractCommand _extractDatasetCommand;
        private string _sql = null;

        private AutomateExtractionRepository _repo;
        private AutomateExtraction _automateExtraction;
        private DataLoadInfo _dataLoadInfo;
        private IdentifierAccumulator _accumulator;
        private IExtractableDataSet _dataset;

        public DataTable ProcessPipelineData(DataTable toProcess, IDataLoadEventListener listener,GracefulCancellationToken cancellationToken)
        {
            //it is a request for custom data
            var ds = _extractDatasetCommand as ExtractDatasetCommand;
            var global = _extractDatasetCommand as ExtractGlobalsCommand;

            if (ds != null)
                return ProcessPipelineData(ds, toProcess, listener, cancellationToken);

            if(global != null)
                return ProcessPipelineData(global, toProcess, listener, cancellationToken);

            throw new NotSupportedException("Expected IExtractCommand to be ExtractDatasetCommand or ExtractGlobalsCommand");
        }

        private DataTable ProcessPipelineData(ExtractDatasetCommand ds, DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            if(_sql == null)
            {
                _sql = ds.QueryBuilder.SQL;
                _dataset = ds.DatasetBundle.DataSet;
                
                var finder = new AutomateExtractionRepositoryFinder(new RepositoryProvider(ds.DataExportRepository));
                _repo = finder.GetRepositoryIfAny() as AutomateExtractionRepository;

                if(_repo == null)
                    throw new Exception("Could not create AutomateExtractionRepository, are you missing an AutomationPluginsDatabase?");

                var matches = _repo.GetAllObjects<AutomateExtraction>("WHERE ExtractionConfiguration_ID = " + ds.Configuration.ID);

                if(matches.Length == 0)
                    throw new Exception("ExtractionConfiguration '" + ds.Configuration + "' does not have an entry in the AutomateExtractionRepository");

                //index ensure you can't have multiple so this shouldn't blow up
                _automateExtraction = matches.Single();
               
                //delete any old baseline records 
                var success = _automateExtraction.GetSuccessIfAnyFor(ds.DatasetBundle.DataSet);
                if (success != null)
                    success.DeleteInDatabase();

                _accumulator = IdentifierAccumulator.GetInstance(_dataLoadInfo);

            }
            
            foreach (ReleaseIdentifierSubstitution substitution in ds.ReleaseIdentifierSubstitutions)
            {
                foreach (DataRow dr in toProcess.Rows)
                {
                    var value = dr[substitution.GetRuntimeName()];

                    if(value == null || value == DBNull.Value)
                        continue;

                    _accumulator.AddIdentifierIfNotSee(value.ToString());
                }
            }

            return toProcess;
        }
        
        private DataTable ProcessPipelineData(ExtractGlobalsCommand globalData, DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Warning, "Global Data is not audited and supported by " + GetType().Name));

            //we don't do these yet
            return toProcess;
        }


        public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {
            //it completed succesfully right?
            if (pipelineFailureExceptionIfAny == null && _dataset != null)
            {
                var successRecord = new SuccessfullyExtractedResults(_repo, _sql, _automateExtraction, _dataset);
                _accumulator.CommitCurrentState(_repo, _automateExtraction);
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
