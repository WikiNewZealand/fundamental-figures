using System.Collections.Generic;

namespace FigureNZ.FundamentalFigures
{
    public class Column
    {
        public string Name { get; set; }

        public string Separator { get; set; }

        public List<Include> Include { get; set; }

        public List<string> Exclude { get; set; }

        public Column()
        {
            Include = new List<Include>();

            Exclude = new List<string>();
        }
    }
}
