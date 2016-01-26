using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Generate_Cnblogs_Articles_To_Markdown_Files
{
    public class StringHelper
    {
        public static string ConvertUnicode(string str)
        {
            var outStr = "";
            var reg = new Regex(@"(?i)\\u([0-9a-f]{4})");
            outStr = reg.Replace(str,
                m1 => ((char) Convert.ToInt32(m1.Groups[1].Value, 16)).ToString());
            return outStr;
        }
    }
}
