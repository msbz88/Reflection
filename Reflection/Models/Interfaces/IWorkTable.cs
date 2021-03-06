﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models.Interfaces {
    public interface IWorkTable {
        string Name { get; }
        Row Headers { get; }
        List<Row> Rows { get; }
        int ColumnsCount { get; }
        int RowsCount { get; }
        char[] Delimiter { get; }
        //Task<Dictionary<int, HashSet<string>>> GetColumnsAsync();
        void LoadData(IEnumerable<string> data, char[] delimiter, bool isHeadersExist, ComparisonTask comparisonTask, List<MoveColumn> correctionColumns, int columnsCount);
        void SaveToFile(string filePath);
        void CleanUp();
    }
}
