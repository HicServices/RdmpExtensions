﻿using System;
using System.IO;
using System.Linq;
using LoadModules.Extensions.Python.DataProvider;
using LoadModules.Extensions.Tests;
using NUnit.Framework;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Engine.Job;
using ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.Python.Tests.Unit
{
    public class Python2InstalledTests
    {
        [SetUp]
        public void IsPython2Installed()
        {
            PythonDataProvider p = new PythonDataProvider();
            p.Version = PythonVersion.Version2;
            try
            {
                string version = p.GetPythonVersion();

                Console.WriteLine("Found python version:" + version);
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
            string MyPythonScript = @"print 'Hello World'";

            var py = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Myscript.py");
            File.Delete(py);
            File.WriteAllText(py, MyPythonScript);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version2;
            provider.FullPathToPythonScriptToRun = (wrapFilename ? "\"" : "") + py + (wrapFilename ? "\"" : "");
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 0;

            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());
            provider.Check(new ThrowImmediatelyCheckNotifier() { ThrowOnWarning = true });

            provider.Fetch(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken());
        }

        [Test]
        public void PythonScript_Timeout()
        {
            string MyPythonScript = @"s = raw_input ('==>')";

            var py = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Myscript.py");
            File.Delete(py);
            File.WriteAllText(py, MyPythonScript);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version2;
            provider.FullPathToPythonScriptToRun = py;
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 5;

            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());

            //new MockRepository().DynamicMock<IDataLoadJob>()
            var ex = Assert.Throws<Exception>(()=>provider.Fetch(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken()));

            Assert.IsTrue(ex.Message.Contains("Python command timed out"));

        }

        [Test]
        public void PythonScript_OverrideExecutablePath_DodgyFileType()
        {
            string MyPythonScript = @"s = raw_input ('==>')";

            var py = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Myscript.py");

            File.Delete(py);
            File.WriteAllText(py, MyPythonScript);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version2;
            provider.FullPathToPythonScriptToRun = py;
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 5;
            provider.OverridePythonExecutablePath = new FileInfo(py);
            //call with accept all
            var ex = Assert.Throws<Exception>(()=>provider.Check(new AcceptAllCheckNotifier()));

            StringAssert.Contains(@"Myscript.py file is not called python.exe... what is going on here?",ex.Message);
        }

        [Test]
        public void PythonScript_NonExistentFile()
        {
            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version2;
            provider.FullPathToPythonScriptToRun = "ImANonExistentFile.py";
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 50;
            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());

            var toMemory = new ToMemoryDataLoadJob(false);

            var result = provider.Fetch(toMemory, new GracefulCancellationToken());

            Assert.IsTrue(toMemory.EventsReceivedBySender[provider].Any(m => m.Message.Contains("can't open file 'ImANonExistentFile.py'")));

        }
    }
}