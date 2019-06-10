using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Model {
   public class MatchedFileNames {
        public string MasterFileName { get; private set; }
        public string TestFileName { get; private set; }
        public string MasterFilePath { get; private set; }
        public string TestFilePath { get; private set; }

        public MatchedFileNames(string masterFilePath, string testFilePath) {
            MasterFilePath = masterFilePath;
            TestFilePath = testFilePath;
            MasterFileName = Path.GetFileName(masterFilePath);
            TestFileName = Path.GetFileName(testFilePath);
        }
    }
}
