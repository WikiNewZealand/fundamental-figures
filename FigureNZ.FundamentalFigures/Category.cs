using System.Collections.Generic;

namespace FigureNZ.FundamentalFigures
{
    public class Category
    {
        public string Column { get; set; }

        public List<Include> Include { get; set; }

        public List<string> Exclude { get; set; }
    }
}