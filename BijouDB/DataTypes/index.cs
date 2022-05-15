#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

internal interface IIndex { }

public struct @index<R, D> : IIndex, IDataType
    where R : Record, new()
    where D : IDataType, new()
{
    private (@record<R>, D) _value;

    private @index((R record, D index) value) => _value = value;

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

    public static implicit operator (R record, D index)(@index<R, D> value) => value._value;
    public static implicit operator @index<R, D>((R record, D index) value) => new(value);
    public static implicit operator R(@index<R, D> value) => value._value.Item1;



    // Nullable
    public sealed class nullable : IDataType
    {
        private (@record<R>, D)? _value;

        private nullable((R record, D index)? value) => _value = value;

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

        public override string ToString() => _value.ToString() ?? "";

        public static implicit operator (R record, D index)?(nullable value) => value._value;
        public static implicit operator nullable((R record, D index)? value) => new(value);
        public static implicit operator R?(nullable value) => value._value.HasValue ? value._value.Value.Item1 : default;
    }
}

//public struct @index<R, D> : IDataType
//    where R : Record, new()
//    where D : IDataType, new()
//{
//    private readonly R _record;
//    private readonly D _index;

//    private @index((R, D) value)
//    {
//        _record = value.Item1;
//        _index = value.Item2;
//    }

//    public void Deserialize(Stream stream)
//    {
//        ((@record<R>)_record).Deserialize(stream);
//        _index.Deserialize(stream);
//    }

//    public void Serialize(Stream stream)
//    {
//        ((@record<R>)_record).Serialize(stream);
//        _index.Serialize(stream);
//    }

//    public static implicit operator (R, D)(@index<R, D> value) => (value._record, value._index);
//    public static implicit operator R(@index<R, D> value) => value._record;
//    public static implicit operator @index<R, D>((R, D) value) => new(value);



//    // Nullable
//    public sealed class nullable : IDataType
//    {
//        private R? _record;
//        private D? _index;

//        private nullable((R?, D?)? value)
//        {
//            if (value.HasValue)
//            {
//                _record = value.Value.Item1;
//                _index = value.Value.Item2;
//            }
//        }

//        public void Deserialize(Stream stream)
//        {
//            switch (stream.ReadByte())
//            {
//                case < 0:
//                    {
//                        throw new CorruptedException<nullable>();
//                    }
//                case 0:
//                    {
//                        _record = null;
//                        break;
//                    }
//                default:
//                    {
//                        byte[] bytes = new byte[16];
//                        if (stream.TryFill(bytes)) TryGet(new Guid(bytes), out _record);
//                        else throw new CorruptedException<nullable>();
//                        break;
//                    }
//            }
//            switch (stream.ReadByte())
//            {
//                case < 0:
//                    {
//                        throw new CorruptedException<nullable>();
//                    }
//                case 0:
//                    {
//                        _record = null;
//                        break;
//                    }
//                default:
//                    {
//                        _index = new();
//                        _index.Deserialize(stream);
//                        break;
//                    }
//            }
//        }

//        public void Serialize(Stream stream)
//        {
//            if (_record is null)
//            {
//                stream.WriteByte(byte.MinValue);
//            }
//            else
//            {
//                stream.WriteByte(byte.MaxValue);
//                ((@record<R>)_record).Serialize(stream);
//            }
//            if (_index is null)
//            {
//                stream.WriteByte(byte.MinValue);
//            }
//            else
//            {
//                stream.WriteByte(byte.MaxValue);
//                _index.Serialize(stream);
//            }
//        }

//        public static implicit operator (R?, D?)?(nullable value) => (value._record, value._index);
//        public static implicit operator R?(nullable value) => value._record;
//        public static implicit operator nullable((R?, D?)? value) => new(value);
//    }
//}
