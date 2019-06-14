using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reflection.Models.Interfaces;

namespace Reflection.Models {
    public class FileReader: IFileReader {
        public IEnumerable<string> ReadFile(string filePath, int rowsToSkip, Encoding encoding) {
            return File.ReadLines(filePath, encoding).Skip(rowsToSkip);
        }

        public IEnumerable<string> ReadFewLines(string filePath, int rowsToTake, Encoding encoding) {
            return File.ReadLines(filePath, encoding).Take(rowsToTake);
        }
    }
}
