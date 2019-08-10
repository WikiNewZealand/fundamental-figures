using System.Collections.Generic;

namespace FigureNZ.FundamentalFigures
{
    public class Figure
    {
        public List<Dataset> Datasets { get; set; }

        public string InputPath { get; set; }

        public string OutputPath { get; set; }

        public Figure()
        {
            InputPath = @"./csv";
            OutputPath = @"./xlsx";
        }
    }
}