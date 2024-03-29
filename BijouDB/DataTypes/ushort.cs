﻿#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB;

public struct @ushort : IDataType
{
    private ushort _value;

    private @ushort(ushort value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[2];
        if (stream.TryFill(bytes)) _value = BitConverter.ToUInt16(bytes, 0);
        else throw new CorruptedException<@ushort>().Log();
    }

    public void Serialize(Stream stream) =>
        stream.Write(BitConverter.GetBytes(_value), 0, 2);

    public static implicit operator ushort(@ushort value) => value._value;
    public static implicit operator @ushort(ushort value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private ushort? _value;

        private nullable(ushort? value) => _value = value;

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
                        byte[] bytes = new byte[2];
                        if (stream.TryFill(bytes)) _value = BitConverter.ToUInt16(bytes, 0);
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
                stream.Write(BitConverter.GetBytes((ushort)_value), 0, 2);
            }
        }

        public static implicit operator ushort?(nullable value) => value?._value;
        public static implicit operator nullable(ushort? value) => new(value);
    }
}
