using System.Collections.Generic;

namespace FigureNZ.FundamentalFigures
{
    public class Measure
    {
        public string Column { get; set; }

        public Group Group { get; set; }

        public List<Include> Include { get; set; }

        public List<string> Exclude { get; set; }
    }
}