using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Reflection.Model;
using Reflection.Models;

namespace Reflection.ViewModels {
    public class ImportViewModel : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        public string FilePath { get; set; }
        public List<int> UserKeys { get; set; }
        public List<int> UserIdColumns { get; set; }
        public List<int> UserExcludeColumns { get; set; }
        public string Delimiter { get; set; }
        public int RowsToSkip { get; set; }
        bool isHeadersExist;
        public bool IsHeadersExist {
            get { return isHeadersExist; }
            set {
                if (isHeadersExist != value) {
                    isHeadersExist = value;
                    if (!IsMultiple) {
                        RaisePropertyChanged("IsHeadersExist");
                    }                   
                }
            }
        }
        public Encoding Encoding { get; set; }
        public List<string> FileHeaders { get; set; }
        public ObservableCollection<string[]> PreviewContent { get; set; }
        public List<string> SkippedLines { get; set; }
        public int PreviewCount { get; private set; } = 200;
        FileReader FileReader;
        string[] FileContent;
        string[] FirstRow;
        string[] SecondRow;
        public bool IsGeneratedHeaders { get; set; }
        string previewFileName;
        public string PreviewFileName {
            get { return Path.GetFileName(FilePath); }
            set {
                if (previewFileName != value) {
                    previewFileName = value;
                    if (!IsMultiple) {
                        RaisePropertyChanged("PreviewFileName");
                    }
                }
            }
        }
        public bool IsFirstStart { get; set; }
        public bool IsUserInput { get; set; }
        public bool IsMultiple { get; set; }

        public ImportViewModel() {
            UserKeys = new List<int>();
            UserIdColumns = new List<int>();
            UserExcludeColumns = new List<int>();
            FileHeaders = new List<string>();
            if (IsMultiple) {
                PreviewContent = new ObservableCollection<string[]>();
            }else {
                PreviewContent = new AsyncObservableCollection<string[]>();
            }           
            SkippedLines = new List<string>();
            FileReader = new FileReader();
            IsFirstStart = true;           
        }

        private void UpdateHeaders(string[] headers) {
            FileHeaders.Clear();
            FileHeaders.AddRange(headers);
        }

        public void AnalyseFile() {
            Encoding = GetEncoding(FilePath);
            FileContent = FileReader.ReadFewLines(FilePath, PreviewCount, Encoding).ToArray();
            if (FileContent.Any()) {
                Delimiter = FindDelimiter(FileContent.Take(50));
                RowsToSkip = FindDataBeginning(FileContent.Take(50));
                if (FileContent.Skip(RowsToSkip).Count() > 1) {
                    FirstRow = FileContent.Skip(RowsToSkip).FirstOrDefault().Split(new[] { Delimiter }, StringSplitOptions.None);
                    SecondRow = FileContent.Skip(RowsToSkip + 1).FirstOrDefault().Split(new[] { Delimiter }, StringSplitOptions.None);
                    var headers = HeaderCheck(FirstRow, SecondRow);
                    if (headers == null) {
                        UpdateHeaders(GenerateDefaultHeaders(FirstRow.Length));
                        IsHeadersExist = false;
                    } else {
                        UpdateHeaders(headers);
                        IsHeadersExist = true;
                    }                   
                } else {
                    var headers = FileContent.First().Split(new[] { Delimiter }, StringSplitOptions.None);
                    IsHeadersExist = IsHeadersRow(headers);
                    if (IsHeadersExist) {
                        UpdateHeaders(headers);
                    } else {
                        UpdateHeaders(GenerateDefaultHeaders(headers.Length));
                    }
                }
                SetPreview(FilePath);
            }
            IsFirstStart = false;
            IsUserInput = true;
        }

        public void RereadFile() {
            FileContent = FileReader.ReadFewLines(FilePath, PreviewCount, Encoding).ToArray();
        }

        public void ManualUpdate() {
            if (RowsToSkip >= FileContent.Length - 1) {
                UpdateHeaders(GenerateDefaultHeaders(FirstRow.Length));
                SkippedLines.Clear();
                SkippedLines.AddRange(FileContent.Take(RowsToSkip));
                UpdatePreview();
                return;
            }
            if (IsHeadersExist) {
                FirstRow = FileContent.Skip(RowsToSkip).FirstOrDefault().Split(new[] { Delimiter }, StringSplitOptions.None);
                SecondRow = FileContent.Skip(RowsToSkip + 1).FirstOrDefault().Split(new[] { Delimiter }, StringSplitOptions.None);
                var headers = HeaderCheck(FirstRow, SecondRow);
                if (headers == null) {
                    UpdateHeaders(FirstRow);
                } else {
                    UpdateHeaders(headers);
                }
            } else {
                UpdateHeaders(GenerateDefaultHeaders(FirstRow.Length));
            }
            SetPreview(FilePath);
        }

