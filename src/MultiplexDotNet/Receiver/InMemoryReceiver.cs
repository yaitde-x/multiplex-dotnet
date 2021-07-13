using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Multiplex
{
    public class InMemoryReceiver<T> : IDisposable where T : IBuffer
    {
        private readonly Func<string, Stream> _resourceCreator;
        private Action<string, Stream> _resourceDisposer;
        private ConcurrentDictionary<string, ReceiverContext<T>> _buffers;

        public InMemoryReceiver(Func<string, Stream> resourceCreator, Action<string, Stream> resourceDisposer)
        {
            _resourceCreator = resourceCreator;
            _resourceDisposer = resourceDisposer;
            _buffers = new ConcurrentDictionary<string, ReceiverContext<T>>();
        }

        public void Dispose()
        {
            var keys = _buffers.Keys;
            while (keys != null && keys.Count > 0)
            {
                foreach (var key in keys)
                {
                    if (_buffers.Remove(key, out var context))
                    {
                        DisposeContext(context).GetAwaiter().GetResult();
                    }
                }

                keys = _buffers.Keys;
            }
        }

        private async Task DisposeContext(ReceiverContext<T> context)
        {
            if (context.Resource != null)
            {
                await context.Writer.FlushAsync();
                context.Writer.Close();
                _resourceDisposer(context.ResourceId, context.Resource);
            }
        }

        public void Receive(T buf)
        {
            if (!_buffers.ContainsKey(buf.bufferId))
            {
                var context = new ReceiverContext<T>()
                {
                    Buffer = new MultiplexAdapter<T>(b =>
                    {
                        try
                        {
                            if (_buffers.TryGetValue(b.bufferId, out var ctx))
                            {
                                if (ctx.Resource == null)
                                {
                                    //var outFile = Path.Combine(Path.GetDirectoryName(b.fileName), "x_" + Path.GetFileName(b.fileName));
                                    //ctx.Resource = File.OpenWrite(outFile);
                                    ctx.ResourceId = b.bufferId;
                                    ctx.Resource = _resourceCreator(b.bufferId);
                                    ctx.Writer = new StreamWriter(ctx.Resource, Encoding.UTF8, 4096, true);

                                }

                                ctx.Writer.WriteLine(Encoding.UTF8.GetString(b.buffer));

                            }
                            Thread.Sleep(10);
                            //Console.WriteLine(b.bufferId);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    })
                };

                _buffers.AddOrUpdate(buf.bufferId, context, (fileName, ctx) => ctx);
            }

            _buffers[buf.bufferId].Buffer.Send(buf);
        }

        public bool IsRunningWork()
        {
            foreach (var buf in _buffers)
            {
                if (buf.Value.Buffer.QueueCount > 0)
                    return true;
            }
            return false;
        }

        public async Task Stop()
        {
            foreach (var buf in _buffers)
            {
                await buf.Value.Buffer.Stop();
                await DisposeContext(buf.Value);
            }
        }
    }
}