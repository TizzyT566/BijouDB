#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB;

public struct @int : IDataType
{
    private int _value;

    private @int(int value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[4];
        if (stream.TryFill(bytes)) _value = BitConverter.ToInt32(bytes, 0);
        else throw new CorruptedException<@int>();
    }

    public void Serialize(Stream stream) =>
        stream.Write(BitConverter.GetBytes(_value), 0, 4);

    public static implicit operator int(@int value) => value._value;
    public static implicit operator @int(int value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private int? _value;

        private nullable(int? value) => _value = value;

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
                        if (stream.TryFill(bytes)) _value = BitConverter.ToInt32(bytes, 0);
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
                stream.Write(BitConverter.GetBytes((int)_value), 0, 4);
            }
        }

        public static implicit operator int?(nullable value) => value?._value;
        public static implicit operator nullable(int? value) => new(value);
    }
}
