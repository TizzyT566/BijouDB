#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @blob : IDataType
{
    private byte[] _value;

    private @blob(byte[] value) => _value = value ?? Array.Empty<byte>();

    public void Deserialize(Stream stream)
    {
        if (stream.TryReadDynamicData(out byte[] data)) _value = data;
        else throw new CorruptedException<@bint>();
    }

    public void Serialize(Stream stream) => stream.WriteDynamicData(_value ?? Array.Empty<byte>());

    public static implicit operator byte[](@blob value) => value._value;
    public static implicit operator @blob(byte[] value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private byte[]? _value;

        private nullable(byte[]? value) => _value = value;

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
                        if (stream.TryReadDynamicData(out byte[] data)) _value = data;
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
                stream.WriteDynamicData(_value);
            }
        }

        public static implicit operator byte[]?(nullable value) => value._value;
        public static implicit operator nullable(byte[]? value) => new(value);
    }
}
