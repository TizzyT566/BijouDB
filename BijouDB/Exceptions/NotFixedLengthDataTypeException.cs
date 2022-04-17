namespace BijouDB.Exceptions;

public class NotFixedLengthDataTypeException<T> : Exception
{
    public NotFixedLengthDataTypeException() : base($"{nameof(T)} is not a fixed length data type.") { }
}
