using System;
using System.Linq.Expressions;
using CsvHelper.Configuration;

namespace FigureNZ.FundamentalFigures
{
    public class RecordMap : ClassMap<Record>
    {
        public RecordMap Map<TMember>(Expression<Func<Record, TMember>> expression, string name)
        {
            Map(expression).Name(name);

            return this;
        }
    }
}