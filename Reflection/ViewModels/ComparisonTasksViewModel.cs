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

namespace Reflection.ViewModels {
    public class ComparisonTasksViewModel  {
        private int comparisonCount;
        public ObservableCollection<ComparisonTask> AllComparisonDetails { get; }
        public ICollectionView AllComparisonDetailsView {
            get { return CollectionViewSource.GetDefaultView(AllComparisonDetails); }
        }

        public ComparisonTasksViewModel() {
            AllComparisonDetails = new ObservableCollection<ComparisonTask>();         
            comparisonCount = 1;
        }

        public void ImportConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "ImportConfiguration") {
                var importConfiguration = (ImportConfiguration)sender;
                var comparisonDetail = new ComparisonTask(comparisonCount++, importConfiguration.MasterFilePath, importConfiguration.TestFilePath);
                if (AllComparisonDetails.Count == 99) {
                    AllComparisonDetails.RemoveAt(0);
                }
                AllComparisonDetails.Add(comparisonDetail);               
            }
        }



    }
}
