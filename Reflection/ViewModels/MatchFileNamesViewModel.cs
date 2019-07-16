using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Reflection.Model;
using Reflection.Models;
using Reflection.Views;

namespace Reflection.ViewModels {
    public class MatchFileNamesViewModel {
        public string[] MasterSelectedFiles { get; set; }
        public string[] TestSelectedFiles { get; set; }
        string MasterCurrentFile { get; set; }
        string TestCurrentFile { get; set; }
        public bool IsReady { get; private set; }
        MatchedFilesWindow MatchedFilesWindow { get; set; }
        public List<MatchedFileNames> MatchedFileNames { get; set; }

        public MatchFileNamesViewModel() {
            MatchedFileNames = new List<MatchedFileNames>();
        }

        public void SelectFiles() {
            MasterSelectedFiles = AskFilePath("Master");
            if (MasterSelectedFiles.Length > 0) {
                TestSelectedFiles = AskFilePath("Test");
                if (TestSelectedFiles.Length > 0) {
                    if (MasterSelectedFiles.Length > 1 || TestSelectedFiles.Length > 1) {
                        MatchMultipleFileNames();
                    } else {
                        MatchSingleFileNames();
                    }
                }
            }
        }

        private void MatchSingleFileNames() {
            MasterCurrentFile = Path.GetFileName(MasterSelectedFiles[0]);
            TestCurrentFile = Path.GetFileName(TestSelectedFiles[0]);
            try {
                CheckIfFileSelectionCorrect();
                MatchedFileNames.Add(new MatchedFileNames(MasterSelectedFiles[0], TestSelectedFiles[0]));
                IsReady = true;
            } catch (InvalidOperationException ex) {
                var userResponse = NotifyUser(ex.Message, MessageBoxButton.YesNo);
                if (userResponse == MessageBoxResult.Yes) {
                    MatchedFileNames.Add(new MatchedFileNames(MasterSelectedFiles[0], TestSelectedFiles[0]));
                    IsReady = true;
                } else if (userResponse == MessageBoxResult.No) {
                    IsReady = false;
                }
            }
        }

        private MessageBoxResult NotifyUser(string message, MessageBoxButton messageBoxButton) {
            return MessageBox.Show(message, "", messageBoxButton, MessageBoxImage.Question, MessageBoxResult.No);
        }

        private string GetCorrectedFileName(string path) {
            var originName = Path.GetFileName(path);
            var fileName = "";
            if (originName[0] == '[') {
                fileName = originName.TrimStart('[');
            }else if (originName[0] == ']') {
                fileName = originName.TrimStart(']');
            }
            return fileName;
        }

        private void MatchMultipleFileNames() {
            var testFiles = TestSelectedFiles.ToList();
            var prevLen = 0;
            string bestMatchedTest = "";
            foreach (var masterPath in MasterSelectedFiles) {
                var masterFile = GetCorrectedFileName(masterPath);
                prevLen = 0;
                MatchedFileNames pair = null;
                foreach (var testPath in testFiles) {
                    var testFile = GetCorrectedFileName(testPath);
                    int matchedLen = Lcs(masterFile, testFile);
                    if (matchedLen == masterFile.Length) {
                        pair = new MatchedFileNames(masterPath, testPath);
                        bestMatchedTest = testFile;
                        break;
                    } else {
                        if (matchedLen > prevLen && matchedLen > 5) {
                            prevLen = matchedLen;
                            bestMatchedTest = testPath;
                        }
                    }
                }
                if (pair == null) {
                    pair = new MatchedFileNames(masterPath, bestMatchedTest);
                }
                MatchedFileNames.Add(pair);
                testFiles.Remove(bestMatchedTest);
            }
            var masterExtraFiles = MasterSelectedFiles.Where(item => !MatchedFileNames.Select(pair => pair.MasterFilePath).Contains(item)).Select(item => new MatchedFileNames(item, ""));
            masterExtraFiles.Concat(MatchedFileNames.Where(item => item.TestFilePath == ""));
            MatchedFileNames.AddRange(masterExtraFiles);
            var testExtraFiles = TestSelectedFiles.Where(item => !MatchedFileNames.Select(pair => pair.TestFilePath).Contains(item)).Select(item => new MatchedFileNames("", item));
            testExtraFiles.Concat(MatchedFileNames.Where(item => item.MasterFilePath == ""));
            MatchedFileNames.AddRange(testExtraFiles);
            MatchedFilesWindow = new MatchedFilesWindow(MatchedFileNames);
            MatchedFilesWindow.FilesMatched += OnFilesMatched;
            MatchedFilesWindow.ShowDialog();
        }

        private void OnFilesMatched(object sender, EventArgs e) {
            IsReady = true;
            MatchedFilesWindow.FilesMatched -= OnFilesMatched;
            var unMatchedFiles = MatchedFileNames.Where(item => item.MasterFilePath == "" || item.TestFilePath == "").ToList();
            foreach (var item in unMatchedFiles) {
                MatchedFileNames.Remove(item);
            }
        }

        private int Lcs(string a, string b) {
            var lengths = new int[a.Length, b.Length];
            int greatestLength = 0;
            string output = "";
            for (int i = 0; i < a.Length; i++) {
                for (int j = 0; j < b.Length; j++) {
                    if (a[i] == b[j]) {
                        lengths[i, j] = i == 0 || j == 0 ? 1 : lengths[i - 1, j - 1] + 1;
                        if (lengths[i, j] > greatestLength) {
                            greatestLength = lengths[i, j];
                            output = a.Substring(i - greatestLength + 1, greatestLength);
                        }
                    } else {
                        lengths[i, j] = 0;
                    }
                }
            }
            return output.Length;
        }

        public string[] AskFilePath(string fileVersion) {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Title = "Select " + fileVersion + " file";
            dialog.RestoreDirectory = true;
            if (dialog.ShowDialog() == true) {
                return dialog.FileNames;
            }
            return new string[0];
        }

        public void CheckIfFileSelectionCorrect() {
            if (MasterCurrentFile == TestCurrentFile) {
                throw new InvalidOperationException("You select the same file twice\n"
                    + "Master file: " + MasterCurrentFile + "\n"
                    + "Test File: " + TestCurrentFile + "\n"
                    + "Proceed anyway?");
            } else if (Path.GetFileName(MasterCurrentFile)[0] == ']' || (MasterCurrentFile.ToLower().Contains("test") && !TestCurrentFile.ToLower().Contains("test"))) {
                throw new InvalidOperationException("It looks like you select Test file instead of Master file\n"
                    + "Master file: " + MasterCurrentFile + "\n"
                    + "Test File: " + TestCurrentFile + "\n"
                    + "Proceed anyway?");
            } else if (Path.GetFileName(TestCurrentFile)[0] == '[' || (TestCurrentFile.Contains("Master") && !MasterCurrentFile.Contains("Master"))) {
                throw new InvalidOperationException("It looks like you select Master file instead of Test file\n"
                    + "Master file: " + MasterCurrentFile + "\n"
                    + "Test File: " + TestCurrentFile + "\n"
                    + "Proceed anyway?");
            }
        }
    }
}
