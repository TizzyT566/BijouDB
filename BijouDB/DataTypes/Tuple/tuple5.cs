#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes
{
    public struct @tuple<T1, T2, T3, T4, T5> : IDataType
        where T1 : IDataType, new()
        where T2 : IDataType, new()
        where T3 : IDataType, new()
        where T4 : IDataType, new()
        where T5 : IDataType, new()
    {
        public static long Length => 0;

        public (T1, T2, T3, T4, T5) _value;

        public @tuple((T1, T2, T3, T4, T5) value) => _value = value;

        public void Deserialize(Stream stream)
        {
            _value.Item1.Deserialize(stream);
            _value.Item2.Deserialize(stream);
            _value.Item3.Deserialize(stream);
            _value.Item4.Deserialize(stream);
            _value.Item5.Deserialize(stream);
        }

        public void Serialize(Stream stream)
        {
            _value.Item1.Serialize(stream);
            _value.Item2.Serialize(stream);
            _value.Item3.Serialize(stream);
            _value.Item4.Serialize(stream);
            _value.Item5.Serialize(stream);
        }

        public override string ToString() => _value.ToString();

        public static implicit operator (T1, T2, T3, T4, T5)(@tuple<T1, T2, T3, T4, T5> value) => value._value;
        public static implicit operator @tuple<T1, T2, T3, T4, T5>((T1, T2, T3, T4, T5) value) => new(value);



        // Nullable
        public sealed class nullable : IDataType
        {
            public static long Length => 0;

            public (T1, T2, T3, T4, T5)? _value;

            public nullable((T1, T2, T3, T4, T5)? value) => _value = value;

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
                            _value = (new T1(), new T2(), new T3(), new T4(), new T5());
                            _value.Value.Item1.Deserialize(stream);
                            _value.Value.Item2.Deserialize(stream);
                            _value.Value.Item3.Deserialize(stream);
                            _value.Value.Item4.Deserialize(stream);
                            _value.Value.Item5.Deserialize(stream);
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
                }
            }

            public override string ToString() => _value.ToString() ?? "\0";

            public static implicit operator (T1, T2, T3, T4, T5)?(nullable value) => value._value;
            public static implicit operator nullable((T1, T2, T3, T4, T5)? value) => new(value);
        }
    }
}
