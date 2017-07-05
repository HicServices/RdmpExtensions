using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HIC.Logging;
using LoadModules.Extensions.AutomationPlugins.Data;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using MapsDirectlyToDatabaseTable;
using NUnit.Framework;
using ReusableLibraryCode;

namespace LoadModules.Extensions.AutomationPlugins.Execution.ExtractionPipeline
{
    public class IdentifierAccumulator
    {
        private static readonly Dictionary<int, IdentifierAccumulator> Accumulators =
            new Dictionary<int, IdentifierAccumulator>();

        private static readonly object oAccumulatorsLock = new object();

        public static IdentifierAccumulator GetInstance(DataLoadInfo dataLoadInfo)
        {
            lock (oAccumulatorsLock)
            {
                if (!Accumulators.ContainsKey(dataLoadInfo.ID))
                    Accumulators.Add(dataLoadInfo.ID, new IdentifierAccumulator());

                return Accumulators[dataLoadInfo.ID];
            }
        }

        private IdentifierAccumulator()
        {

        }
        
        HashSet<string>  identifiers = new HashSet<string>();

        public void AddIdentifierIfNotSee(string identifier)
        {
            identifiers.Add(identifier);
        }

        public void CommitCurrentState(AutomateExtractionRepository repository, AutomateExtraction automateExtraction)
        {
            //todo this must be a MERGE if we want it to work with incremental deltas executions

            //only clar/commit on one thread at once!
            lock (oAccumulatorsLock)
            {
                var dt = new DataTable();

                dt.Columns.Add("AutomateExtraction_ID", typeof(int));
                dt.Columns.Add("ReleaseID", typeof(string));

                int id = automateExtraction.ID;

                foreach (string s in identifiers)
                    dt.Rows.Add(id, s);

                //clear old history
                using (SqlConnection con = new SqlConnection(repository.ConnectionString))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("DELETE FROM ReleaseIdentifiersSeen where AutomateExtraction_ID = " + automateExtraction.ID, con);
                    cmd.ExecuteNonQuery();
                }
            
                //bulk insert new history
                var bulkCopy = new SqlBulkCopy(repository.DiscoveredServer.Builder.ConnectionString);
                bulkCopy.ColumnMappings.Add("AutomateExtraction_ID", "AutomateExtraction_ID");
                bulkCopy.ColumnMappings.Add("ReleaseID", "ReleaseID");
                bulkCopy.DestinationTableName = "ReleaseIdentifiersSeen";
                UsefulStuff.BulkInsertWithBetterErrorMessages(bulkCopy, dt, repository.DiscoveredServer);
            }
        }
    }
}
