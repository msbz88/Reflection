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
        bool IsExcelInstaled = Type.GetTypeFromProgID("Excel.Application") == null ? false : true;
        public bool IsLinearView { get; set; }
        public bool IsDeviationsOnly { get; set; }     

        public ComparisonTasksViewModel() {
            AllComparisonDetails = new ObservableCollection<ComparisonTask>();
            comparisonCount = 1;
            FileReader = new FileReader();
            ComparisonProcessor = new ComparisonProcessor();
            IsExcelInstaled = Type.GetTypeFromProgID("Excel.Application") == null ? false : true;
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
                        } else {
                            comparisonTask.StopClock();
                            comparisonTask.Status = Status.Error;
                            comparisonTask.ErrorMessage = e.Message;
                            ComparisonProcessor.IsBusy = false;
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

        public bool TryOpenExcel(ComparisonTask comparisonTask) {
            if (IsExcelInstaled) {
                if (comparisonTask.IsLinearView) {
                    Task.Run(() => Process.Start(comparisonTask.ResultFile + ".xlsm"));
                }else {
                    Task.Run(() => Process.Start(comparisonTask.ResultFile + ".xlsx"));
                }              
                return true;
            } else {
                return false;
            }
        }

        public void DeleteTask(ComparisonTask comparisonTask) {
            AllComparisonDetails.Remove(comparisonTask);
            comparisonTask.CancellationToken.Cancel();
        }


    }
}
