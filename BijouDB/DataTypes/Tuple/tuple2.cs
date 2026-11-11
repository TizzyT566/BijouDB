#pragma warning disable IDE1006 // Naming Styles

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
        D1 item1 = new();
        item1.Deserialize(stream);
        D2 item2 = new();
        item2.Deserialize(stream);
        _value = (item1, item2);
    }

    public void Serialize(Stream stream)
    {
        (_value.Item1 is null ? new D1() : _value.Item1).Serialize(stream);
        (_value.Item2 is null ? new D2() : _value.Item2).Serialize(stream);
    }

    public static implicit operator (D1, D2)(@tuple<D1, D2> value) => value._value;
    public static implicit operator @tuple<D1, D2>((D1, D2) value) => new(value);

    public sealed class nullable : IDataType
    {
        private (D1, D2)? _value;

        private nullable((D1, D2)? value) => _value = value;

        public nullable() => _value = null;

        public void Deserialize(Stream stream)
        {
            int marker = stream.ReadByte();
            if (marker < 0) throw new CorruptedException<nullable>().Log();
            if (marker == 0)
            {
                _value = null;
                return;
            }

            if (marker != byte.MaxValue) throw new CorruptedException<nullable>().Log();

            D1 item1 = new();
            item1.Deserialize(stream);
            D2 item2 = new();
            item2.Deserialize(stream);
            // Assign once: Nullable<T>.Value exposes a copy of a struct tuple.
            _value = (item1, item2);
        }

        public void Serialize(Stream stream)
        {
            if (_value is null)
            {
                stream.WriteByte(0);
                return;
            }

            stream.WriteByte(byte.MaxValue);
            (D1, D2) value = _value.Value;
            (value.Item1 is null ? new D1() : value.Item1).Serialize(stream);
            (value.Item2 is null ? new D2() : value.Item2).Serialize(stream);
        }

        public static implicit operator (D1, D2)?(nullable value) => value?._value;
        public static implicit operator nullable((D1, D2)? value) => new(value);
    }
}
