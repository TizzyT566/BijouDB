#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;
using static BijouDB.Table;

namespace BijouDB.DataTypes;

public struct @record<T> : IDataType where T : Table, new()
{
    public static long Length => 16;

    private T _value;

    private @record(T value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[16];
        if (stream.TryFill(bytes)) TryGet(new(bytes), out _value!);
        else throw new CorruptedException<@record<T>.nullable>();
    }

    public void Serialize(Stream stream)
    {
        stream.WriteByte(byte.MaxValue);
        stream.Write((_value is null ? Guid.Empty : _value.Id).ToByteArray());
    }

    public static implicit operator T(@record<T> value) => value._value;
    public static implicit operator @record<T>(T value) => value is null ? throw new NotNullableException($"record<{typeof(T).Name}>") : new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        public static long Length => record<T>.Length + 1;

        private T? _value;

        private nullable(T? value) => _value = value;

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
                        byte[] bytes = new byte[16];
                        if (stream.TryFill(bytes)) TryGet(new(bytes), out _value);
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
                stream.Write(_value.Id.ToByteArray());
            }
        }

        public static implicit operator T?(nullable value) => value._value;
        public static implicit operator nullable(T? value) => new(value);
    }
}
