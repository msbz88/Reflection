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
    public partial class MainWindow : Window {
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

        private void TextBoxSearchFileTextChanged(object sender, TextChangedEventArgs e) {
            CollectionViewSource.GetDefaultView(lvComparisonDetails.ItemsSource).Refresh();
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

