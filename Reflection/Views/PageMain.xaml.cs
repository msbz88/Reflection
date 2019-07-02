using System;
using System.Collections.Generic;
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
        public EventHandler ShowViewResult { get; set; }
        public ComparisonTasksViewModel ComparisonTasksViewModel { get; set; }
        
        public PageMain(ComparisonTasksViewModel comparisonTasksViewModel) {
            ComparisonTasksViewModel = comparisonTasksViewModel;
            this.DataContext = ComparisonTasksViewModel;
            InitializeComponent();
            CollectionViewSource.GetDefaultView(lvComparisonDetails.ItemsSource).Filter = UserFilter;          
        }

        private void ButtonOpenFilesClick(object senderIn, RoutedEventArgs eIn) {
            OpenFiles?.Invoke(senderIn, eIn);
        }

        private void TextBoxSearchFileTextChanged(object sender, TextChangedEventArgs e) {
            CollectionViewSource.GetDefaultView(lvComparisonDetails.ItemsSource).Refresh();
        }

        private void ButtonOpenFolder(object senderIn, RoutedEventArgs eIn) {
            var listViewItem = GetAncestorOfType<ListViewItem>(senderIn as Button);
            if (listViewItem != null) {
                var selectedItem = (ComparisonTask)listViewItem.DataContext;
                string argument = "/select, \"" + selectedItem.ImportConfiguration.MasterFilePath + "\"";
                try {
                    //Process.Start("explorer.exe", argument);
                    Process.Start(selectedItem.CommonDirectoryPath);
                } catch (Exception) {
                    //Process.Start(selectedItem.CommonDirectoryPath);
                }
            }
        }

        private void ButtonViewResult(object senderIn, RoutedEventArgs eIn) {
            var listViewItem = GetAncestorOfType<ListViewItem>(senderIn as Button);
            if (listViewItem != null) {
                var selectedItem = (ComparisonTask)listViewItem.DataContext;
                ShowViewResult?.Invoke(selectedItem, null);
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
                if (comparisonTask.Status == Status.Error) {
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
    }
}
