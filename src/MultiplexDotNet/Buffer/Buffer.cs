namespace Multiplex
{
    public interface IBuffer
    {
        string bufferId { get; set; }
        byte[] buffer { get; set; }
    }

    public class Buffer : IBuffer
    {
        public string bufferId { get; set; }
        public byte[] buffer { get; set; }
    }
}