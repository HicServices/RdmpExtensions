using System;
using System.IO;
using System.Text.RegularExpressions;
using CatalogueLibrary.Data.Automation;
using FluentNHibernate.Conventions;
using LoadModules.Extensions.AutomationPlugins.Data;
using RDMPAutomationService;
using RDMPAutomationService.Interfaces;

namespace LoadModules.Extensions.AutomationPlugins.Execution.AutomationPipeline
{
    public class RoutineExtractionRun : IAutomateable
    {
        private AutomationServiceSlot _serviceSlot;
        private readonly AutomateExtraction _configurationToRun;

        public AutomationJob AutomationJob { get; private set; }

        public const string RoutineExtractionJobsPrefix = "RE:";
        public const string RoutineExtractionJobsNameRegex = "RE:([\\d]+)";

        public RoutineExtractionRun(AutomationServiceSlot serviceSlot, AutomateExtraction configurationToRun)
        {
            _serviceSlot = serviceSlot;
            _configurationToRun = configurationToRun;

            AutomationJob = _serviceSlot.AddNewJob(AutomationJobType.UserCustomPipeline, RoutineExtractionJobsPrefix + "placeholder");
        }

        public int GetScheduleIDIfAnyFromJobName(AutomationJob job)
        {
            var m = new Regex(RoutineExtractionJobsNameRegex).Match(job.Description);
            if (m.Success)
                return int.Parse(m.Groups[1].Value);

            return -1;
        }

        public OnGoingAutomationTask GetTask()
        {
            return new OnGoingAutomationTask(AutomationJob, this);
        }

        public void RunTask(OnGoingAutomationTask task)
        {
            try
            {
                task.Job.SetLastKnownStatus(AutomationJobStatus.Running);

                string path = @"C:\temp\" + DateTime.Now.ToString().Replace(":", "_").Replace("/","_") + ".txt";
                File.WriteAllText(path, "great scottt it works");
                
                //it worked!
                task.Job.SetLastKnownStatus(AutomationJobStatus.Finished);
                task.Job.DeleteInDatabase();
            }
            catch (Exception)
            {
                task.Job.SetLastKnownStatus(AutomationJobStatus.Crashed);
            }
        }
    }
}
