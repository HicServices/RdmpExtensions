using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode;

namespace LoadModules.Extensions.AutomationPlugins.Data
{
    public class SuccessfullyExtractedResults : DatabaseEntity
    {
        #region Database Properties

        private string _sQL;
        private DateTime _extractDate;
        #endregion

        public string SQL
        {
            get { return _sQL; }
            set { SetField(ref _sQL, value); }
        }
        public DateTime ExtractDate
        {
            get { return _extractDate; }
            set { SetField(ref _extractDate, value); }
        }
        public SuccessfullyExtractedResults(AutomateExtractionRepository repository,string sql)
        {
            repository.InsertAndHydrate(this, new Dictionary<string, object>()
            {
                {"SQL",sql}
            });

            if (ID == 0 || Repository != repository)
                throw new ArgumentException("Repository failed to properly hydrate this class");
        }
        public SuccessfullyExtractedResults(AutomateExtractionRepository repository, DbDataReader r)
            : base(repository, r)
        {
            SQL = r["SQL"].ToString();
            ExtractDate = Convert.ToDateTime(r["ExtractDate"]);
        }


        public void SetExtractionIdentifiers(HashSet<string> releaseIdentifiersSeen)
        {
            var repo = (TableRepository) Repository;
            var server = repo.DiscoveredServer;

            var dt = new DataTable();

            dt.Columns.Add("SuccessfullyExtractedResults_ID", typeof (int));
            dt.Columns.Add("ReleaseIdentifier", typeof (string));

            foreach (string s in releaseIdentifiersSeen)
                dt.Rows.Add(ID, s);

            var bulkCopy = new SqlBulkCopy(server.Builder.ConnectionString);
            bulkCopy.ColumnMappings.Add("SuccessfullyExtractedResults_ID", "SuccessfullyExtractedResults_ID");
            bulkCopy.ColumnMappings.Add("ReleaseIdentifier", "ReleaseIdentifier");

            UsefulStuff.BulkInsertWithBetterErrorMessages(bulkCopy, dt, repo.DiscoveredServer);
        }
    }
}
