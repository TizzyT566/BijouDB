using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BijouDB.Exceptions
{
    public class OperationStatus
    {
        public static readonly OperationStatus Success = new();
        public static implicit operator bool(OperationStatus status)
        {

        }
    }
}
