#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB;

public struct @time : IDataType
{
    private DateTime _value;

    private @time(DateTime value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[8];
        if (stream.TryFill(bytes)) _value = new DateTime(BitConverter.ToInt64(bytes, 0));
        else throw new CorruptedException<@time>();
    }

    public void Serialize(Stream stream) =>
        stream.Write(BitConverter.GetBytes(_value.Ticks), 0, 8);

    public static implicit operator DateTime(@time value) => value._value;
    public static implicit operator @time(DateTime value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
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
                stream.Write(BitConverter.GetBytes(((DateTime)_value).Ticks), 0, 8);
            }
        }

        public static implicit operator DateTime?(nullable value) => value._value;
        public static implicit operator nullable(DateTime? value) => new(value);
    }
}