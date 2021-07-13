
using System;
using System.IO;

namespace Multiplex.Adapters {
    public class DuplexMemoryStream : Stream
    {
        private readonly Stream _input;
        private Stream _output;

        public DuplexMemoryStream(Stream input, Stream output) {
            _input = input;
            _output = output;
        }

        public Stream Input => _input;
        public Stream Output => _output;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _input.Length;

        public override long Position { get => _input.Length; set => _output.Position = value; }

        public override void Flush()
        {
            _output.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _input.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException("seek elsewhere");
        }

        public override void SetLength(long value)
        {
            _output.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _output.Write(buffer, offset, count);
        }
    }
}