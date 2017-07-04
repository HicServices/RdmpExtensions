using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.Interfaces.Data.DataTables;
using MapsDirectlyToDatabaseTable;

namespace LoadModules.Extensions.AutomationPlugins.Data
{
    public class AutomateExtraction : DatabaseEntity
    {
        private readonly AutomateExtractionRepository _repository;

        #region Database Properties

        private int _extractionConfiguration_ID;
        private DateTime? _lastAttempt;
        private int? _lastAttemptDataLoadRunID;
        private int _automateExtractionSchedule_ID;
        private int? _successfullyExtractedResults_ID;
        private bool _disabled;
        
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
        public int AutomateExtractionSchedule_ID
        {
            get { return _automateExtractionSchedule_ID; }
            set { SetField(ref _automateExtractionSchedule_ID, value); }
        }
        public int? SuccessfullyExtractedResults_ID
        {
            get { return _successfullyExtractedResults_ID; }
            set { SetField(ref _successfullyExtractedResults_ID, value); }
        }

        public bool Disabled
        {
            get { return _disabled; }
            set { SetField(ref _disabled, value); }
        }
        #endregion

        #region Relationships

        public SuccessfullyExtractedResults SuccessfullyExtractedResults
        {
            get
            {
                return SuccessfullyExtractedResults_ID != null
                    ? _repository.GetObjectByID<SuccessfullyExtractedResults>(SuccessfullyExtractedResults_ID.Value)
                    : null;
            }
        }

        #endregion

        public AutomateExtraction(AutomateExtractionRepository repository,AutomateExtractionSchedule schedule, IExtractionConfiguration config)
        {
            _repository = repository;
            repository.InsertAndHydrate(this, new Dictionary<string, object>()
            {
                {"AutomateExtractionSchedule_ID",schedule.ID},
                {"ExtractionConfiguration_ID",config.ID}
            });

            if (ID == 0 || Repository != repository)
                throw new ArgumentException("Repository failed to properly hydrate this class");
        }
        public AutomateExtraction(AutomateExtractionRepository repository, DbDataReader r)
            : base(repository, r)
        {
            _repository = repository;
            ExtractionConfiguration_ID = Convert.ToInt32(r["ExtractionConfiguration_ID"]);
            LastAttempt = ObjectToNullableDateTime(r["LastAttempt"]);
            LastAttemptDataLoadRunID = ObjectToNullableInt(r["LastAttemptDataLoadRunID"]);
            AutomateExtractionSchedule_ID = Convert.ToInt32(r["AutomateExtractionSchedule_ID"]);
            SuccessfullyExtractedResults_ID = ObjectToNullableInt(r["SuccessfullyExtractedResults_ID"]);
            Disabled = Convert.ToBoolean(r["Disabled"]);
        }

        private ExtractionConfiguration _cachedExtractionConfiguration;
        public override string ToString()
        {
            if (_cachedExtractionConfiguration == null)
                _cachedExtractionConfiguration = _repository.DataExportRepository.GetObjectByID<ExtractionConfiguration>(ExtractionConfiguration_ID);

            return _cachedExtractionConfiguration.Name;
        }
    }
}
