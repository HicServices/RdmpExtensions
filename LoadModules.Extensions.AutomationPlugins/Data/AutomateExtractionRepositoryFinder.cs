﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using MapsDirectlyToDatabaseTable;
using RDMPStartup;
using ReusableLibraryCode.DataAccess;
using ReusableUIComponents;

namespace LoadModules.Extensions.AutomationPlugins.Data
{
    public class AutomateExtractionRepositoryFinder : PluginRepositoryFinder
    {
        private readonly IRDMPPlatformRepositoryServiceLocator _repositoryLocator;
        private Assembly _databaseAssembly;

        public AutomateExtractionRepositoryFinder(IRDMPPlatformRepositoryServiceLocator repositoryLocator) : base(repositoryLocator)
        {
            _repositoryLocator = repositoryLocator;

            _databaseAssembly = typeof(Database.Class1).Assembly;
        }

        public override IRepository GetRepositoryIfAny()
        {
            if (_repositoryLocator.CatalogueRepository == null || _repositoryLocator.DataExportRepository == null)
                return null;

            var compatibleServers = _repositoryLocator.CatalogueRepository.GetAllObjects<ExternalDatabaseServer>()
                .Where(e => e.CreatedByAssembly == _databaseAssembly.GetName().Name).ToArray();

            if (compatibleServers.Length > 1)
                WideMessageBox.Show("There are 2+ ExternalDatabaseServers of type '" + _databaseAssembly.GetName().Name + "'.  This is not allowed, you must delete one.  The servers were called:" + string.Join(",", compatibleServers.Select(s => s.ToString())));

            if (compatibleServers.Length == 0)
                return null;

            var server = DataAccessPortal.GetInstance().ExpectServer(compatibleServers[0], DataAccessContext.InternalDataProcessing);

            Exception ex;
            if (!server.RespondsWithinTime(5, out ex))
                ExceptionViewer.Show(ex);

            return new AutomateExtractionRepository(_repositoryLocator,server.Builder);
        }

        public override Type GetRepositoryType()
        {
            return typeof (AutomateExtractionRepository);
        }
    }
}