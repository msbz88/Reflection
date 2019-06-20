using System;
using System.Collections.Generic;
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
        public bool IsBusy { get; private set; }

        public ComparisonProcessor() {}

        public void StartComparison(IFileReader fileReader, ComparisonTask comparisonTask) {
            FileReader = fileReader;
            ImportConfiguration = comparisonTask.ImportConfiguration;
            ComparisonTask = comparisonTask;
            IsBusy = true;
            ComparisonTask.Status = Status.Executing;
            //start
            perfCounter.Start();
            int headerRowCount = ImportConfiguration.IsHeadersExist ? 1 : 0;
            var masterFileContent = FileReader.ReadFile(ImportConfiguration.MasterFilePath, ImportConfiguration.RowsToSkip, ImportConfiguration.Encoding);
            ComparisonTask.UpdateProgress(1);
            var countMasterLines = FileReader.CountLines(ImportConfiguration.MasterFilePath) - (ImportConfiguration.RowsToSkip + headerRowCount);
            ComparisonTask.MasterRowsCount = countMasterLines;
            var testFileContent = FileReader.ReadFile(ImportConfiguration.TestFilePath, ImportConfiguration.RowsToSkip, ImportConfiguration.Encoding);
            ComparisonTask.UpdateProgress(1);
            var countTestLines = FileReader.CountLines(ImportConfiguration.TestFilePath) - (ImportConfiguration.RowsToSkip + headerRowCount);          
            ComparisonTask.TestRowsCount = countTestLines;
            ComparisonTask.ActualRowsDiff = ComparisonTask.MasterRowsCount - ComparisonTask.TestRowsCount;
            perfCounter.Stop("Read two init files");
            CheckIfEqualColumns(masterFileContent.FirstOrDefault(), testFileContent.FirstOrDefault());
            perfCounter.Start();
            var exceptedMasterData = Except(masterFileContent, testFileContent);
            ComparisonTask.UpdateProgress(2);
            var exceptedTestData = Except(testFileContent, masterFileContent);
            ComparisonTask.UpdateProgress(2);
            perfCounter.Stop("Except files");         
            IWorkTable masterTable = new WorkTable("Master");
            IWorkTable testTable = new WorkTable("Test");

            if (exceptedMasterData.Any() && exceptedTestData.Any()) {
                perfCounter.Start();
                masterTable.LoadData(exceptedMasterData, ImportConfiguration.Delimiter, ImportConfiguration.IsHeadersExist, ComparisonTask);
                testTable.LoadData(exceptedTestData, ImportConfiguration.Delimiter, ImportConfiguration.IsHeadersExist, ComparisonTask);
                perfCounter.Stop("Load two files to WorkTable");
                ComparisonCore comparisonCore = new ComparisonCore(perfCounter, ComparisonTask);
                comparisonCore.Execute(masterTable, testTable);
            } else {
                perfCounter.SaveAllResults();
            }
            IsBusy = false;
        }

        private IEnumerable<string> Except(IEnumerable<string> dataFirst, IEnumerable<string> dataSecond) {
            IEnumerable<string> headersLine = Enumerable.Empty<string>();
            if (ImportConfiguration.IsHeadersExist) {
                headersLine = dataFirst.Take(1);
                dataFirst = dataFirst.Skip(1);
                dataSecond = dataSecond.Skip(1);
            }
            var uniqHashes = new HashSet<string>(dataSecond.Select(line => CalculateMD5Hash(line)));
            //var duplicates = dataFirst.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key);
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
    }
}
