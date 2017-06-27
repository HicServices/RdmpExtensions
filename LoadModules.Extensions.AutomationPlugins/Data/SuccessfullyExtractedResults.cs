using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using MapsDirectlyToDatabaseTable;

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
        public SuccessfullyExtractedResults(IRepository repository,string sql)
        {
            repository.InsertAndHydrate(this, new Dictionary<string, object>()
            {
                {"SQL",sql}
            });

            if (ID == 0 || Repository != repository)
                throw new ArgumentException("Repository failed to properly hydrate this class");
        }
        public SuccessfullyExtractedResults(IRepository repository, DbDataReader r)
            : base(repository, r)
        {
            SQL = r["SQL"].ToString();
            ExtractDate = Convert.ToDateTime(r["ExtractDate"]);
        }
    }
}
