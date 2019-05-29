using Rdmp.Core.Curation.Data;
using Rdmp.Core.Repositories;
using Rdmp.Core.Startup;
using System;
using System.Linq;
using System.Reflection;

namespace LoadModules.Extensions.AutomationPlugins.Data.Repository
{
    public class AutomateExtractionRepositoryFinder : PluginRepositoryFinder
    {
        public static int Timeout = 5;
        private Assembly _databaseAssembly;
        
        public AutomateExtractionRepositoryFinder(IRDMPPlatformRepositoryServiceLocator repositoryLocator) : base(repositoryLocator)
        {
            
        }

        public override PluginRepository GetRepositoryIfAny()
        {
            if (RepositoryLocator.CatalogueRepository == null || RepositoryLocator.DataExportRepository == null)
                return null;

            var compatibleServers = RepositoryLocator.CatalogueRepository.GetAllObjects<ExternalDatabaseServer>()
                .Where(e => e.CreatedByAssembly == _databaseAssembly.GetName().Name).ToArray();

            if (compatibleServers.Length > 1)
                throw new Exception("There are 2+ ExternalDatabaseServers of type '" + _databaseAssembly.GetName().Name + 
                                    "'.  This is not allowed, you must delete one.  The servers were called:" + 
                                    string.Join(",", compatibleServers.Select(s => s.ToString())));

            if (compatibleServers.Length == 0)
                return null;

            return new AutomateExtractionRepository(RepositoryLocator, compatibleServers[0]);
        }

        public override Type GetRepositoryType()
        {
            return typeof (AutomateExtractionRepository);
        }
    }
}
