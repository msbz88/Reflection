using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reflection.Models {
    public enum Status {
        Queued,
        Executing,
        Passed,
        Failed,
        Error,
        Canceled
    }
}
