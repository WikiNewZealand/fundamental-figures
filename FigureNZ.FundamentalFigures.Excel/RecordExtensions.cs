using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace FigureNZ.FundamentalFigures.Excel
{
    public static class RecordExtensions
    {
        public static async Task<FileInfo> ToExcelPackage(this Task<List<Record>> records, string path)
        {
            return (await records).ToExcelPackage(path);
        }

        public static FileInfo ToExcelPackage(this List<Record> records, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (FileStream output = new FileStream(path, FileMode.Create))
            using (ExcelPackage package = new ExcelPackage(output))
            {
                foreach (var set in records.GroupBy(r => r.Parent))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(set.Key).PopulateFromRecords(set.ToList());
                }

                package.Save();

            }

            FileInfo file = new FileInfo(path);

            Console.WriteLine($"Wrote '{file.FullName}'");

            return file;
        }
    }
}
