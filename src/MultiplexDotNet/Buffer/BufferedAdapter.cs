using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Multiplex
{
    public class MultiplexAdapter<T>
    {
        private readonly BlockingCollection<T> _dataItems;
        private readonly Action<T> _processor;
        private Task _processingTask;

        public MultiplexAdapter(Action<T> processor)
        {
            _dataItems = new BlockingCollection<T>(100);
            _processingTask = Task.Run(() => Process());
            _processor = processor;
        }

        private void Process()
        {
            while (!_dataItems.IsCompleted)
            {
                try
                {
                    var buf = _dataItems.Take();
                    _processor(buf);
                }
                catch (InvalidOperationException)
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public void Send(T buf)
        {
            try
            {
                _dataItems.Add(buf);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public int QueueCount => _dataItems.Count;

        public Task Stop()
        {
            _dataItems.CompleteAdding();
            return _processingTask;
        }
    }
}