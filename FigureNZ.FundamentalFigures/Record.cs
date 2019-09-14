using System;
using System.Collections.Generic;
using System.Linq;

namespace FigureNZ.FundamentalFigures
{
    public class Record
    {
        public string Parent { get; set; }

        public Uri Uri { get; set; }

        public string Selector { get; set; }

        public string Date { get; set; }

        public string DateLabel { get; set; }

        public Dictionary<string, ColumnValue> Measures { get; set; }

        public Dictionary<string, ColumnValue> Categories { get; set; }

        public decimal? Value { get; set; }

        public string ValueUnit { get; set; }

        public string ValueLabel { get; set; }

        public string NullReason { get; set; }

        public bool ConvertToPercentage { get; set; }
        
        public string MeasureFormatted()
        {
            return string.Join(string.Empty, Measures.OrderBy(m => m.Value.Index).Select((m, i) =>
            {
                string measure = m.Value.Label ?? m.Value.Value;

                if (Measures.Count > 1 && i < (Measures.Count - 1))
                {
                    measure = measure + m.Value.Separator;
                }

                return measure;
            }));
        }

        public string CategoryFormatted()
        {
            return string.Join(string.Empty, Categories.OrderBy(c => c.Value.Index).Select((c, i) =>
            {
                string category = c.Value.Label ?? c.Value.Value;

                if (category.StartsWith("*-", StringComparison.OrdinalIgnoreCase))
                {
                    category = category.Remove(0, 2) + " and under";
                }

                if (category.EndsWith("-*", StringComparison.OrdinalIgnoreCase))
                {
                    category = category.Remove(category.Length - 2, 2) + " and over";
                }

                if (Categories.Count > 1 && i < (Categories.Count - 1))
                {
                    category = category + c.Value.Separator;
                }

                return category;
            }));
        }

        public string UriFormatted()
        {
            return Uri.ToString().ReplaceCaseInsensitive("/download", string.Empty);
        }
    }
}