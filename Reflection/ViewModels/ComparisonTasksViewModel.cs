﻿using System;
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
        public bool IsLinearView { get; set; } = true;

        public ComparisonTasksViewModel() {
            AllComparisonDetails = new ObservableCollection<ComparisonTask>();
            comparisonCount = 1;
            FileReader = new FileReader();
            ComparisonProcessor = new ComparisonProcessor();
            IsExcelInstaled = Type.GetTypeFromProgID("Excel.Application") == null ? false : true;
        }

        public async void ImportConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "ImportConfiguration") {
                var importConfiguration = (ImportConfiguration)sender;
                if (AllComparisonDetails.Count == 99) {
                    AllComparisonDetails.RemoveAt(0);
                    comparisonCount = 1;
                }
                var comparisonTask = new ComparisonTask(comparisonCount++, importConfiguration);
                comparisonTask.IsLinearView = IsLinearView;
                AllComparisonDetails.Add(comparisonTask);
                if (!ComparisonProcessor.IsBusy) {
                    while (true) {
                        await TriggerComparison(comparisonTask);
                        var nextTask = AllComparisonDetails.Where(task => task.Status == Status.Queued).FirstOrDefault();
                        if (nextTask != null) {
                            comparisonTask = nextTask;
                        } else {
                            break;
                        }
                    }
                }
            }
        }

        private async Task TriggerComparison(ComparisonTask comparisonTask) {
            var task = new Task(() => ComparisonProcessor.StartComparison(FileReader, comparisonTask));
            task.Start();
            try {
                await task;
            } catch (Exception e) {
                comparisonTask.Status = Status.Error;
                comparisonTask.ErrorMessage = e.Message;
                if (comparisonTask.ExcelApplication!=null) {
                    comparisonTask.ExcelApplication.Result.Quit();
                }              
                ComparisonProcessor.IsBusy = false;
            }
        }

        public bool TryOpenExcel(ComparisonTask comparisonTask) {
            if (IsExcelInstaled) {
                Task.Run(() => Process.Start(comparisonTask.ResultFile + ".xlsx"));
                return true;
            } else {
                return false;
            }
        }



    }
}
