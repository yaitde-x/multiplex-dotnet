using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Multiplex.Adapters;
using Multiplex.Pipeline;
using Xunit;

namespace Multiplex.Test {
    public class StreamToPipeAdapterTests {

        [Fact]
        public async Task WritesStreamContentsToPipe() {
            var cancellationSource = new CancellationTokenSource();
            var token = cancellationSource.Token;
            
            var pipe = new Pipe();
            var inputStream = new MemoryStream();
            var testObject = new StreamToPipeAdapter(inputStream, pipe.Writer);

            inputStream.WriteByte(1);
            inputStream.WriteByte(2);
            inputStream.WriteByte(3);
            await inputStream.FlushAsync();
            inputStream.Seek(0, SeekOrigin.Begin);

            await testObject.Run(token);

            var buf = await PipelineExtensions.ReadSomeDataAsMemoryAsync(pipe.Reader);
            pipe.Reader.Complete();

            Assert.Equal(1, buf.Span[0]);
            Assert.Equal(2, buf.Span[1]);
            Assert.Equal(3, buf.Span[2]);
        }
    }
}