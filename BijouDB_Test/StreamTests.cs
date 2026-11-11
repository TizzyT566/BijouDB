using BijouDB;
using BijouDB.Components;

namespace BijouDB_Test;

internal static class StreamTests
{
    public static void Register(Action<string, Action> test)
    {
        test("LRU promotion, eviction, update and removal", CacheRecency);
        test("LRU handles concurrent access", CacheConcurrency);
        test("Stream comparison handles chunk boundaries and short reads", StreamComparison);
        test("Masking preserves input, offsets and legacy bytes", MaskedWrites);
        test("Masking handles short reads without advancing past returned data", MaskedReads);
        test("Masking rejects unsupported seeking and invalid buffers", MaskedContract);
        test("File-backed stream spills large values and preserves overwrite position", FileSpill);
        test("File-backed stream SetLength and Close obey Stream contract", FileLifetime);
        test("Temporary files are removed on disposal", TemporaryFiles);
        test("Variable length encoding remains compatible at every boundary", LengthEncoding);
        test("Truncated and excessive dynamic lengths are rejected", InvalidDynamicLengths);
        test("TryFill honors offsets, zero length and partial reads", Filling);
    }

    private static void CacheRecency()
    {
        Task operation = Task.Run(() =>
        {
            foreach (int capacity in new[] { 1, 2, 3, 128 })
            {
                Cache<@int> cache = new(capacity);
                List<Guid> recency = new();
                Dictionary<Guid, int> expected = new();
                Guid[] keys = Enumerable.Range(0, capacity + 5).Select(_ => Guid.NewGuid()).ToArray();
                Random random = new(147);
                for (int i = 0; i < 2000; i++)
                {
                    Guid key = keys[random.Next(keys.Length)];
                    switch (random.Next(3))
                    {
                        case 0:
                            cache.Set(key, i);
                            if (!expected.ContainsKey(key) && expected.Count == capacity)
                            {
                                expected.Remove(recency[recency.Count - 1]);
                                recency.RemoveAt(recency.Count - 1);
                            }

                            expected[key] = i;
                            recency.Remove(key);
                            recency.Insert(0, key);
                            break;
                        case 1:
                            bool found = cache.TryGet(key, out @int value);
                            TestAssert.Equal(expected.TryGetValue(key, out int number), found);
                            if (found)
                            {
                                TestAssert.Equal(number, (int)value);
                                recency.Remove(key);
                                recency.Insert(0, key);
                            }

                            break;
                        default:
                            cache.Remove(key);
                            expected.Remove(key);
                            recency.Remove(key);
                            break;
                    }
                }
            }
        });
        TestAssert.True(operation.Wait(TimeSpan.FromSeconds(10)), "Cache operations did not complete.");
        operation.GetAwaiter().GetResult();
        TestAssert.Throws<ArgumentOutOfRangeException>(() => new Cache<@int>(0));
    }

    private static void CacheConcurrency()
    {
        Cache<@int> cache = new(3);
        Guid[] keys = Enumerable.Range(0, 16).Select(_ => Guid.NewGuid()).ToArray();
        Task operation = Task.Run(() => Parallel.For(0, 2000, i =>
        {
            Guid key = keys[i % keys.Length];
            cache.Set(key, i);
            cache.TryGet(key, out _);
            if (i % 5 == 0)
                cache.Remove(key);
        }));
        TestAssert.True(operation.Wait(TimeSpan.FromSeconds(10)), "Concurrent cache operations did not complete.");
        operation.GetAwaiter().GetResult();
    }

    private static void StreamComparison()
    {
        foreach (int length in new[] { 0, 1, 8191, 8192, 8193, 16384, 90000 })
        {
            byte[] data = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();
            using ShortReadStream first = new(data, 3);
            using ShortReadStream second = new(data, 7);
            TestAssert.True(Misc.StreamCompare(first, second));
            using MemoryStream longer = new(data.Concat(new byte[] { 1 }).ToArray());
            using MemoryStream same = new(data);
            TestAssert.True(!Misc.StreamCompare(longer, same));
            if (length == 0) continue;

            byte[] changed = (byte[])data.Clone();
            changed[length - 1] ^= 1;
            using MemoryStream original = new(data);
            using MemoryStream different = new(changed);
            TestAssert.True(!Misc.StreamCompare(original, different));
        }
    }

