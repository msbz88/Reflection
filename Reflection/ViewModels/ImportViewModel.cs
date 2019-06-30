using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reflection.Model;
using Reflection.Models;

namespace Reflection.ViewModels {
    public class ImportViewModel : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        private string pathMasterFile;
        public string PathMasterFile {
            get { return pathMasterFile; }
            set {
                pathMasterFile = value;
                Encoding = GetEncoding(PathMasterFile);
            }
        }
        public List<int> UserKeys { get; set; } = new List<int>();
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

        public void AnalyseFile(string path) {
            var fileReader = new FileReader();
            var fileContent = fileReader.ReadFewLines(path, PreviewCount, Encoding);
            Delimiter = FindDelimiter(fileContent.Take(50));
            RowsToSkip = FindDataBeginning(fileContent.Take(50));
            var firstRow = fileContent.Skip(RowsToSkip).FirstOrDefault().Split(new[] { Delimiter }, StringSplitOptions.None);
            IsHeadersExist = IsHeadersRow(firstRow);
            if (IsHeadersExist) {
                FileHeaders = firstRow;
            } else {
                FileHeaders = GenerateDefaultHeaders(firstRow.Length);
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

        private string[] GenerateDefaultHeaders(int columnsCount) {
            var headers = new string[columnsCount];
            for (int i = 0; i < columnsCount; i++) {
                headers[i] = "Column" + i;
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

        public void SetImportConfiguration() {
            ImportConfiguration = new ImportConfiguration(
                pathMasterFile: PathMasterFile,
                pathTestFile: PathTestFile,
                delimiter: Delimiter,
                rowsToSkip: RowsToSkip,
                isHeadersExist: IsHeadersExist,
                encoding: Encoding,
                userKeys: UserKeys
                );
            RaisePropertyChanged("ImportConfiguration");
        }

        private void RaisePropertyChanged(string propertyName) {
            this.PropertyChanged?.Invoke(this.ImportConfiguration, new PropertyChangedEventArgs(propertyName));
        }


    }
}
