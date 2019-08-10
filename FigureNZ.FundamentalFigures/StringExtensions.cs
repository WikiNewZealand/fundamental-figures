using System.Threading;

namespace FigureNZ.FundamentalFigures
{
    public static class StringExtensions
    {
        public static string ToTitleCase(this string s)
        {
            return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(s);
        }
    }
}