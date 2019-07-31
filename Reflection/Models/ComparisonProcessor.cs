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
        IImportConfiguration ImportConfiguration { get; set; }
        PerformanceCounter perfCounter = new PerformanceCounter();
        IWorkTable MasterTable;
        IWorkTable TestTable;
        public bool IsBusy { get; set; }

        public ComparisonProcessor() {
        }

        private void PrepareData(ImportConfiguration importConfiguration) {
            ImportConfiguration = importConfiguration;            
            //start
            perfCounter.Start();
            int headerRowCount = ImportConfiguration.IsHeadersExist ? 1 : 0;
            var masterFileContent = FileReader.ReadFile(ImportConfiguration.MasterFilePath, ImportConfiguration.RowsToSkip, ImportConfiguration.Encoding);
            ComparisonTask.UpdateProgress(1);
            ComparisonTask.IfCancelRequested();
            var countMasterLines = FileReader.CountLines(ImportConfiguration.MasterFilePath) - (ImportConfiguration.RowsToSkip + headerRowCount);
            ComparisonTask.MasterRowsCount = countMasterLines;
            var testFileContent = FileReader.ReadFile(ImportConfiguration.TestFilePath, ImportConfiguration.RowsToSkip, ImportConfiguration.Encoding);
            ComparisonTask.UpdateProgress(1);
            ComparisonTask.IfCancelRequested();
            var countTestLines = FileReader.CountLines(ImportConfiguration.TestFilePath) - (ImportConfiguration.RowsToSkip + headerRowCount);
            ComparisonTask.TestRowsCount = countTestLines;
            ComparisonTask.ActualRowsDiff = ComparisonTask.MasterRowsCount - ComparisonTask.TestRowsCount;
            perfCounter.Stop("Read two init files");
            CheckIfEqualColumns(masterFileContent.FirstOrDefault(), testFileContent.FirstOrDefault());
            perfCounter.Start();
            var exceptedMasterData = Except(masterFileContent, testFileContent);
            ComparisonTask.IfCancelRequested();
            ComparisonTask.UpdateProgress(2);
            var exceptedTestData = Except(testFileContent, masterFileContent);
            ComparisonTask.UpdateProgress(2);
            perfCounter.Stop("Except files");
            MasterTable = new WorkTable("Master");
            TestTable = new WorkTable("Test");
            if (ChaeckPreparison(exceptedMasterData, exceptedTestData)) {
                perfCounter.Start();
                ComparisonTask.IfCancelRequested();
                MasterTable.LoadData(exceptedMasterData, ImportConfiguration.Delimiter, ImportConfiguration.IsHeadersExist, ComparisonTask);
                ComparisonTask.IfCancelRequested();
                TestTable.LoadData(exceptedTestData, ImportConfiguration.Delimiter, ImportConfiguration.IsHeadersExist, ComparisonTask);
                perfCounter.Stop("Load two files to WorkTable");
            } else {
                ComparisonTask.Status = Status.Passed;
                ComparisonTask.UpdateProgress(100);
                //perfCounter.SaveAllResults();
            }
        }

        public bool StartComparison(IFileReader fileReader, ComparisonTask comparisonTask) {
            IsBusy = true;
            ComparisonTask = comparisonTask;
            ComparisonTask.Status = Status.Executing;
            ComparisonTask.StartClock();
            FileReader = fileReader;
            ComparisonTask.IfCancelRequested();
            PrepareData(ComparisonTask.ImportConfiguration);
            if (MasterTable.RowsCount > 0 || TestTable.RowsCount > 0) {
                ComparisonCore comparisonCore = new ComparisonCore(perfCounter, ComparisonTask);
                ComparisonTask.IfCancelRequested();
                comparisonCore.Execute(MasterTable, TestTable);
            }
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

        private IEnumerable<string> Except(IEnumerable<string> dataFirst, IEnumerable<string> dataSecond) {
            IEnumerable<string> headersLine = Enumerable.Empty<string>();
            if (ImportConfiguration.IsHeadersExist) {
                headersLine = dataFirst.Take(1);
                dataFirst = dataFirst.Skip(1);
                dataSecond = dataSecond.Skip(1);
            }
            var uniqHashes = new HashSet<string>(dataSecond.Select(line => CalculateMD5Hash(line)));
            //var duplicates = dataFirst.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key).Skip(1);
            return headersLine.Concat(dataFirst.Where(x => !uniqHashes.Contains(CalculateMD5Hash(x))));
        }

        private string CalculateMD5Hash(string input) {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = ImportConfiguration.Encoding.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++) {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private void CheckIfEqualColumns(string masterFirstLine, string testFirstLine) {
            var parseMaster = masterFirstLine.Split(new[] { ImportConfiguration.Delimiter }, StringSplitOptions.None);
            var parseTest = testFirstLine.Split(new[] { ImportConfiguration.Delimiter }, StringSplitOptions.None);
            if (parseMaster.Length != parseTest.Length) {
                throw new Exception("There is different number of columns between master and test files.");
            }
        }

        public void Analyse(IFileReader fileReader, ImportConfiguration importConfiguration) {
            PrepareData(importConfiguration);
            if (MasterTable.RowsCount > 0 || TestTable.RowsCount > 0) {
                ComparisonCore comparisonCore = new ComparisonCore(perfCounter, ComparisonTask);
                comparisonCore.RunEarlyAnalysis(MasterTable, TestTable);
            }
        }

        private void WriteLog() {
            try {
                string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Replace("SCDOM\\", "");
                string logFile = @"O:\DATA\COMMON\core\log\" + userName + "_result.log";
                List<string> content = new List<string>();
                content.Add("StartTime: " + ComparisonTask.StartTime);
                content.Add("MasterFilePath: " + ComparisonTask.ImportConfiguration.MasterFilePath);
                content.Add("TestFilePath: " + ComparisonTask.ImportConfiguration.TestFilePath);
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
    }
}
