using BijouDB.Primitives;

namespace BijouDB.Exceptions;

public class CorruptedException<D> : Exception where D : IDataType
{
    public CorruptedException() : base($"{nameof(D)} data is corrupted.") { }
}
