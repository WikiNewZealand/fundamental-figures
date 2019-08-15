using System;

namespace FigureNZ.FundamentalFigures
{
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

        public string NullReason { get; set; }

        public bool ConvertToPercentage { get; set; }
        
        public string MeasureFormatted()
        {
            return !string.IsNullOrWhiteSpace(Group) ? $"{MeasureLabel ?? Measure} {Separator ?? "—"} {GroupLabel ?? Group}" : $"{MeasureLabel ?? Measure}";
        }

        public string CategoryFormatted()
        {
            return $"{CategoryLabel ?? Category}";
        }

        public string UriFormatted()
        {
            return Uri.ToString().ReplaceCaseInsensitive("/download", string.Empty);
        }
    }
}