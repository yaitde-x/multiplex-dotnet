using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Multiplex.Adapters;

namespace Multiplex.Pipeline
{
    public static class HeaderWriter
    {
        public static void Write(PipeWriter writer, int multiplexId, int length)
        {
            Memory<byte> memory = writer.GetMemory(TransportConstants.HeaderSize);
            var midSlice = memory.Slice(0, 4);
            var lenSlice = memory.Slice(4, 4);

            var mid = BitConverter.GetBytes(multiplexId);
            var len = BitConverter.GetBytes((int)length);

            mid.AsMemory().CopyTo(midSlice);
            len.AsMemory().CopyTo(lenSlice);

            writer.Advance(TransportConstants.HeaderSize);
        }
    }
}