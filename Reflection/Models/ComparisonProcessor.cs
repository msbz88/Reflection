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
        ComparisonTask ComparisonTask { get; set; }
        IFileReader FileReader { get; set; }
        //IImportConfiguration ImportConfiguration { get; set; }
        PerformanceCounter perfCounter = new PerformanceCounter();
        IWorkTable MasterTable;
        IWorkTable TestTable;
        public bool IsBusy { get; set; }
        CompareTable CompareTable;

        public ComparisonProcessor() {
        }

        private void PrepareData(ImportConfiguration masterConfiguration, ImportConfiguration testConfiguration) {
            //ImportConfiguration = importConfiguration;            
            //start
            perfCounter.Start();
            int masterHeaderRowCount = masterConfiguration.IsHeadersExist ? 1 : 0;
            int testHeaderRowCount = testConfiguration.IsHeadersExist ? 1 : 0;
            var masterFileContent = FileReader.ReadFile(masterConfiguration.FilePath, masterConfiguration.RowsToSkip, masterConfiguration.Encoding);
            ComparisonTask.UpdateProgress(1);
            ComparisonTask.IfCancelRequested();
            var countMasterLines = FileReader.CountLines(masterConfiguration.FilePath) - (masterConfiguration.RowsToSkip + masterHeaderRowCount);
            ComparisonTask.MasterRowsCount = countMasterLines;
            var testFileContent = FileReader.ReadFile(testConfiguration.FilePath, testConfiguration.RowsToSkip, testConfiguration.Encoding);
            ComparisonTask.UpdateProgress(1);
            ComparisonTask.IfCancelRequested();
            var countTestLines = FileReader.CountLines(testConfiguration.FilePath) - (testConfiguration.RowsToSkip + testHeaderRowCount);
            ComparisonTask.TestRowsCount = countTestLines;
            ComparisonTask.ActualRowsDiff = ComparisonTask.MasterRowsCount - ComparisonTask.TestRowsCount;
            perfCounter.Stop("Read two init files");
            CheckIfEqualColumns(masterFileContent.FirstOrDefault(), testFileContent.FirstOrDefault());
            perfCounter.Start();
            var exceptedMasterData = Except(masterFileContent, testFileContent, "Master");
            ComparisonTask.IfCancelRequested();
            ComparisonTask.UpdateProgress(2);
            var exceptedTestData = Except(testFileContent, masterFileContent, "Test");
            ComparisonTask.UpdateProgress(2);
            perfCounter.Stop("Except files");
            MasterTable = new WorkTable("Master");
            TestTable = new WorkTable("Test");
            //if (ChaeckPreparison(exceptedMasterData, exceptedTestData)) {
                perfCounter.Start();
                ComparisonTask.IfCancelRequested();
            IEnumerable<string> headersLine = Enumerable.Empty<string>();
            if (ComparisonTask.MasterConfiguration.IsHeadersExist) {
                headersLine = masterFileContent.Take(1);
            }
                var headers = ComparisonTask.MasterConfiguration.IsHeadersExist ? masterFileContent.FirstOrDefault() : null;
                MasterTable.LoadData(headersLine.Concat(exceptedMasterData), masterConfiguration.Delimiter, masterConfiguration.IsHeadersExist, ComparisonTask);
                ComparisonTask.IfCancelRequested();
                TestTable.LoadData(headersLine.Concat(exceptedTestData), testConfiguration.Delimiter, testConfiguration.IsHeadersExist, ComparisonTask);
                perfCounter.Stop("Load two files to WorkTable");
            //} 
        }

        public bool StartComparison(IFileReader fileReader, ComparisonTask comparisonTask) {
            IsBusy = true;
            ComparisonTask = comparisonTask;
            ComparisonTask.Status = Status.Executing;
            ComparisonTask.StartClock();
            FileReader = fileReader;
            CompareTable = new CompareTable(comparisonTask);
            ComparisonTask.IfCancelRequested();
            PrepareData(ComparisonTask.MasterConfiguration, ComparisonTask.TestConfiguration);         
            if (MasterTable.RowsCount > 0 || TestTable.RowsCount > 0) {
                ComparisonCore comparisonCore = new ComparisonCore(perfCounter, ComparisonTask);
                ComparisonTask.IfCancelRequested();
                CompareTable = comparisonCore.Execute(MasterTable, TestTable);
            }
            if (CompareTable.ComparedRowsCount == 0 && CompareTable.MasterExtraCount == 0 && CompareTable.TestExtraCount == 0) {
                var masterContent = FileReader.ReadFile(ComparisonTask.MasterConfiguration.FilePath, ComparisonTask.MasterConfiguration.RowsToSkip, ComparisonTask.MasterConfiguration.Encoding);
                var testContent = FileReader.ReadFile(ComparisonTask.TestConfiguration.FilePath, ComparisonTask.TestConfiguration.RowsToSkip, ComparisonTask.TestConfiguration.Encoding);
                var array = GetPassedIds(masterContent, testContent);
                ComparisonTask.SetResultFile(true);
                CompareTable.SavePassed(ComparisonTask.ResultFile, ComparisonTask.MasterConfiguration.Delimiter, array);
                ComparisonTask.Status = Status.Passed;
            } else {              
                ComparisonTask.SetResultFile(false);
                CompareTable.SaveComparedRows(ComparisonTask.ResultFile);
                ComparisonTask.Status = Status.Failed;
            }
            ComparisonTask.UpdateProgress(100);
            ComparisonTask.StopClock();
            WriteLog();
            IsBusy = false;        
            return true;
        }

        private bool ChaeckPreparison(IEnumerable<string> master, IEnumerable<string> test) {
            if (master.Any() && test.Any()) {
                var m = master.Take(2);
                var t = test.Take(2);
                if(m.Count()==1 && test.Count() == 1) {
                    return false;
                } else { return true; }
            }else {
                return false;
            }
        }

        private IEnumerable<string> Except(IEnumerable<string> dataFirst, IEnumerable<string> dataSecond, string version) {
            //IEnumerable<string> headersLine = Enumerable.Empty<string>();
            //if (ComparisonTask.MasterConfiguration.IsHeadersExist) {
            //    headersLine = dataFirst.Take(1);
            //    dataFirst = dataFirst.Skip(1);
            //    dataSecond = dataSecond.Skip(1);
            //}
            Dictionary<string, int> duplicates = new Dictionary<string, int>();
            var uniqHashes = new HashSet<string>();
            List<string> passed = new List<string>();
            foreach (var item in dataSecond) {
                var hash = CalculateMD5Hash(item);
                if (!uniqHashes.Add(hash)) {
                    if (duplicates.ContainsKey(hash)) {
                        duplicates[hash] = duplicates[hash] + 1;
                    }else {
                        duplicates.Add(hash, 2);
                    }
                }
            }           
            foreach (var item in dataFirst) {
                var hash = CalculateMD5Hash(item);
                var isPresent = uniqHashes.Contains(hash);
                if (isPresent) {
                    var isDuplicated = duplicates.ContainsKey(hash);
                    if (isDuplicated) {
                        duplicates[hash] = duplicates[hash] - 1;
                        if(duplicates[hash] < 0) {
                            yield return item;
                        }
                    }
                    if (!ComparisonTask.IsDeviationsOnly) {
                        passed.Add(item);
                    }
                } else {
                    yield return item;
                }
            }
            if (!ComparisonTask.IsDeviationsOnly) {
                File.WriteAllLines(ComparisonTask.CommonDirectoryPath + "\\" + version + "Passed.temp", passed);
                ComparisonTask.ExceptedRecords = passed.Count;
            }
            //return headersLine.Concat(res);
        }

        private string CalculateMD5Hash(string input) {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = ComparisonTask.TestConfiguration.Encoding.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++) {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private bool IsPassedRowsExcepted() {
            if (File.Exists(ComparisonTask.CommonDirectoryPath + "\\passed.temp")) {
                return File.ReadLines(ComparisonTask.CommonDirectoryPath + "\\passed.temp").Any();
            }else {
                return false;
            }
        }

        private void CheckIfEqualColumns(string masterFirstLine, string testFirstLine) {
            var parseMaster = masterFirstLine.Split(new[] { ComparisonTask.MasterConfiguration.Delimiter }, StringSplitOptions.None);
            var parseTest = testFirstLine.Split(new[] { ComparisonTask.TestConfiguration.Delimiter }, StringSplitOptions.None);
            if (parseMaster.Length != parseTest.Length) {
                throw new Exception("There is different number of columns between master and test files.");
            }
        }

        private void WriteLog() {
            try {
                string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Replace("SCDOM\\", "");
                string logFile = @"O:\DATA\COMMON\core\log\" + userName + "_result.log";
                List<string> content = new List<string>();
                content.Add("StartTime: " + ComparisonTask.StartTime);
                content.Add("MasterFilePath: " + ComparisonTask.MasterConfiguration.FilePath);
                content.Add("TestFilePath: " + ComparisonTask.TestConfiguration.FilePath);
                content.Add("IsLinearView: " + ComparisonTask.IsLinearView.ToString());
                content.Add("MasterRowsCount: " + ComparisonTask.MasterRowsCount.ToString());
                content.Add("TestRowsCount: " + ComparisonTask.TestRowsCount.ToString());
                content.Add("ActualRowsDiff: " + ComparisonTask.ActualRowsDiff.ToString());
                content.Add("RowsWithDeviations: " + ComparisonTask.RowsWithDeviations.ToString());
                content.Add("ExtraMasterCount: " + ComparisonTask.ExtraMasterCount.ToString());
                content.Add("ExtraTestCount: " + ComparisonTask.ExtraTestCount.ToString());
                content.Add("Status: " + ComparisonTask.Status);
                content.Add("ErrorMessage: " + ComparisonTask.ErrorMessage);
                content.Add("--------------------------------------------------------------------------");
                File.AppendAllLines(logFile, content);
            } catch (Exception) { }
        }

        private string[,] GetPassedIds(IEnumerable<string> masterFileContent, IEnumerable<string> testFileContent) {
            List<int> mainColumnsToGet = new List<int>();
            if(ComparisonTask.ComparisonKeys.MainKeys.Count == 0) {
                var comparisonKeys = AnalyseFiles(masterFileContent, testFileContent);
                ComparisonTask.ComparisonKeys.MainKeys = comparisonKeys.MainKeys;
                ComparisonTask.ComparisonKeys.BinaryValues = comparisonKeys.BinaryValues.Concat(comparisonKeys.UserIdColumnsBinary).Distinct().ToList();
                ComparisonTask.ComparisonKeys.UserIdColumns = comparisonKeys.UserIdColumns;
            }           
            mainColumnsToGet = ComparisonTask.ComparisonKeys.MainKeys.Concat(ComparisonTask.ComparisonKeys.UserIdColumns).Distinct().ToList();
            int allColumnsCount = 1 + mainColumnsToGet.Count + ComparisonTask.ComparisonKeys.BinaryValues.Count * 2;
            string[,] outputArray = new string[1 + ComparisonTask.MasterRowsCount, allColumnsCount];
            int rowCount = 0;
            int columnCount = 0;
            outputArray[rowCount, 0] = "Comparison Result";
            var transHeaders = GetValuesByPositions(MasterTable.Headers.Data, ComparisonTask.ComparisonKeys.BinaryValues);
            foreach (var item in transHeaders) {
                columnCount++;
                outputArray[rowCount, columnCount] = "M_" + item;
                columnCount++;
                outputArray[rowCount, columnCount] = "T_" + item;              
            }
            var mainHeaders = GetValuesByPositions(MasterTable.Headers.Data, mainColumnsToGet);
            foreach (var item in mainHeaders) {
                columnCount++;
                outputArray[rowCount, columnCount] = item;
            }
            masterFileContent = ComparisonTask.MasterConfiguration.IsHeadersExist ? masterFileContent.Skip(1) : masterFileContent;
            testFileContent = ComparisonTask.TestConfiguration.IsHeadersExist ? testFileContent.Skip(1) : testFileContent;
            if (ComparisonTask.ComparisonKeys.BinaryValues.Count > 0) {
                foreach (var line in masterFileContent) {
                    var rowMaster = line.Split(new[] { ComparisonTask.MasterConfiguration.Delimiter }, StringSplitOptions.None);
                    var rowTest = testFileContent.Skip(rowCount).First().Split(new[] { ComparisonTask.TestConfiguration.Delimiter }, StringSplitOptions.None);
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
            }else {
                foreach (var line in masterFileContent) {
                    var rowMaster = line.Split(new[] { ComparisonTask.MasterConfiguration.Delimiter }, StringSplitOptions.None);
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

        private ComparisonKeys AnalyseFiles(IEnumerable<string> masterFileContent, IEnumerable<string> testFileContent) {
            int rowsForAnalysis = (int)Math.Round(ComparisonTask.MasterRowsCount * 0.05);
            rowsForAnalysis = rowsForAnalysis == 0 ? 1 : rowsForAnalysis;
            int middleOfFile = (int)Math.Round(ComparisonTask.MasterRowsCount / 2.0);
            var masterData = masterFileContent
                .Take(rowsForAnalysis)
                .Concat(masterFileContent.Skip(middleOfFile).Take(rowsForAnalysis))
                .Concat(masterFileContent.Skip(ComparisonTask.MasterRowsCount - rowsForAnalysis));
            var testData = testFileContent
                .Take(rowsForAnalysis)
                .Concat(testFileContent.Skip(middleOfFile).Take(rowsForAnalysis))
                .Concat(testFileContent.Skip(ComparisonTask.MasterRowsCount - rowsForAnalysis));
            MasterTable.LoadData(masterData, ComparisonTask.MasterConfiguration.Delimiter, ComparisonTask.MasterConfiguration.IsHeadersExist, ComparisonTask);
            TestTable.LoadData(testData, ComparisonTask.TestConfiguration.Delimiter, ComparisonTask.TestConfiguration.IsHeadersExist, ComparisonTask);
            ComparisonCore comparisonCore = new ComparisonCore(perfCounter, ComparisonTask);
            var stat = comparisonCore.GatherStatistics(MasterTable.Rows, TestTable.Rows);
            return comparisonCore.AnalyseForPivotKey(MasterTable.Rows, stat, MasterTable.Headers.Data);
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
