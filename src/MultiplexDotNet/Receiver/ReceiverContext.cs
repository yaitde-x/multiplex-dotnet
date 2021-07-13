using System.IO;

namespace Multiplex
{
    class ReceiverContext<T> where T : IBuffer
    {
        internal string ResourceId { get; set; }
        internal Stream Resource { get; set; }
        internal StreamWriter Writer { get; set; }
        internal MultiplexAdapter<T> Buffer { get; set; }
    }
}