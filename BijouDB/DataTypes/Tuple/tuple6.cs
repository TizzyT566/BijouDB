#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes
{
    public struct @tuple<D1, D2, D3, D4, D5, D6> : IDataType
        where D1 : IDataType, new()
        where D2 : IDataType, new()
        where D3 : IDataType, new()
        where D4 : IDataType, new()
        where D5 : IDataType, new()
        where D6 : IDataType, new()
    {
        public static long Length => 0;

        private (D1, D2, D3, D4, D5, D6) _value;

        public @tuple((D1, D2, D3, D4, D5, D6) value) => _value = value;

        public void Deserialize(Stream stream)
        {
            _value.Item1.Deserialize(stream);
            _value.Item2.Deserialize(stream);
            _value.Item3.Deserialize(stream);
            _value.Item4.Deserialize(stream);
            _value.Item5.Deserialize(stream);
            _value.Item6.Deserialize(stream);
        }

        public void Serialize(Stream stream)
        {
            _value.Item1.Serialize(stream);
            _value.Item2.Serialize(stream);
            _value.Item3.Serialize(stream);
            _value.Item4.Serialize(stream);
            _value.Item5.Serialize(stream);
            _value.Item6.Serialize(stream);
        }

        public override string ToString() => _value.ToString();

        public static implicit operator (D1, D2, D3, D4, D5, D6)(@tuple<D1, D2, D3, D4, D5, D6> value) => value._value;
        public static implicit operator @tuple<D1, D2, D3, D4, D5, D6>((D1, D2, D3, D4, D5, D6) value) => new(value);



        // Nullable
        public sealed class nullable : IDataType
        {
            public static long Length => 0;

            private (D1, D2, D3, D4, D5, D6)? _value;

            public nullable((D1, D2, D3, D4, D5, D6)? value) => _value = value;

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
                            _value = (new D1(), new D2(), new D3(), new D4(), new D5(), new D6());
                            _value.Value.Item1.Deserialize(stream);
                            _value.Value.Item2.Deserialize(stream);
                            _value.Value.Item3.Deserialize(stream);
                            _value.Value.Item4.Deserialize(stream);
                            _value.Value.Item5.Deserialize(stream);
                            _value.Value.Item6.Deserialize(stream);
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
                }
            }

            public override string ToString() => _value.ToString() ?? "";

            public static implicit operator (D1, D2, D3, D4, D5, D6)?(nullable value) => value._value;
            public static implicit operator nullable((D1, D2, D3, D4, D5, D6)? value) => new(value);
        }
    }
}
