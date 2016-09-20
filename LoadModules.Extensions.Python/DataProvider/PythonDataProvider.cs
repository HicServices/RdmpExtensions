﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        private IDataLoadEventListener _listener;

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
                    notifier.OnCheckPerformed(new CheckEventArgs("Python is not installed on the host", CheckResult.Fail,e));
                else
                    notifier.OnCheckPerformed(new CheckEventArgs(e.Message, CheckResult.Fail, e));
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
            
            var toMemory = new ToMemoryDataLoadEventReceiver(true);

            int result = ExecuteProcess(toMemory, info, 600);
            
            if (result != 0)
                return null;
            
            var msg = toMemory.EventsReceivedBySender[this].SingleOrDefault();

            if (msg != null)
                return msg.Message;

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

        public ProcessExitCode Fetch(IDataLoadJob job, GracefulCancellationToken cancellationToken)
        {
            ProcessStartInfo processStartInfo = GetPythonCommand(FullPathToPythonScriptToRun);
            
            int exitCode;
            try
            {
                exitCode = ExecuteProcess(job, processStartInfo,MaximumNumberOfSecondsToLetScriptRunFor);
            }
            catch (TimeoutException e)
            {
                job.OnNotify(this,new NotifyEventArgs(ProgressEventType.Error, "Python command timed out (See inner exception for details)",e));
                return ProcessExitCode.Failure;
            }

            job.OnNotify(this, new NotifyEventArgs(exitCode == 0 ? ProgressEventType.Information : ProgressEventType.Error, "Python script terminated with exit code " + exitCode));

            return exitCode == 0 ? ProcessExitCode.Success : ProcessExitCode.Failure;
        }

        private int ExecuteProcess(IDataLoadEventListener listener, ProcessStartInfo processStartInfo, int maximumNumberOfSecondsToLetScriptRunFor)
        {
            _listener = listener;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;

            processStartInfo.UseShellExecute = false;
            
            Process p = null;
            
            try
            {
                p = Process.Start(processStartInfo);
                p.OutputDataReceived += OutputDataReceived;
                p.ErrorDataReceived += OutputDataReceived;
                
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();

            }
            catch (Exception e)
            {
                throw new Exception("Failed to launch:" + Environment.NewLine + processStartInfo.FileName +Environment.NewLine +  " with Arguments:" + processStartInfo.Arguments,e);
            }
            
            // To avoid deadlocks, always read the output stream first and then wait.
            DateTime startTime = DateTime.Now;

            
            while (!p.WaitForExit(100))//while process has not exited
            {
                if (TimeoutExpired(startTime))//if timeout expired
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

                    throw new TimeoutException("Process command " + processStartInfo.FileName + " with arguments " + processStartInfo.Arguments + " did not complete after  " + maximumNumberOfSecondsToLetScriptRunFor + " seconds " + (killed ? "(After timeout we killed the process succesfully)" : "(We also failed to kill the process after the timeout expired)"));
                }
            }
            
            return p.ExitCode;
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if(e.Data == null)
                return;
            
            lock (this)
            {
                //it has expired the standard out
                _listener.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information, e.Data));
            }
        }
        private bool TimeoutExpired(DateTime startTime)
        {
            if (MaximumNumberOfSecondsToLetScriptRunFor == 0)
                return false;

            return DateTime.Now - startTime > new TimeSpan(0, 0, 0, MaximumNumberOfSecondsToLetScriptRunFor);
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
                    return "python35";
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
