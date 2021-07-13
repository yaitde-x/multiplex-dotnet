using System.IO.Pipelines;

namespace Multiplex.Pipeline
{
    public class Concentrator : IOutputSelector
    {
        private readonly Pipe _pipe;

        public Concentrator(Pipe pipe)
        {
            _pipe = pipe;
        }

        public void Complete(int multiplexId)
        {
            _pipe.Writer.Complete();
        }

        public void CompleteAll()
        {
            _pipe.Writer.Complete();
        }

        public PipeWriter GetPipeWriter(int multiplexId)
        {
            return _pipe.Writer;
        }
    }
}