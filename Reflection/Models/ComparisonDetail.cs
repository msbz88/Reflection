using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class ComparisonDetail : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        public int ComparisonId { get; private set; }
        public string MasterFileName { get; private set; }
        public string TestFileName { get; private set; }
        int masterRowsCount;
        public int MasterRowsCount {
            get { return masterRowsCount; }
            set {
                masterRowsCount = value;
                NotifyPropertyChanged("MasterRowsCount");
            }
        }
        int testRowsCount;
        public int TestRowsCount {
            get { return masterRowsCount; }
            set {
                testRowsCount = value;
                NotifyPropertyChanged("TestRowsCount");
            }
        }
        public int ActualRowsDiff { get; set; }
        public int ComparedRows { get; set; }
        public int ExtraMasterCount { get; set; }
        public int ExtraTestCount { get; set; }
        public DateTime StartTime { get; }
        public bool IsComplited { get; set; }
        public string Message { get; set; }

        public ComparisonDetail(int comparisonId, string masterFileName, string testFileName) {
            ComparisonId = comparisonId;
            MasterFileName = masterFileName;
            TestFileName = testFileName;          
            StartTime = DateTime.Now;
        }

        public void NotifyPropertyChanged(string propName) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
