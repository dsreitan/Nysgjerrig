using System.Linq;

namespace Nysgjerrig
{
    public static class StringExtensions
    {
        public static string Capitalize(this string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return "";

            return str.First().ToString().ToUpper() + str.Substring(1);
        }
    }
}