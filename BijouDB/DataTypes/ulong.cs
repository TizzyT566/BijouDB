﻿#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @ulong : IDataType
{
    public static long Length => 8;

    private ulong _value;

    private @ulong(ulong value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[8];
        if (stream.TryFill(bytes)) _value = BitConverter.ToUInt64(bytes, 0);
        else throw new CorruptedException<@ulong>();
    }

    public void Serialize(Stream stream)
    {
        byte[] bytes = BitConverter.GetBytes(_value);
        stream.Write(bytes);
    }

    public override string ToString() => _value.ToString();

    public static implicit operator ulong(@ulong value) => value._value;
    public static implicit operator @ulong(ulong value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        public static long Length => @ulong.Length + 1;

        private ulong? _value;

        private nullable(ulong? value) => _value = value;

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
                        byte[] bytes = new byte[8];
                        if (stream.TryFill(bytes)) _value = BitConverter.ToUInt64(bytes, 0);
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
                stream.Write(BitConverter.GetBytes((ulong)_value));
            }
        }

        public override string ToString() => _value.ToString() ?? "";

        public static implicit operator ulong?(nullable value) => value._value;
        public static implicit operator nullable(ulong? value) => new(value);
    }
}
