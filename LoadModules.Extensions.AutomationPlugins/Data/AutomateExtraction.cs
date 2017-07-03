using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using DataExportLibrary.Data.DataTables;
using MapsDirectlyToDatabaseTable;

namespace LoadModules.Extensions.AutomationPlugins.Data
{
    public class AutomateExtraction : DatabaseEntity
    {
        #region Database Properties

        private int _extractionConfiguration_ID;
        private DateTime? _lastAttempt;
        private int? _lastAttemptDataLoadRunID;
        private int? _pipeline_ID;
        private int _executionSchedule_ID;
        private int? _successfullyExtractedResults_ID;
        #endregion

        public int ExtractionConfiguration_ID
        {
            get { return _extractionConfiguration_ID; }
            set { SetField(ref _extractionConfiguration_ID, value); }
        }
        public DateTime? LastAttempt
        {
            get { return _lastAttempt; }
            set { SetField(ref _lastAttempt, value); }
        }
        public int? LastAttemptDataLoadRunID
        {
            get { return _lastAttemptDataLoadRunID; }
            set { SetField(ref _lastAttemptDataLoadRunID, value); }
        }
        public int? Pipeline_ID
        {
            get { return _pipeline_ID; }
            set { SetField(ref _pipeline_ID, value); }
        }
        public int ExecutionSchedule_ID
        {
            get { return _executionSchedule_ID; }
            set { SetField(ref _executionSchedule_ID, value); }
        }
        public int? SuccessfullyExtractedResults_ID
        {
            get { return _successfullyExtractedResults_ID; }
            set { SetField(ref _successfullyExtractedResults_ID, value); }
        }
        public AutomateExtraction(AutomateExtractionRepository repository,ExtractionConfiguration config)
        {
            repository.InsertAndHydrate(this, new Dictionary<string, object>()
            {
                {"ExtractionConfiguration_ID",config.ID}
            });

            if (ID == 0 || Repository != repository)
                throw new ArgumentException("Repository failed to properly hydrate this class");
        }
        public AutomateExtraction(IRepository repository, DbDataReader r)
            : base(repository, r)
        {
            ExtractionConfiguration_ID = Convert.ToInt32(r["ExtractionConfiguration_ID"]);
            LastAttempt = ObjectToNullableDateTime(r["LastAttempt"]);
            LastAttemptDataLoadRunID = ObjectToNullableInt(r["LastAttemptDataLoadRunID"]);
            Pipeline_ID = ObjectToNullableInt(r["Pipeline_ID"]);
            ExecutionSchedule_ID = Convert.ToInt32(r["ExecutionSchedule_ID"]);
            SuccessfullyExtractedResults_ID = ObjectToNullableInt(r["SuccessfullyExtractedResults_ID"]);
        }
    }
}
