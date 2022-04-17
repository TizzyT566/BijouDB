#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.Primitives;

public struct @long : IDataType
{
    public static long Length => 8;

    private long _value;

    private @long(long value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[8];
        if (stream.TryFill(bytes)) _value = BitConverter.ToInt64(bytes, 0);
        else throw new CorruptedException<@long>();
    }

    public void Serialize(Stream stream)
    {
        byte[] bytes = BitConverter.GetBytes(_value);
        stream.Write(bytes);
    }

    public static implicit operator long(@long value) => value._value;
    public static implicit operator @long(long value) => new(value);
}
