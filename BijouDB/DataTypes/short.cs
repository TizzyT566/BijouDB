#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @short : IDataType
{
    public static long Length => 2;

    private short _value = default;

    private @short(short value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[2];
        if (stream.TryFill(bytes)) _value = BitConverter.ToInt16(bytes, 0);
        else throw new CorruptedException<@short>();
    }

    public void Serialize(Stream stream)
    {
        byte[] bytes = BitConverter.GetBytes(_value);
        stream.Write(bytes);
    }

    public static implicit operator short(@short value) => value._value;
    public static implicit operator @short(short value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        public static long Length => @short.Length + 1;

        private short? _value;

        private nullable(short? value) => _value = value;

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
                        byte[] bytes = new byte[2];
                        if (stream.TryFill(bytes)) _value = BitConverter.ToInt16(bytes, 0);
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
                stream.Write(BitConverter.GetBytes((short)_value));
            }
        }

        public static implicit operator short?(nullable value) => value._value;
        public static implicit operator nullable(short? value) => new(value);
    }
}
