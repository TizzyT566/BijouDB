#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.Primitives;

public struct @sbyte : IDataType
{
    public static long Length => 1;

    private sbyte _value;

    private @sbyte(sbyte value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[1];
        if (stream.TryFill(bytes)) _value = (sbyte)bytes[0];
        else throw new CorruptedException<@sbyte>();
    }

    public void Serialize(Stream stream) => stream.WriteByte((byte)_value);

    public static implicit operator sbyte(@sbyte value) => value._value;
    public static implicit operator @sbyte(sbyte value) => new(value);
}
