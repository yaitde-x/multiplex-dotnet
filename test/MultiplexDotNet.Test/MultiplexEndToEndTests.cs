using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Multiplex.Pipeline;
using Multiplex.Adapters;
using Xunit;

namespace Multiplex.Test
{
    public class MultiplexEndToEndTests
    {
        [Fact]
        public async Task SingleInput()
        {
            var inputs = await TextSeedDataGenerator.GenerateSeedData(1);
            var results = await RunTest(inputs);
            Assert.Single(results);
        }

        [Fact]
        public async Task TwoInputs()
        {
            var inputs = await TextSeedDataGenerator.GenerateSeedData(2);
            var results = await RunTest(inputs);
            Assert.Single(results);
        }

        public async Task<List<string>> RunTest(List<(string, Stream)> inputs)
        {
            var outputs = new List<(string, Stream)>();
            var inputTasks = new List<Task>();
            var outputTasks = new List<Task>();
            var token = new CancellationToken();
            var outputSelector = new TestOutputPipeSelector();
            var multiplexId = 0;

            var multiplexedPipe = new Pipe();
            var concentrator = new Concentrator(multiplexedPipe);

            var demultiplex = new PipeDemultiplexer(multiplexedPipe.Reader, outputSelector);
            outputTasks.Add(demultiplex.Run(token));

            foreach (var input in inputs)
            {
                multiplexId++;

                var inputLink = new Pipe();
                var inputPump = new StreamToPipeAdapter(input.Item2, inputLink.Writer);

                var outputLink = new Pipe();
                var outputStream = new MemoryStream();
                var outputPump = new PipeToStreamAdapter(outputStream, outputLink.Reader);

                outputs.Add((input.Item1, outputStream));
                outputSelector.AddOutput(multiplexId, outputLink);

                var multiplex = new PipeMultiplexer(inputLink.Reader, multiplexId, concentrator);

                inputTasks.Add(inputPump.Run(token));
                inputTasks.Add(multiplex.Run(token));
                outputTasks.Add(outputPump.Run(token));
            }

            await Task.WhenAll(inputTasks);
            concentrator.Complete(0);
            await Task.WhenAll(outputTasks);

            var results = await BufferValidation.ValidateTextData(inputs, outputs);
            return results;
        }
    }
}