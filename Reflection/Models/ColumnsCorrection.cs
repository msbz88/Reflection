using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class ColumnsCorrection {
        List<string> MasterHeaders { get; set; }
        List<string> TestHeaders { get; set; }
        public List<MoveColumn> MasterCorrection { get; set; }
        public List<MoveColumn> TestCorrection { get; set; }
        int ColumnsCount;

        public ColumnsCorrection(List<string> masterHeaders, List<string> testHeaders) {
            MasterHeaders = masterHeaders;
            TestHeaders = testHeaders;
            ColumnsCount = MasterHeaders.Count >= TestHeaders.Count ? MasterHeaders.Count : TestHeaders.Count;
            MasterCorrection = new List<MoveColumn>();
            TestCorrection = new List<MoveColumn>();
        }

        public void AnalyseFileDimensions() {
            CheckHowHeadersAreDifferent();
            int innerColumnsCount = ColumnsCount;
            for (int i = 0; i < ColumnsCount; i++) {
                if (i >= TestHeaders.Count) {
                    var moveColumn = new MoveColumn(i, i);
                    TestCorrection.Add(moveColumn);
                } else if (i >= MasterHeaders.Count) {
                    var moveColumn = new MoveColumn(i, i);
                    MasterCorrection.Add(moveColumn);
                } else if (MasterHeaders[i] != TestHeaders[i]) {
                    int testIndex = TestHeaders.IndexOf(MasterHeaders[i], i);
                    int masterIndex = MasterHeaders.IndexOf(TestHeaders[i], i);
                    if (testIndex >= 0 && masterIndex < 0) {
                        if (TestHeaders[i] != "") {
                            TestCorrection.Add(new MoveColumn(i, innerColumnsCount));
                            TestCorrection.Add(new MoveColumn(testIndex, i));
                            MasterCorrection.Add(new MoveColumn(innerColumnsCount, innerColumnsCount));
                            TestHeaders.RemoveAt(testIndex);
                            TestHeaders.Insert(testIndex, "");
                            TestCorrection.Add(new MoveColumn(testIndex, testIndex));
                            innerColumnsCount++;
                        }
                    } else if (testIndex >= 0 && masterIndex >= 0) {
                        TestCorrection.Add(new MoveColumn(testIndex, i));
                        MasterCorrection.Add(new MoveColumn(i, masterIndex));
                    } else if (testIndex < 0 && masterIndex >= 0) {
                        if (MasterHeaders[i] != "") {
                            MasterCorrection.Add(new MoveColumn(i, innerColumnsCount));
                            MasterCorrection.Add(new MoveColumn(masterIndex, i));
                            TestCorrection.Add(new MoveColumn(innerColumnsCount, innerColumnsCount));
                            MasterHeaders.RemoveAt(testIndex);
                            MasterHeaders.Insert(testIndex, "");
                            MasterCorrection.Add(new MoveColumn(masterIndex, masterIndex));
                            innerColumnsCount++;
                        }
                    } else {
                        if (MasterHeaders[i] != "" && TestHeaders[i] != "") {
                            MasterCorrection.Add(new MoveColumn(i, innerColumnsCount++));
                            TestCorrection.Add(new MoveColumn(i, innerColumnsCount++));
                        }
                    }
                }
            }
        }

        private void CheckHowHeadersAreDifferent() {
            var matchedNames = MasterHeaders.Intersect(TestHeaders);
            var res = matchedNames.Count() / (double)ColumnsCount;
            if (res <= 0.80 && MasterHeaders.Count != TestHeaders.Count) {
                throw new Exception("Unable to match columns for comparison. Master and Test files have different numbers of columns, and the headers between them are very different.");
            }
        }


    }
}
