namespace BijouDB;

public interface IColumn<D> where D : IDataType
{
    public D Get<R>(R record) where R : Record;
    public void Set<R>(R record, D value) where R : Record;
}
