using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Multiplex.Test {
    public class End2EndTest {
        [Fact]
        public async Task RunParallelTest() {
            var stopWatch = Stopwatch.StartNew();
            var inputs = await TextSeedDataGenerator.GenerateSeedData();
            var source = new CancellationTokenSource();
            var outputs = await End2End.ProcessTextDataParallelTest(inputs, source.Token);
            var results = await BufferValidation.ValidateTextData(inputs, outputs);

            var runtime = stopWatch.ElapsedMilliseconds;
            Trace.WriteLine($"runtime: {runtime}");
            
            Assert.Single(results);
            Assert.Equal("inputs and outputs match!", results[0]);         
        }
    }
}