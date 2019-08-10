using System.Collections.Generic;

namespace FigureNZ.FundamentalFigures
{
    public class Group
    {
        public string Column { get; set; }

        public string Separator { get; set; }

        public List<Include> Include { get; set; }

        public List<string> Exclude { get; set; }
    }
}