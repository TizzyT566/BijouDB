#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @uint : IDataType
{
    public static long Length => 4;

    private uint _value = default;

    private @uint(uint value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[4];
        if (stream.TryFill(bytes)) _value = BitConverter.ToUInt32(bytes, 0);
        else throw new CorruptedException<@uint>();
    }

    public void Serialize(Stream stream)
    {
        byte[] bytes = BitConverter.GetBytes(_value);
        stream.Write(bytes);
    }

    public static implicit operator uint(@uint value) => value._value;
    public static implicit operator @uint(uint value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        public static long Length => 4;

        private uint _value = default;

        private nullable(uint value) => _value = value;

        public nullable() { }

        public void Deserialize(Stream stream)
        {
            byte[] bytes = new byte[4];
            if (stream.TryFill(bytes)) _value = BitConverter.ToUInt32(bytes, 0);
            else throw new CorruptedException<@uint>();
        }

        public void Serialize(Stream stream)
        {
            byte[] bytes = BitConverter.GetBytes(_value);
            stream.Write(bytes);
        }

        public static implicit operator uint(nullable value) => value._value;
        public static implicit operator nullable(uint value) => new(value);
    }
}
