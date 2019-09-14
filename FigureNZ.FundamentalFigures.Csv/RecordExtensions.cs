using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;

namespace FigureNZ.FundamentalFigures.Csv
{
    public static class RecordExtensions
    {
        public static async Task<FileInfo> ToCsv(this Task<List<Record>> records, string path)
        {
            return (await records).ToCsv(path);
        }

        public static FileInfo ToCsv(this List<Record> records, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (FileStream output = new FileStream(path, FileMode.Create))
            using (StreamWriter writer = new StreamWriter(output, Encoding.UTF8))
            using (var csv = new CsvWriter(writer))
            {
                var map = new RecordMap()
                    .Map(r => r.Selector, "Territorial Authority")
                    .Map(r => r.Parent, "Topic");

                map
                    .Map(r => r.Measures)
                    .Name("Measure")
                    .ConvertUsing(r => r.MeasureFormatted());

                map
                    .Map(r => r.Categories)
                    .Name("Category")
                    .ConvertUsing(r => r.CategoryFormatted());

                map
                    .Map(r => r.Value, "Value")
                    .Map(r => r.ValueUnit, "ValueUnit")
                    .Map(r => r.ValueLabel, "ValueLabel")
                    .Map(r => r.NullReason, "NullReason")
                    .Map(r => r.Date, "Date")
                    .Map(r => r.DateLabel, "DateLabel");

                map
                    .Map(r => r.Uri)
                    .Name("Source")
                    .ConvertUsing(r => r.UriFormatted());

                csv.Configuration.RegisterClassMap(map);

                csv.WriteRecords(records);
            }

            FileInfo file = new FileInfo(path);

            Console.WriteLine($"Wrote '{file.FullName}'");

            return file;
        }
    }
}
