using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

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
        public string ResultFile { get; set; }

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
            return MasterFileName[0]=='['? MasterFileName.TrimStart('['): MasterFileName;
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

    }
}
