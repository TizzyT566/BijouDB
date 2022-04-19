#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @decimal : IDataType
{
    public static long Length => 16;

    private decimal _value = default;

    private @decimal(decimal value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[16];
        if (stream.TryFill(bytes))
        {
            int[] ints = new int[4];
            ints[0] = BitConverter.ToInt32(bytes, 0);
            ints[1] = BitConverter.ToInt32(bytes, 4);
            ints[2] = BitConverter.ToInt32(bytes, 8);
            ints[3] = BitConverter.ToInt32(bytes, 12);
            _value = new(ints);
        }
        else throw new CorruptedException<@decimal>();
    }

    public void Serialize(Stream stream)
    {
        int[] bits = decimal.GetBits(_value);
        foreach (int bit in bits)
        {
            byte[] bytes = BitConverter.GetBytes(bit);
            stream.Write(bytes);
        }
    }

    public static implicit operator decimal(@decimal value) => value._value;
    public static implicit operator @decimal(decimal value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        public static long Length => 16;

        private decimal _value = default;

        private nullable(decimal value) => _value = value;

        public nullable() { }

        public void Deserialize(Stream stream)
        {
            byte[] bytes = new byte[16];
            if (stream.TryFill(bytes))
            {
                int[] ints = new int[4];
                ints[0] = BitConverter.ToInt32(bytes, 0);
                ints[1] = BitConverter.ToInt32(bytes, 4);
                ints[2] = BitConverter.ToInt32(bytes, 8);
                ints[3] = BitConverter.ToInt32(bytes, 12);
                _value = new(ints);
            }
            else throw new CorruptedException<@decimal>();
        }

        public void Serialize(Stream stream)
        {
            int[] bits = decimal.GetBits(_value);
            foreach (int bit in bits)
            {
                byte[] bytes = BitConverter.GetBytes(bit);
                stream.Write(bytes);
            }
        }

        public static implicit operator decimal(nullable value) => value._value;
        public static implicit operator nullable(decimal value) => new(value);
    }
}
