using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Multiplex.Test
{
    public class BufferedAdapterTests
    {
        [Fact]
        public void Happy()
        {
            var stopWatch = Stopwatch.StartNew();
            var testObject = new MultiplexAdapter<string>((buf) =>
            {
                Console.WriteLine(buf);
            });

            Parallel.ForEach(Utility.Seq(100), (index) =>
            {
                testObject.Send($"buffer-{index}");
            });

            //while (testObject.QueueCount > 0) { Thread.Sleep(1); };
            testObject.Stop();
            Console.WriteLine($"done in {stopWatch.ElapsedTicks} ticks");
        }
    }

    public static class Utility
    {
        public static IEnumerable<int> Seq(int max)
        {
            var items = new List<int>();
            for (int i = 0; i < max; i++)
                items.Add(i);
            return items;
        }
    }
}
