using CatalogueLibrary.Data;

namespace LoadModules.Extensions.ReleasePlugins.Automation
{
    public class WebdavAutomationAudit : DatabaseEntity
    {
        public string FileHref { get; set; }
        public FileResult FileResult { get; set; }
        public string Message { get; set; }
    }
}