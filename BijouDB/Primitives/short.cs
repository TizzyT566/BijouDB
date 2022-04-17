#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.Primitives;

public struct @short : IDataType
{
    public static long Length => 2;

    private short _value;

    private @short(short value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[2];
        if (stream.TryFill(bytes)) _value = BitConverter.ToInt16(bytes, 0);
        else throw new CorruptedException<@short>();
    }

    public void Serialize(Stream stream)
    {
        byte[] bytes = BitConverter.GetBytes(_value);
        stream.Write(bytes);
    }

    public static implicit operator short(@short value) => value._value;
    public static implicit operator @short(short value) => new(value);
}
