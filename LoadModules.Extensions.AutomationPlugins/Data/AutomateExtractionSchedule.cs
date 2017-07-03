using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Pipelines;
using DataExportLibrary.Data.DataTables;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode.Checks;
using ReusableUIComponents;
using Ticketing;

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
        #endregion

        #region Database Relationships

        [NoMappingToDatabase]
        public Pipeline Pipeline { get
        {
            return Pipeline_ID != null ? _repository.CatalogueRepository.GetObjectByID<Pipeline>(Pipeline_ID.Value) : null;
        } }

        #endregion

        public AutomateExtractionSchedule(AutomateExtractionRepository repository, Project project)
        {
            repository.InsertAndHydrate(this, new Dictionary<string, object>()
            {
                {"Project_ID",project.ID},
                {"Name","New Schedule"+Guid.NewGuid()},
                {"ExecutionTimescale",AutomationTimeScale.Never}
            });

            if (ID == 0 || Repository != repository)
                throw new ArgumentException("Repository failed to properly hydrate this class");
        }
        public AutomateExtractionSchedule(AutomateExtractionRepository repository, DbDataReader r)
            : base(repository, r)
        {
            _repository = (AutomateExtractionRepository)repository;

            ExecutionTimescale = (AutomationTimeScale) Enum.Parse(typeof(AutomationTimeScale),r["ExecutionTimescale"].ToString());
            UserRequestingRefresh = r["UserRequestingRefresh"] as string;
            UserRequestingRefreshDate = ObjectToNullableDateTime(r["UserRequestingRefreshDate"]);
            Ticket = r["Ticket"] as string;
            Name = r["Name"].ToString();
            Comment = r["Comment"] as string;
            Disabled = Convert.ToBoolean(r["Disabled"]);
            Project_ID = Convert.ToInt32(r["Project_ID"]);
            Pipeline_ID = ObjectToNullableInt(r["Pipeline_ID"]);
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
