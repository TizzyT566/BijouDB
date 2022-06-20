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
        _value.Item1.Deserialize(stream);
        _value.Item2.Deserialize(stream);
        _value.Item3.Deserialize(stream);
        _value.Item4.Deserialize(stream);
        _value.Item5.Deserialize(stream);
        _value.Item6.Deserialize(stream);
        _value.Item7.Deserialize(stream);
        _value.Item8.Deserialize(stream);
    }

    public void Serialize(Stream stream)
    {
        _value.Item1.Serialize(stream);
        _value.Item2.Serialize(stream);
        _value.Item3.Serialize(stream);
        _value.Item4.Serialize(stream);
        _value.Item5.Serialize(stream);
        _value.Item6.Serialize(stream);
        _value.Item7.Serialize(stream);
        _value.Item8.Serialize(stream);
    }

    public static implicit operator (D1, D2, D3, D4, D5, D6, D7, DRest)(@tuple<D1, D2, D3, D4, D5, D6, D7, DRest> value) => value._value;
    public static implicit operator @tuple<D1, D2, D3, D4, D5, D6, D7, DRest>((D1, D2, D3, D4, D5, D6, D7, DRest) value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        private (D1, D2, D3, D4, D5, D6, D7, DRest)? _value;

        private nullable((D1, D2, D3, D4, D5, D6, D7, DRest)? value) => _value = value;

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
                        _value = (new(), new(), new(), new(), new(), new(), new(), new());
                        _value.Value.Item1.Deserialize(stream);
                        _value.Value.Item2.Deserialize(stream);
                        _value.Value.Item3.Deserialize(stream);
                        _value.Value.Item4.Deserialize(stream);
                        _value.Value.Item5.Deserialize(stream);
                        _value.Value.Item6.Deserialize(stream);
                        _value.Value.Item7.Deserialize(stream);
                        _value.Value.Item8.Deserialize(stream);
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
                _value.Value.Item3.Serialize(stream);
                _value.Value.Item4.Serialize(stream);
                _value.Value.Item5.Serialize(stream);
                _value.Value.Item6.Serialize(stream);
                _value.Value.Item7.Serialize(stream);
                _value.Value.Item8.Serialize(stream);
            }
        }

        public static implicit operator (D1, D2, D3, D4, D5, D6, D7, DRest)?(nullable value) => value?._value;
        public static implicit operator nullable((D1, D2, D3, D4, D5, D6, D7, DRest)? value) => new(value);
    }
}
