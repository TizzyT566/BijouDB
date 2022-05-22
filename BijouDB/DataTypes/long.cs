#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB;

public struct @long : IDataType
{
    private long _value;

    private @long(long value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[8];
        if (stream.TryFill(bytes)) _value = BitConverter.ToInt64(bytes, 0);
        else throw new CorruptedException<@long>();
    }

    public void Serialize(Stream stream) =>
        stream.Write(BitConverter.GetBytes(_value), 0, 8);

    public static implicit operator long(@long value) => value._value;
    public static implicit operator @long(long value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private long? _value;

        private nullable(long? value) => _value = value;

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
                        byte[] bytes = new byte[8];
                        if (stream.TryFill(bytes)) _value = BitConverter.ToInt64(bytes, 0);
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
                stream.Write(BitConverter.GetBytes((long)_value), 0, 8);
            }
        }

        public static implicit operator long?(nullable value) => value._value;
        public static implicit operator nullable(long? value) => new(value);
    }
}
