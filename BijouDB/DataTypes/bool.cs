#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @bool : IDataType
{
    private bool _value;

    private @bool(bool value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[1];
        if (stream.TryFill(bytes)) _value = bytes[0] != 0;
        else throw new CorruptedException<@bool>();
    }

    public void Serialize(Stream stream) => stream.WriteByte(_value ? byte.MaxValue : byte.MinValue);

    public static implicit operator bool(@bool value) => value._value;
    public static implicit operator @bool(bool value) => new(value);



    // Nullable
    public sealed class @nullable : IDataType
    {
        private bool? _value;

        private @nullable(bool? value) => _value = value;

        public @nullable() => _value = null;

        public void Deserialize(Stream stream)
        {
            switch (stream.ReadByte())
            {
                case < 0: throw new CorruptedException<nullable>();
                case 0:
                    {
                        _value = null;
                        break;
                    }
                default:
                    {
                        byte[] bytes = new byte[1];
                        if (stream.TryFill(bytes)) _value = bytes[0] != 0;
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
                stream.WriteByte((bool)_value ? byte.MaxValue : byte.MinValue);
            }
        }

        public static implicit operator bool?(@nullable value) => value._value;
        public static implicit operator @nullable(bool? value) => new(value);
    }
}