using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using Reflection.Models.Interfaces;

namespace Reflection.Models {
    public class CompareTable {
        Row MasterHeaders { get; set; }
        Row TestHeaders { get; set; }
        List<ComparedRow> Data { get; set; }
        int comparedRowsCount;
        public int ComparedRowsCount {
            get {
                if (comparedRowsCount == 0) {
                    comparedRowsCount = Data.Where(row => !row.IsPassed).Count();
                    return comparedRowsCount;
                } else {
                    return comparedRowsCount;
                }
            }
        }
        List<string> ExtraMaster { get; set; }
        public int MasterExtraCount { get { return ExtraMaster.Count; } }
        List<string> ExtraTest { get; set; }
        public int TestExtraCount { get { return ExtraTest.Count; } }
        string Delimiter { get; set; }
        int TotalColumns { get; set; }
        List<int> MasterPassedRows;
        List<int> TestPassedRows;
        ComparisonKeys ComparisonKeys { get; set; }
        bool IsExcelInstaled { get; set; }
        List<string> Headers;
        List<int> DeviationColumns;
        int RowCount = 0;
        bool IsLinearView { get; set; }
        int _deviations = 0;

        public CompareTable() {
            Data = new List<ComparedRow>();
            ExtraMaster = new List<string>();
            ExtraTest = new List<string>();
            IsExcelInstaled = Type.GetTypeFromProgID("Excel.Application") == null ? false : true;
        }

        public CompareTable(string delimiter, Row masterHeaders, Row testHeaders, int totalColumns, ComparisonKeys comparisonKeys, bool isLinearView) {
            Data = new List<ComparedRow>();
            ExtraMaster = new List<string>();
            ExtraTest = new List<string>();
            Delimiter = delimiter;
            MasterHeaders = masterHeaders;
            TestHeaders = testHeaders;
            TotalColumns = totalColumns;
            ComparisonKeys = comparisonKeys;
            MasterPassedRows = new List<int>();
            TestPassedRows = new List<int>();
            IsExcelInstaled = Type.GetTypeFromProgID("Excel.Application") == null ? false : true;
            IsLinearView = isLinearView;
        }

        public void AddComparedRows(IEnumerable<ComparedRow> comparedRows) {
            foreach (var item in comparedRows) {
                if (item.IsPassed) {
                    MasterPassedRows.Add(item.MasterRowId);
                    TestPassedRows.Add(item.TestRowId);
                } else {
                    Data.Add(item);
                }
            }
        }

        public void AddComparedRow(ComparedRow comparedRow) {
            if (comparedRow.IsPassed) {
                MasterPassedRows.Add(comparedRow.MasterRowId);
                TestPassedRows.Add(comparedRow.TestRowId);
            } else {
                Data.Add(comparedRow);
            }
        }

        public void AddMasterExtraRows(IEnumerable<Row> extraRows) {
            foreach (var item in extraRows) {
                ExtraMaster.Add(string.Join(Delimiter, item.Data));
            }
        }

        public IEnumerable<int> GetMasterComparedRowsId() {
            return Data.Select(row => row.MasterRowId).Concat(MasterPassedRows);
        }

        public IEnumerable<int> GetTestComparedRowsId() {
            return Data.Select(row => row.TestRowId).Concat(TestPassedRows);
        }

        public void AddTestExtraRows(IEnumerable<Row> extraRows) {
            foreach (var item in extraRows) {
                ExtraTest.Add(string.Join(Delimiter, item.Data));
            }
        }

        private List<string> GenerateHeadersForFile(List<int> transNo, List<int> allColumns) {
            List<string> headers = new List<string>();
            headers.Add("Defect No");
            headers.Add("Comparison Result");
            headers.Add("Diff");
            foreach (var item in transNo) {
                headers.Add("M_" + MasterHeaders[item]);
                headers.Add("T_" + MasterHeaders[item]);
            }
            foreach (var item in allColumns) {
                headers.Add(MasterHeaders[item]);
            }
            return headers;
        }

