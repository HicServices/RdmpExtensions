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
    public class Python2And3InstalledTests
    {
        [SetUp]
        public void IsPython2ANDPython3Installed()
        {
            new Python2InstalledTests().IsPython2Installed();
            new Python3InstalledTests().IsPython3Installed();
        }
        [Test]
        [ExpectedException(ExpectedMessage = @"which is incompatible with the desired version 2.7.1", MatchType = MessageMatch.Contains)]
        public void PythonScript_OverrideExecutablePath_VersionMismatch()
        {
            string MyPythonScript = @"s = print('==>')";
            File.Delete("Myscript.py");
            File.WriteAllText("Myscript.py", MyPythonScript);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version3;
            provider.FullPathToPythonScriptToRun = "Myscript.py";
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 500;
            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());// version 3 should now be installed

            //version 3 executable path is explicit override for executing commands
            provider.OverridePythonExecutablePath = new FileInfo(Path.Combine(provider.GetFullPythonInstallDirectory(), "python.exe"));
            provider.Version = PythonVersion.Version2;

            provider.Check(new ThrowImmediatelyCheckNotifier());
            //so we now know that version 3 is installed, and we have overriden the python path to the .exe explicitly and we are trying to launch with Version2 enum now
            provider.Fetch(MockRepository.GenerateStub<IDataLoadJob>(), new GracefulCancellationToken());
        }
    }
}
