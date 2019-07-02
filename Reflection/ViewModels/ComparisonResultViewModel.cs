using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reflection.Models;

namespace Reflection.ViewModels {
    public class ComparisonResultViewModel {
        public List<string[]> Content { get; set; }
        public List<string> Headers { get; set; }
        FileReader FileReader { get; set; }

        public ComparisonResultViewModel() {
            FileReader = new FileReader();
        }

        public async Task GetResult(string path, string delimiter, Encoding encoding) {
            var headers = FileReader.ReadFewLines(path, 1, encoding).FirstOrDefault().Split(new string[] { delimiter }, StringSplitOptions.None);
            Headers = headers.ToList();
            var content = await Task.Run(()=> FileReader.ReadFile(path, 1, encoding).Select(line => line.Split(new string[] { delimiter }, StringSplitOptions.None)).ToList());
            Content = content;
        }
    }
}