    private static void MaskedWrites()
    {
        byte[] data = { 1, 2, 3, 4, 5 };
        byte[] original = (byte[])data.Clone();
        using MemoryStream raw = new();
        using (MaskedStream masked = new(raw, 712247))
            masked.Write(data, 0, data.Length);
        TestAssert.SequenceEqual(original, data);
        TestAssert.SequenceEqual(new byte[] { 176, 241, 249, 43, 161 }, raw.ToArray());
        using MemoryStream withOffset = new();
        using (MaskedStream masked = new(withOffset, 712247))
            masked.Write(new byte[] { 0, 0, 1, 2, 3, 4, 5, 0 }, 2, 5);
        TestAssert.SequenceEqual(raw.ToArray(), withOffset.ToArray());
        TestAssert.True(raw.CanWrite, "Disposing a masking view must leave its owner's stream open.");
    }

    private static void MaskedReads()
    {
        byte[] expected = Enumerable.Range(0, 21000).Select(i => (byte)(i * 7)).ToArray();
        using MemoryStream raw = new();
        using (MaskedStream writer = new(raw, 57))
            writer.Write(expected, 0, expected.Length);
        using ShortReadStream shortReads = new(raw.ToArray(), 2);
        using MaskedStream reader = new(shortReads, 57);
        byte[] output = Enumerable.Repeat((byte)99, expected.Length + 2).ToArray();
        TestAssert.True(reader.TryFill(output, 1, expected.Length));
        TestAssert.Equal((byte)99, output[0]);
        TestAssert.Equal((byte)99, output[output.Length - 1]);
        TestAssert.SequenceEqual(expected, output.Skip(1).Take(expected.Length));
        TestAssert.Equal(-1, reader.ReadByte());
        TestAssert.Equal(0, reader.Read(output, 0, output.Length));
    }

    private static void MaskedContract()
    {
        using MemoryStream raw = new();
        using MaskedStream stream = new(raw, 0);
        TestAssert.True(!stream.CanSeek);
        TestAssert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        TestAssert.Throws<NotSupportedException>(() => stream.Position = 0);
        TestAssert.Throws<ArgumentNullException>(() => stream.Write(null!, 0, 0));
        TestAssert.Throws<ArgumentOutOfRangeException>(() => stream.Write(new byte[2], -1, 1));
        TestAssert.Throws<ArgumentOutOfRangeException>(() => _ = stream.Read(new byte[2], 0, -1));
        TestAssert.Throws<ArgumentException>(() => stream.Write(new byte[2], 1, 2));
        // Rejected writes must not consume mask state.
        stream.WriteByte(1);
        TestAssert.SequenceEqual(new byte[] { 39 }, raw.ToArray());
    }

    private static void FileSpill()
    {
        byte[] large = Enumerable.Range(0, 1048577).Select(i => (byte)i).ToArray();
        using FileBackedStream stream = new();
        stream.Write(large, 0, large.Length);
        stream.Position = 0;
        byte[] actual = new byte[large.Length];
        TestAssert.True(stream.TryFill(actual));
        TestAssert.SequenceEqual(large, actual);
        using FileBackedStream overwrite = new(8);
        overwrite.Write(new byte[] { 1, 2, 3, 4, 5, 6 }, 0, 6);
        overwrite.Position = 2;
        overwrite.Write(Enumerable.Repeat((byte)9, 10).ToArray(), 0, 10);
        TestAssert.Equal(12L, overwrite.Position);
        TestAssert.Equal(12L, overwrite.Length);
        overwrite.Position = 0;
        byte[] changed = new byte[12];
        TestAssert.True(overwrite.TryFill(changed));
        TestAssert.SequenceEqual(new byte[] { 1, 2 }.Concat(Enumerable.Repeat((byte)9, 10)), changed);
    }

