using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class PerformanceCounter {
        private Stopwatch Watch;
        private double MemoryBeforeStart { get; set; }
        private double MemoryAfterEnd { get; set; }
        private long ElapsedTimeMs { get; set; }
        private List<string> AllResults { get; set; }
        private const char Delimiter = ';';

        public PerformanceCounter() {
            AllResults = new List<string>();
            AllResults.Add(string.Join(Delimiter.ToString(), "TaskName", "ElapsedTimeMs", "AllocatedMemoryMb", "Gen0", "Gen1", "Gen2"));
        }

        public void Start() {
            Watch = new Stopwatch();
            Watch.Start();
            //MemoryBeforeStart = ConvertBytesToMegaBytes(GC.GetTotalMemory(false));
        }

        public void Stop(string taskName) {
            Watch.Stop();
            ElapsedTimeMs = Watch.ElapsedMilliseconds;
            AllResults.Add(string.Join(Delimiter.ToString(), taskName, ElapsedTimeMs));
        }

        public double ConvertBytesToMegaBytes(long bytes) {
            return Math.Round((bytes / 1024f) / 1024f, 2);
        }

        public void SaveAllResults() {
            File.WriteAllLines(@"C:\Users\MSBZ\Desktop\ExecutionTime.txt", AllResults);
        }

        private string GarbageCollectorRunCount() {
            MemoryAfterEnd = ConvertBytesToMegaBytes(GC.GetTotalMemory(false));
            return string.Join(Delimiter.ToString(),
                MemoryAfterEnd - MemoryBeforeStart,
                GC.CollectionCount(0).ToString("N0"),
                GC.CollectionCount(1).ToString("N0"),
                GC.CollectionCount(2).ToString("N0"));
        }
    }
}
