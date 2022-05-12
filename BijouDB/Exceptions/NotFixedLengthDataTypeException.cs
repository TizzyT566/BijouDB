namespace BijouDB.Exceptions;

public class NotFixedLengthDataTypeException<D> : Exception where D : IDataType
{
    public NotFixedLengthDataTypeException() : base($"{nameof(D)} is not a fixed length data type.") { }
}
