using System;
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
        string Delimiter { get; }
        void SetGroupId(List<int> pivotKeys);
        //Task<Dictionary<int, HashSet<string>>> GetColumnsAsync();
        void LoadData(IEnumerable<string> data, string delimiter, bool isHeadersExist);
        void SaveToFile(string filePath);
    }
}
