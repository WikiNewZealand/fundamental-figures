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
                        Dataset = Path.GetFileNameWithoutExtension(path),
                        Records = records
                            .GroupBy(r => r.Parent)
                            .Select(p => new
                            {
                                Label = p.Key,
                                Measures = p
                                    .GroupBy(r => new { Measure = r.MeasureFormatted(), r.Discriminator })
                                    .Select(m => new
                                    {
                                        m.FirstOrDefault()?.Discriminator,
                                        Label = m.Key.Measure,
                                        Categories = m.Select(r => new
                                        {
                                            Label = r.CategoryFormatted(),
                                            Value = r.ValueFormatted(),
                                            r.ValueUnit,
                                            r.ValueLabel,
                                            r.Date,
                                            r.DateLabel
                                        }),
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

        public static string ValueFormatted(this Record record)
        {
            switch (record.ValueUnit)
            {
                case "null":
                    return record.NullReason;

                case "nzd":
                    return record.Value?.ToString("$###,###,###,###,###,###,###,###,##0.00");

                case "percentage":

                    return (record.Value / 100)?.ToString("0.0%");

                case "number":
                default:
                    if (record.Value % 1 != 0)
                    {
                        // This number has decimal places, so format expecting a decimal point
                        return record.Value?.ToString("###,###,###,###,###,###,###,###,##0.##");
                    }
                    else
                    {
                        // This number is a whole number, with no decimal point
                        return record.Value?.ToString("###,###,###,###,###,###,###,###,##0");
                    }
            }
        }
    }
}
