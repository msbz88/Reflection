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
using Reflection.ViewModels;

namespace Reflection.Views {
    /// <summary>
    /// Interaction logic for PageImport.xaml
    /// </summary>
    public partial class PageImport : Page {
        public ImportViewModel ImportViewModel { get; set; }
        public MatchFileNamesViewModel MatchFileNamesViewModel { get; set; }
        public bool IsSingle { get; private set; }
        public EventHandler FilesLoaded { get; set; }
        public EventHandler GoBack { get; set; }

        public PageImport() {
            InitializeComponent();
            IsSingle = false;
            ImportViewModel = new ImportViewModel();           
            ComboBoxDataBinding();
        }

        public void SelectFiles() {
            MatchFileNamesViewModel = new MatchFileNamesViewModel();
            MatchFileNamesViewModel.SelectFiles();
            if (MatchFileNamesViewModel.IsReady) {
                    foreach (var item in MatchFileNamesViewModel.MatchedFileNames) {
                    ImportViewModel.PathMasterFile = item.MasterFilePath;
                    ImportViewModel.PathTestFile = item.TestFilePath;
                    ImportViewModel.LoadFileForPreview(ImportViewModel.PathMasterFile);
                    ImportViewModel.SetImportConfiguration();//cause duplicates
                }
                if (MatchFileNamesViewModel.MatchedFileNames.Count == 1) {
                    RenderFileToView();
                    TextBoxDelimiter.DataContext = ImportViewModel;
                    DisplayOnLoadEncoding();
                    IsSingle = true;
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
                viewDelimiter = "<\t> (Tab)";
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

        //private void TextBoxTextChanged(object senderIn, RoutedEventArgs eIn) {
        //    var viewDelimiter = PresentDelimiter(ImportViewModel.Delimiter);
        //    if (TextBoxDelimiter.Text != viewDelimiter) {
        //        TextBoxDelimiter.Text = viewDelimiter;
        //    }
        //}

        private void RenderFileToView() {
            PrintFileContent(System.IO.Path.GetFileName(ImportViewModel.PathMasterFile));
            TextBoxHeaderRow.Text = ImportViewModel.RowsToSkip.ToString();
        }

        private void ComboBoxSelectionChanged(object senderIn, RoutedEventArgs eIn) {
            var encodingInfo = (EncodingInfo)comboBoxEncoding.SelectedItem;
            ImportViewModel.Encoding = encodingInfo.GetEncoding();
            RenderFileToView();
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
        }

        private void PrintSkippedLines() {
            if (ImportViewModel.SkippedLines.Length > 0) {
                Grid.SetRow(dgData, 2);
                Grid.SetRow(TextBoxDelimiter, 2);
                Grid.SetRow(TextBoxHeaderRow, 3);
                Grid.SetRow(comboBoxEncoding, 4);
                Grid.SetRow(btnLoad, 5);
                TextBlockSkippedRows.Text = string.Join(Environment.NewLine, ImportViewModel.SkippedLines);
                ExpanderSkippedRows.IsExpanded = true;
            } else {
                ExpanderSkippedRows.Visibility = Visibility.Collapsed;
                Grid.SetRow(dgData, 1);
                Grid.SetRow(TextBoxDelimiter, 1);
                Grid.SetRow(TextBoxHeaderRow, 2);
                Grid.SetRow(comboBoxEncoding, 3);
                Grid.SetRow(btnLoad, 4);
            }
        }

        private void ButtonLoadClick(object senderIn, RoutedEventArgs eIn) {
            ImportViewModel.SetImportConfiguration();
            FilesLoaded?.Invoke(senderIn, eIn);
        }

        private void ButtonBackClick(object senderIn, RoutedEventArgs eIn) {
            GoBack?.Invoke(senderIn, eIn);
        }

        public void ResetPopUp() {
            var offset = PopupSkipedRows.HorizontalOffset;
            PopupSkipedRows.HorizontalOffset = offset + 1;
            PopupSkipedRows.HorizontalOffset = offset;
        }

        public void ClosePopup() {
            PopupSkipedRows.IsOpen = false;
        }

        private void PopupSkipedRows_Closed(object sender, EventArgs e) {
            ExpanderSkippedRows.IsExpanded = false;
        }
    }
}
