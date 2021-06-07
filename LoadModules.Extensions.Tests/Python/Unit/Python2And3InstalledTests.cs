﻿using System;
using System.IO;
using LoadModules.Extensions.Python.DataProvider;
using NUnit.Framework;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad.Engine.Job;
using ReusableLibraryCode.Checks;
using Rhino.Mocks;

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
            var ex = Assert.Throws<Exception>(()=>provider.Fetch(MockRepository.GenerateStub<IDataLoadJob>(), new GracefulCancellationToken()));
            StringAssert.Contains(@"which is incompatible with the desired version 2.7.1",ex.Message);
        }
    }
}