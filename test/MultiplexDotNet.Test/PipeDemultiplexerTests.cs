using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Multiplex.Pipeline;
using Xunit;
using System.Linq;
using System.Threading;
using System.Diagnostics;

namespace Multiplex.Test
{
    public class PipeDemultiplexerTests
    {

        [Fact]
        public async Task SimpleBufferWrite()
        {
            var cancellationSource = new CancellationTokenSource();
            var token = cancellationSource.Token;

            var inputPipe = new Pipe();
            var outputPipe1 = new Pipe();
            var outputPipe2 = new Pipe();

            var outputSelector = new Mock<IOutputSelector>();
            outputSelector.Setup(s => s.GetPipeWriter(1)).Returns(outputPipe1.Writer);
            outputSelector.Setup(s => s.GetPipeWriter(2)).Returns(outputPipe2.Writer);

            var demultiplexer = new PipeDemultiplexer(inputPipe.Reader, outputSelector.Object);
            var writer = inputPipe.Writer;

            var tasks = new[]
                            { Task.Run(async () =>
                                    {
                                        var input1Buf = new byte[] { 1, 2, 3 };
                                        HeaderWriter.Write(writer, 1, input1Buf.Length);
                                        await PipelineUtilities.WriteSomeDataAsync(writer, input1Buf.AsMemory());

                                        var input2Buf = new byte[] { 4, 5, 6 };
                                        HeaderWriter.Write(writer, 2, input2Buf.Length);
                                        await PipelineUtilities.WriteSomeDataAsync(writer, input2Buf.AsMemory());

                                        writer.Complete();
                                    })
                            , demultiplexer.Run(token) };
            await Task.WhenAll(tasks);

            outputPipe1.Writer.Complete();
            outputPipe2.Writer.Complete();

            var buf1 = new byte[10];
            var result1 = await PipelineUtilities.ReadSomeDataAsync(outputPipe1.Reader, buf1);
            Assert.Equal(1, result1[0]);
            Assert.Equal(2, result1[1]);
            Assert.Equal(3, result1[2]);

            var buf2 = new byte[10];
            var result2 = await PipelineUtilities.ReadSomeDataAsync(outputPipe2.Reader, buf2);
            Assert.Equal(4, result2[0]);
            Assert.Equal(5, result2[1]);
            Assert.Equal(6, result2[2]);
        }

        [Fact]
        public async Task CanReadLargePayloads()
        {
            var cancellationSource = new CancellationTokenSource();
            var token = cancellationSource.Token;
            var size = 10000;

            var inputPipe = new Pipe();
            var outputPipe1 = new Pipe();

            var outputSelector = new Mock<IOutputSelector>();
            outputSelector.Setup(s => s.GetPipeWriter(1)).Returns(outputPipe1.Writer);

            var demultiplexer = new PipeDemultiplexer(inputPipe.Reader, outputSelector.Object);
            var writer = inputPipe.Writer;

            var tasks = new[]
                            { Task.Run(async () =>
                                    {
                                        var input1Buf = GetLargePayload(size, 2);
                                        HeaderWriter.Write(writer, 1, input1Buf.Length);
                                        await PipelineUtilities.WriteSomeDataAsync(writer, input1Buf.AsMemory());

                                        writer.Complete();
                                    })
                            , demultiplexer.Run(token) };
            await Task.WhenAll(tasks);

            outputPipe1.Writer.Complete();

            var readBuf = new byte[10000];
            var newBuf = await PipelineUtilities.ReadSomeDataAsync(outputPipe1.Reader, readBuf);
            
            for (int i = 0; i < size; i++) {

                if (2 != newBuf[i])
                    Trace.WriteLine(newBuf[i]);
                    
                Assert.Equal(2, newBuf[i]);
            }
                
        }

        private byte[] GetLargePayload(int size, byte value)
        {
            var buf = new byte[size];
            for (int i = 0; i < size; i++)
                buf[i] = value;

            return buf;
        }

        [Fact]
        public async Task WriteSomeDataReadSomeData()
        {
            var pipe = new Pipe();
            var buf = new byte[] { 1, 2, 3 };
            var mem = buf.AsMemory();
            await PipelineUtilities.WriteSomeDataAsync(pipe.Writer, mem);
            pipe.Writer.Complete();

            var readBuf = new byte[10];
            var newBuf = await PipelineUtilities.ReadSomeDataAsync(pipe.Reader, readBuf);
            pipe.Reader.Complete();

            Assert.Equal(buf[0], newBuf[0]);
            Assert.Equal(buf[1], newBuf[1]);
            Assert.Equal(buf[2], newBuf[2]);
        }

    }
}