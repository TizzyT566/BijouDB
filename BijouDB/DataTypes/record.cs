#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;
using static BijouDB.Record;

namespace BijouDB.DataTypes;

public struct @record<R> : IDataType where R : Record, new()
{
    public static long Length => 16;

    private R _value;

    private @record(R value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[16];
        if (stream.TryFill(bytes))
        {
            Guid guid = new Guid(bytes);
            TryGet(guid, out _value!);
        }
        else throw new CorruptedException<@record<R>.nullable>();
    }

    public void Serialize(Stream stream)
    {
        stream.Write((_value is null ? Guid.Empty : _value.Id).ToByteArray());
    }

    public override string ToString() => _value.ToString() ?? "\0";

    public static implicit operator R(@record<R> value) => value._value;
    public static implicit operator @record<R>(R value) => value is null ? throw new NotNullableException($"record<{typeof(R).Name}>") : new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        public static long Length => record<R>.Length + 1;

        private R? _value;

        private nullable(R? value) => _value = value;

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

        public override string ToString() => _value?.ToString() ?? "";

        public static implicit operator R?(nullable value) => value._value;
        public static implicit operator nullable(R? value) => new(value);
    }
}
