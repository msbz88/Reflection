using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using Reflection.Models.Interfaces;

namespace Reflection.Models {
    public class CompareTable {
        Row MasterHeaders { get; set; }
        Row TestHeaders { get; set; }
        List<ComparedRow> Data { get; set; }
        public int ComparedRowsCount { get { return Data.Count; } }
        List<string> ExtraMaster { get; set; }
        public int MasterExtraCount { get { return ExtraMaster.Count; } }
        List<string> ExtraTest { get; set; }
        public int TestExtraCount { get { return ExtraTest.Count; } }
        string Delimiter { get; set; }
        int TotalColumns { get; set; }
        List<int> MasterPassedRows;
        List<int> TestPassedRows;
        Application ExcelApplication;
        ComparisonKeys ComparisonKeys { get; set; }
        bool IsExcelInstaled { get; set; }
        List<string> Headers;
        List<int> DeviationColumns;
        int RowCount = 0;
        bool IsLinearView { get; set; }

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
            headers.Add("Version");
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
            int rowCount = 1 + Data.Count + ExtraMaster.Count + ExtraTest.Count;
            int columnCount = Headers.Count;
            string[,] outputArray = new string[rowCount, columnCount];
            for (int col = 0; col < Headers.Count; col++) {
                outputArray[RowCount, col] = Headers[col];
            }
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
            Headers.Add("Column Name");
            Headers.Add("Master Value");
            Headers.Add("Test Value");
            int rowCount = rowsWithDeviations.Sum(item => item.Deviations.Count) + 1;
            int columnCount = 4 + Data.First().IdFields.Count + 3;
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
            return outputArray;
        }

        private void AddExtraRowsLinear(string[,] outputArray, string version, List<string> extraLines, List<int> transNo, List<int> allColumns) {
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
                rowToSave.Add(version);
                extraDiff = extraDiff == 0 ? rowToSave.Count() : extraDiff;
                rowToSave.Add(extraDiff.ToString());
                rowToSave.AddRange(transNoPart.Concat(mainRowPart));
                AddListToTwoDimArray(outputArray, rowToSave.ToArray());
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
                rowToSave.Add(version);
                extraDiff = extraDiff == 0 ? rowToSave.Count() : extraDiff;
                rowToSave.Add(extraDiff.ToString());
                rowToSave.AddRange(transNoPart.Concat(mainRowPart));
                AddListToTwoDimArray(outputArray, rowToSave.ToArray());
            }
        }

        private void AddListToTwoDimArray(string[,] twoDimArray, string[] list) {
            int columnCount = list.Length;
            for (int col = 0; col < columnCount; col++) {
                twoDimArray[RowCount, col] = list[col];
            }
            RowCount++;
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
                SaveToExcel(filePath + ".xlsx");               
            } else {
                SaveToFlatFile(filePath + ".txt");
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

        private void SaveToExcel(string filePath) {
            string[,] outputArray;
            if (IsLinearView) {
                outputArray = PrepareDataLinar();
            } else {
                outputArray = PrepareDataTabular();
            }
            ExcelApplication = new Application();
            Workbook workbook;
            Worksheet sheet;
            Range range;
            object misvalue = System.Reflection.Missing.Value;
            ExcelApplication.DisplayAlerts = false;
            ExcelApplication.Visible = false;
            workbook = ExcelApplication.Workbooks.Add("");
            sheet = (Worksheet)workbook.ActiveSheet;
            sheet.Name = "Comparison";
            ExcelApplication.ActiveWindow.Zoom = 80;                 
            int columnCount = Headers.Count;
            range = (Range)sheet.Cells[1, 1];
            range = range.Resize[RowCount, columnCount];
            range.set_Value(XlRangeValueDataType.xlRangeValueDefault, outputArray);
            FormatExcelSheet(sheet, columnCount);
            workbook.SaveAs(filePath, XlFileFormat.xlWorkbookDefault, Type.Missing, Type.Missing, false, false, XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            workbook.Close(0);
            ExcelApplication.Quit();
        }

        private void FormatExcelSheet(Worksheet sheet, int columnCount) {
            //headers row to bold
            sheet.get_Range("A1", GetExcelColumnName(Headers.Count) + 1).Font.Bold = true;
            //color deviation columns with red
            int deviationColumns = columnCount - 2;
            if (DeviationColumns != null) {
                deviationColumns = (columnCount - DeviationColumns.Count) + 1;
            }            
            string condNotExplained = "=AND($A2=\"\";" + GetExcelColumnName(deviationColumns) + "2 <>\"0\")";
            FormatConditions sheetFormatConditions = sheet.get_Range(GetExcelColumnName(deviationColumns) + "2", GetExcelColumnName(columnCount) + RowCount).FormatConditions;
            FormatCondition formatCondNotExplained = (FormatCondition)sheetFormatConditions.Add(XlFormatConditionType.xlExpression, Type.Missing, condNotExplained);
            Interior interior = formatCondNotExplained.Interior;
            interior.Color = XlRgbColor.rgbIndianRed;
            //color deviation columns when explained
            string condExplained = "=\"0\"";
            FormatCondition formatCondExplained = (FormatCondition)sheetFormatConditions.Add(XlFormatConditionType.xlCellValue, XlFormatConditionOperator.xlNotEqual, condExplained);
            interior = formatCondExplained.Interior;
            interior.Color = XlRgbColor.rgbLightGreen;
            //frize 1 row + add filter
            sheet.Activate();
            sheet.Application.ActiveWindow.SplitRow = 1;
            sheet.Application.ActiveWindow.FreezePanes = true;
            Range firstRow = (Range)sheet.Rows[1];
            firstRow.AutoFilter(1, Type.Missing, XlAutoFilterOperator.xlAnd, Type.Missing, true);
            //color id columns
            Range rng = sheet.get_Range("A2", GetExcelColumnName(deviationColumns - 1) + RowCount);
            rng.Interior.Color = XlRgbColor.rgbLightGray;
            //color header row
            Range headersExl = sheet.get_Range("A1", GetExcelColumnName(columnCount) + 1);
            headersExl.Interior.Color = XlRgbColor.rgbBlack;
            headersExl.Font.Color = XlRgbColor.rgbWhite;
            //color deviation No column
            Range def = sheet.get_Range("A1");
            def.Interior.Color = XlRgbColor.rgbGreen;
            //color Version and Diff columns
            Range tech = sheet.get_Range("B1", "C1");
            tech.Interior.Color = XlRgbColor.rgbOrange;
            //autofit id columns
            if (IsLinearView) {
                headersExl.Columns.AutoFit();
            }else {
                Range idFields = sheet.get_Range("A1", GetExcelColumnName(deviationColumns) + 1);
                idFields.Columns.AutoFit();
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



    }
}
