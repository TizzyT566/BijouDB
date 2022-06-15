namespace BijouDB.Components;

public static class CityHash
{
    private const ulong K0 = 0xc3a5c85c97cb3127, K1 = 0xb492b66fbe98f273, K2 = 0x9ae16a3b2f90404f;

    public static byte[] Compute(Stream data)
    {
        ulong hashValue = data.Length switch
        {
            > 64 => Hash64(data),
            > 32 => Hash32(data),
            > 16 => Hash16(data),
            _ => Hash(data)
        };

        return BitConverter.GetBytes(hashValue);
    }

    private static ulong Hash128_64(ulong[] x)
    {
        const ulong kMul = 0x9ddfea08eb382d69;

        ulong a = (x[0] ^ x[1]) * kMul;
        a ^= a >> 47;

        ulong b = (x[1] ^ a) * kMul;
        b ^= b >> 47;
        b *= kMul;

        return b;
    }

    private static ulong Hash16(ulong u, ulong v, ulong mul)
    {
        ulong a = (u ^ v) * mul;
        a ^= a >> 47;

        ulong b = (v ^ a) * mul;
        b ^= b >> 47;
        b *= mul;

        return b;
    }

    private static ulong RotateRight(ulong operand, int shiftCount)
    {
        shiftCount &= 0x3f;
        return (operand >> shiftCount) | (operand << (64 - shiftCount));
    }

    private static ulong Hash(Stream data)
    {
        byte[] group = data.ReadBytes((int)data.Length, out _);

        if (group.Length >= 8)
        {
            ulong mul = K2 + (ulong)group.Length * 2;
            ulong a = BitConverter.ToUInt64(group, 0) + K2;
            ulong b = BitConverter.ToUInt64(group, group.Length - 8);
            ulong c = RotateRight(b, 37) * mul + a;
            ulong d = (RotateRight(a, 25) + b) * mul;

            return Hash16(c, d, mul);
        }

        if (group.Length >= 4)
        {
            ulong mul = K2 + (ulong)group.Length * 2;
            ulong a = BitConverter.ToUInt32(group, 0);
            return Hash16((ulong)group.Length + (a << 3), BitConverter.ToUInt32(group, group.Length - 4), mul);
        }

        if (group.Length > 0)
        {
            byte a = group[0];
            byte b = group[group.Length >> 1];
            byte c = group[group.Length - 1];

            uint y = a + ((uint)b << 8);
            uint z = (uint)group.Length + ((uint)c << 2);

            return Mix(y * K2 ^ z * K0) * K2;
        }

        return K2;
    }

    private static ulong Hash16(Stream data)
    {
        byte[] group = data.ReadBytes((int)data.Length, out _);

        ulong mul = K2 + (ulong)data.Length * 2;
        ulong a = BitConverter.ToUInt64(group, 0) * K1;
        ulong b = BitConverter.ToUInt64(group, 8);
        ulong c = BitConverter.ToUInt64(group, (int)(data.Length - 8)) * mul;
        ulong d = BitConverter.ToUInt64(group, (int)(data.Length - 16)) * K2;

        return Hash16(RotateRight(a + b, 43) + RotateRight(c, 30) + d, a + RotateRight(b + K2, 18) + c, mul);
    }

    private static ulong[] WeakHashLen32WithSeeds(
        ulong w, ulong x, ulong y, ulong z, ulong a, ulong b)
    {
        a += w;
        b = RotateRight(b + a + z, 21);

        ulong c = a;
        a += x;
        a += y;

        b += RotateRight(a, 44);

        return new ulong[] { a + z, b + c };
    }

    private static ulong[] WeakHashLen32WithSeeds(byte[] data, int startIndex, ulong a, ulong b)
    {
        return WeakHashLen32WithSeeds(
            BitConverter.ToUInt64(data, startIndex),
            BitConverter.ToUInt64(data, startIndex + 8),
            BitConverter.ToUInt64(data, startIndex + 16),
            BitConverter.ToUInt64(data, startIndex + 24),
            a,
            b);
    }

