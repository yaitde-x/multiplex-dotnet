using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Multiplex.Pipeline {
    public static class PipelineExtensions {
        public static async Task<ReadOnlyMemory<byte>> ReadSomeDataAsMemoryAsync(this PipeReader reader)
        {
            var buf = new byte[10];
            var mem = buf.AsMemory();

            while (true)
            {
                ReadResult read = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = read.Buffer;

                if (buffer.IsEmpty && read.IsCompleted)
                    break;

                foreach (var segment in buffer)
                {
                    for (int j = 0; j < segment.Span.Length; j++)
                    {
                        buf[j] = segment.Span[j];
                    }
                }

                reader.AdvanceTo(buffer.End);
            }
            return buf;
        }
    }
}