using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FigureNZ.FundamentalFigures.Excel;
using FigureNZ.FundamentalFigures.Json;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace FigureNZ.FundamentalFigures.Console
{
    class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication();

            var pathToConfiguration = app.Argument<string>("Path", "Local path to the .json configuration file that defines your Figure").IsRequired();

            var discriminatorTerm = app.Argument<string>("Term", "The term used to search the discriminator column of each dataset, e.g. 'Auckland'").IsRequired();

            var inputPath = app.Option<string>("-in|--input-path", "Local path where input .csv files will be cached, defaults to './input'", CommandOptionType.SingleValue);

            var outputPath = app.Option<string>("-out|--output-path", "Local path where the output file will be written, defaults to './output'", CommandOptionType.SingleValue);

            var outputType = app.Option<OutputTypeEnum>("-t|--output-type", "Either 'json' or 'excel', default is 'excel'", CommandOptionType.SingleValue);

            var openOutputFile = app.Option<bool>("-o|--open-output-file", "Open the output file in your default file handler when processing is complete", CommandOptionType.NoValue);

            app.HelpOption();

            app.OnExecute(async () =>
            {
                string config = pathToConfiguration.ParsedValue;
                string term = discriminatorTerm.ParsedValue;
                string input = inputPath.ParsedValue ?? "./input";
                string output = outputPath.ParsedValue ?? "./output";
                OutputTypeEnum type = outputType.ParsedValue;
                bool open = openOutputFile.Values.Any();

                FileInfo file;

                switch (type)
                {
                    case OutputTypeEnum.Excel:

                        file = await JsonConvert.DeserializeObject<Figure>(File.ReadAllText(config))
                            .ToRecords(term, input)
                            .ToExcelPackage(Path.Combine(output, $"{term}.xlsx"));

                        break;

                    case OutputTypeEnum.Json:

                        file = await JsonConvert.DeserializeObject<Figure>(File.ReadAllText(config))
                            .ToRecords(term, input)
                            .ToJson(Path.Combine(output, $"{term}.json"), Formatting.Indented);

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (open)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{file.FullName}\""));
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", $"\"{file.FullName}\"");
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", $"\"{file.FullName}\"");
                    }
                }

                return 0;
            });

            return app.Execute(args);
        }
    }
}
