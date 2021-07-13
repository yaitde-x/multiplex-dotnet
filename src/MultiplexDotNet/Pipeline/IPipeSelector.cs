using System.IO.Pipelines;

namespace Multiplex.Pipeline
{
    public interface IOutputSelector
    {
        PipeWriter GetPipeWriter(int multiplexId);
        void Complete(int multiplexId);
        void CompleteAll();
    }
}