using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class MoveColumn {
        public int From;
        public int To;

        public MoveColumn(int from, int to) {
            From = from;
            To = to;
        }
    }
}
