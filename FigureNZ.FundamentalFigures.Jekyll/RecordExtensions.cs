using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FigureNZ.FundamentalFigures.Jekyll
{
    public static class RecordExtensions
    {

        public static async Task<FileInfo> ToYaml(this Task<List<Record>> records, string path)
        {
            return (await records).ToYaml(path);
        }

        public static FileInfo ToYaml(this List<Record> records, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (FileStream output = new FileStream(path, FileMode.Create))
            using (StreamWriter writer = new StreamWriter(output, new UTF8Encoding(false)))
            {
                writer.WriteLine("---");
                writer.WriteLine($"data: {Path.GetFileNameWithoutExtension(path)}");
                writer.WriteLine("---");
                writer.WriteLine();
            }

            FileInfo file = new FileInfo(path);

            Console.WriteLine($"Wrote '{file.FullName}'");

            return file;
        }
    }
}
