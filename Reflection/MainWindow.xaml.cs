using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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

        public MainWindow() {
            InitializeComponent();
            this.Title = "Reflection (version " + Models.Version.GetVersion() + ")";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ComparisonDetailViewModel = new ComparisonTasksViewModel();
            PageMain = new PageMain(ComparisonDetailViewModel);
            Main.Content = PageMain;
            PageMain.OpenFiles += OnOpenFiles;
            PageMain.Error += OnError;
            PageImport = new PageImport();
            PageImport.FilesLoaded += OnFilesLoaded;
            PageImport.GoBack += OnGoBack;
            PageImport.Message += OnMessage;
            ChildWindowRaised += OnChildWindowRaised;
            PageMain.LinearView += OnChangeDeviationsView;
            PageMain.ResultFileView += OnChangeResultView;
            PageMain.BackToImport += OnBackToImport;
        }

        private void OnOpenFiles(object sender, EventArgs e) {
            PageImport.SingleFileView += OnIsSingle;
            PageImport.SelectFiles();
        }

        private void OnIsSingle(object sender, EventArgs e) {
            Main.Content = PageImport;           
            PageImport.SingleFileView -= OnIsSingle;
        }

        private void OnFilesLoaded(object sender, EventArgs e) {
            StatusBarContent.Text = "";
            var configs = (List<ImportConfiguration>)sender;
            ComparisonDetailViewModel.AddComparisonTask(configs[0], configs[1]);           
            Main.Content = PageMain;         
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
                    DeleteInstance(@"O:\DATA\COMMON\core\");
                    e.Cancel = false;
                } else {
                    e.Cancel = true;
                }
            }
        }

        private void DeleteInstance(string pathOrigin) {
            try {
                string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Replace("SCDOM\\", "").ToUpper();
                string path = pathOrigin + "Reflection_" + userName + ".exe";
                if (File.Exists(path)) {
                    File.Delete(path);
                }
            } catch (Exception) {}
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

        private void OnChangeDeviationsView(object senderIn, EventArgs eIn) {
            ComparisonDetailViewModel.IsLinearView = (bool)senderIn;
        }

        private void OnMessage(object sender, EventArgs e) {
            StatusBarContent.Foreground = new SolidColorBrush(Colors.Black);
            StatusBarContent.Text = sender.ToString();
        }

        private void OnChangeResultView(object senderIn, EventArgs eIn) {
            ComparisonDetailViewModel.IsDeviationsOnly = (bool)senderIn;
        }

        private void ButtonHelpClick(object sender, RoutedEventArgs e) {
            try {
                dynamic ie = Activator.CreateInstance(Type.GetTypeFromProgID("InternetExplorer.Application"));
                ie.AddressBar = false;
                ie.MenuBar = false;
                ie.ToolBar = false;
                ie.Visible = true;
                ie.Navigate(@"O:\DATA\COMMON\core\doc\doc.html");
            } catch (Exception) {
                StatusBarContent.Foreground = new SolidColorBrush(Colors.Red);
                StatusBarContent.Text = "Sorry, documentation is currently unavailable";
            }
        }

        private void OnBackToImport(object senderIn, EventArgs eIn) {
            var compTask = (ComparisonTask)senderIn;
            PageImport.SingleFileView += OnIsSingle;
            PageImport.ReturnToView(compTask);
        }
    }
}

