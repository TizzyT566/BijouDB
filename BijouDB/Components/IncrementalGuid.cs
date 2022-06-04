namespace BijouDB;

internal static class IncrementalGuid
{
    private const int SPACING = 500;
    private static readonly FileStream _saveStream;
    private static readonly byte[] _guidBytes;
    private static string SavePath => @$"{Globals.DatabasePath}\Guid.state";
    private static int _lock = 0;
    private static int _count = 0;

    static IncrementalGuid()
    {
        try
        {
            _ = Directory.CreateDirectory(Globals.DatabasePath);
        }
        catch (Exception ex)
        {
            ex.Log();
            throw ex;
        }

        if (File.Exists(SavePath))
        {
            _saveStream = new(SavePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            _guidBytes = new byte[16];
            if (!_saveStream.TryFill(_guidBytes))
            {
                Exception ex = new($"{nameof(SavePath)} must be 16 bytes in size.");
                ex.Log();
                throw ex;
            }
            Increment(SPACING);
        }
        else
        {
            _saveStream = new(SavePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            _guidBytes = new byte[16];
        }
        Save();
    }

    private static void Increment(int amount)
    {
        // Generate longs
        long l1 = BitConverter.ToInt64(_guidBytes, 0), l2 = BitConverter.ToInt64(_guidBytes, 8);
        // Increment check
        long remaining = long.MaxValue - l1;
        if (remaining < amount) l2++;
        l1 += amount;
        // Write bytes back
        byte[] l1Bytes = BitConverter.GetBytes(l1), l2Bytes = BitConverter.GetBytes(l2);
        Array.Copy(l1Bytes, 0, _guidBytes, 0, l1Bytes.Length);
        Array.Copy(l2Bytes, 0, _guidBytes, 8, l2Bytes.Length);
    }

    public static void Save()
    {
        while (Interlocked.Exchange(ref _lock, 1) == 1) ;
        try
        {
            _saveStream.Position = 0;
            _saveStream.Write(_guidBytes, 0, 16);
            _saveStream.Flush();
        }
        catch (Exception ex)
        {
            ex.Log();
        }
        finally
        {
            Interlocked.Exchange(ref _lock, 0);
        }
    }

    public static Guid NextGuid()
    {
        while (Interlocked.Exchange(ref _lock, 1) == 1) ;
        Increment(1);
        Guid guid = new(_guidBytes);
        Interlocked.Exchange(ref _lock, 0);
        Interlocked.Increment(ref _count);
        if (Interlocked.CompareExchange(ref _count, 0, SPACING) == SPACING) Save();
        return guid;
    }
}