        private void SetPreview(string path) {
            UpdatePreview();
            IsGeneratedHeaders = FileHeaders.Any(item => item.Contains("Column"));
            SkippedLines.Clear();
            SkippedLines.AddRange(FileContent.Take(RowsToSkip));
        }

        private void UpdatePreview() {
            var fileContentCorr = IsHeadersExist ? FileContent.Skip(RowsToSkip + 1) : FileContent.Skip(RowsToSkip);
            PreviewContent.Clear();
            foreach (var item in fileContentCorr) {
                PreviewContent.Add(item.Split(new[] { Delimiter }, StringSplitOptions.None));
            }
        }

        public string FindDelimiter(IEnumerable<string> fileContent) {
            List<char> delimiters = new List<char> { '\t', ';', ',', '|' };
            Dictionary<char, int> counts = delimiters.ToDictionary(key => key, value => 0);
            foreach (char delimiter in delimiters) {
                counts[delimiter] = fileContent.Sum(item => item.Where(chr => chr == delimiter).Count());
            }
            var maxRepeated = counts.Max(item => item.Value);
            return counts.Where(item => item.Value == maxRepeated).Select(item => item.Key).FirstOrDefault().ToString();
        }

        private int FindDataBeginning(IEnumerable<string> fileContent) {
            if (!fileContent.Any()) {
                return 0;
            }
            var data = fileContent.Select(line => line.Split(new string[] { Delimiter }, StringSplitOptions.None).Length);
            var max = data.Max();
            return data.ToList().IndexOf(max);
        }

        private bool IsHeadersRow(string[] row) {
            foreach (var item in row) {
                if (string.IsNullOrWhiteSpace(item)) {
                    return false;
                }
                if (!IsString(item)) {
                    return false;
                }
            }
            return true;
        }

        private string[] HeaderCheck(string[] firstRow, string[] secondRow) {
            if(firstRow.Length != secondRow.Length) {
                return null;
            }
            var result = new string[firstRow.Length];
            for (int i = 0; i < firstRow.Length; i++) {
                var isFirstString = IsString(firstRow[i]);
                var isSecondString = IsString(secondRow[i]);
                if (firstRow[i] == "") {
                    result[i] = ApplyHeadersCount(firstRow[i], i);
                } else if (isFirstString && !isSecondString) {
                    result[i] = ApplyHeadersCount(firstRow[i], i);
                } else if (isFirstString && isSecondString) {
                    if (firstRow[i] == secondRow[i]) {
                        return null;
                    } else {
                        result[i] = ApplyHeadersCount(firstRow[i], i);
                    }
                } else if (!isFirstString && !isSecondString || !isFirstString && isSecondString) {
                    double d;
                    DateTime t;
                    if (double.TryParse(firstRow[i], out d) || DateTime.TryParse(firstRow[i], out t)) {
                        return null;
                    } else {
                        result[i] = ApplyHeadersCount(firstRow[i], i);
                    }
                }
            }
            return result;
        }

        private string ApplyHeadersCount(string header, int i) {
            return "[" + (i + 1) + "] " + header;
        }

        private bool IsString(string item) {
            int i;
            long l;
            double d;
            DateTime t;
            if (int.TryParse(item, out i) || long.TryParse(item, out l) || double.TryParse(item, out d) || DateTime.TryParse(item, out t)) {
                return false;
            }
            return true;
        }

        private string[] GenerateDefaultHeaders(int columnsCount) {
            var headers = new string[columnsCount];
            for (int i = 0; i < columnsCount; i++) {
                headers[i] = "Column" + (i + 1);
            }
            return headers;
        }

        private Encoding GetEncoding(string filename) {
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
                file.Read(bom, 0, 4);
            }
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76)
                return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
                return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe)
                return Encoding.Unicode;
            if (bom[0] == 0xfe && bom[1] == 0xff)
                return Encoding.BigEndianUnicode;
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff)
                return Encoding.UTF32;
            return Encoding.Default;
        }

        public ImportConfiguration SetImportConfiguration() {
            return new ImportConfiguration(
                filePath: FilePath,
                delimiter: Delimiter,
                rowsToSkip: RowsToSkip,
                isHeadersExist: IsHeadersExist,
                encoding: Encoding,
                userKeys: UserKeys,
                userIdColumns: UserIdColumns,
                userExcludeColumns: UserExcludeColumns,
                columnsCount: FileHeaders.Count
                );
        }

        private void RaisePropertyChanged(string propertyName) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



    }
}
