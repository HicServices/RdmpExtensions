using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapsDirectlyToDatabaseTable;

namespace LoadModules.Extensions.AutomationPlugins.Data
{
    public class AutomateExtractionRepository : TableRepository
    {
        public AutomateExtractionRepository(DbConnectionStringBuilder builder):base(null,builder)
        {
            
        }
        protected override IMapsDirectlyToDatabaseTable ConstructEntity(Type t, DbDataReader reader)
        {
            // Find the constructor
            var constructorInfo = t.GetConstructor(new[] { typeof(IRepository), typeof(DbDataReader) });
            if (constructorInfo == null)
                throw new Exception("ConstructEntity<" + t.Name + "> requires that the specified IMapsDirectlyToDatabaseTable object implements the constructor IRepository, DbDataReader");

            var toReturn = (IMapsDirectlyToDatabaseTable)constructorInfo.Invoke(new object[] { this, reader });

            toReturn.Repository = this;
            return toReturn;
        }
    }
}
