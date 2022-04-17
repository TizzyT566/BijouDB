﻿#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.Primitives;

public struct @char : IDataType
{
    public static long Length => 2;

    private char _value;

    private @char(char value) => _value = value;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[2];
        if (stream.TryFill(bytes)) _value = BitConverter.ToChar(bytes);
        else throw new CorruptedException<@char>();
    }

    public void Serialize(Stream stream)
    {
        byte[] bytes = BitConverter.GetBytes(_value);
        stream.Write(bytes);
    }

    public static implicit operator char(@char value) => value._value;
    public static implicit operator @char(char value) => new(value);
}
