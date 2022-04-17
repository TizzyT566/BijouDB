using System.Security.Cryptography;

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
    public static Guid GetSkipHash(this Stream @this)
    {
        long hashingSize = @this.Length - @this.Position; // Intentionally throws NotSupportedException if stream isn't seekable
        using MD5 md5 = MD5.Create();
        int _samples;

        if (hashingSize <= 256) return new(md5.ComputeHash(@this));
        else _samples = (int)(((hashingSize - 32) / Math.Sqrt(hashingSize)) + 2);

        double position = 0, offset = (hashingSize - 16.0) / (_samples - 1.0);
        byte[] buffer = new byte[16];
        using MemoryStream ms = new();

        while (@this.Position < hashingSize)
        {
            int total = 0, read;
            while (((total += read = @this.Read(buffer, total, 16 - total)) < 16) && (read != 0)) ;
            ms.Write(buffer);
            position += offset;
            @this.Position = (long)Math.Round(position);
        }
        ms.Position = 0;
        return new(md5.ComputeHash(ms));
    }
}
