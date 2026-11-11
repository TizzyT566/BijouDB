using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using BijouDB.Exceptions;

namespace BijouDB;

public abstract class Record : IEqualityComparer<Record>
{
    private static readonly object _typeLock = new();
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
            RegisterType(GetType());

            lock (Storage.SyncRoot)
            {
                Storage.EnsureInitialized();
                if (_id.HasValue) return _id.Value;

                string directory = Path.Combine(Globals.DatabasePath, GetType().FullName!, Globals.Rec);
                Directory.CreateDirectory(directory);
                while (true)
                {
                    Guid candidate = IncrementalGuid.NextGuid();
                    string path = Path.Combine(directory, $"{candidate}.{Globals.Rec}");

                    try
                    {
                        new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read).Dispose();
                        _id = candidate;
                        return candidate;
                    }
                    catch (IOException) when (File.Exists(path))
                    {
                        // Try another Id if this filename already exists.
                    }
                }
            }
        }
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
        where R : Record, new()
    {
        if (Guid.TryParse(id, out Guid guid)) return TryGet(guid, out record);
        record = null;
        return false;
    }

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
        bool found = TryGet(typeof(R), id, out Record? result);
        record = (R?)result;
        return found;
    }

    internal static bool TryGet(Type type, Guid id, out Record? record)
    {
        record = null;
        if (id == Guid.Empty) return false;
        RegisterType(type);
        RuntimeHelpers.RunClassConstructor(type.TypeHandle);

        lock (Storage.SyncRoot)
        {
            Storage.EnsureInitialized();
            if (!File.Exists(Storage.RecordPath(type, id))) return false;
            record = Materialize(type, id);
            return true;
        }
    }

    internal static Record Materialize(Type type, Guid id)
    {
        Record record = (Record)FormatterServices.GetUninitializedObject(type);
        record._id = id;
        return record;
    }

    internal static void RegisterType(Type type)
    {
        if (type.IsAbstract || !typeof(Record).IsAssignableFrom(type) || type.ContainsGenericParameters || type.FullName is null)
            throw new ArgumentException("A concrete record type is required.", nameof(type));
        if (!type.IsVisible) throw new NotPublicRecordException(type);

        lock (_typeLock)
        {
            if (_types.TryGetValue(type.FullName, out Type? existing) && existing != type)
                throw new InvalidOperationException($"Multiple record types use the name '{type.FullName}'.");
            _types[type.FullName] = type;
        }
    }

    /// <summary>
    /// Register models in another assembly, including dynamically loaded plugins.
    /// </summary>
    public static void RegisterAssembly(Assembly assembly)
    {
        if (assembly is null) throw new ArgumentNullException(nameof(assembly));

        IEnumerable<Type> types;

        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).Cast<Type>();
        }
        catch (NotSupportedException)
        {
            return;
        }

        foreach (Type type in types)
            if (!type.IsAbstract && !type.ContainsGenericParameters && type.IsVisible && typeof(Record).IsAssignableFrom(type))
                RegisterType(type);
    }

    internal static bool TryResolveType(string name, out Type? type)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            RegisterAssembly(assembly);

        lock (_typeLock)
            return _types.TryGetValue(name, out type);
    }

    /// <summary>
    /// Gets all available Record types in the current application.
    /// </summary>
    /// <returns>A string array with the full name of available record types.</returns>
    public static string[] Types
    {
        get
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                RegisterAssembly(assembly);

            lock (_typeLock)
                return _types.Keys.OrderBy(n => n, StringComparer.Ordinal).ToArray();
        }
    }

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
        RegisterType(typeof(R));
        RuntimeHelpers.RunClassConstructor(typeof(R).TypeHandle);

        lock (Storage.SyncRoot)
        {
            Storage.EnsureInitialized();
            string directory = Path.Combine(Globals.DatabasePath, typeof(R).FullName!, Globals.Rec);
            if (!Directory.Exists(directory)) return Array.Empty<R>();
            List<R> records = new();
            foreach (string path in Directory.GetFiles(directory, Globals.RecPattern))
            {
                string recordName = Path.GetFileNameWithoutExtension(path);
                if (!Guid.TryParse(recordName, out Guid id) || id == Guid.Empty)
                    throw new InvalidDataException($"Invalid record filename: {path}");

                records.Add((R)Materialize(typeof(R), id));
            }
            return records.ToArray();
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
        if (columnMatches is null) throw new ArgumentNullException(nameof(columnMatches));
        if (columnMatches.Length == 0 || columnMatches.Any(matches => matches is null)) return Array.Empty<R>();
        if (columnMatches.Length == 1) return columnMatches[0];

        // Start with the smallest collection.
        HashSet<Guid>[] sets = columnMatches
            .Select(matches => new HashSet<Guid>(matches.Select(row => row.Id)))
            .OrderBy(set => set.Count)
            .ToArray();

        HashSet<Guid> result = sets[0];
        foreach (HashSet<Guid> set in sets.Skip(1))
            result.IntersectWith(set);

        return result.Select(id => (R)Materialize(typeof(R), id)).ToArray();
    }

    public void Remove() => Remove(this);

    public bool TryRemove() => TryRemove(out _);

    /// <summary>
    /// Tries to remove a Record from the database.
    /// </summary>
    /// <param name="exception">Any exception which occurred.</param>
    /// <returns>true if Record was removed, otherwise false.</returns>
    public bool TryRemove(out Exception? exception)
    {
        try
        {
            Remove();
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
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        lock (Storage.SyncRoot)
            return x._id.HasValue && y._id.HasValue && x.GetType() == y.GetType() && x._id == y._id;
    }

    /// <summary>
    /// Hashing establishes a persistent ID so the hash remains stable after property access.
    /// </summary>
    /// <param name="obj">The Record to get the hash code for.</param>
    /// <returns>The hash code.</returns>
    public int GetHashCode(Record obj) => obj is null ? throw new ArgumentNullException(nameof(obj)) : obj.Id.GetHashCode();

    public override int GetHashCode() => Id.GetHashCode();

    internal static void Remove<R>(R record)
        where R : Record
    {
        RuntimeHelpers.RunClassConstructor(record.GetType().TypeHandle);
        Action<Record>? definition;

        lock (_typeLock)
            _removeDefinitions.TryGetValue(record.GetType(), out definition);
        if (definition is null) throw new InvalidOperationException($"No schema is registered for {record.GetType().FullName}.");
        definition(record);
    }

    internal static void AddRemoveDefinition<R>(Action<Record> removeDefinition)
        where R : Record
    {
        RegisterType(typeof(R));

        lock (_typeLock)
            _removeDefinitions[typeof(R)] = removeDefinition;
    }
}
