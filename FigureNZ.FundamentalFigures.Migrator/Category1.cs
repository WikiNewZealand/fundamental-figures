using System.Collections.Generic;

namespace FigureNZ.FundamentalFigures.Migrator
{
    public class Category1
    {
        public string Column { get; set; }

        public List<Include> Include { get; set; }

        public List<string> Exclude { get; set; }
    }
}