        private string[,] PrepareDataTabular() {
            var transNo = ComparisonKeys.TransactionKeys;
            var mainKeys = ComparisonKeys.MainKeys;
            DeviationColumns = Data.SelectMany(row => row.Deviations.Select(col => col.ColumnId)).Distinct().OrderBy(colId => colId).ToList();
            var allColumns = mainKeys.Concat(DeviationColumns).ToList();
            Headers = GenerateHeadersForFile(transNo, allColumns);
            int rowCount = 1 + ComparedRowsCount + ExtraMaster.Count + ExtraTest.Count;
            int columnCount = Headers.Count;
            string[,] outputArray = new string[rowCount, columnCount];
            for (int col = 0; col < Headers.Count; col++) {
                outputArray[RowCount, col] = Headers[col];
            }
            _deviations = DeviationColumns.Count;
            var rowsWithDeviations = Data.Where(row => !row.IsPassed);
            foreach (var row in rowsWithDeviations) {
                var rowToSave = new RowToSave(row.IdFields, Delimiter, DeviationColumns);
                rowToSave.FillValues(row.Deviations);
                rowToSave.SetData();
                RowCount++;
                for (int col = 0; col < rowToSave.Data.Length; col++) {
                    outputArray[RowCount, col] = rowToSave.Data[col];
                }
            }
            AddExtraRowsTabular(outputArray, "Master", ExtraMaster, transNo, allColumns);
            AddExtraRowsTabular(outputArray, "Test", ExtraTest, transNo, allColumns);
            return outputArray;
        }

        private string[,] PrepareDataLinar() {
            var transNo = ComparisonKeys.TransactionKeys;
            var mainKeys = ComparisonKeys.MainKeys;
            Headers = GenerateHeadersForFile(transNo, mainKeys);
            var rowsWithDeviations = Data.Where(row => !row.IsPassed);
            var deviations = Data.Where(row => !row.IsPassed).SelectMany(item => item.Deviations.Select(col => col.ColumnId)).Distinct().ToList();
            if (deviations.Count > 0) {
                Headers.Add("Column Name");
                Headers.Add("Master Value");
                Headers.Add("Test Value");
            }
            _deviations = deviations.Count;
            var deviationsCount = deviations.Count == 0 ? 1 : deviations.Count;
            int rowCount = rowsWithDeviations.Sum(item => item.Deviations.Count) + 1 + ((ExtraMaster.Count + ExtraTest.Count) * deviationsCount);
            int columnCount = Headers.Count;
            string[,] outputArray = new string[rowCount, columnCount];
            for (int col = 0; col < Headers.Count; col++) {
                outputArray[RowCount, col] = Headers[col];
            }
            foreach (var row in rowsWithDeviations) {
                RowToSave rowTosave = new RowToSave(row.IdFields);
                var dataSet = rowTosave.TransposeRow(row.Deviations, MasterHeaders.Data);
                for (int i = 0; i < dataSet.GetLength(0); i++) {
                    RowCount++;
                    for (int ii = 0; ii < dataSet.GetLength(1); ii++) {
                        outputArray[RowCount, ii] = dataSet[i, ii];
                    }
                }
            }
            AddExtraRowsLinear(outputArray, "Master", ExtraMaster, transNo, mainKeys, deviations);
            AddExtraRowsLinear(outputArray, "Test", ExtraTest, transNo, mainKeys, deviations);
            return outputArray;
        }

        private void AddExtraRowsLinear(string[,] outputArray, string version, List<string> extraLines, List<int> transNo, List<int> mainKeys, List<int> deviations) {
            foreach (var row in extraLines) {
                var extraRow = row.Split(new string[] { Delimiter }, StringSplitOptions.None);
                List<string> transNoPart = new List<string>();
                foreach (var colId in transNo) {
                    if (version == "Master") {
                        transNoPart.Add(extraRow[colId]);
                        transNoPart.Add("");
                    } else {
                        transNoPart.Add("");
                        transNoPart.Add(extraRow[colId]);
                    }
                }
                var idFields = GetValuesByPositions(extraRow, mainKeys);
                transNoPart.AddRange(idFields);
                RowToSave rowTosave = new RowToSave(transNoPart);
                var dataSet = rowTosave.TransposeExtraRow(GetValuesByPositions(extraRow, deviations), GetValuesByPositions(MasterHeaders.Data, deviations).ToArray(), version);
                for (int i = 0; i < dataSet.GetLength(0); i++) {
                    RowCount++;
                    for (int ii = 0; ii < dataSet.GetLength(1); ii++) {
                        outputArray[RowCount, ii] = dataSet[i, ii];
                    }
                }
            }
        }

        private void AddExtraRowsTabular(string[,] outputArray, string version, List<string> extraLines, List<int> transNo, List<int> allColumns) {
            int extraDiff = 0;
            List<string> rowToSave = new List<string>();
            foreach (var item in extraLines) {
                rowToSave.Clear();
                var extraRow = item.Split(new string[] { Delimiter }, StringSplitOptions.None);
                List<string> transNoPart = new List<string>();
                foreach (var colId in transNo) {
                    if (version == "Master") {
                        transNoPart.Add(extraRow[colId]);
                        transNoPart.Add("");
                    } else {
                        transNoPart.Add("");
                        transNoPart.Add(extraRow[colId]);
                    }
                }
                var mainRowPart = GetValuesByPositions(extraRow, allColumns);
                rowToSave.Add("");
                rowToSave.Add("Extra from " + version);
                extraDiff = extraDiff == 0 ? rowToSave.Count() : extraDiff;
                rowToSave.Add(extraDiff.ToString());
                rowToSave.AddRange(transNoPart.Concat(mainRowPart));
                AddListToTwoDimArray(outputArray, rowToSave.ToArray());
            }
        }

