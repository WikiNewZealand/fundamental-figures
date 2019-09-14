using System;
using Newtonsoft.Json;

namespace FigureNZ.FundamentalFigures.Migrator
{
    public class Dataset1
    {
        public Uri Uri { get; set; }

        public string Parent { get; set; }

        public string Term { get; set; }

        [JsonProperty("term-mapping")]
        public string TermMapping { get; set; }

        public string Discriminator { get; set; }
        
        public string Value { get; set; }

        public string ValueUnit { get; set; }

        public string ValueLabel { get; set; }

        public string NullReason { get; set; }

        [JsonProperty("exclude-zero-values")]
        public bool ExcludeZeroValues { get; set; }

        public Measure1 Measure { get; set; }

        public Category1 Category { get; set; }

        public string Date { get; set; }
    }
}