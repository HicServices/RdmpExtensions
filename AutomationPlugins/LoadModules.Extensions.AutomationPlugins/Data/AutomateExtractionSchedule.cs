using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Pipelines;
using CatalogueLibrary.Ticketing;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.Interfaces.Data.DataTables;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using MapsDirectlyToDatabaseTable;
using RDMPStartup;
using ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.AutomationPlugins.Data
{
    public class AutomateExtractionSchedule : DatabaseEntity, INamed
    {
        #region Database Properties

        private AutomationTimeScale _executionTimescale;
        private string _userRequestingRefresh;
        private DateTime? _userRequestingRefreshDate;
        private string _ticket;
        private string _name;
        private string _comment;
        private bool _disabled;
        private int _project_ID;
        private int? _pipeline_ID;
        private int? _releasePipelineId;
        private TimeSpan _executionTimeOfDay;

        private AutomateExtractionRepository _repository;

        public AutomationTimeScale ExecutionTimescale
        {
            get { return _executionTimescale; }
            set { SetField(ref _executionTimescale, value); }
        }
        public string UserRequestingRefresh
        {
            get { return _userRequestingRefresh; }
            set { SetField(ref _userRequestingRefresh, value); }
        }
        public DateTime? UserRequestingRefreshDate
        {
            get { return _userRequestingRefreshDate; }
            set { SetField(ref _userRequestingRefreshDate, value); }
        }
        public string Ticket
        {
            get { return _ticket; }
            set { SetField(ref _ticket, value); }
        }
        public string Name
        {
            get { return _name; }
            set { SetField(ref _name, value); }
        }
        public string Comment
        {
            get { return _comment; }
            set { SetField(ref _comment, value); }
        }
        public bool Disabled
        {
            get { return _disabled; }
            set { SetField(ref _disabled, value); }
        }
        public int Project_ID
        {
            get { return _project_ID; }
            set { SetField(ref _project_ID, value); }
        }
        public int? Pipeline_ID
        {
            get { return _pipeline_ID; }
            set { SetField(ref _pipeline_ID, value); }
        }

        public TimeSpan ExecutionTimeOfDay
        {
            get { return _executionTimeOfDay; }
            set { SetField(ref _executionTimeOfDay , value); }
        }

        public int? ReleasePipeline_ID
        {
            get { return _releasePipelineId; }
            set { SetField(ref _releasePipelineId , value); }
        }

        #endregion

        #region Database Relationships

        [NoMappingToDatabase]
        public Pipeline Pipeline { get
        {
            return Pipeline_ID != null ? _repository.CatalogueRepository.GetObjectByID<Pipeline>(Pipeline_ID.Value) : null;
        } }

        [NoMappingToDatabase]
        public IPipeline ReleasePipeline
        {
            get
            {
                return ReleasePipeline_ID != null ? _repository.CatalogueRepository.GetObjectByID<Pipeline>(ReleasePipeline_ID.Value) : null;
            }
        }

        [NoMappingToDatabase]
        public Project Project
        {
            get
        {
            return  _repository.DataExportRepository.GetObjectByID<Project>(Project_ID);
        } }

        [NoMappingToDatabase]
        public AutomateExtraction[] AutomateExtractions { get
        {
            return _repository.GetAllObjectsWithParent<AutomateExtraction>(this);
        } }


        #endregion

        public AutomateExtractionSchedule(PluginRepository repository, Project project)
        {
            _repository = (AutomateExtractionRepository) repository;
            repository.InsertAndHydrate(this, new Dictionary<string, object>()
            {
                {"Project_ID",project.ID},
                {"Name","New Schedule"+Guid.NewGuid()},
                {"ExecutionTimescale",AutomationTimeScale.Never},
                {"ExecutionTimeOfDay","12:00:00"}
            });

            if (ID == 0 || Repository != repository)
                throw new ArgumentException("Repository failed to properly hydrate this class");
        }
        public AutomateExtractionSchedule(PluginRepository repository, DbDataReader r)
            : base(repository, r)
        {
            _repository = (AutomateExtractionRepository) repository;

            ExecutionTimescale = (AutomationTimeScale) Enum.Parse(typeof(AutomationTimeScale),r["ExecutionTimescale"].ToString());
            UserRequestingRefresh = r["UserRequestingRefresh"] as string;
            UserRequestingRefreshDate = ObjectToNullableDateTime(r["UserRequestingRefreshDate"]);
            Ticket = r["Ticket"] as string;
            Name = r["Name"].ToString();
            Comment = r["Comment"] as string;
            Disabled = Convert.ToBoolean(r["Disabled"]);
            Project_ID = Convert.ToInt32(r["Project_ID"]);
            Pipeline_ID = ObjectToNullableInt(r["Pipeline_ID"]);

            try
            {
                ExecutionTimeOfDay = TimeSpan.Parse(r["ExecutionTimeOfDay"].ToString());
            }
            catch (Exception)
            {
                ExecutionTimeOfDay = new TimeSpan(12,00,00);
            }
            ReleasePipeline_ID = ObjectToNullableInt(r["ReleasePipeline_ID"]);

        }

        public override string ToString()
        {
            return Name;
        }

        public void CheckTicketing(ICheckNotifier notifier)
        {
            if (string.IsNullOrWhiteSpace(Ticket))
            {
                notifier.OnCheckPerformed(new CheckEventArgs("No Ticket specified, governance will not be checked",CheckResult.Warning));
                return;
            }
            
            var ticketingSystem = _repository.CatalogueRepository.GetTicketingSystem();


            var factory = new TicketingSystemFactory(_repository.CatalogueRepository);
            var config = factory.CreateIfExists(ticketingSystem);

            if(config == null)
            {
                notifier.OnCheckPerformed( new CheckEventArgs("There is a Ticket specified but no ITicketingSystem configured in the Catalogue Repository",CheckResult.Warning));
                return;
            }

            Exception exc;
            string reason;

            var evaluation = config.GetDataReleaseabilityOfTicket(Ticket, null, null, out reason, out exc);

            if(evaluation == TicketingReleaseabilityEvaluation.Releaseable)
                return;

            notifier.OnCheckPerformed(
                new CheckEventArgs(
                    "Ticket '" + Ticket + "' is " + evaluation + ".  Reason given was:" + Environment.NewLine + reason,
                    CheckResult.Fail, exc));
        }

        public IExtractionConfiguration[] GetImportableExtractionConfigurations()
        {
            var idsAlreadyPartOfSchedule = AutomateExtractions.Select(e => e.ExtractionConfiguration_ID);

            var available = Project.ExtractionConfigurations;

            return available.Where(e => !idsAlreadyPartOfSchedule.Contains(e.ID)).ToArray();
        }
    }

    public enum AutomationTimeScale
    {
        Never = 0,
        Daily,
        Weekly,
        BiWeekly,
        Monthly,
        Yearly
    }
}
