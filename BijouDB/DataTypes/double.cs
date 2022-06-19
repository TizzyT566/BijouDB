#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public class @double : IDataType
{
    private double _value;

    private @double(double value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[8];
        if (stream.TryFill(bytes)) _value = BitConverter.ToDouble(bytes, 0);
        else throw new CorruptedException<@double>();
    }

    public void Serialize(Stream stream) =>
        stream.Write(BitConverter.GetBytes(_value), 0, 8);

    public static implicit operator double(@double value) => value._value;
    public static implicit operator @double(double value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private double? _value;

        private nullable(double? value) => _value = value;

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
                        if (stream.TryFill(bytes)) _value = BitConverter.ToDouble(bytes, 0);
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
                stream.Write(BitConverter.GetBytes((float)_value), 0, 8);
            }
        }

        public static implicit operator double?(nullable value) => value._value;
        public static implicit operator nullable(double? value) => new(value);
    }
}
