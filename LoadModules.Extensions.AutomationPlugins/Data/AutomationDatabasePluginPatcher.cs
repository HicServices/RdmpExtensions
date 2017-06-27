﻿using System.Linq;
using System.Reflection;
using CatalogueLibrary.Data;
using CatalogueLibrary.ExternalDatabaseServerPatching;
using CatalogueLibrary.Repositories;

namespace LoadModules.Extensions.AutomationPlugins.Data
{
    public class AutomationDatabasePluginPatcher:IPluginPatcher
    {
        private CatalogueRepository _repository;

        public AutomationDatabasePluginPatcher(CatalogueRepository repository)
        {
            _repository = repository;
        }

        public IExternalDatabaseServer[] FindDatabases(out Assembly hostAssembly, out Assembly dbAssembly)
        {
            hostAssembly = GetType().Assembly;
            dbAssembly = typeof (LoadModules.Extensions.AutomationPlugins.Database.Class1).Assembly;

            var dbAssemblyName = dbAssembly.GetName().Name;

            return 
                _repository.GetAllObjects<ExternalDatabaseServer>()
                .Where(s => s.CreatedByAssembly != null && s.CreatedByAssembly.Equals(dbAssemblyName))
                .ToArray();
        }
    }
}
