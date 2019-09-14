using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FigureNZ.FundamentalFigures
{
    public class Dataset
    {
        public Uri Source { get; set; }

        public string Parent { get; set; }
        
        public string Selector { get; set; }

        public List<Column> Measure { get; set; }

        public List<Column> Category { get; set; }

        public string Value { get; set; }

        public string ValueUnit { get; set; }

        public string ValueLabel { get; set; }

        public string NullReason { get; set; }

        public string Date { get; set; }

        [JsonProperty("all-selectors-match-term")]
        public string AllSelectorsMatchTerm { get; set; }

        [JsonProperty("term-mapping")]
        public string TermMapping { get; set; }

        [JsonProperty("exclude-zero-values")]
        public bool ExcludeZeroValues { get; set; }

        public Dataset()
        {
            Measure = new List<Column>();
            Category = new List<Column>();
        }
    }
}