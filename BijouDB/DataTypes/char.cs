﻿#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB;

public struct @char : IDataType
{
    private char _value;

    private @char(char value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[2];
        if (stream.TryFill(bytes)) _value = BitConverter.ToChar(bytes, 0);
        else throw new CorruptedException<@char>().Log();
    }

    public void Serialize(Stream stream) =>
        stream.Write(BitConverter.GetBytes(_value), 0, 2);

    public static implicit operator char(@char value) => value._value;
    public static implicit operator @char(char value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private char? _value;

        private nullable(char? value) => _value = value;

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
                        if (stream.TryFill(bytes)) _value = BitConverter.ToChar(bytes, 0);
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
                stream.Write(BitConverter.GetBytes((char)_value), 0, 2);
            }
        }

        public static implicit operator char?(nullable value) => value?._value;
        public static implicit operator nullable(char? value) => new(value);
    }
}