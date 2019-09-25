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
            IEnumerable<string> masterHeaders = Enumerable.Empty<string>();
            IEnumerable<string> testHeaders = Enumerable.Empty<string>();
            if (comparisonTask.MasterConfiguration.IsHeadersExist) {
                MasterHeadersLine = masterContent.Take(1);
                masterHeaders = Splitter.Split(MasterHeadersLine.First(), comparisonTask.MasterConfiguration.Delimiter);
            }
            if (comparisonTask.TestConfiguration.IsHeadersExist) {
                TestHeadersLine = testContent.Take(1);
                testHeaders = Splitter.Split(TestHeadersLine.First(), comparisonTask.TestConfiguration.Delimiter);
            }
            ColumnsCorrection columnsCorrection = new ColumnsCorrection(masterHeaders.ToArray(), testHeaders.ToArray());
            columnsCorrection.Correct();
            comparisonTask.IfCancelRequested();
            MasterTable.LoadData(MasterHeadersLine.Concat(exceptedMasterData), comparisonTask.MasterConfiguration.Delimiter, comparisonTask.MasterConfiguration.IsHeadersExist, comparisonTask, columnsCorrection.MasterCorrection, comparisonTask.MasterConfiguration.ColumnsCount);
            comparisonTask.IfCancelRequested();
            TestTable.LoadData(TestHeadersLine.Concat(exceptedTestData), comparisonTask.TestConfiguration.Delimiter, comparisonTask.TestConfiguration.IsHeadersExist, comparisonTask, columnsCorrection.TestCorrection, comparisonTask.TestConfiguration.ColumnsCount);
        }

        public CompareTable Process(IWorkTable masterTable, IWorkTable testTable, IEnumerable<string> masterContent, IEnumerable<string> testContent, ComparisonTask comparisonTask, UserKeys userKeys) {
            ComparisonCore comparisonCore = new ComparisonCore(comparisonTask);
            if (masterTable.RowsCount > 0 && testTable.RowsCount > 0) {
                CompareTable = comparisonCore.Execute(masterTable, testTable, userKeys);
            } else if (IsOnlyExtra()) {
                var numberedHeaders = Helpers.NumerateSequence(masterTable.Headers.Data);
                SetComparisonKeys(comparisonCore, comparisonTask, masterContent, testContent, userKeys, numberedHeaders);
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

        public bool StartComparison(IFileReader fileReader, ComparisonTask comparisonTask, UserKeys userKeys) {
            IsBusy = true;
            comparisonTask.Status = Status.Executing;
            comparisonTask.StartClock();
            FileReader = fileReader;
            comparisonTask.IfCancelRequested();
            MasterConfiguration = comparisonTask.MasterConfiguration;
            TestConfiguration = comparisonTask.TestConfiguration;
            ReadFiles(MasterConfiguration, TestConfiguration, comparisonTask);
            PrepareData(MasterFileContent, TestFileContent, comparisonTask);
            var compTable = Process(MasterTable, TestTable, MasterFileContent, TestFileContent, comparisonTask, userKeys);
            if (compTable == null) {
                ComparisonCore comparisonCore = new ComparisonCore(comparisonTask);
                var numberedHeaders = Helpers.NumerateSequence(FindHeaders(MasterFileContent.FirstOrDefault(), comparisonTask.MasterConfiguration.IsHeadersExist, comparisonTask.MasterConfiguration.Delimiter));
                SetComparisonKeys(comparisonCore, comparisonTask, MasterFileContent, TestFileContent, userKeys, numberedHeaders);
                comparisonTask.SetResultFile(true);
                compTable = new CompareTable(MasterTable.Headers, TestTable.Headers, comparisonTask);
                compTable.SavePassed(comparisonTask.ResultFile, comparisonTask.MasterConfiguration.Delimiter, MasterFileContent, TestFileContent);
                comparisonTask.Status = Status.Passed;
            } else if (compTable.ComparedRowsCount == 0 && compTable.MasterExtraCount == 0 && compTable.TestExtraCount == 0) {               
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
            comparisonTask.Headers = MasterTable.Headers.Data;
            compTable.CleanUp();
            IsBusy = false;
            return true;
        }

        public void SetComparisonKeys(ComparisonCore comparisonCore, ComparisonTask comparisonTask, IEnumerable<string> masterContent, IEnumerable<string> testContent, UserKeys userKeys, Dictionary<int, string> numberedHeaders) {
            var masterSample = PrepareSampleRows(masterContent, comparisonTask.MasterRowsCount, comparisonTask.MasterConfiguration, comparisonTask);
            var testSample = PrepareSampleRows(testContent, comparisonTask.TestRowsCount, comparisonTask.TestConfiguration, comparisonTask);
            var baseStat = comparisonCore.GatherStatistics(masterSample, testSample);
            var sampleRows = comparisonTask.MasterRowsCount > comparisonTask.TestRowsCount ? masterSample : testSample;
            comparisonTask.ComparisonKeys = comparisonCore.MergeComparisonKeys(userKeys, sampleRows, numberedHeaders, baseStat);
        }

        private bool IsOnlyExtra() {
            if ((MasterTable.RowsCount == 0 && TestTable.RowsCount > 0) || (MasterTable.RowsCount > 0 && TestTable.RowsCount == 0)) {
                return true;
            } else {
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

        private List<Row> PrepareSampleRows(IEnumerable<string> fileContent, int rowsCount, ImportConfiguration impConfig, ComparisonTask comparisonTask) {
            int rowsForAnalysis = (int)Math.Round(rowsCount * 0.05);
            if (rowsForAnalysis > 10000) {
                rowsForAnalysis = 10000;
            } else if (rowsForAnalysis == 0) {
                rowsForAnalysis = 1;
            }
            int middleOfFile = (int)Math.Round(rowsCount / 2.0);
            var data = fileContent
                .Take(rowsForAnalysis)
                .Concat(fileContent.Skip(middleOfFile).Take(rowsForAnalysis))
                .Concat(fileContent.Skip(rowsCount - rowsForAnalysis));
            var tempTable = new WorkTable("Temp");
            tempTable.LoadData(data, impConfig.Delimiter, impConfig.IsHeadersExist, comparisonTask, new List<MoveColumn>(), comparisonTask.MasterConfiguration.ColumnsCount);
            return tempTable.Rows;
        }

        public string[] FindHeaders(string firstLine, bool isHeadersExist, char[] delimiter) {
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
