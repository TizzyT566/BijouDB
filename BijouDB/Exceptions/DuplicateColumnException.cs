using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BijouDB.Exceptions
{
    public class DuplicateColumnException : Exception
    {
        public DuplicateColumnException(string columnName) : base($"Column {columnName} already exists.") { }
    }
}
