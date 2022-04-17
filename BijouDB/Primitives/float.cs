#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.Primitives;

public struct @float : IDataType
{
    public static long Length => 4;

    private float _value;

    private @float(float value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[4];
        if (stream.TryFill(bytes)) _value = BitConverter.ToSingle(bytes, 0);
        else throw new CorruptedException<@float>();
    }

    public void Serialize(Stream stream)
    {
        byte[] bytes = BitConverter.GetBytes(_value);
        stream.Write(bytes);
    }

    public static implicit operator float(@float value) => value._value;
    public static implicit operator @float(float value) => new(value);
}
