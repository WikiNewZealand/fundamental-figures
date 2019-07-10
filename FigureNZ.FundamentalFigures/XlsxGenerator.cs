using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;

namespace FigureNZ.FundamentalFigures
{
    public class XlsxGenerator
    {
        public async Task<Stream> FromFigure(Figure figure, string term)
        {
            Console.WriteLine($"Processing {figure.Datasets.Count} datasets with term '{term}'...");
            Console.WriteLine();

            using (ExcelPackage workbook = new ExcelPackage())
            {
                ExcelWorksheet worksheet = workbook.Workbook.Worksheets.Add(term);

                int row = 1;
                string parentLabel = null;
                string measureLabel = null;

                using (HttpClient client = new HttpClient())
                {
                    foreach (Dataset dataset in figure.Datasets)
                    {
                        int countRecords = 0;
                        int countMissingDiscriminator = 0;
                        int countExcludedByDiscriminator = 0;
                        int countExcludedByMeasure = 0;
                        int countExcludedByGroup = 0;
                        int countExcludedByCategory = 0;

                        string csvFile;

                        using (HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, dataset.Uri)))
                        {
                            response.EnsureSuccessStatusCode();

                            Console.WriteLine($"Expecting '{response.Content.Headers.ContentDisposition.FileName}'");
                            csvFile = response.Content.Headers.ContentDisposition.FileName;
                        }

                        if (File.Exists(csvFile))
                        {
                            Console.WriteLine($"Found '{csvFile}'");
                        }
                        else
                        {
                            Console.WriteLine($"Downloading '{csvFile}' from '{dataset.Uri}'");

                            using (FileStream stream = new FileStream(csvFile, FileMode.Create))
                            using (HttpResponseMessage response = await client.GetAsync(dataset.Uri))
                            {
                                response.EnsureSuccessStatusCode();
                                
                                (await response.Content.ReadAsStreamAsync()).CopyTo(stream);
                            }
                        }

                        using (StreamReader reader = new StreamReader(csvFile, Encoding.UTF8))
                        using (CsvReader csv = new CsvReader(reader))
                        {
                            Console.WriteLine($"Processing '{csvFile}'");

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

                            foreach (Record r in csv.GetRecords<Record>())
                            {
                                countRecords++;

                                if (r.Discriminator == null)
                                {
                                    countMissingDiscriminator++;
                                    continue;
                                }

                                if (!r.Discriminator.Equals(term, StringComparison.OrdinalIgnoreCase))
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

                                r.Parent = dataset.Parent;
                                r.Uri = dataset.Uri;
                                r.Separator = dataset.Measure?.Group?.Separator;
                                r.DateLabel = dataset.Date;

                                Include measure = dataset.Measure?.Include?.FirstOrDefault(i => i.Value.Equals(r.Measure, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(i.Label));

                                if (measure != null)
                                {
                                    r.MeasureLabel = measure.Label;
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
                                .GroupBy(r => new { r.Measure, r.Group, r.Category })
                                .Select(g => g
                                    .OrderByDescending(r => r.Date)
                                    .First()
                                )
                                .OrderBy(r => dataset.Measure?.Include?.FindIndex(i => i.Value.Equals(r.Measure, StringComparison.OrdinalIgnoreCase)))
                                .ThenBy(r => dataset.Category?.Include?.FindIndex(i => i.Value.Equals(r.Category, StringComparison.OrdinalIgnoreCase)))
                                .ToList();

                            foreach (Record record in set)
                            {
                                if (parentLabel != dataset.Parent)
                                {
                                    parentLabel = dataset.Parent;
                                    worksheet.Cells[row, 1].Value = parentLabel;
                                }

                                if (measureLabel != record.MeasureFormatted())
                                {
                                    // Add a blank row between measures
                                    // TODO: Figure out how to avoid this null check
                                    if (measureLabel != null)
                                    {
                                        row++;
                                    }

                                    measureLabel = record.MeasureFormatted();
                                    worksheet.Cells[row, 2].Value = measureLabel;
                                }

                                worksheet.Cells[row, 3].Value = record.CategoryFormatted();
                                worksheet.Cells[row, 4].Value = record.Value;
                                worksheet.Cells[row, 5].Value = record.Date;
                                worksheet.Cells[row, 6].Value = record.DateLabel;
                                worksheet.Cells[row, 7].Value = record.Uri.ToString().Replace("/download", string.Empty, StringComparison.OrdinalIgnoreCase);

                                row++;
                            }

                            // Add a blank row between datasets
                            measureLabel = null;
                            row++;

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

                            Console.WriteLine($" - {set.Count} records written to output");
                            Console.WriteLine();
                        
                        }
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
    }

    public class Dataset
    {
        public Uri Uri { get; set; }

        public string Parent { get; set; }

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
    }
}