        private void AddListToTwoDimArray(string[,] twoDimArray, string[] list) {
            int columnCount = list.Length;
            RowCount++;
            for (int col = 0; col < columnCount; col++) {
                twoDimArray[RowCount, col] = list[col];
            }
        }

        private void SaveToFlatFile(string filePath) {
            PrepareDataTabular();
            File.WriteAllText(filePath, string.Join(Delimiter, Headers));
            StringBuilder lines = new StringBuilder();
            lines.Append(Environment.NewLine);
            var rowsWithDeviations = Data.Where(row => !row.IsPassed);
            foreach (var comparedRow in rowsWithDeviations) {
                var rowToSave = new RowToSave(comparedRow.IdFields, Delimiter, DeviationColumns);
                rowToSave.FillValues(comparedRow.Deviations);
                lines.Append(rowToSave.ToString());
            }
            File.AppendAllText(filePath, lines.ToString());
            //SaveExtraRows(filePath, transNo, allColumns, extraColumns);
        }

        public void SaveComparedRows(string filePath) {
            if (IsExcelInstaled) {
                if (IsLinearView) {
                    SaveToExcel(filePath + ".xlsx", PrepareDataLinar(), false);
                } else {
                    SaveToExcel(filePath + ".xlsx", PrepareDataTabular(), false);
                }
            } else {
                //SaveToFlatFile(filePath + ".txt");
            }
        }

        public void SaveExtraRows(string filePath, List<int> transNo, List<int> allColumns, List<int> remaining) {
            if (MasterExtraCount == 0 && TestExtraCount == 0) {
                return;
            }
            File.AppendAllText(filePath, CreateLines("Master", ExtraMaster, transNo, allColumns, remaining));
            File.AppendAllText(filePath, CreateLines("Test", ExtraTest, transNo, allColumns, remaining));
        }

        private string CreateLines(string version, List<string> extraLines, List<int> transNo, List<int> allColumns, List<int> remaining) {
            StringBuilder lines = new StringBuilder();
            int extraDiff = 0;
            foreach (var item in extraLines) {
                var row = item.Split(new string[] { Delimiter }, StringSplitOptions.None);
                List<string> transNoPart = new List<string>();
                foreach (var i in transNo) {
                    if (version == "Master") {
                        transNoPart.Add(row[i] + Delimiter);
                    } else {
                        transNoPart.Add(Delimiter + row[i]);
                    }
                }
                var mainRowPart = GetValuesByPositions(row, allColumns);
                var remainingRowPart = GetValuesByPositions(row, remaining);
                var rowToSave = transNoPart.Concat(mainRowPart.Concat(remainingRowPart));
                extraDiff = extraDiff == 0 ? rowToSave.Count() : extraDiff;
                var line = string.Join(Delimiter, rowToSave);
                lines.Append(version);
                lines.Append(Delimiter);
                lines.Append(extraDiff);
                lines.Append(Delimiter);
                lines.Append(line);
                lines.Append(Environment.NewLine);
            }
            return lines.ToString();
        }

        public List<string> GetValuesByPositions(string[] data, IEnumerable<int> positions) {
            var query = new List<string>();
            foreach (var item in positions) {
                query.Add(data[item]);
            }
            return query;
        }

        private Task<Application> CreateExcelApp() {
            return Task.Run(() => new Application());
        }

