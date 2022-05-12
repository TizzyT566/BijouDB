using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BijouDB.Columns;

public class Column<D> : IColumn<D> where D : IDataType
{
    public D Get<T>(T record) where T : Record
    {
        throw new NotImplementedException();
    }

    public void Set<T>(T record, D value) where T : Record
    {
        throw new NotImplementedException();
    }
}
