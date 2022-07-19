using BijouDB.Components;

namespace BijouDB;

public static class SkipHash
{
    /// <summary>
    /// Generates a hash that skip portions of the data for speed.
    /// </summary>
    /// <param name="this">The seekable stream to hash from.</param>
    /// <param name="samples">The number of samples to use to generate the SkipHash. If omitted a value will be calculated based on the length of data to hash.</param>
    /// <returns>A Guid which represents the SkipHash.</returns>
    /// <remarks>Inputs of 256 bytes or less will be read in its entirety.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static ulong GetSkipHash(this Stream @this)
    {
        long hashingSize = @this.Length - @this.Position; // Intentionally throws NotSupportedException if stream isn't seekable
        int _samples;

        if (hashingSize <= 256) return BitConverter.ToUInt64(CityHash.Compute(@this), 0);
        else _samples = (int)(((hashingSize - 32) / Math.Sqrt(hashingSize)) + 2);

        double position = 0, offset = (hashingSize - 16.0) / (_samples - 1.0);
        byte[] buffer = new byte[16];
        using FileBackedStream ms = new();

        while (@this.Position < hashingSize)
        {
            int total = 0, read;
            while (((total += read = @this.Read(buffer, total, 16 - total)) < 16) && (read != 0)) ;
            ms.Write(buffer, 0, buffer.Length);
            position += offset;
            @this.Position = (long)Math.Round(position);
        }
        ms.Position = 0;
        return BitConverter.ToUInt64(CityHash.Compute(ms), 0);
    }
}
