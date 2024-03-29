﻿using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LoadModules.Extensions.Python.DataProvider;
using LoadModules.Extensions.Python.Tests.Unit;
using LoadModules.Extensions.Tests;
using NUnit.Framework;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.Python.Tests;

public class ScriptExecutionTests
{
    [SetUp]
    public void IsPython2ANDPython3Installed()
    {
        new Python2InstalledTests().IsPython2Installed();
        new Python3InstalledTests().IsPython3Installed();
    }

    [Test]
    public void SlowRollerTest()
    {
        const string script = @"import time

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
            var py = new PythonDataProvider
            {
                FullPathToPythonScriptToRun = file.FullName,
                Version = PythonVersion.Version3
            };

            var toMemory = new ToMemoryDataLoadJob();

            var exitCode = py.Fetch(toMemory, new GracefulCancellationToken());

            Assert.Multiple(() =>
            {
                Assert.That(exitCode, Is.EqualTo(ExitCodeType.Success));

                Assert.That(toMemory.EventsReceivedBySender[py].Count(m => m.Message.Equals("1")), Is.EqualTo(5));
            });
        }
        finally
        {
            file.Delete();
        }
    }

    [Test]
    public void SlowRollerAsync()
    {
        const string script = @"import time
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
            var py = new PythonDataProvider
            {
                FullPathToPythonScriptToRun = file.FullName,
                Version = PythonVersion.Version3
            };

            var tomemory = new ToMemoryDataLoadJob();


            var task = new Task(() => py.Fetch(tomemory, new GracefulCancellationToken()));
            task.Start();

            //wait 1 second, the first message should have come through but not the second
            Task.Delay(2000).Wait();

            Assert.Multiple(() =>
            {
                Assert.That(tomemory.EventsReceivedBySender[py].Any(static m => m.Message.Equals("GetReady")), Is.True);
                Assert.That(tomemory.EventsReceivedBySender[py].Any(static m => m.Message.Equals("FIGHT!")), Is.False);
                Assert.That(task.IsCompleted, Is.False);
            });

            //wait another 6 seconds
            Task.Delay(6000).Wait();

            Assert.Multiple(() =>
            {
                //now both messages should have come through
                Assert.That(tomemory.EventsReceivedBySender[py].Any(static m => m.Message.Equals("GetReady")), Is.True);
                Assert.That(tomemory.EventsReceivedBySender[py].Any(static m => m.Message.Equals("FIGHT!")), Is.True);

                //should already be finished
                Assert.That(task.IsCompleted, Is.True);
            });
        }
        finally
        {
            file.Delete();
        }
    }


    [Test]
    public void WriteToErrorAndStandardOut()
    {
        const string script = @"from __future__ import print_function
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
            var py = new PythonDataProvider
            {
                FullPathToPythonScriptToRun = file.FullName,
                Version = PythonVersion.Version3
            };

            var tomem = new ToMemoryDataLoadJob(true);
            py.Fetch(tomem, new GracefulCancellationToken());

            Assert.Multiple(() =>
            {
                Assert.That(
                            tomem.EventsReceivedBySender[py].First(static n => n.ProgressEventType == ProgressEventType.Information)
                                .Message, Is.EqualTo("Test Normal Msg"));


                Assert.That(
                    tomem.EventsReceivedBySender[py].Single(static n => n.ProgressEventType == ProgressEventType.Warning)
                        .Message, Is.EqualTo("Test Error"));
            });
        }
        finally
        {
            file.Delete();
        }
    }

    [Test]
    public void TestCodeSources()
    {
        var tomemory = new ToMemoryDataLoadJob();
        tomemory.OnNotify(this, new NotifyEventArgs(ProgressEventType.Warning, "pippo"));

    }
}