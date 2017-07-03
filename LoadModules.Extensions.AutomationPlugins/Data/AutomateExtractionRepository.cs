using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Repositories;
using CatalogueLibrary.Repositories.Construction;
using DataExportLibrary.Repositories;
using MapsDirectlyToDatabaseTable;
using RDMPStartup;

namespace LoadModules.Extensions.AutomationPlugins.Data
{
    public class AutomateExtractionRepository : TableRepository
    {
        public CatalogueRepository CatalogueRepository { get; private set; }
        public IDataExportRepository DataExportRepository { get; private set; }

        readonly ObjectConstructor _constructor = new ObjectConstructor();

        public AutomateExtractionRepository(IRDMPPlatformRepositoryServiceLocator repositoryLocator, DbConnectionStringBuilder builder):base(null,builder)
        {
            CatalogueRepository = repositoryLocator.CatalogueRepository;
            DataExportRepository = repositoryLocator.DataExportRepository;
        }

        protected override IMapsDirectlyToDatabaseTable ConstructEntity(Type t, DbDataReader reader)
        {
            return _constructor.ConstructIMapsDirectlyToDatabaseObject(t, this, reader);
        }
    }
}
