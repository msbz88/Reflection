using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Reflection.Models;
using Reflection.ViewModels;

namespace Reflection.Views {
    /// <summary>
    /// Interaction logic for MatchedFilesWindow.xaml
    /// </summary>
    public partial class MatchedFilesWindow : Window {
        public EventHandler FilesMatched { get; set; }
        MatchFileNamesViewModel MatchFileNamesViewModel;

        public MatchedFilesWindow(MatchFileNamesViewModel matchFileNamesViewModel) {
            MatchFileNamesViewModel = matchFileNamesViewModel;
            InitializeComponent();
            DataContext = MatchFileNamesViewModel;
            SourceInitialized += (x, y) => {
                this.HideMinimizeAndMaximizeButtons();
            };
            InitializeMasterDragDrop();
            InitializeTestDragDrop();
        }

        private void InitializeMasterDragDrop() {
            Style itemContainerStyle = new Style(typeof(ListBoxItem));
            itemContainerStyle.Setters.Add(new Setter(AllowDropProperty, true));
            itemContainerStyle.Setters.Add(new EventSetter(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(MasterPreviewMouseLeftButtonDown)));
            itemContainerStyle.Setters.Add(new EventSetter(DropEvent, new DragEventHandler(ListViewMasterFilesDrop)));
            ListViewMasterFiles.ItemContainerStyle = itemContainerStyle;
        }

        void ListViewMasterFilesDrop(object sender, DragEventArgs e) {
            FileName droppedData = e.Data.GetData(typeof(FileName)) as FileName;
            FileName target = ((ListBoxItem)(sender)).DataContext as FileName;
            int removedIdx = ListViewMasterFiles.Items.IndexOf(droppedData);
            int targetIdx = ListViewMasterFiles.Items.IndexOf(target);
            if (removedIdx < targetIdx) {
                MatchFileNamesViewModel.MasterSelectedFiles.Insert(targetIdx + 1, droppedData);
                MatchFileNamesViewModel.MasterSelectedFiles.RemoveAt(removedIdx);
            } else {
                int remIdx = removedIdx + 1;
                if (MatchFileNamesViewModel.MasterSelectedFiles.Count + 1 > remIdx) {
                    MatchFileNamesViewModel.MasterSelectedFiles.Insert(targetIdx, droppedData);
                    MatchFileNamesViewModel.MasterSelectedFiles.RemoveAt(remIdx);
                }
            }
        }

        private void InitializeTestDragDrop() {
            Style itemContainerStyle = new Style(typeof(ListBoxItem));
            itemContainerStyle.Setters.Add(new Setter(AllowDropProperty, true));
            itemContainerStyle.Setters.Add(new EventSetter(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(TestPreviewMouseLeftButtonDown)));
            itemContainerStyle.Setters.Add(new EventSetter(DropEvent, new DragEventHandler(ListViewTestFilesDrop)));
            ListViewTestFiles.ItemContainerStyle = itemContainerStyle;
        }

        void ListViewTestFilesDrop(object sender, DragEventArgs e) {
            FileName droppedData = e.Data.GetData(typeof(FileName)) as FileName;
            FileName target = ((ListBoxItem)(sender)).DataContext as FileName;
            int removedIdx = ListViewTestFiles.Items.IndexOf(droppedData);
            int targetIdx = ListViewTestFiles.Items.IndexOf(target);
            if (removedIdx < targetIdx) {
                MatchFileNamesViewModel.TestSelectedFiles.Insert(targetIdx + 1, droppedData);
                MatchFileNamesViewModel.TestSelectedFiles.RemoveAt(removedIdx);
            } else {
                int remIdx = removedIdx + 1;
                if (MatchFileNamesViewModel.TestSelectedFiles.Count + 1 > remIdx) {
                    MatchFileNamesViewModel.TestSelectedFiles.Insert(targetIdx, droppedData);
                    MatchFileNamesViewModel.TestSelectedFiles.RemoveAt(remIdx);
                }
            }
        }

        private void MasterPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (sender is ListBoxItem) {
                ListBoxItem draggedItem = sender as ListBoxItem;
                var delButton = FindVisualChild<Button>(draggedItem);
                if (delButton.IsMouseOver) {
                    var item = (FileName)draggedItem.DataContext;
                    MatchFileNamesViewModel.MasterSelectedFiles.Remove(item);
                } else {
                    DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                    draggedItem.IsSelected = true;
                }
            }
        }

        private void TestPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (sender is ListBoxItem) {
                ListBoxItem draggedItem = sender as ListBoxItem;
                var delButton = FindVisualChild<Button>(draggedItem);
                if (delButton.IsMouseOver) {
                    var item = (FileName)draggedItem.DataContext;
                    MatchFileNamesViewModel.TestSelectedFiles.Remove(item);
                } else {
                    DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                    draggedItem.IsSelected = true;
                }
            }
        }

        private void OnOKButtonClick(object senderIn, RoutedEventArgs eIn) {
            FilesMatched?.Invoke(senderIn, eIn);
            this.Close();
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj)
    where childItem : DependencyObject {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++) {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
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
