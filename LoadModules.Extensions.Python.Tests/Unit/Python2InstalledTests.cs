using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngine.Job;
using LoadModules.Extensions.Python.DataProvider;
using NUnit.Framework;
using ReusableLibraryCode.Checks;
using Rhino.Mocks;
using Tests.Common;

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

            File.Delete("Myscript.py");
            File.WriteAllText("Myscript.py", MyPythonScript);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version2;
            provider.FullPathToPythonScriptToRun = (wrapFilename ? "\"" : "") + "Myscript.py" + (wrapFilename ? "\"" : "");
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 0;

            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());
            provider.Check(new ThrowImmediatelyCheckNotifier() { ThrowOnWarning = true });

            //new MockRepository().DynamicMock<IDataLoadJob>()
            provider.Fetch(MockRepository.GenerateStub<IDataLoadJob>(), new GracefulCancellationToken());
        }

        [Test]
        public void PythonScript_Timeout()
        {
            string MyPythonScript = @"s = raw_input ('==>')";

            File.Delete("Myscript.py");
            File.WriteAllText("Myscript.py", MyPythonScript);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version2;
            provider.FullPathToPythonScriptToRun = "Myscript.py";
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 5;

            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());

            //new MockRepository().DynamicMock<IDataLoadJob>()
            ProcessExitCode result = provider.Fetch(MockRepository.GenerateStub<IDataLoadJob>(), new GracefulCancellationToken());
            Assert.AreEqual(ProcessExitCode.Failure, result);
        }

        [Test]
        [ExpectedException(ExpectedMessage = @"The specified OverridePythonExecutablePath:Myscript.py file is not called python.exe... what is going on here?", MatchType = MessageMatch.Contains)]
        public void PythonScript_OverrideExecutablePath_DodgyFileType()
        {
            string MyPythonScript = @"s = raw_input ('==>')";

            File.Delete("Myscript.py");
            File.WriteAllText("Myscript.py", MyPythonScript);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version2;
            provider.FullPathToPythonScriptToRun = "Myscript.py";
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 5;
            provider.OverridePythonExecutablePath = new FileInfo(@"Myscript.py");
            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());

            provider.Fetch(MockRepository.GenerateStub<IDataLoadJob>(), new GracefulCancellationToken());
        }

        [Test]
        [ExpectedException(ExpectedMessage = "can't open file 'ImANonExistentFile.py': [Errno 2] No such file or directory", MatchType = MessageMatch.Contains)]
        public void PythonScript_NonExistentFile()
        {
            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version2;
            provider.FullPathToPythonScriptToRun = "ImANonExistentFile.py";
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 50;
            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());

            var result = provider.Fetch(MockRepository.GenerateStub<IDataLoadJob>(), new GracefulCancellationToken());

            Assert.AreEqual(ProcessExitCode.Failure,result);
        }
    }
}
