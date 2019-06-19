using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
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

        public ComparisonTasksViewModel() {
            AllComparisonDetails = new ObservableCollection<ComparisonTask>();
            comparisonCount = 1;
            FileReader = new FileReader();
            ComparisonProcessor = new ComparisonProcessor();
        }

        public void ImportConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "ImportConfiguration") {
                var importConfiguration = (ImportConfiguration)sender;
                var comparisonTask = new ComparisonTask(comparisonCount++, importConfiguration);
                if (AllComparisonDetails.Count == 99) {
                    AllComparisonDetails.RemoveAt(0);
                }
                AllComparisonDetails.Add(comparisonTask);
                if (!ComparisonProcessor.IsBusy) {
                    TriggerComparison(comparisonTask);
                }
            }
        }

        private async void TriggerComparison(ComparisonTask comparisonTask) {
            while (true) {
                var t = new Task(() => ComparisonProcessor.StartComparison(FileReader, comparisonTask));
                t.Start();
                try {
                    await t;
                    var nextTask = AllComparisonDetails.Where(task => task.Status == Status.Queued).OrderBy(task => task.ComparisonId).FirstOrDefault();
                    if (nextTask != null) {
                        comparisonTask = nextTask;
                    } else {
                        break;
                    }
                } catch (Exception ae) {
                    comparisonTask.Status = Status.Error;
                    comparisonTask.ErrorMessage = ae.Message;
                }
            }
        }



    }
}
