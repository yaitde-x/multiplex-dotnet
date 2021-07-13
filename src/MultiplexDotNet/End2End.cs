using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Multiplex.Pipeline;
using Multiplex.Adapters;

namespace Multiplex
{
    public class End2End
    {
        public static async Task<List<(string, Stream)>> ProcessTextDataSequentialTest(List<(string, Stream)> inputs, CancellationToken token)
        {
            var outputs = new List<(string, Stream)>();
            var transport = new InMemoryInputAdapter();

            var receiver = new InMemoryReceiver<Buffer>((bufferId) =>
            {
                return new MemoryStream();
            }, (bufferId, outputBuffer) =>
            {
                var output = (bufferId, outputBuffer);
                outputBuffer.Seek(0, SeekOrigin.Begin);
                outputs.Add(output);
            });

            var buffer = new MultiplexAdapter<Multiplex.Buffer>(b =>
            {
                receiver.Receive(b);
            });

            foreach (var item in inputs)
            {
                using (var reader = new StreamReader(item.Item2, Encoding.UTF8, false, 4096, true))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        var buf = new Multiplex.Buffer() { bufferId = item.Item1, buffer = Encoding.UTF8.GetBytes(line) };
                        buffer.Send(buf);
                    }
                }
            }

            await buffer.Stop();
            await receiver.Stop();

            return outputs;
        }

        public static async Task<List<(string, Stream)>> ProcessTextDataParallelTest(List<(string, Stream)> inputs, CancellationToken token)
        {
            var outputs = new List<(string, Stream)>();

            var receiver = new InMemoryReceiver<Buffer>((bufferId) =>
            {
                return new MemoryStream();
            }, (bufferId, outputBuffer) =>
            {
                var output = (bufferId, outputBuffer);
                outputBuffer.Seek(0, SeekOrigin.Begin);
                outputs.Add(output);
            });

            var multiplexBuffer = new MultiplexAdapter<Multiplex.Buffer>(b =>
            {
                receiver.Receive(b);
            });

            var tasks = new List<Task>();

            foreach (var item in inputs)
            {
                tasks.Add(Task.Run(async () =>
                {
                    //await transport.Injest(item.Item1, item.Item2);
                    using (var reader = new StreamReader(item.Item2, Encoding.UTF8, false, 4096, true))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync();
                            var buf = new Multiplex.Buffer() { bufferId = item.Item1, buffer = Encoding.UTF8.GetBytes(line) };
                            multiplexBuffer.Send(buf);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
            await multiplexBuffer.Stop();
            await receiver.Stop();

            return outputs;
        }

        public static async Task<List<(string, Stream)>> ProcessDataWithTransport(List<(string, Stream)> inputs, CancellationToken token)
        {
            var outputs = new List<(string, Stream)>();
            var transport = new InMemoryInputAdapter();
            var multiplexId = 0;
            var tasks = new List<Task>();

            foreach (var item in inputs)
            {
                multiplexId++;
                var outputStream = new MemoryStream();
                var output = (item.Item1, outputStream);
                outputs.Add(output);

                var pipe = new Pipe();
                var writer = new StreamToPipeAdapter(item.Item2, pipe.Writer);
                var reader = new PipeToStreamAdapter(outputStream, pipe.Reader);

                tasks.Add(writer.Run(token));
                tasks.Add(reader.Run(token));
            }

            await Task.WhenAll(tasks);

            return outputs;
        }

        public static async Task<List<(string, Stream)>> ProcessDataWithTransport2(List<(string, Stream)> inputs, CancellationToken token)
        {

            var outputs = new List<(string, Stream)>();
            var tasks = new List<Task>();

            foreach (var item in inputs)
            {
                var outputStream = new MemoryStream();
                var output = (item.Item1, outputStream);
                outputs.Add(output);

                var copier = new StreamCopyViaPipeLine();
                tasks.Add(copier.Copy(item.Item2, outputStream));
            }

            await Task.WhenAll(tasks);

            return outputs;
        }

        public static async Task<List<(string, Stream)>> ProcessDataWithStreamCopy(List<(string, Stream)> inputs, CancellationToken token)
        {

            var outputs = new List<(string, Stream)>();
            var tasks = new List<Task>();

            foreach (var item in inputs)
            {
                var outputStream = new MemoryStream();
                var output = (item.Item1, outputStream);
                outputs.Add(output);

                var copier = new StreamCopy();
                tasks.Add(copier.Copy(item.Item2, outputStream));
            }

            await Task.WhenAll(tasks);

            return outputs;
        }

        public static async Task<List<(string, Stream)>> LoopbackTest(List<(string, Stream)> inputs, CancellationToken token)
        {

            var outputs = new List<(string, Stream)>();
            var tasks = new List<Task>();

            foreach (var item in inputs)
            {
                var outputStream = new MemoryStream();
                var output = (item.Item1, outputStream);
                outputs.Add(output);

                var loopback = new LoopbackPipe();
                var inputPump = new StreamToPipeAdapter(item.Item2, loopback.InputPipe.Writer);
                var outputPump = new PipeToStreamAdapter(outputStream, loopback.OutputPipe.Reader);
                tasks.Add(inputPump.Run(token));
                tasks.Add(outputPump.Run(token));
                tasks.Add(loopback.Run(token));
            }

            await Task.WhenAll(tasks);

            return outputs;
        }

        public static async Task<List<(string, Stream)>> PipeMultiplexerTest(List<(string, Stream)> inputs, CancellationToken token)
        {

            var outputs = new List<(string, Stream)>();
            var tasks = new List<Task>();

            var senderReceiverLink = new Pipe();
            var pipeSelector = new Concentrator(senderReceiverLink);
            var outputSelector = new TestOutputPipeSelector();
            var multiplexId = 0;
            var demultiplexer = new PipeDemultiplexer(senderReceiverLink.Reader, outputSelector);

            foreach (var item in inputs)
            {
                multiplexId++;

                var outputStream = new MemoryStream();
                var output = (item.Item1, outputStream);
                outputs.Add(output);

                //var loopback = new LoopbackPipe();
                var multiplexPipe = new Pipe();

                var inputPump = new StreamToPipeAdapter(item.Item2, multiplexPipe.Writer);
                var multiplexer = new PipeMultiplexer(multiplexPipe.Reader, multiplexId, pipeSelector);

                var outputLink = new Pipe();
                outputSelector.AddOutput(multiplexId, outputLink);

                var outputPump = new PipeToStreamAdapter(outputStream, outputLink.Reader);
                //var demultiplexer = new PipeDemultiplexer(senderReceiverLink.Reader, outputSelector);

                tasks.Add(multiplexer.Run(token));
                tasks.Add(inputPump.Run(token));
                tasks.Add(outputPump.Run(token));
                //tasks.Add(loopback.Run(token));
            }

            tasks.Add(demultiplexer.Run(token));

            await Task.WhenAll(tasks);

            return outputs;
        }
    }
}