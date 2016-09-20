using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngineTests.Integration;
using LoadModules.Extensions.Python.DataProvider;
using NUnit.Framework;

namespace LoadModules.Extensions.Python.Tests
{
    public class ScriptExecutionTests
    {
        [Test]
        public void SlowRollerTest()
        {
            var script =
@"import time

print (""1"")
time.sleep(1)
print (""1"")
time.sleep(1)
print (""1"")
time.sleep(1)
print (""1"")
time.sleep(1)
print (""1"")
time.sleep(1)
";

            var file = new FileInfo("threadscript.py");

            File.WriteAllText(file.FullName,script);
            try
            {
                var py = new PythonDataProvider();
                py.FullPathToPythonScriptToRun = file.FullName;
                py.Version = PythonVersion.Version3;

                var tomemory = new ToMemoryDataLoadJob();

                var exitCode = py.Fetch(tomemory, new GracefulCancellationToken());

                Assert.AreEqual(ProcessExitCode.Success, exitCode);

                Assert.AreEqual(5, tomemory.EventsReceivedBySender[py].Count(m=>m.Message.Equals("1")));


            }
            finally
            {
                file.Delete();
            }
        }
    }
}
