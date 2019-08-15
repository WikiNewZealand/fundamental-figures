using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FigureNZ.FundamentalFigures.Json
{
    public static class RecordExtensions
    {
        public static async Task<FileInfo> ToJson(this Task<List<Record>> records, string path, Formatting formatting = Formatting.None)
        {
            return (await records).ToJson(path, formatting);
        }

        public static FileInfo ToJson(this List<Record> records, string path, Formatting formatting = Formatting.None)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (FileStream output = new FileStream(path, FileMode.Create))
            using (StreamWriter writer = new StreamWriter(output, Encoding.UTF8))
            using (JsonWriter json = new JsonTextWriter(writer))
            {
                new JsonSerializer { Formatting = formatting }.Serialize(
                    json,
                    records.GroupBy(r => r.Parent).ToDictionary(
                        g => g.Key,
                        g => g.Select(r => new
                        {
                            r.Discriminator,
                            Measure = r.MeasureFormatted(),
                            Category = r.CategoryFormatted(),
                            r.Value,
                            r.ValueUnit,
                            r.ValueLabel,
                            r.Date,
                            r.DateLabel,
                            Uri = r.UriFormatted()
                        }))
                );
            }

            FileInfo file = new FileInfo(path);

            Console.WriteLine($"Wrote '{file.FullName}'");

            return file;
        }
    }
}
