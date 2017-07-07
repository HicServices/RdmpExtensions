using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CatalogueLibrary.DataFlowPipeline;
using CatalogueLibrary.DataFlowPipeline.Requirements;
using DataExportLibrary.ExtractionTime;
using DataExportLibrary.ExtractionTime.Commands;
using DataExportLibrary.Interfaces.Data.DataTables;
using DataExportLibrary.Interfaces.ExtractionTime.Commands;
using HIC.Logging;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
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
                _dataset = ds.DatasetBundle.DataSet;
                var finder = new AutomateExtractionRepositoryFinder(ds.RepositoryLocator);
                _repo = finder.GetRepositoryIfAny() as AutomateExtractionRepository;

                if(_repo == null)
                    throw new Exception("Could not create AutomateExtractionRepository, are you missing an AutomationPluginsDatabase?");

                var matches = _repo.GetAllObjects<AutomateExtraction>("WHERE ExtractionConfiguration_ID = " + ds.Configuration.ID);

                if(matches.Length == 0)
                    throw new Exception("ExtractionConfiguration '" + ds.Configuration + "' does not have an entry in the AutomateExtractionRepository");

                //index ensure you can't have multiple so this shouldn't blow up
                _automateExtraction = matches.Single();
                if(_automateExtraction.BaselineDate == null)
                {
                    _automateExtraction.BaselineDate = DateTime.Now;
                    _automateExtraction.SaveToDatabase();
                }
                
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
        
        private DataTable ProcessPipelineData(ExtractCohortCustomTableCommand customData, DataTable toProcess, IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Warning, "Custom Data is not audited and supported by " + GetType().Name));

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
