using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models.Interfaces {
    public interface IFileReader {
        IEnumerable<string> ReadFile(string filePath, int skipRecords, Encoding encoding);
        IEnumerable<string> ReadFewLines(string filePath, int rowsToTake, Encoding encoding);
    }
}
