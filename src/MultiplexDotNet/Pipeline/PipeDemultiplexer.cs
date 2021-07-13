using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Multiplex.Adapters;

namespace Multiplex.Pipeline
{
    public class PipeDemultiplexer
    {

        private readonly PipeReader _inputReader;
        private readonly IOutputSelector _outputSelector;

        public PipeDemultiplexer(PipeReader inputReader, IOutputSelector outputSelector)
        {
            _inputReader = inputReader;
            _outputSelector = outputSelector;
        }

        public async Task Run(CancellationToken token)
        {
            var state = 0;
            var multiplexId = 0;
            var payloadLen = 0L;
            var headerBuffer = new MemoryStream(TransportConstants.HeaderSize);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    Trace.WriteLine("demux: reading from concentrator...");
                    ReadResult readOp = await _inputReader.ReadAsync(token);
                    ReadOnlySequence<byte> buffer = readOp.Buffer;

                    if (buffer.IsEmpty && readOp.IsCompleted)
                        break; // exit loop

                    if (state == 0 && buffer.Length >= TransportConstants.HeaderSize)
                    {
                        // just read the 
                        var midSlice = buffer.Slice(0, 8);
                        foreach (var segment in midSlice)
                            await headerBuffer.WriteAsync(segment);

                        var buf = headerBuffer.GetBuffer();
                        multiplexId = BitConverter.ToInt32(buf);
                        payloadLen = BitConverter.ToInt32(new byte[] { buf[4], buf[5], buf[6], buf[7] });
                        state = 1;

                        Trace.WriteLine($"demux: wrote header, muxId={multiplexId}, frameLength={payloadLen}");
                        _inputReader.AdvanceTo(buffer.GetPosition(TransportConstants.HeaderSize));
                    }
                    else if (state == 1 && buffer.Length >= payloadLen)
                    {
                        var outputPipe = _outputSelector.GetPipeWriter(multiplexId);
                        var payload = buffer.Slice(0, Math.Min(buffer.Length, payloadLen));

                        foreach (var segment in payload)
                            await outputPipe.WriteAsync(segment, token);

                        await outputPipe.FlushAsync(token);
                        payloadLen -= payload.Length;
                        _inputReader.AdvanceTo(payload.End);

                        if (payloadLen == 0)
                        {
                            state = 0;
                            headerBuffer.Seek(0, SeekOrigin.Begin);
                        }
                        Trace.WriteLine($"demux: wrote payload bytes, muxId={multiplexId}, frameLength={payloadLen}");
                    }

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
            }

            _inputReader.Complete();
            _outputSelector.CompleteAll();
        }

    }
}