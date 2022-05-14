#pragma warning disable IDE1006 // Naming Styles

using System.Text;
using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @string : IDataType
{
    private string _value;

    private @string(string value) => _value = value ?? "";

    public void Deserialize(Stream stream)
    {
        if (stream.TryReadDynamicData(out byte[] data)) _value = Encoding.UTF8.GetString(data);
        else throw new CorruptedException<@string>();
    }

    public void Serialize(Stream stream) => stream.WriteDynamicData(Encoding.UTF8.GetBytes(_value ?? ""));

    public static implicit operator string(@string value) => value._value ?? "";
    public static implicit operator @string(string value) => value is null ? throw new NotNullableException(typeof(@string).Name) : new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private string? _value;

        private nullable(string? value) => _value = value;

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
                        if (stream.TryReadDynamicData(out byte[] data)) _value = Encoding.UTF8.GetString(data);
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
                stream.WriteDynamicData(Encoding.UTF8.GetBytes(_value));
            }
        }

        public static implicit operator string?(nullable value) => value._value;
        public static implicit operator nullable(string? value) => new(value);
    }
}
