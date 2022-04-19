namespace BijouDB;

public abstract class Tables
{
    private Guid? _guid;
    public Guid Id => _guid ?? Guid.Empty;

    public static long Length { get; }

    public void Assign() => _guid ??= IncrementalGuid.NextGuid();

    public static bool TryGet<T>(Guid id, out T? record) where T : Tables, new()
    {
        try
        {
            using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Rec, $"{id}.{Globals.Rec}"), FileMode.Open, FileAccess.Read, FileShare.Read);
            record = new();
            record._guid = id;
            return true;
        }
        catch (Exception ex)
        {
            if (Globals.Logging) Console.WriteLine(ex.ToString());
            record = null;
            return false;
        }
    }

    public void Delete() => Delete(this);

    public static void Delete<T>(T record) where T : Tables
    {
        // Delete logic here
    }
}
