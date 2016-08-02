using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngine;
using DataLoadEngine.DataProvider;
using DataLoadEngine.Job;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.Python.DataProvider
{
    public enum PythonVersion
    {
        NotSet,
        Version2,
        Version3
    }

    public class PythonDataProvider:IPluginDataProvider
    {
        [DemandsInitialization("The Python script to run")]
        public string FullPathToPythonScriptToRun { get; set; }

        [DemandsInitialization("The maximum number of seconds to allow the python script to run for before declaring it a failure, 0 for indefinetly")]
        public int MaximumNumberOfSecondsToLetScriptRunFor { get; set; }

        [DemandsInitialization("Python version required to run your script")] 
        public PythonVersion Version { get; set; }

        [DemandsInitialization("Override Python Executable Path")]
        public FileInfo OverridePythonExecutablePath { get; set; }

        public bool DisposeImmediately { get; set; }
        public void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
        {
            
        }

        public void Check(ICheckNotifier notifier)
        {

            if (Version == PythonVersion.NotSet)
            {
                notifier.OnCheckPerformed(
                    new CheckEventArgs("Version of Python required for script has not been selected", CheckResult.Fail));
                return;
            }

            //make sure Python is installed
            try
            {
                string version = GetPythonVersion();

                if (version.StartsWith(GetExpectedPythonVersion()))
                    notifier.OnCheckPerformed(
                        new CheckEventArgs(
                            "Found Expected Python version " + version + " on the host machine at directory " +
                            GetFullPythonInstallDirectory(), CheckResult.Success));
                else if (version.StartsWith(GetCompatiblePythonVersion()))
                    notifier.OnCheckPerformed(
                        new CheckEventArgs(
                            "Found Compatible Python version " + version + " on the host machine at directory " +
                            GetFullPythonInstallDirectory(), CheckResult.Success));
                else
                {
                    notifier.OnCheckPerformed(
                            new CheckEventArgs(
                                "Python version on the host machine is " + version +
                                " which is incompatible with the desired version " + GetExpectedPythonVersion(),
                                CheckResult.Fail));
                }
            }
            catch (FileNotFoundException e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs(e.Message, CheckResult.Fail, e));
            }
            catch (Exception e)
            {
                //python is not installed
                if (e.Message.Equals("The system cannot find the file specified"))
                {
                    //install it?
                    bool attemptToInstallIt = notifier.OnCheckPerformed(new CheckEventArgs("Python is not installed on the host",CheckResult.Fail,e,"Attempt to install Python?"));

                    if (attemptToInstallIt)
                        InstallPython(notifier);//yes please install it for me
                }
                else
                    notifier.OnCheckPerformed(new CheckEventArgs(e.Message,CheckResult.Fail, e));
            }

            if (FullPathToPythonScriptToRun.Contains(" ") && !FullPathToPythonScriptToRun.Contains("\""))
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "FullPathToPythonScriptToRun contains spaces but is not wrapped by quotes which will likely fail when we assemble the python execute command",
                        CheckResult.Fail));

            if (!File.Exists(FullPathToPythonScriptToRun.Trim('\"', '\'')))
                notifier.OnCheckPerformed(
                    new CheckEventArgs("File " + FullPathToPythonScriptToRun + " does not exist (FullPathToPythonScriptToRun)",
                        CheckResult.Warning));
        }

        public string GetPythonVersion()
        {
            var info = GetPythonCommand(@"-c ""import sys; print(sys.version)""");
            
            string output;
            string error;

            int result = ExecuteProcess(info, out output, out error, 600);
            
            if (result != 0)
                return null;

            if (!string.IsNullOrWhiteSpace(output))
                return output;

            if (!string.IsNullOrWhiteSpace(error))
                return error;
            
            throw new Exception("Call to " + info.Arguments + " did not return any value but exited with code " + result);
        }

        private ProcessStartInfo GetPythonCommand(string command)
        {
            string exeFullPath;

            if (OverridePythonExecutablePath == null)
            {
                //e.g. c:\python34
                string instalDir = GetFullPythonInstallDirectory();
                exeFullPath = Path.Combine(instalDir, "python");
            }
            else
            {
                if (!OverridePythonExecutablePath.Exists)
                    throw new FileNotFoundException("The specified OverridePythonExecutablePath:" +
                                                    OverridePythonExecutablePath +
                                                    " does not exist");
                else
                    if(OverridePythonExecutablePath.Name != "python.exe")
                        throw new FileNotFoundException("The specified OverridePythonExecutablePath:" +
                                                    OverridePythonExecutablePath +
                                                    " file is not called python.exe... what is going on here?");

                exeFullPath = OverridePythonExecutablePath.FullName;
            }

            ProcessStartInfo info = new ProcessStartInfo(exeFullPath);
            info.Arguments = command;

            return info;
        }


        private void InstallPython(ICheckNotifier notifier)
        {
            //Create the MSI file in the same directory as the dll
            string pythonExecutableName = GetPythonMSIFileName();
            Assembly assembly = typeof (PythonDataProvider).Assembly;
            string currentDllDirectory;
            try
            {
                currentDllDirectory = Path.GetDirectoryName(assembly.Location);    
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "Could not figure out where the currently executing assembly " + assembly +
                        " is stored on the hard disk", CheckResult.Fail, e));
                return;
            }

            string pythonInstallerLocation = Path.Combine(currentDllDirectory, pythonExecutableName);

            notifier.OnCheckPerformed(new CheckEventArgs("About to create Python MSI file in " + currentDllDirectory,CheckResult.Success));
            try
            {

                //e.g. LoadModules.Generic.python-3.4.3.msi
                string manifestName = typeof (PythonDataProvider).Namespace + "." + pythonExecutableName;

                Stream MSIFile = assembly.GetManifestResourceStream(manifestName);

                if(MSIFile == null)
                    notifier.OnCheckPerformed(new CheckEventArgs("Assembly " + assembly + " did not contain a manifest resource called '" + manifestName + "', the only mainfest resources found in the dll were:" + Environment.NewLine + string.Join(Environment.NewLine, assembly.GetManifestResourceNames()), CheckResult.Fail));
            
                //get the bytes
                byte[] buffer = new byte[MSIFile.Length];
                MSIFile.Read(buffer, 0, buffer.Length);

                File.WriteAllBytes(pythonInstallerLocation,buffer);
                
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "Succesfully created Python MSI in " + currentDllDirectory + " (it was " + buffer.Length +
                        " bytes)", CheckResult.Success));

                MSIFile.Close();
                MSIFile.Dispose();
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(
                    new CheckEventArgs("Failed to create Python MSI executable in " + currentDllDirectory,
                        CheckResult.Fail, e));
                return;
            }


            string driveToInstallTo = GetFullPythonInstallDirectory();

            notifier.OnCheckPerformed(
                new CheckEventArgs(
                    "About to attempt to install Python to  " + driveToInstallTo,
                    CheckResult.Success));

            try
            {
                
                
                Process p = new Process();
                p.StartInfo.FileName = "msiexec";
                p.StartInfo.Arguments = "/i " + pythonInstallerLocation + " TARGETDIR=" + driveToInstallTo + " /qn";

                notifier.OnCheckPerformed(new CheckEventArgs("About to run msiexec with arguments " + p.StartInfo.Arguments, CheckResult.Success));
                p.Start();

                bool waitForExit = p.WaitForExit(600000);

                if (!waitForExit)
                    notifier.OnCheckPerformed(new CheckEventArgs("Python installer failed to complete after 600 seconds",
                        CheckResult.Fail));
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Python install MSI failed to install correctly",
                    CheckResult.Fail, e));
            }
        }

        public ProcessExitCode Fetch(IDataLoadJob job, GracefulCancellationToken cancellationToken)
        {
            ProcessStartInfo processStartInfo = GetPythonCommand(FullPathToPythonScriptToRun);

            string output;
            string error;

            int exitCode;
            try
            {
                exitCode = ExecuteProcess(processStartInfo, out output, out error,
                    MaximumNumberOfSecondsToLetScriptRunFor);
            }
            catch (TimeoutException e)
            {
                
                job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Error, "Python command timed out (See inner exception for details)",e));
                return ProcessExitCode.Failure;
            }

            if (!string.IsNullOrWhiteSpace(output))
                job.OnNotify(this, new NotifyEventArgs(exitCode == 0 ? ProgressEventType.Information : ProgressEventType.Error, output));

            if (!string.IsNullOrWhiteSpace(error))
                job.OnNotify(this, new NotifyEventArgs(exitCode == 0 ? ProgressEventType.Information : ProgressEventType.Error, error));

            //did it succeed?
            if (exitCode != 0)
            {
                job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Error, "Python command " + FullPathToPythonScriptToRun + " returned exit code " + exitCode +" (expected exit code 0)"));
                return ProcessExitCode.Failure;
            }
            return exitCode == 0 ? ProcessExitCode.Success : ProcessExitCode.Failure;
        }

        private static int ExecuteProcess(ProcessStartInfo processStartInfo,out string output, out string error, int maximumNumberOfSecondsToLetScriptRunFor)
        {

            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;

            processStartInfo.UseShellExecute = false;

            Process p = Process.Start(processStartInfo);
         
            
            // To avoid deadlocks, always read the output stream first and then wait.
            var outputAwait = p.StandardOutput.ReadToEndAsync();
            var errorAwait = p.StandardError.ReadToEndAsync();

            if (maximumNumberOfSecondsToLetScriptRunFor == 0)
                p.WaitForExit();
            else
            {

                bool ended = p.WaitForExit(maximumNumberOfSecondsToLetScriptRunFor*1000);
                if (!ended)
                {
                    bool killed = false;
                    try
                    {
                        p.Kill();
                        killed = true;
                    }
                    catch (Exception)
                    {
                        killed = false;
                    }

                    throw new TimeoutException("Process command " + processStartInfo.FileName + " with arguments " + processStartInfo.Arguments + " did not complete after  " +
                                               maximumNumberOfSecondsToLetScriptRunFor + " seconds " +(killed ? "(After timeout we killed the process succesfully)":"(We also failed to kill the process after the timeout expired)"));
                }
            }

            output = outputAwait.Result;
            error = errorAwait.Result;

            return p.ExitCode;
        }


        public string GetFullPythonInstallDirectory()
        {
            return Path.Combine(Path.GetPathRoot(typeof(PythonDataProvider).Assembly.Location), GetPythonFolderName());
        }

        private string GetPythonFolderName()
        {
            switch (Version)
            {
                case PythonVersion.NotSet:
                    throw new Exception("Python version not set yet");
                case PythonVersion.Version2:
                    return "python27";
                case PythonVersion.Version3:
                    return "python34";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string GetPythonMSIFileName()
        {
            switch (Version)
            {
                case PythonVersion.NotSet:
                    throw new Exception("Python version not set yet");
                case PythonVersion.Version2:
                    return "python-2.7.10.msi";
                case PythonVersion.Version3:
                    return "python-3.4.3.msi";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string GetExpectedPythonVersion()
        {
            switch (Version)
            {
                case PythonVersion.NotSet:
                    throw new Exception("Python version not set yet");
                case PythonVersion.Version2:
                    return "2.7.1";
                case PythonVersion.Version3:
                    return "3.4.3";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private string GetCompatiblePythonVersion()
        {
            switch (Version)
            {
                case PythonVersion.NotSet:
                    throw new Exception("Python version not set yet");
                case PythonVersion.Version2:
                    return "2";
                case PythonVersion.Version3:
                    return "3";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public string GetDescription()
        {
            throw new NotImplementedException();
        }

        public IDataProvider Clone()
        {
            throw new NotImplementedException();
        }

        public bool Validate(IHICProjectDirectory destination)
        {
            return true;
        }
    }
}
