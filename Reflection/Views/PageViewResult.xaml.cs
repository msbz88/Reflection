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
using Reflection.ViewModels;

namespace Reflection.Views {
    /// <summary>
    /// Interaction logic for PageViewResult.xaml
    /// </summary>
    public partial class PageViewResult : Page {
        public EventHandler GoBack { get; set; }
        ComparisonResultViewModel ComparisonResultViewModel { get; set; }
        public EventHandler Error { get; set; }

        public PageViewResult() {
            InitializeComponent();
            ComparisonResultViewModel = new ComparisonResultViewModel();
        }

        private void ButtonGoBackClick(object senderIn, RoutedEventArgs eIn) {
            GoBack?.Invoke(senderIn, eIn);
        }

        public async void PrintFileContent(string path, string delimiter, Encoding encoding) {
            dgData.Columns.Clear();
            try {
                await ComparisonResultViewModel.GetResult(path, delimiter, encoding);
            } catch (Exception ex) {
                Error?.Invoke(ex.Message, null);
                return;
            }
            int index = 0;
            foreach (var item in ComparisonResultViewModel.Headers) {
                var column = new DataGridTextColumn();
                column.Header = item;
                column.Binding = new Binding(string.Format("[{0}]", index));
                dgData.Columns.Add(column);
                index++;
            }
            dgData.DataContext = ComparisonResultViewModel.Content;
        }
    }
}
