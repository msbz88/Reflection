using System;
using System.Collections.Generic;
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
    /// Interaction logic for PageImport.xaml
    /// </summary>
    public partial class PageImport : Page {
        public ImportViewModel ImportViewModel { get; set; }
        public MatchFileNamesViewModel MatchFileNamesViewModel { get; set; }
        ColumnNamesViewModel ColumnNamesViewModel = new ColumnNamesViewModel();
        public bool IsSingle { get; private set; }
        public EventHandler FilesLoaded { get; set; }
        public EventHandler GoBack { get; set; }
        private string customDelimiter;

        public PageImport() {
            InitializeComponent();
            ImportViewModel = new ImportViewModel();
            ComboBoxDataBinding();
            this.DataContext = ImportViewModel;
        }

        public void SelectFiles() {
            IsSingle = false;
            MatchFileNamesViewModel = new MatchFileNamesViewModel();
            MatchFileNamesViewModel.SelectFiles();
            if (MatchFileNamesViewModel.IsReady) {
                if (MatchFileNamesViewModel.MatchedFileNames.Count == 1) {
                    ImportViewModel.PathMasterFile = MatchFileNamesViewModel.MatchedFileNames[0].MasterFilePath;
                    ImportViewModel.PathTestFile = MatchFileNamesViewModel.MatchedFileNames[0].TestFilePath;
                    ImportViewModel.AnalyseFile(ImportViewModel.PathMasterFile);
                    RenderFileToView(ImportViewModel.PathMasterFile);
                    TextBoxDelimiter.Text = PresentDelimiter(ImportViewModel.Delimiter);
                    DisplayOnLoadEncoding();
                    IsSingle = true;
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

        private void RenderFileToView(string path) {
            PrintFileContent(System.IO.Path.GetFileName(path));
            if (ImportViewModel.RowsToSkip == 0 && !ImportViewModel.IsHeadersExist) {
                TextBoxHeaderRow.Text = "No header found";
            } else {
                TextBoxHeaderRow.Text = ImportViewModel.RowsToSkip.ToString();
            }
        }

        private void ComboBoxSelectionChanged(object senderIn, RoutedEventArgs eIn) {
            var encodingInfo = (EncodingInfo)comboBoxEncoding.SelectedItem;
            ImportViewModel.Encoding = encodingInfo.GetEncoding();
            if (TextBlockFileName.Text == (System.IO.Path.GetFileName(ImportViewModel.PathMasterFile))) {
                RenderFileToView(ImportViewModel.PathMasterFile);
            } else {
                RenderFileToView(ImportViewModel.PathTestFile);
            }
        }

        private void ComboBoxDataBinding() {
            EncodingInfo[] codePages = Encoding.GetEncodings();
            comboBoxEncoding.ItemsSource = codePages;
        }

        public void PrintFileContent(string fileName) {
            dgData.Columns.Clear();
            int index = 0;
            foreach (var item in ImportViewModel.FileHeaders) {
                var column = new DataGridTextColumn();
                column.Header = item.Replace("_", "__");
                column.Binding = new Binding(string.Format("[{0}]", index));
                dgData.Columns.Add(column);
                index++;
            }
            //dgData.DataContext = ImportViewModel.PreviewContent;
            dgData.UpdateLayout();
            PrintSkippedLines();
            PrintFileDettails(fileName);
        }

        private void PrintFileDettails(string fileName) {
            TextBlockFileName.Text = fileName;
        }

        private void PrintSkippedLines() {
            if (ImportViewModel.SkippedLines.Length > 0) {
                Grid.SetRow(dgData, 2);
                Grid.SetRow(TextBoxDelimiter, 2);
                Grid.SetRow(TextBoxHeaderRow, 3);
                Grid.SetRow(comboBoxEncoding, 4);
                Grid.SetRow(ButtonExecute, 5);
                Grid.SetRow(ButtonSuggestKey, 5);
                TextBlockSkippedRows.Text = string.Join(Environment.NewLine, ImportViewModel.SkippedLines);
                ExpanderSkippedRows.Visibility = Visibility.Visible;
                ExpanderSkippedRows.IsExpanded = true;
            } else {
                ExpanderSkippedRows.Visibility = Visibility.Collapsed;
                Grid.SetRow(dgData, 1);
                Grid.SetRow(TextBoxDelimiter, 1);
                Grid.SetRow(TextBoxHeaderRow, 2);
                Grid.SetRow(comboBoxEncoding, 3);
                Grid.SetRow(ButtonExecute, 4);
                Grid.SetRow(ButtonSuggestKey, 4);
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
            if (TextBlockFileName.Text == (System.IO.Path.GetFileName(ImportViewModel.PathTestFile))) {
                ImportViewModel.AnalyseFile(ImportViewModel.PathMasterFile);
                RenderFileToView(ImportViewModel.PathMasterFile);
                TextBoxDelimiter.Text = PresentDelimiter(ImportViewModel.Delimiter);
                DisplayOnLoadEncoding();
                ButtonGoForward.Visibility = Visibility.Visible;
                ButtonGoBack.ToolTip = "Back to Main Page";
            } else {
                ResetUserKeys();
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
            if (TextBlockFileName.Text != (System.IO.Path.GetFileName(ImportViewModel.PathTestFile))) {
                ImportViewModel.AnalyseFile(ImportViewModel.PathTestFile);
                RenderFileToView(ImportViewModel.PathTestFile);
                TextBoxDelimiter.Text = PresentDelimiter(ImportViewModel.Delimiter);
                DisplayOnLoadEncoding();
                ButtonGoForward.Visibility = Visibility.Collapsed;
                ButtonGoBack.ToolTip = "Back to Master file";
            }
        }

        private void OnExpanded(object senderIn, RoutedEventArgs eIn) {
            PopupSkipedRows.IsOpen = true;
        }

        private void OnCollapsed(object senderIn, RoutedEventArgs eIn) {
            PopupSkipedRows.IsOpen = false;
        }

        public void ResetPopUp() {
            var offset = PopupSkipedRows.HorizontalOffset;
            PopupSkipedRows.HorizontalOffset = offset + 1;
            PopupSkipedRows.HorizontalOffset = offset;
        }

        private void PopupSkipedRows_Closed(object sender, EventArgs e) {
            ExpanderSkippedRows.IsExpanded = false;
        }

        private void TextBoxHeaderRowGotFocus(object senderIn, RoutedEventArgs eIn) {
            if (TextBoxHeaderRow.Text == "No header found") {
                TextBoxHeaderRow.Text = "";
            }
        }

        private void TextBoxHeaderRowLostFocus(object senderIn, RoutedEventArgs eIn) {
            if (TextBoxHeaderRow.Text == "") {
                TextBoxHeaderRow.Text = "No header found";
            }
        }

        private void TextBoxHeaderRowTextChanged(object sender, TextChangedEventArgs e) {
            if (TextBoxHeaderRow.Text != "No header found" && !string.IsNullOrEmpty(TextBoxHeaderRow.Text)) {
                var val = int.Parse(TextBoxHeaderRow.Text);
                if (val == 0 && !ImportViewModel.IsHeadersExist) {
                    ImportViewModel.RowsToSkip = val;
                    ImportViewModel.IsHeadersExist = true;
                    ImportViewModel.SetPreview(ImportViewModel.PathMasterFile);
                    RenderFileToView(ImportViewModel.PathMasterFile);
                    LoadColumnNames();
                } else if (ImportViewModel.RowsToSkip != val) {
                    ImportViewModel.RowsToSkip = val;
                    ImportViewModel.IsHeadersExist = true;
                    ImportViewModel.SetPreview(ImportViewModel.PathMasterFile);
                    RenderFileToView(ImportViewModel.PathMasterFile);
                    LoadColumnNames();
                }
            } else if (TextBoxHeaderRow.Text == "No header found" && ImportViewModel.IsHeadersExist) {
                ImportViewModel.RowsToSkip = 0;
                ImportViewModel.IsHeadersExist = false;
                ImportViewModel.SetPreview(ImportViewModel.PathMasterFile);
                RenderFileToView(ImportViewModel.PathMasterFile);
                LoadColumnNames();
            }
        }
    }
}

