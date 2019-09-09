using System;
using Newtonsoft.Json;

namespace FigureNZ.FundamentalFigures
{
    public class Dataset
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

        public Measure Measure { get; set; }

        public Category Category { get; set; }

        public string Date { get; set; }
    }
}