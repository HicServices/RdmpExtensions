using System;
using System.IO;
using LoadModules.Extensions.Python.DataProvider;
using NUnit.Framework;
using ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.Python.Tests.Unit
{
    public class TestsThatWorkRegardless
    {

        [Test]
        public void PythonVersionNotSetYet()
        {
            PythonDataProvider provider = new PythonDataProvider();
            var ex = Assert.Throws<Exception>(()=>provider.Check(new ThrowImmediatelyCheckNotifier()));
            Assert.AreEqual("Version of Python required for script has not been selected",ex.Message);
            
        }


        [Test]
        public void PythonScript_OverrideExecutablePath_FileDoesntExist()
        {
            string MyPythonScript = @"s = raw_input ('==>')";

            var py = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Myscript.py");

            File.Delete(py);
            File.WriteAllText(py, MyPythonScript);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version2;
            provider.FullPathToPythonScriptToRun = py;
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 5;
            provider.OverridePythonExecutablePath = new FileInfo(@"C:\fishmongers\python");
            //call with accept all
            var ex = Assert.Throws<Exception>(()=>provider.Check(new AcceptAllCheckNotifier()));
            
            StringAssert.Contains(@"The specified OverridePythonExecutablePath:C:\fishmongers\python does not exist",ex.Message);

        }

    }
}
