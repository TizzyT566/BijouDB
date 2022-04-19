#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @ushort : IDataType
{
    public static long Length => 2;

    private ushort _value = default;

    private @ushort(ushort value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[2];
        if (stream.TryFill(bytes)) _value = BitConverter.ToUInt16(bytes, 0);
        else throw new CorruptedException<@ushort>();
    }

    public void Serialize(Stream stream)
    {
        byte[] bytes = BitConverter.GetBytes(_value);
        stream.Write(bytes);
    }

    public static implicit operator ushort(@ushort value) => value._value;
    public static implicit operator @ushort(ushort value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        public static long Length => 2;

        private ushort _value = default;

        private nullable(ushort value) => _value = value;

        public nullable() { }

        public void Deserialize(Stream stream)
        {
            byte[] bytes = new byte[2];
            if (stream.TryFill(bytes)) _value = BitConverter.ToUInt16(bytes, 0);
            else throw new CorruptedException<@ushort>();
        }

        public void Serialize(Stream stream)
        {
            byte[] bytes = BitConverter.GetBytes(_value);
            stream.Write(bytes);
        }

        public static implicit operator ushort(nullable value) => value._value;
        public static implicit operator nullable(ushort value) => new(value);
    }
}
