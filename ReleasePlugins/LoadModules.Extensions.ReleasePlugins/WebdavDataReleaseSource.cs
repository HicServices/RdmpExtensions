using System;
using CatalogueLibrary.DataFlowPipeline;
using RDMPAutomationService;
using ReusableLibraryCode.Progress;

namespace LoadModules.Extensions.ReleasePlugins
{
    public class WebdavDataReleaseAutomationSource : IDataFlowSource<OnGoingAutomationTask>
    {
        public OnGoingAutomationTask GetChunk(IDataLoadEventListener listener, GracefulCancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose(IDataLoadEventListener listener, Exception pipelineFailureExceptionIfAny)
        {
            throw new NotImplementedException();
        }

        public void Abort(IDataLoadEventListener listener)
        {
            throw new NotImplementedException();
        }

        public OnGoingAutomationTask TryGetPreview()
        {
            throw new NotImplementedException();
        }
    }
}