﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CatalogueLibrary.Data.Automation;
using RDMPAutomationService;
using RDMPAutomationService.Interfaces;

namespace LoadModules.Extensions.AutomationPlugins.DataExport
{
    class RoutineExtractionRun : IAutomateable
    {
        private AutomationServiceSlot _serviceSlot;
        public AutomationJob AutomationJob { get; private set; }
        public const string RoutineExtractionJobsPrefix = "RE:";
        
        public RoutineExtractionRun(AutomationServiceSlot serviceSlot)
        {
            _serviceSlot = serviceSlot;
            
            AutomationJob = new AutomationJob(_serviceSlot.Repository, _serviceSlot, AutomationJobType.UserCustomPipeline, RoutineExtractionJobsPrefix + "placeholder");
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

                File.WriteAllText(@"C:\temp\" + DateTime.Now + ".txt", "great scottt it works");
            }
            catch (Exception)
            {
                task.Job.SetLastKnownStatus(AutomationJobStatus.Crashed);
            }
        }
    }
}
