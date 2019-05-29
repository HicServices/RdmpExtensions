using System;
using System.Collections.Generic;
using System.Data;
using LoadModules.Extensions.Interactive.DeAnonymise;
using NUnit.Framework;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataFlowPipeline;
using ReusableLibraryCode.Progress;
using Tests.Common.Scenarios;

namespace LoadModules.Extensions.Interactive.Tests
{
    public class DeAnonymiseAgainstCohortTests:TestsRequiringACohort,IDeAnonymiseAgainstCohortConfigurationFulfiller
    {
        private DeAnonymiseAgainstCohort _deAnonymiseAgainstCohort;
        public IExtractableCohort ChosenCohort { get; set; }
        public string OverrideReleaseIdentifier { get; set; }

        [SetUp]
        public void setup()
        {
            _deAnonymiseAgainstCohort = new DeAnonymiseAgainstCohort();

            ChosenCohort = _extractableCohort;
            _deAnonymiseAgainstCohort.ConfigurationGetter = this;//we force it to use this one (otherwise it would launch a Windows Form)
            
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void Normal_ReleaseDeAnonToPrivateKeys(bool doRedundantOverride)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("ReleaseID");
            dt.Columns.Add("Animal");

            foreach (KeyValuePair<string, string> kvp in _cohortKeysGenerated)
                dt.Rows.Add(kvp.Value, "fish");

            var clone = dt.Copy();

            if (doRedundantOverride)
                OverrideReleaseIdentifier = "ReleaseID";

            DataTable result = _deAnonymiseAgainstCohort.ProcessPipelineData(dt, new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());
            dt = clone;//refetch the original because ProcessPipelineData is a destructive operation

            Assert.IsTrue(result.Columns.Contains("PrivateID"));

            for(int i=0;i<result.Rows.Count;i++)
            {
                Assert.AreEqual(
                    _cohortKeysGenerated[(string)result.Rows[i]["PrivateID"]],
                    dt.Rows[i]["ReleaseID"]);
            }

            OverrideReleaseIdentifier = null;
        }

        [Test]
        public void Freaky_ColumnNameOverriding()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("HappyFunTimes");
            dt.Columns.Add("Animal");

            foreach (KeyValuePair<string, string> kvp in _cohortKeysGenerated)
                dt.Rows.Add(kvp.Value, "fish");

            var clone = dt.Copy();

            OverrideReleaseIdentifier = "HappyFunTimes";
            try
            {
                DataTable result = _deAnonymiseAgainstCohort.ProcessPipelineData(dt, new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken());
                dt = clone;//refetch the original because ProcessPipelineData is a destructive operation

                Assert.IsTrue(result.Columns.Contains("PrivateID"));

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Assert.AreEqual(
                        _cohortKeysGenerated[(string)result.Rows[i]["PrivateID"]],
                        dt.Rows[i]["HappyFunTimes"]);
                }
            }
            finally
            {
                OverrideReleaseIdentifier = null;
            }
        }

        [Test]
        public void Throws_ColumnMissing()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Animal");

            foreach (KeyValuePair<string, string> kvp in _cohortKeysGenerated)
                dt.Rows.Add("fish");

            var ex = Assert.Throws<ArgumentException>(() => _deAnonymiseAgainstCohort.ProcessPipelineData(dt, new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken()));

            Assert.IsTrue(ex.Message.StartsWith("Column 'ReleaseID' does not belong to table"));
        }

        [Test]
        public void Throws_ColumnMissingWithOverride()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Animal");

            foreach (KeyValuePair<string, string> kvp in _cohortKeysGenerated)
                dt.Rows.Add("fish");

            OverrideReleaseIdentifier = "HappyFace";

            var ex = Assert.Throws<ArgumentException>(() => _deAnonymiseAgainstCohort.ProcessPipelineData(dt, new ThrowImmediatelyDataLoadEventListener(), new GracefulCancellationToken()));
            Assert.AreEqual(ex.Message,"Cannot DeAnonymise cohort because you specified OverrideReleaseIdentifier of 'HappyFace' but the DataTable toProcess did not contain a column of that name");
        }

    }
}
