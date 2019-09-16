using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reflection.Models;

namespace Reflection.Test {
    [TestClass]
    public class ComProcUnitTest {
        public void Run(List<string> master, List<string> test, string[,] expectedResult) {
            ComparisonProcessor cp = new ComparisonProcessor();
            FileReader fr = new FileReader();
            var delimiter = new char[] { ';' };
            var mConfig = new ImportConfiguration("c:\\data", delimiter, 0, false, Encoding.Default, new List<int>(), new List<int>(), new List<int>(), 3);
            var tConfig = new ImportConfiguration("c:\\data", delimiter, 0, false, Encoding.Default, new List<int>(), new List<int>(), new List<int>(), 3);
            ComparisonTask ct = new ComparisonTask(0, mConfig, tConfig);
            ct.IsDeviationsOnly = true;
            cp.PrepareData(master, test, ct);
            var compTab = cp.Process(cp.MasterTable, cp.TestTable, ct);
            string[,] actualResult = null;         
            if (compTab == null || (compTab.ComparedRowsCount == 0 && compTab.MasterExtraCount == 0 && compTab.TestExtraCount == 0)) {
                ct.MasterRowsCount = master.Count;
                ct.TestRowsCount = test.Count;
                cp.SetComparisonKeysForPassed(master, test, ct);
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
        public void BasicTest() {
            var master = new List<string>();
            master.Add("S1;P1;100");
            master.Add("S1;P1;101");
            master.Add("S1;P1;108");
            var test = new List<string>();
            test.Add("S1;P1;101");
            test.Add("S1;P1;107");
            test.Add("S1;P1;102");
            var expectedResult = new string[3, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "Column1";
            expectedResult[0, 4] = "Column2";
            expectedResult[0, 5] = "Column3";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "P1";
            expectedResult[1, 5] = "100 | 102";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "1";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "P1";
            expectedResult[2, 5] = "108 | 107";
            Run(master, test, expectedResult);
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
            expectedResult[1, 5] = "100 | 102";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "1";
            expectedResult[2, 3] = "P1";
            expectedResult[2, 4] = "S1";
            expectedResult[2, 5] = "108 | 107";
            expectedResult[3, 0] = null;
            expectedResult[3, 1] = "Extra from Master";
            expectedResult[3, 2] = "3";
            expectedResult[3, 3] = "P1";
            expectedResult[3, 4] = "S2";
            expectedResult[3, 5] = null;
            Run(master, test, expectedResult);
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
            expectedResult[1, 5] = "100 | 102";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "1";
            expectedResult[2, 3] = "P1";
            expectedResult[2, 4] = "S1";
            expectedResult[2, 5] = "108 | 107";
            expectedResult[3, 0] = null;
            expectedResult[3, 1] = "Extra from Test";
            expectedResult[3, 2] = "3";
            expectedResult[3, 3] = "P1";
            expectedResult[3, 4] = "S2";
            expectedResult[3, 5] = null;
            Run(master, test, expectedResult);
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
            var expectedResult = new string[4, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "Column1";
            expectedResult[0, 4] = "Column2";
            expectedResult[0, 5] = "Column3";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "P1";
            expectedResult[1, 5] = "100 | 101";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "1";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "P1"; 
            expectedResult[2, 5] = "108 | 102";
            expectedResult[3, 0] = "";
            expectedResult[3, 1] = "Deviations";
            expectedResult[3, 2] = "1";
            expectedResult[3, 3] = "S1";
            expectedResult[3, 4] = "P1";
            expectedResult[3, 5] = "108 | 107";
            Run(master, test, expectedResult);
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
            var expectedResult = new string[4, 6];
            expectedResult[0, 0] = "Defect No/Explanation";
            expectedResult[0, 1] = "Comparison Result";
            expectedResult[0, 2] = "Diff";
            expectedResult[0, 3] = "Column1";
            expectedResult[0, 4] = "Column2";
            expectedResult[0, 5] = "Column3";
            expectedResult[1, 0] = "";
            expectedResult[1, 1] = "Deviations";
            expectedResult[1, 2] = "1";
            expectedResult[1, 3] = "S1";
            expectedResult[1, 4] = "P1";
            expectedResult[1, 5] = "100 | 101";
            expectedResult[2, 0] = "";
            expectedResult[2, 1] = "Deviations";
            expectedResult[2, 2] = "1";
            expectedResult[2, 3] = "S1";
            expectedResult[2, 4] = "P1";
            expectedResult[2, 5] = "107 | 108";
            expectedResult[3, 0] = "";
            expectedResult[3, 1] = "Deviations";
            expectedResult[3, 2] = "1";
            expectedResult[3, 3] = "S1";
            expectedResult[3, 4] = "P1";
            expectedResult[3, 5] = "102 | 108";
            Run(master, test, expectedResult);
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
            var expectedResult = new string[4, 3];
            expectedResult[0, 0] = "Comparison Result";
            expectedResult[0, 1] = "Column1";
            expectedResult[0, 2] = "Column2";
            expectedResult[1, 0] = "Passed";
            expectedResult[1, 1] = "S1";
            expectedResult[1, 2] = "P1";
            expectedResult[2, 0] = "Passed";
            expectedResult[2, 1] = "S1";
            expectedResult[2, 2] = "P1";
            expectedResult[3, 0] = "Passed";
            expectedResult[3, 1] = "S1";
            expectedResult[3, 2] = "P1";
            Run(master, test, expectedResult);
        }

        [TestMethod]
        public void PassedDuplicatesTest() {
            var master = new List<string>();
            master.Add("S1;P1;100");
            master.Add("S1;P1;107");
            master.Add("S1;P2;102");
            master.Add("S1;P2;102");
            var test = new List<string>();
            test.Add("S1;P1;100");
            test.Add("S1;P1;107");
            test.Add("S1;P2;102");
            test.Add("S1;P2;102");
            var expectedResult = new string[5, 3];
            expectedResult[0, 0] = "Comparison Result";
            expectedResult[0, 1] = "Column2";
            expectedResult[0, 2] = "Column1";
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
            Run(master, test, expectedResult);
        }
    }
}
