namespace BijouDB;

public interface IColumn<D> where D : IDataType
{
    public D Get<T>(T record) where T : Record;
    public void Set<T>(T record, D value) where T : Record;
}
