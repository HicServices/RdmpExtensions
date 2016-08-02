using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngine.Job;
using LoadModules.Extensions.Python.DataProvider;
using NUnit.Framework;
using ReusableLibraryCode.Checks;
using Rhino.Mocks;
using Tests.Common;

namespace LoadModules.Extensions.Python.Tests.Unit
{
    public class TestsThatWorkRegardless
    {

        [Test]
        [ExpectedException(ExpectedMessage = "Version of Python required for script has not been selected")]
        public void PythonVersionNotSetYet()
        {
            PythonDataProvider provider = new PythonDataProvider();
            provider.Check(new ThrowImmediatelyCheckNotifier());
        }


        [Test]
        [ExpectedException(ExpectedMessage = @"The specified OverridePythonExecutablePath:C:\fishmongers\python does not exist", MatchType = MessageMatch.Contains)]
        public void PythonScript_OverrideExecutablePath_FileDoesntExist()
        {
            string MyPythonScript = @"s = raw_input ('==>')";

            File.Delete("Myscript.py");
            File.WriteAllText("Myscript.py", MyPythonScript);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version2;
            provider.FullPathToPythonScriptToRun = "Myscript.py";
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 5;
            provider.OverridePythonExecutablePath = new FileInfo(@"C:\fishmongers\python");
            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());

            provider.Fetch(MockRepository.GenerateStub<IDataLoadJob>(), new GracefulCancellationToken());
        }

    }
}
