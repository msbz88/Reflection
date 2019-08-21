using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class FileName {
        public string FilePath { get; set; }
        public string Name { get; private set; }

        public FileName(string filePath) {
            FilePath = filePath;
            Name = Path.GetFileName(FilePath);
        }
    }
}
