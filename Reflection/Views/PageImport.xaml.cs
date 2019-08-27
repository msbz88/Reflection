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
        ColumnNamesViewModel CurrentColumnNamesVM { get; set; }

        public PageImport() {
            InitializeComponent();
            ComboBoxDataBinding();
        }

        public void SelectFiles() {
            MatchFileNamesViewModel = new MatchFileNamesViewModel();
            SuggestedKeyColumnNames = new ColumnNamesViewModel("SuggestedKey");
            UserIdColumnNames = new ColumnNamesViewModel("UserId");
            ExcludeColumnNames = new ColumnNamesViewModel("ExcludeColumn");
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
            } else {
                MasterViewModel.PropertyChanged -= OnIsHeaderExistsChanged;
                TestViewModel.PropertyChanged -= OnIsHeaderExistsChanged;
            }
        }

        public async void ReturnToView(ComparisonTask comparisonTask) {
            SuggestedKeyColumnNames = new ColumnNamesViewModel("SuggestedKey");
            UserIdColumnNames = new ColumnNamesViewModel("UserId");
            ExcludeColumnNames = new ColumnNamesViewModel("ExcludeColumn");
            MasterViewModel = new ImportViewModel();
            TestViewModel = new ImportViewModel();
            MasterViewModel.PropertyChanged += OnIsHeaderExistsChanged;
            TestViewModel.PropertyChanged += OnIsHeaderExistsChanged;
            Version = "Master";
            this.DataContext = MasterViewModel;
            MasterViewModel.FilePath = comparisonTask.MasterConfiguration.FilePath;
            TestViewModel.FilePath = comparisonTask.TestConfiguration.FilePath;
            SingleFileView?.Invoke(null, null);
            await AsyncRenderFileWithAnalyseToView(MasterViewModel);
            CheckSelectedKey(UserIdColumnNames, comparisonTask.ComparisonKeys.UserIdColumns.Concat(comparisonTask.ComparisonKeys.UserIdColumnsBinary).Concat(comparisonTask.ComparisonKeys.BinaryValues).ToList());
            CheckSelectedKey(ExcludeColumnNames, comparisonTask.ComparisonKeys.ExcludeColumns.Concat(comparisonTask.MasterConfiguration.UserExcludeColumns).Concat(comparisonTask.TestConfiguration.UserExcludeColumns).ToList());
            ShowAvailableKeys();
            ShowSelectedKeys();
            CheckSelectedKey(SuggestedKeyColumnNames, comparisonTask.ComparisonKeys.MainKeys);
            TextBlockCurrentUserSelection.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#0e0fed"));
            TextBlockCurrentUserSelection.Text = "Comparison Key";
            HandleSelectedKeys();
        }

        private void CheckSelectedKey(ColumnNamesViewModel columnNamesViewModel, List<int> selectedColumns) {
            CurrentColumnNamesVM = columnNamesViewModel;
            LoadColumnNames();
            foreach (var item in selectedColumns) {
                var columnName = columnNamesViewModel.AvailableKeys.Where(key => key.Id == item).FirstOrDefault();
                if (columnName != null) {
                    columnName.IsChecked = true;
                    HandleChecked(columnName);
                }
            }
        }

        private void HandleSelectedKeys() {
            if (UserIdColumnNames.SelectedKeys.Count > 0) {
                ChangeButtonColor(ButtonAddIdColumns, new SolidColorBrush(Colors.DarkSlateBlue));
            }
            if (ExcludeColumnNames.SelectedKeys.Count > 0) {
                ChangeButtonColor(ButtonExcludeColumns, new SolidColorBrush(Colors.DarkSlateBlue));
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
                    Dispatcher.Invoke(() => FilesLoaded?.Invoke(configs, null));
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

        private async Task AsyncRenderFileWithAnalyseToView(ImportViewModel importViewModel) {
            dgData.Columns.Clear();
            dgData.ItemsSource = null;
            await Task.Run(() => {
                importViewModel.AnalyseFile();
                Dispatcher.Invoke(() => {
                    InitializeDataGrid(importViewModel);
                    PrintFileInfo(importViewModel);
                    if (ListBoxAvailableKeys.Visibility == Visibility.Visible) {
                        LoadColumnNames();
                    }
                });
            });
        }

        private async void AsyncRenderFileWithSetPreviewToView(ImportViewModel importViewModel) {
            dgData.ItemsSource = null;
            await Task.Run(() => {
                importViewModel.ManualUpdate();
                Dispatcher.Invoke(() => {
                    UpdateDataGrid(importViewModel);
                    PrintSkippedLines(importViewModel.SkippedLines);
                    ShowHeaderRow(importViewModel.RowsToSkip);
                    ShowRowsAndColumnsCount(importViewModel);
                    if (ListBoxAvailableKeys.Visibility == Visibility.Visible) {
                        LoadColumnNames();
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
                if (i + 1 > importViewModel.FileHeaders.Count) {
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

        private void LoadColumnNames() {
            var currViewModel = Version == "Master" ? MasterViewModel : TestViewModel;
            CurrentColumnNamesVM.AvailableKeys.Clear();
            int index = 0;
            foreach (var item in currViewModel.FileHeaders) {
                if (!CurrentColumnNamesVM.UnAvailableKeys.Contains(index)) {
                    var key = new ColumnName(index, item.Replace("_", "__"));
                    CurrentColumnNamesVM.AvailableKeys.Add(key);
                    if (CurrentColumnNamesVM.SelectedKeys.Any(k => k.Id == index)) {
                        key.IsChecked = true;
                    }
                }
                index++;
            }
            ListBoxAvailableKeys.ItemsSource = CurrentColumnNamesVM.FilteredAvailableKeys;
            ListBoxSelectedKeys.ItemsSource = CurrentColumnNamesVM.SelectedKeys;
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
            TextBoxSearchColumnNames.Visibility = Visibility.Visible;
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
            TextBoxSearchColumnNames.Visibility = Visibility.Collapsed;
        }

        private void HideSelectedKeys() {
            PanelSelectedKeys.Visibility = Visibility.Collapsed;
            SpliterLists.Visibility = Visibility.Collapsed;
            SeparatorLists.Visibility = Visibility.Collapsed;
            LabelSelectedKeys.Visibility = Visibility.Collapsed;
            ListBoxSelectedKeys.Visibility = Visibility.Collapsed;
            Grid.SetRowSpan(ListBoxAvailableKeys, 2);
        }

        private void ChangeButtonColor(Button button, SolidColorBrush color) {
            Style editingStyle = new Style(typeof(Button));
            editingStyle.BasedOn = (Style)FindResource(typeof(Button));
            editingStyle.Setters.Add(new Setter(BackgroundProperty, color));
            button.Style = editingStyle;
        }

        private void ButtonApplyUserKeyClick(object senderIn, RoutedEventArgs eIn) {
            TextBlockCurrentUserSelection.Text = "";
            HideSelectedKeys();
            HideAvailableKeys();
            var currViewModel = Version == "Master" ? MasterViewModel : TestViewModel;
            if (CurrentColumnNamesVM.SelectedKeys.Count > 0) {
                if (CurrentColumnNamesVM.Name == "SuggestedKey") {
                    currViewModel.UserKeys = CurrentColumnNamesVM.SelectedKeys.Select(item => item.Id).ToList();
                    ChangeButtonColor(ButtonSuggestKey, new SolidColorBrush(Colors.DarkSlateBlue));
                } else if (CurrentColumnNamesVM.Name == "UserId") {
                    currViewModel.UserIdColumns = CurrentColumnNamesVM.SelectedKeys.Select(item => item.Id).ToList();
                    ChangeButtonColor(ButtonAddIdColumns, new SolidColorBrush(Colors.DarkSlateBlue));
                } else if (CurrentColumnNamesVM.Name == "ExcludeColumn") {
                    currViewModel.UserExcludeColumns = CurrentColumnNamesVM.SelectedKeys.Select(item => item.Id).ToList();
                    ChangeButtonColor(ButtonExcludeColumns, new SolidColorBrush(Colors.DarkSlateBlue));
                }
            } else {
                if (CurrentColumnNamesVM.Name == "SuggestedKey") {
                    ChangeButtonColor(ButtonSuggestKey, (SolidColorBrush)(new BrushConverter().ConvertFrom("#373737")));
                } else if (CurrentColumnNamesVM.Name == "UserId") {
                    ChangeButtonColor(ButtonAddIdColumns, (SolidColorBrush)(new BrushConverter().ConvertFrom("#373737")));
                } else if (CurrentColumnNamesVM.Name == "ExcludeColumn") {
                    ChangeButtonColor(ButtonExcludeColumns, (SolidColorBrush)(new BrushConverter().ConvertFrom("#373737")));
                }
            }
        }

        private void HandleChecked(ColumnName columnName) {
            if (CurrentColumnNamesVM.Name == "SuggestedKey") {
                ExcludeColumnNames.UnAvailableKeys.Add(columnName.Id);
                UserIdColumnNames.UnAvailableKeys.Add(columnName.Id);
            } else if (CurrentColumnNamesVM.Name == "UserId") {
                SuggestedKeyColumnNames.UnAvailableKeys.Add(columnName.Id);
                ExcludeColumnNames.UnAvailableKeys.Add(columnName.Id);
            } else if (CurrentColumnNamesVM.Name == "ExcludeColumn") {
                SuggestedKeyColumnNames.UnAvailableKeys.Add(columnName.Id);
                UserIdColumnNames.UnAvailableKeys.Add(columnName.Id);
            }
        }

        private void OnKeyChecked(object senderIn, RoutedEventArgs eIn) {
            if (ListBoxSelectedKeys.Visibility == Visibility.Collapsed) {
                ShowSelectedKeys();
            }
            var checkBox = (CheckBox)senderIn;
            var columnName = (ColumnName)checkBox.DataContext;
            HandleChecked(columnName);
        }

        private void OnKeyUnChecked(object senderIn, RoutedEventArgs eIn) {
            if (CurrentColumnNamesVM.SelectedKeys.Count == 0) {
                HideSelectedKeys();
            }
            var checkBox = (CheckBox)senderIn;
            var columnName = (ColumnName)checkBox.DataContext;
            if (CurrentColumnNamesVM.Name == "SuggestedKey") {
                ExcludeColumnNames.UnAvailableKeys.Remove(columnName.Id);
                UserIdColumnNames.UnAvailableKeys.Remove(columnName.Id);
            } else if (CurrentColumnNamesVM.Name == "UserId") {
                SuggestedKeyColumnNames.UnAvailableKeys.Remove(columnName.Id);
                ExcludeColumnNames.UnAvailableKeys.Remove(columnName.Id);
            } else if (CurrentColumnNamesVM.Name == "ExcludeColumn") {
                SuggestedKeyColumnNames.UnAvailableKeys.Remove(columnName.Id);
                UserIdColumnNames.UnAvailableKeys.Remove(columnName.Id);
            }
        }

        private void SwitchModelView(string name) {
            if (name == "Master") {
                this.DataContext = MasterViewModel;
            } else if (name == "Test") {
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
            ClearSelectedKeys();
            HideAvailableKeys();
            HideSelectedKeys();
            TextBlockCurrentUserSelection.Text = "";
            TextBoxSearchColumnNames.Text = "";
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

        private void ClearSelectedKeys() {
            SuggestedKeyColumnNames.AvailableKeys.Clear();
            UserIdColumnNames.AvailableKeys.Clear();
            ExcludeColumnNames.AvailableKeys.Clear();
            SuggestedKeyColumnNames.SelectedKeys.Clear();
            UserIdColumnNames.SelectedKeys.Clear();
            ExcludeColumnNames.SelectedKeys.Clear();
            HideSelectedKeys();
            ChangeButtonColor(ButtonSuggestKey, (SolidColorBrush)(new BrushConverter().ConvertFrom("#373737")));
            ChangeButtonColor(ButtonAddIdColumns, (SolidColorBrush)(new BrushConverter().ConvertFrom("#373737")));
            ChangeButtonColor(ButtonExcludeColumns, (SolidColorBrush)(new BrushConverter().ConvertFrom("#373737")));
            if (ListBoxAvailableKeys.Visibility == Visibility.Visible && CurrentColumnNamesVM.Name== "SuggestedKey") {
                TextBlockCurrentUserSelection.Text = "Suggest Key";
            }          
        }

        private void TextBoxHeaderRowTextChanged(object sender, TextChangedEventArgs e) {
            if (!string.IsNullOrEmpty(TextBoxHeaderRow.Text)) {               
                var val = int.Parse(TextBoxHeaderRow.Text);
                var currViewModel = Version == "Master" ? MasterViewModel : TestViewModel;
                if (currViewModel.IsUserInput) {
                    ClearSelectedKeys();
                }
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
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void OnIsHeaderExistsChanged(object sender, PropertyChangedEventArgs e) {
            var importViewmodel = (ImportViewModel)sender;
            if (e.PropertyName == "IsHeadersExist") {
                if (importViewmodel.IsUserInput && !importViewmodel.IsFirstStart) {
                    AsyncRenderFileWithSetPreviewToView(importViewmodel);
                }
            }
        }

        private void TextBoxHeaderRowLostFocus(object senderIn, RoutedEventArgs eIn) {
            if (TextBoxHeaderRow.Text == "") {
                TextBoxHeaderRow.Text = MasterViewModel.RowsToSkip.ToString();
            }
        }

        private void ButtonSuggestKeyClick(object senderIn, RoutedEventArgs eIn) {
            TextBoxSearchColumnNames.Text = "";
            TextBlockCurrentUserSelection.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#0e0fed"));
            TextBlockCurrentUserSelection.Text = "Suggest Key";
            CurrentColumnNamesVM = SuggestedKeyColumnNames;
            if (SuggestedKeyColumnNames.SelectedKeys.Count > 0) {
                ShowAvailableKeys();
                ShowSelectedKeys();
            } else {
                ShowAvailableKeys();
            }
            LoadColumnNames();
        }

        private void ButtonAddIdColumnsClick(object senderIn, RoutedEventArgs eIn) {
            TextBoxSearchColumnNames.Text = "";
            TextBlockCurrentUserSelection.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFFFA500"));
            TextBlockCurrentUserSelection.Text = "Add Id Columns";
            CurrentColumnNamesVM = UserIdColumnNames;
            if (UserIdColumnNames.SelectedKeys.Count > 0) {
                ShowAvailableKeys();
                ShowSelectedKeys();
            } else {
                ShowAvailableKeys();
            }
            LoadColumnNames();
        }

        private void ButtonExcludeColumnsClick(object senderIn, RoutedEventArgs eIn) {
            TextBoxSearchColumnNames.Text = "";
            TextBlockCurrentUserSelection.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FC4A1A"));
            TextBlockCurrentUserSelection.Text = "Exclude Columns";
            CurrentColumnNamesVM = ExcludeColumnNames;
            if (ExcludeColumnNames.SelectedKeys.Count > 0) {
                ShowAvailableKeys();
                ShowSelectedKeys();
            } else {
                ShowAvailableKeys();
            }
            LoadColumnNames();
        }

        private void TextBoxSearchColumnNamesTextChanged(object sender, TextChangedEventArgs e) {
            var text = TextBoxSearchColumnNames.Text.ToLower();
            CurrentColumnNamesVM.FilteredAvailableKeys.Filter = i => ((ColumnName)i).Value.ToLower().Contains(text);
        }
    }
}

