﻿using System;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using RDMPStartup;

namespace LoadModules.Extensions.AutomationPlugins.Data.Repository
{
    public class AutomateExtractionRepository : PluginRepository
    {
        public CatalogueRepository CatalogueRepository { get; private set; }
        public IDataExportRepository DataExportRepository { get; private set; }

        public AutomateExtractionRepository(IRDMPPlatformRepositoryServiceLocator repositoryLocator, ExternalDatabaseServer server):base(server,null)
        {
            CatalogueRepository = repositoryLocator.CatalogueRepository;
            DataExportRepository = repositoryLocator.DataExportRepository;
        }
        
        protected override bool IsCompatibleType(Type type)
        {
            return typeof(DatabaseEntity).IsAssignableFrom(type);
        }
    }
}
