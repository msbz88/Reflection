using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Office.Interop.Excel;

namespace Reflection.Models {
    public class ComparisonTask : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        public int ComparisonId { get; private set; }
        public string MasterFileName { get; private set; }
        public string TestFileName { get; private set; }
        int masterRowsCount;
        public int MasterRowsCount {
            get { return masterRowsCount; }
            set {
                masterRowsCount = value;
                OnPropertyChanged("MasterRowsCount");
            }
        }
        int testRowsCount;
        public int TestRowsCount {
            get { return testRowsCount; }
            set {
                testRowsCount = value;
                OnPropertyChanged("TestRowsCount");
            }
        }
        int actualRowsDiff;
        public int ActualRowsDiff {
            get { return actualRowsDiff; }
            set {
                actualRowsDiff = value;
                OnPropertyChanged("ActualRowsDiff");
            }
        }
        int rowsWithDeviations;
        public int RowsWithDeviations {
            get { return rowsWithDeviations; }
            set {
                rowsWithDeviations = value;
                OnPropertyChanged("RowsWithDeviations");
            }
        }
        int extraMasterCount;
        public int ExtraMasterCount {
            get { return extraMasterCount; }
            set {
                extraMasterCount = value;
                OnPropertyChanged("ExtraMasterCount");
            }
        }
        int extraTestCount;
        public int ExtraTestCount {
            get { return extraTestCount; }
            set {
                extraTestCount = value;
                OnPropertyChanged("ExtraTestCount");
            }
        }
        double progress;
        public double Progress {
            get { return progress; }
            set {
                if (progress != value) {
                    progress = value;
                    OnPropertyChanged("Progress");
                }
            }
        }
        DateTime startTime { get; set; }
        public string StartTime {
            get { return startTime.ToString("dd/MM/yyyy HH:mm:ss"); }
        }
        Status status;
        public Status Status {
            get { return status; }
            set {
                if (status != value) {
                    status = value;
                    OnPropertyChanged("Status");
                }
            }
        }
        public string ErrorMessage { get; set; }
        public string CommonDirectoryPath { get; set; }
        public string CommonName { get; set; }
        public ImportConfiguration ImportConfiguration { get; set; }
        string resultFile;
        public string ResultFile {
            get { return resultFile; }
            set { resultFile = SetResultFile(value); }
        }
        public Task<Application> ExcelApplication { get; set; }
        public bool IsLinearView { get; set; } = true;

        public ComparisonTask(int comparisonId, ImportConfiguration importConfiguration) {
            ComparisonId = comparisonId;
            ImportConfiguration = importConfiguration;
            MasterFileName = Path.GetFileName(importConfiguration.MasterFilePath);
            TestFileName = Path.GetFileName(importConfiguration.TestFilePath);
            startTime = DateTime.Now;
            CommonDirectoryPath = FindCommonDirectory(importConfiguration.MasterFilePath, importConfiguration.TestFilePath);
            CommonName = GetCommonName();
            Status = Status.Queued;
            //SimulateProgress();
        }

        public void UpdateProgress(double val) {
            Progress += val;
        }

        public void OnPropertyChanged(string propName) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public void SimulateProgress() {
            new Thread(() => {               
                for (int i = 0; i <= 100; i++) {
                    Progress = i;
                    Thread.Sleep(50);
                    Status = Status.Executing;
                }
                Status = Status.Passed;
            }).Start();
        }

        private string GetCommonName() {
            var masterFile = MasterFileName[0] == '[' ? MasterFileName.TrimStart('[') : MasterFileName;
            var testFile = TestFileName[0] == ']' ? TestFileName.TrimStart(']') : TestFileName;
            var commonName = GetLongestCommonPrefix(new string[] { masterFile, testFile });
            return Path.GetFileNameWithoutExtension(commonName).TrimEnd('_').TrimEnd('.').TrimEnd('-');
        }

        private string FindCommonDirectory(string masterPath, string testPath) {
            var mDir = masterPath.Split(new[] { @"\" }, StringSplitOptions.None);
            var tDir = testPath.Split(new[] { @"\" }, StringSplitOptions.None);
            var result = "";
            var countDir = mDir.Length > tDir.Length ? tDir.Length : mDir.Length;
            for (int i = 0; i < countDir; i++) {
                if (mDir[i] != tDir[i]) {
                    mDir[0] = mDir[0] + @"\";
                    result = Path.Combine(mDir.Take(i).ToArray());
                }
            }
            return result;
        }

        private string GetLongestCommonPrefix(string[] s) {
            int k = s[0].Length;
            for (int i = 1; i < s.Length; i++) {
                k = Math.Min(k, s[i].Length);
                for (int j = 0; j < k; j++)
                    if (s[i][j] != s[0][j]) {
                        k = j;
                        break;
                    }
            }
            return s[0].Substring(0, k);
        }

        private string SetResultFile(string value) {
            return value + "_" + DateTime.Now.ToString("ddMMyyyy_HH-mm-ss");
        }


    }
}
