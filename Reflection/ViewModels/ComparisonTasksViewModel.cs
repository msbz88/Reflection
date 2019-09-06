using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Microsoft.Office.Interop.Excel;
using Reflection.Models;
using Reflection.Models.Interfaces;

namespace Reflection.ViewModels {
    public class ComparisonTasksViewModel {
        private int comparisonCount;
        public ObservableCollection<ComparisonTask> AllComparisonDetails { get; }
        public ICollectionView AllComparisonTasksView {
            get { return CollectionViewSource.GetDefaultView(AllComparisonDetails); }
        }
        IFileReader FileReader { get; set; }
        ComparisonProcessor ComparisonProcessor;
        public bool IsLinearView { get; set; }
        public bool IsDeviationsOnly { get; set; }     

        public ComparisonTasksViewModel() {
            AllComparisonDetails = new ObservableCollection<ComparisonTask>();
            comparisonCount = 1;
            FileReader = new FileReader();
            ComparisonProcessor = new ComparisonProcessor();
            IsLinearView = true;
            IsDeviationsOnly = true;
        }       

        public void AddComparisonTask(ImportConfiguration masterConfiguration, ImportConfiguration testConfiguration) {
            var comparisonTask = new ComparisonTask(comparisonCount++, masterConfiguration, testConfiguration);
            if (AllComparisonDetails.Count == 99) {
                AllComparisonDetails.RemoveAt(0);
                comparisonCount = 1;
            }
            comparisonTask.IsLinearView = IsLinearView;
            comparisonTask.IsDeviationsOnly = IsDeviationsOnly;
            AllComparisonDetails.Add(comparisonTask);
            TriggerComparison();          
        }

        private async void TriggerComparison() {
            while (true) {
                if (!ComparisonProcessor.IsBusy) {
                    var comparisonTask = AllComparisonDetails.Where(item => item.Status == Status.Queued).FirstOrDefault();
                    if (comparisonTask == null) {
                        return;
                    }
                    try {                       
                        await Task.Run(() => ComparisonProcessor.StartComparison(FileReader, comparisonTask));
                    } catch (Exception e) {
                        var cancelTask = e as OperationCanceledException;
                        if (cancelTask != null) {
                            comparisonTask.StopClock();
                            comparisonTask.Status = Status.Canceled;
                            comparisonTask.ErrorMessage = "Task was canceled by user";
                            ComparisonProcessor.IsBusy = false;
                            CleanUpTempFilesOnError(comparisonTask);
                        } else {
                            comparisonTask.StopClock();
                            comparisonTask.Status = Status.Error;
                            comparisonTask.ErrorMessage = e.Message;
                            ComparisonProcessor.IsBusy = false;
                            WriteLog(comparisonTask);
                            CleanUpTempFilesOnError(comparisonTask);
                        }
                    }
                } else {
                    return;
                }
            }
        }

        private async Task<bool> RunComparison(ComparisonTask comparisonTask) {
            return await Task.Run(() => ComparisonProcessor.StartComparison(FileReader, comparisonTask));
        }

        public void DeleteTask(ComparisonTask comparisonTask) {
            AllComparisonDetails.Remove(comparisonTask);
            comparisonTask.CancellationToken.Cancel();
        }

        private void WriteLog(ComparisonTask comparisonTask) {
            try {
                string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Replace("SCDOM\\", "");
                string logFile = @"O:\DATA\COMMON\core\log\" + userName + "_result.log";
                List<string> content = new List<string>();
                content.Add("StartTime: " + comparisonTask.StartTime);
                content.Add("MasterFilePath: " + comparisonTask.MasterConfiguration.FilePath);
                content.Add("TestFilePath: " + comparisonTask.TestConfiguration.FilePath);
                content.Add("IsLinearView: " + comparisonTask.IsLinearView.ToString());
                content.Add("MasterRowsCount: " + comparisonTask.MasterRowsCount.ToString());
                content.Add("TestRowsCount: " + comparisonTask.TestRowsCount.ToString());
                content.Add("ActualRowsDiff: " + comparisonTask.ActualRowsDiff.ToString());
                content.Add("RowsWithDeviations: " + comparisonTask.RowsWithDeviations.ToString());
                content.Add("ExtraMasterCount: " + comparisonTask.ExtraMasterCount.ToString());
                content.Add("ExtraTestCount: " + comparisonTask.ExtraTestCount.ToString());
                content.Add("Status: " + comparisonTask.Status);
                content.Add("Time: " + comparisonTask.ElapsedTime);
                content.Add("Progress: " + comparisonTask.Progress);
                content.Add("ErrorMessage: " + comparisonTask.ErrorMessage);
                content.Add("--------------------------------------------------------------------------");
                File.AppendAllLines(logFile, content);
            } catch (Exception) { }
        }

        private void CleanUpTempFilesOnError(ComparisonTask comparisonTask) {
            if(File.Exists(comparisonTask.CommonDirectoryPath + "\\Passed.temp")) {
                File.Delete(comparisonTask.CommonDirectoryPath + "\\Passed.temp");
            }
        }

    }
}
