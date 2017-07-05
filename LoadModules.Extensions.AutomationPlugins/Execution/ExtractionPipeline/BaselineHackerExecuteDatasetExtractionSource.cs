using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataExportLibrary.ExtractionTime.ExtractionPipeline.Sources;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline
{
    public class BaselineHackerExecuteDatasetExtractionSource : ExecuteDatasetExtractionSource
    {
        public override string HackExtractionSQL(string sql, IDataLoadEventListener listener)
        {
            var finder = new AutomateExtractionRepositoryFinder(Request.RepositoryLocator);
            var repository = (AutomateExtractionRepository) finder.GetRepositoryIfAny();

            if(repository == null)
                throw new Exception("AutomateExtractionRepositoryFinder returned null, are you missing an AutomationPlugins database");

            QueryHacker hacker = new QueryHacker(repository,Request);

            string hackSql;

            //hacking allowed
            if (hacker.ExecuteHackIfAllowed(listener, out hackSql) == BaselineHackEvaluation.Allowed)
                return sql + hackSql;

            //no hacking allowed
            return sql;
        }
    }
}
