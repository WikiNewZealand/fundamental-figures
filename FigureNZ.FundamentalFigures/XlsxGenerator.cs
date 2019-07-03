﻿using System;
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
                        using (HttpResponseMessage response = await client.GetAsync(dataset.Uri))
                        {
                            response.EnsureSuccessStatusCode();

                            using (StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync(), Encoding.UTF8))
                            using (CsvReader csv = new CsvReader(reader))
                            {
                                csv.Configuration.HeaderValidated = null;
                                csv.Configuration.MissingFieldFound = null;
                                csv.Configuration.RegisterClassMap(new RecordMap()
                                    .Map(r => r.TerritorialAuthority, "Territorial Authority")
                                    .Map(r => r.Date, dataset.Date)
                                    .Map(r => r.Measure, dataset.Measure.Column)
                                    .Map(r => r.Group, dataset.Measure.Group)
                                    .Map(r => r.Category, dataset.Category.Column)
                                    .Map(r => r.Value, "Value")
                                    .Map(r => r.ValueUnit, "Value Unit")
                                    .Map(r => r.ValueLabel, "Value Label")
                                );

                                bool hasMeasureExclusions = dataset.Measure.Exclude != null && dataset.Measure.Exclude.Any();
                                HashSet<string> measureExclusions = new HashSet<string>(dataset.Measure.Exclude ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

                                bool hasMeasureInclusions = dataset.Measure.Include != null && dataset.Measure.Include.Any();
                                HashSet<string> measureInclusions = new HashSet<string>(dataset.Measure.Include?.Select(i => i.Value) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

                                bool hasCategoryExclusions = dataset.Category.Exclude != null && dataset.Category.Exclude.Any();
                                HashSet<string> categoryExclusions = new HashSet<string>(dataset.Category.Exclude ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

                                bool hasCategoryInclusions = dataset.Category.Include != null && dataset.Category.Include.Any();
                                HashSet<string> categoryInclusions = new HashSet<string>(dataset.Category.Include?.Select(i => i.Value) ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

                                List<Record> set = new List<Record>();

                                foreach (Record r in csv.GetRecords<Record>())
                                {
                                    if (!r.TerritorialAuthority.Equals(term, StringComparison.OrdinalIgnoreCase))
                                    {
                                        continue;
                                    }

                                    if (hasMeasureExclusions && measureExclusions.Contains(r.Measure))
                                    {
                                        continue;
                                    }

                                    if (hasMeasureInclusions && !measureInclusions.Contains(r.Measure))
                                    {
                                        continue;
                                    }

                                    if (hasCategoryExclusions && categoryExclusions.Contains(r.Category))
                                    {
                                        continue;
                                    }

                                    if (hasCategoryInclusions && !categoryInclusions.Contains(r.Category))
                                    {
                                        continue;
                                    }

                                    r.Parent = dataset.Parent;
                                    r.Uri = dataset.Uri;

                                    Include measure = dataset.Measure.Include?.FirstOrDefault(i => i.Value.Equals(r.Measure, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(i.Label));

                                    if (measure != null)
                                    {
                                        r.MeasureLabel = measure.Label;
                                    }

                                    Include category = dataset.Category.Include?.FirstOrDefault(i => i.Value.Equals(r.Category, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(i.Label));

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
                                    .OrderBy(r => dataset.Measure.Include?.FindIndex(i => i.Value.Equals(r.Measure, StringComparison.OrdinalIgnoreCase)))
                                    .ThenBy(r => dataset.Category.Include?.FindIndex(i => i.Value.Equals(r.Category, StringComparison.OrdinalIgnoreCase)))
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
                                    worksheet.Cells[row, 6].Value = record.Uri.ToString().Replace("/download", string.Empty, StringComparison.OrdinalIgnoreCase);

                                    row++;
                                }

                                // Add a blank row between datasets
                                measureLabel = null;
                                row++;
                            }
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

        public string TerritorialAuthority { get; set; }

        public string Date { get; set; }

        public string Measure { get; set; }

        public string MeasureLabel { get; set; }

        public string Group { get; set; }

        public string Category { get; set; }

        public string CategoryLabel { get; set; }

        public long? Value { get; set; }

        public string ValueUnit { get; set; }

        public string ValueLabel { get; set; }

        public string MeasureFormatted()
        {
            return !string.IsNullOrWhiteSpace(Group) ? $"{MeasureLabel ?? Measure} by {Group}" : $"{MeasureLabel ?? Measure}";
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

        public Measure Measure { get; set; }

        public Category Category { get; set; }

        public string Date { get; set; }
    }

    public class Measure
    {
        public string Column { get; set; }

        public string Group { get; set; }

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