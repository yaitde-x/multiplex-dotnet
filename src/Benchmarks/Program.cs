using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Multiplex;

namespace Benchmarks
{
     [ClrJob(baseline: true), CoreJob, MonoJob, CoreRtJob]
    [RPlotExporter, RankColumn]
    public class MultiplexerBenchmarks
    {
        private CancellationTokenSource _source;
        private CancellationToken _token;

        [GlobalSetup]
        public void Setup()
        {
            _source = new CancellationTokenSource();
            _token = _source.Token;
        }

        [Benchmark]
        public Task<(long, List<string>)> RunMultiplexerTest() => Program.RunPipeMultiplexerTest(_token);

    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<MultiplexerBenchmarks>();
        }

        public static async Task RunTests(bool runLegacy, CancellationToken token)
        {
            var testResult = await RunPipeMultiplexerTest(token);

            if (runLegacy)
            {
                var parallelTestResult = await RunParallelTextTest(token);
                var sequentialTestResult = await RunSequentialTextTest(token);

                Console.WriteLine($"parallel test completed in {parallelTestResult.Item1} millis.");
                foreach (var result in parallelTestResult.Item2)
                    Console.WriteLine(result);

                Console.WriteLine($"sequential test completed in {sequentialTestResult.Item1} millis.");
                foreach (var result in sequentialTestResult.Item2)
                    Console.WriteLine(result);
            }

            Console.WriteLine($"pipeline test completed in {testResult.Item1} millis.");
            foreach (var result in testResult.Item2)
                Console.WriteLine(result);

        }

        public static async Task<(long, List<string>)> RunSequentialTextTest(CancellationToken token)
        {
            return await RunTest(End2End.ProcessTextDataSequentialTest, token);
        }

        public static async Task<(long, List<string>)> RunParallelTextTest(CancellationToken token)
        {
            return await RunTest(End2End.ProcessTextDataParallelTest, token);
        }

        public static async Task<(long, List<string>)> RunPipelineCopyTest(CancellationToken token)
        {
            return await RunTest(End2End.ProcessDataWithTransport2, token);
        }

        public static async Task<(long, List<string>)> RunStreamCopyTest(CancellationToken token)
        {
            return await RunTest(End2End.ProcessDataWithStreamCopy, token);
        }
        public static async Task<(long, List<string>)> RunLoopbackTest(CancellationToken token)
        {
            return await RunTest(End2End.LoopbackTest, token);
        }

        public static async Task<(long, List<string>)> RunPipeMultiplexerTest(CancellationToken token)
        {
            return await RunTest(End2End.PipeMultiplexerTest, token);
        }

        public static async Task<(long, List<string>)> RunTest(Func<List<(string, Stream)>, CancellationToken, Task<List<(string, Stream)>>> testFunc,
                                  CancellationToken token)
        {

            var inputs = await TextSeedDataGenerator.GenerateSeedData(1);
            var stopWatch = Stopwatch.StartNew();
            var outputs = await testFunc(inputs, token);
            var runtime = stopWatch.ElapsedMilliseconds;
            var results = await BufferValidation.ValidateTextData(inputs, outputs);

            var data = (runtime, results);
            return data;
        }
    }
}
