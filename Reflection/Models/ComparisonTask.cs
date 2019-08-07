﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
                    if (value == Status.Passed) {
                        DeviationsView = "";
                    }
                    OnPropertyChanged("Status");
                }
            }
        }
        public string ErrorMessage { get; set; }
        public string CommonDirectoryPath { get; set; }
        public string CommonName { get; set; }
        public ImportConfiguration ImportConfiguration { get; set; }
        public string ResultFile { get; private set; }
        public Task<Application> ExcelApplication { get; set; }
        bool isLinearView;
        public bool IsLinearView {
            get { return isLinearView; }
            set {
                isLinearView = value;
                SetDeviationsView();
            }
        }
        public CancellationTokenSource CancellationToken { get; set; }
        string elapsedTime;
        public string ElapsedTime {
            get { return elapsedTime; }
            set {
                elapsedTime = value;
                OnPropertyChanged("ElapsedTime");
            }
        }
        DispatcherTimer Timer { get; set; }
        Stopwatch Stopwatch { get; set; }
        string deviationsView;
        public string DeviationsView {
            get {return deviationsView; }
            set {
                deviationsView = value;
                OnPropertyChanged("DeviationsView");
            }
        }
        public ComparisonKeys ComparisonKeys { get; set; }

        public ComparisonTask(int comparisonId, ImportConfiguration importConfiguration) {
            ComparisonId = comparisonId;
            ImportConfiguration = importConfiguration;
            MasterFileName = Path.GetFileName(importConfiguration.MasterFilePath);
            TestFileName = Path.GetFileName(importConfiguration.TestFilePath);
            startTime = DateTime.Now;
            CommonDirectoryPath = FindCommonDirectory(importConfiguration.MasterFilePath, importConfiguration.TestFilePath);
            CommonName = GetCommonName();
            Status = Status.Queued;
            CancellationToken = new CancellationTokenSource();
            ErrorMessage = "";
            IsLinearView = true;
            Timer = new DispatcherTimer();
            Stopwatch = new Stopwatch();
            ComparisonKeys = new ComparisonKeys();
            ComparisonKeys.UserKeys = importConfiguration.UserKeys;
        }

        public void UpdateProgress(double val) {
            Progress += val;
        }

        public void OnPropertyChanged(string propName) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
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

        public void SetResultFile(bool isPassed) {
            string viewType = "";
            string resultType = "";
            if (isPassed) {
                resultType = "Passed";
            }else {
                resultType = "Compared";
                viewType = IsLinearView ? "LV_" : "TV_";
            }
            ResultFile = CommonDirectoryPath + @"\" + resultType + "_" + viewType + CommonName + "_" + DateTime.Now.ToString("ddMMyyyy_HH-mm-ss");
        }

        public void IfCancelRequested() {
            var cancelationToken = CancellationToken.Token;
            if (cancelationToken.IsCancellationRequested) {
                cancelationToken.ThrowIfCancellationRequested();
            }
        }

        private void InitializeClock() {
            Timer.Interval = TimeSpan.FromSeconds(1);
            Timer.Tick += TimerTick;
        }

        public void StartClock() {
            InitializeClock();
            Stopwatch.Start();
            Timer.Start();
        }

        private void TimerTick(object sender, EventArgs e) {
            if (Stopwatch.IsRunning) {
                TimeSpan ts = Stopwatch.Elapsed;
                ElapsedTime = string.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
            }
        }

        public void StopClock() {
            Timer.Stop();
        }

        private void SetDeviationsView() {
            if (IsLinearView) {
                DeviationsView = "Linear Deviations View";
            }else {
                DeviationsView = "Tabular Deviations View";
            }
        }
    }
}
