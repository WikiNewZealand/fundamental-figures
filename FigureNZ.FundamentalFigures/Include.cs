using Newtonsoft.Json;

namespace FigureNZ.FundamentalFigures
{
    public class Include
    {
        public string Value { get; set; }

        public string Label { get; set; }

        [JsonProperty("convert-to-percentage")]
        public bool ConvertToPercentage { get; set; }
    }
}