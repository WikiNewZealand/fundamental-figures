using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
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

            // Use custom HttpClient to prevent auto-following 30x redirect responses because we want to interrogate redirects manually
            using (HttpClient client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
            using (ExcelPackage workbook = new ExcelPackage())
            {
                ExcelWorksheet worksheet = workbook.Workbook.Worksheets.Add(term);
                worksheet.Cells.Style.Font.Name = "Arial";
                worksheet.Cells.Style.Font.Size = 12;

                int row = 1;
                string parentLabel = null;
                string measureLabel = null;

                foreach (Dataset dataset in figure.Datasets)
                {
                    bool hasMeasureExclusions = dataset.Measure?.Exclude != null && dataset.Measure.Exclude.Any();
                    HashSet<string> measureExclusions = new HashSet<string>(dataset.Measure?.Exclude ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

                    bool hasMeasureInclusions = dataset.Measure?.Include != null && dataset.Measure.Include.Any();
                    HashSet<string> measureInclusions = new HashSet<string>(dataset.Measure?.Include?.Select(i => i.Value) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

                    bool hasGroupExclusions = dataset.Measure?.Group?.Exclude != null && dataset.Measure.Group.Exclude.Any();
                    HashSet<string> groupExclusions = new HashSet<string>(dataset.Measure?.Group?.Exclude ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

                    bool hasGroupInclusions = dataset.Measure?.Group?.Include != null && dataset.Measure.Group.Include.Any();
                    HashSet<string> groupInclusions = new HashSet<string>(dataset.Measure?.Group?.Include?.Select(i => i.Value) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

                    bool hasCategoryExclusions = dataset.Category?.Exclude != null && dataset.Category.Exclude.Any();
                    HashSet<string> categoryExclusions = new HashSet<string>(dataset.Category?.Exclude ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

                    bool hasCategoryInclusions = dataset.Category?.Include != null && dataset.Category.Include.Any();
                    HashSet<string> categoryInclusions = new HashSet<string>(dataset.Category?.Include?.Select(i => i.Value) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

                    List<Record> set = new List<Record>();

                    int countRecords = 0;
                    int countMissingDiscriminator = 0;
                    int countExcludedByDiscriminator = 0;
                    int countExcludedByMeasure = 0;
                    int countExcludedByGroup = 0;
                    int countExcludedByCategory = 0;

                    HttpFile httpFile = await client.GetHttpFile(dataset.Uri);

                    Uri uri = httpFile.Uri;
                    string csvFile = Path.Combine(figure.InputPath, httpFile.FileName);

                    if (File.Exists(csvFile))
                    {
                        Console.WriteLine($"Found '{Path.GetFileName(csvFile)}'");
                    }
                    else
                    {
                        Console.WriteLine($"Downloading '{Path.GetFileName(csvFile)}' from '{uri}'");

                        string directory = Path.GetDirectoryName(csvFile);

                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        using (FileStream stream = new FileStream(csvFile, FileMode.Create))
                        using (HttpResponseMessage response = await client.GetAsync(uri))
                        {
                            if (!response.IsSuccessStatusCode)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Downloading '{Path.GetFileName(csvFile)}' from '{uri}' failed with '{response.StatusCode}: {response.ReasonPhrase}'");
                                Console.WriteLine();
                                Console.ResetColor();
                                continue;
                            }
                            
                            (await response.Content.ReadAsStreamAsync()).CopyTo(stream);
                        }
                    }

                    using (StreamReader reader = new StreamReader(csvFile, Encoding.UTF8))
                    using (CsvReader csv = new CsvReader(reader))
                    {
                        Console.WriteLine($"Processing '{Path.GetFileName(csvFile)}'");

                        csv.Configuration.HeaderValidated = null;
                        csv.Configuration.MissingFieldFound = null;
                        csv.Configuration.RegisterClassMap(new RecordMap()
                            .Map(r => r.Discriminator, dataset.Discriminator ?? "Territorial Authority")
                            .Map(r => r.Date, dataset.Date)
                            .Map(r => r.Measure, dataset.Measure?.Column)
                            .Map(r => r.Group, dataset.Measure?.Group?.Column)
                            .Map(r => r.Category, dataset.Category?.Column)
                            .Map(r => r.Value, dataset.Value ?? "Value")
                            .Map(r => r.ValueUnit, dataset.ValueUnit ?? "Value Unit")
                            .Map(r => r.ValueLabel, dataset.ValueLabel ?? "Value Label")
                        );

                        foreach (Record r in csv.GetRecords<Record>())
                        {
                            countRecords++;

                            if (r.Discriminator == null)
                            {
                                countMissingDiscriminator++;
                                continue;
                            }

                            if (!term.Equals(r.Discriminator, StringComparison.OrdinalIgnoreCase) && !term.Equals(dataset.Term, StringComparison.OrdinalIgnoreCase))
                            {
                                countExcludedByDiscriminator++;
                                continue;
                            }

                            if (hasMeasureExclusions && measureExclusions.Contains(r.Measure))
                            {
                                countExcludedByMeasure++;
                                continue;
                            }

                            if (hasMeasureInclusions && !measureInclusions.Contains(r.Measure))
                            {
                                countExcludedByMeasure++;
                                continue;
                            }

                            if (hasGroupExclusions && groupExclusions.Contains(r.Group))
                            {
                                countExcludedByGroup++;
                                continue;
                            }

                            if (hasGroupInclusions && !groupInclusions.Contains(r.Group))
                            {
                                countExcludedByGroup++;
                                continue;
                            }

                            if (hasCategoryExclusions && categoryExclusions.Contains(r.Category))
                            {
                                countExcludedByCategory++;
                                continue;
                            }

                            if (hasCategoryInclusions && !categoryInclusions.Contains(r.Category))
                            {
                                countExcludedByCategory++;
                                continue;
                            }

                            if (!term.Equals(r.Discriminator, StringComparison.OrdinalIgnoreCase))
                            {
                                r.Discriminator = $"{term.ToTitleCase()} — {r.Discriminator}";
                            }

                            r.Parent = dataset.Parent;
                            r.Uri = dataset.Uri;
                            r.Separator = dataset.Measure?.Group?.Separator;
                            r.DateLabel = dataset.Date;

                            Include measure = dataset.Measure?.Include?.FirstOrDefault(i => i.Value.Equals(r.Measure, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(i.Label));

                            if (measure != null)
                            {
                                r.MeasureLabel = measure.Label;
                                r.ConvertToPercentage = measure.ConvertToPercentage;
                            }

                            Include group = dataset.Measure?.Group?.Include?.FirstOrDefault(i => i.Value.Equals(r.Group, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(i.Label));

                            if (group != null)
                            {
                                r.GroupLabel = group.Label;
                            }

                            Include category = dataset.Category?.Include?.FirstOrDefault(i => i.Value.Equals(r.Category, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(i.Label));

                            if (category != null)
                            {
                                r.CategoryLabel = category.Label;
                            }

                            set.Add(r);
                        }
                        
                        set = set
                            .GroupBy(r => new { r.Discriminator, r.Measure, r.Group, r.Category })
                            .Select(g => g
                                .OrderByDescending(r => r.Date)
                                .First()
                            )
                            .GroupBy(r => new { r.Discriminator, r.Measure, r.Group })
                            .Select(g =>
                            {
                                decimal? total = g.Sum(r => r.Value);

                                if (total == null || total == 0)
                                {
                                    return g;
                                }

                                foreach (Record r in g.Where(r => r.ConvertToPercentage))
                                {
                                    r.ValueUnit = "percentage";
                                    r.Value = (r.Value / total) * 100;
                                }

                                return g;
                            })
                            .SelectMany(g => g)
                            .OrderBy(r => r.Discriminator)
                            .ThenBy(r => dataset.Measure?.Include?.FindIndex(i => i.Value.Equals(r.Measure, StringComparison.OrdinalIgnoreCase)))
                            .ThenBy(r => r.Measure, StringComparer.OrdinalIgnoreCase)
                            .ThenBy(r => dataset.Measure?.Group?.Include?.FindIndex(i => i.Value.Equals(r.Group, StringComparison.OrdinalIgnoreCase)))
                            .ThenBy(r => r.Group, StringComparer.OrdinalIgnoreCase)
                            .ThenBy(r => dataset.Category?.Include?.FindIndex(i => i.Value.Equals(r.Category, StringComparison.OrdinalIgnoreCase)))
                            .ThenBy(r => r.Category, StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        foreach (Record record in set)
                        {
                            int col = 1;

                            if (parentLabel != dataset.Parent)
                            {
                                parentLabel = dataset.Parent;
                                worksheet.Cells[row, col].Value = parentLabel;
                            }
                            col++;

                            if (measureLabel != record.MeasureFormatted())
                            {
                                // Always write Discriminator when we're writing a new measure
                                worksheet.Cells[row, col].Value = record.Discriminator;
                            }
                            col++;

                            if (measureLabel != record.MeasureFormatted())
                            {
                                measureLabel = record.MeasureFormatted();
                                worksheet.Cells[row, col].Value = measureLabel;
                            }
                            col++;

                            worksheet.Cells[row, col].Value = record.CategoryFormatted();
                            col++;

                            switch (record.ValueUnit)
                            {
                                case "nzd":
                                    worksheet.Cells[row, col].Value = record.Value;
                                    worksheet.Cells[row, col].Style.Numberformat.Format = "$###,###,###,###,###,###,###,###,##0.00";
                                    break;

                                case "percentage":
                                    worksheet.Cells[row, col].Value = record.Value / 100;
                                    worksheet.Cells[row, col].Style.Numberformat.Format = "0.00%";
                                    break;

                                case "number":
                                default:
                                    worksheet.Cells[row, col].Value = record.Value;

                                    if (record.Value % 1 != 0)
                                    {
                                        // This number has decimal places, so format expecting a decimal point
                                        worksheet.Cells[row, col].Style.Numberformat.Format = "###,###,###,###,###,###,###,###,##0.##";
                                    }
                                    else
                                    {
                                        // This number is a whole number, with no decimal point
                                        worksheet.Cells[row, col].Style.Numberformat.Format = "###,###,###,###,###,###,###,###,##0";
                                    }

                                    break;
                            }
                            col++;

                            worksheet.Cells[row, col].Value = record.ValueLabel;
                            col++;

                            worksheet.Cells[row, col].Value = record.Date;
                            col++;

                            worksheet.Cells[row, col].Value = record.DateLabel;
                            col++;

                            worksheet.Cells[row, col].Value = record.Uri.ToString().Replace("/download", string.Empty, StringComparison.OrdinalIgnoreCase);
                            col++;

                            row++;
                        }

                        Console.WriteLine($" - {countRecords} records read");

                        if (countMissingDiscriminator > 0)
                        {
                            Console.WriteLine($" - {countMissingDiscriminator} records missing \"discriminator\"");
                        }

                        if (countExcludedByDiscriminator > 0)
                        {
                            Console.WriteLine($" - {countExcludedByDiscriminator} records excluded by \"discriminator\"");
                        }

                        if (countExcludedByMeasure > 0)
                        {
                            Console.WriteLine($" - {countExcludedByMeasure} records excluded by \"measure\"");
                        }

                        if (countExcludedByGroup > 0)
                        {
                            Console.WriteLine($" - {countExcludedByGroup} records excluded by \"group\"");
                        }

                        if (countExcludedByCategory > 0)
                        {
                            Console.WriteLine($" - {countExcludedByCategory} records excluded by \"category\"");
                        }

                        if (set.Count == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                        }

                        Console.WriteLine($" - {set.Count} records written to output");
                        Console.ResetColor();
                        Console.WriteLine();
                    }
                }

                worksheet.Cells.AutoFitColumns();

                return new MemoryStream(workbook.GetAsByteArray());
            }
        }
    }

    public class Record
    {
        public string Parent { get; set; }

        public Uri Uri { get; set; }

        public string Discriminator { get; set; }

        public string Date { get; set; }

        public string DateLabel { get; set; }

        public string Measure { get; set; }

        public string MeasureLabel { get; set; }

        public string Separator { get; set; }

        public string Group { get; set; }

        public string GroupLabel { get; set; }

        public string Category { get; set; }

        public string CategoryLabel { get; set; }

        public decimal? Value { get; set; }

        public string ValueUnit { get; set; }

        public string ValueLabel { get; set; }

        public bool ConvertToPercentage { get; set; }

        public string MeasureFormatted()
        {
            return !string.IsNullOrWhiteSpace(Group) ? $"{MeasureLabel ?? Measure} {Separator ?? "—"} {GroupLabel ?? Group}" : $"{MeasureLabel ?? Measure}";
        }

        public string CategoryFormatted()
        {
            return $"{CategoryLabel ?? Category}";
        }
    }

    public class RecordMap : ClassMap<Record>
    {
        public RecordMap Map<TMember>(Expression<Func<Record, TMember>> expression, string name)
        {
            Map(expression).Name(name);

            return this;
        }
    }

    public class Figure
    {
        public List<Dataset> Datasets { get; set; }

        public string InputPath { get; set; }

        public string OutputPath { get; set; }

        public Figure()
        {
            InputPath = @".\csv";
            OutputPath = @".\xlsx";
        }
    }

    public class Dataset
    {
        public Uri Uri { get; set; }

        public string Parent { get; set; }

        public string Term { get; set; }

        public string Discriminator { get; set; }
        
        public string Value { get; set; }

        public string ValueUnit { get; set; }

        public string ValueLabel { get; set; }

        public Measure Measure { get; set; }

        public Category Category { get; set; }

        public string Date { get; set; }
    }

    public class Measure
    {
        public string Column { get; set; }

        public Group Group { get; set; }

        public List<Include> Include { get; set; }

        public List<string> Exclude { get; set; }
    }

    public class Group
    {
        public string Column { get; set; }

        public string Separator { get; set; }

        public List<Include> Include { get; set; }

        public List<string> Exclude { get; set; }
    }

    public class Category
    {
        public string Column { get; set; }

        public List<Include> Include { get; set; }

        public List<string> Exclude { get; set; }
    }

    public class Include
    {
        public string Value { get; set; }

        public string Label { get; set; }

        [JsonProperty("convert-to-percentage")]
        public bool ConvertToPercentage { get; set; }
    }

    public class HttpFile
    {
        public Uri Uri { get; set; }

        public string FileName { get; set; }
    }

    public static class StringExtensions
    {
        public static string ToTitleCase(this string s)
        {
            return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(s);
        }
    }

    public static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> HeadAsync(this HttpClient client, Uri uri)
        {
            return client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
        }

        public static async Task<HttpFile> GetHttpFile(this HttpClient client, Uri uri)
        {
            using (HttpResponseMessage response = await client.HeadAsync(uri))
            {
                if (response.IsSuccessStatusCode)
                {
                    return new HttpFile
                    {
                        Uri = uri,
                        FileName = response.Content.Headers.ContentDisposition.FileName
                    };
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Response for '{uri}' failed with '{response.StatusCode}: {response.ReasonPhrase}', checking dataset for 302 Redirect");
                Console.ResetColor();

                // We'll try the URL of the data table and see if it has a redirect to a new URL
                // - Remove /download segment, trim trailing slash because Figure.NZ does not expect it
                UriBuilder ub = new UriBuilder(uri)
                {
                    Path = Path.Combine(uri.Segments.Take(uri.Segments.Length - 1).ToArray()).TrimEnd('/')
                };

                // - Replace existing URI
                uri = ub.Uri;
            }

            // - Try again
            using (HttpResponseMessage response = await client.GetAsync(uri))
            {
                if (response.StatusCode != HttpStatusCode.Redirect)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Response for '{uri}' failed with '{response.StatusCode}: {response.ReasonPhrase}'");
                    Console.ResetColor();

                    throw new ArgumentException($"Response for '{uri}' failed with '{response.StatusCode}: {response.ReasonPhrase}'", nameof(uri));
                    // return null;
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Response for '{uri}' succeeded with '{response.StatusCode}: {response.ReasonPhrase} {response.Headers.Location}'");
                Console.ResetColor();

                // - Append /download to the redirect location
                UriBuilder ub = new UriBuilder(response.Headers.Location);
                ub.Path = Path.Combine(response.Headers.Location.AbsolutePath, "download");

                // - Replace existing URI
                uri = ub.Uri;
            }

            // Recurse through the pipeline again, in case this one requires a redirect too
            return await client.GetHttpFile(uri);
        }
    }
}
