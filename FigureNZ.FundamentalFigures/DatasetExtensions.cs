using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;

namespace FigureNZ.FundamentalFigures
{
    public static class DatasetExtensions
    {
        public static List<Record> ToRecords(this Dataset dataset, string csvFile, string term)
        {
            using (StreamReader reader = new StreamReader(csvFile, Encoding.UTF8))
            using (CsvReader csv = new CsvReader(reader))
            {
                Console.WriteLine($"Processing '{Path.GetFileName(csvFile)}'");

                // Danger, Will Robinson!
                //
                // CsvHelper stores property names in couple of Dictionaries that use a string key.
                // These dictionaries are case sensitive, because C# supports properties the same name as long as the cases are different (e.g. "discriminator" and "Discriminator" are different properties)
                //
                // But, we want a case _insensitive_ match for when a new csv file differs in column header case only (e.g. "Territorial Authority" and "Territorial authority") so we don't have to fix our Dataset configuration
                // As per CsvHelper's documentation, we're expected to use the PrepareHeaderForMatch property to call ToLower() on all column headers:
                //   - https://joshclose.github.io/CsvHelper/getting-started/
                //   - https://github.com/JoshClose/CsvHelper/issues/1183
                //
                // That violates Microsoft's sting comparison guidelines, however: https://docs.microsoft.com/en-us/dotnet/csharp/how-to/compare-strings
                //
                // I'd prefer to replace the Dictionaries with ones that use a case insensitive match:
                //
                //  - csv.Context.NamedIndexes = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
                //  - csv.Context.NamedIndexCache = new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase);
                //
                // ...which works right now, but reaching into CsvHelper's internals feels kind of fragile.
                //
                // So, we'll grumble and go ahead with ToLower()
                csv.Configuration.PrepareHeaderForMatch = (header, index) => header.ToLowerInvariant();

                // If we have more or fewer headers or properties than we expect, just keep going
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
                    .Map(r => r.NullReason, dataset.NullReason ?? "Null Reason")
                );
                
                return dataset.ToRecords(csv, term);
            }
        }

        public static List<Record> ToRecords(this Dataset dataset, CsvReader csv, string term)
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
                .GroupBy(r => new {r.Discriminator, r.Measure, r.Group, r.Category})
                .Select(g => g
                    .OrderByDescending(r => r.Date)
                    .ThenByDescending(r => r.Value) // Case where Auckland appears twice for the same year because super city
                    .First()
                )
                .GroupBy(r => new {r.Discriminator, r.Measure, r.Group})
                .Select(g =>
                {
                    decimal? total = g.Sum(r => r.Value);

                    if (total == null || total == 0)
                    {
                        return g;
                    }

                    foreach (Record r in g.Where(r => r.ConvertToPercentage && r.Value != null))
                    {
                        r.ValueUnit = "percentage";
                        r.ValueLabel = r.ValueLabel.Replace("Number", "%", StringComparison.OrdinalIgnoreCase);
                        r.Value = (r.Value / total) *
                                  100; // Multiple by 100 to stay consistent with other values that are natively 100-based
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

            Console.WriteLine($" - {set.Count} records included");
            Console.ResetColor();
            Console.WriteLine();

            return set;
        }
    }
}
