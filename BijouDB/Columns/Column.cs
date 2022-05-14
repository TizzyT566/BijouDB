using BijouDB.Components;
using BijouDB.Exceptions;

namespace BijouDB;

/// <summary>
/// A column for adding to custom table implementations.
/// </summary>
/// <typeparam name="D">The IDataType.</typeparam>
public sealed class Column<D> where D : IDataType, new()
{
    public long Offset { get; }

    internal readonly string _name;

    private readonly LengthRef _tableLength;
    private readonly Type _type;

    private readonly bool _unique;
    private readonly Func<D> _default;
    private readonly Func<D, bool> _check;

    internal Column(LengthRef tableLengthRef, string columnName, Type type, bool unique, Func<D> @default, Func<D, bool> check)
    {
        Offset = _tableLength = tableLengthRef;
        _name = columnName;
        _type = type;
        _unique = unique;
        _default = @default;
        _check = check;
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
    internal bool ValueIndex<R>(D data, out Guid hash, out Guid index) where R : Record
    {
        // Generate hash for new value
        using FileBackedStream ms = new();
        hash = data.Hash(ms);

        // hash lookup
        string hashDir = Path.Combine(Globals.DB_Path, _type.FullName!, Globals.Index, _name, hash.ToString());
        if (Directory.Exists(hashDir))
        {
            foreach (string hashCollision in Directory.EnumerateDirectories(hashDir))
            {
                string collisionName = Path.GetFileNameWithoutExtension(hashCollision);
                string binFilePath = Path.Combine(hashCollision, Globals.BinFile);
                if (Guid.TryParse(collisionName, out index) && File.Exists(binFilePath))
                {
                    using FileStream fs = new(binFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                    ms.Position = 0;
                    if (Misc.StreamCompare(ms, fs)) return true;
                }
            }
        }
        index = IncrementalGuid.NextGuid();
        return false;
    }

    /// <summary>
    /// Get a list of records with the specified value.
    /// </summary>
    /// <param name="data">The value to search records with.</param>
    /// <returns>A readonly dictionary of all records containing the value specified.</returns>
    public R[] RecordsWithValue<R>(D data) where R : Record, new()
    {
        List<R> records = new();
        if (ValueIndex<R>(data, out Guid hash, out Guid index))
        {
            string dataMatchPath = Path.Combine(Globals.DB_Path, _type.FullName!, Globals.Index, _name, hash.ToString(), index.ToString());
            foreach (string reference in Directory.EnumerateFiles(dataMatchPath, Globals.RefPattern))
                if (Guid.TryParse(Path.GetFileNameWithoutExtension(reference), out Guid id))
                    if (Record.TryGet(id, out R? record))
                        records.Add(record!);
        }
        return records.ToArray();
    }

    /// <summary>
    /// Checks to see if a record exists that has the specified value.
    /// </summary>
    /// <param name="data">The value to check for.</param>
    /// <returns>true if a record exists with the value specified, false otherwise.</returns>
    internal bool HasRecordsWithValue<R>(D data) where R : Record, new()
    {
        if (ValueIndex<R>(data, out Guid hash, out Guid index))
        {
            string dataMatchPath = Path.Combine(Globals.DB_Path, _type.FullName!, Globals.Index, _name, hash.ToString(), index.ToString());
            foreach (string reference in Directory.EnumerateFiles(dataMatchPath, Globals.RefPattern))
                if (Guid.TryParse(Path.GetFileNameWithoutExtension(reference), out Guid id) && Record.TryGet<R>(id, out _))
                    return true;
        }
        return false;
    }

    /// <summary>
    /// Gets an array of unique values stored in the column.
    /// </summary>
    /// <returns>An array of unique values stored in the column.</returns>
    public D[] UniqueValues()
    {
        List<D> ds = new();
        string colDir = Path.Combine(Globals.DB_Path, _type.FullName!, Globals.Index, _name);
        if (!Directory.Exists(colDir)) return ds.ToArray();
        string[] uniqueValues = Directory.GetFiles(colDir, Globals.BinFile, SearchOption.AllDirectories);
        foreach (string uniqueValue in uniqueValues)
        {
            using FileStream fs = new(uniqueValue, FileMode.Open, FileAccess.Read, FileShare.None);
            D newValue = new();
            newValue.Deserialize(fs);
            ds.Add(newValue);
        }
        return ds.ToArray();
    }

    public D Get<R>(R record) where R : Record
    {
        string recordPath = Path.Combine(Globals.DB_Path, _type.FullName!, Globals.Rec, $"{record.Id}.{Globals.Rec}");
        if (!File.Exists(recordPath)) throw new FileNotFoundException("Record is missing");
        using FileStream fs = new(recordPath, FileMode.Open, FileAccess.Read, FileShare.None);
        fs.Position = Offset;
        if (fs.ReadHashValue(out Guid crntHash, out Guid crntValue))
        {
            string crntBinPath = Path.Combine(Globals.DB_Path, _type.FullName!, Globals.Index, _name, crntHash.ToString(), crntValue.ToString(), Globals.BinFile);
            if (File.Exists(crntBinPath))
            {
                using FileStream fs2 = new(crntBinPath, FileMode.Open, FileAccess.Read, FileShare.None);
                if (fs.Length - Offset < D.Length) throw new CorruptedException<D>();
                D newType = new();
                newType.Deserialize(fs2);
                return newType;
            }
            else throw new FileNotFoundException("Value for DataType is missing, database maybe corrupted.");
        }
        return _default is null ? default! : _default();
    }

    public void Set<R>(R record, D value) where R : Record
    {
        if (_check is not null && !_check(value)) throw new FailedCheckContraintException();

        string baseDir = Path.Combine(Globals.DB_Path, _type.FullName!, Globals.Rec);
        Directory.CreateDirectory(baseDir);

        // Generate hash for new value
        using FileBackedStream ms = new();
        Guid newHash = value.Hash(ms);

        // Read previous value
        using FileStream fs = new(Path.Combine(baseDir, $"{record.Id}.{Globals.Rec}"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        // Old hash/value exists, read it and delete its reference
        fs.Position = Offset;
        if (fs.Length - Offset >= 32 && fs.ReadHashValue(out Guid oldHash, out Guid oldValue))
        {
            // check if old hash/value is valid
            if (oldValue != Guid.Empty)
            {
                string hashFolder = Path.Combine(Globals.DB_Path, _type.FullName!, Globals.Index, _name, oldHash.ToString());
                string oldBinDir = Path.Combine(hashFolder, oldValue.ToString());
                string oldBinPath = Path.Combine(oldBinDir, Globals.BinFile);

                // Hash matches, check with value
                if (oldHash == newHash)
                {
                    if (File.Exists(oldBinPath))
                    {
                        using FileStream fs2 = new(oldBinPath, FileMode.Open, FileAccess.Read, FileShare.None);
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
                        File.Delete(Path.Combine(oldBinDir, $"{record.Id}.{Globals.Ref}"));

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
        string hashDir = Path.Combine(Globals.DB_Path, _type.FullName!, Globals.Index, _name, newHash.ToString());
        if (Directory.Exists(hashDir))
        {
            string[] hashCollisions = Directory.GetDirectories(hashDir);
            foreach (string hashCollision in hashCollisions)
            {
                string crntBinPath = Path.Combine(hashCollision, Globals.BinFile);
                if (File.Exists(crntBinPath))
                {
                    using FileStream fsCollision = new(crntBinPath, FileMode.Open, FileAccess.Read, FileShare.None);
                    ms.Position = 0;
                    if (Misc.StreamCompare(ms, fsCollision))
                    {
                        // Uniqueness check
                        if (_unique)
                            foreach (string match in Directory.EnumerateFiles(hashCollision, Globals.RefPattern))
                                throw new UniqueConstraintException<D>();

                        File.Create(Path.Combine(hashCollision, $"{record.Id}.{Globals.Ref}")).Dispose();
                        string crntValue = Path.GetFileName(hashCollision);
                        fs.Position = Offset;
                        fs.WriteHashValue(newHash, Guid.Parse(crntValue));
                        fs.Flush(_tableLength);
                        return;
                    }
                }
            }
        }

        Guid newValue = IncrementalGuid.NextGuid();

        // write bin file
        string newBinDir = Path.Combine(Globals.DB_Path, _type.FullName!, Globals.Index, _name, newHash.ToString(), newValue.ToString());
        Directory.CreateDirectory(newBinDir);
        string newBinPath = Path.Combine(newBinDir, Globals.BinFile);
        using FileStream fs3 = new(newBinPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        ms.Position = 0;
        ms.CopyTo(fs3);
        fs3.Flush();

        // add reference
        string newRef = Path.Combine(newBinDir, $"{record.Id}.{Globals.Ref}");
        File.Create(newRef).Dispose();
        if (Globals.Logging) Console.WriteLine($"Created new reference: {newRef}");

        // write new info to record
        fs.Position = Offset;
        fs.WriteHashValue(newHash, newValue);
        fs.Flush(_tableLength);
    }

    public void Remove(Record record)
    {
        // Read record file
        string recordPath = Path.Combine(Globals.DB_Path, _type.FullName!, Globals.Rec, $"{record.Id}.{Globals.Rec}");

        if (!File.Exists(recordPath)) return;

        using FileStream fs = new(recordPath, FileMode.Open, FileAccess.Read, FileShare.None);
        fs.Position = Offset;

        if (fs.Length - Offset >= 32 && fs.ReadHashValue(out Guid hash, out Guid index))
        {
            // Go to indexed location, delete reference file
            string hashDir = Path.Combine(Globals.DB_Path, _type.FullName!, Globals.Index, _name, hash.ToString());
            string indexedDir = Path.Combine(hashDir, index.ToString());
            string refPath = Path.Combine(indexedDir, $"{record.Id}.{Globals.Ref}");

            if (File.Exists(refPath))
                File.Delete(refPath);

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
