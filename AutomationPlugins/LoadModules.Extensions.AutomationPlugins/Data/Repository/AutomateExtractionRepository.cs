using System;
using System.Data.Common;
using CatalogueLibrary.Repositories;
using CatalogueLibrary.Repositories.Construction;
using MapsDirectlyToDatabaseTable;

namespace LoadModules.Extensions.AutomationPlugins.Data.Repository
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

        protected override bool IsCompatibleType(Type type)
        {
            return type == typeof(AutomateExtraction)
                || type == typeof(AutomateExtractionSchedule)
                || type == typeof(QueuedExtraction)
                || type == typeof(SuccessfullyExtractedResults);
        }
    }
}
