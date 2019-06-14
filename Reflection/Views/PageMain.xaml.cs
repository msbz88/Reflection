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
        public EventHandler OpenFiles { get; set; }
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

        private void OnStatusUpdated(object senderIn, RoutedEventArgs eIn) {
            var TextBlockStatus = (TextBlock)senderIn;
            switch (TextBlockStatus.Text) {
                case "Executing":
                TextBlockStatus.Foreground = new SolidColorBrush(Colors.Orange);
                break;
                case "Passed":
                TextBlockStatus.Foreground = new SolidColorBrush(Colors.Green);
                break;
                case "Failed":
                TextBlockStatus.Foreground = new SolidColorBrush(Colors.Red);
                break;
                case "Error":
                TextBlockStatus.Foreground = new SolidColorBrush(Colors.Red);
                break;
            }
        }

        private void ButtonOpenFolder(object senderIn, RoutedEventArgs eIn) {
            var selectedItem = (ComparisonTask)lvComparisonDetails.SelectedItem;
            Process.Start(selectedItem.CommonDirectoryPath);
        }

        private void ButtonViewResult(object senderIn, RoutedEventArgs eIn) {
            //not implemented yet
        }

        private bool UserFilter(object item) {
            if (string.IsNullOrEmpty(TextBoxSearchFile.Text))
                return true;
            var comparisonTask = (ComparisonTask)item;
            return (comparisonTask.MasterFileName.ToLower().Contains(TextBoxSearchFile.Text.ToLower())
                    || comparisonTask.TestFileName.ToLower().Contains(TextBoxSearchFile.Text.ToLower())
                    || comparisonTask.StartTime.Contains(TextBoxSearchFile.Text.ToLower()));
        }

        private Button _previousButton;
        private void ListViewItemSelected(object sender, SelectionChangedEventArgs e) {
            if (_previousButton != null)
                _previousButton.Visibility = Visibility.Collapsed;

            // Make sure an item is selected
            if (lvComparisonDetails.SelectedItems.Count == 0)
                return;

            // Get the first SelectedItem (use a List<object> when 
            // the SelectionMode is set to Multiple)
            object selectedItem = lvComparisonDetails.SelectedItems[0];
            // Get the ListBoxItem from the ContainerGenerator
            ListViewItem listBoxItem = lvComparisonDetails.ItemContainerGenerator.ContainerFromItem(selectedItem) as ListViewItem;
            if (listBoxItem == null)
                return;

            // Find a button in the WPF Tree
            Button button = FindDescendant<Button>(listBoxItem);
            if (button == null)
                return;

            button.Visibility = Visibility.Visible;
            _previousButton = button;
        }

        /// <summary>
        /// Finds the descendant of a dependency object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The obj.</param>
        /// <returns></returns>
        public static T FindDescendant<T>(DependencyObject obj) where T : DependencyObject {
            // Check if this object is the specified type
            if (obj is T)
                return obj as T;

            // Check for children
            int childrenCount = VisualTreeHelper.GetChildrenCount(obj);
            if (childrenCount < 1)
                return null;

            // First check all the children
            for (int i = 0; i < childrenCount; i++) {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is T)
                    return child as T;
            }

            // Then check the childrens children
            for (int i = 0; i < childrenCount; i++) {
                DependencyObject child = FindDescendant<T>(VisualTreeHelper.GetChild(obj, i));
                if (child != null && child is T)
                    return child as T;
            }

            return null;
        }
    }
}
