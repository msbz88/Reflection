using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class ColumnsCorrection {
        Dictionary<int, string> MasterHeaders { get; set; }
        Dictionary<int, string> TestHeaders { get; set; }
        public List<MoveColumn> MasterCorrection { get; set; }
        public List<MoveColumn> TestCorrection { get; set; }
        int MatchedHeadersNames;
        HeadersComparer HeadersComparer;

        int ColumnsCount;

        public ColumnsCorrection(string[] masterHeaders, string[] testHeaders) {
            MasterHeaders = Helpers.NumerateSequence(masterHeaders);
            TestHeaders = Helpers.NumerateSequence(testHeaders);
            ColumnsCount = MasterHeaders.Count >= TestHeaders.Count ? MasterHeaders.Count : TestHeaders.Count;
            MasterCorrection = new List<MoveColumn>();
            TestCorrection = new List<MoveColumn>();
            HeadersComparer = new HeadersComparer();
        }

        //public void Correct() {
        //    CheckHowHeadersAreDifferent();
        //    int innerColumnsCount = ColumnsCount;
        //    for (int i = 0; i < ColumnsCount; i++) {
        //        if (i >= TestHeaders.Count) {
        //            var moveColumn = new MoveColumn(i, i);
        //            TestCorrection.Add(moveColumn);
        //        } else if (i >= MasterHeaders.Count) {
        //            var moveColumn = new MoveColumn(i, i);
        //            MasterCorrection.Add(moveColumn);
        //        } else if (MasterHeaders[i] != TestHeaders[i]) {
        //            int testIndex = TestHeaders.IndexOf(MasterHeaders[i], i);
        //            int masterIndex = MasterHeaders.IndexOf(TestHeaders[i], i);
        //            if (testIndex >= 0 && masterIndex < 0) {
        //                if (TestHeaders[i] != "") {
        //                    TestCorrection.Add(new MoveColumn(i, innerColumnsCount));
        //                    TestCorrection.Add(new MoveColumn(testIndex, i));
        //                    MasterCorrection.Add(new MoveColumn(innerColumnsCount, innerColumnsCount));
        //                    TestHeaders.RemoveAt(testIndex);
        //                    TestHeaders.Insert(testIndex, "");
        //                    TestCorrection.Add(new MoveColumn(testIndex, testIndex));
        //                    innerColumnsCount++;
        //                }
        //            } else if (testIndex >= 0 && masterIndex >= 0) {
        //                TestCorrection.Add(new MoveColumn(testIndex, i));
        //                MasterCorrection.Add(new MoveColumn(i, masterIndex));
        //            } else if (testIndex < 0 && masterIndex >= 0) {
        //                if (MasterHeaders[i] != "") {
        //                    MasterCorrection.Add(new MoveColumn(i, innerColumnsCount));
        //                    MasterCorrection.Add(new MoveColumn(masterIndex, i));
        //                    TestCorrection.Add(new MoveColumn(innerColumnsCount, innerColumnsCount));
        //                    MasterHeaders.RemoveAt(masterIndex);
        //                    MasterHeaders.Insert(masterIndex, "");
        //                    MasterCorrection.Add(new MoveColumn(masterIndex, masterIndex));
        //                    innerColumnsCount++;
        //                }
        //            } else {
        //                if (MasterHeaders[i] != "" && TestHeaders[i] != "") {
        //                    MasterCorrection.Add(new MoveColumn(i, innerColumnsCount++));
        //                    TestCorrection.Add(new MoveColumn(i, innerColumnsCount++));
        //                }
        //            }
        //        }
        //    }
        //}

        public void Correct() {
            if (MasterHeaders.SequenceEqual(TestHeaders)) {
                return;
            }
            CheckHowHeadersAreDifferent();
            if(MasterHeaders.Count >= TestHeaders.Count) {
                TestCorrection = Move(MasterHeaders, TestHeaders);
                MasterCorrection = Extend(MasterHeaders);                
                if(MasterHeaders.Count > TestHeaders.Count && !TestCorrection.Any()) {
                    TestCorrection = Extend(TestHeaders);
                }
            } else {
                MasterCorrection = Move(TestHeaders, MasterHeaders);
                TestCorrection = Extend(TestHeaders);
                if (TestHeaders.Count > MasterHeaders.Count && !MasterCorrection.Any()) {
                    MasterCorrection = Extend(MasterHeaders);
                }
            }
        }

        private List<MoveColumn> Extend(Dictionary<int, string> headers) {
            List<MoveColumn> moveColumns = new List<MoveColumn>();
            var extend = ColumnsCount - headers.Count;
            for (int i = 0; i < extend; i++) {
                var moveColumn = new MoveColumn(headers.Count + i, headers.Count + i);
                moveColumns.Add(moveColumn);
            }
            return moveColumns;
        }

        private List<MoveColumn> Move(Dictionary<int, string> larger, Dictionary<int, string> smaller) {
            List<MoveColumn> moveColumns = new List<MoveColumn>();
            for (int i = 0; i < smaller.Count; i++) {
                for (int ii = 0; ii < larger.Count; ii++) {
                    if(smaller[i] == larger[ii] && i == ii) {
                        break;
                    }else if (smaller[i] == larger[ii] && i != ii) {
                        var moveColumn = new MoveColumn(i, ii);
                        moveColumns.Add(moveColumn);
                        break;
                    } else if (ii == ColumnsCount - 1) {
                        var moveColumn = new MoveColumn(i, ColumnsCount++);
                        moveColumns.Add(moveColumn);
                    }
                }
            }
            return moveColumns;
        }

        private void CheckHowHeadersAreDifferent() {
            if (!MasterHeaders.Any()) {
                throw new Exception("Unable to match columns for comparison.\nMaster and Test files have different numbers of columns, and Master file has no headers.");
            }
            if(!TestHeaders.Any()) {
                throw new Exception("Unable to match columns for comparison.\nMaster and Test files have different numbers of columns, and Test file has no headers.");
            }
            var matchedNames = MasterHeaders.Intersect(TestHeaders, HeadersComparer);
            var matchingRatio = matchedNames.Count() / (double)ColumnsCount;
            if (matchingRatio < 0.75) {
                throw new Exception("Unable to match columns for comparison.\nMaster and Test files have different numbers of columns, and the headers between them are very different.");
            }
        }



    }

    class HeadersComparer : IEqualityComparer<KeyValuePair<int, string>> {
        public bool Equals(KeyValuePair<int, string> x, KeyValuePair<int, string> y) {
            if (x.Value == y.Value) {
                return true;
            }else {
                return false;
            }
        }

        public int GetHashCode(KeyValuePair<int, string> obj) {
            return obj.Value.GetHashCode();
        }
    }
}
