using System.Collections.Concurrent;
using System.IO.Pipelines;

namespace Multiplex.Pipeline
{

    public class TestOutputPipeSelector : IOutputSelector
    {
        private ConcurrentDictionary<int, Pipe> _outputPipes = new ConcurrentDictionary<int, Pipe>();

        public void AddOutput(int multiplexId, Pipe outputPipe)
        {
            _outputPipes.AddOrUpdate(multiplexId, outputPipe, (id, old) => outputPipe);
        }

        public PipeWriter GetPipeWriter(int multiplexId)
        {
            if (_outputPipes.TryGetValue(multiplexId, out var outputPipe))
                return outputPipe.Writer;

            return default(PipeWriter);
        }

        public void Complete(int multiplexId)
        {
            if (_outputPipes.ContainsKey(multiplexId))
            {
                if (_outputPipes.TryRemove(multiplexId, out var pipe))
                    pipe.Writer.Complete();
            }

        }

        public void CompleteAll()
        {
            var writers = _outputPipes.Values;
            _outputPipes.Clear();

            foreach (var writer in writers)
                writer.Writer.Complete();

        }
    }

}