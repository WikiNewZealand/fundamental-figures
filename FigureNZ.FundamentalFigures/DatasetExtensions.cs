using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Transactions;
using CsvHelper;
using CsvHelper.TypeConversion;
using Newtonsoft.Json;

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

                csv.Configuration.ReadingExceptionOccurred = exception =>
                {
                    if (exception is TypeConverterException)
                    {
                        // Some text in the Value column, instead of a number
                        // In most datasets, Value is null and we're supplied a null reason to indicate why we don't have a value
                        // However, some datasets include the null reason in the value column and cause type conversion errors
                        // We'll just skip these rows
                        return false;
                    }

                    return true;
                };

                var map = new RecordMap()
                    .Map(r => r.Selector, dataset.Selector ?? "Territorial Authority")
                    .Map(r => r.Date, dataset.Date);

                map.Map(r => r.Measures)
                    .ConvertUsing(rr => dataset
                        .Measure
                        .Select((measure, index) => new { measure, index })
                        .ToDictionary(
                            mi => mi.measure.Name, 
                            mi => new ColumnValue
                            {
                                Index = mi.index,
                                Column = mi.measure.Name,
                                Separator = mi.measure.Separator ?? " — ",
                                Value = rr.GetField<string>(mi.measure.Name)
                            })
                    );

                map.Map(r => r.Categories)
                    .ConvertUsing(rr => dataset
                        .Category
                        .Select((category, index) => new { category, index })
                        .ToDictionary(ci => 
                            ci.category.Name, 
                            ci => new ColumnValue
                            {
                                Index = ci.index,
                                Column = ci.category.Name,
                                Separator = ci.category.Separator ?? " — ",
                                Value = rr.GetField<string>(ci.category.Name)
                            })
                    );

                map
                    .Map(r => r.Value, dataset.Value ?? "Value")
                    .Map(r => r.ValueUnit, dataset.ValueUnit ?? "Value Unit")
                    .Map(r => r.ValueLabel, dataset.ValueLabel ?? "Value Label")
                    .Map(r => r.NullReason, dataset.NullReason ?? "Null Reason");

                csv.Configuration.RegisterClassMap(map);
                
                return dataset.ToRecords(csv, term);
            }
        }

        public static List<Record> ToRecords(this Dataset dataset, CsvReader csv, string term)
        {
            HashSet<string> terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { term };

            if (!string.IsNullOrEmpty(dataset.TermMapping))
            {
                using (StreamReader file = File.OpenText(dataset.TermMapping))
                {
                    Dictionary<string, List<string>> mapping = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    new JsonSerializer().Populate(file, mapping);

                    if (mapping.TryGetValue(term, out List<string> maps))
                    {
                        foreach (string map in maps)
                        {
                            terms.Add(map);
                        }
                    }
                }
            }

            bool hasMeasureExclusions = dataset.Measure.Any(c => c.Exclude != null && c.Exclude.Any());
            Dictionary<string, HashSet<string>> measureExclusions = dataset.Measure.Where(c => c.Exclude != null).ToDictionary(c => c.Name, c => new HashSet<string>(c.Exclude, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            
            bool hasMeasureInclusions = dataset.Measure.Any(c => c.Include != null && c.Include.Any());
            Dictionary<string, Dictionary<string, (int Index, Include Include)>> measureInclusions = dataset.Measure.Where(c => c.Include != null).ToDictionary(c => c.Name, c => c.Include.Select((include, index) => (Index: index, Include: include)).ToDictionary(ii => ii.Include.Value, i => i, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

            bool hasCategoryExclusions = dataset.Category.Any(c => c.Exclude != null && c.Exclude.Any());
            Dictionary<string, HashSet<string>> categoryExclusions = dataset.Category.Where(c => c.Exclude != null).ToDictionary(c => c.Name, c => new HashSet<string>(c.Exclude, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

            bool hasCategoryInclusions = dataset.Category.Any(c => c.Include != null && c.Include.Any());
            Dictionary<string, Dictionary<string, (int Index, Include Include)>> categoryInclusions = dataset.Category.Where(c => c.Include != null).ToDictionary(c => c.Name, c => c.Include.Select((include, index) => (Index: index, Include: include)).ToDictionary(ii => ii.Include.Value, i => i, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

            List<Record> set = new List<Record>();

            int countRecords = 0;
            int countMissingSelector = 0;
            int countExcludedBySelector = 0;
            int countExcludedByMeasure = 0;
            int countExcludedByGroup = 0;
            int countExcludedByCategory = 0;
            int countExcludedByValue = 0;

            foreach (Record r in csv.GetRecords<Record>())
            {
                countRecords++;

                if (r.Selector == null)
                {
                    countMissingSelector++;
                    continue;
                }

                if (!terms.Contains(r.Selector) && !term.Equals(dataset.AllSelectorsMatchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    countExcludedBySelector++;
                    continue;
                }
                
                if (hasMeasureExclusions && r.Measures.Any(m => measureExclusions.TryGetValue(m.Key, out var exclusions) && exclusions.Contains(m.Value.Value)))
                {
                    countExcludedByMeasure++;
                    continue;
                }

                if (hasMeasureInclusions && r.Measures.Any(m => measureInclusions.TryGetValue(m.Key, out var inclusions) && inclusions.Any() && !inclusions.ContainsKey(m.Value.Value)))
                {
                    countExcludedByMeasure++;
                    continue;
                }

                if (hasCategoryExclusions && r.Categories.Any(m => categoryExclusions.TryGetValue(m.Key, out var exclusions) && exclusions.Contains(m.Value.Value)))
                {
                    countExcludedByCategory++;
                    continue;
                }

                if (hasCategoryInclusions && r.Categories.Any(c => categoryInclusions.TryGetValue(c.Key, out var inclusions) && inclusions.Any() && !inclusions.ContainsKey(c.Value.Value)))
                {
                    countExcludedByCategory++;
                    continue;
                }

                // If we've selected this row without a direct match to the supplied term, it's because we've got some mapping somewhere in the pipeline
                // Prepend the term to the row's selector, so we get something like "Auckland — Waitemata DHB"
                if (!term.Equals(r.Selector, StringComparison.OrdinalIgnoreCase))
                {
                    r.Selector = $"{term} — {r.Selector}";
                }

                if (hasMeasureInclusions)
                {
                    foreach (var m in r.Measures)
                    {
                        if (measureInclusions.TryGetValue(m.Key, out var mi) && mi.TryGetValue(m.Value.Value, out var ii))
                        {
                            m.Value.Label = ii.Include.Label;

                            // TODO: Move this out to Measure?
                            r.ConvertToPercentage = r.ConvertToPercentage || ii.Include.ConvertToPercentage;
                        }
                    }
                }

                if (hasCategoryInclusions)
                {
                    foreach (var c in r.Categories)
                    {
                        if (categoryInclusions.TryGetValue(c.Key, out var ci) && ci.TryGetValue(c.Value.Value, out var ii))
                        {
                            c.Value.Label = ii.Include.Label;

                            // TODO: Move this out to Category?
                            r.ConvertToPercentage = r.ConvertToPercentage || ii.Include.ConvertToPercentage;
                        }
                    }
                }

                if (r.Value != null && r.ValueLabel == "NZD thousands")
                {
                    r.ValueLabel = "NZD";
                    r.Value = r.Value * 1000;
                }

                r.Parent = dataset.Parent;
                r.Uri = dataset.Source;
                r.DateLabel = dataset.Date;

                set.Add(r);
            }

            var ordered = set
                .GroupBy(r => new { r.Selector, Measure = r.MeasureFormatted(), Category = r.CategoryFormatted() })
                .Select(g => g
                    .OrderByDescending(r => r.Date)
                    .ThenByDescending(r => r.Value) // Case where Auckland appears twice for the same year because super city
                    .First()
                )
                .GroupBy(r => new { r.Selector, Measure = r.MeasureFormatted() })
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
                        r.ValueLabel = r.ValueLabel.ReplaceCaseInsensitive("Number", "%");
                        r.Value = (r.Value / total) * 100; // Multiple by 100 to stay consistent with other percentage values that are natively 100-based
                    }

                    return g;
                })
                .SelectMany(g => g)
                .Where(r =>
                {
                    // If we're excluding zero values, pull them out here
                    if (dataset.ExcludeZeroValues && r.Value == 0)
                    {
                        countExcludedByValue++;
                        return false;
                    }

                    return true;
                })
                .OrderBy(r => r.Selector);

            foreach (KeyValuePair<string, Dictionary<string, (int Index, Include Include)>> measureInclusion in measureInclusions)
            {
                ordered = ordered
                    .ThenBy(r =>
                    {
                        if (!r.Measures.TryGetValue(measureInclusion.Key, out var column))
                        {
                            return (int?) null;
                        }

                        if (!measureInclusion.Value.TryGetValue(column.Value, out var inclusion))
                        {
                            return (int?) null;
                        }

                        return inclusion.Index;
                    });
            }

            foreach (Column column in dataset.Measure)
            {
                ordered = ordered
                    .ThenBy(r => r.Measures[column.Name].Value, StringComparer.OrdinalIgnoreCase);
            }

            foreach (KeyValuePair<string, Dictionary<string, (int Index, Include Include)>> categoryInclusion in categoryInclusions)
            {
                ordered = ordered
                    .ThenBy(r =>
                    {
                        if (!r.Categories.TryGetValue(categoryInclusion.Key, out var column))
                        {
                            return (int?) null;
                        }

                        if (!categoryInclusion.Value.TryGetValue(column.Value, out var inclusion))
                        {
                            return (int?) null;
                        }

                        return inclusion.Index;
                    });
            }

            set = ordered.ToList();
            
            Console.WriteLine($" - {countRecords} records read");

            if (countMissingSelector > 0)
            {
                Console.WriteLine($" - {countMissingSelector} records missing \"selector\"");
            }

            if (countExcludedBySelector > 0)
            {
                Console.WriteLine($" - {countExcludedBySelector} records excluded by \"selector\"");
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

            if (countExcludedByValue > 0)
            {
                Console.WriteLine($" - {countExcludedByValue} records excluded by \"value\"");
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
