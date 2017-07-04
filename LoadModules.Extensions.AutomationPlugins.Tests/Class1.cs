using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CatalogueLibrary.Data.Automation;
using CatalogueLibrary.DataFlowPipeline;
using DataLoadEngineTests.Integration;
using LoadModules.Extensions.AutomationPlugins.Execution;
using NUnit.Framework;
using Tests.Common;

namespace LoadModules.Extensions.AutomationPlugins.Tests
{
    
    public class AutomatedExtractionSourceTests : DatabaseTests
    {
        [Test]
        public void OneJobAtOnce()
        {
            var slot = new AutomationServiceSlot(CatalogueRepository);
            var source = new AutomatedExtractionSource();
            source.PreInitialize(slot, new ThrowImmediatelyDataLoadJob());

            Assert.NotNull(source.GetChunk(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken()));
            Assert.IsNull(source.GetChunk(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken()));
        }

        [Test]
        public void CheckRunTask()
        {
            var slot = new AutomationServiceSlot(CatalogueRepository);
            var source = new AutomatedExtractionSource();
            source.PreInitialize(slot, new ThrowImmediatelyDataLoadJob());

            var chunk = source.GetChunk(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken());
            var job = chunk.Job;
            chunk.Task.RunSynchronously();
            Assert.AreEqual(AutomationJobStatus.Finished,job.LastKnownStatus);
        }
    }
}
