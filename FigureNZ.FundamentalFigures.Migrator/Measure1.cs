using System.Collections.Generic;

namespace FigureNZ.FundamentalFigures.Migrator
{
    public class Measure1
    {
        public string Column { get; set; }

        public Group1 Group { get; set; }

        public List<Include> Include { get; set; }

        public List<string> Exclude { get; set; }
    }
}