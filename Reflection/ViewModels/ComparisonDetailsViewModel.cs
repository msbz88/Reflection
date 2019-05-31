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
    public class ComparisonDetailViewModel  {
        private int comparisonCount;
        public ObservableCollection<ComparisonDetail> AllComparisonDetails { get; }
        public ICollectionView AllComparisonDetailsView {
            get { return CollectionViewSource.GetDefaultView(AllComparisonDetails); }
        }

        //private string search;

        //public event PropertyChangedEventHandler PropertyChanged;

        //public string Search {
        //    get { return search; }
        //    set {
        //        search = value;
        //        NotifyPropertyChanged("Search");
        //        AllComparisonDetailsView.Refresh();  
        //    }
        //}

        public ComparisonDetailViewModel() {
            AllComparisonDetails = new ObservableCollection<ComparisonDetail>();         
            comparisonCount = 1;
        }

        public void ImportConfigurationPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "ImportConfiguration") {
                var importConfiguration = (ImportConfiguration)sender;
                var comparisonDetail = new ComparisonDetail(comparisonCount++, importConfiguration.MasterFilePath, importConfiguration.TestFilePath);
                AllComparisonDetails.Add(comparisonDetail);               
            }
        }



    }
}
