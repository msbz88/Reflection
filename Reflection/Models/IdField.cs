using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public class IdField {
        public int TransactionNo { get; set; } = -1;
        public string MasterTransactionNoVal { get; set; }
        public string TestTransactionNoVal { get; set; }
        public int MainKey { get; set; } = -1;
        public string MainVal { get; set; }
    }
}
