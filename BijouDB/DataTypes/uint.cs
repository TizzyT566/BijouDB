#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB;

public struct @uint : IDataType
{
    private uint _value;

    private @uint(uint value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[4];
        if (stream.TryFill(bytes)) _value = BitConverter.ToUInt32(bytes, 0);
        else throw new CorruptedException<@uint>();
    }

    public void Serialize(Stream stream) =>
        stream.Write(BitConverter.GetBytes(_value), 0, 4);

    public static implicit operator uint(@uint value) => value._value;
    public static implicit operator @uint(uint value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private uint? _value;

        private nullable(uint? value) => _value = value;

        public nullable() => _value = null;

        public void Deserialize(Stream stream)
        {
            switch (stream.ReadByte())
            {
                case < 0:
                    {
                        throw new CorruptedException<nullable>();
                    }
                case 0:
                    {
                        _value = null;
                        break;
                    }
                default:
                    {
                        byte[] bytes = new byte[4];
                        if (stream.TryFill(bytes)) _value = BitConverter.ToUInt32(bytes, 0);
                        else throw new CorruptedException<nullable>();
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
                stream.Write(BitConverter.GetBytes((uint)_value), 0, 4);
            }
        }

        public static implicit operator uint?(nullable value) => value._value;
        public static implicit operator nullable(uint? value) => new(value);
    }
}
