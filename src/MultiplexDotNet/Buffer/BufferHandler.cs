using System;

namespace Multiplex
{
    public class BufferHandler
    {
        public void Handle(Buffer buf)
        {
            Console.WriteLine($"{buf.bufferId} - {buf.buffer}");
        }
    }
}