#pragma warning disable IDE1006 // Naming Styles

using System.Text;
using BijouDB.Exceptions;

namespace BijouDB.DataTypes;

public struct @string : IDataType
{
    public static long Length => 0;

    private string _value = "";

    private @string(string value) => _value = value ?? "";

    public void Deserialize(Stream stream)
    {
        if (stream.TryReadDynamicData(out byte[] data)) _value = Encoding.UTF8.GetString(data);
        else throw new CorruptedException<@string>();
    }

    public void Serialize(Stream stream) => stream.WriteDynamicData(Encoding.UTF8.GetBytes(_value ?? ""));

    public static implicit operator string(@string value) => value._value ?? "";
    public static implicit operator @string(string value) => new(value);



    // Nullable
    public sealed class nullable : IDataType
    {
        public static long Length => 0;

        private string _value = "";

        private nullable(string value) => _value = value ?? "";

        public nullable() { }

        public void Deserialize(Stream stream)
        {
            if (stream.TryReadDynamicData(out byte[] data)) _value = Encoding.UTF8.GetString(data);
            else throw new CorruptedException<@string>();
        }

        public void Serialize(Stream stream) => stream.WriteDynamicData(Encoding.UTF8.GetBytes(_value ?? ""));

        public static implicit operator string(nullable value) => value._value ?? "";
        public static implicit operator nullable(string value) => new(value);
    }
}
