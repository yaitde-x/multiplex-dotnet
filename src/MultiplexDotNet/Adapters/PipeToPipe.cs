using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Multiplex.Adapters
{
    public class PipeToPipeAdapter {
        private readonly PipeReader _incomingPipe;
        private readonly PipeWriter _outgoingPipe;

        public PipeToPipeAdapter(PipeReader incomingPipe, PipeWriter outgoingPipe) {
            this._incomingPipe = incomingPipe;
            this._outgoingPipe = outgoingPipe;
        }

        public async Task Run(CancellationToken token)
        {

            var state = 0;
            var mid = 0;
            var payloadLen = 0L;
            var headerBuffer = new MemoryStream(TransportConstants.HeaderSize);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    ReadResult readOp = await _incomingPipe.ReadAsync(token);
                    ReadOnlySequence<byte> buffer = readOp.Buffer;

                    if (buffer.IsEmpty && readOp.IsCompleted)
                        break; // exit loop

                    if (state == 0 && buffer.Length >= TransportConstants.HeaderSize)
                    {
                        var midSlice = buffer.Slice(0, 8);
                        foreach (var segment in midSlice)
                            await headerBuffer.WriteAsync(segment, token);

                        var buf = headerBuffer.GetBuffer();
                        mid = BitConverter.ToInt32(buf);
                        payloadLen = BitConverter.ToInt32(new byte[] { buf[4], buf[5], buf[6], buf[7] });
                        state = 1;
                        //payloadBuffer.Seek(0, SeekOrigin.Begin);
                        _incomingPipe.AdvanceTo(buffer.GetPosition(TransportConstants.HeaderSize));
                    }
                    else if (state == 1 && buffer.Length >= payloadLen)
                    {
                        var payload = buffer.Slice(0, Math.Min(buffer.Length, payloadLen));
                        foreach (var segment in payload)
                            await _outgoingPipe.WriteAsync(segment, token);

                        await _outgoingPipe.FlushAsync(token);
                        payloadLen -= payload.Length;
                        _incomingPipe.AdvanceTo(payload.End);

                        if (payloadLen == 0)
                        {
                            state = 0;
                            headerBuffer.Seek(0, SeekOrigin.Begin);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
            }

            _incomingPipe.Complete();
        }
    }

}