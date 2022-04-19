﻿#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

// Not null version
public struct @bool : IDataType
{
    public static long Length => 1;

    private bool _value = default;

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
        public static long Length => 1;

        private bool _value = default;

        private @nullable(bool value) => _value = value;

        public @nullable() { }

        public void Deserialize(Stream stream)
        {
            byte[] bytes = new byte[1];
            if (stream.TryFill(bytes)) _value = bytes[0] != 0;
            else throw new CorruptedException<@bool>();
        }

        public void Serialize(Stream stream) => stream.WriteByte(_value ? byte.MaxValue : byte.MinValue);

        public static implicit operator bool(@nullable value) => value._value;
        public static implicit operator @nullable(bool value) => new(value);
    }
}