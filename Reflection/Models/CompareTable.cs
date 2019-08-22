using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using Oracle.ManagedDataAccess.Client;
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
        List<ComparedRow> PassedRows;
        bool IsExcelInstaled { get; set; }
        List<string> Headers;
        List<int> DeviationColumns;
        private int RowCount { get; set; }
        Dictionary<int, string> NumberedColumnNames { get; set; }
        DefectsSearch DefectsSearch { get; set; }
        OraSession OraSession { get; set; }
        ComparisonTask ComparisonTask;
        List<int> BinaryValues;
        List<int> MainIdColumns;

        public CompareTable(Row masterHeaders, Row testHeaders, ComparisonTask comparisonTask) {
            Data = new List<ComparedRow>();
            ExtraMaster = new List<string>();
            ExtraTest = new List<string>();
            MasterHeaders = masterHeaders;
            TestHeaders = testHeaders;
            PassedRows = new List<ComparedRow>();
            IsExcelInstaled = Type.GetTypeFromProgID("Excel.Application") == null ? false : true;
            NumberedColumnNames = NumberAllColumnNames(masterHeaders.Data);
            ComparisonTask = comparisonTask;
            Delimiter = ComparisonTask.MasterConfiguration.Delimiter;
        }

        private void SetIdColumns() {
            BinaryValues = ComparisonTask.ComparisonKeys.BinaryValues.Concat(ComparisonTask.ComparisonKeys.UserIdColumnsBinary).Distinct().ToList();
            MainIdColumns = ComparisonTask.ComparisonKeys.MainKeys.Concat(ComparisonTask.ComparisonKeys.UserIdColumns).Distinct().ToList();
        }

        public void AddComparedRows(IEnumerable<ComparedRow> comparedRows) {
            foreach (var item in comparedRows) {
                if (item.IsPassed) {
                    PassedRows.Add(item);
                } else {
                    Data.Add(item);
                }
            }
        }

        public void AddComparedRow(ComparedRow comparedRow) {
            if (comparedRow.IsPassed) {
                PassedRows.Add(comparedRow);
            } else {
                Data.Add(comparedRow);
            }
        }

        public void AddMasterExtraRows(IEnumerable<Row> extraRows) {
            foreach (var item in extraRows) {
                ExtraMaster.Add(string.Join(Delimiter, item.Data));
            }
        }

        public void AddMasterExtraRows(IEnumerable<string> extraRows) {
            ExtraMaster.AddRange(extraRows);
        }

        public IEnumerable<int> GetMasterComparedRowsId() {
            return Data.Select(row => row.MasterRowId).Concat(PassedRows.Select(row=>row.MasterRowId));
        }

        public IEnumerable<int> GetTestComparedRowsId() {
            return Data.Select(row => row.TestRowId).Concat(PassedRows.Select(row=>row.TestRowId));
        }

        public void AddTestExtraRows(IEnumerable<Row> extraRows) {
            foreach (var item in extraRows) {
                ExtraTest.Add(string.Join(Delimiter, item.Data));
            }
        }

        public void AddTestExtraRows(IEnumerable<string> extraRows) {
            ExtraTest.AddRange(extraRows);
        }

        public void SaveComparedRows(string filePath) {
            SetIdColumns();
            string[,] dataToSave = null;
            string fileExt = "";
            int rowsCount = 1;
            if (!ComparisonTask.IsDeviationsOnly) {
                rowsCount += ComparisonTask.ExceptedRecords + PassedRows.Count;
            }
            if (ComparisonTask.IsLinearView) {
                rowsCount += Data.Sum(row => row.Deviations.Count) + ExtraMaster.Count + ExtraTest.Count;
                var projectName = GetProjectName(filePath);
                var upgradeName = GetUpgradeName(filePath);
                OraSession = StartSession();
                DefectsSearch = new DefectsSearch(projectName, upgradeName, OraSession);
                dataToSave = PrepareDataLinar(rowsCount);
                fileExt = ".xlsm";
            } else {
                rowsCount += ComparedRowsCount + ExtraMaster.Count + ExtraTest.Count;
                dataToSave = PrepareDataTabular(rowsCount);
                fileExt = ".xlsx";
            }
            if (!ComparisonTask.IsDeviationsOnly) {
                AddExceptedRecords(dataToSave);
                AddPassedRows(dataToSave);
            }
            if (!IsExcelInstaled || dataToSave.GetLength(0) > 1000000) {
                SaveToFlatFile(filePath + ".txt", dataToSave);
                ComparisonTask.IsToExcelSaved = false;
            } else {
                SaveToExcel(filePath + fileExt, dataToSave, false);
                ComparisonTask.IsToExcelSaved = true;
            }
            dataToSave = null;
        }

        private void AddPassedRows(string[,] dataToSave) {
            foreach (var row in PassedRows) {
                var rowToSave = new RowToSave(row);
                var result = rowToSave.PreparePassedRow();
                ComparisonTask.IfCancelRequested();
                RowCount++;
                for (int col = 0; col < result.Count; col++) {
                    dataToSave[RowCount, col] = result[col];
                }
            }
        }

        private void AddExceptedRecords(string[,] dataToSave) {
            var exceptedRecords = File.ReadLines(ComparisonTask.CommonDirectoryPath + "\\Passed.temp");
            int rowsIndex = 0;
            foreach (var masterLine in exceptedRecords) {
                var parsedRow = masterLine.Split(new string[] { Delimiter }, StringSplitOptions.None);
                var rowToSave = new RowToSave(parsedRow);         
                var result = rowToSave.PrepareExceptedRow(BinaryValues, MainIdColumns);
                ComparisonTask.IfCancelRequested();
                RowCount++;
                for (int col = 0; col < result.Count; col++) {
                    dataToSave[RowCount, col] = result[col];
                }
                rowsIndex++;
            }
            File.Delete(ComparisonTask.CommonDirectoryPath + "\\Passed.temp");
        }

        public void SavePassed(string filePath, string delimiter, string[,] array) {
            if (!IsExcelInstaled || array.GetLength(0) > 1000000) {
                Delimiter = delimiter;
                SaveToFlatFile(filePath + ".txt", array);
                ComparisonTask.IsToExcelSaved = false;
            } else {
                SaveToExcel(filePath + ".xlsx", array, true);
                ComparisonTask.IsToExcelSaved = true;
            }
        }

        private string[,] PrepareDataTabular(int rowsCount) {
            DeviationColumns = Data.SelectMany(row => row.Deviations.Select(col => col.ColumnId)).Distinct().OrderBy(colId => colId).ToList();
            var allColumns = MainIdColumns.Concat(DeviationColumns).ToList();
            Headers = GenerateHeadersForFile(BinaryValues, allColumns);
            int columnCount = Headers.Count;
            string[,] dataToSave = new string[rowsCount, columnCount];
            for (int col = 0; col < Headers.Count; col++) {
                dataToSave[RowCount, col] = Headers[col];
            }
            var rowsWithDeviations = Data.Where(row => !row.IsPassed);           
            foreach (var comparedRow in rowsWithDeviations) {
                RowToSave rowToSave = new RowToSave(comparedRow);
                var result = rowToSave.PrepareRowTabular(NumberedColumnNames, DefectsSearch, DeviationColumns);
                ComparisonTask.IfCancelRequested();
                ComparisonTask.UpdateProgress(10 / (double)rowsCount);
                RowCount++;
                for (int col = 0; col < result.Count; col++) {
                    dataToSave[RowCount, col] = result[col];
                }
            }
            AddExtraRows(dataToSave, "Master", ExtraMaster, BinaryValues, MainIdColumns);
            AddExtraRows(dataToSave, "Test", ExtraTest, BinaryValues, MainIdColumns);
            return dataToSave;
        }

        private string[,] PrepareDataLinar(int rowsCount) {
            Headers = GenerateHeadersForFile(BinaryValues, MainIdColumns);           
            Headers.Add("Column Name");
            Headers.Add("Master Value");
            Headers.Add("Test Value");
            var rowsWithDeviations = Data.Where(row => !row.IsPassed);
            int columnCount = Headers.Count;
            string[,]  dataToSave = new string[rowsCount, columnCount];
            for (int col = 0; col < Headers.Count; col++) {
                dataToSave[RowCount, col] = Headers[col];
            }
            foreach (var comparedRow in rowsWithDeviations) {
                RowToSave rowToSave = new RowToSave(comparedRow);
                var result = rowToSave.PrepareRowLinear(NumberedColumnNames, DefectsSearch);
                ComparisonTask.IfCancelRequested();
                ComparisonTask.UpdateProgress(10 / (double)rowsCount);               
                foreach (var row in result) {
                    RowCount++;
                    int index = 0;
                    foreach (var col in row) {
                        dataToSave[RowCount, index++] = col;
                    }
                }
            }
            OraSession.CloseConnection();
            AddExtraRows(dataToSave, "Master", ExtraMaster, BinaryValues, MainIdColumns);
            AddExtraRows(dataToSave, "Test", ExtraTest, BinaryValues, MainIdColumns);
            return dataToSave;
        }

        private void SaveToFlatFile(string filePath, string[,] values) {
            int numRows = values.GetUpperBound(0) + 1;
            int numCols = values.GetUpperBound(1) + 1;
            StringBuilder sb = new StringBuilder();
            for (int row = 0; row < numRows; row++) {
                ComparisonTask.IfCancelRequested();
                ComparisonTask.UpdateProgress(10 / (double)numRows);
                sb.Append(values[row, 0]);
                for (int col = 1; col < numCols; col++)
                    sb.Append(Delimiter + values[row, col]);
                sb.AppendLine();
            }
            File.WriteAllText(filePath, sb.ToString());
        }

        private void AddExtraRows(string[,] dataToSave, string version, List<string> extraLines, List<int> binaryValues, List<int> mainIdColumns) {
            foreach (var extraRow in extraLines) {
                var parsedRow = extraRow.Split(new string[] { Delimiter }, StringSplitOptions.None);
                RowToSave rowToSave = new RowToSave(parsedRow);
                var result = rowToSave.PrepareExtraRow(version, binaryValues, mainIdColumns);
                ComparisonTask.IfCancelRequested();
                RowCount++;
                for (int col = 0; col < result.Count; col++) {
                    dataToSave[RowCount, col] = result[col];
                }
            }           
        }

        private List<string> GenerateHeadersForFile(List<int> binaryValues, List<int> allColumns) {
            List<string> headers = new List<string>();
            headers.Add("Defect No/Explanation");
            headers.Add("Comparison Result");
            headers.Add("Diff");
            foreach (var item in binaryValues) {
                headers.Add("M_" + MasterHeaders[item]);
                headers.Add("T_" + MasterHeaders[item]);
            }
            foreach (var item in allColumns) {
                headers.Add(MasterHeaders[item]);
            }
            return headers;
        }

        private Dictionary<int, string> NumberAllColumnNames(string[] columnNames) {
            Dictionary<int, string> result = new Dictionary<int, string>();
            for (int i = 0; i < columnNames.Length; i++) {
                result.Add(i, columnNames[i]);
            }
            return result;
        }

        private OraSession StartSession() {
            var oraSession = new OraSession("*", "*", "*", "*", "*");
            oraSession.OpenConnection();
            return oraSession;
        }

        private string GetProjectName(string filePath) {
            var r = filePath.Split('\\');
            if (r.Length > 3) {
                return r[2].Trim();
            }else {
                return "";
            }
        }

        private string GetUpgradeName(string filePath) {
            var r = filePath.Split('\\');
            if (r.Length > 3) {              
                return r[3].Replace("'", "").Trim();
            }else {
                return "";
            }            
        }

        private void SaveToExcel(string filePath, string[,] outputArray, bool isPassed) {
            Application excelApplication = null;
            Workbook workbook = null;
            Worksheet sheet = null;
            Range range = null;
            var columnsCount = outputArray.GetLength(1);
            try {
                excelApplication = new Application();
                ComparisonTask.UpdateProgress(3);
                excelApplication.DisplayAlerts = false;
                excelApplication.Visible = false;
                if (ComparisonTask.IsLinearView && !isPassed) {
                    //pass=#Reflection888
                    workbook = excelApplication.Workbooks.Open(@"O:\DATA\COMMON\core\data\template.xlsm");
                }else {
                    workbook = excelApplication.Workbooks.Add("");
                }               
                sheet = (Worksheet)workbook.ActiveSheet;
                sheet.Name = "Comparison";
                excelApplication.ActiveWindow.Zoom = 80;
                excelApplication.Calculation = XlCalculation.xlCalculationAutomatic;
                range = (Range)sheet.Cells[1, 1];
                int rowsCount = outputArray.GetLength(0);
                range = range.Resize[rowsCount, columnsCount];
                range.set_Value(XlRangeValueDataType.xlRangeValueDefault, outputArray);
                ComparisonTask.UpdateProgress(2);
                FormatExcelSheet(sheet, range, columnsCount, rowsCount, isPassed);               
                workbook.SaveAs(filePath, Type.Missing, Type.Missing, Type.Missing, false, false, XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                workbook.Close();
                excelApplication.Quit();
            }catch(Exception ex) {
                if (range != null)
                    Marshal.ReleaseComObject(range);
                if (sheet != null)
                    Marshal.ReleaseComObject(sheet);
                if (workbook != null)
                    Marshal.ReleaseComObject(workbook);
                if (excelApplication != null)
                    Marshal.ReleaseComObject(excelApplication);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                throw new Exception(ex.Message);
            } 
            finally {
                if (range != null)
                    Marshal.ReleaseComObject(range);
                if (sheet != null)
                    Marshal.ReleaseComObject(sheet);
                if (workbook != null)
                    Marshal.ReleaseComObject(workbook);
                if (excelApplication != null)
                    Marshal.ReleaseComObject(excelApplication);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

        }

        private void FormatExcelSheet(Worksheet sheet, Range range, int columnsCount, int rowsCount, bool isPassed) {
            //headers row to bold
            sheet.get_Range("A1", GetExcelColumnName(columnsCount) + 1).Font.Bold = true;
            //color deviation columns with red
            int deviationColumns = columnsCount - 2;
            if (DeviationColumns != null) {
                deviationColumns = (columnsCount - DeviationColumns.Count) + 1;
            }
            string condNotExplained = "";
            string condExplained = "";
            if (ComparisonTask.IsLinearView) {
                condNotExplained = "=AND($A2=\"\";$B2<>\"Passed\")";
                condExplained = "=OR($A2<>\"\";$B2=\"Passed\")";
            } else {
                condNotExplained = "=AND($A2=\"\";AND(" + GetExcelColumnName(deviationColumns) + "2 <>\"0\";$B2<>\"Passed\"))";
                condExplained = "=\"0\"";
            }
            FormatConditions sheetFormatConditions = null;
            FormatCondition formatCondNotExplained = null;
            FormatCondition formatCondExplained = null;
            Interior interior = null;
            try {
                if (!isPassed) {
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
                if (!isPassed) {
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
                if (ComparisonTask.IsLinearView) {
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
            } catch (Exception ex) {
                if (sheetFormatConditions != null)
                    Marshal.ReleaseComObject(sheetFormatConditions);
                if (formatCondNotExplained != null)
                    Marshal.ReleaseComObject(formatCondNotExplained);
                if (formatCondExplained != null)
                    Marshal.ReleaseComObject(formatCondExplained);
                if (interior != null)
                    Marshal.ReleaseComObject(interior);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                throw new Exception(ex.Message);
            } finally {
                if (sheetFormatConditions != null)
                    Marshal.ReleaseComObject(sheetFormatConditions);
                if (formatCondNotExplained != null)
                    Marshal.ReleaseComObject(formatCondNotExplained);
                if (formatCondExplained != null)
                    Marshal.ReleaseComObject(formatCondExplained);
                if (interior != null)
                    Marshal.ReleaseComObject(interior);
                GC.Collect();
                GC.WaitForPendingFinalizers();
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

        public void CleanUp() {
            Data.Clear();
            ExtraMaster.Clear();
            ExtraTest.Clear();
            PassedRows.Clear();
        }
    }
}
