using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosellaInterpreter
{
    public static class StringExtensions
    {
        public static string JavaSubstring(this string text, int from, int to) => text.Substring(from, to - from);
    }
}
