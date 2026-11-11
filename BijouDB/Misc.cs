using BijouDB.Components;
using System.Globalization;

namespace BijouDB;

internal static class Misc
{
    internal const int MaxDynamicDataLength = 256 * 1024 * 1024;

    public static bool StreamCompare(Stream firstStream, Stream secondStream)
    {
        if (firstStream is null) throw new ArgumentNullException(nameof(firstStream));
        if (secondStream is null) throw new ArgumentNullException(nameof(secondStream));

        byte[] first = new byte[8192], second = new byte[8192];
        while (true)
        {
            int firstCount = ReadChunk(firstStream, first), secondCount = ReadChunk(secondStream, second);
            if (firstCount != secondCount) return false;
            for (int i = 0; i < firstCount; i++)
                if (first[i] != second[i]) return false;

            if (firstCount == 0) return true;
        }
    }

    private static int ReadChunk(Stream stream, byte[] buffer)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int read = stream.Read(buffer, total, buffer.Length - total);
            if (read == 0) break;
            total += read;
        }

        return total;
    }

    public static string PaddedString(this ulong value) => value.ToString("D20", CultureInfo.InvariantCulture);

    public static ulong Hash(this IDataType value, Stream stream)
    {
        long position = stream.Position;

        try
        {
            using MaskedStream masked = new(stream, Globals.BitMaskSeed);
            value.Serialize(masked);
            stream.Position = 0;
            return stream.GetSkipHash();
        }
        finally
        {
            stream.Position = position;
        }
    }

    public static FileStream GetTemporaryFileStream(int bufferSize = 4096)
    {
        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
        for (int attempt = 0; attempt < 8; attempt++)
        {
            string path = Path.Combine(Path.GetTempPath(), $"BijouDB-{Guid.NewGuid():N}.tmp");

            try
            {
                return new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, bufferSize, FileOptions.DeleteOnClose);
            }
            catch (IOException) when (File.Exists(path))
            {
            }
        }

        throw new IOException("Could not allocate a unique temporary file.");
    }

    public static bool ReadHashValue(this Stream stream, out ulong hash, out Guid value)
    {
        byte[] hashBytes = new byte[8], valueBytes = new byte[16];
        if (stream.TryFill(hashBytes) && stream.TryFill(valueBytes))
        {
            hash = BitConverter.ToUInt64(hashBytes, 0);
            value = new(valueBytes);
            return true;
        }

        hash = default;
        value = default;
        return false;
    }

    public static void WriteHashValue(this Stream stream, in ulong hash, in Guid value)
    {
        stream.Write(BitConverter.GetBytes(hash), 0, 8);
        stream.Write(value.ToByteArray(), 0, 16);
    }

    public static bool TryFill(this Stream stream, byte[] buffer, int offset = 0, int length = -1)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (buffer is null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0 || offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
        if (length == -1)
            length = buffer.Length - offset;
        if (length < 0 || length > buffer.Length - offset) throw new ArgumentOutOfRangeException(nameof(length));

        int total = 0;
        while (total < length)
        {
            int read = stream.Read(buffer, offset + total, length - total);
            if (read == 0) return false;
            total += read;
        }

        return true;
    }

    public static bool TryReadValueSize<T>(this T stream, out long valueLength)
        where T : Stream
    {
        int prefix = stream.ReadByte();
        valueLength = 0;
        if (prefix < 0) return false;
        if (prefix < 128)
        {
            valueLength = prefix;
            return true;
        }

        int bytes, mask;
        if (prefix < 192)
        {
            bytes = 1;
            mask = 63;
        }
        else if (prefix < 224)
        {
            bytes = 2;
            mask = 31;
        }
        else if (prefix < 240)
        {
            bytes = 3;
            mask = 15;
        }
        else if (prefix < 248)
        {
            bytes = 4;
            mask = 7;
        }
        else if (prefix < 252)
        {
            bytes = 5;
            mask = 3;
        }
        else if (prefix < 254)
        {
            bytes = 6;
            mask = 1;
        }
        else if (prefix == 254)
        {
            bytes = 7;
            mask = 0;
        }
        else
        {
            bytes = 8;
            mask = 0;
        }

        byte[] bits = new byte[8];
        if (!stream.TryFill(bits, 0, bytes)) return false;
        if (bytes < 8)
            bits[bytes] = (byte)(prefix & mask);
        long decoded = BitConverter.ToInt64(bits, 0);
        if (decoded < 0) return false;
        valueLength = decoded;
        return true;
    }

    public static bool TryReadDynamicData<T>(this T stream, out byte[] data)
        where T : Stream
    {
        data = Array.Empty<byte>();
        if (!stream.TryReadValueSize(out long size) || size > MaxDynamicDataLength) return false;
        if (size == 0) return true;
        if (stream.CanSeek && size > stream.Length - stream.Position) return false;

        byte[] value = new byte[(int)size];
        if (!stream.TryFill(value)) return false;
        data = value;
        return true;
    }

    public static T WriteValueSize<T>(this T @this, in long valLen)
        where T : Stream
    {
        if (valLen < 0) throw new ArgumentOutOfRangeException(nameof(valLen)).Log();

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
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (data.Length > MaxDynamicDataLength) throw new ArgumentOutOfRangeException(nameof(data), "Dynamic values cannot exceed 256 MiB.");
        @this.WriteValueSize(data.LongLength);
        @this.Write(data, 0, data.Length);
    }
}
