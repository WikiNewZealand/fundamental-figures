using System.Text.RegularExpressions;
using System.Threading;

namespace FigureNZ.FundamentalFigures
{
    public static class StringExtensions
    {
        public static string ToTitleCase(this string s)
        {
            return Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(s);
        }

        // FUTURE: Added this netstandard-compatible String.Replace(string, string, comparer) for case-insensitive replaces. Remove this when we shift to dotnet core. 
        // https://stackoverflow.com/questions/244531/is-there-an-alternative-to-string-replace-that-is-case-insensitive
        // https://stackoverflow.com/a/24580455
        public static string ReplaceCaseInsensitive(this string s, string oldValue, string newValue)
        {
            return Regex.Replace(s,
                Regex.Escape(oldValue),
                Regex.Replace(newValue, "\\$[0-9]+", @"$$$0"),
                RegexOptions.IgnoreCase);
        }
    }
}