namespace BijouDB;

public abstract partial class Table
{
    private Guid? _guid;
    public Guid Id => _guid ?? Guid.Empty;
    public bool OnDisk => Id != Guid.Empty;

    public void Assign() => _guid ??= IncrementalGuid.NextGuid();

    public static bool TryGet<T>(Guid id, out T? record) where T : Table, new()
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

    public static IReadOnlyDictionary<Guid, T> RecordsWithValues<T>(params IReadOnlyDictionary<Guid, T>[] columnMatches) where T : Table, new()
    {
        if (columnMatches.Length == 1) return columnMatches[0];
        Dictionary<Guid, T> result = new();
        Array.Sort(columnMatches, (x, y) => x.Count - y.Count);
        foreach (KeyValuePair<Guid, T> pair in columnMatches[0])
        {
            int i = 1;
            for (; i < columnMatches.Length; i++)
                if (!columnMatches[i].ContainsKey(pair.Key))
                    break;
            if (i == columnMatches.Length)
                result.Add(pair.Key, pair.Value);
        }
        return result;
    }
}
