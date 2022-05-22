﻿#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB;

public struct @tuple<D1, D2> : IDataType
    where D1 : IDataType, new()
    where D2 : IDataType, new()
{
    private (D1, D2) _value;

    private @tuple((D1, D2) value) => _value = value;

    public void Deserialize(Stream stream)
    {
        _value.Item1.Deserialize(stream);
        _value.Item2.Deserialize(stream);
    }

    public void Serialize(Stream stream)
    {
        _value.Item1.Serialize(stream);
        _value.Item2.Serialize(stream);
    }

    public static implicit operator (D1, D2)(@tuple<D1, D2> value) => value._value;
    public static implicit operator @tuple<D1, D2>((D1, D2) value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private (D1, D2)? _value;

        private nullable((D1, D2)? value) => _value = value;

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
                        _value = (new(), new());
                        _value.Value.Item1.Deserialize(stream);
                        _value.Value.Item2.Deserialize(stream);
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
                _value.Value.Item1.Serialize(stream);
                _value.Value.Item2.Serialize(stream);
            }
        }

        public static implicit operator (D1, D2)?(nullable value) => value._value;
        public static implicit operator nullable((D1, D2)? value) => new(value);
    }
}
