using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using DataExportLibrary.Data.DataTables;
using MapsDirectlyToDatabaseTable;

namespace LoadModules.Extensions.AutomationPlugins.Data
{
    public class ExecutionSchedule : DatabaseEntity, INamed
    {
        #region Database Properties

        private TimeScale _executionTimescale;
        private string _userRequestingRefresh;
        private DateTime? _userRequestingRefreshDate;
        private string _ticket;
        private string _name;
        private string _comment;
        private bool _disabled;
        private int _project_ID;
        #endregion

        public TimeScale ExecutionTimescale
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
        public ExecutionSchedule(IRepository repository,Project project)
        {
            repository.InsertAndHydrate(this, new Dictionary<string, object>()
            {
                {"Project_ID",project.ID},
                {"Name","New Schedule"+Guid.NewGuid()},
                {"ExecutionTimescale",TimeScale.Never}
            });

            if (ID == 0 || Repository != repository)
                throw new ArgumentException("Repository failed to properly hydrate this class");
        }
        public ExecutionSchedule(IRepository repository, DbDataReader r)
            : base(repository, r)
        {
            ExecutionTimescale = (TimeScale) Enum.Parse(typeof(TimeScale),r["ExecutionTimescale"].ToString());
            UserRequestingRefresh = r["UserRequestingRefresh"] as string;
            UserRequestingRefreshDate = ObjectToNullableDateTime(r["UserRequestingRefreshDate"]);
            Ticket = r["Ticket"] as string;
            Name = r["Name"].ToString();
            Comment = r["Comment"] as string;
            Disabled = Convert.ToBoolean(r["Disabled"]);
            Project_ID = Convert.ToInt32(r["Project_ID"]);
        }
    }

    public enum TimeScale
    {
        Never = 0,
        Daily,
        Weekly,
        BiWeekly,
        Monthly,
        Yearly
    }
}
