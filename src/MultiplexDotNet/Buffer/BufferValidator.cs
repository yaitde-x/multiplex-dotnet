using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Multiplex
{
    public static class BufferValidation
    {
        public static async Task<List<string>> ValidateTextData(List<(string, Stream)> inputs, List<(string, Stream)> outputs)
        {
            var inputIndex = new Dictionary<string, BufferStat>();
            var outputIndex = new Dictionary<string, BufferStat>();
            var results = new List<string>();

            foreach (var input in inputs)
                inputIndex[input.Item1] = await GetBufferStats(input.Item2);

            foreach (var output in outputs)
                outputIndex[output.Item1] = await GetBufferStats(output.Item2);

            if (inputs.Count != outputs.Count)
                results.Add("input stream count doesn't match output stream count");

            foreach (var inputKv in inputIndex)
            {
                if (outputIndex.ContainsKey(inputKv.Key))
                {
                    var inputStat = inputKv.Value;
                    var outputStat = outputIndex[inputKv.Key];

                    if (inputStat.Lines != outputStat.Lines)
                        results.Add($"buffer {inputKv.Key} line counts: {inputStat.Lines}, {outputStat.Lines}");

                    if (inputStat.Counts.Count != outputStat.Counts.Count)
                        results.Add($"length counts don't match {inputStat.Counts.Count}, {outputStat.Counts.Count}");

                    foreach (var lengthCountKv in inputStat.Counts)
                    {
                        if (outputStat.Counts.ContainsKey(lengthCountKv.Key))
                        {
                            if (lengthCountKv.Value != outputStat.Counts[lengthCountKv.Key])
                                results.Add($"{inputKv.Key}, line length {lengthCountKv.Key} mismatch: {lengthCountKv.Value},{outputStat.Counts[lengthCountKv.Key]}");
                        }
                        else
                            results.Add($"output for {inputKv.Key} doesn't have a count for length {lengthCountKv.Key}");

                    }
                }
                else
                    results.Add($"input {inputKv.Key} was not in the output");
            }

            if (results.Count == 0)
                results.Add("inputs and outputs match!");

            return results;
        }

        class BufferStat
        {
            public int Lines { get; set; }
            public Dictionary<int, int> Counts { get; set; } = new Dictionary<int, int>();
        }

        private static async Task<BufferStat> GetBufferStats(Stream stream)
        {
            var stat = new BufferStat();
            stream.Seek(0, SeekOrigin.Begin);

            using (var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, true))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var len = line.Length;

                    stat.Lines++;
                    if (!stat.Counts.ContainsKey(len))
                        stat.Counts[len] = 0;

                    stat.Counts[len]++;
                }
            }
            return stat;
        }
    }
}