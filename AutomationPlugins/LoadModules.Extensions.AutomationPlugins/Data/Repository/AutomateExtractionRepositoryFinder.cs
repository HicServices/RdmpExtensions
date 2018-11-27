using System;
using System.Linq;
using System.Reflection;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using RDMPStartup;

namespace LoadModules.Extensions.AutomationPlugins.Data.Repository
{
    public class AutomateExtractionRepositoryFinder : PluginRepositoryFinder
    {
        public static int Timeout = 5;
        private readonly IRDMPPlatformRepositoryServiceLocator _repositoryLocator;
        private Assembly _databaseAssembly;
        
        public AutomateExtractionRepositoryFinder(IRDMPPlatformRepositoryServiceLocator repositoryLocator) : base(repositoryLocator)
        {
            _repositoryLocator = repositoryLocator;

            _databaseAssembly = typeof(Database.Class1).Assembly;
        }

        public override PluginRepository GetRepositoryIfAny()
        {
            if (_repositoryLocator.CatalogueRepository == null || _repositoryLocator.DataExportRepository == null)
                return null;

            var compatibleServers = _repositoryLocator.CatalogueRepository.GetAllObjects<ExternalDatabaseServer>()
                .Where(e => e.CreatedByAssembly == _databaseAssembly.GetName().Name).ToArray();

            if (compatibleServers.Length > 1)
                throw new Exception("There are 2+ ExternalDatabaseServers of type '" + _databaseAssembly.GetName().Name + 
                                    "'.  This is not allowed, you must delete one.  The servers were called:" + 
                                    string.Join(",", compatibleServers.Select(s => s.ToString())));

            if (compatibleServers.Length == 0)
                return null;

            return new AutomateExtractionRepository(_repositoryLocator, compatibleServers[0]);
        }

        public override Type GetRepositoryType()
        {
            return typeof (AutomateExtractionRepository);
        }
    }
}
