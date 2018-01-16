using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngine;
using DataLoadEngine.DataProvider;
using DataLoadEngine.Job;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.DatabaseHelpers.Discovery;
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
            
            var toMemory = new ToMemoryDataLoadEventListener(true);

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

        public void Initialize(IHICProjectDirectory hicProjectDirectory, DiscoveredDatabase dbInfo)
        {
            
        }

        public ExitCodeType Fetch(IDataLoadJob job, GracefulCancellationToken cancellationToken)
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
                return ExitCodeType.Error;
            }

            job.OnNotify(this, new NotifyEventArgs(exitCode == 0 ? ProgressEventType.Information : ProgressEventType.Error, "Python script terminated with exit code " + exitCode));

            return exitCode == 0 ? ExitCodeType.Success : ExitCodeType.Error;
        }

        private int ExecuteProcess(IDataLoadEventListener listener, ProcessStartInfo processStartInfo, int maximumNumberOfSecondsToLetScriptRunFor)
        {
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;

            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;

            Process p = null;

            bool allErrorDataConsumed = false;
            bool allOutputDataConsumed = false;

            try
            {
                p =  new Process();
                p.StartInfo = processStartInfo;
                p.OutputDataReceived += (s, e) => allOutputDataConsumed = OutputDataReceived(s, e, listener,false);
                p.ErrorDataReceived += (s, e) => allErrorDataConsumed = OutputDataReceived(s, e, listener,true);
                
                p.Start();
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

                    throw new TimeoutException("Process command " + processStartInfo.FileName + " with arguments " + processStartInfo.Arguments + " did not complete after  " + maximumNumberOfSecondsToLetScriptRunFor + " seconds " + (killed ? "(After timeout we killed the process successfully)" : "(We also failed to kill the process after the timeout expired)"));
                }
            }

            while (!allErrorDataConsumed || !allOutputDataConsumed)
            {
                Task.Delay(100);

                if(TimeoutExpired(startTime))
                    throw new TimeoutException("Timeout expired while waiting for all output streams from the Python process to finish being read");
            }

            if (outputDataReceivedExceptions.Any())
                if (outputDataReceivedExceptions.Count == 1)
                    throw outputDataReceivedExceptions[0];
                else
                    throw new AggregateException(outputDataReceivedExceptions);

            return p.ExitCode;
        }

        List<Exception> outputDataReceivedExceptions = new List<Exception>();

        private bool OutputDataReceived(object sender, DataReceivedEventArgs e, IDataLoadEventListener listener,bool isErrorStream)
        {
            if(e.Data == null)
                return true;
            
            lock (this)
            {
                try
                {
                    //it has expired the standard out
                    listener.OnNotify(this, new NotifyEventArgs(isErrorStream?ProgressEventType.Warning : ProgressEventType.Information, e.Data));
                }
                catch (Exception ex)
                {
                    //the notify handler is crashing... lets stop tyring to read data from this async handler.  Also add the exception to the list because we don't want it throwing out of this lamda
                    outputDataReceivedExceptions.Add(ex);
                    return true;
                }
            }
             
            return false;
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