        private void SaveToExcel(string filePath, string[,] outputArray, bool isPassed) {
            Application excelApplication = null;
            Workbook workbook = null;
            Worksheet sheet = null;
            Range range = null;
            object misvalue = System.Reflection.Missing.Value;
            var columnsCount = outputArray.GetLength(1);
            try {
                excelApplication = new Application();
                excelApplication.DisplayAlerts = false;
                excelApplication.Visible = false;
                workbook = excelApplication.Workbooks.Add("");
                sheet = (Worksheet)workbook.ActiveSheet;
                sheet.Name = "Comparison";
                excelApplication.ActiveWindow.Zoom = 80;
                excelApplication.Calculation = XlCalculation.xlCalculationAutomatic;
                range = (Range)sheet.Cells[1, 1];
                int rowsCount = outputArray.GetLength(0);
                range = range.Resize[rowsCount, columnsCount];
                range.set_Value(XlRangeValueDataType.xlRangeValueDefault, outputArray);
                FormatExcelSheet(sheet, range, columnsCount, _deviations, rowsCount, isPassed);
                workbook.SaveAs(filePath, XlFileFormat.xlWorkbookDefault, Type.Missing, Type.Missing, false, false, XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                workbook.Close();
                excelApplication.Quit();
            } finally {
                if (range != null)
                    Marshal.ReleaseComObject(range);
                if (sheet != null)
                    Marshal.ReleaseComObject(sheet);
                if (workbook != null)
                    Marshal.ReleaseComObject(workbook);
                if (excelApplication != null)
                    Marshal.ReleaseComObject(excelApplication);
            }
        }

        private void FormatExcelSheet(Worksheet sheet, Range range, int columnsCount, int deviations, int rowsCount, bool isPassed) {
            //headers row to bold
            sheet.get_Range("A1", GetExcelColumnName(columnsCount) + 1).Font.Bold = true;
            //color deviation columns with red
            int deviationColumns = columnsCount - 2;
            if (DeviationColumns != null) {
                deviationColumns = (columnsCount - DeviationColumns.Count) + 1;
            }
            string condNotExplained = "";
            string condExplained = "";
            if (IsLinearView) {
                condNotExplained = "=$A2=\"\"";
                condExplained = "=$A2<>\"\"";
            } else {
                condNotExplained = "=AND($A2=\"\";" + GetExcelColumnName(deviationColumns) + "2 <>\"0\")";
                condExplained = "=\"0\"";
            }
            FormatConditions sheetFormatConditions = null;
            FormatCondition formatCondNotExplained = null;
            FormatCondition formatCondExplained = null;
            Interior interior = null;
            try {
                if (deviations != 0) {
                    sheetFormatConditions = sheet.get_Range(GetExcelColumnName(deviationColumns) + "2", GetExcelColumnName(columnsCount) + rowsCount).FormatConditions;
                    formatCondNotExplained = (FormatCondition)sheetFormatConditions.Add(XlFormatConditionType.xlExpression, Type.Missing, condNotExplained);
                    interior = formatCondNotExplained.Interior;
                    interior.Color = XlRgbColor.rgbIndianRed;
                    //color deviation columns when explained
                    formatCondExplained = (FormatCondition)sheetFormatConditions.Add(XlFormatConditionType.xlCellValue, XlFormatConditionOperator.xlNotEqual, condExplained);
                    interior = formatCondExplained.Interior;
                    interior.Color = XlRgbColor.rgbLightGreen;
                }
                //frize 1 row + add filter
                sheet.Activate();
                sheet.Application.ActiveWindow.SplitRow = 1;
                sheet.Application.ActiveWindow.FreezePanes = true;
                range = (Range)sheet.Rows[1];
                range.AutoFilter(1, Type.Missing, XlAutoFilterOperator.xlAnd, Type.Missing, true);
                //color id columns
                if (deviations != 0) {
                    range = sheet.get_Range("A2", GetExcelColumnName(deviationColumns - 1) + rowsCount);
                } else {
                    range = sheet.get_Range("A2", GetExcelColumnName(columnsCount) + rowsCount);
                }
                range.Interior.Color = XlRgbColor.rgbLightGray;
                //color header row
                range = sheet.get_Range("A1", GetExcelColumnName(columnsCount) + 1);
                range.Interior.Color = XlRgbColor.rgbBlack;
                range.Font.Color = XlRgbColor.rgbWhite;
                //autofit id columns
                if (IsLinearView) {
                    range.Columns.AutoFit();
                } else {
                    range = sheet.get_Range("A1", GetExcelColumnName(deviationColumns) + 1);
                    range.Columns.AutoFit();
                }
                //color deviation No column
                range = sheet.get_Range("A1");
                range.Interior.Color = XlRgbColor.rgbGreen;
                if (!isPassed) {
                    //color Version and Diff columns
                    range = sheet.get_Range("B1", "C1");
                    range.Interior.Color = XlRgbColor.rgbOrange;
                }
            } finally {
                if (sheetFormatConditions != null)
                    Marshal.ReleaseComObject(sheetFormatConditions);
                if (formatCondNotExplained != null)
                    Marshal.ReleaseComObject(formatCondNotExplained);
                if (formatCondExplained != null)
                    Marshal.ReleaseComObject(formatCondExplained);
                if (interior != null)
                    Marshal.ReleaseComObject(interior);
            }
        }

        private string GetExcelColumnName(int columnNumber) {
            int dividend = columnNumber;
            string columnName = string.Empty;
            int modulo;
            while (dividend > 0) {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }
            return columnName;
        }

        public void SavePassed(string path, string[,] array) {
            if (IsExcelInstaled) {
                SaveToExcel(path + ".xlsx", array, true);
            }
        }

    }
}
