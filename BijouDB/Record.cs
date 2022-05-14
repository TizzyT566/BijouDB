namespace BijouDB;

public abstract class Record
{
    private static readonly Dictionary<Type, Action<Record>> _removeDefinitions = new();

    public Guid Id { get; init; } = IncrementalGuid.NextGuid();

    public static bool TryGet<R>(string id, out R? record) where R : Record, new() =>
        TryGet(Guid.Parse(id), out record);
    public static bool TryGet<R>(Guid id, out R? record) where R : Record, new()
    {
        try
        {
            string path = Path.Combine(Globals.DB_Path, typeof(R).FullName!, Globals.Rec, $"{id}.{Globals.Rec}");
            if (!File.Exists(path)) throw new FileNotFoundException("Record is missing.");
            record = new() { Id = id };
            return true;
        }
        catch (Exception ex)
        {
            if (Globals.Logging) Console.WriteLine(ex.ToString());
            record = null;
            return false;
        }
    }

    public static R[] GetAll<R>() where R : Record, new()
    {
        string path = Path.Combine(Globals.DB_Path, typeof(R).FullName!, Globals.Rec);
        if (!Directory.Exists(path)) return Array.Empty<R>();
        string[] records = Directory.GetFiles(path, Globals.RecPattern);
        R[] result = new R[records.Length];
        for (int i = 0; i < records.Length; i++)
        {
            string recordName = Path.GetFileNameWithoutExtension(records[i]);
            result[i] = new() { Id = Guid.Parse(recordName) };
        }
        return result;
    }

    public static IReadOnlySet<Guid> RecordsWithValues(params IReadOnlySet<Guid>[] columnMatches)
    {
        if (columnMatches.Length == 1) return columnMatches[0];
        HashSet<Guid> result = new();
        if (columnMatches.Length == 0) return result;
        Array.Sort(columnMatches, (x, y) => x.Count - y.Count);
        foreach (Guid id in columnMatches[0])
        {
            int i = 1;
            for (; i < columnMatches.Length; i++)
                if (!columnMatches[i].Contains(id))
                    break;
            if (i == columnMatches.Length)
                result.Add(id);
        }
        return result;
    }

    internal static void AddRemoveDefinition<R>(Action<Record> removeDefinition) where R : Record =>
        _removeDefinitions.TryAdd(typeof(R), removeDefinition);

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
}