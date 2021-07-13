using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Multiplex
{
    public static class TextSeedDataGenerator
    {

        public static async Task<List<(string, Stream)>> GenerateSeedData()
        {
            return await GenerateSeedData(10);
        }
        public static async Task<List<(string, Stream)>> GenerateSeedData(int numFiles)
        {
            try
            {
                var inputs = new List<(string, Stream)>();

                for (var i = 0; i < numFiles; i++)
                {
                    var fileName = $"tmp/file_{i}.txt";
                    //using (var stream = new FileStream(fileName, FileMode.Create))
                    var stream = new MemoryStream();
                    using (var writer = new StreamWriter(stream, Encoding.UTF8, 4096, true))
                    {
                        var r = new Random(Environment.TickCount);
                        var numberOfLines = r.Next(10, 500);
                        for (int lineNumber = 0; lineNumber < numberOfLines; lineNumber++)
                        {
                            var line = GenerateRandomLine(r.Next(50, 250), () => (char)r.Next(32, 126));
                            await writer.WriteLineAsync(line);
                        }
                        await writer.FlushAsync();
                        writer.Close();
                    }

                    stream.Seek(0, SeekOrigin.Begin);
                    var input = (fileName, stream);
                    inputs.Add(input);
                }

                return inputs;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        private static string GenerateRandomLine(int length, Func<char> charGen)
        {
            var buf = new StringBuilder();

            length.Times(() => buf.Append(charGen()));

            return buf.ToString();
        }
    }
}