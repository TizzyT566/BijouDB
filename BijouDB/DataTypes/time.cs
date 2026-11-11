#pragma warning disable IDE1006 // Naming Styles

using BijouDB.Exceptions;

namespace BijouDB;

public struct @time : IDataType
{
    private DateTime _value;

    private @time(DateTime value) => _value = value;

    // Nonnegative values retain the legacy ticks-only encoding (Unspecified).
    // Negative values reserve bit 63 as a format marker and bit 62 for Local;
    // the remaining 62 bits store ticks. A marked value without bit 62 is UTC.
    // DateTime.MaxValue.Ticks fits in 62 bits; no legacy value is reinterpreted.
    private static long Encode(DateTime value) => value.Kind == DateTimeKind.Unspecified ? value.Ticks : unchecked((long)((1UL << 63) | (value.Kind == DateTimeKind.Local ? 1UL << 62 : 0) | (ulong)value.Ticks));

    private static DateTime Decode(long encoded)
    {
        if (encoded >= 0)
        {
            if (encoded > DateTime.MaxValue.Ticks) throw new CorruptedException<@time>().Log();
            return new DateTime(encoded, DateTimeKind.Unspecified);
        }

        ulong bits = unchecked((ulong)encoded);
        long ticks = (long)(bits & ((1UL << 62) - 1));
        if (ticks > DateTime.MaxValue.Ticks) throw new CorruptedException<@time>().Log();
        return new DateTime(ticks, (bits & (1UL << 62)) == 0 ? DateTimeKind.Utc : DateTimeKind.Local);
    }

    public void Deserialize(Stream stream)
    {
        byte[] bytes = new byte[8];
        if (!stream.TryFill(bytes)) throw new CorruptedException<@time>().Log();
        _value = Decode(BitConverter.ToInt64(bytes, 0));
    }

    public void Serialize(Stream stream) => stream.Write(BitConverter.GetBytes(Encode(_value)), 0, 8);

    public static implicit operator DateTime(@time value) => value._value;
    public static implicit operator @time(DateTime value) => new(value);

    public sealed class nullable : IDataType
    {
        private DateTime? _value;

        private nullable(DateTime? value) => _value = value;

        public nullable() => _value = null;

        public void Deserialize(Stream stream)
        {
            int marker = stream.ReadByte();
            if (marker == 0)
            {
                _value = null;
                return;
            }

            if (marker != byte.MaxValue) throw new CorruptedException<nullable>().Log();

            byte[] bytes = new byte[8];
            if (!stream.TryFill(bytes)) throw new CorruptedException<nullable>().Log();
            _value = Decode(BitConverter.ToInt64(bytes, 0));
        }

        public void Serialize(Stream stream)
        {
            if (_value is null)
            {
                stream.WriteByte(0);
                return;
            }

            stream.WriteByte(byte.MaxValue);
            stream.Write(BitConverter.GetBytes(Encode(_value.Value)), 0, 8);
        }

        public static implicit operator DateTime?(nullable value) => value?._value;
        public static implicit operator nullable(DateTime? value) => new(value);
    }
}
