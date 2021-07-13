using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Multiplex.Pipeline;
using Xunit;
using Moq;
using System;

namespace Multiplex.Test
{

    public class PipeMultiplexerTests
    {
        [Fact]
        public async Task SplitPayloads()
        {
            var cancellationSource = new CancellationTokenSource();
            var token = cancellationSource.Token;

            var outputPipe1 = new Pipe();
            var outputSelector = new Mock<IOutputSelector>();
            outputSelector.Setup(s => s.GetPipeWriter(It.IsAny<int>())).Returns(outputPipe1.Writer);

            var inputPipe1 = new Pipe();
            var multiplex1 = new PipeMultiplexer(inputPipe1.Reader, 1, 5, outputSelector.Object);
            var multiplexTask1 = multiplex1.Run(token);

            // write some data
            var input1Buf = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            await PipelineUtilities.WriteSomeDataAsync(inputPipe1.Writer, input1Buf.AsMemory());
            inputPipe1.Writer.Complete();

            await multiplexTask1;
            outputPipe1.Writer.Complete();

            var readBuf = new byte[100];
            var result1 = await PipelineUtilities.ReadSomeDataAsync(outputPipe1.Reader, readBuf);
            Assert.Equal(1, result1[0]);
            Assert.Equal(0, result1[1]);
            Assert.Equal(0, result1[2]);
            Assert.Equal(0, result1[3]);

            Assert.Equal(5, result1[4]);
            Assert.Equal(0, result1[5]);
            Assert.Equal(0, result1[6]);
            Assert.Equal(0, result1[7]);

            Assert.Equal(1, result1[8]);
            Assert.Equal(2, result1[9]);
            Assert.Equal(3, result1[10]);
            Assert.Equal(4, result1[11]);
            Assert.Equal(5, result1[12]);

            Assert.Equal(1, result1[13]);
            Assert.Equal(0, result1[14]);
            Assert.Equal(0, result1[15]);
            Assert.Equal(0, result1[16]);

            Assert.Equal(5, result1[17]);
            Assert.Equal(0, result1[18]);
            Assert.Equal(0, result1[19]);
            Assert.Equal(0, result1[20]);


            Assert.Equal(6, result1[21]);
            Assert.Equal(7, result1[22]);
            Assert.Equal(8, result1[23]);
            Assert.Equal(9, result1[24]);
            Assert.Equal(10, result1[25]);

        }

        [Fact]
        public async Task WritesMultipleStreams()
        {
            var cancellationSource = new CancellationTokenSource();
            var token = cancellationSource.Token;

            var outputPipe1 = new Pipe();
            var outputSelector = new Mock<IOutputSelector>();
            outputSelector.Setup(s => s.GetPipeWriter(It.IsAny<int>())).Returns(outputPipe1.Writer);

            var inputPipe1 = new Pipe();
            var multiplex1 = new PipeMultiplexer(inputPipe1.Reader, 1, outputSelector.Object);
            var multiplexTask1 = multiplex1.Run(token);

            var inputPipe2 = new Pipe();
            var multiplex2 = new PipeMultiplexer(inputPipe2.Reader, 2, outputSelector.Object);
            var multiplexTask2 = multiplex2.Run(token);


            // write some data
            var input1Buf = new byte[] { 1, 2, 3 };
            await PipelineUtilities.WriteSomeDataAsync(inputPipe1.Writer, input1Buf.AsMemory());
            inputPipe1.Writer.Complete();

            var input2Buf = new byte[] { 4, 5, 6 };
            await PipelineUtilities.WriteSomeDataAsync(inputPipe2.Writer, input2Buf.AsMemory());
            inputPipe2.Writer.Complete();

            await Task.WhenAll(multiplexTask1, multiplexTask2);
            outputPipe1.Writer.Complete();

            var readBuf = new byte[100];
            var result1 = await PipelineUtilities.ReadSomeDataAsync(outputPipe1.Reader, readBuf);
            Assert.Equal(1, result1[0]);
            Assert.Equal(0, result1[1]);
            Assert.Equal(0, result1[2]);
            Assert.Equal(0, result1[3]);

            Assert.Equal(3, result1[4]);
            Assert.Equal(0, result1[5]);
            Assert.Equal(0, result1[6]);
            Assert.Equal(0, result1[7]);

            Assert.Equal(1, result1[8]);
            Assert.Equal(2, result1[9]);
            Assert.Equal(3, result1[10]);

            Assert.Equal(2, result1[11]);
            Assert.Equal(0, result1[12]);
            Assert.Equal(0, result1[13]);
            Assert.Equal(0, result1[14]);

            Assert.Equal(3, result1[15]);
            Assert.Equal(0, result1[16]);
            Assert.Equal(0, result1[17]);
            Assert.Equal(0, result1[18]);

            Assert.Equal(4, result1[19]);
            Assert.Equal(5, result1[20]);
            Assert.Equal(6, result1[21]);

        }

    }
}