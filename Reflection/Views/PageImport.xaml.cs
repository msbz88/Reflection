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
        public ImportViewModel MasterViewModel { get; set; }
        public ImportViewModel TestViewModel { get; set; }
        public MatchFileNamesViewModel MatchFileNamesViewModel { get; set; }
        ColumnNamesViewModel SuggestedKeyColumnNames { get; set; }
        ColumnNamesViewModel UserIdColumnNames { get; set; }
        ColumnNamesViewModel ExcludeColumnNames { get; set; }
        public EventHandler FilesLoaded { get; set; }
        public EventHandler GoBack { get; set; }
        public EventHandler SingleFileView { get; set; }
        public EventHandler Message { get; set; }
        private string customDelimiter;
        private string Version { get; set; }

        public PageImport() {
            InitializeComponent();
            ComboBoxDataBinding();
        }

        public void SelectFiles() {
            MatchFileNamesViewModel = new MatchFileNamesViewModel();
            SuggestedKeyColumnNames = new ColumnNamesViewModel();
            UserIdColumnNames = new ColumnNamesViewModel();
            ExcludeColumnNames = new ColumnNamesViewModel();
            MasterViewModel = new ImportViewModel();
            TestViewModel = new ImportViewModel();
            MasterViewModel.PropertyChanged += OnIsHeaderExistsChanged;
            TestViewModel.PropertyChanged += OnIsHeaderExistsChanged;
            MatchFileNamesViewModel.SelectFiles();
            if (MatchFileNamesViewModel.IsReady) {
                if (MatchFileNamesViewModel.MatchedFileNames.Count == 1) {
                    MasterViewModel.FilePath = MatchFileNamesViewModel.MatchedFileNames[0].MasterFilePath;
                    TestViewModel.FilePath = MatchFileNamesViewModel.MatchedFileNames[0].TestFilePath;
                    SingleFileView?.Invoke(null, null);
                    AsyncRenderFileWithAnalyseToView(MasterViewModel);
                    Version = "Master";
                    this.DataContext = MasterViewModel;
                } else {
                    AsyncRunMultyFiles();
                }
            }else {
                MasterViewModel.PropertyChanged -= OnIsHeaderExistsChanged;
                TestViewModel.PropertyChanged -= OnIsHeaderExistsChanged;
            }
        }

        private async void AsyncRunMultyFiles() {
            await Task.Run(() => {
                    var configs = new List<ImportConfiguration>();
                    MasterViewModel.IsMultiple = true;
                    TestViewModel.IsMultiple = true;
                    foreach (var item in MatchFileNamesViewModel.MatchedFileNames) {
                        MasterViewModel.FilePath = item.MasterFilePath;
                        TestViewModel.FilePath = item.TestFilePath;
                        MasterViewModel.AnalyseFile();
                        TestViewModel.AnalyseFile();
                        configs.Clear();
                        configs.Add(MasterViewModel.SetImportConfiguration());
                        configs.Add(TestViewModel.SetImportConfiguration());
                    Dispatcher.Invoke(() => FilesLoaded?.Invoke(configs, null) );
                    }
            });
        }

        private void DisplayOnLoadEncoding(Encoding encoding) {
            var t = comboBoxEncoding.Items.OfType<ComboBoxItem>().Select(item => item.Content.ToString());
            var comboBoxItem = comboBoxEncoding.Items.SourceCollection.Cast<EncodingInfo>().FirstOrDefault(x => x.CodePage == encoding.CodePage);
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
            if (PresentDelimiter(MasterViewModel.Delimiter) == TextBoxDelimiter.Text) {
                var delimiter = MasterViewModel.Delimiter;
                if (delimiter == "\t") {
                    delimiter = "\\t";
                }
                TextBoxDelimiter.Text = delimiter;
            } else {
                TextBoxDelimiter.Text = customDelimiter;
            }
        }

        private void TextBoxDelimiterLostFocus(object senderIn, RoutedEventArgs eIn) {
            if (TextBoxDelimiter.Text != MasterViewModel.Delimiter && TextBoxDelimiter.Text != "\\t") {
                customDelimiter = TextBoxDelimiter.Text;
                TextBoxDelimiter.Text = PresentDelimiter(TextBoxDelimiter.Text);
            } else {
                TextBoxDelimiter.Text = PresentDelimiter(MasterViewModel.Delimiter);
            }
        }

        private async void AsyncRenderFileWithAnalyseToView(ImportViewModel importViewModel) {
            dgData.Columns.Clear();
            dgData.ItemsSource = null;
            await Task.Run(() => {
                importViewModel.AnalyseFile();
                //InitializeDataGrid();
                Dispatcher.Invoke(() => {
                    InitializeDataGrid(importViewModel);
                    PrintFileInfo(importViewModel);
                    if (ListBoxAvailableKeys.Visibility == Visibility.Visible) {
                        LoadSuggestedKeyColumnNames();
                        LoadIdColumnNames();
                        LoadExcludeColumnNames();
                    }
                });
            });
        }

        private async void AsyncRenderFileWithSetPreviewToView(ImportViewModel importViewModel) {
            dgData.ItemsSource = null;
            await Task.Run(() => {
                importViewModel.ManualUpdate();
                //InitializeDataGrid();
                Dispatcher.Invoke(() => {
                    UpdateDataGrid(importViewModel);
                    PrintSkippedLines(importViewModel.SkippedLines);
                    ShowHeaderRow(importViewModel.RowsToSkip);
                    ShowRowsAndColumnsCount(importViewModel);                   
                    if (ListBoxAvailableKeys.Visibility == Visibility.Visible) {
                        LoadSuggestedKeyColumnNames();
                        LoadIdColumnNames();
                        LoadExcludeColumnNames();
                    }
                });
            });
        }

        private void ComboBoxSelectionChanged(object senderIn, RoutedEventArgs eIn) {
            var encodingInfo = (EncodingInfo)comboBoxEncoding.SelectedItem;
            MasterViewModel.Encoding = encodingInfo.GetEncoding();
            if (TextBlockFileName.Text == (System.IO.Path.GetFileName(MasterViewModel.FilePath))) {
                //RenderFileToView(ImportViewModel.PathMasterFile);
            } else {
                //RenderFileToView(ImportViewModel.PathTestFile);
            }
        }

        private void ComboBoxDataBinding() {
            EncodingInfo[] codePages = Encoding.GetEncodings();
            comboBoxEncoding.ItemsSource = codePages;
        }

        public void InitializeDataGrid(ImportViewModel importViewModel) {
            int index = 0;
            foreach (var item in importViewModel.FileHeaders) {
                var column = new DataGridTextColumn();
                column.Header = item.Replace("_", "__");
                column.Binding = new Binding(string.Format("[{0}]", index));
                dgData.Columns.Add(column);
                index++;
            }
            if (dgData.ItemsSource == null) {
                dgData.ItemsSource = importViewModel.PreviewContent;
            }
        }

        public void UpdateDataGrid(ImportViewModel importViewModel) {
            for (int i = 0; i < dgData.Columns.Count; i++) {
                if(i + 1 > importViewModel.FileHeaders.Count) {
                    dgData.Columns[i].Header = "";
                } else {
                    dgData.Columns[i].Header = importViewModel.FileHeaders[i].Replace("_", "__");
                }               
            }
            if (dgData.ItemsSource == null) {
                dgData.ItemsSource = importViewModel.PreviewContent;
            }
        }

        public void PrintFileInfo(ImportViewModel importViewModel) {
            PrintSkippedLines(importViewModel.SkippedLines);
            ShowHeaderRow(importViewModel.RowsToSkip);
            TextBoxDelimiter.Text = PresentDelimiter(importViewModel.Delimiter);
            DisplayOnLoadEncoding(importViewModel.Encoding);
            ShowRowsAndColumnsCount(importViewModel);
        }

        private void ShowRowsAndColumnsCount(ImportViewModel importViewModel) {
            var prev = importViewModel.PreviewCount < 200 ? "" : " preview";
            Message.Invoke("Rows" + prev + ": " + importViewModel.PreviewContent.Count + " Columns: " + importViewModel.FileHeaders.Count, null);
        }

        private void ShowHeaderRow(int rowsToSkip) {
           TextBoxHeaderRow.Text = rowsToSkip.ToString();
        }

        private void PrintSkippedLines(List<string> skippedLines) {
            if (skippedLines.Count > 0) {
                TextBlockSkippedRows.Text = string.Join(Environment.NewLine, skippedLines);
                ExpanderSkippedRows.Visibility = Visibility.Visible;
                ExpanderSkippedRows.IsExpanded = true;
            } else {
                ExpanderSkippedRows.Visibility = Visibility.Collapsed;
                PopupSkipedRows.IsOpen = false;
            }
        }

        private void ButtonExecuteClick(object senderIn, RoutedEventArgs eIn) {
            MasterViewModel.IsUserInput = false;
            var configs = new List<ImportConfiguration>();
            if (TestViewModel.IsFirstStart) {
                TestViewModel.AnalyseFile();
            }
            configs.Add(MasterViewModel.SetImportConfiguration());
            configs.Add(TestViewModel.SetImportConfiguration());           
            FilesLoaded?.Invoke(configs, eIn);
            ResetUserKeys();
            MasterViewModel.PropertyChanged -= OnIsHeaderExistsChanged;
            TestViewModel.PropertyChanged -= OnIsHeaderExistsChanged;
        }

        private void ButtonSuggestKeyClick(object senderIn, RoutedEventArgs eIn) {
            if (SuggestedKeyColumnNames.SelectedKeys.Count > 0) {
                ShowAvailableKeys();
                ShowSelectedKeys();
            } else {
                ShowAvailableKeys();
            }
            LoadSuggestedKeyColumnNames();
        }

        private void LoadSuggestedKeyColumnNames() {
            var currViewModel = Version == "Master" ? MasterViewModel : TestViewModel;
            SuggestedKeyColumnNames.AvailableKeys.Clear();
            int index = 0;
            foreach (var item in currViewModel.FileHeaders) {
                var key = new ColumnName(index, item.Replace("_", "__"));
                SuggestedKeyColumnNames.AvailableKeys.Add(key);
                if (SuggestedKeyColumnNames.SelectedKeys.Any(k => k.Id == index)) {
                    key.IsChecked = true;
                }
                index++;
            }
            ListBoxAvailableKeys.ItemsSource = SuggestedKeyColumnNames.AvailableKeys;
            ListBoxSelectedKeys.ItemsSource = SuggestedKeyColumnNames.SelectedKeys;
        }

        private void LoadIdColumnNames() {
            var currViewModel = Version == "Master" ? MasterViewModel : TestViewModel;
            UserIdColumnNames.AvailableKeys.Clear();
            int index = 0;
            foreach (var item in currViewModel.FileHeaders) {
                var key = new ColumnName(index, item.Replace("_", "__"));
                UserIdColumnNames.AvailableKeys.Add(key);
                if (UserIdColumnNames.SelectedKeys.Any(k => k.Id == index)) {
                    key.IsChecked = true;
                }
                index++;
            }
            ListBoxAvailableKeys.ItemsSource = UserIdColumnNames.AvailableKeys;
            ListBoxSelectedKeys.ItemsSource = UserIdColumnNames.SelectedKeys;
        }

        private void LoadExcludeColumnNames() {
            var currViewModel = Version == "Master" ? MasterViewModel : TestViewModel;
            ExcludeColumnNames.AvailableKeys.Clear();
            int index = 0;
            foreach (var item in currViewModel.FileHeaders) {
                var key = new ColumnName(index, item.Replace("_", "__"));
                ExcludeColumnNames.AvailableKeys.Add(key);
                if (ExcludeColumnNames.SelectedKeys.Any(k => k.Id == index)) {
                    key.IsChecked = true;
                }
                index++;
            }
            ListBoxAvailableKeys.ItemsSource = ExcludeColumnNames.AvailableKeys;
            ListBoxSelectedKeys.ItemsSource = ExcludeColumnNames.SelectedKeys;
        }

        private void ShowAvailableKeys() {
            ButtonSuggestKey.Visibility = Visibility.Collapsed;
            ButtonAddIdColumns.Visibility = Visibility.Collapsed;
            ButtonExcludeColumns.Visibility = Visibility.Collapsed;
            ButtonExecute.Visibility = Visibility.Collapsed;
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
            ButtonSuggestKey.Visibility = Visibility.Visible;
            ButtonAddIdColumns.Visibility = Visibility.Visible;
            ButtonExcludeColumns.Visibility = Visibility.Visible;
            ButtonExecute.Visibility = Visibility.Visible;
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
            var currViewModel = Version == "Master" ? MasterViewModel : TestViewModel;
            if (SuggestedKeyColumnNames.SelectedKeys.Count > 0) {
                //ButtonSuggestKey.Content = "Show Key";
                currViewModel.UserKeys = SuggestedKeyColumnNames.SelectedKeys.Select(item => item.Id).ToList();
                ButtonSuggestKey.Background = new SolidColorBrush(Colors.DarkSlateBlue);
            } else {
                //ButtonSuggestKey.Content = "Suggest Key";
                ButtonSuggestKey.Background = new SolidColorBrush(Colors.Black);
            }
            if (UserIdColumnNames.SelectedKeys.Count > 0) {
                currViewModel.UserIdColumns = UserIdColumnNames.SelectedKeys.Select(item => item.Id).ToList();
                ButtonAddIdColumns.Background = new SolidColorBrush(Colors.DarkSlateBlue);
            } else {
                ButtonAddIdColumns.Background = new SolidColorBrush(Colors.Black);
            }
            if (ExcludeColumnNames.SelectedKeys.Count > 0) {
                currViewModel.UserExcludeColumns = ExcludeColumnNames.SelectedKeys.Select(item => item.Id).ToList();
                ButtonExcludeColumns.Background = new SolidColorBrush(Colors.DarkSlateBlue);
            } else {
                ButtonExcludeColumns.Background = new SolidColorBrush(Colors.Black);
            }
        }

        private void OnKeyChecked(object senderIn, RoutedEventArgs eIn) {
            if (ListBoxSelectedKeys.Visibility == Visibility.Collapsed) {
                ShowSelectedKeys();
            }
        }

        private void OnKeyUnChecked(object senderIn, RoutedEventArgs eIn) {
            //error
            if (SuggestedKeyColumnNames.SelectedKeys.Count == 0) {
                HideSelectedKeys();
            }
            if (UserIdColumnNames.SelectedKeys.Count == 0) {
                HideSelectedKeys();
            }
            if (ExcludeColumnNames.SelectedKeys.Count == 0) {
                HideSelectedKeys();
            }
        }

        private void SwitchModelView(string name) {
            if (name == "Master") {
                this.DataContext = MasterViewModel;                
            }else if (name == "Test") {
                this.DataContext = TestViewModel;
            }
            Version = name;
        }

        private void ButtonGoBackClick(object senderIn, RoutedEventArgs eIn) {
            MasterViewModel.PreviewFileName = "";
            if (Version == "Test") {
                AsyncRenderFileWithSetPreviewToView(MasterViewModel);
                ButtonGoForward.Visibility = Visibility.Visible;
                ButtonGoBack.ToolTip = "Back to Main Page";
                SwitchModelView("Master");
            } else {
                ResetUserKeys();
                GoBack?.Invoke(senderIn, eIn);
            }
        }

        private void ResetUserKeys() {
            SuggestedKeyColumnNames.AvailableKeys.Clear();
            SuggestedKeyColumnNames.SelectedKeys.Clear();
            UserIdColumnNames.AvailableKeys.Clear();
            UserIdColumnNames.SelectedKeys.Clear();
            ExcludeColumnNames.AvailableKeys.Clear();
            ExcludeColumnNames.SelectedKeys.Clear();
            ButtonSuggestKey.Background = new SolidColorBrush(Colors.Black);
            ButtonAddIdColumns.Background = new SolidColorBrush(Colors.Black);
            ButtonExcludeColumns.Background = new SolidColorBrush(Colors.Black);
            HideAvailableKeys();
            HideSelectedKeys();
            //ButtonSuggestKey.Content = "Suggest Key";
        }

        private void ButtonGoForwardClick(object senderIn, RoutedEventArgs eIn) {
            MasterViewModel.PreviewFileName = "";
            SwitchModelView("Test");
            if (TestViewModel.IsFirstStart) {
                    AsyncRenderFileWithAnalyseToView(TestViewModel);
                } else {
                    AsyncRenderFileWithSetPreviewToView(TestViewModel);
                }             
                ButtonGoForward.Visibility = Visibility.Collapsed;
                ButtonGoBack.ToolTip = "Back to Master file";               
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
            if (!string.IsNullOrEmpty(TextBoxHeaderRow.Text)) {
                var val = int.Parse(TextBoxHeaderRow.Text);
                var currViewModel = Version == "Master" ? MasterViewModel : TestViewModel;
                if (currViewModel.RowsToSkip != val) {
                    currViewModel.RowsToSkip = val;
                    AsyncRenderFileWithSetPreviewToView(currViewModel);
                } 
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
            if (e.PropertyName == "IsHeadersExist") {
                if (Version == "Master" && MasterViewModel.IsUserInput && !MasterViewModel.IsFirstStart) {
                    AsyncRenderFileWithSetPreviewToView(MasterViewModel);
                } else if(Version == "Test" && TestViewModel.IsUserInput && !MasterViewModel.IsFirstStart) {
                    AsyncRenderFileWithSetPreviewToView(TestViewModel);
                }
            }
        }

        private void TextBoxHeaderRowLostFocus(object senderIn, RoutedEventArgs eIn) {
            if (TextBoxHeaderRow.Text=="") {
                TextBoxHeaderRow.Text = MasterViewModel.RowsToSkip.ToString();
            }
        }

        private void ButtonAddIdColumnsClick(object senderIn, RoutedEventArgs eIn) {
            if (UserIdColumnNames.SelectedKeys.Count > 0) {
                ShowAvailableKeys();
                ShowSelectedKeys();
            } else {
                ShowAvailableKeys();
            }
            LoadIdColumnNames();
        }

        private void ButtonExcludeColumnsClick(object senderIn, RoutedEventArgs eIn) {
            if (ExcludeColumnNames.SelectedKeys.Count > 0) {
                ShowAvailableKeys();
                ShowSelectedKeys();
            } else {
                ShowAvailableKeys();
            }
            LoadExcludeColumnNames();
        }

    }
}

