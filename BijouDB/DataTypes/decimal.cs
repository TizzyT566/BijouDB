#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB;

public struct @decimal : IDataType
{
    private decimal _value;

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
        else throw new CorruptedException<@decimal>().Log();
    }

    public void Serialize(Stream stream)
    {
        int[] bits = decimal.GetBits(_value);
        foreach (int bit in bits) stream.Write(BitConverter.GetBytes(bit), 0, 4);
    }

    public static implicit operator decimal(@decimal value) => value._value;
    public static implicit operator @decimal(decimal value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private decimal? _value;

        private nullable(decimal? value) => _value = value;

        public nullable() => _value = null;

        public void Deserialize(Stream stream)
        {
            switch (stream.ReadByte())
            {
                case < 0:
                    {
                        throw new CorruptedException<nullable>().Log();
                    }
                case 0:
                    {
                        _value = null;
                        break;
                    }
                default:
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
                        else throw new CorruptedException<nullable>().Log();
                        break;
                    }
            }
        }

        public void Serialize(Stream stream)
        {
            if (_value is null)
            {
                stream.WriteByte(byte.MinValue);
            }
            else
            {
                stream.WriteByte(byte.MaxValue);
                int[] bits = decimal.GetBits((decimal)_value);
                foreach (int bit in bits) stream.Write(BitConverter.GetBytes(bit), 0, 4);
            }
        }

        public static implicit operator decimal?(nullable value) => value?._value;
        public static implicit operator nullable(decimal? value) => new(value);
    }
}
