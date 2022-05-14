#pragma warning disable IDE1006 // Naming Styles

using System.Numerics;
using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @bint : IDataType
{
    private BigInteger _value;

    private @bint(BigInteger value) => _value = value;

    public void Deserialize(Stream stream)
    {
        if (stream.TryReadDynamicData(out byte[] data)) _value = new(data);
        else throw new CorruptedException<@bint>();
    }

    public void Serialize(Stream stream) => stream.WriteDynamicData(_value.ToByteArray());

    public static implicit operator BigInteger(@bint value) => value._value;
    public static implicit operator @bint(BigInteger value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private BigInteger? _value;

        private nullable(BigInteger? value) => _value = value;

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
                        if (stream.TryReadDynamicData(out byte[] data)) _value = new(data);
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
                stream.WriteDynamicData(((BigInteger)_value).ToByteArray());
            }
        }

        public static implicit operator BigInteger?(nullable value) => value._value;
        public static implicit operator nullable(BigInteger? value) => new(value);
    }
}