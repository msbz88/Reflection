using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Reflection.Models {
    public static class Splitter {
        public static string[] Split(string str, char[] delimiters) {
            if(delimiters[0] == ',' || delimiters[0] == ';') {
                return SplitQuoted(str, delimiters[0]);
            }else {
                return str.Split(delimiters);
            }          
        }

        private static string[] SplitQuoted(string str, char delimiter) {
            Regex pattern = new Regex("(?:^|" + delimiter + ")(\"(?:[^\"])*\"|[^" + delimiter + "]*)");
            List<string> list = new List<string>();
            string curr = null;
            foreach (Match match in pattern.Matches(str)) {
                curr = match.Value;
                if (0 == curr.Length) {
                    list.Add("");
                }
                list.Add(curr.TrimStart(delimiter));
            }
            return list.ToArray();
        }
    }
}
