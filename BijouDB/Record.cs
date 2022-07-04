using System.Reflection;
using static BijouDB.Globals;

namespace BijouDB;

public abstract class Record : IEqualityComparer<Record>
{
    private static readonly Dictionary<Type, Action<Record>> _removeDefinitions = new();
    internal static readonly Dictionary<string, Type> _types = new();

    internal Guid? _id;
    public Guid Id
    {
        get
        {
            if (_id is null)
            {
                _id ??= IncrementalGuid.NextGuid();
                try
                {
                    string baseDir = Path.Combine(DatabasePath, GetType().FullName!, Rec);
                    Directory.CreateDirectory(baseDir);
                    string path = Path.Combine(baseDir, $"{Id}.{Rec}");
                    if (!File.Exists(path))
                    {
                        File.Create(path).Dispose();
                    }
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }
            return (Guid)_id;
        }
    }

    public string Json => this.ToJson();

    static Record()
    {
        Type recordType = typeof(Record);
        foreach (Type type in Assembly.GetEntryAssembly().GetTypes())
            if (!type.IsAbstract && recordType.IsAssignableFrom(type) && !string.IsNullOrEmpty(type.FullName))
                _types.Add(type.FullName, type);
    }

    public static bool TryGet<R>(string id, out R? record)
        where R : Record, new() =>
        TryGet(Guid.Parse(id), out record);
    public static bool TryGet<R>(Guid id, out R? record)
        where R : Record, new()
    {
        try
        {
            string path = Path.Combine(DatabasePath, typeof(R).FullName!, Rec, $"{id}.{Rec}");
            record = new() { _id = id };
            return true;
        }
        catch (Exception ex)
        {
            ex.Log();
            record = null;
            return false;
        }
    }

    /// <summary>
    /// Gets all available Record types in the current application.
    /// </summary>
    /// <returns>A string array with the full name of available record types.</returns>
    public static string[] Types => _types.Values.Select(t => t.FullName).ToArray();

    public string[] PropertyNames()
    {
        Type type = GetType();
        PropertyInfo[] props = type.GetProperties();
        return props.Select(p => $"{type.FullName}.{p.Name}").ToArray();
    }

    /// <summary>
    /// Get all entries for a particular record.
    /// </summary>
    /// <typeparam name="R">The record type.</typeparam>
    /// <returns>An enumerable for all records of a particular type.</returns>
    public static IEnumerable<R> GetAll<R>()
        where R : Record, new()
    {
        string path = Path.Combine(DatabasePath, typeof(R).FullName!, Rec);
        if (Directory.Exists(path))
        {
            foreach (string record in Directory.EnumerateFiles(path, RecPattern))
            {
                string recordName = Path.GetFileNameWithoutExtension(record);
                yield return new() { _id = Guid.Parse(recordName) };
            }
        }
    }

    public static IEnumerable<R> WithValues<R>(params IEnumerable<R>[] columnMatches)
        where R : Record, new()
    {
        if (columnMatches.Length == 1)
            return columnMatches[0] is null ? Array.Empty<R>() : columnMatches[0];
        if (columnMatches.Length == 0) return Array.Empty<R>();

        // Turn all arrays into hashsets for fast lookup
        List<HashSet<R>> hashSets = new();
        foreach (IEnumerable<R> arr in columnMatches)
            if (arr is not null) hashSets.Add(new HashSet<R>(arr));

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
            if (Logging) Console.WriteLine(ex.Message);
            return false;
        }
    }

    public void Remove() => Remove(this);
    public static void Remove<R>(R record) where R : Record
    {
        if (_removeDefinitions.TryGetValue(record.GetType(), out Action<Record>? removeDefinition) && removeDefinition is not null)
            removeDefinition(record);
    }

    public bool Equals(Record x, Record y) => x.GetType() == y.GetType() && Equals(x.Id, y.Id);
    public int GetHashCode(Record obj) => obj.Id.GetHashCode();
}
