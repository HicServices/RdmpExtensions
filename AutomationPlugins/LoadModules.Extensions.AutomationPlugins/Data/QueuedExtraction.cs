﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.Pipelines;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.Interfaces.Data.DataTables;
using LoadModules.Extensions.AutomationPlugins.Data.Repository;
using MapsDirectlyToDatabaseTable;

namespace LoadModules.Extensions.AutomationPlugins.Data
{
    public class QueuedExtraction : DatabaseEntity
    {
        #region Database Properties

        private int _extractionConfiguration_ID;
        private int _pipeline_ID;
        private DateTime _dueDate;
        private string _requester;
        private DateTime _requestDate;
        

        public int ExtractionConfiguration_ID
        {
            get { return _extractionConfiguration_ID; }
            set { SetField(ref _extractionConfiguration_ID, value); }
        }
        public int Pipeline_ID
        {
            get { return _pipeline_ID; }
            set { SetField(ref _pipeline_ID, value); }
        }
        public DateTime DueDate
        {
            get { return _dueDate; }
            set { SetField(ref _dueDate, value); }
        }
        public string Requester
        {
            get { return _requester; }
            set { SetField(ref _requester, value); }
        }
        public DateTime RequestDate
        {
            get { return _requestDate; }
            set { SetField(ref _requestDate, value); }
        }
        #endregion

        #region Relationships
        [NoMappingToDatabase]
        public IExtractionConfiguration ExtractionConfiguration
        {
            get
            {
                return ((AutomateExtractionRepository)Repository).DataExportRepository.GetObjectByID<ExtractionConfiguration>(ExtractionConfiguration_ID);
            }
        }

        [NoMappingToDatabase]
        public Pipeline Pipeline
        {
            get
            {
                return ((AutomateExtractionRepository)Repository).CatalogueRepository.GetObjectByID<Pipeline>(Pipeline_ID);
            }
        }
        #endregion

        public QueuedExtraction(AutomateExtractionRepository repository, ExtractionConfiguration configuration, IPipeline extractionPipeline, DateTime dueDate)
        {
            repository.InsertAndHydrate(this, new Dictionary<string, object>()
            {
                {"ExtractionConfiguration_ID",configuration.ID},
                {"Pipeline_ID",extractionPipeline.ID},
                {"DueDate",dueDate},
                {"Requester",Environment.UserName}
            });

            if (ID == 0 || Repository != repository)
                throw new ArgumentException("Repository failed to properly hydrate this class");
        }
        public QueuedExtraction(AutomateExtractionRepository repository, DbDataReader r)
            : base(repository, r)
        {
            ExtractionConfiguration_ID = Convert.ToInt32(r["ExtractionConfiguration_ID"]);
            Pipeline_ID = Convert.ToInt32(r["Pipeline_ID"]);
            DueDate = Convert.ToDateTime(r["DueDate"]);
            Requester = r["Requester"].ToString();
            RequestDate = Convert.ToDateTime(r["RequestDate"]);
        }

        public bool IsDue()
        {
            return DateTime.Now > DueDate;
        }
    }
}