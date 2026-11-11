namespace BijouDB;

internal static class IncrementalGuid
{
    private const int SPACING = 500;

    private static byte[]? _current, _reserved;
    private static int _remaining;

    private static void Increment(byte[] bytes, int amount)
    {
        for (int i = 0; i < bytes.Length && amount != 0; i++)
        {
            amount += bytes[i];
            bytes[i] = (byte)amount;
            amount >>= 8;
        }

        if (amount != 0) throw new OverflowException("The database ID space is exhausted.");
    }

    public static Guid NextGuid()
    {
        lock (Storage.SyncRoot)
        {
            Storage.EnsureInitialized();
            string path = Path.Combine(Globals.DatabasePath, "Guid.state");
            if (_current is null)
            {
                _current = File.Exists(path) ? File.ReadAllBytes(path) : new byte[16];
                if (_current.Length != 16) throw new InvalidDataException("Guid.state must contain exactly 16 bytes.");
                // Legacy versions saved every 500 IDs; skip their possible unsaved range.
                if (File.Exists(path))
                    Increment(_current, SPACING);
            }

            if (_remaining == 0)
            {
                _reserved = (byte[])_current.Clone();
                Increment(_reserved, SPACING);
                using MemoryStream data = new(_reserved, false);
                Storage.WriteAtomic(path, data);
                _remaining = SPACING;
            }

            Increment(_current, 1);
            _remaining--;
            return new Guid(_current);
        }
    }

    public static void Save()
    {
        lock (Storage.SyncRoot)
            if (_reserved is not null)
            {
                using MemoryStream data = new(_reserved, false);
                Storage.WriteAtomic(Path.Combine(Globals.DatabasePath, "Guid.state"), data);
            }
    }
}
