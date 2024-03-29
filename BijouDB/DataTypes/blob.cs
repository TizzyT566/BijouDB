﻿#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB;

public struct @blob : IDataType
{
    private byte[] _value;

    private @blob(byte[] value)
    {
        if (value is null)
        {
            _value = Array.Empty<byte>();
        }
        else
        {
            _value = new byte[value.Length];
            Array.Copy(value, 0, _value, 0, value.Length);
        }
    }

    public void Deserialize(Stream stream)
    {
        if (stream.TryReadDynamicData(out byte[] data)) _value = data;
        else throw new CorruptedException<@bint>().Log();
    }

    public void Serialize(Stream stream) => stream.WriteDynamicData(_value ?? Array.Empty<byte>());

    public static implicit operator byte[](@blob value) => value._value;
    public static implicit operator @blob(byte[] value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private byte[]? _value;

        private nullable(byte[]? value)
        {
            if (value is null)
            {
                _value = null;
            }
            else
            {
                _value = new byte[value.Length];
                Array.Copy(value, 0, _value, 0, value.Length);
            }
        }

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
                        if (stream.TryReadDynamicData(out byte[] data)) _value = data;
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
                stream.WriteDynamicData(_value);
            }
        }

        public static implicit operator byte[]?(nullable value) => value?._value;
        public static implicit operator nullable(byte[]? value) => new(value);
    }
}
