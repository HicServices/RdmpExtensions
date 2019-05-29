using System;
using LoadModules.Extensions.Python.DataProvider;
using NUnit.Framework;
using ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.Python.Tests.Unit
{
    [Category("Integration")]
    public class PythonNotInstalledTests 
    {

        [Test]
        [TestCase(PythonVersion.Version2)]
        [TestCase(PythonVersion.Version3)]
        public void PythonIsNotInstalled(PythonVersion version)
        {
            InconclusiveIfPytonIsInstalled(version);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = version;
            
            var ex = Assert.Throws<Exception>(()=>provider.Check(new ThrowImmediatelyCheckNotifier()));

            Assert.IsTrue(ex.Message.Contains("Failed to launch"));
        }

        private void InconclusiveIfPytonIsInstalled(PythonVersion version)
        {
            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = version;
            string result = "";
            try
            {
                //These tests run if python is not installed so we expect this to throw
                Assert.Throws<Exception>(() => result = provider.GetPythonVersion());
            }
            catch (Exception e)
            {
                //if it didnt' throw then it means python IS installed and we cannot run these tests so Inconclusive
                Console.WriteLine("Could not run tests because Python is already installed on the system, these unit tests only fire if there is no Python.  Python version string is:" + result);
                Assert.Inconclusive();
            }
        }
    }
}

