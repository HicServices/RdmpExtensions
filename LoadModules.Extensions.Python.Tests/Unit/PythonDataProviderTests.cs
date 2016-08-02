using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.Data.EntityNaming;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngine;
using DataLoadEngine.DatabaseManagement;
using DataLoadEngine.DatabaseManagement.EntityNaming;
using DataLoadEngine.DatabaseManagement.Operations;
using DataLoadEngine.Job;
using HIC.Logging;
using LoadModules.Extensions.Python.DataProvider;
using NUnit.Framework;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;
using Rhino.Mocks;
using Tests.Common;

namespace LoadModules.Extensions.Python.Tests.Unit
{
    [Category("Integration")]
    public class PythonDataProviderTests : ToConsoleDataLoadEventReciever
    {
        [Test]
        [ExpectedException(ExpectedMessage = "Version of Python required for script has not been selected")]
        public void PythonVersionNotSetYet()
        {

            PythonDataProvider provider = new PythonDataProvider();
            provider.Check(new ThrowImmediatelyCheckNotifier());
        }

        [Test]
        [TestCase(PythonVersion.Version2)]
        [TestCase(PythonVersion.Version3)]
        [ExpectedException(ExpectedMessage = "Python is not installed on the host")]
        public void PythonIsNotInstalled(PythonVersion version)
        {
            UninstallPythons();

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = version;
            
            provider.Check(new ThrowImmediatelyCheckNotifier());
        }
        [Test]
        [TestCase(PythonVersion.Version2)]
        [TestCase(PythonVersion.Version3)]
        public void PythonIsNotInstalled_InstallItSilently(PythonVersion version)
        {
            UninstallPythons();
            
            string MyPythonScript = @"print 'Hello World'";

            File.Delete("Myscript.py");
            File.WriteAllText("Myscript.py",MyPythonScript);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = version;
            provider.FullPathToPythonScriptToRun = "Myscript.py";

            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());

            //now call with throw immediately, previous accept should have installed python succesfully
            provider.Check(new ThrowImmediatelyCheckNotifier());
        }


        [Test]
        [ExpectedException(ExpectedMessage = "SyntaxError: Missing parentheses in call to 'print'", MatchType = MessageMatch.Contains)]
        public void PythonScript_Version3_DodgySyntax()
        {
            string MyPythonScript = @"print 'Hello World'";

            File.Delete("Myscript.py");
            File.WriteAllText("Myscript.py",MyPythonScript);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version3;
            provider.FullPathToPythonScriptToRun = "Myscript.py";
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 0;
        
            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());

            provider.Fetch(MockRepository.GenerateStub<IDataLoadJob>(), new GracefulCancellationToken());
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
            provider.Check(new ThrowImmediatelyCheckNotifier(){ThrowOnWarning = true});

            //new MockRepository().DynamicMock<IDataLoadJob>()
            provider.Fetch(MockRepository.GenerateStub<IDataLoadJob>(), new GracefulCancellationToken());
        }

        [Test]
        public void PythonScript_ValidScript()
        {
            string MyPythonScript = @"print (""Hello World"")";

            File.Delete("Myscript.py");
            File.WriteAllText("Myscript.py", MyPythonScript);

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version3;
            provider.FullPathToPythonScriptToRun = "Myscript.py";
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 0;

            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());

            //new MockRepository().DynamicMock<IDataLoadJob>()
            provider.Fetch(MockRepository.GenerateStub<IDataLoadJob>(), new GracefulCancellationToken());
        }

        [Test]
        [Ignore("Jenkins can't handle this for some reason")]
        [ExpectedException(ExpectedMessage = "Python command timed out (See inner exception for details)")]
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
            Assert.Equals(ProcessExitCode.Failure,result);
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
            provider.OverridePythonExecutablePath = new FileInfo(Path.Combine(provider.GetFullPythonInstallDirectory(),"python.exe"));
            provider.Version = PythonVersion.Version2;
            
            provider.Check(new ThrowImmediatelyCheckNotifier());
            //so we now know that version 3 is installed, and we have overriden the python path to the .exe explicitly and we are trying to launch with Version2 enum now
            provider.Fetch(MockRepository.GenerateStub<IDataLoadJob>(), new GracefulCancellationToken());
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
        [ExpectedException(ExpectedMessage = "can't open file 'ImANonExistentFile.py': [Errno 2] No such file or directory",MatchType=MessageMatch.Contains)]
        public void PythonScript_NonExistentFile()
        {

            PythonDataProvider provider = new PythonDataProvider();
            provider.Version = PythonVersion.Version2;
            provider.FullPathToPythonScriptToRun = "ImANonExistentFile.py";
            provider.MaximumNumberOfSecondsToLetScriptRunFor = 50;
            //call with accept all
            provider.Check(new AcceptAllCheckNotifier());

            provider.Fetch(MockRepository.GenerateStub<IDataLoadJob>(), new GracefulCancellationToken());
        }

        private void UninstallPythons()
        {
            //uninstall python 3
            try
            {
                ProcessStartInfo info = new ProcessStartInfo("msiexec.exe");
                info.Arguments = " /x{CCD588A7-8D55-49F1-A30C-47FAB40889ED} /qn";
                Process p = Process.Start(info);
                bool waitForExit = p.WaitForExit(60000);
                if (!waitForExit)
                    throw new Exception("Uninstall of Python 3 longer than 60s");

                Console.WriteLine("Uninstall of Python 3 exit code was " + p.ExitCode);
            }
            catch (Exception e)
            {
                Console.WriteLine("Uninstall of Python 3 failed with Exception " + e.Message + " maybe it wasn't installed in the first place");
            }

            //uninstall python 2
            try
            {
                ProcessStartInfo info = new ProcessStartInfo("msiexec.exe");
                info.Arguments = " /x{E2B51919-207A-43EB-AE78-733F9C6797C2} /qn";
                Process p = Process.Start(info);
                bool waitForExit = p.WaitForExit(60000);
                if (!waitForExit)
                    throw new Exception("Uninstall of Python 2 took longer than 60s");

                Console.WriteLine("Uninstall of Python 2 exit code was " + p.ExitCode);
            }
            catch (Exception e)
            {
                Console.WriteLine("Uninstall of Python 2 failed with Exception " + e.Message + " maybe it wasn't installed in the first place");
            }
        }


        public string Description { get; private set; }
        public IDataLoadInfo DataLoadInfo { get; private set; }
        public IHICProjectDirectory HICProjectDirectory { get; set; }
        public int JobID { get; set; }
        public ILoadMetadata LoadMetadata { get; private set; }
        public bool DisposeImmediately { get; private set; }
        public string ArchiveFilepath { get; private set; }
        public List<TableInfo> RegularTablesToLoad { get; private set; }
        public List<TableInfo> LookupTablesToLoad { get; private set; }
        public void StartLogging()
        {
            throw new NotImplementedException();
        }

        public void CloseLogging()
        {
            throw new NotImplementedException();
        }

        public void AddForDisposalAfterCompletion(IDisposeAfterDataLoad disposable)
        {
            throw new NotImplementedException();
        }

        public void LoadCompletedSoDispose(ExitCodeType exitCode)
        {
            throw new NotImplementedException();
        }

        public void StageCompletedSoDispose(ExitCodeType exitCode)
        {
            throw new NotImplementedException();
        }

        public void CreateTablesInStage(DatabaseCloner cloner, INameDatabasesAndTablesDuringLoads namingScheme,LoadBubble namingConvention)
        {
            throw new NotImplementedException();
        }
    }
}

