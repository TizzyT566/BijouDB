#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @sbyte : IDataType
{
    public static long Length => 1;

    private sbyte _value = default;

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



    // Nullable
    public sealed class nullable : IDataType
    {
        public static long Length => 1;

        private sbyte _value = default;

        private nullable(sbyte value) => _value = value;

        public nullable() { }

        public void Deserialize(Stream stream)
        {
            byte[] bytes = new byte[1];
            if (stream.TryFill(bytes)) _value = (sbyte)bytes[0];
            else throw new CorruptedException<@sbyte>();
        }

        public void Serialize(Stream stream) => stream.WriteByte((byte)_value);

        public static implicit operator sbyte(nullable value) => value._value;
        public static implicit operator nullable(sbyte value) => new(value);
    }
}
