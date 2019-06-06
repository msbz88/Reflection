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
        PageImport PageImport { get; set; }
        PageMain PageMain { get; set; }

        public MainWindow() {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ComparisonDetailViewModel = new ComparisonDetailViewModel();
            PageMain = new PageMain(ComparisonDetailViewModel);
            Main.Content = PageMain;
            PageMain.OpenFiles += OnOpenFiles;
            PageImport = new PageImport();
            PageImport.FilesLoaded += OnFilesLoaded;
            PageImport.GoBack += OnGoBack;
            PageImport.ImportViewModel.PropertyChanged += ComparisonDetailViewModel.ImportConfigurationPropertyChanged;
        }

        private void OnOpenFiles(object sender, EventArgs e) {
            PageImport.SelectFiles();            
            if (PageImport.IsReady) {
                Main.Content = PageImport;
            }
        }

        private void OnFilesLoaded(object sender, EventArgs e) {
            Main.Content = PageMain;
        }

        private void OnGoBack(object sender, EventArgs e) {
            Main.Content = PageMain;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
            PageImport.ResetPopUp();
        }

        private void Window_LocationChanged(object sender, EventArgs e) {
            PageImport.ResetPopUp();
        }


    }
}

