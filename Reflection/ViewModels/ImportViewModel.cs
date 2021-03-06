﻿using System;
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
        public char[] Delimiter { get; set; }
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
        public ImportConfiguration ReturnedImportConfiguration { get; set; }

        public ImportViewModel() {
            FileHeaders = new List<string>();
            if (IsMultiple) {
                PreviewContent = new ObservableCollection<string[]>();
            } else {
                PreviewContent = new AsyncObservableCollection<string[]>();
            }
            SkippedLines = new List<string>();
            FileReader = new FileReader();
            IsFirstStart = true;
            FirstRow = new string[0];
            SecondRow = new string[0];
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
                    FirstRow = Splitter.Split(FileContent.Skip(RowsToSkip).FirstOrDefault(), Delimiter);
                    SecondRow = Splitter.Split(FileContent.Skip(RowsToSkip + 1).FirstOrDefault(), Delimiter);
                    var headers = HeaderCheck(FirstRow, SecondRow);
                    if (headers == null) {
                        UpdateHeaders(GenerateDefaultHeaders(FirstRow.Length));
                        IsHeadersExist = false;
                    } else {
                        UpdateHeaders(headers);
                        IsHeadersExist = true;
                    }
                } else {
                    var headers = Splitter.Split(FileContent.First(), Delimiter);
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

        public void AnalyseFile(ImportConfiguration importConfiguration) {
            Encoding = importConfiguration.Encoding;
            FileContent = FileReader.ReadFewLines(importConfiguration.FilePath, PreviewCount, Encoding).ToArray();
            if (FileContent.Any()) {
                Delimiter = importConfiguration.Delimiter;
                RowsToSkip = importConfiguration.RowsToSkip;
                if (FileContent.Skip(RowsToSkip).Count() > 1) {
                    FirstRow = Splitter.Split(FileContent.Skip(RowsToSkip).FirstOrDefault(), Delimiter);
                    SecondRow = Splitter.Split(FileContent.Skip(RowsToSkip + 1).FirstOrDefault(), Delimiter);
                    var headers = HeaderCheck(FirstRow, SecondRow);
                    if (headers == null) {
                        UpdateHeaders(GenerateDefaultHeaders(FirstRow.Length));
                        IsHeadersExist = false;
                    } else {
                        UpdateHeaders(headers);
                        IsHeadersExist = true;
                    }
                } else {
                    var headers = Splitter.Split(FileContent.First(), Delimiter);
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
                UpdateSkippedRows();
                UpdatePreview();
                return;
            }
            if (IsHeadersExist) {
                FirstRow = Splitter.Split(FileContent.Skip(RowsToSkip).FirstOrDefault(), Delimiter);
                SecondRow = Splitter.Split(FileContent.Skip(RowsToSkip + 1).FirstOrDefault(), Delimiter);
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

        private void UpdateSkippedRows() {
            SkippedLines.Clear();
            foreach (var item in FileContent.Take(RowsToSkip)) {
                var len = item.Length;
                var trimmedItem = len <= 500 ? item : item.Substring(0, 499);
                SkippedLines.Add(trimmedItem);
            }
        }

        private void SetPreview(string path) {
            UpdatePreview();
            IsGeneratedHeaders = FileHeaders.Any(item => item.Contains("Column"));
            UpdateSkippedRows();
        }

        private void UpdatePreview() {
            var fileContentCorr = IsHeadersExist ? FileContent.Skip(RowsToSkip + 1) : FileContent.Skip(RowsToSkip);
            PreviewContent.Clear();
            foreach (var item in fileContentCorr) {
                PreviewContent.Add(Splitter.Split(item, Delimiter));
            }
        }

        public char[] FindDelimiter(IEnumerable<string> fileContent) {
            List<char> delimiters = new List<char> { '\t', ';', ',', '|' };
            Dictionary<char, int> counts = delimiters.ToDictionary(key => key, value => 0);
            foreach (char delimiter in delimiters) {
                counts[delimiter] = fileContent.Sum(item => item.Where(chr => chr == delimiter).Count());
            }
            var topTwoDelimiters = counts.OrderByDescending(item => item.Value).Take(2);
            if (topTwoDelimiters.Last().Key == '\t' && topTwoDelimiters.Last().Value > 0) {
                return new char[] { '\t' };
            } else if (topTwoDelimiters.First().Key == '\t' && topTwoDelimiters.Last().Key == ';' && topTwoDelimiters.Last().Value > 0) {
                return new char[] { ';' };
            } else {
                return new char[] { topTwoDelimiters.First().Key };
            }
        }

        private int FindDataBeginning(IEnumerable<string> fileContent) {
            if (!fileContent.Any()) {
                return 0;
            }
            var data = fileContent.Select(line => Splitter.Split(line, Delimiter).Length);
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
            if (firstRow.Length != secondRow.Length) {
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
                columnsCount: FileHeaders.Count
                );
        }

        private void RaisePropertyChanged(string propertyName) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



    }
}
