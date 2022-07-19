using System.Reflection;
using System.Runtime.Serialization;
using BijouDB.Exceptions;
using static BijouDB.Globals;

namespace BijouDB;

public abstract class Record : IEqualityComparer<Record>
{
    private static readonly Dictionary<Type, Action<Record>> _removeDefinitions = new();
    internal static readonly Dictionary<string, Type> _types = new();

    internal Guid? _id;
    /// <summary>
    /// The Record's Id.
    /// </summary>
    public Guid Id
    {
        get
        {
            if (_id is null)
            {
                try
                {
                    _id = IncrementalGuid.NextGuid();
                    string baseDir = Path.Combine(DatabasePath, GetType().FullName!, Rec);
                    Directory.CreateDirectory(baseDir);
                    string path = Path.Combine(baseDir, $"{Id}.{Rec}");
                    while (File.Exists(path))
                        _id = IncrementalGuid.NextGuid();
                    File.Create(path).Dispose();
                }
                catch (Exception ex)
                {
                    throw ex.Log();
                }
            }
            return (Guid)_id;
        }
    }

    static Record()
    {
        Type recordType = typeof(Record);
        foreach (Type type in Assembly.GetEntryAssembly().GetTypes())
            if (!type.IsAbstract && recordType.IsAssignableFrom(type) && !string.IsNullOrEmpty(type.FullName))
                _types.Add(type.FullName, type.IsPublic ? type : throw new NotPublicRecordException(type));
    }

    /// <summary>
    /// Gets the Json representation of this object.
    /// </summary>
    public string Json => this.ToJson();

    /// <summary>
    /// Tries to retrieve a Record via a string.
    /// </summary>
    /// <typeparam name="R">The type of Record.</typeparam>
    /// <param name="id">The Id of the Record to retrieve.</param>
    /// <param name="record">The Retrieved record.</param>
    /// <returns>true if Record was retrieved, otherwise false.</returns>
    public static bool TryGet<R>(string id, out R? record)
        where R : Record, new() =>
        TryGet(Guid.Parse(id), out record);

    /// <summary>
    /// Tries to retrieve a Record via a Guid.
    /// </summary>
    /// <typeparam name="R">The type of Record.</typeparam>
    /// <param name="id">The Id of the Record to retrieve.</param>
    /// <param name="record">The Retrieved record.</param>
    /// <returns>true if Record was retrieved, otherwise false.</returns>
    public static bool TryGet<R>(Guid id, out R? record)
        where R : Record, new()
    {
        try
        {
            string path = Path.Combine(DatabasePath, typeof(R).FullName!, Rec, $"{id}.{Rec}");
            if (File.Exists(path))
            {
                record = GetUninitializedRecord<R>(id);
                return true;
            }
        }
        catch (Exception ex)
        {
            ex.Log();
        }
        record = null;
        return false;
    }

    /// <summary>
    /// Gets all available Record types in the current application.
    /// </summary>
    /// <returns>A string array with the full name of available record types.</returns>
    public static string[] Types => _types.Keys.ToArray();

    /// <summary>
    /// Retrieves all property names for the current Record type.
    /// </summary>
    /// <returns>An array of property names for the Record type.</returns>
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
            foreach (string record in Directory.EnumerateFiles(path, RecPattern))
            {
                string recordName = Path.GetFileNameWithoutExtension(record);
                yield return GetUninitializedRecord<R>(Guid.Parse(recordName));
            }
    }

    /// <summary>
    /// Retrieve Records with the specified values.
    /// </summary>
    /// <typeparam name="R">The type of Record.</typeparam>
    /// <param name="columnMatches">Individual column matches obtained by WithValue() method.</param>
    /// <returns>Enumerable of all </returns>
    public static IEnumerable<R> WithValues<R>(params IEnumerable<R>[] columnMatches)
        where R : Record, new()
    {
        if (columnMatches.Length == 1)
            return columnMatches[0] is null ? Array.Empty<R>() : columnMatches[0];
        if (columnMatches.Length == 0) return Array.Empty<R>();

        // Turn all enumerables into hashsets for fast lookup
        List<HashSet<R>> hashSets = new();
        foreach (IEnumerable<R> arr in columnMatches)
            if (arr is not null) hashSets.Add(new HashSet<R>(arr));

        // start with the smallest collection
        hashSets.Sort((x, y) => x.Count.CompareTo(y.Count));

        HashSet<R> result = hashSets[0];

        for (int i = 1; i < hashSets.Count; i++)
            result.IntersectWith(hashSets[i]);

        return result;
    }

    /// <summary>
    /// Tries to remove a Record from the database.
    /// </summary>
    /// <param name="exception">Any exception which occurred.</param>
    /// <returns>true if Record was removed, otherwise false.</returns>
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
            ex.Log();
            return false;
        }
    }

    /// <summary>
    /// Checks if two records of equal.
    /// </summary>
    /// <param name="x">The first Record to check.</param>
    /// <param name="y">The second Record to check.</param>
    /// <returns></returns>
    public bool Equals(Record x, Record y)
    {
        if (x._id is null || y._id is null) return false;
        return x.GetType() == y.GetType() && Equals(x._id, y._id);
    }

    /// <summary>
    /// The hash code for the Record.
    /// </summary>
    /// <param name="obj">The Record to get the hash code for.</param>
    /// <returns>The hash code.</returns>
    public int GetHashCode(Record obj) => obj._id.GetHashCode();

    internal static void Remove<R>(R record) where R : Record
    {
        if (_removeDefinitions.TryGetValue(record.GetType(), out Action<Record>? removeDefinition) && removeDefinition is not null)
            removeDefinition(record);
    }

    internal static void AddRemoveDefinition<R>(Action<Record> removeDefinition)
        where R : Record
    {
        Type type = typeof(R);
        if (_removeDefinitions.ContainsKey(type)) return;
        _removeDefinitions.Add(type, removeDefinition);
    }

    private static R GetUninitializedRecord<R>(Guid id) where R : Record, new()
    {
        if (id == Guid.Empty) throw new ArgumentException("Record id cannot be empty.");
        object obj = FormatterServices.GetUninitializedObject(typeof(R));
        Type type = obj.GetType();
        FieldInfo field = type.GetField("_id", BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(obj, id);
        return (R)obj;
    }
}
