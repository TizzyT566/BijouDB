﻿using BijouDB.Exceptions;

namespace BijouDB;

public static class Misc
{
    public static string EnsureAlphaNumeric(string str, bool @throw = false)
    {
        int i = 0;
        char[] chars = new char[str.Length];
        foreach (char c in str)
            if (char.IsLetterOrDigit(c)) chars[i++] = c;
            else if (@throw) throw new NotAlphaNumericException(str);
        return new(chars);
    }

    public static bool TryFill(this Stream @this, byte[] buffer, int offset = 0, int length = -1)
    {
        if (length < 1) length = buffer.Length;
        int total = 0, read;
        while ((total += read = @this.Read(buffer, total + offset, length - total)) < length)
            if (read == 0) return false;
        return total == length;
    }

    public static bool TryReadValueSize<T>(this T @this, out long valLen) where T : Stream
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

    public static bool TryReadDynamicData<T>(this T @this, out byte[] data) where T : Stream
    {
        if (@this.TryReadValueSize(out long size))
        {
            data = new byte[size];
            return @this.TryFill(data);
        }
        data = Array.Empty<byte>();
        return false;
    }

    public static T WriteValueSize<T>(this T @this, in long valLen) where T : Stream
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
                    @this.Write(bytes);
                    break;
                }
        }
        return @this;
    }

    public static void WriteDynamicData<T>(this T @this, in byte[] data) where T : Stream
    {
        @this.WriteValueSize(data.LongLength);
        @this.Write(data);
    }
}
