using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FigureNZ.FundamentalFigures.Csv;
using FigureNZ.FundamentalFigures.Excel;
using FigureNZ.FundamentalFigures.Jekyll;
using FigureNZ.FundamentalFigures.Json;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace FigureNZ.FundamentalFigures.Console
{
    class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated
            };

            var pathToConfiguration = app.Argument<string>("Path", "Local path to the .json configuration file that defines your Figure").IsRequired();

            var discriminatorTerm = app.Argument<string>("Term", "The term used to search the discriminator column of each dataset, e.g. 'Auckland'", true).IsRequired();

            var inputPath = app.Option<string>("-in|--input-path", "Local path where input .csv files will be cached, defaults to './input'", CommandOptionType.SingleValue);

            var outputPath = app.Option<string>("-out|--output-path", "Local path where the output file will be written, defaults to './output'", CommandOptionType.SingleValue);

            var outputType = app.Option<OutputTypeEnum>("-t|--output-type", "One or more of 'excel', 'json', 'csv', or 'yaml', default is 'excel'", CommandOptionType.MultipleValue);

            var openOutputFile = app.Option<bool>("-o|--open-output-file", "Open the output file in your default file handler when processing is complete", CommandOptionType.NoValue);

            app.HelpOption();

            app.OnExecute(async () =>
            {
                string config = pathToConfiguration.ParsedValue;
                string input = inputPath.ParsedValue ?? "./input";
                string output = outputPath.ParsedValue ?? "./output";
                bool open = openOutputFile.Values.Any();

                IReadOnlyList<string> terms = discriminatorTerm.ParsedValues;

                IEnumerable<OutputTypeEnum> types = outputType.ParsedValues;

                if (types == null || !types.Any())
                {
                    types = new List<OutputTypeEnum> { OutputTypeEnum.Excel };
                }

                foreach (string term in terms)
                {
                    var records = await JsonConvert.DeserializeObject<Figure>(File.ReadAllText(config)).ToRecords(term, input);

                    foreach (OutputTypeEnum type in types)
                    {
                        FileInfo file;

                        switch (type)
                        {
                            case OutputTypeEnum.Excel:

                                file = records.ToExcelPackage(Path.Combine(output, $"{term}.xlsx"));
                                break;

                            case OutputTypeEnum.Json:

                                file = records.ToJson(Path.Combine(output, $"{term}.json"), Formatting.Indented);
                                break;

                            case OutputTypeEnum.Csv:

                                file = records.ToCsv(Path.Combine(output, $"{term}.csv"));
                                break;

                            case OutputTypeEnum.Yaml:
                                file = records.ToYaml(Path.Combine(output, $"{term}.md"));
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
                    }
                }

                return 0;
            });

            return app.Execute(args);
        }
    }
}
