#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.Primitives;

public struct table<T> : IDataType where T : Table, new()
{
    public static long Length => 16;

    private Guid _value;

    private table(Guid id) => _value = id;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[16];
        if (stream.TryFill(bytes)) _value = new Guid(bytes);
        else throw new CorruptedException<table<T>>();
    }

    public void Serialize(Stream stream) => stream.Write(_value.ToByteArray());

    public static implicit operator T(table<T> value) => Table.Load<T>(value._value);
    public static implicit operator table<T>(T value) => new(value.Id);
}
