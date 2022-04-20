#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @ushort : IDataType
{
    public static long Length => 2;

    private ushort _value;

    private @ushort(ushort value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[2];
        if (stream.TryFill(bytes)) _value = BitConverter.ToUInt16(bytes, 0);
        else throw new CorruptedException<@ushort>();
    }

    public void Serialize(Stream stream)
    {
        byte[] bytes = BitConverter.GetBytes(_value);
        stream.Write(bytes);
    }

    public static implicit operator ushort(@ushort value) => value._value;
    public static implicit operator @ushort(ushort value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        public static long Length => @ushort.Length + 1;

        private ushort? _value;

        private nullable(ushort? value) => _value = value;

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
                        if (stream.TryFill(bytes)) _value = BitConverter.ToUInt16(bytes, 0);
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
                stream.Write(BitConverter.GetBytes((ushort)_value));
            }
        }

        public static implicit operator ushort?(nullable value) => value._value;
        public static implicit operator nullable(ushort? value) => new(value);
    }
}
