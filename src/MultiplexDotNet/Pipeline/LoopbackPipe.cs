using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Multiplex.Adapters;

namespace Multiplex.Pipeline
{
    public class LoopbackPipe
    {
        public Pipe InputPipe { get; } = new Pipe();
        public Pipe OutputPipe { get; } = new Pipe();

        public async Task Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    ReadResult readOp = await InputPipe.Reader.ReadAsync(token);
                    ReadOnlySequence<byte> buffer = readOp.Buffer;

                    if (buffer.IsEmpty && readOp.IsCompleted)
                        break; // exit loop

                    foreach (var segment in buffer)
                        await OutputPipe.Writer.WriteAsync(segment);

                    await OutputPipe.Writer.FlushAsync(token);
                    InputPipe.Reader.AdvanceTo(buffer.End);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
            }

            InputPipe.Reader.Complete();
            OutputPipe.Writer.Complete();
        }
        
    }
}