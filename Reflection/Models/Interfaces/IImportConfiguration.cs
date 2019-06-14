using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models.Interfaces {
    public interface IImportConfiguration {
        string MasterFilePath { get; set; }
        string TestFilePath { get; set; }
        string Delimiter { get; set; }
        int RowsToSkip { get; set; }
        bool IsHeadersExist { get; set; }
        Encoding Encoding { get; set; }
    }
}
