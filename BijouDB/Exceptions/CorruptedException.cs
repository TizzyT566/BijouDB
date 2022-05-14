namespace BijouDB.Exceptions;

public class CorruptedException<D> : Exception
    where D : IDataType
{
    public CorruptedException() : base($"{typeof(D).Name} data is missing or corrupted.") { }
}
