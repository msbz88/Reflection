using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Reflection.Models.Interfaces;

namespace Reflection.Models {
    public class ComparisonProcessor {
        //public ComparisonTask ComparisonTask { get; set; }
        IFileReader FileReader { get; set; }
        public IWorkTable MasterTable;
        public IWorkTable TestTable;
        public bool IsBusy { get; set; }
        CompareTable CompareTable;
        ImportConfiguration MasterConfiguration;
        ImportConfiguration TestConfiguration;
        IEnumerable<string> MasterFileContent;
        IEnumerable<string> TestFileContent;

        public ComparisonProcessor() {
        }

        private void ReadFiles(ImportConfiguration masterConfiguration, ImportConfiguration testConfiguration, ComparisonTask comparisonTask) {        
            int masterHeaderRowCount = masterConfiguration.IsHeadersExist ? 1 : 0;
            int testHeaderRowCount = testConfiguration.IsHeadersExist ? 1 : 0;
            MasterFileContent = FileReader.ReadFile(masterConfiguration.FilePath, masterConfiguration.RowsToSkip, masterConfiguration.Encoding);
            comparisonTask.UpdateProgress(1);
            comparisonTask.IfCancelRequested();
            var countMasterLines = FileReader.CountLines(masterConfiguration.FilePath) - (masterConfiguration.RowsToSkip + masterHeaderRowCount);
            comparisonTask.MasterRowsCount = countMasterLines;
            TestFileContent = FileReader.ReadFile(testConfiguration.FilePath, testConfiguration.RowsToSkip, testConfiguration.Encoding);
            comparisonTask.UpdateProgress(1);
            comparisonTask.IfCancelRequested();
            var countTestLines = FileReader.CountLines(testConfiguration.FilePath) - (testConfiguration.RowsToSkip + testHeaderRowCount);
            comparisonTask.TestRowsCount = countTestLines;
            comparisonTask.ActualRowsDiff = comparisonTask.MasterRowsCount - comparisonTask.TestRowsCount;
        }

        public void PrepareData(IEnumerable<string> masterContent, IEnumerable<string> testContent, ComparisonTask comparisonTask) {
            var exceptedMasterData = Except(masterContent, testContent, comparisonTask, comparisonTask.MasterConfiguration.Encoding);
            comparisonTask.IfCancelRequested();
            comparisonTask.UpdateProgress(2);
            var exceptedTestData = Except(testContent, masterContent, comparisonTask, comparisonTask.TestConfiguration.Encoding);
            comparisonTask.UpdateProgress(2);
            MasterTable = new WorkTable("Master");
            TestTable = new WorkTable("Test");
            IEnumerable<string> MasterHeadersLine = Enumerable.Empty<string>();
            IEnumerable<string> TestHeadersLine = Enumerable.Empty<string>();
            if (comparisonTask.MasterConfiguration.IsHeadersExist) {
                MasterHeadersLine = MasterFileContent.Take(1);
            }
            if (comparisonTask.TestConfiguration.IsHeadersExist) {
                TestHeadersLine = TestFileContent.Take(1);
            }
            var masterHeaders = FindHeaders(masterContent.FirstOrDefault(), comparisonTask.MasterConfiguration.IsHeadersExist, comparisonTask.MasterConfiguration.Delimiter);
            var testHeaders = FindHeaders(testContent.FirstOrDefault(), comparisonTask.TestConfiguration.IsHeadersExist, comparisonTask.TestConfiguration.Delimiter);
            ColumnsCorrection colCorr = new ColumnsCorrection(masterHeaders.ToList(), testHeaders.ToList());
            colCorr.AnalyseFileDimensions();
            comparisonTask.IfCancelRequested();
            MasterTable.LoadData(MasterHeadersLine.Concat(exceptedMasterData), comparisonTask.MasterConfiguration.Delimiter, comparisonTask.MasterConfiguration.IsHeadersExist, comparisonTask, colCorr.MasterCorrection);
            comparisonTask.IfCancelRequested();
            TestTable.LoadData(TestHeadersLine.Concat(exceptedTestData), comparisonTask.TestConfiguration.Delimiter, comparisonTask.TestConfiguration.IsHeadersExist, comparisonTask, colCorr.TestCorrection);
        }

