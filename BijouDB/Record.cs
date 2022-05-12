namespace BijouDB;

public class Record
{
    public Guid Id { get; init; } = IncrementalGuid.NextGuid();

    public static bool TryGet<R>(Guid id, out R? record) where R : Record, new()
    {
        try
        {
            string path = Path.Combine(Globals.DB_Path, typeof(R).FullName!, Globals.Rec, $"{id}.{Globals.Rec}");
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
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
        for(int i = 0; i < records.Length; i++)
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

    public void Remove()
    {
        Type type = GetType();

        // get all columns

        // check all references

        // loop through all columns and delete values for this record

        // delete record
    }
}
