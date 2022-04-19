#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @long : IDataType
{
    public static long Length => 8;

    private long _value = default;

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



    // Nullable
    public sealed class nullable : IDataType
    {
        public static long Length => 8;

        private long _value = default;
        private nullable(long value) => _value = value;

        public nullable() { }

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

        public static implicit operator long(nullable value) => value._value;
        public static implicit operator nullable(long value) => new(value);
    }
}
