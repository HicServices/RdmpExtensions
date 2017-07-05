using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.Interfaces.Data.DataTables;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode;

namespace LoadModules.Extensions.AutomationPlugins.Data
{
    public class SuccessfullyExtractedResults : DatabaseEntity
    {
        #region Database Properties

        private string _sQL;
        private int _extractableDataSet_ID;
        private int _automateExtraction_ID;
        
        public string SQL
        {
            get { return _sQL; }
            set { SetField(ref _sQL, value); }
        }
        public int ExtractableDataSet_ID
        {
            get { return _extractableDataSet_ID; }
            set { SetField(ref _extractableDataSet_ID, value); }
        }
        public int AutomateExtraction_ID
        {
            get { return _automateExtraction_ID; }
            set { SetField(ref _automateExtraction_ID, value); }
        }
        #endregion

        public SuccessfullyExtractedResults(AutomateExtractionRepository repository,string sql, AutomateExtraction parent, IExtractableDataSet dataset)
        {
            repository.InsertAndHydrate(this, new Dictionary<string, object>()
            {
                {"SQL",sql},
                {"ExtractableDataSet_ID",dataset.ID},
                {"AutomateExtraction_ID",parent.ID}
            });

            if (ID == 0 || Repository != repository)
                throw new ArgumentException("Repository failed to properly hydrate this class");
        }
        public SuccessfullyExtractedResults(AutomateExtractionRepository repository, DbDataReader r)
            : base(repository, r)
        {
            SQL = r["SQL"].ToString();
            ExtractableDataSet_ID = Convert.ToInt32(r["ExtractableDataSet_ID"]);
            AutomateExtraction_ID = Convert.ToInt32(r["AutomateExtraction_ID"]);
        }
    }
}
