﻿using System;
using System.IO;
using System.Linq;
using LoadModules.Extensions.Python.DataProvider;
using LoadModules.Extensions.Tests;
using NUnit.Framework;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.Python.Tests.Unit;

public class Python2InstalledTests
{
    [SetUp]
    public void IsPython2Installed()
    {
        var p = new PythonDataProvider
        {
            Version = PythonVersion.Version2
        };
        try
        {
            var version = p.GetPythonVersion();

            Console.WriteLine($"Found python version:{version}");
        }
        catch (Exception e)
        {
            Console.WriteLine("Tests are inconclusive because python version 2 is not installed in the expected location");

            Console.WriteLine(e.ToString());
            Assert.Inconclusive();
        }
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void PythonScript_Version2_GoodSyntax(bool wrapFilename)
    {
        var MyPythonScript = @"print 'Hello World'";

        var py = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Myscript.py");
        File.Delete(py);
        File.WriteAllText(py, MyPythonScript);

        var provider = new PythonDataProvider
        {
            Version = PythonVersion.Version2,
            FullPathToPythonScriptToRun = (wrapFilename ? "\"" : "") + py + (wrapFilename ? "\"" : ""),
            MaximumNumberOfSecondsToLetScriptRunFor = 0
        };

        //call with accept all
        provider.Check(new AcceptAllCheckNotifier());
        provider.Check(ThrowImmediatelyCheckNotifier.QuietPicky);

        provider.Fetch(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken());
    }

    [Test]
    public void PythonScript_Timeout()
    {
        var MyPythonScript = "import time\ntime.sleep(10)";

        var py = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Myscript.py");
        File.Delete(py);
        File.WriteAllText(py, MyPythonScript);

        var provider = new PythonDataProvider
        {
            Version = PythonVersion.Version2,
            FullPathToPythonScriptToRun = py,
            MaximumNumberOfSecondsToLetScriptRunFor = 5
        };

        //call with accept all
        provider.Check(new AcceptAllCheckNotifier());

        //new MockRepository().DynamicMock<IDataLoadJob>()
        var ex = Assert.Throws<Exception>(()=>provider.Fetch(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken()));

        Assert.That(ex?.Message.Contains("Python command timed out"), Is.True, $"Unexpected exception for timeout: {ex?.Message}");

    }

    [Test]
    public void PythonScript_OverrideExecutablePath_DodgyFileType()
    {
        const string MyPythonScript = "s = raw_input ('==>')";

        var py = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Myscript.py");

        File.Delete(py);
        File.WriteAllText(py, MyPythonScript);

        var provider = new PythonDataProvider
        {
            Version = PythonVersion.Version2,
            FullPathToPythonScriptToRun = py,
            MaximumNumberOfSecondsToLetScriptRunFor = 5,
            OverridePythonExecutablePath = new FileInfo(py)
        };
        //call with accept all
        var ex = Assert.Throws<Exception>(() => provider.Check(new AcceptAllCheckNotifier()));

        Assert.That(ex?.Message, Does.Contain(@"Myscript.py file is not called python.exe... what is going on here?"));
    }

    [Test]
    public void PythonScript_NonExistentFile()
    {
        var provider = new PythonDataProvider
        {
            Version = PythonVersion.Version2,
            FullPathToPythonScriptToRun = "ImANonExistentFile.py",
            MaximumNumberOfSecondsToLetScriptRunFor = 50
        };
        //call with accept all
        provider.Check(new AcceptAllCheckNotifier());

        var toMemory = new ToMemoryDataLoadJob(false);

        var result = provider.Fetch(toMemory, new GracefulCancellationToken());

        Assert.That(toMemory.EventsReceivedBySender[provider].Any(m => m.Message.Contains("can't open file 'ImANonExistentFile.py'")), Is.True);

    }
}