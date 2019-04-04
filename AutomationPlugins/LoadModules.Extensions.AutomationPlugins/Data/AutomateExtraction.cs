using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using CatalogueLibrary.Data;
using DataExportLibrary.Data.DataTables;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using MapsDirectlyToDatabaseTable;
using RDMPStartup;

namespace LoadModules.Extensions.AutomationPlugins.Data
{
    public class AutomateExtraction : DatabaseEntity, IMapsDirectlyToDatabaseTable
    {
        private readonly AutomateExtractionRepository _repository;

        #region Database Properties

        private int _extractionConfiguration_ID;
        private int _automateExtractionSchedule_ID;
        private bool _disabled;
        private DateTime? _baselineDate;
        private bool _refreshCohort;
        private bool _release;

        public int ExtractionConfiguration_ID
        {
            get { return _extractionConfiguration_ID; }
            set { SetField(ref _extractionConfiguration_ID, value); }
        }
        public int AutomateExtractionSchedule_ID
        {
            get { return _automateExtractionSchedule_ID; }
            set { SetField(ref _automateExtractionSchedule_ID, value); }
        }
        public bool Disabled
        {
            get { return _disabled; }
            set { SetField(ref _disabled, value); }
        }
        public DateTime? BaselineDate
        {
            get { return _baselineDate; }
            set { SetField(ref _baselineDate, value); }
        }

        public bool RefreshCohort
        {
            get { return _refreshCohort; }
            set {SetField(ref _refreshCohort , value); }
        }

        public bool Release
        {
            get { return _release; }
            set { SetField(ref _release , value);}
        }

        #endregion

        #region Relationships
        
        [NoMappingToDatabase]
        public IExtractionConfiguration ExtractionConfiguration { get
        {
            return _repository.DataExportRepository.GetObjectByID<ExtractionConfiguration>(ExtractionConfiguration_ID);
        } }

        [NoMappingToDatabase]
        public AutomateExtractionSchedule AutomateExtractionSchedule { get
        {
            return _repository.GetObjectByID<AutomateExtractionSchedule>(AutomateExtractionSchedule_ID);
        }}

        #endregion

        public AutomateExtraction(PluginRepository repository, AutomateExtractionSchedule schedule, IExtractionConfiguration config)
        {
            _repository = (AutomateExtractionRepository) repository;
            repository.InsertAndHydrate(this, new Dictionary<string, object>()
            {
                {"AutomateExtractionSchedule_ID",schedule.ID},
                {"ExtractionConfiguration_ID",config.ID},
                {"RefreshCohort",false},
                {"Release",false},

            });

            if (ID == 0 || Repository != repository)
                throw new ArgumentException("Repository failed to properly hydrate this class");
        }
        public AutomateExtraction(PluginRepository repository, DbDataReader r)
            : base(repository, r)
        {
            _repository = (AutomateExtractionRepository) repository;
            ExtractionConfiguration_ID = Convert.ToInt32(r["ExtractionConfiguration_ID"]);
            AutomateExtractionSchedule_ID = Convert.ToInt32(r["AutomateExtractionSchedule_ID"]);
            Disabled = Convert.ToBoolean(r["Disabled"]);
            BaselineDate = ObjectToNullableDateTime(r["BaselineDate"]);

            RefreshCohort = Convert.ToBoolean(r["RefreshCohort"]);
            Release = Convert.ToBoolean(r["Release"]);
        }

        private ExtractionConfiguration _cachedExtractionConfiguration;
        

        public override string ToString()
        {
            if (_cachedExtractionConfiguration == null)
                _cachedExtractionConfiguration = _repository.DataExportRepository.GetObjectByID<ExtractionConfiguration>(ExtractionConfiguration_ID);

            return _cachedExtractionConfiguration.Name;
        }

        public DataTable GetIdentifiersTable()
        {
            var dt = new DataTable();

            var repo = (TableRepository)Repository;
            var server = repo.DiscoveredServer;

            using (var con = server.GetConnection())
            {
                con.Open();
                var cmd = server.GetCommand("Select ReleaseID from ReleaseIdentifiersSeen", con);
                var da = server.GetDataAdapter(cmd);
                da.Fill(dt);
            }

            return dt;
        }

        public SuccessfullyExtractedResults GetSuccessIfAnyFor(IExtractableDataSet ds)
        {
            return _repository.GetAllObjects<SuccessfullyExtractedResults>(@"WHERE ExtractableDataSet_ID  = " + ds.ID + " AND AutomateExtraction_ID = " + ID).SingleOrDefault();
        }

        public void ClearBaselines()
        {
            using (var con = _repository.DiscoveredServer.GetConnection())
            {
                con.Open();
                new SqlCommand(@"Delete From 
  [ReleaseIdentifiersSeen]
  where
  AutomateExtraction_ID = " + ID, (SqlConnection) con).ExecuteNonQuery();
            }

            foreach (SuccessfullyExtractedResults r in _repository.GetAllObjectsWithParent<SuccessfullyExtractedResults>(this))
                r.DeleteInDatabase();
            
            BaselineDate = null;
            SaveToDatabase();
        }
    }
}
