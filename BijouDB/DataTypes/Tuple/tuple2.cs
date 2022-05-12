#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes
{
    public struct @tuple<T1, T2> : IDataType where T1 : IDataType, new() where T2 : IDataType, new()
    {
        public static long Length => 0;

        private (T1, T2) _value;

        public @tuple((T1, T2) value) => _value = value;

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

        public override string ToString() => _value.ToString();

        public static implicit operator (T1, T2)(@tuple<T1, T2> value) => value._value;
        public static implicit operator @tuple<T1, T2>((T1, T2) value) => new(value);



        // Nullable
        public sealed class nullable : IDataType
        {
            public static long Length => 0;

            private (T1, T2)? _value;

            public nullable((T1, T2)? value) => _value = value;

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
                            _value = (new T1(), new T2());
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

            public override string ToString() => _value.ToString() ?? "";

            public static implicit operator (T1, T2)?(nullable value) => value._value;
            public static implicit operator nullable((T1, T2)? value) => new(value);
        }
    }
}