        public CompareTable Process(IWorkTable masterTable, IWorkTable testTable, ComparisonTask comparisonTask) {
            if (masterTable.RowsCount > 0 && testTable.RowsCount > 0) {
                ComparisonCore comparisonCore = new ComparisonCore(comparisonTask);
                CompareTable = comparisonCore.Execute(masterTable, testTable);
            } else if (IsOnlyExtra()) {
                var comparisonKeys = AnalyseFiles(MasterFileContent, TestFileContent, comparisonTask);
                if (comparisonTask.ComparisonKeys.UserKeys.Count == 0) {
                    comparisonTask.ComparisonKeys.MainKeys = comparisonKeys.MainKeys;
                } else {
                    comparisonTask.ComparisonKeys.MainKeys = comparisonTask.ComparisonKeys.UserKeys;
                }
                comparisonTask.ComparisonKeys.BinaryValues = comparisonKeys.BinaryValues;
                comparisonTask.ComparisonKeys.ExcludeColumns = comparisonKeys.ExcludeColumns;
                comparisonTask.ComparisonKeys.UserIdColumns = comparisonKeys.UserIdColumns;
                comparisonTask.ComparisonKeys.UserIdColumnsBinary = comparisonKeys.UserIdColumnsBinary;
                comparisonTask.IsKeyReady = true;
                CompareTable = new CompareTable(masterTable.Headers, testTable.Headers, comparisonTask);
                CompareTable.AddMasterExtraRows(masterTable.Rows);
                CompareTable.AddTestExtraRows(testTable.Rows);
                comparisonTask.RowsWithDeviations = CompareTable.ComparedRowsCount;
                comparisonTask.ExtraMasterCount = CompareTable.MasterExtraCount;
                comparisonTask.ExtraTestCount = CompareTable.TestExtraCount;
            }
            masterTable.CleanUp();
            testTable.CleanUp();          
            return CompareTable;
        }

        public bool StartComparison(IFileReader fileReader, ComparisonTask comparisonTask) {
            IsBusy = true;
            comparisonTask.Status = Status.Executing;
            comparisonTask.StartClock();
            FileReader = fileReader;
            comparisonTask.IfCancelRequested();
            MasterConfiguration = comparisonTask.MasterConfiguration;
            TestConfiguration = comparisonTask.TestConfiguration;
            ReadFiles(MasterConfiguration, TestConfiguration, comparisonTask);
            PrepareData(MasterFileContent, TestFileContent, comparisonTask);
            //ComparisonTask = comparisonTask;
            var compTable = Process(MasterTable, TestTable, comparisonTask);           
            if (compTable == null || (compTable.ComparedRowsCount == 0 && compTable.MasterExtraCount == 0 && compTable.TestExtraCount == 0)) {
                SetComparisonKeysForPassed(MasterFileContent, TestFileContent, comparisonTask);
                comparisonTask.SetResultFile(true);
                compTable = new CompareTable(MasterTable.Headers, TestTable.Headers, comparisonTask);
                compTable.SavePassed(comparisonTask.ResultFile, comparisonTask.MasterConfiguration.Delimiter, MasterFileContent, TestFileContent);
                comparisonTask.Status = Status.Passed;
            } else {
                comparisonTask.SetResultFile(false);
                compTable.SaveComparedRows(comparisonTask.ResultFile);
                comparisonTask.Status = Status.Failed;
            }
            comparisonTask.UpdateProgress(100);
            comparisonTask.StopClock();
            compTable.CleanUp();
            IsBusy = false;
            return true;
        }

        public void SetComparisonKeysForPassed(IEnumerable<string> masterContent, IEnumerable<string> testContent, ComparisonTask comparisonTask) {
            if (comparisonTask.ComparisonKeys.UserKeys.Count == 0) {
                var comparisonKeys = AnalyseFiles(masterContent, testContent, comparisonTask);
                comparisonTask.ComparisonKeys.MainKeys = comparisonKeys.MainKeys;
                comparisonTask.ComparisonKeys.UserIdColumns = comparisonKeys.UserIdColumns;
            } else {
                comparisonTask.ComparisonKeys.MainKeys = comparisonTask.ComparisonKeys.UserKeys;
                comparisonTask.ComparisonKeys.UserIdColumns = comparisonTask.ComparisonKeys.UserIdColumns;
            }
        }

        private bool IsOnlyExtra() {
            if ((MasterTable.RowsCount == 0 && TestTable.RowsCount > 0) || (MasterTable.RowsCount > 0 && TestTable.RowsCount == 0)) {
                return true;
            }else {
                return false;
            }           
        }

