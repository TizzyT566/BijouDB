﻿#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB;

public struct @byte : IDataType
{
    private byte _value;

    private @byte(byte value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[1];
        if (stream.TryFill(bytes)) _value = bytes[0];
        else throw new CorruptedException<@byte>().Log();
    }

    public void Serialize(Stream stream) => stream.WriteByte(_value);

    public static implicit operator byte(@byte value) => value._value;
    public static implicit operator @byte(byte value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private byte? _value;

        private nullable(byte? value) => _value = value;

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
                        byte[] bytes = new byte[1];
                        if (stream.TryFill(bytes)) _value = bytes[0];
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
                stream.WriteByte((byte)_value);
            }
        }

        public static implicit operator byte?(nullable value) => value?._value;
        public static implicit operator nullable(byte? value) => new(value);
    }
}
