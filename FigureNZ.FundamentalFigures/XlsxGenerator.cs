using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace FigureNZ.FundamentalFigures
{
    public class XlsxGenerator
    {
        public async Task<Stream> FromFigure(Figure figure, string term)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Processing {figure.Datasets.Count} datasets with term '{term}'...");
            Console.ResetColor();
            Console.WriteLine();

            using (ExcelPackage workbook = new ExcelPackage())
            {
                foreach (var set in (await figure.ToRecords(term)).GroupBy(r => r.Parent))
                {
                    ExcelWorksheet worksheet = workbook.Workbook.Worksheets.Add(set.Key).FromRecords(set.ToList());
                }

                return new MemoryStream(workbook.GetAsByteArray());
            }
        }
    }
}