        private IEnumerable<string> Except(IEnumerable<string> dataFirst, IEnumerable<string> dataSecond, ComparisonTask comparisonTask, Encoding encoding) {
            var totalLines = comparisonTask.MasterRowsCount > comparisonTask.TestRowsCount ? comparisonTask.MasterRowsCount : comparisonTask.TestRowsCount;
            if (comparisonTask.MasterConfiguration.IsHeadersExist) {
                dataFirst = dataFirst.Skip(1);
                dataSecond = dataSecond.Skip(1);
            }
            Dictionary<string, int> duplicates = new Dictionary<string, int>();
            var uniqHashes = new HashSet<string>();
            List<string> passed = new List<string>();
            foreach (var item in dataSecond) {
                comparisonTask.IfCancelRequested();
                var hash = CalculateMD5Hash(item, encoding);
                if (!uniqHashes.Add(hash)) {
                    if (duplicates.ContainsKey(hash)) {
                        duplicates[hash] = duplicates[hash] + 1;
                    } else {
                        duplicates.Add(hash, 2);
                    }
                }
            }
            foreach (var item in dataFirst) {
                comparisonTask.UpdateProgress(10.0 / (totalLines / 0.5));
                comparisonTask.IfCancelRequested();
                var hash = CalculateMD5Hash(item, encoding);
                var isPresent = uniqHashes.Contains(hash);
                if (isPresent) {
                    var isDuplicated = duplicates.ContainsKey(hash);
                    if (isDuplicated) {
                        duplicates[hash] = duplicates[hash] - 1;
                        if (duplicates[hash] < 0) {
                            yield return item;
                        }
                    }
                    if (!comparisonTask.IsDeviationsOnly) {
                        passed.Add(item);
                    }
                } else {
                    yield return item;
                }
            }
            if (!comparisonTask.IsDeviationsOnly && !File.Exists(comparisonTask.CommonDirectoryPath + "\\Passed.temp")) {
                File.WriteAllLines(comparisonTask.CommonDirectoryPath + "\\Passed.temp", passed);
                comparisonTask.ExceptedRecords = passed.Count;
            }
            uniqHashes.Clear();
            uniqHashes.TrimExcess();
            duplicates.Clear();
        }

        private string CalculateMD5Hash(string input, Encoding encoding) {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = encoding.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++) {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private bool IsPassedRowsExcepted(ComparisonTask comparisonTask) {
            if (File.Exists(comparisonTask.CommonDirectoryPath + "\\passed.temp")) {
                return File.ReadLines(comparisonTask.CommonDirectoryPath + "\\passed.temp").Any();
            } else {
                return false;
            }
        }

        private void CheckIfEqualColumns(string masterFirstLine, string testFirstLine, ComparisonTask comparisonTask) {
            var parseMaster = Splitter.Split(masterFirstLine, comparisonTask.MasterConfiguration.Delimiter);
            var parseTest = Splitter.Split(testFirstLine, comparisonTask.TestConfiguration.Delimiter);
            if (parseMaster.Length != parseTest.Length) {
                throw new Exception("There is different number of columns between master and test files.");
            }
        }
     
        private ComparisonKeys AnalyseFiles(IEnumerable<string> masterFileContent, IEnumerable<string> testFileContent, ComparisonTask comparisonTask) {
            int rowsForAnalysis = (int)Math.Round(comparisonTask.MasterRowsCount * 0.05);
            if (rowsForAnalysis > 10000) {
                rowsForAnalysis = 10000;
            } else if (rowsForAnalysis == 0) {
                rowsForAnalysis = 1;
            }
            int middleOfFile = (int)Math.Round(comparisonTask.MasterRowsCount / 2.0);
            var masterData = masterFileContent
                .Take(rowsForAnalysis)
                .Concat(masterFileContent.Skip(middleOfFile).Take(rowsForAnalysis))
                .Concat(masterFileContent.Skip(comparisonTask.MasterRowsCount - rowsForAnalysis));
            var testData = testFileContent
                .Take(rowsForAnalysis)
                .Concat(testFileContent.Skip(middleOfFile).Take(rowsForAnalysis))
                .Concat(testFileContent.Skip(comparisonTask.MasterRowsCount - rowsForAnalysis));
            var masterTab = new WorkTable("Master");
            var testTab = new WorkTable("Test");
            masterTab.LoadData(masterData, comparisonTask.MasterConfiguration.Delimiter, comparisonTask.MasterConfiguration.IsHeadersExist, comparisonTask, new List<MoveColumn>());
            testTab.LoadData(testData, comparisonTask.TestConfiguration.Delimiter, comparisonTask.TestConfiguration.IsHeadersExist, comparisonTask, new List<MoveColumn>());
            ComparisonCore comparisonCore = new ComparisonCore(comparisonTask);
            var stat = comparisonCore.GatherStatistics(masterTab.Rows, testTab.Rows);
            return comparisonCore.AnalyseForPivotKey(masterTab.Rows, stat, masterTab.Headers.Data);
        }

        private string[] FindHeaders(string firstLine, bool isHeadersExist, char[] delimiter) {
            string[] res;
            var firstRow = Splitter.Split(firstLine, delimiter);
            if (isHeadersExist) {
                res = firstRow;
            } else {
                res = GenerateDefaultHeaders(firstRow.Length);
            }
            return res;
        }

        private string[] GenerateDefaultHeaders(int count) {
            string[] res = new string[count];
            for (int i = 0; i < count; i++) {
                res[i] = "Column" + (i + 1);
            }
            return res;
        }


    }
}
