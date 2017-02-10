using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary;
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

                Assert.AreEqual(ExitCodeType.Success, exitCode);

                Assert.AreEqual(5, tomemory.EventsReceivedBySender[py].Count(m=>m.Message.Equals("1")));


            }
            finally
            {
                file.Delete();
            }
        }

        [Test]
        public void SlowRollerAsync()
        {
            var script =
@"import time
import sys

print (""GetReady"")
sys.stdout.flush()

time.sleep(5)
print (""FIGHT!"")
sys.stdout.flush()
";

            var file = new FileInfo("threadscript.py");

            File.WriteAllText(file.FullName, script);
            try
            {
                var py = new PythonDataProvider();
                py.FullPathToPythonScriptToRun = file.FullName;
                py.Version = PythonVersion.Version3;

                var tomemory = new ToMemoryDataLoadJob();


                var task = new Task(() => py.Fetch(tomemory, new GracefulCancellationToken()));
                task.Start();

                //wait 1 second, the first message should have come through but not the second
                Task.Delay(2000).Wait();

                Assert.IsTrue(tomemory.EventsReceivedBySender[py].Any(m => m.Message.Equals("GetReady")));
                Assert.IsFalse(tomemory.EventsReceivedBySender[py].Any(m => m.Message.Equals("FIGHT!")));
                Assert.IsFalse(task.IsCompleted);

                //wait another 6 seconds
                Task.Delay(6000).Wait();
                
                //now both messages should have come through
                Assert.IsTrue(tomemory.EventsReceivedBySender[py].Any(m => m.Message.Equals("GetReady")));
                Assert.IsTrue(tomemory.EventsReceivedBySender[py].Any(m => m.Message.Equals("FIGHT!")));

                //should already be finished
                Assert.IsTrue(task.IsCompleted);

            }
            finally
            {
                file.Delete();
            }
        }


        [Test]
        public void ThrowDeadlock()
        {
            var script =
@"from __future__ import print_function
import sys

def eprint(*args, **kwargs):
    print(*args, file=sys.stderr, **kwargs)

print(""Test Normal Msg"")

eprint(""Test Error"")
";

            var file = new FileInfo("threadscript.py");

            File.WriteAllText(file.FullName, script);
            try
            {
                var py = new PythonDataProvider();
                py.FullPathToPythonScriptToRun = file.FullName;
                py.Version = PythonVersion.Version3;

                var tomem = new ToMemoryDataLoadJob(true);
                var ex = Assert.Throws<Exception>(()=>py.Fetch(tomem, new GracefulCancellationToken()));
                Assert.AreEqual("Test Error",ex.Message);

            }
            finally
            {
                file.Delete();
            }
        }
    }
}
