using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FigureNZ.FundamentalFigures
{
    public static class FigureExtensions
    {
        public static async Task<List<Record>> ToRecords(this Figure figure, string term, string inputPath)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Processing {figure.Datasets.Count} datasets with term '{term}'...");
            Console.ResetColor();
            Console.WriteLine();

            List<Record> set = new List<Record>();

            // Use custom HttpClient to prevent auto-following 30x redirect responses because we want to interrogate redirects manually
            using (HttpClient client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
            {
                foreach (Dataset dataset in figure.Datasets)
                {
                    var csvFile = await client.DownloadHttpFileAsync(dataset.Uri, inputPath);
                    var records = dataset.ToRecords(csvFile, term);

                    set.AddRange(records);
                }
            }

            return set;
        }
    }
}
