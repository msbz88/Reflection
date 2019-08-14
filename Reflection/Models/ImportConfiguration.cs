using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reflection.Models.Interfaces;

namespace Reflection.Models {
    public class ImportConfiguration : IImportConfiguration {
        public string FilePath { get; set; }
        public string Delimiter { get; set; }
        public int RowsToSkip { get; set; }
        public bool IsHeadersExist { get; set; }
        public Encoding Encoding { get; set; }
        public List<int> UserKeys { get; set; }
        public List<int> UserIdColumns { get; set; }
        public List<int> UserExcludeColumns { get; set; }

        public ImportConfiguration(string filePath, string delimiter, int rowsToSkip, bool isHeadersExist, Encoding encoding, List<int> userKeys, List<int> userIdColumns, List<int> userExcludeColumns) {
            FilePath = filePath;
            Delimiter = delimiter;
            RowsToSkip = rowsToSkip;
            IsHeadersExist = isHeadersExist;
            Encoding = encoding;
            UserKeys = userKeys;
            UserIdColumns = userIdColumns;
            UserExcludeColumns = userExcludeColumns;
        }
    }
}
