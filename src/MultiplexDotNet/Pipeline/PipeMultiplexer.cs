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

    public class PipeMultiplexer
    {
        private readonly PipeReader _inputReader;
        private readonly IOutputSelector _outputSelector;
        private readonly int _multiplexId;
        private readonly int _frameSize;
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public PipeMultiplexer(PipeReader inputPipe, int multiplexId, IOutputSelector pipeSelector)
            : this(inputPipe, multiplexId, TransportConstants.BufferSize, pipeSelector)
        {

        }

        public PipeMultiplexer(PipeReader inputReader, int multiplexId, int frameSize, IOutputSelector outputSelector)
        {
            _inputReader = inputReader;
            _outputSelector = outputSelector;
            _multiplexId = multiplexId;
            _frameSize = frameSize;
        }

        public async Task Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {

                    var readOp = await _inputReader.ReadAsync(token);
                    var buffer = readOp.Buffer;

                    if (buffer.IsEmpty && readOp.IsCompleted)
                        break;

                    try
                    {
                        await semaphoreSlim.WaitAsync();

                        var outputWriter = _outputSelector.GetPipeWriter(_multiplexId);
                        Memory<byte> memory = outputWriter.GetMemory(TransportConstants.HeaderSize);

                        var bufSize = Math.Min(buffer.Length, _frameSize);
                        var multiplexIdBytes = BitConverter.GetBytes(_multiplexId);
                        var frameLengthBytes = BitConverter.GetBytes((int)bufSize);

                        multiplexIdBytes.AsMemory().CopyTo(memory.Slice(0, 4));
                        frameLengthBytes.AsMemory().CopyTo(memory.Slice(4, 4));

                        outputWriter.Advance(TransportConstants.HeaderSize);

                        var payloadSlice = buffer.Slice(0, bufSize);

                        foreach (var segment in payloadSlice)
                            await outputWriter.WriteAsync(segment, token);

                        //outputPipe.Advance((int)bufSize);
                        _inputReader.AdvanceTo(payloadSlice.End);

                        // Make the data available to the PipeReader
                        FlushResult result = await outputWriter.FlushAsync(token);

                        if (result.IsCompleted || result.IsCanceled)
                            break;
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                    break;
                }

            }

            _inputReader.Complete();
        }
    }
}