using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public ObservableCollection<FileName> MasterSelectedFiles { get; set; }
        public ObservableCollection<FileName> TestSelectedFiles { get; set; }
        string MasterCurrentFile { get; set; }
        string TestCurrentFile { get; set; }
        public bool IsReady { get; set; }
        MatchedFilesWindow MatchedFilesWindow { get; set; }
        public List<MatchedFileNames> MatchedFileNames { get; set; }
        OpenFileDialog Dialog;
        MainWindow MainWindow { get; set; }

        public MatchFileNamesViewModel() {
            MatchedFileNames = new List<MatchedFileNames>();
            Application curApp = Application.Current;
            MainWindow = (MainWindow)curApp.MainWindow;
            InitializeDialog();
        }

        public void SelectFiles() {
            MasterSelectedFiles = new ObservableCollection<FileName>(AskFilePath("Master").Select(item=>new FileName(item)));
            if (MasterSelectedFiles.Count > 0) {
                TestSelectedFiles = new ObservableCollection<FileName>(AskFilePath("Test").Select(item => new FileName(item)));
                if (TestSelectedFiles.Count > 0) {
                    if (MasterSelectedFiles.Count > 1 || TestSelectedFiles.Count > 1) {
                        MatchMultipleFileNames();
                    } else {
                        VerifySingleFileNames();
                    }
                }
            }
        }

        private void VerifySingleFileNames() {
            MatchedFileNames.Clear();
            MasterCurrentFile = MasterSelectedFiles.First().Name;
            TestCurrentFile = TestSelectedFiles.First().Name;
            try {
                CheckIfFileSelectionCorrect();
                MatchedFileNames.Add(new MatchedFileNames(MasterSelectedFiles.First().FilePath, TestSelectedFiles.First().FilePath));
                IsReady = true;
            } catch (InvalidOperationException ex) {
                var userResponse = NotifyUser(ex.Message, MessageBoxButton.YesNo);
                if (userResponse == MessageBoxResult.Yes) {
                    MatchedFileNames.Add(new MatchedFileNames(MasterSelectedFiles.First().FilePath, TestSelectedFiles.First().FilePath));
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
            MatchedFilesWindow = new MatchedFilesWindow(this);
            MainWindow.ChildWindowRaised?.Invoke(MatchedFilesWindow, null);
            MatchedFilesWindow.Owner = MainWindow;          
            MatchedFilesWindow.FilesMatched += OnFilesMatched;
            MatchedFilesWindow.ShowDialog();
        }

        private void OnFilesMatched(object sender, EventArgs e) {
            IsReady = true;
            MatchedFilesWindow.FilesMatched -= OnFilesMatched;
            MatchedFileNames.Clear();
            var len = MasterSelectedFiles.Count > TestSelectedFiles.Count ? MasterSelectedFiles.Count : TestSelectedFiles.Count;
            for (int i = 0; i < len; i++) {
                string m = "";
                string t = "";
                if (MasterSelectedFiles.Count > i) {
                    m = MasterSelectedFiles[i].FilePath;
                }
                if (TestSelectedFiles.Count > i) {
                    t = TestSelectedFiles[i].FilePath;
                }
                var pair = new MatchedFileNames(m, t);
                MatchedFileNames.Add(pair);
            }
            MatchedFileNames.RemoveAll(item => item.MasterFilePath == "" || item.TestFilePath == "");          
        }

        private void InitializeDialog() {
            Dialog = new OpenFileDialog();
            Dialog.Multiselect = true;
            Dialog.RestoreDirectory = true;
        }

        public string[] AskFilePath(string fileVersion) {                  
            Dialog.Title = "Select " + fileVersion + " file";            
            if (Dialog.ShowDialog() == true) {
                return Dialog.FileNames;
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
