using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Multiplex.Pipeline
{
    public static class PipelineUtilities
    {
        public static async Task WriteSomeDataAsync(PipeWriter writer, ReadOnlyMemory<byte> bytesToWrite)
        {
            await writer.WriteAsync(bytesToWrite);
            await writer.FlushAsync();
        }

        public static async Task<byte[]> ReadSomeDataAsync(PipeReader reader, byte[] buf)
        {
            var mem = buf.AsMemory();
            var bufIndex = 0;

            while (true)
            {
                ReadResult read = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = read.Buffer;

                if (!buffer.IsEmpty)
                {

                    foreach (var segment in buffer)
                    {
                        for (int j = 0; j < segment.Span.Length; j++)
                        {
                            buf[bufIndex + j] = segment.Span[j];
                        }

                        bufIndex += segment.Span.Length;
                    }

                    reader.AdvanceTo(buffer.End);
                }

                if (read.IsCompleted)
                    break;
            }
            return buf;
        }
    }
}