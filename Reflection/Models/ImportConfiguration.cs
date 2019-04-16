using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class ImportConfiguration {
        public string PathMasterFile { get; set; }
        public string PathTestFile { get; set; }
        public string Delimiter { get; set; }
        public int RowsToSkip { get; set; }
        public bool IsHeadersExist { get; set; }
        public Encoding Encoding { get; set; }

        public ImportConfiguration(string pathMasterFile, string pathTestFile, string delimiter, int rowsToSkip, bool isHeadersExist, Encoding encoding) {
            PathMasterFile = pathMasterFile;
            PathTestFile = pathTestFile;
            Delimiter = delimiter;
            RowsToSkip = rowsToSkip;
            IsHeadersExist = isHeadersExist;
            Encoding = encoding;
        }
    }
}
