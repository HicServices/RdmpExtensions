using System;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using Rdmp.Core.DataExport.DataExtraction.Pipeline.Sources;
using Rdmp.Core.Repositories;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline;

public class BaselineHackerExecuteDatasetExtractionSource : ExecuteDatasetExtractionSource
{
    public override string HackExtractionSQL(string sql, IDataLoadEventListener listener)
    {
        var finder = new AutomateExtractionRepositoryFinder(new RepositoryProvider(Request.DataExportRepository));
        var repository = (AutomateExtractionRepository) finder.GetRepositoryIfAny();

        if(repository == null)
            throw new Exception("AutomateExtractionRepositoryFinder returned null, are you missing an AutomationPlugins database");

        DeltaHacker hacker = new DeltaHacker(repository,Request);

        string hackSql;

        //hacking allowed
        if (hacker.ExecuteHackIfAllowed(listener, out hackSql) == BaselineHackEvaluation.Allowed)
        {
            var newSql = sql + hackSql;
            listener.OnNotify(this,new NotifyEventArgs(ProgressEventType.Information, "Full Hacked Query is now:" + Environment.NewLine + newSql));

            return newSql;
        }


        //no hacking allowed
        return sql;
    }
}