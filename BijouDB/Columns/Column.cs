using BijouDB.Components;
using BijouDB.Exceptions;

namespace BijouDB;

/// <summary>
/// A column for adding to custom table implementations.
/// </summary>
/// <typeparam name="D">The IDataType.</typeparam>
public sealed class Column<D>
    where D : IDataType, new()
{
    internal readonly string _name;

    private readonly long _offset;
    private readonly bool _unique;
    private readonly Type _type;
    private readonly Func<D> _default;
    private readonly Func<D, bool> _check;
    private readonly Cache<D>? _cache;

    internal Column(long offset, string columnName, Type type, bool unique, Func<D> @default, Func<D, bool> check, int cache)
    {
        _offset = offset;
        _name = columnName;
        _type = type;
        _unique = unique;
        _default = @default;
        _check = check;
        if (cache > 0) _cache = new(cache);
    }

    /// <summary>
    /// Checks if a value is already present in an indexed column,
    /// true if found and presents the hash and value of the existing entry.
    /// false if the value was not found, the hash and a value candidate is presented.
    /// </summary>
    /// <param name="data">The value to check the index for.</param>
    /// <param name="hash">The hash of the value.</param>
    /// <param name="index">The index of the value if found, and a candidate if not.</param>
    /// <returns>true if the value was found, false otherwise.</returns>
    internal bool ValueIndex<R>(D data, out ulong hash, out Guid index)
        where R : Record
    {
        // Generate hash for new value
        using FileBackedStream ms = new();
        hash = data.Hash(ms);

        try
        {
            // hash lookup
            string hashDir = Path.Combine(Globals.DatabasePath, _type.FullName!, Globals.Index, _name, hash.PaddedString());
            if (Directory.Exists(hashDir))
            {
                foreach (string hashCollision in Directory.EnumerateDirectories(hashDir))
                {
                    string collisionName = Path.GetFileNameWithoutExtension(hashCollision);
                    string binFilePath = Path.Combine(hashCollision, Globals.BinFile);
                    if (Guid.TryParse(collisionName, out index) && File.Exists(binFilePath))
                    {
                        FileStream? fs = null;

                        if (SpinWait.SpinUntil(() =>
                        {
                            try
                            {
                                fs = new(binFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                                return true;
                            }
                            catch (Exception ex)
                            {
                                ex.Log();
                                return false;
                            }
                        }, 10000))
                        {
                            ms.Position = 0;
                            if (Misc.StreamCompare(ms, fs!)) return true;
                        }

                        fs?.Dispose();

                        return false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ex.Log();
        }
        index = IncrementalGuid.NextGuid();
        return false;
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
        if (ValueIndex<R>(value, out ulong hash, out Guid index))
        {
            string dataMatchPath = Path.Combine(Globals.DatabasePath, _type.FullName!, Globals.Index, _name, hash.PaddedString(), index.ToString());
            foreach (string reference in Directory.EnumerateFiles(dataMatchPath, Globals.RefPattern))
                if (Guid.TryParse(Path.GetFileNameWithoutExtension(reference), out Guid id) && Record.TryGet(id, out R? record))
                    yield return record!;
        }
    }

    /// <summary>
    /// Checks to see if a record exists that has the specified value.
    /// </summary>
    /// <param name="data">The value to check for.</param>
    /// <returns>true if a record exists with the value specified, false otherwise.</returns>
    public bool Contains<R>(D data)
        where R : Record, new()
    {
        if (ValueIndex<R>(data, out ulong hash, out Guid index))
        {
            string dataMatchPath = Path.Combine(Globals.DatabasePath, _type.FullName!, Globals.Index, _name, hash.PaddedString(), index.ToString());
            foreach (string reference in Directory.EnumerateFiles(dataMatchPath, Globals.RefPattern))
                if (Guid.TryParse(Path.GetFileNameWithoutExtension(reference), out _))
                    return true;
        }
        return false;
    }

    /// <summary>
    /// Gets an array of unique values stored in the column.
    /// </summary>
    /// <returns>An enumerable of unique values stored in the column.</returns>
    public IEnumerable<D> UniqueValues()
    {
        string colDir = Path.Combine(Globals.DatabasePath, _type.FullName!, Globals.Index, _name);
        if (Directory.Exists(colDir))
        {
            foreach (string uniqueValue in Directory.EnumerateFiles(colDir, Globals.BinFile, SearchOption.AllDirectories))
            {
                using FileStream fs = new(uniqueValue, FileMode.Open, FileAccess.Read, FileShare.Read);
                using MaskedStream ms = new(fs, Globals.BitMaskSeed);
                D newValue = new();
                newValue.Deserialize(ms);
                yield return newValue;
            }
        }
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
        Guid id = record.Id;

        if (id != Guid.Empty)
        {
            // check for cache
            if (_cache is not null && _cache.TryGet(id, out D? value)) return value!;

            string recordPath = Path.Combine(Globals.DatabasePath, _type.FullName!, Globals.Rec, $"{record.Id}.{Globals.Rec}");
            if (!File.Exists(recordPath)) throw new FileNotFoundException("Record is missing");
            using FileStream fs = new(recordPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            fs.Position = _offset;
            if (fs.ReadHashValue(out ulong crntHash, out Guid crntValue))
            {
                string crntBinPath = Path.Combine(Globals.DatabasePath, _type.FullName!, Globals.Index, _name, crntHash.PaddedString(), crntValue.ToString(), Globals.BinFile);
                if (File.Exists(crntBinPath))
                {
                    using FileStream fs2 = new(crntBinPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    D newValue = new();
                    using MaskedStream ms = new(fs2, Globals.BitMaskSeed);
                    newValue.Deserialize(ms);

                    // set for cache
                    if (_cache is not null) _cache.Set(id, newValue);

                    return newValue;
                }
            }
        }
        return _default is null ? default! : _default();
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
        Guid id = record.Id;

        if (_check is not null && !_check(value)) throw new CheckContraintException();

        string baseDir = Path.Combine(Globals.DatabasePath, _type.FullName!, Globals.Rec);
        Directory.CreateDirectory(baseDir);

        // Generate hash for new value
        using FileBackedStream ms = new();
        ulong newHash = value.Hash(ms);

        // Read previous value
        using FileStream fs = new(Path.Combine(baseDir, $"{id}.{Globals.Rec}"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

        // Old hash/value exists, read it and delete its reference
        fs.Position = _offset;
        if (fs.Length - _offset >= 24 && fs.ReadHashValue(out ulong oldHash, out Guid oldValue))
        {
            // check if old hash/value is valid
            if (oldValue != Guid.Empty)
            {
                string hashFolder = Path.Combine(Globals.DatabasePath, _type.FullName!, Globals.Index, _name, oldHash.PaddedString());
                string oldBinDir = Path.Combine(hashFolder, oldValue.ToString());
                string oldBinPath = Path.Combine(oldBinDir, Globals.BinFile);

                // Hash matches, check with value
                if (oldHash == newHash)
                {
                    if (File.Exists(oldBinPath))
                    {
                        using FileStream fs2 = new(oldBinPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        ms.Position = 0;
                        if (Misc.StreamCompare(ms, fs2))
                        {
                            // values match so no need to continue
                            if (Globals.Logging) Console.WriteLine("value is the same, skipping.");
                            return;
                        }
                    }
                }
                else
                {
                    try
                    {
                        // values differ, delete reference
                        File.Delete(Path.Combine(oldBinDir, $"{id}.{Globals.Ref}"));

                        // if value has no more references, delete it
                        if (Directory.GetFiles(oldBinDir, Globals.RefPattern).Length == 0)
                        {
                            Directory.Delete(oldBinDir, true);

                            // if hash has no more values, delete it
                            if (Directory.GetDirectories(hashFolder).Length == 0) Directory.Delete(hashFolder, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Globals.Logging) Console.WriteLine(ex.ToString());
                    }
                }
            }
        }

        // check if hash already exist
        string hashDir = Path.Combine(Globals.DatabasePath, _type.FullName!, Globals.Index, _name, newHash.PaddedString());
        if (Directory.Exists(hashDir))
        {
            string[] hashCollisions = Directory.GetDirectories(hashDir);
            foreach (string hashCollision in hashCollisions)
            {
                string crntBinPath = Path.Combine(hashCollision, Globals.BinFile);
                if (File.Exists(crntBinPath))
                {
                    using FileStream fsCollision = new(crntBinPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    ms.Position = 0;
                    if (Misc.StreamCompare(ms, fsCollision))
                    {
                        // Uniqueness check
                        if (_unique)
                            foreach (string match in Directory.EnumerateFiles(hashCollision, Globals.RefPattern))
                                throw new UniqueConstraintException<D>();

                        File.Create(Path.Combine(hashCollision, $"{id}.{Globals.Ref}")).Dispose();
                        string crntValue = Path.GetFileName(hashCollision);
                        fs.Position = _offset;
                        fs.WriteHashValue(newHash, Guid.Parse(crntValue));

                        // set for cache
                        if (_cache is not null) _cache.Set(id, value);

                        return;
                    }
                }
            }
        }

        Guid newValue = IncrementalGuid.NextGuid();

        // write bin file
        string newBinDir = Path.Combine(Globals.DatabasePath, _type.FullName!, Globals.Index, _name, newHash.PaddedString(), newValue.ToString());
        Directory.CreateDirectory(newBinDir);
        string newBinPath = Path.Combine(newBinDir, Globals.BinFile);
        using FileStream fs3 = new(newBinPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        ms.Position = 0;
        ms.CopyTo(fs3);
        fs3.Flush();

        // set for cache
        if (_cache is not null) _cache.Set(id, value);

        // add reference
        string newRef = Path.Combine(newBinDir, $"{id}.{Globals.Ref}");
        File.Create(newRef).Dispose();
        if (Globals.Logging) Console.WriteLine($"Created new reference: {newRef}");

        // write new info to record
        fs.Position = _offset;
        fs.WriteHashValue(newHash, newValue);
        fs.Flush();
    }

    internal void Remove(Record record)
    {
        Guid id = record.Id;

        // Read record file
        string recordPath = Path.Combine(Globals.DatabasePath, _type.FullName!, Globals.Rec, $"{id}.{Globals.Rec}");

        if (!File.Exists(recordPath)) return;

        using FileStream fs = new(recordPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        fs.Position = _offset;

        if (fs.Length - _offset >= 24 && fs.ReadHashValue(out ulong hash, out Guid index))
        {
            // Go to indexed location, delete reference file
            string hashDir = Path.Combine(Globals.DatabasePath, _type.FullName!, Globals.Index, _name, hash.PaddedString());
            string indexedDir = Path.Combine(hashDir, index.ToString());
            string refPath = Path.Combine(indexedDir, $"{id}.{Globals.Ref}");

            if (File.Exists(refPath))
                File.Delete(refPath);

            // remove from cache
            _cache?.Remove(id);

            // If no more references delete indexed folder
            if (Directory.Exists(indexedDir))
            {
                bool empty = true;
                foreach (string refernce in Directory.EnumerateFiles(indexedDir, Globals.RefPattern))
                {
                    empty = false;
                    break;
                }
                if (empty) Directory.Delete(indexedDir, true);
            }

            // If no more values delete hash folder
            if (Directory.Exists(hashDir))
            {
                bool empty = true;
                foreach (string indexes in Directory.EnumerateDirectories(hashDir))
                {
                    empty = false;
                    break;
                }
                if (empty) Directory.Delete(hashDir, true);
            }
        }
    }
}
