using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Reflection.Model;

namespace Reflection.Views {
    /// <summary>
    /// Interaction logic for MatchedFilesWindow.xaml
    /// </summary>
    public partial class MatchedFilesWindow : Window {
        public EventHandler FilesMatched { get; set; }
        MainWindow MainWindow { get; set; }

        public MatchedFilesWindow(List<MatchedFileNames> content) {          
            InitializeComponent();
            MatchedFiles.DataContext = content;
            SourceInitialized += (x, y) => {
                this.HideMinimizeAndMaximizeButtons();
            };
            Application curApp = Application.Current;
            MainWindow = (MainWindow)curApp.MainWindow;           
        }

        private void MatchedFilesWindowLoaded(object senderIn, EventArgs eIn) {
            MainWindow.ChildWindowRaised?.Invoke(this, null);
            this.Left = MainWindow.Left + (MainWindow.Width - this.ActualWidth) / 2;
            this.Top = MainWindow.Top + (MainWindow.Height - this.ActualHeight) / 2;          
        }

        private void OnOKButtonClick(object senderIn, RoutedEventArgs eIn) {
            FilesMatched?.Invoke(senderIn, eIn);
            this.Close();
        }


    }

    internal static class WindowExtensions {
        private const int GWL_STYLE = -16, WS_MAXIMIZEBOX = 0x10000, WS_MINIMIZEBOX = 0x20000;

        [DllImport("user32.dll")]
        extern private static int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        extern private static int SetWindowLong(IntPtr hwnd, int index, int value);

        internal static void HideMinimizeAndMaximizeButtons(this Window window) {
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            var currentStyle = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, (currentStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX));
        }
    }
}
