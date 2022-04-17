#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.Primitives;

public struct @byte : IDataType
{
    public static long Length => 1;

    private byte _value;

    private @byte(byte value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[1];
        if (stream.TryFill(bytes)) _value = bytes[0];
        else throw new CorruptedException<@byte>();
    }

    public void Serialize(Stream stream) => stream.WriteByte(_value);

    public static implicit operator byte(@byte value) => value._value;
    public static implicit operator @byte(byte value) => new(value);
}
