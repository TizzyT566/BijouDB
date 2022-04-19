using BijouDB.DataTypes;

namespace BijouDB.Exceptions;

public class RecordMissingException<D> : Exception where D : IDataType
{
    public RecordMissingException() : base($"{nameof(D)} data is missing.") { }
}
