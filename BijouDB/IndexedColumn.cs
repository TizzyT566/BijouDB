using BijouDB.Components;
using BijouDB.Exceptions;
using System.Collections.ObjectModel;

namespace BijouDB;

/// <summary>
/// A column for adding to custom table implementations.
/// </summary>
/// <typeparam name="D">The IDataType.</typeparam>
public sealed class IndexedColumn<T, D> where T : Table, new() where D : IDataType, new()
{
    public string Name { get; }
    public long Offset { get; }
    public ColumnType Type { get; }

    private readonly LengthRef _tableLength;

    internal IndexedColumn(ColumnType type, long offset, string columnName, LengthRef tableLengthRef)
    {
        Type = type;
        Offset = offset;
        Name = columnName;
        _tableLength = tableLengthRef;
    }

    // Checks if a value is already present in an indexed column,
    // true if found and presents the hash and value of the existing entry.
    // false if the value was not found, the hash and a value candidate is presented.
    public bool ValueIndex(D data, out Guid hash, out Guid value)
    {
        if (Type == ColumnType.None) throw new InvalidOperationException("Indexed lookups only valid on indexed columns.");

        // Generate hash for new value
        using FileBackedStream ms = new();
        hash = data.Hash(ms);

        // hash lookup
        string hashDir = Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Index, Name, hash.ToString());
        if (Directory.Exists(hashDir))
        {
            foreach (string hashCollision in Directory.EnumerateDirectories(hashDir))
            {
                string binFilePath = Path.Combine(hashCollision, Globals.BinFile);
                if (Guid.TryParse(hashCollision, out value) && File.Exists(binFilePath))
                {
                    using FileStream fs = new(binFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    ms.Position = 0;
                    if (Misc.StreamCompare(ms, fs)) return true;
                }
            }
        }
        value = IncrementalGuid.NextGuid();
        return false;
    }

