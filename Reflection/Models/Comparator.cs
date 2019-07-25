using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class Comparator {
         ComparisonKeys IdColumns { get; set; }

        public Comparator(ComparisonKeys idColumns) {
            IdColumns = idColumns;
        }

        public ComparedRow Compare(List<ComparedRow> allCombinations, Row masterRow, Row testRow, ref int minDeviations) {
            ComparedRow comparedRow = new ComparedRow(masterRow.Id, testRow.Id);
            int currentDeviations = 0;
            for (int i = 0; i < masterRow.Data.Length; i++) {
                if (!IdColumns.TransactionKeys.Contains(i) && !IdColumns.ExcludeColumns.Contains(i)) {
                    if (masterRow.Data[i] != testRow.Data[i]) {
                        currentDeviations++;
                        if (currentDeviations <= minDeviations) {
                            var deviation = new Deviation(i, masterRow.Data[i], testRow.Data[i]);
                            comparedRow.AddDeviation(deviation);
                        } else {
                            return null;
                        }
                    }
                }
            }
            if (comparedRow.Deviations.Count > 0) {
                var prevBooked = allCombinations.Where(row => row.TestRowId == testRow.Id).FirstOrDefault();
                if (prevBooked != null) {
                    int countPrevResult = prevBooked.Deviations.Count;
                    if (countPrevResult > currentDeviations) {
                        allCombinations.Remove(prevBooked);
                    } else if (currentDeviations > countPrevResult) {
                        return null;
                    }
                }
                comparedRow.AddIdFields(GetIdFields(masterRow, testRow));
                minDeviations = currentDeviations;
                return comparedRow;
            } else {
                comparedRow.IsPassed = true;
                return comparedRow;
            }
        }

        public ComparedRow CompareSingle(Row masterRow, Row testRow) {
            ComparedRow comparedRow = new ComparedRow(masterRow.Id, testRow.Id);
            for (int i = 0; i < masterRow.Data.Length; i++) {
                if (!IdColumns.TransactionKeys.Contains(i) && !IdColumns.ExcludeColumns.Contains(i)) {
                    if (masterRow.Data[i] != testRow.Data[i]) {
                        var deviation = new Deviation(i, masterRow.Data[i], testRow.Data[i]);
                        comparedRow.AddDeviation(deviation);
                    }
                }
            }
            if (comparedRow.Deviations.Count > 0) {
                comparedRow.AddIdFields(GetIdFields(masterRow, testRow));
                return comparedRow;
            } else {
                comparedRow.IsPassed = true;
                return comparedRow;
            }
        }

        private List<IdField> GetIdFields(Row masterRow, Row testRow) {
            List<IdField> idFields = new List<IdField>();
            foreach (var item in IdColumns.TransactionKeys) {
                IdField printFields = new IdField();
                printFields.TransactionNo = item;
                printFields.MasterTransactionNoVal = masterRow.Data[item];
                printFields.TestTransactionNoVal = testRow.Data[item];
                idFields.Add(printFields);
            }
            foreach (var item in IdColumns.MainKeys) {
                IdField printFields = new IdField();
                printFields.MainKey = item;
                printFields.MainVal = masterRow.Data[item];
                idFields.Add(printFields);
            }
            return idFields;
        }
    }
}
