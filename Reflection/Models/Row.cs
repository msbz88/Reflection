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
    }
}
