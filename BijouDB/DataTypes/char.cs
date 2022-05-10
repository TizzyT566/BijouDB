#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @char : IDataType
{
    public static long Length => 2;

    private char _value;

    private @char(char value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[2];
        if (stream.TryFill(bytes)) _value = BitConverter.ToChar(bytes);
        else throw new CorruptedException<@char>();
    }

    public void Serialize(Stream stream)
    {
        byte[] bytes = BitConverter.GetBytes(_value);
        stream.Write(bytes);
    }

    public override string ToString() => _value.ToString();

    public static implicit operator char(@char value) => value._value;
    public static implicit operator @char(char value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        public static long Length => @char.Length + 1;

        private char? _value;

        private nullable(char? value) => _value = value;

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
                        if (stream.TryFill(bytes)) _value = BitConverter.ToChar(bytes);
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
                stream.Write(BitConverter.GetBytes((char)_value));
            }
        }

        public override string ToString() => _value.ToString() ?? "\0";

        public static implicit operator char?(nullable value) => value._value;
        public static implicit operator nullable(char? value) => new(value);
    }
}