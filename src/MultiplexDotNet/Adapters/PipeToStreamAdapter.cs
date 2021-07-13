using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Multiplex.Adapters
{
    public class PipeToStreamAdapter
    {
        private readonly Stream _outputStream;
        private readonly PipeReader _inputReader;

        public PipeToStreamAdapter(Stream outputStream, PipeReader inputReader)
        {
            _outputStream = outputStream;
            _inputReader = inputReader;
        }
        public async Task Run(CancellationToken token)
        {
            while (true)
            {
                try
                {
                    ReadResult readOp = await _inputReader.ReadAsync(token);
                    ReadOnlySequence<byte> buffer = readOp.Buffer;

                    if (buffer.IsEmpty && readOp.IsCompleted)
                        break; 
                        
                    foreach (var segment in buffer)
                            await _outputStream.WriteAsync(segment, token);

                    await _outputStream.FlushAsync(token);
                    _inputReader.AdvanceTo(buffer.End);

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
            }

            _inputReader.Complete();
        }
    }

}