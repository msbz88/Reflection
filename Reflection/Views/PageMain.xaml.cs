using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Reflection.Models;
using Reflection.ViewModels;

namespace Reflection.Views {
    /// <summary>
    /// Interaction logic for PageMain.xaml
    /// </summary>
    public partial class PageMain : Page {
        public EventHandler Error { get; set; }
        public EventHandler OpenFiles { get; set; }
        public EventHandler LinearView { get; set; }
        public EventHandler ResultFileView { get; set; }
        public ComparisonTasksViewModel ComparisonTasksViewModel { get; set; }
        ComparisonTask currectComparisonTask;
        Button stopRestartButton;

        public PageMain(ComparisonTasksViewModel comparisonTasksViewModel) {
            ComparisonTasksViewModel = comparisonTasksViewModel;
            this.DataContext = ComparisonTasksViewModel;
            InitializeComponent();
            CollectionViewSource.GetDefaultView(lvComparisonDetails.ItemsSource).Filter = UserFilter;
            SetWelcomeTextBlock();
        }

        private void ButtonOpenFilesClick(object senderIn, RoutedEventArgs eIn) {
            ClearWelcomeTextBlock();
            OpenFiles?.Invoke(senderIn, eIn);
        }

        private void TextBoxSearchFileTextChanged(object sender, TextChangedEventArgs e) {
            CollectionViewSource.GetDefaultView(lvComparisonDetails.ItemsSource).Refresh();
        }

        private void ButtonOpenFolder(object senderIn, RoutedEventArgs eIn) {
            var listViewItem = GetAncestorOfType<ListViewItem>(senderIn as Button);
            if (listViewItem != null) {
                var selectedItem = (ComparisonTask)listViewItem.DataContext;
                try {
                    Process.Start(selectedItem.CommonDirectoryPath);
                } catch (Exception ex) {
                    Error?.Invoke(ex.Message, null);
                }
            }
        }

        private void ButtonViewResult(object senderIn, RoutedEventArgs eIn) {
            var listViewItem = GetAncestorOfType<ListViewItem>(senderIn as Button);
            if (listViewItem != null) {
                var selectedItem = (ComparisonTask)listViewItem.DataContext;
                var isExcelOpened = Task.Run(() => ComparisonTasksViewModel.TryOpenExcel(selectedItem));
                if (!isExcelOpened.Result) {
                    OpenTextFile(selectedItem.ResultFile + ".txt");
                }
            }
        }

        private bool UserFilter(object item) {
            if (string.IsNullOrEmpty(TextBoxSearchFile.Text))
                return true;
            var comparisonTask = (ComparisonTask)item;
            return (comparisonTask.MasterFileName.ToLower().Contains(TextBoxSearchFile.Text.ToLower())
                    || comparisonTask.TestFileName.ToLower().Contains(TextBoxSearchFile.Text.ToLower())
                    || comparisonTask.Status.ToString().ToLower().Contains(TextBoxSearchFile.Text.ToLower())
                    || comparisonTask.StartTime.ToString().Contains(TextBoxSearchFile.Text));
        }

        private void ListViewItemSelected(object sender, SelectionChangedEventArgs e) {
            var comparisonTask = (ComparisonTask)lvComparisonDetails.SelectedItem;
            if (comparisonTask != null) {
                if (comparisonTask.Status == Status.Error || comparisonTask.Status == Status.Canceled) {
                    Error?.Invoke(comparisonTask.ErrorMessage, null);
                } else {
                    Error?.Invoke("", null);
                }
            }
        }

        private T GetAncestorOfType<T>(FrameworkElement child) where T : FrameworkElement {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent != null && !(parent is T))
                return (T)GetAncestorOfType<T>((FrameworkElement)parent);
            return (T)parent;
        }

        private void TabularDeviationsView_Checked(object sender, RoutedEventArgs e) {
            LinearView?.Invoke(false, null);
        }

        private void LinearDeviationsView_Checked(object sender, RoutedEventArgs e) {
            LinearView?.Invoke(true, null);
        }

        private void ProgressBarMouseEnter(object sender, MouseEventArgs e) {
            var progressBar = sender as ProgressBar;
            stopRestartButton = progressBar.Template.FindName("StopRestartButton", progressBar) as Button;
            if (stopRestartButton != null) {
                currectComparisonTask = (ComparisonTask)progressBar.DataContext;
                if (currectComparisonTask.Status == Status.Executing) {
                    stopRestartButton.Background = new SolidColorBrush(Colors.Red);
                    stopRestartButton.Content = "Stop";
                    currectComparisonTask.PropertyChanged += OnComparisonTaskStatusChanged;
                    stopRestartButton.Visibility = Visibility.Visible;
                } else if (currectComparisonTask.Status == Status.Failed || currectComparisonTask.Status == Status.Canceled) {
                    stopRestartButton.Background = new SolidColorBrush(Color.FromArgb(0xFF, 20, 0xC5, 20));
                    stopRestartButton.Content = "Restart";
                    currectComparisonTask.PropertyChanged += OnComparisonTaskStatusChanged;
                    stopRestartButton.Visibility = Visibility.Visible;
                }
            }
        }

        public void OnComparisonTaskStatusChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "Status") {
                this.Dispatcher.Invoke(() => {
                    if (currectComparisonTask.Status == Status.Executing) {
                        stopRestartButton.Background = new SolidColorBrush(Colors.Red);
                        stopRestartButton.Content = "Stop";
                    } else if (currectComparisonTask.Status == Status.Failed || currectComparisonTask.Status == Status.Canceled) {
                        stopRestartButton.Background = new SolidColorBrush(Color.FromArgb(0xFF, 20, 0xC5, 20));
                        stopRestartButton.Content = "Restart";
                    }
                });
            }
        }

        private void ProgressBarMouseLeave(object sender, MouseEventArgs e) {
            if (stopRestartButton != null) {
                stopRestartButton.Visibility = Visibility.Collapsed;
                currectComparisonTask.PropertyChanged -= OnComparisonTaskStatusChanged;
            }
        }

        private void StopRestartButtonClick(object sender, RoutedEventArgs e) {
            if (stopRestartButton.Content.ToString() == "Stop" && currectComparisonTask.Status != Status.Canceling) {
                currectComparisonTask.Status = Status.Canceling;
                currectComparisonTask.CancellationToken.Cancel();
            } else if (stopRestartButton.Content.ToString() == "Restart") {
                ComparisonTasksViewModel.AddComparisonTask(currectComparisonTask.MasterConfiguration, currectComparisonTask.TestConfiguration);
            }
        }

        private void OpenTextFile(string path) {
            try {
                Process.Start("notepad++.exe", path);
            } catch (Exception ex) {
                Error?.Invoke(ex.Message, null);
            }
        }

        private void MenuItemOpenMasterClick(object sender, RoutedEventArgs e) {
            var comparisonTask = lvComparisonDetails.SelectedItem as ComparisonTask;
            if (comparisonTask != null) {
                OpenTextFile(comparisonTask.MasterConfiguration.FilePath);
            }
        }

        private void MenuItemOpenTestClick(object sender, RoutedEventArgs e) {
            var comparisonTask = lvComparisonDetails.SelectedItem as ComparisonTask;
            if (comparisonTask != null) {
                OpenTextFile(comparisonTask.TestConfiguration.FilePath);
            }
        }

        private void ButtonDeleteComparisonTaskClick(object sender, RoutedEventArgs e) {
            var listViewItem = GetAncestorOfType<ListViewItem>(sender as Button);
            if (listViewItem != null) {
                var selectedTask = (ComparisonTask)listViewItem.DataContext;
                if(selectedTask.Status != Status.Canceling) {
                    ComparisonTasksViewModel.DeleteTask(selectedTask);
                    Error?.Invoke("", null);
                }else {
                    Error?.Invoke("Unable to delete task with \"Canceling\" status", null);
                }
            }
        }

        private void ClearWelcomeTextBlock() {
            WelcomeTextBlock.Text = "";
            WelcomeTextBlock.Visibility = Visibility.Collapsed;
        }

        private async void SetWelcomeTextBlock() {
            string userName = "";
            try {
                userName = await Task.Run(() => System.DirectoryServices.AccountManagement.UserPrincipal.Current.DisplayName);
                WelcomeTextBlock.Text = "Hello, " + userName.Split(' ')[0] + "!";
            } catch (Exception) {
                WelcomeTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void TextBlockStatusTargetUpdated(object sender, DataTransferEventArgs e) {
            var listViewItem = GetAncestorOfType<ListViewItem>(sender as TextBlock);
            ContentPresenter myContentPresenter = FindVisualChild<ContentPresenter>(listViewItem);
            DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
            Border BorderListItem = (Border)myDataTemplate.FindName("BorderListItem", myContentPresenter);
            var comparisonTask = (ComparisonTask)listViewItem.DataContext;
            if (BorderListItem != null && comparisonTask.Status == Status.Error) {
                BorderListItem.Effect = new DropShadowEffect { Color = Color.FromArgb(255, 255, 00, 00), BlurRadius = 5, Opacity = 0.2 };
            }
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++) {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void DeviationsOnly_Checked(object sender, RoutedEventArgs e) {
            ResultFileView?.Invoke(true, null);
        }

        private void DeviationsAndPassed_Checked(object sender, RoutedEventArgs e) {
            ResultFileView?.Invoke(false, null);
        }
    }
}
