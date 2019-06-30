using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reflection.Models.Interfaces;

namespace Reflection.Models {
    public class ImportConfiguration: IImportConfiguration {
        public string MasterFilePath { get; set; }
        public string TestFilePath { get; set; }
        public string Delimiter { get; set; }
        public int RowsToSkip { get; set; }
        public bool IsHeadersExist { get; set; }
        public Encoding Encoding { get; set; }
        public List<int> UserKeys { get; set; }

        public ImportConfiguration(string pathMasterFile, string pathTestFile, string delimiter, int rowsToSkip, bool isHeadersExist, Encoding encoding, List<int> userKeys) {
            MasterFilePath = pathMasterFile;
            TestFilePath = pathTestFile;
            Delimiter = delimiter;
            RowsToSkip = rowsToSkip;
            IsHeadersExist = isHeadersExist;
            Encoding = encoding;
            UserKeys = userKeys;
    }
    }
}
