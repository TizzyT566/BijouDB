using BijouDB.Components;

namespace BijouDB;

public static class Misc
{
    /// <summary>
    /// Ensures a stream's total size.
    /// </summary>
    /// <param name="this">The stream to flush.</param>
    /// <param name="size">The desired total size of the stream.</param>
    public static void Flush(this Stream @this, long size, byte[]? padding = null)
    {
        // Seek to end of stream
        if (padding is null) padding = new byte[8192];
        @this.Position = @this.Length;
        while (@this.Position < size)
        {
            int writeLength = (int)Math.Min(Math.Abs(size - @this.Position), padding.Length);
            @this.Write(padding, 0, writeLength);
        }
        @this.Flush();
    }

    public static bool StreamCompare(Stream s1, Stream s2)
    {
        long? s1Length = null, s2Length = null;

        try { s1Length = s1.Length; }
        catch (Exception) { }

        try { s2Length = s2.Length; }
        catch (Exception) { }

        if (s1Length != s2Length || s1Length is null) return false;

        int bufferLength = 8192;
        byte[] s1Buffer = new byte[bufferLength], s2Buffer = new byte[bufferLength];

        long s1Total = 0, s2Total = 0;
        int s1Read = 0, s2Read = 0;

        while (true)
        {
            while (((s1Total += s1Read = s1.Read(s1Buffer, s1Read, bufferLength - s1Read)) < bufferLength) && s1Read != 0) ;
            while (((s2Total += s2Read = s2.Read(s2Buffer, s2Read, bufferLength - s2Read)) < bufferLength) && s2Read != 0) ;
            if (s1Total != s2Total) return false;
            for (int i = 0; i < s1Length; i++) if (s1Buffer[i] != s2Buffer[i]) return false;
            if (s1Read == 0 && s2Read == 0) break;
        }

        return true;
    }

    public static Guid Hash(this IDataType value)
    {
        using MemoryStream ms = new();
        return value.Hash(ms);
    }
    public static Guid Hash(this IDataType value, Stream stream)
    {
        long pos = stream.Position;
        using MaskedStream ms = new(stream, Globals.SeedMask);
        value.Serialize(ms);
        stream.Position = 0;
        Guid ret = stream.GetSkipHash();
        stream.Position = pos;
        return ret;
    }

    public static bool ReadHashValue(this Stream @this, out Guid hash, out Guid value)
    {
        byte[] prevHashBytes = new byte[16];
        byte[] prevValueBytes = new byte[16];
        if (@this.TryFill(prevHashBytes) && @this.TryFill(prevValueBytes))
        {
            hash = new(prevHashBytes);
            value = new(prevValueBytes);
            return true;
        }
        hash = default;
        value = default;
        return false;
    }

    public static void WriteHashValue(this Stream @this, in Guid newHash, in Guid newValue)
    {
        @this.Write(newHash.ToByteArray(), 0, 16);
        @this.Write(newValue.ToByteArray(), 0, 16);
    }

    public static bool TryFill(this Stream @this, byte[] buffer, int offset = 0, int length = -1)
    {
        if (length < 1) length = buffer.Length;
        int total = 0, read;
        while ((total += read = @this.Read(buffer, total + offset, length - total)) < length)
            if (read == 0) return false;
        return total == length;
    }

