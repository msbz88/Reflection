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
        public List<ComparedRow> Data { get; set; }
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
        public List<string> ExtraMaster { get; set; }
        public int MasterExtraCount { get { return ExtraMaster.Count; } }
        public List<string> ExtraTest { get; set; }
        public int TestExtraCount { get { return ExtraTest.Count; } }
        char[] Delimiter { get; set; }
        public List<ComparedRow> PassedRows;
        bool IsExcelInstaled { get; set; }
        List<string> Headers;
        List<int> DeviationColumns;
        private int RowCount { get; set; }
        public Dictionary<int, string> NumberedColumnNames { get; set; }
        DefectsSearch DefectsSearch { get; set; }
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
            NumberedColumnNames = NumberAllColumnNames();
            ComparisonTask = comparisonTask;
            ComparisonTask.NumberedColumnNames = NumberedColumnNames;
            Delimiter = ComparisonTask.MasterConfiguration.Delimiter;
            DefectsSearch = new DefectsSearch();
        }

        public void SetIdColumns() {
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
                ExtraMaster.Add(string.Join(string.Join("", Delimiter), item.Data));
            }
        }

        public void AddMasterExtraRows(IEnumerable<string> extraRows) {
            ExtraMaster.AddRange(extraRows);
        }

        public IEnumerable<int> GetMasterComparedRowsId() {
            return Data.Select(row => row.MasterRowId).Concat(PassedRows.Select(row => row.MasterRowId));
        }

        public IEnumerable<int> GetTestComparedRowsId() {
            return Data.Select(row => row.TestRowId).Concat(PassedRows.Select(row => row.TestRowId));
        }

        public void AddTestExtraRows(IEnumerable<Row> extraRows) {
            foreach (var item in extraRows) {
                ExtraTest.Add(string.Join(string.Join("", Delimiter), item.Data));
            }
        }

        public void AddTestExtraRows(IEnumerable<string> extraRows) {
            ExtraTest.AddRange(extraRows);
        }

        public void SaveComparedRows(string filePath) {
            SetIdColumns();
            string[,] dataToSave = null;
            int rowsCount = 1;
            if (!ComparisonTask.IsDeviationsOnly) {
                rowsCount += ComparisonTask.ExceptedRecords + PassedRows.Count;
            }
            if (ComparisonTask.IsLinearView) {
                rowsCount += Data.Sum(row => row.Deviations.Count) + ExtraMaster.Count + ExtraTest.Count;
                var projectName = GetProjectName(filePath);
                var upgradeVersions = GetUpgradeName(filePath);
                ComparisonTask.ProjectName = projectName;
                ComparisonTask.UpgradeVersions = upgradeVersions;
                EnableDefectsSearchEngine(projectName, upgradeVersions);
                dataToSave = PrepareDataLinar(rowsCount);
                DisableDefectsSearchEngine();
            } else {
                rowsCount += ComparedRowsCount + ExtraMaster.Count + ExtraTest.Count;
                dataToSave = PrepareDataTabular(rowsCount);
            }
            if (!ComparisonTask.IsDeviationsOnly) {
                AddExceptedRecords(dataToSave);
                AddPassedRows(dataToSave);
            }
            if (!IsExcelInstaled || dataToSave.GetLength(0) > 1000000) {
                SaveToFlatFile(filePath, dataToSave);
                ComparisonTask.IsToExcelSaved = false;
            } else {
                SaveToExcel(filePath, dataToSave, false);
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

        private void EnableDefectsSearchEngine(string projectName, List<double> upgradeName) {
            if (projectName != "" && upgradeName != null) {
                DefectsSearch.Enable(projectName, upgradeName[0], upgradeName[1]);
            }
        }

        private void DisableDefectsSearchEngine() {
            if (DefectsSearch.IsEnabled) {
                DefectsSearch.Disable();
            }
        }

        private void AddExceptedRecords(string[,] dataToSave) {
            var exceptedRecords = File.ReadLines(ComparisonTask.CommonDirectoryPath + "\\Passed.temp");
            int rowsIndex = 0;
            foreach (var masterLine in exceptedRecords) {
                var parsedRow = Splitter.Split(masterLine, Delimiter);
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

        public void SavePassed(string filePath, char[] delimiter, IEnumerable<string> masterContent, IEnumerable<string> testContent) {
            string[,] array = null;
            if (!IsExcelInstaled || ComparisonTask.MasterRowsCount > 1000000) {
                Delimiter = delimiter;
                SavePassedToFlatFile(filePath, masterContent, testContent);
                ComparisonTask.IsToExcelSaved = false;
            } else {
                array = GetPassedForExcel(masterContent, testContent);
                SaveToExcel(filePath, array, true);
                ComparisonTask.IsToExcelSaved = true;
            }
        }

        public string[,] PrepareDataTabular(int rowsCount) {
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
                var result = rowToSave.PrepareRowTabular(NumberedColumnNames, DeviationColumns);
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

        public string[,] PrepareDataLinar(int rowsCount) {
            Headers = GenerateHeadersForFile(BinaryValues, MainIdColumns);
            Headers.Add("Column Name");
            Headers.Add("Master Value");
            Headers.Add("Test Value");
            var rowsWithDeviations = Data.Where(row => !row.IsPassed);
            int columnCount = Headers.Count;
            string[,] dataToSave = new string[rowsCount, columnCount];
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
                    sb.Append(string.Join("", Delimiter) + values[row, col]);
                sb.AppendLine();
            }
            File.WriteAllText(filePath + ".txt", sb.ToString());
        }

        private void AddExtraRows(string[,] dataToSave, string version, List<string> extraLines, List<int> binaryValues, List<int> mainIdColumns) {
            foreach (var extraRow in extraLines) {
                var parsedRow = Splitter.Split(extraRow, Delimiter);
                RowToSave rowToSave = new RowToSave(parsedRow);
                var result = rowToSave.PrepareExtraRow(version, binaryValues, mainIdColumns, DefectsSearch);
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
                headers.Add("T_" + TestHeaders[item]);
            }
            foreach (var item in allColumns) {
                string headerName = "";
                if (MasterHeaders[item] == "") {
                    headerName = "(Test extra column) " + TestHeaders[item];
                }else if(TestHeaders[item] == "") {
                    headerName = "(Master extra column) " + MasterHeaders[item];
                }else {
                    headerName = MasterHeaders[item];
                }
                headers.Add(headerName);
            }
            return headers;
        }

        private Dictionary<int, string> NumberAllColumnNames() {
            var columnsCount = MasterHeaders.Data.Length;
            Dictionary<int, string> result = new Dictionary<int, string>();
            for (int i = 0; i < columnsCount; i++) {
                string headerName = "";
                if (MasterHeaders[i] == "") {
                    headerName = "(Test extra column) " + TestHeaders[i];
                } else if (TestHeaders[i] == "") {
                    headerName = "(Master extra column) " + MasterHeaders[i];
                } else {
                    headerName = MasterHeaders[i];
                }
                result.Add(i, headerName);
            }
            return result;
        }

        private string GetProjectName(string filePath) {
            var r = Splitter.Split(filePath, new char[] { '\\' });
            if (r.Length > 3) {
                return r[2].Trim();
            } else {
                return "";
            }
        }

        private List<double> GetUpgradeName(string filePath) {
            var r = Splitter.Split(filePath, new char[] { '\\' });
            if (r.Length <= 3) {
                return null;
            }
            var upgrade = r[3];
            StringBuilder pattern = new StringBuilder();
            List<double> upgradeName = new List<double>();
            foreach (var item in upgrade) {
                if (char.IsDigit(item) || (pattern.Length > 0 && item == '.' || pattern.Length > 0 && item == ',')) {
                    pattern.Append(item);
                } else if (pattern.Length > 0 && item == ' ') {
                    double d;
                    var isDouble = double.TryParse(pattern.ToString().Replace('.', ','), out d);
                    if (isDouble) {
                        upgradeName.Add(d);
                        pattern.Clear();
                    } else {
                        return null;
                    }
                }
            }
            if (pattern.Length > 0) {
                double d;
                var isDouble = double.TryParse(pattern.ToString().Replace('.', ','), out d);
                if (isDouble) {
                    upgradeName.Add(d);
                    pattern.Clear();
                    return upgradeName;
                } else {
                    return null;
                }
            } else {
                return null;
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
                //if (ComparisonTask.IsLinearView && !isPassed) {
                //pass=#Reflection888
                //workbook = excelApplication.Workbooks.Open(@"O:\DATA\COMMON\core\data\template.xlsx");
                //}else {
                workbook = excelApplication.Workbooks.Add("");
                //}               
                sheet = (Worksheet)workbook.ActiveSheet;
                sheet.Name = "Comparison";
                var addin = excelApplication.AddIns.Add(@"I:\VT Execution\xDefectsUpdater\runDefectsUpdater.xlam", false);
                addin.Installed = true;
                excelApplication.ActiveWindow.Zoom = 80;
                excelApplication.Calculation = XlCalculation.xlCalculationAutomatic;
                range = (Range)sheet.Cells[1, 1];
                int rowsCount = outputArray.GetLength(0);
                range = range.Resize[rowsCount, columnsCount];
                range.set_Value(XlRangeValueDataType.xlRangeValueDefault, outputArray);
                ComparisonTask.UpdateProgress(2);
                FormatExcelSheet(sheet, range, columnsCount, rowsCount, isPassed);
                workbook.SaveAs(filePath + ".xlsx");
                workbook.Close();
                excelApplication.Quit();
            } catch (Exception ex) {
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
            } finally {
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

        private void SavePassedToFlatFile(string path, IEnumerable<string> masterFileContent, IEnumerable<string> testFileContent) {
            List<string> rowToSave = new List<string>();
            List<int> mainColumnsToGet = new List<int>();
            var delimiter = string.Join("", Delimiter);
            mainColumnsToGet = ComparisonTask.ComparisonKeys.MainKeys.Concat(ComparisonTask.ComparisonKeys.UserIdColumns).Distinct().ToList();
            rowToSave.Add("Comparison Result");
            var transHeaders = GetValuesByPositions(MasterHeaders.Data, ComparisonTask.ComparisonKeys.BinaryValues);
            foreach (var item in transHeaders) {
                rowToSave.Add("M_" + item);
                rowToSave.Add("T_" + item);
            }
            var mainHeaders = GetValuesByPositions(TestHeaders.Data, mainColumnsToGet);
            foreach (var item in mainHeaders) {
                rowToSave.Add(item);
            }
            File.WriteAllText(path+".txt", string.Join(delimiter, rowToSave));
            masterFileContent = ComparisonTask.MasterConfiguration.IsHeadersExist ? masterFileContent.Skip(1) : masterFileContent;
            testFileContent = ComparisonTask.TestConfiguration.IsHeadersExist ? testFileContent.Skip(1) : testFileContent;
            List<string> content = new List<string>();        
            if (ComparisonTask.ComparisonKeys.BinaryValues.Count > 0) {
                int rowCount = 0;
                foreach (var line in masterFileContent) {
                    rowToSave.Clear();
                    rowToSave.Add(Environment.NewLine);
                    var rowMaster = Splitter.Split(line, ComparisonTask.MasterConfiguration.Delimiter);
                    var rowTest = Splitter.Split(testFileContent.Skip(rowCount).First(), ComparisonTask.TestConfiguration.Delimiter);
                    rowToSave.Add("Passed");
                    var masterVals = GetValuesByPositions(rowMaster, ComparisonTask.ComparisonKeys.BinaryValues);
                    var testVals = GetValuesByPositions(rowTest, ComparisonTask.ComparisonKeys.BinaryValues);
                    for (int i = 0; i < masterVals.Count; i++) {
                        rowToSave.Add(masterVals[i]);
                        rowToSave.Add(testVals[i]);
                    }
                    rowToSave.AddRange(GetValuesByPositions(rowMaster, mainColumnsToGet));
                    rowCount++;
                    content.Add(string.Join(delimiter, rowToSave));
                    if (content.Count > 50000) {
                        File.AppendAllText(path, string.Join(delimiter, content));
                        content.Clear();
                    }                   
                }
            } else {
                foreach (var line in masterFileContent) {
                    var rowMaster = Splitter.Split(line, ComparisonTask.MasterConfiguration.Delimiter);
                    rowToSave.Clear();
                    rowToSave.Add(Environment.NewLine);
                    rowToSave.Add("Passed");
                    rowToSave.AddRange(GetValuesByPositions(rowMaster, mainColumnsToGet));
                    content.Add(string.Join(delimiter, rowToSave));
                    if (content.Count > 50000) {
                        File.AppendAllText(path, string.Join(delimiter, content));
                        content.Clear();
                    }
                }
            }
            File.AppendAllText(path, string.Join(delimiter, content));
            content.Clear();
        }

        public string[,] GetPassedForExcel(IEnumerable<string> masterFileContent, IEnumerable<string> testFileContent) {
            List<int> mainColumnsToGet = new List<int>();           
            mainColumnsToGet = ComparisonTask.ComparisonKeys.MainKeys.Concat(ComparisonTask.ComparisonKeys.UserIdColumns).Distinct().ToList();
            int allColumnsCount = 1 + mainColumnsToGet.Count + ComparisonTask.ComparisonKeys.BinaryValues.Count * 2;
            string[,] outputArray = new string[1 + ComparisonTask.MasterRowsCount, allColumnsCount];
            int rowCount = 0;
            int columnCount = 0;
            outputArray[rowCount, 0] = "Comparison Result";
            var transHeaders = GetValuesByPositions(MasterHeaders.Data, ComparisonTask.ComparisonKeys.BinaryValues);
            foreach (var item in transHeaders) {
                columnCount++;
                outputArray[rowCount, columnCount] = "M_" + item;
                columnCount++;
                outputArray[rowCount, columnCount] = "T_" + item;
            }
            var mainHeaders = GetValuesByPositions(TestHeaders.Data, mainColumnsToGet);
            foreach (var item in mainHeaders) {
                columnCount++;
                outputArray[rowCount, columnCount] = item;
            }
            masterFileContent = ComparisonTask.MasterConfiguration.IsHeadersExist ? masterFileContent.Skip(1) : masterFileContent;
            testFileContent = ComparisonTask.TestConfiguration.IsHeadersExist ? testFileContent.Skip(1) : testFileContent;
            if (ComparisonTask.ComparisonKeys.BinaryValues.Count > 0) {
                foreach (var line in masterFileContent) {
                    var rowMaster = Splitter.Split(line, ComparisonTask.MasterConfiguration.Delimiter);
                    var rowTest = Splitter.Split(testFileContent.Skip(rowCount).First(), ComparisonTask.TestConfiguration.Delimiter);
                    List<string> rowToSave = new List<string>();
                    rowToSave.Add("Passed");
                    var masterVals = GetValuesByPositions(rowMaster, ComparisonTask.ComparisonKeys.BinaryValues);
                    var testVals = GetValuesByPositions(rowTest, ComparisonTask.ComparisonKeys.BinaryValues);
                    for (int i = 0; i < masterVals.Count; i++) {
                        rowToSave.Add(masterVals[i]);
                        rowToSave.Add(testVals[i]);
                    }
                    rowToSave.AddRange(GetValuesByPositions(rowMaster, mainColumnsToGet));
                    rowCount++;
                    for (int i = 0; i < rowToSave.Count; i++) {
                        outputArray[rowCount, i] = rowToSave[i];
                    }
                }
            } else {
                foreach (var line in masterFileContent) {
                    var rowMaster = Splitter.Split(line, ComparisonTask.MasterConfiguration.Delimiter);
                    List<string> rowToSave = new List<string>();
                    rowToSave.Add("Passed");
                    rowToSave.AddRange(GetValuesByPositions(rowMaster, mainColumnsToGet));
                    rowCount++;
                    for (int i = 0; i < rowToSave.Count; i++) {
                        outputArray[rowCount, i] = rowToSave[i];
                    }
                }
            }
            return outputArray;
        }

        public List<string> GetValuesByPositions(string[] data, IEnumerable<int> positions) {
            var query = new List<string>();
            foreach (var item in positions) {
                query.Add(data[item]);
            }
            return query;
        }
    }
}