    public ReadOnlyDictionary<Guid, T> RecordsWithValue(D data)
    {
        Dictionary<Guid, T> records = new();
        if (ValueIndex(data, out Guid hash, out Guid value))
        {
            string dataMatchPath = Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Index, Name, hash.ToString(), value.ToString());
            foreach (string reference in Directory.EnumerateFiles(dataMatchPath, Globals.RefPattern))
                if (Guid.TryParse(Path.GetFileNameWithoutExtension(reference), out Guid id))
                    if (Table.TryGet(id, out T? record))
                        records.Add(record!.Id, record!);
        }
        return new(records);
    }

    public bool HasValue(D data)
    {
        if (ValueIndex(data, out Guid hash, out Guid value))
        {
            string dataMatchPath = Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Index, Name, hash.ToString(), value.ToString());
            foreach (string reference in Directory.EnumerateFiles(dataMatchPath, Globals.RefPattern))
                if (Guid.TryParse(Path.GetFileNameWithoutExtension(reference), out Guid id))
                    if (Table.TryGet<T>(id, out _))
                        return true;
        }
        return false;
    }

    public D[] UniqueValues()
    {
        List<D> ds = new();
        string colDir = Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Index, Name);
        string[] uniqueValues = Directory.GetFiles(colDir, Globals.BinFile, SearchOption.AllDirectories);
        foreach (string uniqueValue in uniqueValues)
        {
            using FileStream fs = new(uniqueValue, FileMode.Open, FileAccess.Read, FileShare.Read);
            D newValue = new();
            newValue.Deserialize(fs);
            ds.Add(newValue);
        }
        return ds.ToArray();
    }

    public D Get(T record)
    {
        if (Type == ColumnType.None)
        {
            if (D.Length > 0)
            {
                using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Rec, $"{record.Id}.{Globals.Rec}"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                if (fs.Length - Offset < D.Length) throw new CorruptedException<D>();
                fs.Position = Offset;
                D obj = new();
                obj.Deserialize(fs);
                return obj;
            }
            else // Is reference
            {
                using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Rec, $"{record.Id}.{Name}"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                if (fs.Length - Offset < D.Length) throw new CorruptedException<D>();
                D obj = new();
                obj.Deserialize(fs);
                return obj;
            }
        }
        else
        {
            using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Rec, $"{record.Id}.{Globals.Rec}"), FileMode.Open, FileAccess.Read, FileShare.Read);
            fs.Position = Offset;
            if (fs.ReadHashValue(out Guid crntHash, out Guid crntValue))
            {
                string crntBinPath = Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Index, Name, crntHash.ToString(), crntValue.ToString(), Globals.BinFile);
                if (File.Exists(crntBinPath))
                {
                    using FileStream fs2 = new(crntBinPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    if (fs.Length - Offset < D.Length) throw new CorruptedException<D>();
                    D newType = new();
                    newType.Deserialize(fs2);
                    return newType;
                }
            }
        }
        return new();
    }

    public void Set(T table, D value)
    {
        if (table.Id == Guid.Empty) throw new IncompleteRecordException<T>();

        string baseDir = Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Rec);
        Directory.CreateDirectory(baseDir);

        if (Type == ColumnType.None)
        {
            if (D.Length > 0)
            {
                using FileStream fs = new(Path.Combine(baseDir, $"{table.Id}.{Globals.Rec}"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                fs.Position = Offset;
                if (value is null)
                {
                    byte[] bytes = new byte[D.Length];
                    fs.Write(bytes, 0, bytes.Length);
                }
                else
                    value.Serialize(fs);
                fs.Flush(_tableLength);
            }
            else // Is reference
            {
                FileStream rcrd = new(Path.Combine(baseDir, $"{table.Id}.{Globals.Rec}"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                rcrd.Flush(_tableLength);
                rcrd.Dispose();
                using FileStream fs = new(Path.Combine(baseDir, $"{table.Id}.{Name}"), FileMode.Create, FileAccess.Write, FileShare.None);
                if (value is null)
                {
                    byte[] bytes = new byte[D.Length];
                    fs.Write(bytes, 0, bytes.Length);
                }
                else
                    value.Serialize(fs);
                fs.Flush(_tableLength);
            }
        }
        else
        {
            // Generate hash for new value
            using FileBackedStream ms = new();
            Guid newHash = value.Hash(ms);

            // Read previous value
            using FileStream fs = new(Path.Combine(baseDir, $"{table.Id}.{Globals.Rec}"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // Old hash/value exists, read it and delete its reference
            fs.Position = Offset;
            if (fs.Length - Offset >= 32 && fs.ReadHashValue(out Guid oldHash, out Guid oldValue))
            {
                // check if old hash/value is valid
                if (oldValue != Guid.Empty)
                {
                    string hashFolder = Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Index, Name, oldHash.ToString());
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
                            File.Delete(Path.Combine(oldBinDir, $"{table.Id}.{Globals.Ref}"));

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
            string hashDir = Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Index, Name, newHash.ToString());
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
                            if (Type == ColumnType.Unique && Directory.GetFiles(hashCollision, Globals.RefPattern).Length > 0)
                                throw new UniquenessConstraintException<D>();

                            File.Create(Path.Combine(hashCollision, $"{table.Id}.{Globals.Ref}"));
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
            string newBinDir = Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Index, Name, newHash.ToString(), newValue.ToString());
            Directory.CreateDirectory(newBinDir);
            string newBinPath = Path.Combine(newBinDir, Globals.BinFile);
            using FileStream fs3 = new(newBinPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            ms.Position = 0;
            ms.CopyTo(fs3);
            fs3.Flush();

            // add reference
            string newRef = Path.Combine(newBinDir, $"{table.Id}.{Globals.Ref}");
            File.Create(newRef);
            if (Globals.Logging) Console.WriteLine($"Created new reference: {newRef}");

            // write new info to record
            fs.Position = Offset;
            fs.WriteHashValue(newHash, newValue);
            fs.Flush(_tableLength);
        }
    }
}