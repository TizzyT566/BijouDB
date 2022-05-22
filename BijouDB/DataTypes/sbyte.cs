#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB;

public struct @sbyte : IDataType
{
    private sbyte _value;

    private @sbyte(sbyte value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[1];
        if (stream.TryFill(bytes)) _value = (sbyte)bytes[0];
        else throw new CorruptedException<@sbyte>();
    }

    public void Serialize(Stream stream) => stream.WriteByte((byte)_value);

    public static implicit operator sbyte(@sbyte value) => value._value;
    public static implicit operator @sbyte(sbyte value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private sbyte? _value;

        private nullable(sbyte? value) => _value = value;

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
                        byte[] bytes = new byte[1];
                        if (stream.TryFill(bytes)) _value = (sbyte)bytes[0];
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
                stream.WriteByte((byte)_value);
            }
        }

        public static implicit operator sbyte?(nullable value) => value._value;
        public static implicit operator nullable(sbyte? value) => new(value);
    }
}
