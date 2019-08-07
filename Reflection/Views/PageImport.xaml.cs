using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for PageImport.xaml
    /// </summary>
    public partial class PageImport : Page {
        public ImportViewModel ImportViewModel { get; set; }
        public MatchFileNamesViewModel MatchFileNamesViewModel { get; set; }
        ColumnNamesViewModel ColumnNamesViewModel = new ColumnNamesViewModel();
        public EventHandler FilesLoaded { get; set; }
        public EventHandler GoBack { get; set; }
        public EventHandler IsSingle { get; set; }
        public EventHandler Message { get; set; }
        private string customDelimiter;

        public PageImport() {
            InitializeComponent();
            ImportViewModel = new ImportViewModel();
            ComboBoxDataBinding();
            MatchFileNamesViewModel = new MatchFileNamesViewModel();
            this.DataContext = ImportViewModel;
            ImportViewModel.PropertyChanged += OnIsHeaderExistsChanged;
        }

        public void SelectFiles() {
            MatchFileNamesViewModel.SelectFiles();
            if (MatchFileNamesViewModel.IsReady) {
                if (MatchFileNamesViewModel.MatchedFileNames.Count == 1) {
                    ImportViewModel.PathMasterFile = MatchFileNamesViewModel.MatchedFileNames[0].MasterFilePath;
                    ImportViewModel.PathTestFile = MatchFileNamesViewModel.MatchedFileNames[0].TestFilePath;
                    IsSingle?.Invoke(null, null);
                    AsyncRenderFileWithAnalyseToView(ImportViewModel.PathMasterFile);
                } else {
                    foreach (var item in MatchFileNamesViewModel.MatchedFileNames) {
                        ImportViewModel.PathMasterFile = item.MasterFilePath;
                        ImportViewModel.PathTestFile = item.TestFilePath;
                        ImportViewModel.AnalyseFile(ImportViewModel.PathMasterFile);
                        ImportViewModel.SetImportConfiguration();
                    }
                }
            }
        }

        private void DisplayOnLoadEncoding() {
            var t = comboBoxEncoding.Items.OfType<ComboBoxItem>().Select(item => item.Content.ToString());
            var comboBoxItem = comboBoxEncoding.Items.SourceCollection.Cast<EncodingInfo>().FirstOrDefault(x => x.CodePage == ImportViewModel.Encoding.CodePage);
            comboBoxEncoding.SelectedIndex = comboBoxEncoding.SelectedIndex = comboBoxEncoding.Items.IndexOf(comboBoxItem);
        }

        private string PresentDelimiter(string delimiter) {
            string viewDelimiter = "";
            switch (delimiter) {
                case "\t":
                viewDelimiter = "<\\t> (Tab)";
                break;
                case ";":
                viewDelimiter = "<;> (Semicolon)";
                break;
                case ",":
                viewDelimiter = "<,> (Comma)";
                break;
                case "|":
                viewDelimiter = "<|> (Vertical line)";
                break;
                default:
                viewDelimiter = "<" + delimiter + "> (Custom)";
                break;
            }
            return viewDelimiter;
        }

        private void TextBoxDelimiterGotFocus(object senderIn, RoutedEventArgs eIn) {
            if (PresentDelimiter(ImportViewModel.Delimiter) == TextBoxDelimiter.Text) {
                var delimiter = ImportViewModel.Delimiter;
                if (delimiter == "\t") {
                    delimiter = "\\t";
                }
                TextBoxDelimiter.Text = delimiter;
            } else {
                TextBoxDelimiter.Text = customDelimiter;
            }
        }

        private void TextBoxDelimiterLostFocus(object senderIn, RoutedEventArgs eIn) {
            if (TextBoxDelimiter.Text != ImportViewModel.Delimiter && TextBoxDelimiter.Text != "\\t") {
                customDelimiter = TextBoxDelimiter.Text;
                TextBoxDelimiter.Text = PresentDelimiter(TextBoxDelimiter.Text);
            } else {
                TextBoxDelimiter.Text = PresentDelimiter(ImportViewModel.Delimiter);
            }
        }

        private async void AsyncRenderFileWithAnalyseToView(string path) {
            dgData.Columns.Clear();
            dgData.ItemsSource = null;
            await Task.Run(() => {
                ImportViewModel.AnalyseFile(path);
                Dispatcher.Invoke(() => {
                    InitializeDataGrid();
                    PrintFileInfo(System.IO.Path.GetFileName(path));
                    if (ListBoxAvailableKeys.Visibility == Visibility.Visible) {
                        LoadColumnNames();
                    }
                });
            });
        }

        private async void AsyncRenderFileWithSetPreviewToView(string path) {
            dgData.Columns.Clear();
            dgData.ItemsSource = null;
            await Task.Run(() => {
                ImportViewModel.ManualUpdate(path);
                Dispatcher.Invoke(() => {
                    InitializeDataGrid();
                    PrintSkippedLines();
                    ShowHeaderRow();
                    ShowRowsAndColumnsCount();                   
                    if (ListBoxAvailableKeys.Visibility == Visibility.Visible) {
                        LoadColumnNames();
                    }
                });
            });
        }

        private void ComboBoxSelectionChanged(object senderIn, RoutedEventArgs eIn) {
            var encodingInfo = (EncodingInfo)comboBoxEncoding.SelectedItem;
            ImportViewModel.Encoding = encodingInfo.GetEncoding();
            if (TextBlockFileName.Text == (System.IO.Path.GetFileName(ImportViewModel.PathMasterFile))) {
                //RenderFileToView(ImportViewModel.PathMasterFile);
            } else {
                //RenderFileToView(ImportViewModel.PathTestFile);
            }
        }

        private void ComboBoxDataBinding() {
            EncodingInfo[] codePages = Encoding.GetEncodings();
            comboBoxEncoding.ItemsSource = codePages;
        }

        public void InitializeDataGrid() {
            int index = 0;
            foreach (var item in ImportViewModel.FileHeaders) {
                var column = new DataGridTextColumn();
                column.Header = item.Replace("_", "__");
                column.Binding = new Binding(string.Format("[{0}]", index));
                dgData.Columns.Add(column);
                index++;
            }
            if (dgData.ItemsSource == null) {
                dgData.ItemsSource = ImportViewModel.PreviewContent;
            }
        }

        public void PrintFileInfo(string fileName) {
            PrintSkippedLines();
            ImportViewModel.PreviewFileName = fileName;
            ShowHeaderRow();
            TextBoxDelimiter.Text = PresentDelimiter(ImportViewModel.Delimiter);
            DisplayOnLoadEncoding();
            ShowRowsAndColumnsCount();
        }

        private void ShowRowsAndColumnsCount() {
            var prev = ImportViewModel.PreviewCount < 200 ? "" : " preview";
            Message.Invoke("Rows" + prev + ": " + ImportViewModel.PreviewContent.Count + " Columns: " + ImportViewModel.FileHeaders.Count, null);
        }

        private void ShowHeaderRow() {
           TextBoxHeaderRow.Text = ImportViewModel.RowsToSkip.ToString();
        }

        private void PrintSkippedLines() {
            if (ImportViewModel.SkippedLines.Count > 0) {
                TextBlockSkippedRows.Text = string.Join(Environment.NewLine, ImportViewModel.SkippedLines);
                ExpanderSkippedRows.Visibility = Visibility.Visible;
                ExpanderSkippedRows.IsExpanded = true;
            } else {
                ExpanderSkippedRows.Visibility = Visibility.Collapsed;
                PopupSkipedRows.IsOpen = false;
            }
        }

        private void ButtonExecuteClick(object senderIn, RoutedEventArgs eIn) {
            ImportViewModel.SetImportConfiguration();
            FilesLoaded?.Invoke(senderIn, eIn);
            ResetUserKeys();
        }

        private void ButtonSuggestKeyClick(object senderIn, RoutedEventArgs eIn) {
            if (ColumnNamesViewModel.SelectedKeys.Count > 0) {
                ShowAvailableKeys();
                ShowSelectedKeys();
            } else {
                ShowAvailableKeys();
            }
            LoadColumnNames();
        }

        private void LoadColumnNames() {
            ColumnNamesViewModel.AvailableKeys.Clear();
            int index = 0;
            foreach (var item in ImportViewModel.FileHeaders) {
                var key = new ColumnName(index, item.Replace("_", "__"));
                ColumnNamesViewModel.AvailableKeys.Add(key);
                if (ColumnNamesViewModel.SelectedKeys.Any(k => k.Id == index)) {
                    key.IsChecked = true;
                }
                index++;
            }
            ListBoxAvailableKeys.ItemsSource = ColumnNamesViewModel.AvailableKeys;
            ListBoxSelectedKeys.ItemsSource = ColumnNamesViewModel.SelectedKeys;
        }

        private void ShowAvailableKeys() {
            ButtonExecute.Visibility = Visibility.Collapsed;
            ButtonSuggestKey.Visibility = Visibility.Collapsed;
            Grid.SetColumnSpan(dgData, 1);
            Grid.SetRowSpan(ListBoxAvailableKeys, 2);
            BorderUserKeys.Visibility = Visibility.Visible;
            LabelAvailableKeys.Visibility = Visibility.Visible;
            ListBoxAvailableKeys.Visibility = Visibility.Visible;
            ButtonApplyUserKey.Visibility = Visibility.Visible;
        }

        private void ShowSelectedKeys() {
            Grid.SetRowSpan(ListBoxAvailableKeys, 1);
            PanelSelectedKeys.Visibility = Visibility.Visible;
            SpliterLists.Visibility = Visibility.Visible;
            SeparatorLists.Visibility = Visibility.Visible;
            LabelSelectedKeys.Visibility = Visibility.Visible;
            ListBoxSelectedKeys.Visibility = Visibility.Visible;
        }

        private void HideAvailableKeys() {
            Grid.SetColumnSpan(dgData, 2);
            ButtonExecute.Visibility = Visibility.Visible;
            ButtonSuggestKey.Visibility = Visibility.Visible;
            BorderUserKeys.Visibility = Visibility.Collapsed;
            LabelAvailableKeys.Visibility = Visibility.Collapsed;
            ListBoxAvailableKeys.Visibility = Visibility.Collapsed;
            ButtonApplyUserKey.Visibility = Visibility.Collapsed;
        }

        private void HideSelectedKeys() {
            PanelSelectedKeys.Visibility = Visibility.Collapsed;
            SpliterLists.Visibility = Visibility.Collapsed;
            SeparatorLists.Visibility = Visibility.Collapsed;
            LabelSelectedKeys.Visibility = Visibility.Collapsed;
            ListBoxSelectedKeys.Visibility = Visibility.Collapsed;
            Grid.SetRowSpan(ListBoxAvailableKeys, 2);
        }

        private void ButtonApplyUserKeyClick(object senderIn, RoutedEventArgs eIn) {
            HideSelectedKeys();
            HideAvailableKeys();
            if (ColumnNamesViewModel.SelectedKeys.Count > 0) {
                ButtonSuggestKey.Content = "Show Key";
                ImportViewModel.UserKeys = ColumnNamesViewModel.SelectedKeys.Select(item => item.Id).ToList();
            } else {
                ButtonSuggestKey.Content = "Suggest Key";
            }
        }

        private void OnKeyChecked(object senderIn, RoutedEventArgs eIn) {
            if (ListBoxSelectedKeys.Visibility == Visibility.Collapsed) {
                ShowSelectedKeys();
            }
        }

        private void OnKeyUnChecked(object senderIn, RoutedEventArgs eIn) {
            if (ColumnNamesViewModel.SelectedKeys.Count == 0) {
                HideSelectedKeys();
            }
        }

        private void ButtonGoBackClick(object senderIn, RoutedEventArgs eIn) {
            ImportViewModel.PreviewFileName = "";
            if (ImportViewModel.Version == "Test") {
                AsyncRenderFileWithAnalyseToView(ImportViewModel.PathMasterFile);
                ButtonGoForward.Visibility = Visibility.Visible;
                ButtonGoBack.ToolTip = "Back to Main Page";
                ImportViewModel.Version = "Master";
            } else {
                ResetUserKeys();
                ImportViewModel.PreviewFileName = "";
                GoBack?.Invoke(senderIn, eIn);
            }
        }

        private void ResetUserKeys() {
            ColumnNamesViewModel.AvailableKeys.Clear();
            ColumnNamesViewModel.SelectedKeys.Clear();
            HideAvailableKeys();
            HideSelectedKeys();
            ButtonSuggestKey.Content = "Suggest Key";
        }

        private void ButtonGoForwardClick(object senderIn, RoutedEventArgs eIn) {
            ImportViewModel.PreviewFileName = "";
            if (ImportViewModel.Version == "Master") {
                AsyncRenderFileWithAnalyseToView(ImportViewModel.PathTestFile);
                ButtonGoForward.Visibility = Visibility.Collapsed;
                ButtonGoBack.ToolTip = "Back to Master file";
                ImportViewModel.Version = "Test";
            }
        }

        private void OnExpanded(object senderIn, RoutedEventArgs eIn) {
            PopupSkipedRows.IsOpen = true;
        }

        public void ResetPopUp() {
            var offset = PopupSkipedRows.HorizontalOffset;
            PopupSkipedRows.HorizontalOffset = offset + 1;
            PopupSkipedRows.HorizontalOffset = offset;
        }

        private void PopupSkipedRowsClosed(object sender, EventArgs e) {
            ExpanderSkippedRows.IsExpanded = false;
        }


        private void TextBoxHeaderRowTextChanged(object sender, TextChangedEventArgs e) {
            if (TextBoxHeaderRow.Text != "No header found" && !string.IsNullOrEmpty(TextBoxHeaderRow.Text)) {
                var val = int.Parse(TextBoxHeaderRow.Text);
                if (val == 0 && !ImportViewModel.IsHeadersExist) {
                    ImportViewModel.RowsToSkip = val;
                    ImportViewModel.IsHeadersExist = true;
                    AsyncRenderFileWithSetPreviewToView(ImportViewModel.PathMasterFile);
                } else if (ImportViewModel.RowsToSkip != val) {
                    ImportViewModel.RowsToSkip = val;
                    ImportViewModel.IsHeadersExist = true;
                    AsyncRenderFileWithSetPreviewToView(ImportViewModel.PathMasterFile);
                }
            } else if (TextBoxHeaderRow.Text == "No header found" && ImportViewModel.IsHeadersExist) {
                ImportViewModel.RowsToSkip = 0;
                ImportViewModel.IsHeadersExist = false;
                AsyncRenderFileWithSetPreviewToView(ImportViewModel.PathMasterFile);
            }
        }

        private void TextBoxHeaderRowPreviewTextInput(object sender, TextCompositionEventArgs e) {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void DataGridLoadingRow(object sender, DataGridRowEventArgs e) {
            e.Row.Header = (e.Row.GetIndex() +1).ToString();
        }

        private void OnIsHeaderExistsChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "IsHeadersExist" && !ImportViewModel.IsFirstStart) {
                var path = "";
                if (ImportViewModel.Version == "Master") {
                    path = ImportViewModel.PathMasterFile;
                } else {
                    path = ImportViewModel.PathTestFile;
                }
                AsyncRenderFileWithSetPreviewToView(path);
            }
        }

        private void TextBoxHeaderRowLostFocus(object senderIn, RoutedEventArgs eIn) {
            if (TextBoxHeaderRow.Text=="") {
                TextBoxHeaderRow.Text = ImportViewModel.RowsToSkip.ToString();
            }
        }

    }
}

