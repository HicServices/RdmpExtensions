using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using MapsDirectlyToDatabaseTable;

namespace LoadModules.Extensions.AutomationPlugins.Data
{
    public class ExecutionSchedule : DatabaseEntity, INamed
    {
        #region Database Properties

        private int? _everyXDays;
        private string _userRequestingRefresh;
        private DateTime? _userRequestingRefreshDate;
        private string _ticket;
        private int _permissionWindow_ID;
        private string _name;
        #endregion

        public int? EveryXDays
        {
            get { return _everyXDays; }
            set { SetField(ref _everyXDays, value); }
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
        public int PermissionWindow_ID
        {
            get { return _permissionWindow_ID; }
            set { SetField(ref _permissionWindow_ID, value); }
        }
        public string Name
        {
            get { return _name; }
            set { SetField(ref _name, value); }
        }
        public ExecutionSchedule(IRepository repository,string name)
        {
            name = name ?? "New Schedule" + Guid.NewGuid();
            repository.InsertAndHydrate(this, new Dictionary<string, object>()
            {
                {"Name",name}
            });

            if (ID == 0 || Repository != repository)
                throw new ArgumentException("Repository failed to properly hydrate this class");
        }
        public ExecutionSchedule(IRepository repository, DbDataReader r)
            : base(repository, r)
        {
            EveryXDays = ObjectToNullableInt(r["EveryXDays"]);
            UserRequestingRefresh = r["UserRequestingRefresh"] as string;
            UserRequestingRefreshDate = ObjectToNullableDateTime(r["UserRequestingRefreshDate"]);
            Ticket = r["Ticket"] as string;
            PermissionWindow_ID = Convert.ToInt32(r["PermissionWindow_ID"]);
            Name = r["Name"].ToString();
        }
    }
}
