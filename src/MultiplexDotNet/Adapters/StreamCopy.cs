using System.IO;
using System.Threading.Tasks;

namespace Multiplex.Adapters
{
    public class StreamCopy
    {
        public async Task Copy(Stream input, Stream output)
        {
            await input.CopyToAsync(output);
        }

    }
}