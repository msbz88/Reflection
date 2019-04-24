using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using Reflection.Models;
using Reflection.ViewModels;
using Reflection.Views;

namespace Reflection {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged {
        public ComparisonDetailViewModel ComparisonDetailViewModel { get; set; }

        public MainWindow() {
            ComparisonDetailViewModel = new ComparisonDetailViewModel();
            this.DataContext = ComparisonDetailViewModel;
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            CollectionViewSource.GetDefaultView(lvComparisonDetails.ItemsSource).Filter = UserFilter;    
        }

        private void ButtonOpenFilesClick(object senderIn, RoutedEventArgs eIn) {
            var importView = new ImportView();
            importView.ImportViewModel.PropertyChanged += ComparisonDetailViewModel.ImportConfigurationPropertyChanged;
            if (importView.IsReady) {
                importView.Show();
            }         
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected virtual void OnPropertyChanged(string prop) {
            PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        private double pctComplete = 10.0;
        public double PctComplete {
            get { return pctComplete; }
            set {
                if (pctComplete != value) {
                    pctComplete = value;
                    OnPropertyChanged("PctComplete");
                }
            }
        }

        private void Button_Click() {
            PctComplete = 0.0;
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += (s, ea) =>
            {
                PctComplete += 1.0;
                if (PctComplete >= 100.0)
                    timer.Stop();
            };
            timer.Interval = new TimeSpan(0, 0, 0, 0, 30); 
            timer.Start();
        }

        private void TextBoxSearchFileTextChanged(object sender, TextChangedEventArgs e) {
            CollectionViewSource.GetDefaultView(lvComparisonDetails.ItemsSource).Refresh();
            Button_Click();
        }

        private bool UserFilter(object item) {
            if (string.IsNullOrEmpty(TextBoxSearchFile.Text))
                return true;
            var comparisonDetail = (ComparisonDetail)item;
            return (comparisonDetail.MasterFileName.ToLower().Contains(TextBoxSearchFile.Text.ToLower())
                    || comparisonDetail.TestFileName.ToLower().Contains(TextBoxSearchFile.Text.ToLower()));
        }


    }
}

