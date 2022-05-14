namespace BijouDB;

public class Record : IEqualityComparer<Record>
{
    private static readonly Dictionary<Type, Action<Record>> _removeDefinitions = new();

    public Guid Id { get; }

    public Record() => Id = IncrementalGuid.NextGuid();

    public Record(Guid id) => Id = id;

    public static bool TryGet<R>(string id, out R? record) where R : Record, new() =>
        TryGet(Guid.Parse(id), out record);
    public static bool TryGet<R>(Guid id, out R? record) where R : Record, new()
    {
        try
        {
            string path = Path.Combine(Globals.DB_Path, typeof(R).FullName!, Globals.Rec, $"{id}.{Globals.Rec}");
            if (!File.Exists(path)) throw new FileNotFoundException("Record is missing.");
            record = (R)new Record(id);
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
            result[i] = (R)new Record(Guid.Parse(recordName));
        }
        return result;
    }

    public static R[] RecordsWithValues<R>(params R[][] columnMatches) where R : Record, new()
    {
        if (columnMatches.Length == 1) return columnMatches[0];
        HashSet<R> result = new();
        if (columnMatches.Length == 0) return result.ToArray();
        Array.Sort(columnMatches, (x, y) => x.Length - y.Length);
        foreach (R id in columnMatches[0])
        {
            int i = 1;
            for (; i < columnMatches.Length; i++)
                if (!columnMatches[i].Contains(id))
                    break;
            if (i == columnMatches.Length)
                result.Add(id);
        }
        return result.ToArray();
    }

    internal static void AddRemoveDefinition<R>(Action<Record> removeDefinition) where R : Record
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

    public bool Equals(Record x, Record y) => Guid.Equals(x.Id, y.Id);

    public int GetHashCode(Record obj) => obj.Id.GetHashCode();
}