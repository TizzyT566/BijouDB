#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @float : IDataType
{
    public static long Length => 4;

    private float _value;

    private @float(float value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[4];
        if (stream.TryFill(bytes)) _value = BitConverter.ToSingle(bytes, 0);
        else throw new CorruptedException<@float>();
    }

    public void Serialize(Stream stream)
    {
        byte[] bytes = BitConverter.GetBytes(_value);
        stream.Write(bytes);
    }

    public override string ToString() => _value.ToString();

    public static implicit operator float(@float value) => value._value;
    public static implicit operator @float(float value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        public static long Length => @float.Length + 1;

        private float? _value;

        private nullable(float? value) => _value = value;

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
                        if (stream.TryFill(bytes)) _value = BitConverter.ToSingle(bytes, 0);
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
                stream.Write(BitConverter.GetBytes((float)_value));
            }
        }

        public override string ToString() => _value.ToString() ?? "";

        public static implicit operator float?(nullable value) => value._value;
        public static implicit operator nullable(float? value) => new(value);
    }
}
