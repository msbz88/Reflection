using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reflection.Models;

namespace Reflection.Test {
    [TestClass]
    public class RowsMatchUnitTest {
        [TestMethod]
        public void CrossDeviationTest() {
            ComparisonCore ComparisonCore = new ComparisonCore(null, null);

            var mRows = new List<Row>();
            mRows.Add(new Row(1, new string[] { "S1", "P1", "1000", "100004"}));
            mRows.Add(new Row(2, new string[] { "S1", "P1", "1002", "100005" }));

            var tRows = new List<Row>();
            tRows.Add(new Row(1, new string[] { "S1", "P1", "1000", "100006" }));
            tRows.Add(new Row(2, new string[] { "S1", "P1", "1001", "100005" }));

            var baseStat = ComparisonCore.GatherStatistics(mRows, tRows);
            RowsMatch RowsMatch = new RowsMatch(baseStat, null, null);

            var expectedResult = new List<ComparedRow>();
            var comparedRow1 = new ComparedRow(1, 2);
            comparedRow1.AddDeviation(new Deviation(3, "100004", "100006"));
            var comparedRow2 = new ComparedRow(2, 1);
            comparedRow2.AddDeviation(new Deviation(2, "1002", "1001"));

            expectedResult.Add(comparedRow1);
            expectedResult.Add(comparedRow2);

            var actualResult = RowsMatch.ProcessGroup(mRows, tRows, 0);

            CollectionAssert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void SortingTest() {
            ComparisonCore ComparisonCore = new ComparisonCore(null, null);

            var mRows = new List<Row>();
            mRows.Add(new Row(1, new string[] { "S1", "P1", "1005", "100004" }));
            mRows.Add(new Row(2, new string[] { "S1", "P1", "1007", "100004" }));

            var tRows = new List<Row>();
            tRows.Add(new Row(1, new string[] { "S1", "P1", "1008", "100004" }));
            tRows.Add(new Row(2, new string[] { "S1", "P1", "1002", "100004" }));
            tRows.Add(new Row(3, new string[] { "S1", "P1", "1006", "100004" }));

            var baseStat = ComparisonCore.GatherStatistics(mRows, tRows);
            RowsMatch RowsMatch = new RowsMatch(baseStat, null, null);

            var expectedResult = new List<ComparedRow>();
            var comparedRow1 = new ComparedRow(1, 3);
            comparedRow1.AddDeviation(new Deviation(2, "1005", "1006"));
            var comparedRow2 = new ComparedRow(2, 1);
            comparedRow2.AddDeviation(new Deviation(2, "1007", "1008"));

            expectedResult.Add(comparedRow1);
            expectedResult.Add(comparedRow2);

            var actualResult = RowsMatch.ProcessGroup(mRows, tRows, 0);

            CollectionAssert.AreEqual(expectedResult, actualResult);
        }
    }
}
