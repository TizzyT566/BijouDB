#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;
using static BijouDB.Tables;

namespace BijouDB.DataTypes;

public struct @record<T> : IDataType where T : Tables, new()
{
    public static long Length => 16;

    private Guid _value = default;

    private @record(Guid id) => _value = id;

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[16];
        if (stream.TryFill(bytes)) _value = new Guid(bytes);
        else throw new CorruptedException<record<T>>();
    }

    public void Serialize(Stream stream) => stream.Write(_value.ToByteArray());

    public static implicit operator T(@record<T> value) =>
        TryGet(value._value, out T? record) ? record! : (new());

    public static implicit operator @record<T>(T value) => new(value.Id);



    // Nullable
    public sealed class nullable
    {
        public static long Length => 16;

        private Guid _value = default;

        private nullable(Guid id) => _value = id;

        public nullable() { }

        public void Deserialize(Stream stream)
        {
            byte[] bytes = new byte[16];
            if (stream.TryFill(bytes)) _value = new Guid(bytes);
            else throw new CorruptedException<record<T>>();
        }

        public void Serialize(Stream stream) => stream.Write(_value.ToByteArray());

        public static implicit operator T(nullable value) =>
            TryGet(value._value, out T? record) ? record! : (new());

        public static implicit operator nullable(T value) => new(value.Id);
    }
}
