using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reflection.Models;

namespace Reflection.ViewModels {
    public class ImportViewModel : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        public string PathMasterFile { get; set; }
        public string PathTestFile { get; set; }
        public string Delimiter { get; set; }
        public int RowsToSkip { get; set; }
        public bool IsHeadersExist { get; set; }
        public Encoding Encoding { get; set; }
        public string[] FileHeaders { get; set; }
        public List<string[]> PreviewContent { get; set; }
        public string[] SkippedLines { get; set; }
        public int PreviewCount { get; private set; } = 200;
        public ImportConfiguration ImportConfiguration { get; private set; }

        public ImportViewModel() {
        }

        public void CheckIfFileSelectionCorrect() {
            if (PathMasterFile == PathTestFile) {
                throw new InvalidOperationException("You select the same file twice\n"
                    + "\tMaster file: " + PathMasterFile + "\n"
                    + "\tTest File: " + PathTestFile + "\n"
                    + "Do you want to pick files again?");
            } else if (Path.GetFileName(PathMasterFile)[0] == ']' || (PathMasterFile.Contains("Test") && !PathTestFile.Contains("Test"))) {
                throw new InvalidOperationException("It looks like you select Test file instead of Master file\n"
                    + "\tMaster file: " + PathMasterFile + "\n"
                    + "\tTest File: " + PathTestFile + "\n"
                    + "Do you want to pick files again?");
            } else if (Path.GetFileName(PathTestFile)[0] == '[' || (PathTestFile.Contains("Master") && !PathMasterFile.Contains("Master"))) {
                throw new InvalidOperationException("It looks like you select Master file instead of Test file\n"
                    + "\tMaster file: " + PathMasterFile + "\n"
                    + "\tTest File: " + PathTestFile + "\n"
                    + "Do you want to pick files again?");
            }
        }

        public void LoadFileForPreview(string path) {
            var fileReader = new FileReader();
            Encoding = Encoding.ASCII;
            var fileContent = fileReader.ReadFewLines(path, PreviewCount, Encoding);
            Delimiter = FindDelimiter(fileContent.Take(50));
            RowsToSkip = FindDataBeginning(fileContent.Take(50));
            var firstRow = fileContent.Skip(RowsToSkip).FirstOrDefault().Split(new[] { Delimiter }, StringSplitOptions.None);
            IsHeadersExist = IsHeadersRow(firstRow);
            if (IsHeadersExist) {
                FileHeaders = firstRow;
            }
            PreviewContent = new List<string[]>();
            foreach (var line in fileContent.Skip(RowsToSkip + 1)) {
                PreviewContent.Add(line.Split(new[] { Delimiter }, StringSplitOptions.None));
            }
            SkippedLines = fileContent.Take(RowsToSkip).ToArray();
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

        public void SetImportConfiguration() {
            ImportConfiguration = new ImportConfiguration(
                pathMasterFile: PathMasterFile,
                pathTestFile: PathTestFile,
                delimiter: Delimiter,
                rowsToSkip: RowsToSkip,
                isHeadersExist: IsHeadersExist,
                encoding: Encoding.ASCII
                );
            RaisePropertyChanged("ImportConfiguration");
        }

        private void RaisePropertyChanged(string propertyName) {
            this.PropertyChanged?.Invoke(this.ImportConfiguration, new PropertyChangedEventArgs(propertyName));
        }

    }
}
