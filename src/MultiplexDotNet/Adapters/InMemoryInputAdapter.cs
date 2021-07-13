using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace Multiplex.Adapters
{

    public class InMemoryInputAdapter
    {

        public InMemoryInputAdapter()
        {
            
        }
        public Task Run(Func<PipeWriter,Task> writer, Func<PipeReader, Task> reader)
        {
            var pipe = new Pipe();
            Task writing = writer(pipe.Writer);
            Task reading = reader(pipe.Reader);
            return Task.WhenAll(reading, writing);
        }
    }
}