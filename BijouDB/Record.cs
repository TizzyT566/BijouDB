﻿using System.Reflection;

namespace BijouDB;

public abstract class Record : IEqualityComparer<Record>
{
    private static readonly Dictionary<Type, Action<Record>> _removeDefinitions = new();
    private static readonly Dictionary<string, Type> _types = new();

    private Guid? _id;
    public Guid Id
    {
        get
        {
            if (_id is null)
            {
                _id ??= IncrementalGuid.NextGuid();
                string baseDir = Path.Combine(Globals.DatabasePath, GetType().FullName!, Globals.Rec);
                Directory.CreateDirectory(baseDir);
                File.Create(Path.Combine(baseDir, $"{Id}.{Globals.Rec}")).Dispose();
            }
            return (Guid)_id;
        }
    }

    public string Json => BijouDB.Json.ToJson(this);

    static Record()
    {
        Type recordType = typeof(Record);
        foreach (Type type in Assembly.GetEntryAssembly().GetTypes())
            if (!type.IsAbstract && recordType.IsAssignableFrom(type) && type.FullName is not null)
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
            string path = Path.Combine(Globals.DatabasePath, typeof(R).FullName!, Globals.Rec, $"{id}.{Globals.Rec}");
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

    public void Save()
    {
        if (Id == Guid.Empty) throw new Exception("Unexpected record state.");
    }

    /// <summary>
    /// Gets all available Record types in the current application.
    /// </summary>
    /// <returns>A string array with the full name of available record types.</returns>
    public static string[] Types => _types.Values.Select<Type, string>(t => t.FullName).ToArray();

    public string[] PropertyNames()
    {
        Type type = GetType();
        PropertyInfo[] props = type.GetProperties();
        return props.Select(p => $"{type.FullName}.{p.Name}").ToArray();
    }

    public static Record? Get(string type, string id)
    {
        try
        {
            Type t = _types[type];
            ConstructorInfo ci = t.GetConstructor(Array.Empty<Type>());
            dynamic obj = ci.Invoke(null);
            obj._id = Guid.Parse(id);
            return BijouDB.Json.ToJson(obj);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static object? GetProperty(string type, string id, string property)
    {
        if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(property)) return null;
        try
        {
            Type t = _types[type];
            ConstructorInfo ci = t.GetConstructor(Array.Empty<Type>());
            dynamic obj = ci.Invoke(null);
            obj._id = Guid.Parse(id);
            return BijouDB.Json.ToJson(t.GetProperty(property).GetValue(obj));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static R[] GetAll<R>()
        where R : Record, new()
    {
        string path = Path.Combine(Globals.DatabasePath, typeof(R).FullName!, Globals.Rec);
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

    public bool Equals(Record x, Record y) => x.GetType() == y.GetType() && Equals(x.Id, y.Id);
    public int GetHashCode(Record obj) => obj.Id.GetHashCode();
}
