using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
        public EventHandler ChildWindowRaised { get; set; }
        public ComparisonTasksViewModel ComparisonDetailViewModel { get; set; }
        PageImport PageImport { get; set; }
        PageMain PageMain { get; set; }
        PageViewResult PageViewResult { get; set; }

        public MainWindow() {
            InitializeComponent();
            this.Title = "Reflection (version " + Models.Version.GetVersion() + ")";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ComparisonDetailViewModel = new ComparisonTasksViewModel();
            PageMain = new PageMain(ComparisonDetailViewModel);
            Main.Content = PageMain;
            PageMain.OpenFiles += OnOpenFiles;
            PageMain.Error += OnError;
            PageMain.ShowViewResult += OnShowViewResult;
            PageImport = new PageImport();
            PageImport.FilesLoaded += OnFilesLoaded;
            PageImport.GoBack += OnGoBack;
            PageImport.ImportViewModel.PropertyChanged += ComparisonDetailViewModel.ImportConfigurationPropertyChanged;
            ChildWindowRaised += OnChildWindowRaised;
            PageMain.ChangeDeviationsView += OnChangeDeviationsView;
        }

        private void OnOpenFiles(object sender, EventArgs e) {
            PageImport.SelectFiles();            
            if (PageImport.IsSingle) {
                Main.Content = PageImport;
            }
        }

        private void OnFilesLoaded(object sender, EventArgs e) {
            Main.Content = PageMain;
            if (PageViewResult != null) {
                PageViewResult.GoBack -= OnGoBack;
                PageViewResult.Error -= OnError;
            }          
        }

        private void OnGoBack(object sender, EventArgs e) {
            Main.Content = PageMain;
            StatusBarContent.Text = "";
        }

        private void OnClosing(object sender, CancelEventArgs e) {
            var isTasksExist = ComparisonDetailViewModel.AllComparisonDetails.Any();
            if (isTasksExist) {
                var userAnswer = MessageBox.Show("Do you want to exit?\nComparison history will be lost.", "", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (userAnswer == MessageBoxResult.Yes) {
                    e.Cancel = false;
                } else {
                    e.Cancel = true;
                }
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
            PageImport.ResetPopUp();
        }

        private void Window_LocationChanged(object sender, EventArgs e) {
            PageImport.ResetPopUp();
        }

        private void OnChildWindowRaised(object senderIn, EventArgs eIn) {
            var childWindow = (Window)senderIn;
            childWindow.Closed += OnChildWindowClosed;
            GrayWindow.Visibility = Visibility.Visible;
        }

        private void OnChildWindowClosed(object senderIn, EventArgs eIn) {
            var childWindow = (Window)senderIn;
            childWindow.Closed -= OnChildWindowClosed;
            GrayWindow.Visibility = Visibility.Collapsed;
        }

        private void OnError(object sender, EventArgs e) {
            var erorrMessage = (string)sender;
            StatusBarContent.Foreground = new SolidColorBrush(Colors.Red);
            StatusBarContent.Text = erorrMessage;
        }

        private void OnShowViewResult(object sender, EventArgs e) {
            PageViewResult = new PageViewResult();
            PageViewResult.GoBack += OnGoBack;
            PageViewResult.Error += OnError;
            Main.Content = PageViewResult;
            var comparisonTask = (ComparisonTask)sender;
            if(comparisonTask!=null)
            PageViewResult.PrintFileContent(comparisonTask.ResultFile + ".txt", comparisonTask.ImportConfiguration.Delimiter, comparisonTask.ImportConfiguration.Encoding);
        }

        private void OnChangeDeviationsView(object senderIn, EventArgs eIn) {
            ComparisonDetailViewModel.IsLinearView = (bool)senderIn;
        }


    }
}

