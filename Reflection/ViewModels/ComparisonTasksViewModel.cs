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
    public class ComparisonTasksViewModel  {
        private int comparisonCount;
        public ObservableCollection<ComparisonTask> AllComparisonDetails { get; }
        public ICollectionView AllComparisonTasksView {
            get { return CollectionViewSource.GetDefaultView(AllComparisonDetails); }
        }
        IFileReader FileReader { get; set; }

        public ComparisonTasksViewModel() {
            AllComparisonDetails = new ObservableCollection<ComparisonTask>();         
            comparisonCount = 1;
            FileReader = new FileReader();
        }

        public async void ImportConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "ImportConfiguration") {
                var importConfiguration = (ImportConfiguration)sender;
                var comparisonTask = new ComparisonTask(comparisonCount++, importConfiguration.MasterFilePath, importConfiguration.TestFilePath);
                if (AllComparisonDetails.Count == 99) {
                    AllComparisonDetails.RemoveAt(0);
                }
                AllComparisonDetails.Add(comparisonTask);
                var comparisonProcessor = new ComparisonProcessor(FileReader, importConfiguration, comparisonTask);
                var t = new Task(() => comparisonProcessor.PrepareForComparison());
                t.Start();
                try {
                    await t;
                } catch (Exception ae) {
                    comparisonTask.Status = Status.Error;
                    comparisonTask.ErrorMessage = ae.Message;
                }
            }
        }



    }
}
