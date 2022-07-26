#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB;

public struct @ulong : IDataType
{
    private ulong _value;

    private @ulong(ulong value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[8];
        if (stream.TryFill(bytes)) _value = BitConverter.ToUInt64(bytes, 0);
        else throw new CorruptedException<@ulong>().Log();
    }

    public void Serialize(Stream stream) =>
        stream.Write(BitConverter.GetBytes(_value), 0, 8);

    public static implicit operator ulong(@ulong value) => value._value;
    public static implicit operator @ulong(ulong value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private ulong? _value;

        private nullable(ulong? value) => _value = value;

        public nullable() => _value = null;

        public void Deserialize(Stream stream)
        {
            switch (stream.ReadByte())
            {
                case < 0:
                    {
                        throw new CorruptedException<nullable>().Log();
                    }
                case 0:
                    {
                        _value = null;
                        break;
                    }
                default:
                    {
                        byte[] bytes = new byte[8];
                        if (stream.TryFill(bytes)) _value = BitConverter.ToUInt64(bytes, 0);
                        else throw new CorruptedException<nullable>().Log();
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
                stream.Write(BitConverter.GetBytes((ulong)_value), 0, 8);
            }
        }

        public static implicit operator ulong?(nullable value) => value?._value;
        public static implicit operator nullable(ulong? value) => new(value);
    }
}
