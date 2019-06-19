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
            ComparisonTask.Progress += 2;
            var countMasterLines = Task.Run(() => FileReader.CountLines(ImportConfiguration.MasterFilePath) - (ImportConfiguration.RowsToSkip + headerRowCount));
            var testFileContent = FileReader.ReadFile(ImportConfiguration.TestFilePath, ImportConfiguration.RowsToSkip, ImportConfiguration.Encoding);
            ComparisonTask.Progress += 2;
            var countTestLines = Task.Run(() => FileReader.CountLines(ImportConfiguration.TestFilePath) - (ImportConfiguration.RowsToSkip + headerRowCount));
            Task.WaitAll(countMasterLines, countTestLines);
            ComparisonTask.MasterRowsCount = countMasterLines.Result;
            ComparisonTask.TestRowsCount = countTestLines.Result;
            ComparisonTask.ActualRowsDiff = ComparisonTask.MasterRowsCount - ComparisonTask.TestRowsCount;
            perfCounter.Stop("Read two init files");
            perfCounter.Start();
            var exceptedMasterData = Except(masterFileContent, testFileContent);
            ComparisonTask.Progress += 2;
            var exceptedTestData = Except(testFileContent, masterFileContent);
            ComparisonTask.Progress += 2;
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
            }
            var uniqHashes = new HashSet<string>(dataSecond);
            var duplicates = dataFirst.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key);
            return headersLine.Concat(duplicates.Concat(dataFirst.Where(x => !uniqHashes.Contains(x))));
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

    }
}
