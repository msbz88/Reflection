﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public static class Helpers {
        public static List<string> GetValuesByPositions(string[] data, IEnumerable<int> positions) {
            var values = new List<string>();
            foreach (var item in positions) {
                values.Add(data[item]);
            }
            return values;
        }

        public static List<int> GetPositionsByValues(string[] data, string[] valuesToGet) {
            var query = data.Select((val, index) => new { val, index })
                .Where(item => valuesToGet.Contains(item.val))
                .Select(item => item.index);
            return query.ToList();
        }

        public static Dictionary<int, string> NumerateSequence(string[] data) {
            Dictionary<int, string> numSequence = new Dictionary<int, string>();
            for (int i = 0; i < data.Length; i++) {
                numSequence.Add(i, data[i]);
            }
            return numSequence;
        }

        public static string CleanUpNumber(string str) {          
            if (str.Contains(',') && str.Contains('.')) {
                string strWithoutSpaces = str.Replace(" ", "");
                if (strWithoutSpaces.IndexOf(',') > strWithoutSpaces.IndexOf('.')) {
                    return strWithoutSpaces.Replace(".", "");
                } else {
                    return strWithoutSpaces.Replace(",", "");
                }
            }
            return str;
        }

        public static CultureInfo SetCultureInfo(string str) {
            if (str.Contains(',') && str.Contains('.')) {
                if (str.IndexOf(',') > str.IndexOf('.')) {
                    return new CultureInfo("da-DK");
                } else {
                    return new CultureInfo("en-GB");
                }
            }else if(str.Contains(',') && !str.Contains('.')) {
                return new CultureInfo("da-DK");
            }else {
                return new CultureInfo("en-GB");
            }           
        }
    }
}
