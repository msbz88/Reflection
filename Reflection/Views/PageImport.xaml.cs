using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private char[] customDelimiter;
        private string Version { get; set; }
        public UserKeys UserKeys { get; set; }
        ColumnNamesViewModel CurrentColumnNamesVM { get; set; }
        List<string> DiffColumns;

        public PageImport() {
            InitializeComponent();
            ComboBoxDataBinding();
            UserKeys = new UserKeys();
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
                    var matchFileNamesViewModel = new MatchFileNamesViewModel();
                    matchFileNamesViewModel.MatchedFileNames = MatchFileNamesViewModel.MatchedFileNames;
                    AsyncRunMultyFiles(matchFileNamesViewModel, MasterViewModel, TestViewModel);
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
            MasterViewModel.FilePath = comparisonTask.MasterConfiguration.FilePath;
            TestViewModel.FilePath = comparisonTask.TestConfiguration.FilePath;
            MasterViewModel.ReturnedImportConfiguration = comparisonTask.MasterConfiguration;
            TestViewModel.ReturnedImportConfiguration = comparisonTask.TestConfiguration;
            SingleFileView?.Invoke(null, null);
            await AsyncRenderFileWithAnalyseToView(MasterViewModel);
            Version = "Master";
            this.DataContext = MasterViewModel;
            CheckSelectedKey(UserIdColumnNames, new HashSet<int>(comparisonTask.ComparisonKeys.SingleIdColumns.Concat(comparisonTask.ComparisonKeys.BinaryIdColumns)));
            CheckSelectedKey(ExcludeColumnNames, comparisonTask.ComparisonKeys.ExcludeColumns);
            ShowAvailableKeys();
            ShowSelectedKeys();
            CheckSelectedKey(SuggestedKeyColumnNames, comparisonTask.ComparisonKeys.MainKeys);
            TextBlockCurrentUserSelection.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#0e0fed"));
            TextBlockCurrentUserSelection.Text = "Comparison Key";
            HandleSelectedKeys();
        }

        private void CheckSelectedKey(ColumnNamesViewModel columnNamesViewModel, HashSet<int> selectedColumns) {
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

        private async void AsyncRunMultyFiles(MatchFileNamesViewModel matchFileNamesViewModel, ImportViewModel masterViewModel, ImportViewModel testViewModel) {
            var configs = new List<ImportConfiguration>();
            masterViewModel.IsMultiple = true;
            testViewModel.IsMultiple = true;
            foreach (var item in matchFileNamesViewModel.MatchedFileNames) {
                masterViewModel.FilePath = item.MasterFilePath;
                testViewModel.FilePath = item.TestFilePath;
                await Task.Run(() => {
                    masterViewModel.AnalyseFile();
                    testViewModel.AnalyseFile();
                });
                configs.Clear();
                configs.Add(masterViewModel.SetImportConfiguration());
                configs.Add(testViewModel.SetImportConfiguration());
                FilesLoaded?.Invoke(configs, null);
            }
        }

        private void DisplayOnLoadEncoding(Encoding encoding) {
            var t = comboBoxEncoding.Items.OfType<ComboBoxItem>().Select(item => item.Content.ToString());
            var comboBoxItem = comboBoxEncoding.Items.SourceCollection.Cast<EncodingInfo>().FirstOrDefault(x => x.CodePage == encoding.CodePage);
            comboBoxEncoding.SelectedIndex = comboBoxEncoding.SelectedIndex = comboBoxEncoding.Items.IndexOf(comboBoxItem);
        }

        private string PresentDelimiter(char[] delimiter) {
            string viewDelimiter = "";
            switch (string.Join("", delimiter)) {
                case "\t":
                viewDelimiter = "<\\t> (Tab)";
                break;
                case "\\t":
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
                viewDelimiter = "<" + string.Join("", delimiter) + "> (Custom)";
                break;
            }
            return viewDelimiter;
        }

        private void TextBoxDelimiterGotFocus(object senderIn, RoutedEventArgs eIn) {
            if (PresentDelimiter(MasterViewModel.Delimiter) == TextBoxDelimiter.Text) {
                var delimiter = MasterViewModel.Delimiter;
                if (string.Join("", delimiter) == "\t") {
                    delimiter = new char[] { '\\', 't' };
                }
                TextBoxDelimiter.Text = string.Join("", delimiter);
            } else {
                TextBoxDelimiter.Text = string.Join("", customDelimiter);
            }
        }

        private void TextBoxDelimiterLostFocus(object senderIn, RoutedEventArgs eIn) {
            if (TextBoxDelimiter.Text.ToCharArray() != MasterViewModel.Delimiter && TextBoxDelimiter.Text != "") {
                customDelimiter = TextBoxDelimiter.Text.ToCharArray();
                TextBoxDelimiter.Text = PresentDelimiter(TextBoxDelimiter.Text.ToCharArray());
                var currViewModel = Version == "Master" ? MasterViewModel : TestViewModel;
                currViewModel.Delimiter = string.Join("", customDelimiter) == "\\t" ? new char[] { '\t' } : customDelimiter;
                if (currViewModel.IsUserInput && !currViewModel.IsFirstStart) {
                    AsyncRenderFileWithSetPreviewToView(currViewModel, false);
                }
            } else {
                TextBoxDelimiter.Text = PresentDelimiter(MasterViewModel.Delimiter);
            }
        }

        private async Task AsyncRenderFileWithAnalyseToView(ImportViewModel importViewModel) {
            dgData.Columns.Clear();
            dgData.ItemsSource = null;
            await Task.Run(() => {
                if (importViewModel.ReturnedImportConfiguration == null) {
                    importViewModel.AnalyseFile();
                } else {
                    importViewModel.AnalyseFile(importViewModel.ReturnedImportConfiguration);
                }
                Dispatcher.Invoke(() => {
                    InitializeDataGrid(importViewModel);
                    PrintFileInfo(importViewModel);
                    if (ListBoxAvailableKeys.Visibility == Visibility.Visible) {
                        LoadColumnNames();
                    }
                });
            });
        }

        private async void AsyncRenderFileWithSetPreviewToView(ImportViewModel importViewModel, bool isReread) {
            dgData.Columns.Clear();
            dgData.ItemsSource = null;
            await Task.Run(() => {
                if (isReread) {
                    importViewModel.RereadFile();
                }
                importViewModel.ManualUpdate();
                Dispatcher.Invoke(() => {
                    InitializeDataGrid(importViewModel);
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
            var currViewModel = Version == "Master" ? MasterViewModel : TestViewModel;
            if (currViewModel.Encoding.HeaderName != encodingInfo.Name) {
                currViewModel.Encoding = encodingInfo.GetEncoding();
                AsyncRenderFileWithSetPreviewToView(currViewModel, true);
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
                ButtonSkippedRows.Visibility = Visibility.Visible;
                LabelSkippedRows.Visibility = Visibility.Visible;
                ButtonSkippedRowsClick(null, null);
            } else {
                ButtonSkippedRows.Visibility = Visibility.Collapsed;
                LabelSkippedRows.Visibility = Visibility.Collapsed;
            }
        }

        private void ButtonExecuteClick(object senderIn, RoutedEventArgs eIn) {
            ReturnFromTestFile();
            MasterViewModel.IsUserInput = false;
            if (TestViewModel.IsFirstStart) {
                TestViewModel.AnalyseFile();
            }
            var masterConfig = MasterViewModel.SetImportConfiguration();
            var testConfig = TestViewModel.SetImportConfiguration();
            var configs = VerifyConfigs(masterConfig, testConfig);
            if(configs == null) {
                return;
            }
            FilesLoaded?.Invoke(configs, eIn);
            ResetUserKeys();
            MasterViewModel.PropertyChanged -= OnIsHeaderExistsChanged;
            TestViewModel.PropertyChanged -= OnIsHeaderExistsChanged;
        }

        private List<ImportConfiguration> VerifyConfigs(ImportConfiguration masterConfig, ImportConfiguration testConfig) {
            if (!masterConfig.Equals(testConfig)) {
                var userResponse = MessageBox.Show("Master and Test files have different import settings.\nDo you want to set the same settings for the Test file as the Master file?", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
                if (userResponse == MessageBoxResult.Yes) {
                    testConfig.EqualizeTo(masterConfig);
                }else if (userResponse == MessageBoxResult.Cancel) {
                    return null;
                }
            }
            return new List<ImportConfiguration>() { masterConfig, testConfig };
        }

        private void LoadColumnNames() {
            var currViewModel = Version == "Master" ? MasterViewModel : TestViewModel;
            var selectedKeys = new List<ColumnName>(CurrentColumnNamesVM.SelectedKeys);
            if(DiffColumns == null) {
                DiffColumns = GetDifferentHeaders();             
            }
            CurrentColumnNamesVM.AddUnAvailableKeys(DiffColumns);
            CurrentColumnNamesVM.AvailableKeys.Clear();
            CurrentColumnNamesVM.SelectedKeys.Clear();
            int index = 0;
            foreach (var colName in currViewModel.FileHeaders) {
                var clearedColname = ClearIndexFromColumnName(colName);
                if (!CurrentColumnNamesVM.IsKeyUnAvailable(clearedColname)) {
                    var key = new ColumnName(index, colName.Replace("_", "__"));
                    CurrentColumnNamesVM.AvailableKeys.Add(key);
                    if (selectedKeys.Any(k => ClearIndexFromColumnName(k.Value) == clearedColname)) {
                        key.IsChecked = true;
                    }
                }
                index++;
            }
            ListBoxAvailableKeys.ItemsSource = CurrentColumnNamesVM.FilteredAvailableKeys;
            ListBoxSelectedKeys.ItemsSource = CurrentColumnNamesVM.SelectedKeys;
        }

        private string ClearIndexFromColumnName(string str) {
            int startIdx = str.IndexOf(']') + 2;
            var s = str.Substring(startIdx, str.Length - startIdx);
            return s;
        }

        private void AnalyseTestFile() {
            if (TestViewModel.FileHeaders.Any()) {
                return;
            }
            if (TestViewModel.ReturnedImportConfiguration == null) {
                TestViewModel.AnalyseFile();
            } else {
                TestViewModel.AnalyseFile(TestViewModel.ReturnedImportConfiguration);
            }
        }

        private List<string> GetDifferentHeaders() {
            AnalyseTestFile();
            var mDiff = MasterViewModel.FileHeaders.Select(item=> ClearIndexFromColumnName(item)).Except(TestViewModel.FileHeaders.Select(item => ClearIndexFromColumnName(item)));
            var tDiff = TestViewModel.FileHeaders.Select(item => ClearIndexFromColumnName(item)).Except(MasterViewModel.FileHeaders.Select(item => ClearIndexFromColumnName(item)));
            return mDiff.Concat(tDiff).Distinct().ToList();
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
            var currentViewmodel = Version == "Master" ? MasterViewModel : TestViewModel;
            if (CurrentColumnNamesVM.SelectedKeys.Count > 0) {
                if (CurrentColumnNamesVM.Name == "SuggestedKey") {
                    UserKeys.UserComparisonKeys = new HashSet<int>(GetIks(CurrentColumnNamesVM.SelectedKeys));
                    ChangeButtonColor(ButtonSuggestKey, new SolidColorBrush(Colors.DarkSlateBlue));
                } else if (CurrentColumnNamesVM.Name == "UserId") {
                    UserKeys.UserIdColumns = new HashSet<int>(GetIks(CurrentColumnNamesVM.SelectedKeys));
                    ChangeButtonColor(ButtonAddIdColumns, new SolidColorBrush(Colors.DarkSlateBlue));
                } else if (CurrentColumnNamesVM.Name == "ExcludeColumn") {
                    UserKeys.UserExcludeColumns = new HashSet<int>(GetIks(CurrentColumnNamesVM.SelectedKeys));
                    ChangeButtonColor(ButtonExcludeColumns, new SolidColorBrush(Colors.DarkSlateBlue));
                }
            } else {
                if (CurrentColumnNamesVM.Name == "SuggestedKey") {
                    ChangeButtonColor(ButtonSuggestKey, (SolidColorBrush)(new BrushConverter().ConvertFrom("#373737")));
                    UserKeys.UserComparisonKeys.Clear();
                } else if (CurrentColumnNamesVM.Name == "UserId") {
                    ChangeButtonColor(ButtonAddIdColumns, (SolidColorBrush)(new BrushConverter().ConvertFrom("#373737")));
                    UserKeys.UserIdColumns.Clear();
                } else if (CurrentColumnNamesVM.Name == "ExcludeColumn") {
                    ChangeButtonColor(ButtonExcludeColumns, (SolidColorBrush)(new BrushConverter().ConvertFrom("#373737")));
                    UserKeys.UserExcludeColumns.Clear();
                }
            }
        }

        private List<int> GetIks(ObservableCollection<ColumnName> columnNames) {
            var headers = MasterViewModel.FileHeaders.Count >= TestViewModel.FileHeaders.Count ? MasterViewModel.FileHeaders : TestViewModel.FileHeaders;
            var numberedHeaders = Helpers.NumerateSequence(headers.Select(item => ClearIndexFromColumnName(item)).ToArray());
            return numberedHeaders.Where(item => columnNames.Select(col => ClearIndexFromColumnName(col.Value)).Contains(item.Value)).Select(item => item.Key).ToList();
        }

        private void HandleChecked(ColumnName columnName) {
            var clearedColName = new ColumnName(columnName.Id, ClearIndexFromColumnName(columnName.Value));           
            if (CurrentColumnNamesVM.Name == "SuggestedKey") {
                ExcludeColumnNames.UnAvailableKeys.Add(clearedColName);
                UserIdColumnNames.UnAvailableKeys.Add(clearedColName);
            } else if (CurrentColumnNamesVM.Name == "UserId") {
                SuggestedKeyColumnNames.UnAvailableKeys.Add(clearedColName);
                ExcludeColumnNames.UnAvailableKeys.Add(clearedColName);
            } else if (CurrentColumnNamesVM.Name == "ExcludeColumn") {
                SuggestedKeyColumnNames.UnAvailableKeys.Add(clearedColName);
                UserIdColumnNames.UnAvailableKeys.Add(clearedColName);
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
            var clearedColName = new ColumnName(columnName.Id, ClearIndexFromColumnName(columnName.Value));
            if (CurrentColumnNamesVM.Name == "SuggestedKey") {
                ExcludeColumnNames.UnAvailableKeys.Remove(clearedColName);
                UserIdColumnNames.UnAvailableKeys.Remove(clearedColName);
            } else if (CurrentColumnNamesVM.Name == "UserId") {
                SuggestedKeyColumnNames.UnAvailableKeys.Remove(clearedColName);
                ExcludeColumnNames.UnAvailableKeys.Remove(clearedColName);
            } else if (CurrentColumnNamesVM.Name == "ExcludeColumn") {
                SuggestedKeyColumnNames.UnAvailableKeys.Remove(clearedColName);
                UserIdColumnNames.UnAvailableKeys.Remove(clearedColName);
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

        private void ReturnFromTestFile() {
            if (Version == "Test") {
                ButtonGoForward.Visibility = Visibility.Visible;
                ButtonGoBack.ToolTip = "Back to Main Page";
            } else {
                ResetUserKeys();
            }
        }

        private void ButtonGoBackClick(object senderIn, RoutedEventArgs eIn) {
            MasterViewModel.PreviewFileName = "";
            if (Version == "Test") {
                AsyncRenderFileWithSetPreviewToView(MasterViewModel, false);
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
                AsyncRenderFileWithSetPreviewToView(TestViewModel, false);
            }
            ButtonGoForward.Visibility = Visibility.Collapsed;
            ButtonGoBack.ToolTip = "Back to Master file";
        }

        public void ResetPopUp() {
            var offset = PopupSkipedRows.HorizontalOffset;
            PopupSkipedRows.HorizontalOffset = offset + 1;
            PopupSkipedRows.HorizontalOffset = offset;
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
            if (ListBoxAvailableKeys.Visibility == Visibility.Visible && CurrentColumnNamesVM.Name == "SuggestedKey") {
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
                    AsyncRenderFileWithSetPreviewToView(currViewModel, false);
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
                    AsyncRenderFileWithSetPreviewToView(importViewmodel, false);
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

        private void ButtonSkippedRowsClick(object sender, RoutedEventArgs e) {
            PopupSkipedRows.IsOpen = true;
        }

    }
}

