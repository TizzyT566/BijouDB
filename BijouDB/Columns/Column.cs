namespace BijouDB.Columns;

public class Column<D> : IColumn<D> where D : IDataType
{
    public D Get<R>(R record) where R : Record
    {
        throw new NotImplementedException();
    }

    public void Set<R>(R record, D value) where R : Record
    {
        throw new NotImplementedException();
    }
}
