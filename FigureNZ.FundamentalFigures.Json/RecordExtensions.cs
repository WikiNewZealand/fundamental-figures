using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
                new JsonSerializer
                {
                    Formatting = formatting,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }.Serialize(
                    json,
                    new
                    {
                        records.FirstOrDefault()?.Discriminator,
                        Records = records
                            .GroupBy(r => r.Parent)
                            .Select(p => new
                            {
                                Label = p.Key,
                                Measures = p
                                    .GroupBy(r => r.MeasureFormatted())
                                    .Select(m => new
                                    {
                                        Label = m.Key,
                                        Categories = m.Select(r => new
                                        {
                                            Label = r.CategoryFormatted(),
                                            r.Value
                                        }),
                                        m.FirstOrDefault()?.ValueUnit,
                                        m.FirstOrDefault()?.ValueLabel,
                                        m.FirstOrDefault()?.Date,
                                        m.FirstOrDefault()?.DateLabel,
                                        Source = m.FirstOrDefault()?.UriFormatted()
                                    })
                            })
                    }
                );
            }

            FileInfo file = new FileInfo(path);

            Console.WriteLine($"Wrote '{file.FullName}'");

            return file;
        }
    }
}
