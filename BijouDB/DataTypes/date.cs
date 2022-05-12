#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @date : IDataType
{
    public static long Length => 8;

    private DateTime _value;

    private @date(DateTime value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[8];
        if (stream.TryFill(bytes)) _value = new DateTime(BitConverter.ToInt64(bytes, 0));
        else throw new CorruptedException<@date>();
    }

    public void Serialize(Stream stream)
    {
        byte[] bytes = BitConverter.GetBytes(_value.Ticks);
        stream.Write(bytes);
    }

    public override string ToString() => _value.ToString();

    public static implicit operator DateTime(@date value) => value._value;
    public static implicit operator @date(DateTime value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        public static long Length => date.Length + 1;

        private DateTime? _value;

        private nullable(DateTime? value) => _value = value;

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
                        if (stream.TryFill(bytes)) _value = new(BitConverter.ToInt64(bytes, 0));
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
                stream.Write(BitConverter.GetBytes(((DateTime)_value).Ticks));
            }
        }

        public override string ToString() => _value.ToString() ?? "";

        public static implicit operator DateTime?(nullable value) => value._value;
        public static implicit operator nullable(DateTime? value) => new(value);
    }
}