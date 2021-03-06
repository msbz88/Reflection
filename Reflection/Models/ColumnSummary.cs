﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class ColumnSummary {
        private int UniqCount { get; set; }
        public int UniqMatchCount { get; set; }
        public int ColumnId { get; set; }
        ///<summary>Percentage of equal values</summary>
        public double MatchingRate { get; set; }
        ///<summary>Percentage of distinct values</summary>
        public double UniquenessRate { get; set; }
        ///<summary>Percentage of equal values to total values in column</summary>
        public double UniqMatchRate { get; set; }
        public double UniqDistinctMatchRate { get; set; }
        public bool IsString { get; set; }
        public bool IsDouble { get; set; }
        public bool HasNulls { get; set; }
        public bool IsNumber { get; set; }
        public bool IsTransNo { get; set; }
        public bool IsDate { get; set; }
        //public bool IsIK { get; set; }
        public bool IsTimestamp { get; set; }

        public ColumnSummary(int id, int totalRowsCount, HashSet<string> masterUniqVals, HashSet<string> testUniqVals) {
            UniqMatchCount = masterUniqVals.Intersect(testUniqVals).Count();
            UniqCount = masterUniqVals.Count > testUniqVals.Count ? testUniqVals.Count : masterUniqVals.Count;
            ColumnId = id;
            MatchingRate = CalculateRate(masterUniqVals.Count, testUniqVals.Count, UniqMatchCount);
            UniquenessRate = CalculatePercentage(UniqCount, totalRowsCount);
            UniqMatchRate = CalculatePercentage(UniqMatchCount, totalRowsCount);
            UniqDistinctMatchRate = CalculatePercentage(UniqMatchCount, UniqCount);
            HasNulls = CheckIfHasNulls(masterUniqVals);
            IsNumber = CheckIfNumeric(masterUniqVals);
            IsDouble = IsNumber ? false : CheckIfDouble(masterUniqVals);
            IsString = IsDouble || IsNumber ? false : true;
            IsTransNo = CheckIfTransNo(masterUniqVals);
            //IsIK = IsTransNo ? false : CheckIfIK(masterUniqVals);
            if (!IsTransNo && !IsNumber) {
                IsTimestamp = CheckIfTimestamp(masterUniqVals);
            }
            IsDate = ChechIfDate(masterUniqVals);
            if (IsDate) {
                IsNumber = false;
            }
        }

        private bool CheckIfDouble(HashSet<string> columnData) {
            if (columnData.Count == 1 && columnData.First() == null) {
                return false;
            }
            double d;
            var clearSeq = columnData.Where(item => item != "" && item.ToUpper() != "NULL");
            return clearSeq.Any() ? clearSeq.All(item => double.TryParse(Helpers.CleanUpNumber(item), out d)) : false;
        }


        private bool ChechIfDate(HashSet<string> columnData) {
            if (columnData.Count == 1 && columnData.First() == null) {
                return false;
            } else if (!IsNumber) {
                return false;
            }else {
                DateTime date;
                string[] format = new string[] {
                "yyyyMMdd", "yyyy-MM-dd", "yyyy/MM/dd", "yyyy.MM.dd",
                "MMddyyyy", "MM-dd-yyyy","MM/dd/yyyy", "MM.dd.yyyy",
                "ddMMyyyy", "dd-MM-yyyy","dd/MM/yyyy", "dd.MM.yyyy"};
                return columnData.All(item => DateTime.TryParseExact(item, format, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out date));
            }
        }

        private bool CheckIfHasNulls(HashSet<string> columnData) {
            if(columnData.Count == 1 && columnData.First() == null) {
                return true;
            }
            return columnData.Any(item => item == "" || item.ToUpper() == "NULL");
        }

        private bool CheckIfNumeric(HashSet<string> columnData) {
            if (columnData.Count == 1 && columnData.First() == null) {
                return false;
            }
            int n = 0;
            long l = 0;
            var clearSeq = columnData.Where(item => item != "" && item.ToUpper() != "NULL").Select(item => Helpers.CleanUpNumber(item));
            return clearSeq.Any() ? clearSeq.All(item => int.TryParse(item, out n) || long.TryParse(item, out l)) : false;
        }

        private double CalculatePercentage(int x, int y) {
            return Math.Round(((double)x / y) * 100, 2);
        }

        private double CalculateRate(int uniqueRowsMaster, int uniqueRowsTest, int matchedValues) {
            if (matchedValues == 0) {
                return 0;
            } else {
                double finalRate = 0;
                var higherNumber = uniqueRowsMaster > uniqueRowsTest ? uniqueRowsMaster : uniqueRowsTest;
                finalRate = ((double)matchedValues / higherNumber) * 100;
                return Math.Round(finalRate, 2);
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append(ColumnId);
            sb.Append(";");
            sb.Append(MatchingRate);
            sb.Append(";");
            sb.Append(UniquenessRate);
            sb.Append(";");
            sb.Append(UniqMatchRate);
            sb.Append(";");
            sb.Append(IsDouble);
            sb.Append(";");
            sb.Append(IsString);
            sb.Append(";");
            sb.Append(HasNulls);
            sb.Append(";");
            sb.Append(IsTransNo);
            sb.Append(";");
            sb.Append(UniqDistinctMatchRate);
            sb.Append(";");
            sb.Append(UniqMatchCount);
            sb.Append(";");
            sb.Append(UniqCount);
            return sb.ToString();
        }

        private bool CheckIfTransNo(HashSet<string> columnData) {
            if (IsNumber && !HasNulls) {
                foreach (var item in columnData) {
                    long n = 0;
                    long.TryParse(item, out n);
                    if (n == 0) {
                        return false;
                    } else if (n < 0) {
                        return false;
                    } else if (Math.Floor(Math.Log10(n) + 1) != 14) {
                        return false;
                    }
                }
                return true;
            } else {
                return false;
            }
        }

        //not accurate, hard to identify IK 
        private bool CheckIfIK(HashSet<string> columnData) {
            if (IsNumber && !HasNulls) {
                var firstVal = columnData.FirstOrDefault();
                long l = 0;
                long.TryParse(firstVal, out l);
                var len = Math.Floor(Math.Log10(l) + 1);
                if (len == 8) {
                    return false;
                }
                foreach (var item in columnData) {
                    long n = 0;
                    long.TryParse(item, out n);
                    if (n == 0) {
                        return false;
                    } else if (n < 0) {
                        return false;
                    } else if (Math.Floor(Math.Log10(n) + 1) != len) {
                        return false;
                    }
                }
                return true;
            } else {
                return false;
            }
        }

        private bool CheckIfTimestamp(HashSet<string> columnData) {
            if (columnData.Count == 1 && columnData.First() == null) {
                return false;
            }
            string[] format = new string[] {
                "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd HH:mm:ss", "yyyy.MM.dd HH:mm:ss",
                "MM-dd-yyyy HH:mm:ss","MM/dd/yyyy HH:mm:ss", "MM.dd.yyyy HH:mm:ss",
                "dd-MM-yyyy HH:mm:ss","dd/MM/yyyy HH:mm:ss", "dd.MM.yyyy HH:mm:ss",
                "yyyy-MM-dd HH:mm", "yyyy/MM/dd HH:mm", "yyyy.MM.dd HH:mm",
                "MM-dd-yyyy HH:mm","MM/dd/yyyy HH:mm", "MM.dd.yyyy HH:mm",
                "dd-MM-yyyy HH:mm","dd/MM/yyyy HH:mm", "dd.MM.yyyy HH:mm",
                "dd-MM-yyyy H:mm:ss","dd/MM/yyyy H:mm:ss", "dd.MM.yyyy H:mm:ss"
            };
            DateTime result;
            foreach (var item in columnData) {
                if(item == "" && columnData.Count == 1) {
                    return false;
                }else if (item == "" || item == "0") {
                    continue;
                }
                var str = item.ToLower().Replace(" am", "").Replace(" pm","");
                if (!DateTime.TryParseExact(str, format, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out result)) {
                    if (IsDouble && str.Length > 8) {
                        string[] format2 = new string[] { "yyyyMMdd" };
                        var withoutTrail = str.Substring(0, 8);
                        if (!DateTime.TryParseExact(withoutTrail, format2, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out result) && str != "") {
                            return false;
                        }
                    }else {
                        return false;
                    }
                }              
            }
            return true;
        }

    }
}
