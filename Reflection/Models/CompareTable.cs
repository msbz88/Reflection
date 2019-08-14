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
        List<int> MasterPassedRows;
        List<int> TestPassedRows;
        ComparisonKeys ComparisonKeys { get; set; }
        bool IsExcelInstaled { get; set; }
        List<string> Headers;
        List<int> DeviationColumns;
        private int RowCount { get; set; }
        bool IsLinearView { get; set; }
        public bool IsDeviationsOnly { get; set; }
        Dictionary<int, string> NumberedColumnNames { get; set; }
        DefectsSearch DefectsSearch { get; set; }
        OraSession OraSession { get; set; }

        public CompareTable() {
            Data = new List<ComparedRow>();
            ExtraMaster = new List<string>();
            ExtraTest = new List<string>();
            IsExcelInstaled = Type.GetTypeFromProgID("Excel.Application") == null ? false : true;
            RowCount = 0;
        }

        public CompareTable(Row masterHeaders, Row testHeaders, ComparisonTask comparisonTask) {
            Data = new List<ComparedRow>();
            ExtraMaster = new List<string>();
            ExtraTest = new List<string>();
            Delimiter = comparisonTask.MasterConfiguration.Delimiter;
            MasterHeaders = masterHeaders;
            TestHeaders = testHeaders;
            ComparisonKeys = comparisonTask.ComparisonKeys;
            MasterPassedRows = new List<int>();
            TestPassedRows = new List<int>();
            IsExcelInstaled = Type.GetTypeFromProgID("Excel.Application") == null ? false : true;
            IsLinearView = comparisonTask.IsLinearView;
            IsDeviationsOnly = comparisonTask.IsDeviationsOnly;
            NumberedColumnNames = NumberAllColumnNames(masterHeaders.Data);           
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

        public void SaveComparedRows(string filePath) {
            string[,] dataToSave = null;
            string fileExt = "";
            if (IsLinearView) {
                var projectName = GetProjectName(filePath);
                var upgradeName = GetUpgradeName(filePath);
                OraSession = StartSession();
                DefectsSearch = new DefectsSearch(projectName, upgradeName, OraSession);
                dataToSave = PrepareDataLinar();
                fileExt = ".xlsm";
            } else {
                dataToSave = PrepareDataTabular();
                fileExt = ".xlsx";
            }
            if (!IsExcelInstaled || dataToSave.GetLength(0) > 1000000) {
                SaveToFlatFile(filePath + ".txt", dataToSave);
            } else {
                SaveToExcel(filePath + fileExt, dataToSave, false);
            }
        }

        public void SavePassed(string filePath, string delimiter, string[,] array) {
            if (!IsExcelInstaled || array.GetLength(0) > 1000000) {
                Delimiter = delimiter;
                SaveToFlatFile(filePath + ".txt", array);
            } else {
                SaveToExcel(filePath + ".xlsx", array, true);
            }
        }

        private string[,] PrepareDataTabular() {
            var binaryValues = ComparisonKeys.BinaryValues.Concat(ComparisonKeys.UserExcludeColumnsBinary).Distinct().ToList();
            var mainIdColumns = ComparisonKeys.MainKeys.Concat(ComparisonKeys.UserIdColumns).Distinct().ToList();
            DeviationColumns = Data.SelectMany(row => row.Deviations.Select(col => col.ColumnId)).Distinct().OrderBy(colId => colId).ToList();
            var allColumns = mainIdColumns.Concat(DeviationColumns).ToList();
            Headers = GenerateHeadersForFile(binaryValues, allColumns);
            int rowCount = 1 + ComparedRowsCount + ExtraMaster.Count + ExtraTest.Count;
            int columnCount = Headers.Count;
            string[,] dataToSave = new string[rowCount, columnCount];
            for (int col = 0; col < Headers.Count; col++) {
                dataToSave[RowCount, col] = Headers[col];
            }
            var rowsWithDeviations = Data.Where(row => !row.IsPassed);           
            foreach (var comparedRow in rowsWithDeviations) {
                RowToSave rowToSave = new RowToSave(comparedRow);
                var result = rowToSave.PrepareRowTabular(NumberedColumnNames, DefectsSearch, DeviationColumns);
                RowCount++;
                for (int col = 0; col < result.Count; col++) {
                    dataToSave[RowCount, col] = result[col];
                }
            }
            AddExtraRows(dataToSave, "Master", ExtraMaster, binaryValues, mainIdColumns);
            AddExtraRows(dataToSave, "Test", ExtraTest, binaryValues, mainIdColumns);
            if (!IsDeviationsOnly) {
                AddPassedRows();
            }
            return dataToSave;
        }

        private string[,] PrepareDataLinar() {
            var binaryValues = ComparisonKeys.BinaryValues.Concat(ComparisonKeys.UserExcludeColumnsBinary).Distinct().ToList();
            var mainIdColumns = ComparisonKeys.MainKeys.Concat(ComparisonKeys.UserIdColumns).Distinct().ToList();
            Headers = GenerateHeadersForFile(binaryValues, mainIdColumns);           
            Headers.Add("Column Name");
            Headers.Add("Master Value");
            Headers.Add("Test Value");
            var rowsWithDeviations = Data.Where(row => !row.IsPassed);
            int rowCount = 1 + ComparedRowsCount + ExtraMaster.Count + ExtraTest.Count;
            int columnCount = Headers.Count;
            string[,]  dataToSave = new string[rowCount, columnCount];
            for (int col = 0; col < Headers.Count; col++) {
                dataToSave[RowCount, col] = Headers[col];
            }
            foreach (var comparedRow in rowsWithDeviations) {
                RowToSave rowToSave = new RowToSave(comparedRow);
                var result = rowToSave.PrepareRowLinear(NumberedColumnNames, DefectsSearch);
                RowCount++;
                foreach (var row in result) {
                    int index = 0;
                    foreach (var col in row) {
                        dataToSave[RowCount, index++] = col;
                    }
                }
            }
            OraSession.CloseConnection();
            var deviations = Data.Where(row => !row.IsPassed).SelectMany(item => item.Deviations.Select(col => col.ColumnId)).Distinct().ToList();
            AddExtraRows(dataToSave, "Master", ExtraMaster, binaryValues, mainIdColumns);
            AddExtraRows(dataToSave, "Test", ExtraTest, binaryValues, mainIdColumns);
            if (!IsDeviationsOnly) {
                AddPassedRows();
            }
            return dataToSave;
        }

        private void SaveToFlatFile(string filePath, string[,] values) {
            int numRows = values.GetUpperBound(0) + 1;
            int numCols = values.GetUpperBound(1) + 1;
            StringBuilder sb = new StringBuilder();
            for (int row = 0; row < numRows; row++) {
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
                RowCount++;
                for (int col = 0; col < result.Count; col++) {
                    dataToSave[RowCount, col] = result[col];
                }
            }           
        }

        private void AddPassedRows() {

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
            var oraSession = new OraSession("DK01SV7020", "1521", "TESTIMMD", "TESTIMMD", "T7020230");
            oraSession.OpenConnection();
            return oraSession;
        }

        private string GetProjectName(string filePath) {
            var r = filePath.Split('\\');
            return r[2];
        }

        private string GetUpgradeName(string filePath) {
            var r = filePath.Split('\\');
            return r[3];
        }

        private void SaveToExcel(string filePath, string[,] outputArray, bool isPassed) {
            Application excelApplication = null;
            Workbook workbook = null;
            Worksheet sheet = null;
            Range range = null;
            var columnsCount = outputArray.GetLength(1);
            try {
                excelApplication = new Application();
                excelApplication.DisplayAlerts = false;
                excelApplication.Visible = false;
                if (IsLinearView) {
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
                FormatExcelSheet(sheet, range, columnsCount, rowsCount, isPassed);
                //AddMacro(workbook);
                //pass=#Reflection888
                workbook.SaveAs(filePath, Type.Missing, Type.Missing, Type.Missing, false, false, XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
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
            GC.Collect();
            GC.WaitForPendingFinalizers();
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

        private void AddMacro(Workbook workbook) {
            try{
                var newStandardModule = workbook.VBProject.VBComponents.Add(Microsoft.Vbe.Interop.vbext_ComponentType.vbext_ct_StdModule);
                var codeModule = newStandardModule.CodeModule;
                var lineNum = codeModule.CountOfLines + 1;
                List<string> codeText = new List<string>();
                codeText.Add("Public openedDate As Date");
                codeText.Add("Sub Auto_Close()");
                codeText.Add("Dim queueFile As String");
                codeText.Add("Dim thisPath As String");
                codeText.Add("Dim lastUpdated As Date");
                codeText.Add("Dim answer As Integer");
                codeText.Add("On Error Resume Next");
                codeText.Add("lastUpdated = ActiveWorkbook.BuiltinDocumentProperties(\"Last Save Time\")");
                codeText.Add("If lastUpdated > openedDate Then");
                codeText.Add("answer = MsgBox(\"Would you like to update the database of known defects according to your changes?\", vbYesNo + vbQuestion, \"Comparison application\")");
                codeText.Add("If answer = vbYes Then");
                codeText.Add("queueFile = \"O:\\DATA\\COMMON\\core\\defects\\UpdateRequest.txt\"");
                codeText.Add("thisPath = Application.ThisWorkbook.FullName");
                codeText.Add("Open queueFile For Output As #1");
                codeText.Add("Print #1, thisPath");
                codeText.Add("Close #1");
                codeText.Add("Call Shell(\"O:\\DATA\\COMMON\\core\\defects\\DefectUpdater.exe\")");
                codeText.Add("End If");
                codeText.Add("End If");
                codeText.Add("End Sub");
                codeText.Add("Sub Auto_Open()");
                codeText.Add("openedDate = Now");
                codeText.Add("End Sub");
                codeModule.InsertLines(lineNum, string.Join(Environment.NewLine, codeText));
            }catch (Exception) { }
        }


    }
}
