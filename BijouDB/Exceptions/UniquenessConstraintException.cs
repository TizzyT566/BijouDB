using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BijouDB.Exceptions
{
    internal class UniquenessConstraintException<D> : Exception where D : IDataType
    {
        public UniquenessConstraintException() : base($"Value already exists in a {typeof(D).FullName} column with the 'Unique' constraint.") { }
    }
}