    private static void FileLifetime()
    {
        Stream stream = new FileBackedStream(4);
        stream.WriteByte(7);
        stream.Position = 1;
        stream.SetLength(100);
        TestAssert.Equal(1L, stream.Position);
        TestAssert.Equal(100L, stream.Length);
        stream.Position = 0;
        TestAssert.Equal(7, stream.ReadByte());
        TestAssert.Equal(0, stream.ReadByte());
        stream.Close();
        TestAssert.True(!stream.CanWrite && !stream.CanRead && !stream.CanSeek);
        TestAssert.Throws<ObjectDisposedException>(() => stream.WriteByte(8));
        stream.Dispose();
        TestAssert.Throws<ArgumentOutOfRangeException>(() => new FileBackedStream(-1));
    }

    private static void TemporaryFiles()
    {
        string path;
        using (FileStream stream = Misc.GetTemporaryFileStream())
        {
            path = stream.Name;
            TestAssert.True(File.Exists(path));
        }

        TestAssert.True(!File.Exists(path));
        TestAssert.Throws<ArgumentOutOfRangeException>(() => Misc.GetTemporaryFileStream(0));
    }

    private static void LengthEncoding()
    {
        foreach (long length in new long[]
        {
            0,
            1,
            127,
            128,
            16383,
            16384,
            1048575,
            1048576,
            268435455,
            268435456,
            34359738367,
            34359738368,
            4398046511103,
            4398046511104,
            562949953421311,
            562949953421312,
            72057594037927935,
            72057594037927936,
            long.MaxValue
        })
        {
            using MemoryStream stream = new();
            stream.WriteValueSize(length);
            byte[] encoded = stream.ToArray();
            stream.Position = 0;
            TestAssert.True(stream.TryReadValueSize(out long restored));
            TestAssert.Equal(length, restored);
            TestAssert.Equal(stream.Length, stream.Position);
            if (encoded.Length > 1)
            {
                using MemoryStream truncated = new(encoded.Take(encoded.Length - 1).ToArray());
                TestAssert.True(!truncated.TryReadValueSize(out _));
            }
        }
    }

    private static void InvalidDynamicLengths()
    {
        for (int prefix = 128; prefix <= 255; prefix++)
        {
            using MemoryStream truncated = new(new[] { (byte)prefix });
            TestAssert.True(!truncated.TryReadDynamicData(out _));
        }

        using MemoryStream negative = new(Enumerable.Repeat((byte)255, 9).ToArray());
        TestAssert.True(!negative.TryReadValueSize(out _));
        using MemoryStream excessive = new();
        excessive.WriteValueSize(Misc.MaxDynamicDataLength + 1L);
        excessive.Position = 0;
        TestAssert.True(!excessive.TryReadDynamicData(out _));
        using MemoryStream missing = new(new byte[] { 4, 1, 2 });
        TestAssert.True(!missing.TryReadDynamicData(out _));
        using MemoryStream empty = new(new byte[] { 0 });
        TestAssert.True(empty.TryReadDynamicData(out byte[] value));
        TestAssert.Equal(0, value.Length);
    }

    private static void Filling()
    {
        using ShortReadStream source = new(new byte[] { 1, 2, 3 }, 1);
        byte[] output = { 9, 9, 9, 9, 9 };
        TestAssert.True(source.TryFill(output, 1, 3));
        TestAssert.SequenceEqual(new byte[] { 9, 1, 2, 3, 9 }, output);
        TestAssert.True(source.TryFill(output, 5, 0));
        TestAssert.True(!source.TryFill(output, 0, 1));
        TestAssert.Throws<ArgumentOutOfRangeException>(() => source.TryFill(output, 4, 2));
    }

    private sealed class ShortReadStream : MemoryStream
    {
        private readonly int _chunk;

        public ShortReadStream(byte[] bytes, int chunk) : base(bytes) => _chunk = chunk;

        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, _chunk));
    }
}
