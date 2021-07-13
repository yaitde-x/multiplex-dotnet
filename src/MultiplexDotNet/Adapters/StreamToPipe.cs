using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Multiplex.Adapters
{
    public class StreamToPipeAdapter
    {
        private readonly Stream _inputStream;
        private readonly PipeWriter _outputWriter;

        public StreamToPipeAdapter(Stream inputStream, PipeWriter outputWriter)
        {
            _inputStream = inputStream;
            _outputWriter = outputWriter;
        }

        public async Task Run(CancellationToken token)
        {
            var buf = new byte[TransportConstants.BufferSize];
            while (!token.IsCancellationRequested)
            {
                try
                {

                    int bytesRead = await _inputStream.ReadAsync(buf, 0, TransportConstants.BufferSize, token);
                    if (bytesRead == 0)
                        break;

                    Memory<byte> memory = _outputWriter.GetMemory(TransportConstants.BufferSize);
                    var bufferSlice = memory.Slice(0);

                    buf.AsMemory().CopyTo(bufferSlice);
                    _outputWriter.Advance(bytesRead);

                    // Make the data available to the PipeReader
                    FlushResult result = await _outputWriter.FlushAsync(token);

                    if (result.IsCompleted)
                        break;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                    break;
                }
            }

            _outputWriter.Complete();
        }
    }
}