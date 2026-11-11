using BijouDB.Components;
using BijouDB.Exceptions;

namespace BijouDB;

/// <summary>
/// An indexed, constrained column belonging to one record schema.
/// </summary>
/// <typeparam name="D">The IDataType.</typeparam>
public sealed class Column<D>
    where D : IDataType, new()
{
    internal readonly string _name;

    private readonly long _offset;
    private readonly bool _unique;
    private readonly Type _type;
    private readonly Func<D>? _default;
    private readonly Func<D, bool>? _check;
    private readonly Cache<D>? _cache;

    internal Column(long offset, string columnName, Type type, bool unique, Func<D> @default, Func<D, bool> check, int cache)
    {
        _offset = offset;
        _name = columnName;
        _type = type;
        _unique = unique;
        _default = @default;
        _check = check;
        if (cache > 0)
            _cache = new(cache);
    }

    private string ColumnDirectory => Path.Combine(Globals.DatabasePath, _type.FullName!, Globals.Index, _name);

    private string ValueDirectory(ulong hash, Guid index) => Path.Combine(ColumnDirectory, hash.PaddedString(), index.ToString());

    private void ValidateRecord(Record record)
    {
        if (record is null) throw new ArgumentNullException(nameof(record));
        if (record.GetType() != _type) throw new ArgumentException($"This column belongs to {_type.FullName}.", nameof(record));
    }

    private void ValidateQuery<R>()
        where R : Record
    {
        if (typeof(R) != _type) throw new ArgumentException($"This column belongs to {_type.FullName}.");
    }

    private (ulong Hash, Guid Index) ReadSlot(string recordPath)
    {
        using FileStream stream = new(recordPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length % 24 != 0) throw new InvalidDataException($"Truncated record: {recordPath}");
        if (stream.Length <= _offset) return (0, Guid.Empty);
        stream.Position = _offset;
        if (!stream.ReadHashValue(out ulong hash, out Guid index)) throw new InvalidDataException($"Truncated column pointer: {recordPath}");
        if (index == Guid.Empty && hash != 0) throw new InvalidDataException($"Invalid column pointer: {recordPath}");
        return (hash, index);
    }

    private List<Guid> MatchingIndexes(Stream serialized, ulong hash)
    {
        List<Guid> matches = new();
        string directory = Path.Combine(ColumnDirectory, hash.PaddedString());
        if (!Directory.Exists(directory)) return matches;
        foreach (string candidate in Directory.GetDirectories(directory))
        {
            if (!Guid.TryParse(Path.GetFileName(candidate), out Guid index) || index == Guid.Empty) continue;

            string path = Path.Combine(candidate, Globals.BinFile);
            if (!File.Exists(path))
            {
                // Undo removes staged files; an empty directory can remain after a failed write.
                if (!Directory.EnumerateFileSystemEntries(candidate).Any()) continue;
                throw new InvalidDataException($"Indexed value is missing: {path}");
            }

            using FileStream stored = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            serialized.Position = 0;
            if (Misc.StreamCompare(serialized, stored))
                matches.Add(index);
        }

        return matches;
    }

    private IEnumerable<Guid> LiveReferences(ulong hash, Guid index)
    {
        string directory = ValueDirectory(hash, index);
        foreach (string reference in Directory.GetFiles(directory, Globals.RefPattern))
        {
            if (!Guid.TryParse(Path.GetFileNameWithoutExtension(reference), out Guid id) || id == Guid.Empty) continue;

            string recordPath = Storage.RecordPath(_type, id);
            if (!File.Exists(recordPath)) continue;

            (ulong Hash, Guid Index) slot = ReadSlot(recordPath);
            if (slot.Hash == hash && slot.Index == index)
                yield return id;
        }
    }

    /// <summary>
    /// Checks if a value is already present in an indexed column,
    /// true if found and presents the hash and value of the existing entry.
    /// false if the value was not found, with an empty index.
    /// </summary>
    /// <param name="value">The value to check the index for.</param>
    /// <param name="hash">The hash of the value.</param>
    /// <param name="index">The index of the value if found, otherwise Guid.Empty.</param>
    /// <returns>true if the value was found, false otherwise.</returns>
    internal bool ValueIndex<R>(D value, out ulong hash, out Guid index)
        where R : Record
    {
        ValidateQuery<R>();
        if (value is null) throw new ArgumentNullException(nameof(value));

        lock (Storage.SyncRoot)
        {
            Storage.EnsureInitialized();
            using FileBackedStream serialized = new();
            hash = value.Hash(serialized);
            List<Guid> matches = MatchingIndexes(serialized, hash);
            index = matches.Count == 0 ? Guid.Empty : matches[0];
            return matches.Count != 0;
        }
    }

    /// <summary>
    /// Get records with the specified value.
    /// </summary>
    /// <typeparam name="R">The return record type.</typeparam>
    /// <param name="value">The value to search records with.</param>
    /// <returns>An enumerable of all records containing the value specified.</returns>
    public IEnumerable<R> WithValue<R>(D value)
        where R : Record, new()
    {
        ValidateQuery<R>();
        if (value is null) throw new ArgumentNullException(nameof(value));

        lock (Storage.SyncRoot)
        {
            Storage.EnsureInitialized();
            using FileBackedStream serialized = new();
            ulong hash = value.Hash(serialized);
            List<R> records = new();
            HashSet<Guid> ids = new();
            foreach (Guid index in MatchingIndexes(serialized, hash))
                foreach (Guid id in LiveReferences(hash, index))
                    if (ids.Add(id)) records.Add((R)Record.Materialize(_type, id));

            return records.ToArray();
        }
    }

    /// <summary>
    /// Checks to see if a record exists that has the specified value.
    /// </summary>
    /// <param name="data">The value to check for.</param>
    /// <returns>true if a record exists with the value specified, false otherwise.</returns>
    public bool Contains<R>(D data)
        where R : Record, new() =>
        WithValue<R>(data).Any();

    /// <summary>
    /// Gets an array of unique values stored in the column.
    /// </summary>
    /// <returns>An enumerable of unique values stored in the column.</returns>
    public IEnumerable<D> UniqueValues()
    {
        lock (Storage.SyncRoot)
        {
            Storage.EnsureInitialized();
            List<D> values = new();
            if (!Directory.Exists(ColumnDirectory)) return values;
            foreach (string hashDirectory in Directory.GetDirectories(ColumnDirectory))
            {
                if (!ulong.TryParse(Path.GetFileName(hashDirectory), out ulong hash)) continue;
                foreach (string directory in Directory.GetDirectories(hashDirectory))
                    if (Guid.TryParse(Path.GetFileName(directory), out Guid index) && LiveReferences(hash, index).Any())
                        values.Add(Deserialize(Path.Combine(directory, Globals.BinFile)));
            }

            return values.ToArray();
        }
    }

    private static D Deserialize(string path)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using MaskedStream masked = new(stream, Globals.BitMaskSeed);
        D value = new();
        value.Deserialize(masked);
        if (stream.Position != stream.Length) throw new InvalidDataException($"Trailing bytes in stored value: {path}");
        return value;
    }

    private static D Clone(D value)
    {
        using FileBackedStream stream = new();
        value.Serialize(stream);
        stream.Position = 0;
        D clone = new();
        clone.Deserialize(stream);
        return clone;
    }

    /// <summary>
    /// Gets the value stored in a record.
    /// </summary>
    /// <typeparam name="R">The record type.</typeparam>
    /// <param name="record">The record to get the value from.</param>
    /// <returns>The value stored in the record.</returns>
    /// <exception cref="FileNotFoundException">The record is missing from the database.</exception>
    public D Get<R>(R record)
        where R : Record
    {
        ValidateRecord(record);

        lock (Storage.SyncRoot)
        {
            Storage.EnsureInitialized();
            Guid id = record.Id;
            string path = Storage.RecordPath(_type, id);
            if (!File.Exists(path)) throw new FileNotFoundException($"Record {id} does not exist.", path);
            if (_cache is not null && _cache.TryGet(id, out D? cached)) return Clone(cached!);

            (ulong Hash, Guid Index) slot = ReadSlot(path);
            if (slot.Index == Guid.Empty) return _default is null ? new D() : _default();

            string valuePath = Path.Combine(ValueDirectory(slot.Hash, slot.Index), Globals.BinFile);
            if (!File.Exists(valuePath)) throw new InvalidDataException($"Stored column value is missing: {valuePath}");

            D value = Deserialize(valuePath);
            if (_cache is not null)
            {
                _cache.Set(id, value);
                return Clone(value);
            }

            return value;
        }
    }

    /// <summary>
    /// Sets the value stored in a record.
    /// </summary>
    /// <typeparam name="R">The record type.</typeparam>
    /// <param name="record">The record to set the value to.</param>
    /// <param name="value">The value to store into the record.</param>
    /// <exception cref="CheckContraintException">The value failed the check constraint.</exception>
    /// <exception cref="UniqueConstraintException{D}">The value failed the unique contraint.</exception>
    public void Set<R>(R record, D value)
        where R : Record
    {
        ValidateRecord(record);
        if (value is null) throw new ArgumentNullException(nameof(value));

        lock (Storage.SyncRoot)
        {
            Storage.EnsureInitialized();
            if (_check is not null && !_check(value)) throw new CheckContraintException();

            Guid id = record.Id;
            string recordPath = Storage.RecordPath(_type, id);
            if (!File.Exists(recordPath)) throw new FileNotFoundException($"Record {id} does not exist.", recordPath);

            // Generate the new value's hash without changing its contents.
            using FileBackedStream serialized = new();
            ulong hash = value.Hash(serialized);
            (ulong Hash, Guid Index) old = ReadSlot(recordPath);
            List<Guid> matches = MatchingIndexes(serialized, hash);

            // Validate before removing any previous value or reference.
            if (_unique && matches.Any(index => LiveReferences(hash, index).Any(other => other != id))) throw new UniqueConstraintException<D>();
            if (old.Hash == hash && matches.Contains(old.Index)) return;

            Guid target = matches.Count == 0 ? IncrementalGuid.NextGuid() : matches[0];
            while (matches.Count == 0 && Directory.Exists(ValueDirectory(hash, target)))
                target = IncrementalGuid.NextGuid();

            string targetDirectory = ValueDirectory(hash, target);
            _cache?.Remove(id);

            Storage.RunTransaction(() =>
            {
                if (matches.Count == 0)
                {
                    serialized.Position = 0;
                    Storage.WriteFile(Path.Combine(targetDirectory, Globals.BinFile), serialized);
                }

                Storage.CreateEmptyFile(Path.Combine(targetDirectory, $"{id}.{Globals.Ref}"));

                // Commit the record pointer before cleaning up the previous reference.
                using (MemoryStream updated = new())
                {
                    byte[] original = File.ReadAllBytes(recordPath);
                    updated.Write(original, 0, original.Length);
                    updated.Position = _offset;
                    updated.WriteHashValue(hash, target);
                    updated.Position = 0;
                    Storage.WriteFile(recordPath, updated);
                }

                if (old.Index != Guid.Empty)
                    RemoveReference(id, old.Hash, old.Index);
            });
        }
    }

    private void RemoveReference(Guid id, ulong hash, Guid index)
    {
        string directory = ValueDirectory(hash, index);
        if (!Directory.Exists(directory)) throw new InvalidDataException($"Indexed value directory is missing: {directory}");
        Storage.DeleteFile(Path.Combine(directory, $"{id}.{Globals.Ref}"));
        if (!Directory.EnumerateFiles(directory, Globals.RefPattern).Any())
        {
            Storage.DeleteDirectory(directory);
            string hashDirectory = Path.GetDirectoryName(directory)!;
            if (!Directory.EnumerateFileSystemEntries(hashDirectory).Any())
                Storage.DeleteDirectory(hashDirectory);
        }
    }

    internal void Remove(Record record)
    {
        ValidateRecord(record);

        lock (Storage.SyncRoot)
        {
            Storage.EnsureInitialized();
            if (!record._id.HasValue) return;

            Guid id = record._id.Value;
            string path = Storage.RecordPath(_type, id);
            if (!File.Exists(path)) return;

            (ulong Hash, Guid Index) slot = ReadSlot(path);
            _cache?.Remove(id);
            if (slot.Index != Guid.Empty)
                RemoveReference(id, slot.Hash, slot.Index);
        }
    }
}