    private static ulong Hash32(Stream data)
    {
        byte[] group = data.ReadBytes((int)data.Length, out int _);

        ulong mul = K2 + (ulong)data.Length * 2;
        ulong a = BitConverter.ToUInt64(group, 0) * K2;
        ulong b = BitConverter.ToUInt64(group, 8);
        ulong c = BitConverter.ToUInt64(group, (int)(data.Length - 24));
        ulong d = BitConverter.ToUInt64(group, (int)(data.Length - 32));
        ulong e = BitConverter.ToUInt64(group, 16) * K2;
        ulong f = BitConverter.ToUInt64(group, 24) * 9;
        ulong g = BitConverter.ToUInt64(group, (int)(data.Length - 8));
        ulong h = BitConverter.ToUInt64(group, (int)(data.Length - 16)) * mul;

        ulong u = RotateRight(a + g, 43) + (RotateRight(b, 30) + c) * 9;
        ulong v = ((a + g) ^ d) + f + 1;
        ulong w = ReverseByteOrder((u + v) * mul) + h;
        ulong x = RotateRight(e + f, 42) + c;
        ulong y = (ReverseByteOrder((v + w) * mul) + g) * mul;
        ulong z = e + f + c;

        a = ReverseByteOrder((x + z) * mul + y) + b;
        b = Mix((z + a) * mul + d + h) * mul;
        return b + x;
    }

    private static ulong Mix(ulong value) => value ^ (value >> 47);

    private static ulong ReverseByteOrder(ulong operand)
    {
        return
            (operand >> 56) |
            ((operand & 0x00ff000000000000) >> 40) |
            ((operand & 0x0000ff0000000000) >> 24) |
            ((operand & 0x000000ff00000000) >> 8) |
            ((operand & 0x00000000ff000000) << 8) |
            ((operand & 0x0000000000ff0000) << 24) |
            ((operand & 0x000000000000ff00) << 40) |
            (operand << 56);
    }

    private static ulong Hash64(Stream data)
    {
        var group = data.ReadBytes(data.Length - 64, 64, out _);

        ulong x = BitConverter.ToUInt64(group, 24);
        ulong y = BitConverter.ToUInt64(group, 48) + BitConverter.ToUInt64(group, 8);
        ulong z = Hash128_64(new ulong[] { BitConverter.ToUInt64(group, 16) + (ulong)data.Length, BitConverter.ToUInt64(group, 40) });

        ulong[] v = WeakHashLen32WithSeeds(group, 0, (ulong)data.Length, z);
        ulong[] w = WeakHashLen32WithSeeds(group, 32, y + K1, x);

        x = x * K1 + BitConverter.ToUInt64(data.ReadBytes(0, 8, out _), 0);

        long groupEndOffset = data.Length - (data.Length % 64);

        for (long currentOffset = 0; currentOffset < groupEndOffset; currentOffset += 64)
        {
            group = data.ReadBytes(64, out _);

            x = RotateRight(x + y + v[0] + BitConverter.ToUInt64(group, 8), 37) * K1;
            y = RotateRight(y + v[1] + BitConverter.ToUInt64(group, 48), 42) * K1;
            x ^= w[1];
            y += v[0] + BitConverter.ToUInt64(group, 40);
            z = RotateRight(z + w[0], 33) * K1;
            v = WeakHashLen32WithSeeds(group, 0, v[1] * K1, x + w[0]);
            w = WeakHashLen32WithSeeds(group, 32, z + w[1], y + BitConverter.ToUInt64(group, 16));

            (z, x) = (x, z);
        }

        return Hash128_64(new ulong[] { Hash128_64(new ulong[] { v[0], w[0] }) + Mix(y) * K1 + z, Hash128_64(new ulong[] { v[1], w[1] }) + x });
    }

    private static byte[] ReadBytes(this Stream @this, int length, out int read)
    {
        byte[] result = new byte[length];
        read = @this.Read(result, 0, length);
        return result;
    }

    private static byte[] ReadBytes(this Stream @this, long position, int length, out int read)
    {
        byte[] result = new byte[length];
        @this.Position = position;
        read = @this.Read(result, 0, length);
        return result;
    }
}
