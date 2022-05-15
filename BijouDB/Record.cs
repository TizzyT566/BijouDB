namespace BijouDB;

public class Record : IEqualityComparer<Record>
{
    private static readonly Dictionary<Type, Action<Record>> _removeDefinitions = new();

    private Guid? _id;
    public Guid Id => _id ??= IncrementalGuid.NextGuid();

    public static bool TryGet<R>(string id, out R? record)
        where R : Record, new() =>
        TryGet(Guid.Parse(id), out record);
    public static bool TryGet<R>(Guid id, out R? record)
        where R : Record, new()
    {
        try
        {
            string path = Path.Combine(Globals.DB_Path, typeof(R).FullName!, Globals.Rec, $"{id}.{Globals.Rec}");
            if (!File.Exists(path)) throw new FileNotFoundException("Record is missing.");
            record = new() { _id = id };
            return true;
        }
        catch (Exception ex)
        {
            if (Globals.Logging) Console.WriteLine(ex.ToString());
            record = null;
            return false;
        }
    }

    public static R[] GetAll<R>()
        where R : Record, new()
    {
        string path = Path.Combine(Globals.DB_Path, typeof(R).FullName!, Globals.Rec);
        if (!Directory.Exists(path)) return Array.Empty<R>();
        string[] records = Directory.GetFiles(path, Globals.RecPattern);
        R[] result = new R[records.Length];
        for (int i = 0; i < records.Length; i++)
        {
            string recordName = Path.GetFileNameWithoutExtension(records[i]);
            result[i] = new() { _id = Guid.Parse(recordName) };
        }
        return result;
    }

    public static R[] WithValues<R>(params R[][] columnMatches)
        where R : Record, new()
    {
        if (columnMatches.Length == 1)
            return columnMatches[0] is null ? Array.Empty<R>() : columnMatches[0];
        if (columnMatches.Length == 0) return Array.Empty<R>();

        // Turn all arrays into hashsets for fast lookup
        List<HashSet<R>> hashSets = new();
        foreach (R[] arr in columnMatches)
            if (arr is not null && arr.Length > 0) hashSets.Add(new HashSet<R>(arr));

        // start with the smallest collection
        hashSets.Sort((x, y) => x.Count.CompareTo(y.Count));

        HashSet<R> result = hashSets[0];

        for (int i = 1; i < hashSets.Count; i++)
            result.IntersectWith(hashSets[i]);

        return result.ToArray();
    }

    internal static void AddRemoveDefinition<R>(Action<Record> removeDefinition)
        where R : Record
    {
        Type type = typeof(R);
        if (_removeDefinitions.ContainsKey(type)) return;
        _removeDefinitions.Add(type, removeDefinition);
    }

    public bool TryRemove() => TryRemove(out _);
    public bool TryRemove(out Exception? exception)
    {
        try
        {
            Remove(this);
            exception = null;
            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            if (Globals.Logging) Console.WriteLine(ex.Message);
            return false;
        }
    }

    public void Remove() => Remove(this);
    public static void Remove(Record record)
    {
        if (!_removeDefinitions.TryGetValue(record.GetType(), out Action<Record>? removeDefinition) || removeDefinition is null)
            throw new Exception($"Missing 'Remove()' definition for {record.GetType().FullName}");
        removeDefinition(record);
    }

    public bool Equals(Record x, Record y) => Equals(x.Id, y.Id);

    public int GetHashCode(Record obj) => obj.Id.GetHashCode();
}