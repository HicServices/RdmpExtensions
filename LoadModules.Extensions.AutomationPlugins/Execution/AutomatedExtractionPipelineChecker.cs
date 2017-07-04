using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data.Pipelines;
using NUnit.Framework.Constraints;
using ReusableLibraryCode.Checks;

namespace LoadModules.Extensions.AutomationPlugins.Execution
{
    public class AutomatedExtractionPipelineChecker:ICheckable
    {
        private readonly Pipeline _automateExtractionPipeline;

        public AutomatedExtractionPipelineChecker(Pipeline automateExtractionPipeline)
        {
            _automateExtractionPipeline = automateExtractionPipeline;
        }

        public void Check(ICheckNotifier notifier)
        {
            try
            {
                if (_automateExtractionPipeline == null)
                {
                    notifier.OnCheckPerformed(new CheckEventArgs("No Pipeline specified", CheckResult.Fail));
                    return;
                }
                
                if (_automateExtractionPipeline.PipelineComponents.Any(c => c.Class == typeof (SuccessfullyExtractedResultsDocumenter).FullName))
                    notifier.OnCheckPerformed(new CheckEventArgs("Found SuccessfullyExtractedResultsDocumenter plugin component",CheckResult.Success));
                else
                    notifier.OnCheckPerformed(new CheckEventArgs("Automated Extraction can only take place through Pipelines that include a "+typeof(SuccessfullyExtractedResultsDocumenter).Name+" plugin component", CheckResult.Fail));
            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Checking process failed", CheckResult.Fail, e));
            }
        }
    }
}
