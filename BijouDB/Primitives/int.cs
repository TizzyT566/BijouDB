#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.Primitives;

public struct @int : IDataType
{
    public static long Length => 4;

    private int _value;

    private @int(int value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[4];
        if (stream.TryFill(bytes)) _value = BitConverter.ToInt32(bytes, 0);
        else throw new CorruptedException<@int>();
    }

    public void Serialize(Stream stream)
    {
        byte[] bytes = BitConverter.GetBytes(_value);
        stream.Write(bytes);
    }

    public static implicit operator int(@int value) => value._value;
    public static implicit operator @int(int value) => new(value);
}
