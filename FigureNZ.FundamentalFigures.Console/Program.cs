using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FigureNZ.FundamentalFigures.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Figure figure = JsonConvert.DeserializeObject<Figure>(File.ReadAllText(args[0]));

            string term = args[1];

            string file = Path.Combine(figure.OutputPath, $"{term}.xlsx");

            string directory = Path.GetDirectoryName(file);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FileStream output = new FileStream(file, FileMode.Create))
            {
                (await new XlsxGenerator().FromFigure(figure, term)).CopyTo(output);

                file = output.Name;
            }

            System.Console.WriteLine($"Wrote '{file}'");

            if (args.Length >= 3 && args[2] == "false")
            {
                Environment.Exit(0);
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{file}\""));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", $"\"{file}\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", $"\"{file}\"");
            }
        }
    }
}
