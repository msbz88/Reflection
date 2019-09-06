﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reflection.Models.Interfaces;

namespace Reflection.Models {
    public class ImportConfiguration : IImportConfiguration, IEquatable<ImportConfiguration> {
        public string FilePath { get; set; }
        public char[] Delimiter { get; set; }
        public int RowsToSkip { get; set; }
        public bool IsHeadersExist { get; set; }
        public Encoding Encoding { get; set; }
        public List<int> UserKeys { get; set; }
        public List<int> UserIdColumns { get; set; }
        public List<int> UserExcludeColumns { get; set; }
        public int ColumnsCount { get; set; }

        public ImportConfiguration(string filePath, char[] delimiter, int rowsToSkip, bool isHeadersExist, Encoding encoding, List<int> userKeys, List<int> userIdColumns, List<int> userExcludeColumns, int columnsCount) {
            FilePath = filePath;
            Delimiter = delimiter;
            RowsToSkip = rowsToSkip;
            IsHeadersExist = isHeadersExist;
            Encoding = encoding;
            UserKeys = userKeys;
            UserIdColumns = userIdColumns;
            UserExcludeColumns = userExcludeColumns;
            ColumnsCount = columnsCount;
        }

        public void EqualizeTo(ImportConfiguration other) {
            Delimiter = other.Delimiter;
            RowsToSkip = other.RowsToSkip;
            IsHeadersExist = other.IsHeadersExist;
            Encoding = other.Encoding;
        }

        public bool Equals(ImportConfiguration other) {
            if (string.Join("", Delimiter) == string.Join("", other.Delimiter) &&
            RowsToSkip == other.RowsToSkip &&
            IsHeadersExist == other.IsHeadersExist &&
            Encoding == other.Encoding) {
                return true;
            }else {
                return false;
            }
        }
    }
}
