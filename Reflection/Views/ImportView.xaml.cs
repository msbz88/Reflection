using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;
using Microsoft.Win32;
using Reflection.ViewModels;

namespace Reflection.Views {
    /// <summary>
    /// Interaction logic for ImportView.xaml
    /// </summary>
    public partial class ImportView : Window {
        ImportViewModel ImportViewModel { get; set; }
        public bool IsReady { get; private set; }

        public ImportView() {
            InitializeComponent();
            IsReady = false;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ImportViewModel = new ImportViewModel();
            SelectFiles();
        }

        private void SelectFiles() {
            while (true) {
                ImportViewModel.PathMasterFile = AskFilePath("Master");
                if (!string.IsNullOrEmpty(ImportViewModel.PathMasterFile)) {
                    ImportViewModel.PathTestFile = AskFilePath("Test");
                    if (!string.IsNullOrEmpty(ImportViewModel.PathTestFile)) {
                        try {
                            ImportViewModel.CheckIfPathToFilesCorrect();
                        } catch (InvalidOperationException ex) {
                            var userResponse = NotifyUser(ex.Message, MessageBoxButton.YesNoCancel);
                            if (userResponse == MessageBoxResult.Yes) {
                                continue;
                            }else if(userResponse == MessageBoxResult.Cancel) {
                                return;
                            }                        
                        }
                        ImportViewModel.LoadFileForPreview(ImportViewModel.PathMasterFile);
                        PrintFileContent(System.IO.Path.GetFileName(ImportViewModel.PathMasterFile));
                        IsReady = true;
                        break;
                    }
                }
                break;
            }
        }

        private MessageBoxResult NotifyUser(string message, MessageBoxButton messageBoxButton) {
            return MessageBox.Show(message, "Note", messageBoxButton, MessageBoxImage.Question);
        }

        public string AskFilePath(string fileVersion) {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Select " + fileVersion + " file";
            dialog.RestoreDirectory = true;
            if (dialog.ShowDialog() == true) {
                return dialog.FileName;
            }      
            return string.Empty;
        }

        public void PrintFileContent(string fileName) {
            dgData.Columns.Clear();
            int index = 0;
            foreach (var item in ImportViewModel.FileHeaders) {
                var column = new DataGridTextColumn();
                column.Header = item;
                column.Binding = new Binding(string.Format("[{0}]", index));
                dgData.Columns.Add(column);
                index++;
            }
            dgData.DataContext = ImportViewModel.PreviewContent;
            PrintSkippedLines();
            PrintFileDettails(fileName);
        }

        private void PrintFileDettails(string fileName) {
            TextBlockFileName.Text = fileName;
            TextBlock1.Text = "Master: 1000000";
            TextBlock2.Text = "Test: 2000000";
        }

        private void PrintSkippedLines() {
            if (ImportViewModel.SkippedLines.Length > 0) {
                ExpanderSkippedRows.Visibility = Visibility.Visible;
                ExpanderSkippedRows.IsExpanded = true;
                TextBlockSkippedRows.Text = string.Join(Environment.NewLine, ImportViewModel.SkippedLines);
            }
        }

        private void ButtonLoadClick(object senderIn, RoutedEventArgs eIn) {
            ImportViewModel.SetImportConfiguration();
        }

        private void OnExpanded(object senderIn, RoutedEventArgs eIn) {
            Grid.SetRow(dgData, 3);
            Grid.SetRowSpan(dgData, 3);
            Grid.SetRow(TextBoxDelimiter, 3);
            Grid.SetRow(TextBoxHeaderRow, 4);
            Grid.SetRow(TextBoxEncoding, 5);
        }

        private void OnCollapsed(object senderIn, RoutedEventArgs eIn) {
            Grid.SetRow(dgData, 2);
            Grid.SetRowSpan(dgData, 4);
            Grid.SetRow(TextBoxDelimiter, 2);
            Grid.SetRow(TextBoxHeaderRow, 3);
            Grid.SetRow(TextBoxEncoding, 4);
        }

    }
}
