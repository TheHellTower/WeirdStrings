using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace WeirdStrings
{
    internal static class Runtime
    {
        //X = Pos Start & Y = Pos End so Start of string and End of string.
        internal static string TheHellTower(this int x, int y)
        {
            if (Assembly.GetCallingAssembly() == Assembly.GetExecutingAssembly())
            {
                //StackFrame 1 = Caller Method/Method that called this method.
                var z = new StackFrame(1).GetMethod().Name;

                if (x < 0 || x >= z.Length || y <= x || y > z.Length)
                    throw new ArgumentException("Invalid X & Y.");

                return Encoding.UTF8.GetString(Convert.FromBase64String(z.Substring(x, y - x).Replace("*", "="))); //here we are only taking from start to end to get the right string.
            }
            return string.Join("", new string[] { "T", "h", "e", "H", "e", "l", "l", "T", "o", "w", "e", "r", ",", " ", "B", "a", "s", "i", "c", " ", "A", "n", "t", "i", "-", "I", "n", "v", "o", "k", "e", " ", "s", "o", " ", "y", "o", "u", " ", "s", "h", "o", "u", "l", "d", " ", "b", "e", " ", "o", "k", "a", "y", " ", "!", " ", ":", "'", ")" });
        }
    }
}