    public static bool TryReadValueSize<T>(this T @this, out long valLen)
        where T : Stream
    {
        int temp = @this.ReadByte();

        if (temp < 0)
        {
            valLen = 0;
            return false;
        }
        valLen = temp;
        if (temp == 0)
        {
            valLen = 0;
            return true;
        }

        byte[] bits = new byte[8];

        switch (valLen)
        {
            case >= 0b1111_1111: // Length occupies next 8 bytes
                {
                    @this.TryFill(bits);
                    valLen = BitConverter.ToInt64(bits, 0);
                    break;
                }
            case >= 0b1111_1110: // Length occupies next 7 bytes
                {
                    @this.TryFill(bits, 0, 7);
                    valLen = BitConverter.ToInt64(bits, 0);
                    break;
                }
            case >= 0b1111_1100: // Length occupies next 6 bytes
                {
                    @this.TryFill(bits, 0, 6);
                    bits[6] += (byte)(temp & 0b1);
                    valLen = BitConverter.ToInt64(bits, 0);
                    break;
                }
            case >= 0b1111_1000: // Length occupies next 5 bytes
                {
                    @this.TryFill(bits, 0, 5);
                    bits[5] += (byte)(temp & 0b11);
                    valLen = BitConverter.ToInt64(bits, 0);
                    break;
                }
            case >= 0b1111_0000: // Length occupies next 4 bytes
                {
                    @this.TryFill(bits, 0, 4);
                    bits[4] += (byte)(temp & 0b111);
                    valLen = BitConverter.ToInt64(bits, 0);
                    break;
                }
            case >= 0b1110_0000: // Length occupies next 3 bytes
                {
                    @this.TryFill(bits, 0, 3);
                    bits[3] += (byte)(temp & 0b1111);
                    valLen = BitConverter.ToInt64(bits, 0);
                    break;
                }
            case >= 0b1100_0000: // Length occupies next 2 bytes
                {
                    @this.TryFill(bits, 0, 2);
                    bits[2] += (byte)(temp & 0b11111);
                    valLen = BitConverter.ToInt64(bits, 0);
                    break;
                }
            case >= 0b1000_0000: // Length occupies next 1 bytes
                {
                    @this.TryFill(bits, 0, 1);
                    bits[1] += (byte)(temp & 0b111111);
                    valLen = BitConverter.ToInt64(bits, 0);
                    break;
                }
            default:
                break;
        }
        return true;
    }

    public static bool TryReadDynamicData<T>(this T @this, out byte[] data)
        where T : Stream
    {
        if (@this.TryReadValueSize(out long size))
        {
            data = new byte[size];
            return @this.TryFill(data);
        }
        data = Array.Empty<byte>();
        return false;
    }

    public static T WriteValueSize<T>(this T @this, in long valLen)
        where T : Stream
    {
        if (valLen < 0) throw new ArgumentOutOfRangeException(nameof(valLen));
        byte[] bytes = BitConverter.GetBytes(valLen);
        switch (valLen)
        {
            case < 128:
                {
                    @this.WriteByte(bytes[0]);
                    break;
                }
            case < 16384:
                {
                    bytes[1] += 0b1000_0000;
                    @this.WriteByte(bytes[1]);
                    @this.Write(bytes, 0, 1);
                    break;
                }
            case < 1048576:
                {
                    bytes[2] += 0b1100_0000;
                    @this.WriteByte(bytes[2]);
                    @this.Write(bytes, 0, 2);
                    break;
                }
            case < 268435456:
                {
                    bytes[3] += 0b1110_0000;
                    @this.WriteByte(bytes[3]);
                    @this.Write(bytes, 0, 3);
                    break;
                }
            case < 34359738368:
                {
                    bytes[4] += 0b1111_0000;
                    @this.WriteByte(bytes[4]);
                    @this.Write(bytes, 0, 4);
                    break;
                }
            case < 4398046511104:
                {
                    bytes[5] += 0b1111_1000;
                    @this.WriteByte(bytes[5]);
                    @this.Write(bytes, 0, 5);
                    break;
                }
            case < 562949953421312:
                {
                    bytes[6] += 0b1111_1100;
                    @this.WriteByte(bytes[6]);
                    @this.Write(bytes, 0, 6);
                    break;
                }
            case < 72057594037927936:
                {
                    bytes[7] += 0b1111_1110;
                    @this.WriteByte(bytes[7]);
                    @this.Write(bytes, 0, 7);
                    break;
                }
            default:
                {
                    @this.WriteByte(255);
                    @this.Write(bytes, 0, 8);
                    break;
                }
        }
        return @this;
    }

    public static void WriteDynamicData<T>(this T @this, in byte[] data)
        where T : Stream
    {
        @this.WriteValueSize(data.LongLength);
        @this.Write(data, 0, data.Length);
    }
}
