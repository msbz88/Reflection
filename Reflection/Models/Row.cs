using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class Row {
        public int Id { get; set; }
        public string[] Data;

        public string this[int index] {
            get { return Data[index]; }
            set { Data[index] = value; }
        }

        public Row(int id, int capacity) {
            Id = id;
            Data = new string[capacity];
        }

        public Row(int id, string[] dat) {
            Id = id;
            Data = new string[dat.Length];
            dat.CopyTo(Data, 0);
        }

        public void Fill(string[] dat) {
            dat.CopyTo(Data, 0);
        }

        public int GetValuesHashCode(List<int> positions) {
            var query = Data.Select((f, i) => new { f, i })
                .Where(x => positions.Contains(x.i))
                .Select(x => x.f);
            return GetHashCode(query);
        }

        public int GetHashCode(IEnumerable<string> collection) {
            if (collection != null) {
                unchecked {
                    int hash = 17;
                    foreach (var item in collection) {
                        hash = hash * 23 + ((item != null) ? item.GetHashCode() : 0);
                    }
                    return hash;
                }
            }
            return 0;
        }

        public List<string> ColumnIndexIn(IEnumerable<int> positions) {
            var query = Data.Select((val, index) => new { val, index })
                .Where(item => positions.Contains(item.index))
                .Select(item => item.val);
            return query.ToList();
        }

        //public override string ToString() {
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append(GroupId);
        //    sb.Append(Delimiter);
        //    sb.Append(Id);
        //    sb.Append(Delimiter);
        //    foreach (var item in Data) {
        //        sb.Append(item);
        //        sb.Append(Delimiter);
        //    }
        //    return sb.ToString().TrimEnd(Delimiter);
        //}

    }
}
