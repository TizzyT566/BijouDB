#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB;

public struct @tuple<D1, D2, D3, D4, D5, D6, D7, DRest> : IDataType
    where D1 : IDataType, new()
    where D2 : IDataType, new()
    where D3 : IDataType, new()
    where D4 : IDataType, new()
    where D5 : IDataType, new()
    where D6 : IDataType, new()
    where D7 : IDataType, new()
    where DRest : IDataType, new()
{
    private (D1, D2, D3, D4, D5, D6, D7, DRest) _value;

    private @tuple((D1, D2, D3, D4, D5, D6, D7, DRest) value) => _value = value;

    public void Deserialize(Stream stream)
    {
        D1 item1 = new();
        item1.Deserialize(stream);
        D2 item2 = new();
        item2.Deserialize(stream);
        D3 item3 = new();
        item3.Deserialize(stream);
        D4 item4 = new();
        item4.Deserialize(stream);
        D5 item5 = new();
        item5.Deserialize(stream);
        D6 item6 = new();
        item6.Deserialize(stream);
        D7 item7 = new();
        item7.Deserialize(stream);
        DRest item8 = new();
        item8.Deserialize(stream);
        _value = (item1, item2, item3, item4, item5, item6, item7, item8);
    }

    public void Serialize(Stream stream)
    {
        (_value.Item1 is null ? new D1() : _value.Item1).Serialize(stream);
        (_value.Item2 is null ? new D2() : _value.Item2).Serialize(stream);
        (_value.Item3 is null ? new D3() : _value.Item3).Serialize(stream);
        (_value.Item4 is null ? new D4() : _value.Item4).Serialize(stream);
        (_value.Item5 is null ? new D5() : _value.Item5).Serialize(stream);
        (_value.Item6 is null ? new D6() : _value.Item6).Serialize(stream);
        (_value.Item7 is null ? new D7() : _value.Item7).Serialize(stream);
        (_value.Item8 is null ? new DRest() : _value.Item8).Serialize(stream);
    }

    public static implicit operator (D1, D2, D3, D4, D5, D6, D7, DRest)(@tuple<D1, D2, D3, D4, D5, D6, D7, DRest> value) => value._value;
    public static implicit operator @tuple<D1, D2, D3, D4, D5, D6, D7, DRest>((D1, D2, D3, D4, D5, D6, D7, DRest) value) => new(value);

    public sealed class nullable : IDataType
    {
        private (D1, D2, D3, D4, D5, D6, D7, DRest)? _value;

        private nullable((D1, D2, D3, D4, D5, D6, D7, DRest)? value) => _value = value;

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
            D3 item3 = new();
            item3.Deserialize(stream);
            D4 item4 = new();
            item4.Deserialize(stream);
            D5 item5 = new();
            item5.Deserialize(stream);
            D6 item6 = new();
            item6.Deserialize(stream);
            D7 item7 = new();
            item7.Deserialize(stream);
            DRest item8 = new();
            item8.Deserialize(stream);
            // Assign once: Nullable<T>.Value exposes a copy of a struct tuple.
            _value = (item1, item2, item3, item4, item5, item6, item7, item8);
        }

        public void Serialize(Stream stream)
        {
            if (_value is null)
            {
                stream.WriteByte(0);
                return;
            }

            stream.WriteByte(byte.MaxValue);
            (D1, D2, D3, D4, D5, D6, D7, DRest) value = _value.Value;
            (value.Item1 is null ? new D1() : value.Item1).Serialize(stream);
            (value.Item2 is null ? new D2() : value.Item2).Serialize(stream);
            (value.Item3 is null ? new D3() : value.Item3).Serialize(stream);
            (value.Item4 is null ? new D4() : value.Item4).Serialize(stream);
            (value.Item5 is null ? new D5() : value.Item5).Serialize(stream);
            (value.Item6 is null ? new D6() : value.Item6).Serialize(stream);
            (value.Item7 is null ? new D7() : value.Item7).Serialize(stream);
            (value.Item8 is null ? new DRest() : value.Item8).Serialize(stream);
        }

        public static implicit operator (D1, D2, D3, D4, D5, D6, D7, DRest)?(nullable value) => value?._value;
        public static implicit operator nullable((D1, D2, D3, D4, D5, D6, D7, DRest)? value) => new(value);
    }
}
