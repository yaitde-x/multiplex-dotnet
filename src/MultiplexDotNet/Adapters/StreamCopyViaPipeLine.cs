using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Multiplex.Adapters
{

    public class StreamCopyViaPipeLine
    {
        public async Task Copy(Stream input, Stream output)
        {
            var pipe = new Pipe();

            var writer = FillPipe(input, pipe.Writer);
            var reader = DrainPipe(output, pipe.Reader);
            await Task.WhenAll(reader, writer);
        }

        private async Task FillPipe(Stream inputStream, PipeWriter writer)
        {
            while (true)
            {
                try
                {
                    Memory<byte> memory = writer.GetMemory(TransportConstants.BufferSize);
                    int bytesRead = await inputStream.ReadAsync(memory);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    writer.Advance(bytesRead);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                    break;
                }

                // Make the data available to the PipeReader
                FlushResult result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    break;
                }
            }

            writer.Complete();
        }

        private async Task DrainPipe(Stream output, PipeReader reader)
        {

            while (true)
            {
                try
                {
                    ReadResult readOp = await reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = readOp.Buffer;

                    if (buffer.IsEmpty && readOp.IsCompleted)
                        break; // exit loop

                    foreach (var segment in buffer)
                        await output.WriteAsync(segment);

                    await output.FlushAsync();
                    reader.AdvanceTo(buffer.End);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
            }

            reader.Complete();
        }
    }
}