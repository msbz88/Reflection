using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reflection.Models;

namespace Reflection.Test {
    [TestClass]
    public class ComProcUnitTest {
        public void Run(List<string> master, List<string> test, string[,] expectedResult, bool isHeaderExists, int masterColCount, int testColCount) {
            ComparisonProcessor cp = new ComparisonProcessor();
            FileReader fr = new FileReader();
            var delimiter = new char[] { ';' };
            var mConfig = new ImportConfiguration("c:\\data", delimiter, 0, isHeaderExists, Encoding.Default, masterColCount);
            var tConfig = new ImportConfiguration("c:\\data", delimiter, 0, isHeaderExists, Encoding.Default, testColCount);
            ComparisonTask ct = new ComparisonTask(0, mConfig, tConfig);
            ct.IsDeviationsOnly = true;
            cp.PrepareData(master, test, ct);
            UserKeys userKeys = new UserKeys();
            var compTab = cp.Process(cp.MasterTable, cp.TestTable, master, test, ct, userKeys);
            string[,] actualResult = null;         
            if (compTab == null || (compTab.ComparedRowsCount == 0 && compTab.MasterExtraCount == 0 && compTab.TestExtraCount == 0)) {
                ct.MasterRowsCount = isHeaderExists? master.Count -1 : master.Count;
                ct.TestRowsCount = isHeaderExists ? test.Count -1 : test.Count;
                ComparisonCore cc = new ComparisonCore(ct);
                var numberedHeaders = Helpers.NumerateSequence(cp.FindHeaders(master.FirstOrDefault(), ct.MasterConfiguration.IsHeadersExist, ct.MasterConfiguration.Delimiter));
                cp.SetComparisonKeys(cc, ct, master, test, userKeys, numberedHeaders);
                compTab = new CompareTable(cp.MasterTable.Headers, cp.TestTable.Headers, ct);
                actualResult = compTab.GetPassedForExcel(master, test);
            } else {
                compTab.SetIdColumns();
                int rowsCount = 1;
                if (!ct.IsDeviationsOnly) {
                    rowsCount += ct.ExceptedRecords + compTab.PassedRows.Count;
                }
                if (ct.IsLinearView) {
                    rowsCount += compTab.Data.Sum(row => row.Deviations.Count) + compTab.ExtraMaster.Count + compTab.ExtraTest.Count;
                } else {
                    rowsCount += compTab.ComparedRowsCount + compTab.ExtraMaster.Count + compTab.ExtraTest.Count;
                }
                actualResult = compTab.PrepareDataTabular(rowsCount);
            }
            CollectionAssert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void SortOneColTest() {
            var master = new List<string>();
            master.Add("S1;P1;100");
            master.Add("S1;P1;101");
            master.Add("S1;P1;108");
            var test = new List<string>();
            test.Add("S1;P1;101");
            test.Add("S1;P1;107");
            test.Add("S1;P1;102");
            var expectedResult = new string[3, 5];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "Column1";
            expectedResult[0, 4] = "Column3";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "108 | 107";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "1";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "100 | 102";
            Run(master, test, expectedResult, false, 3, 3);
        }

        [TestMethod]
        public void SortOneColNotObviousChoiceTest() {
            var master = new List<string>();
            master.Add("S1;P1;1");
            master.Add("S1;P1;2");
            master.Add("S1;P1;3");
            var test = new List<string>();
            test.Add("S1;P1;4");
            test.Add("S1;P1;2");
            test.Add("S1;P1;10");
            var expectedResult = new string[3, 5];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "Column1";
            expectedResult[0, 4] = "Column3";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "3 | 4";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "1";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "1 | 10";
            Run(master, test, expectedResult, false, 3, 3);
        }

        [TestMethod]
        public void SortMultiColTest() {
            var master = new List<string>();
            master.Add("S1;P1;100;1");
            master.Add("S1;P1;101;2");
            master.Add("S1;P1;102;3");
            var test = new List<string>();
            test.Add("S1;P1;100;1");
            test.Add("S1;P1;107;3");
            test.Add("S1;P1;107;2");
            var expectedResult = new string[3, 5];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "Column1";
            expectedResult[0, 4] = "Column3";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "102 | 107";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "1";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "101 | 107";
            Run(master, test, expectedResult, false, 4, 4);
        }

        [TestMethod]
        public void SortMultiColNotObviousChoiceTest() {
            var master = new List<string>();
            master.Add("S1;P1;100;1");
            master.Add("S1;P1;101;4");
            master.Add("S1;P1;102;8");
            var test = new List<string>();
            test.Add("S1;P1;100;1");
            test.Add("S1;P1;107;3");
            test.Add("S1;P1;107;5");
            var expectedResult = new string[3, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "Column1";
            expectedResult[0, 4] = "Column3";
            expectedResult[0, 5] = "Column4";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "2";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "102 | 107";
            expectedResult[1, 5] = "8 | 5";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "2";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "101 | 107";
            expectedResult[2, 5] = "4 | 3";
            Run(master, test, expectedResult, false, 4, 4);
        }

        [TestMethod]
        public void SortMultiColDoubleTest() {
            var master = new List<string>();
            master.Add("S1;P1;100.1;18");
            master.Add("S1;P1;100.1;18.1");
            master.Add("S1;P1;101.7;47");
            master.Add("S1;P1;107.4578;82");
            var test = new List<string>();
            test.Add("S1;P1;100.99;1");
            test.Add("S1;P1;107;47");
            test.Add("S1;P1;100.789;3");
            test.Add("S1;P1;100.789;5");
            var expectedResult = new string[5, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "Column1";
            expectedResult[0, 4] = "Column3";
            expectedResult[0, 5] = "Column4";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "101.7 | 107";
            expectedResult[1, 5] = "0";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "2";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "100.1 | 100.789";
            expectedResult[2, 5] = "18 | 5";
            expectedResult[3, 0] = "";
            expectedResult[3, 1] = "Deviations";
            expectedResult[3, 2] = "2";
            expectedResult[3, 3] = "S1";
            expectedResult[3, 4] = "100.1 | 100.789";
            expectedResult[3, 5] = "18.1 | 3";
            expectedResult[4, 0] = "";
            expectedResult[4, 1] = "Deviations";
            expectedResult[4, 2] = "2";
            expectedResult[4, 3] = "S1";
            expectedResult[4, 4] = "107.4578 | 100.99";
            expectedResult[4, 5] = "82 | 1";
            Run(master, test, expectedResult, false, 4, 4);
        }

        [TestMethod]
        public void SortMultiColOfDiffTypeTest() {
            var master = new List<string>();
            master.Add("S1;P1;100;B");
            master.Add("S1;P1;100;1a");
            master.Add("S1;P1;101;b");
            master.Add("S1;P1;102;c");
            var test = new List<string>();
            test.Add("S1;P1;100.01;a");
            test.Add("S1;P1;100.01;1b");
            test.Add("S1;P1;107;2b");
            test.Add("S1;P1;107;bb");
            var expectedResult = new string[5, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "Column1";
            expectedResult[0, 4] = "Column3";
            expectedResult[0, 5] = "Column4";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "2";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "100 | 100.01";
            expectedResult[1, 5] = "B | a";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "2";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "100 | 100.01";
            expectedResult[2, 5] = "1a | 1b";
            expectedResult[3, 0] = "";
            expectedResult[3, 1] = "Deviations";
            expectedResult[3, 2] = "2";
            expectedResult[3, 3] = "S1";
            expectedResult[3, 4] = "102 | 107";
            expectedResult[3, 5] = "c | 2b";
            expectedResult[4, 0] = "";
            expectedResult[4, 1] = "Deviations";
            expectedResult[4, 2] = "2";
            expectedResult[4, 3] = "S1";
            expectedResult[4, 4] = "101 | 107";
            expectedResult[4, 5] = "b | bb";
            Run(master, test, expectedResult, false, 4, 4);
        }

        [TestMethod]
        public void MasterExtraTest() {
            var master = new List<string>();
            master.Add("S1;P1;100");
            master.Add("S1;P1;101");
            master.Add("S1;P1;108");
            master.Add("S2;P1;108");
            var test = new List<string>();
            test.Add("S1;P1;101");
            test.Add("S1;P1;107");
            test.Add("S1;P1;102");
            var expectedResult = new string[4, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "Column2";
            expectedResult[0, 4] = "Column1";
            expectedResult[0, 5] = "Column3";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "P1";
            expectedResult[1, 4] = "S1";
            expectedResult[1, 5] = "108 | 107";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "1";
            expectedResult[2, 3] = "P1";
            expectedResult[2, 4] = "S1";
            expectedResult[2, 5] = "100 | 102";
            expectedResult[3, 0] = null;
            expectedResult[3, 1] = "Extra from Master";
            expectedResult[3, 2] = "3";
            expectedResult[3, 3] = "P1";
            expectedResult[3, 4] = "S2";
            expectedResult[3, 5] = null;
            Run(master, test, expectedResult, false, 3, 3);
        }

        [TestMethod]
        public void TestExtraTest() {
            var master = new List<string>();
            master.Add("S1;P1;100");
            master.Add("S1;P1;101");
            master.Add("S1;P1;108");          
            var test = new List<string>();
            test.Add("S1;P1;101");
            test.Add("S1;P1;107");
            test.Add("S1;P1;102");
            test.Add("S2;P1;108");
            var expectedResult = new string[4, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "Column2";
            expectedResult[0, 4] = "Column1";
            expectedResult[0, 5] = "Column3";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "P1";
            expectedResult[1, 4] = "S1";
            expectedResult[1, 5] = "108 | 107";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "1";
            expectedResult[2, 3] = "P1";
            expectedResult[2, 4] = "S1";
            expectedResult[2, 5] = "100 | 102";
            expectedResult[3, 0] = null;
            expectedResult[3, 1] = "Extra from Test";
            expectedResult[3, 2] = "3";
            expectedResult[3, 3] = "P1";
            expectedResult[3, 4] = "S2";
            expectedResult[3, 5] = null;
            Run(master, test, expectedResult, false, 3, 3);
        }

        [TestMethod]
        public void MasterDuplicatesTest() {
            var master = new List<string>();
            master.Add("S1;P1;100");
            master.Add("S1;P1;108");
            master.Add("S1;P1;108");
            var test = new List<string>();
            test.Add("S1;P1;101");
            test.Add("S1;P1;107");
            test.Add("S1;P1;102");
            var expectedResult = new string[4, 5];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "Column1";
            expectedResult[0, 4] = "Column3";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "100 | 101";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "1";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "108 | 107";
            expectedResult[3, 0] = "";
            expectedResult[3, 1] = "Deviations";
            expectedResult[3, 2] = "1";
            expectedResult[3, 3] = "S1";
            expectedResult[3, 4] = "108 | 102";
            Run(master, test, expectedResult, false, 3, 3);
        }

        [TestMethod]
        public void TestDuplicatesTest() {
            var master = new List<string>();
            master.Add("S1;P1;100");
            master.Add("S1;P1;107");
            master.Add("S1;P1;102");
            var test = new List<string>();
            test.Add("S1;P1;101");
            test.Add("S1;P1;108");
            test.Add("S1;P1;108");
            var expectedResult = new string[4, 5];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "Column1";
            expectedResult[0, 4] = "Column3";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "100 | 101";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "1";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "107 | 108";
            expectedResult[3, 0] = "";
            expectedResult[3, 1] = "Deviations";
            expectedResult[3, 2] = "1";
            expectedResult[3, 3] = "S1";
            expectedResult[3, 4] = "102 | 108";
            Run(master, test, expectedResult, false, 3, 3);
        }

        [TestMethod]
        public void PassedTest() {
            var master = new List<string>();
            master.Add("S1;P1;100");
            master.Add("S1;P1;107");
            master.Add("S1;P1;102");
            var test = new List<string>();
            test.Add("S1;P1;100");
            test.Add("S1;P1;107");
            test.Add("S1;P1;102");
            var expectedResult = new string[4, 2];
            expectedResult[0, 0] = "Comparison Result";
            expectedResult[0, 1] = "Column1";
            expectedResult[1, 0] = "Passed";
            expectedResult[1, 1] = "S1";
            expectedResult[2, 0] = "Passed";
            expectedResult[2, 1] = "S1";
            expectedResult[3, 0] = "Passed";
            expectedResult[3, 1] = "S1";
            Run(master, test, expectedResult, false, 3, 3);
        }

        [TestMethod]
        public void PassedDuplicatesTest() {
            var master = new List<string>();
            master.Add("SecId;Port;Bal");
            master.Add("S1;P1;100");
            master.Add("S1;P1;107");
            master.Add("S1;P2;102");
            master.Add("S1;P2;102");
            var test = new List<string>();
            test.Add("SecId;Port;Bal");
            test.Add("S1;P1;100");
            test.Add("S1;P1;107");
            test.Add("S1;P2;102");
            test.Add("S1;P2;102");
            var expectedResult = new string[5, 3];
            expectedResult[0, 0] = "Comparison Result";
            expectedResult[0, 1] = "Port";
            expectedResult[0, 2] = "SecId";
            expectedResult[1, 0] = "Passed";
            expectedResult[1, 1] = "P1";
            expectedResult[1, 2] = "S1";
            expectedResult[2, 0] = "Passed";
            expectedResult[2, 1] = "P1";
            expectedResult[2, 2] = "S1";
            expectedResult[3, 0] = "Passed";
            expectedResult[3, 1] = "P2";
            expectedResult[3, 2] = "S1";
            expectedResult[4, 0] = "Passed";
            expectedResult[4, 1] = "P2";
            expectedResult[4, 2] = "S1";
            Run(master, test, expectedResult, true, 3, 3);
        }

        [TestMethod]
        public void MasterDiffColumnsAtTheEndTest() {
            var master = new List<string>();
            master.Add("SecId;Port;Bal;RowNum");
            master.Add("S1;P1;100;1");
            master.Add("S1;P1;107;2");
            master.Add("S1;P1;102;3");
            var test = new List<string>();
            test.Add("SecId;Port;Bal");
            test.Add("S1;P1;100");
            test.Add("S1;P1;101");
            test.Add("S1;P1;108");
            var expectedResult = new string[4, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "SecId";
            expectedResult[0, 4] = "Bal";
            expectedResult[0, 5] = "(Master extra column) RowNum";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "0";
            expectedResult[1, 5] = "1 | ";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "2";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "107 | 108";
            expectedResult[2, 5] = "2 | ";
            expectedResult[3, 0] = "";
            expectedResult[3, 1] = "Deviations";
            expectedResult[3, 2] = "2";
            expectedResult[3, 3] = "S1";
            expectedResult[3, 4] = "102 | 101";
            expectedResult[3, 5] = "3 | ";
            Run(master, test, expectedResult, true, 4, 3);
        }

        [TestMethod]
        public void TestDiffColumnsAtTheEndTest() {
            var master = new List<string>();
            master.Add("SecId;Port;Bal");
            master.Add("S1;P1;100");
            master.Add("S1;P1;101");
            master.Add("S1;P1;108");
            var test = new List<string>();
            test.Add("SecId;Port;Bal;RowNum");
            test.Add("S1;P1;100;1");
            test.Add("S1;P1;107;2");
            test.Add("S1;P1;102;3");
            var expectedResult = new string[4, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "SecId";
            expectedResult[0, 4] = "Bal";
            expectedResult[0, 5] = "(Test extra column) RowNum";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "0";
            expectedResult[1, 5] = " | 1";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "2";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "101 | 102";
            expectedResult[2, 5] = " | 3";
            expectedResult[3, 0] = "";
            expectedResult[3, 1] = "Deviations";
            expectedResult[3, 2] = "2";
            expectedResult[3, 3] = "S1";
            expectedResult[3, 4] = "108 | 107";
            expectedResult[3, 5] = " | 2";
            Run(master, test, expectedResult, true, 3, 4);
        }

        [TestMethod]
        public void MasterDiffColumnsAtTheMidTest() {
            var master = new List<string>();
            master.Add("SecId;Port;RowNum;Bal");
            master.Add("S1;P1;1;100");
            master.Add("S1;P1;2;107");
            master.Add("S1;P1;3;102");
            var test = new List<string>();
            test.Add("SecId;Port;Bal");
            test.Add("S1;P1;100");
            test.Add("S1;P1;101");
            test.Add("S1;P1;108");
            var expectedResult = new string[4, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "SecId";
            expectedResult[0, 4] = "(Master extra column) RowNum";
            expectedResult[0, 5] = "Bal";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "1 | ";
            expectedResult[1, 5] = "0";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "2";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "2 | ";
            expectedResult[2, 5] = "107 | 108";
            expectedResult[3, 0] = "";
            expectedResult[3, 1] = "Deviations";
            expectedResult[3, 2] = "2";
            expectedResult[3, 3] = "S1";
            expectedResult[3, 4] = "3 | ";
            expectedResult[3, 5] = "102 | 101";
            Run(master, test, expectedResult, true, 4, 3);
        }

        [TestMethod]
        public void TestDiffColumnsAtTheMidTest() {
            var master = new List<string>();
            master.Add("SecId;Port;Bal");
            master.Add("S1;P1;100");
            master.Add("S1;P1;101");
            master.Add("S1;P1;108");
            var test = new List<string>();
            test.Add("SecId;Port;RowNum;Bal");
            test.Add("S1;P1;1;100");
            test.Add("S1;P1;2;107");
            test.Add("S1;P1;3;102");
            var expectedResult = new string[4, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "SecId";
            expectedResult[0, 4] = "(Test extra column) RowNum";
            expectedResult[0, 5] = "Bal";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = " | 1";
            expectedResult[1, 5] = "0";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "2";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = " | 3";
            expectedResult[2, 5] = "101 | 102";
            expectedResult[3, 0] = "";
            expectedResult[3, 1] = "Deviations";
            expectedResult[3, 2] = "2";
            expectedResult[3, 3] = "S1";
            expectedResult[3, 4] = " | 2";
            expectedResult[3, 5] = "108 | 107";
            Run(master, test, expectedResult, true, 3, 4);
        }

        [TestMethod]
        public void ManyGroupsTest() {
            var master = new List<string>();
            master.Add("SecId;Port;Bal");
            master.Add("S1;P1;100");
            master.Add("S1;P1;101");
            master.Add("S1;P1;108");
            master.Add("S2;P2;100");
            master.Add("S2;P1;101.16");
            master.Add("S1;P1;101");
            master.Add("S3;P1;104");
            master.Add("S2;P3;102");
            var test = new List<string>();
            test.Add("SecId;Port;Bal");
            test.Add("S3;P2;171");
            test.Add("S1;P1;10");
            test.Add("S2;P2;101");
            test.Add("S2;P1;101.1");
            test.Add("S2;P1;101.2");
            test.Add("S2;P1;101.2");
            test.Add("S1;P1;10");
            test.Add("S1;P1;102");
            test.Add("S2;P3;101");
            var expectedResult = new string[12, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "SecId";
            expectedResult[0, 4] = "Port";
            expectedResult[0, 5] = "Bal";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S2";
            expectedResult[1, 4] = "P2";
            expectedResult[1, 5] = "100 | 101";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "1";
            expectedResult[2, 3] = "S2";
            expectedResult[2, 4] = "P3";
            expectedResult[2, 5] = "102 | 101";
            expectedResult[3, 0] = "";
            expectedResult[3, 1] = "Deviations";
            expectedResult[3, 2] = "1";
            expectedResult[3, 3] = "S1";
            expectedResult[3, 4] = "P1";
            expectedResult[3, 5] = "101 | 102";
            expectedResult[4, 0] = "";
            expectedResult[4, 1] = "Deviations";
            expectedResult[4, 2] = "1";
            expectedResult[4, 3] = "S1";
            expectedResult[4, 4] = "P1";
            expectedResult[4, 5] = "100 | 10";
            expectedResult[5, 0] = "";
            expectedResult[5, 1] = "Deviations";
            expectedResult[5, 2] = "1";
            expectedResult[5, 3] = "S1";
            expectedResult[5, 4] = "P1";
            expectedResult[5, 5] = "101 | 10";
            expectedResult[6, 0] = "";
            expectedResult[6, 1] = "Deviations";
            expectedResult[6, 2] = "1";
            expectedResult[6, 3] = "S2";
            expectedResult[6, 4] = "P1";
            expectedResult[6, 5] = "101.16 | 101.2";
            expectedResult[7, 0] = null;
            expectedResult[7, 1] = "Extra from Master";
            expectedResult[7, 2] = "3";
            expectedResult[7, 3] = "S1";
            expectedResult[7, 4] = "P1";
            expectedResult[7, 5] = null;
            expectedResult[8, 0] = null;
            expectedResult[8, 1] = "Extra from Master";
            expectedResult[8, 2] = "3";
            expectedResult[8, 3] = "S3";
            expectedResult[8, 4] = "P1";
            expectedResult[8, 5] = null;
            expectedResult[9, 0] = null;
            expectedResult[9, 1] = "Extra from Test";
            expectedResult[9, 2] = "3";
            expectedResult[9, 3] = "S3";
            expectedResult[9, 4] = "P2";
            expectedResult[9, 5] = null;
            expectedResult[10, 0] = null;
            expectedResult[10, 1] = "Extra from Test";
            expectedResult[10, 2] = "3";
            expectedResult[10, 3] = "S2";
            expectedResult[10, 4] = "P1";
            expectedResult[10, 5] = null;
            expectedResult[11, 0] = null;
            expectedResult[11, 1] = "Extra from Test";
            expectedResult[11, 2] = "3";
            expectedResult[11, 3] = "S2";
            expectedResult[11, 4] = "P1";
            expectedResult[11, 5] = null;
            Run(master, test, expectedResult, true, 3, 3);
        }

        public void ExtraOnlyTest() {
            var master = new List<string>();
            master.Add("SecId;Port;Bal");
            master.Add("S1;P2;100");
            master.Add("S2;P1;101");
            master.Add("S1;P2;101");
            var test = new List<string>();
            test.Add("SecId;Port;Bal");
            test.Add("S1;P2;100");
            test.Add("S2;P1;101");
            test.Add("S2;P3;108");
            var expectedResult = new string[3, 5];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "SecId";
            expectedResult[0, 4] = "Port";
            expectedResult[1, 0] = null;
            expectedResult[1, 1] = "Extra from Master";
            expectedResult[1, 2] = "3";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "P2";
            expectedResult[2, 0] = null;
            expectedResult[2, 1] = "Extra from Test";
            expectedResult[2, 2] = "3";
            expectedResult[2, 3] = "S2";
            expectedResult[2, 4] = "P3";
            Run(master, test, expectedResult, true, 3, 3);
        }

        [TestMethod]
        public void TestSameDiffColumnsAtTheMidTest() {
            var master = new List<string>();
            master.Add("SecId;Port;Bal;RowNum");
            master.Add("S1;P1;100;1");
            master.Add("S1;P1;101;5");
            master.Add("S1;P1;108;4");
            var test = new List<string>();
            test.Add("SecId;Port;RowNum;Bal");
            test.Add("S1;P1;1;100");
            test.Add("S1;P1;2;107");
            test.Add("S1;P1;3;102");
            var expectedResult = new string[3, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "SecId";
            expectedResult[0, 4] = "Bal";
            expectedResult[0, 5] = "RowNum";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "2";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "101 | 102";
            expectedResult[1, 5] = "5 | 3";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "2";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "108 | 107";
            expectedResult[2, 5] = "4 | 2";
            Run(master, test, expectedResult, true, 4, 4);
        }


        [TestMethod]
        public void TestDiffNamesColumnsAtTheMidTest() {
            var master = new List<string>();
            master.Add("SecId;Port;Bal;RowId");
            master.Add("S1;P1;100;1");
            master.Add("S1;P1;101;5");
            master.Add("S1;P1;108;4");
            var test = new List<string>();
            test.Add("SecId;Port;RowNum;Bal");
            test.Add("S1;P1;1;100");
            test.Add("S1;P1;2;107");
            test.Add("S1;P1;3;102");
            var expectedResult = new string[4, 7];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "SecId";
            expectedResult[0, 4] = "Bal";
            expectedResult[0, 5] = "(Master extra column) RowId";
            expectedResult[0, 6] = "(Test extra column) RowNum";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "2";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "0";
            expectedResult[1, 5] = "1 | ";
            expectedResult[1, 6] = " | 1";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "3";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "101 | 102";
            expectedResult[2, 5] = "5 | ";
            expectedResult[2, 6] = " | 3";
            expectedResult[3, 0] = "";
            expectedResult[3, 1] = "Deviations";
            expectedResult[3, 2] = "3";
            expectedResult[3, 3] = "S1";
            expectedResult[3, 4] = "108 | 107";
            expectedResult[3, 5] = "4 | ";
            expectedResult[3, 6] = " | 2";
            Run(master, test, expectedResult, true, 4, 4);
        }

        [TestMethod]
        public void TestDiffColumnsAtTheBeginningTest() {
            var master = new List<string>();
            master.Add("SecId;Port;Bal");
            master.Add("S1;P1;100");
            master.Add("S1;P1;107");
            master.Add("S1;P1;102");
            var test = new List<string>();
            test.Add("RowNum;SecId;Port;Bal");
            test.Add(";S1;P1;100");
            test.Add(";S1;P1;101");
            test.Add(";S1;P1;108");
            var expectedResult = new string[4, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "SecId";
            expectedResult[0, 4] = "(Test extra column) RowNum";
            expectedResult[0, 5] = "Bal";           
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = " | ";
            expectedResult[1, 5] = "0";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "2";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = " | ";
            expectedResult[2, 5] = "107 | 108";           
            expectedResult[3, 0] = "";
            expectedResult[3, 1] = "Deviations";
            expectedResult[3, 2] = "2";
            expectedResult[3, 3] = "S1";
            expectedResult[3, 4] = " | ";
            expectedResult[3, 5] = "102 | 101";          
            Run(master, test, expectedResult, true, 3, 4);
        }

        [TestMethod]
        public void OneColumnFileTest() {
            var master = new List<string>();
            master.Add("SecId");
            master.Add("S1");
            master.Add("S1");
            master.Add("S3");
            var test = new List<string>();
            test.Add("SecId");
            test.Add("S1");
            test.Add("S2");
            test.Add("S1");
            var expectedResult = new string[3, 4];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "SecId";
            expectedResult[1, 0] = null;
            expectedResult[1, 1] = "Extra from Master";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S3";
            expectedResult[2, 0] = null;
            expectedResult[2, 1] = "Extra from Test";
            expectedResult[2, 2] = "1";
            expectedResult[2, 3] = "S2";
            Run(master, test, expectedResult, true, 1, 1);
        }
    }
}
