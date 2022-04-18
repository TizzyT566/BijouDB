using BijouDB.Exceptions;

namespace BijouDB;

public enum ColumnType
{
    None,
    Indexed,
    Unique
}

/// <summary>
/// A column for adding to custom table implementations.
/// </summary>
/// <typeparam name="D">The IDataType.</typeparam>
public sealed class Column<D> where D : IDataType, new()
{
    public string Name { get; }
    public long Offset { get; }
    public ColumnType Type { get; }

    internal Column(string columnName, long offset, ColumnType type)
    {
        Name = columnName;
        Offset = offset;
        Type = type;
    }

    public D Get<T>(T record) where T : Tables
    {
        switch (Type)
        {
            case ColumnType.Indexed:
                {
                    using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Rec, $"{record.Id}.{Globals.Rec}"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    if (fs.ReadHashValue(out Guid crntHash, out Guid crntValue))
                    {
                        string crntBinPath = Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Index, Name, crntHash.ToString(), crntValue.ToString(), Globals.BinFile);
                        if (File.Exists(crntBinPath))
                        {
                            using FileStream fs2 = new(crntBinPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                            D newType = new();
                            newType.Deserialize(fs2);
                            return newType;
                        }
                    }
                    throw new CorruptedException<D>();
                }
            case ColumnType.Unique:
                {
                    if (D.Length > 0)
                    {

                    }
                    else // Is reference
                    {

                    }
                    break;
                }
            default: // None
                {
                    if (D.Length > 0)
                    {
                        using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Row, $"{record.Id}.{Globals.Rec}"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                        fs.Position = Offset;
                        D obj = new();
                        obj.Deserialize(fs);
                        return obj;
                    }
                    else // Is reference
                    {
                        using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Row, $"{record.Id}.{Name}"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                        D obj = new();
                        obj.Deserialize(fs);
                        return obj;
                    }
                }
        }
        return default;
    }

    public void Set<T>(T table, D value) where T : Tables
    {
        if (table.Id == Guid.Empty) table.Assign();
        switch (Type)
        {
            case ColumnType.Indexed:
                {
                    // Generate hash for new value
                    using MemoryStream ms = new();
                    Guid newHash = value.Hash(ms);

                    // Read previous value
                    Directory.CreateDirectory(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Rec));
                    using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Rec, $"{table.Id}.{Globals.Rec}"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

                    // Old hash/value exists, read it and delete its reference
                    fs.Position = Offset;
                    if (fs.Length - Offset >= 32 && fs.ReadHashValue(out Guid oldHash, out Guid oldValue))
                    {
                        // check if old hash/value is valid
                        if (oldValue != Guid.Empty)
                        {
                                string oldBinDir = Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Index, Name, oldHash.ToString(), oldValue.ToString());
                                string oldBinPath = Path.Combine(oldBinDir, Globals.BinFile);

                            // Hash matches, check with value
                            if (oldHash == newHash)
                            {
                                if (File.Exists(oldBinPath))
                                {
                                    using FileStream fs2 = new(oldBinPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                                    ms.Position = 0;
                                    if (Misc.FileCompare(ms, fs2))
                                    {
                                        // values match so no need to continue
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                // values differ, delete reference
                                File.Delete(Path.Combine(oldBinDir, $"{table.Id}.{Globals.Ref}"));
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
                                if (Misc.FileCompare(ms, fsCollision))
                                {
                                    File.Create(Path.Combine(hashCollision, $"{table.Id}.{Globals.Ref}"));
                                    string crntValue = Path.GetFileName(hashCollision);
                                    fs.Position = Offset;
                                    fs.WriteHashValue(newHash, Guid.Parse(crntValue));
                                    fs.Flush();
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

                    // write new info to record
                    fs.Position = Offset;
                    fs.WriteHashValue(newHash, newValue);
                    fs.Flush();

                    // add reference
                    File.Create(Path.Combine(newBinDir, $"{table.Id}.{Globals.Ref}"));

                    break;
                }
            case ColumnType.Unique:
                {

                    break;
                }
            default: // None
                {
                    if (D.Length > 0)
                    {
                        using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Row, $"{table.Id}.{Globals.Rec}"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                        fs.Position = Offset;
                        value.Serialize(fs);
                        fs.Flush();
                    }
                    else // Is reference
                    {
                        Directory.CreateDirectory(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Row));
                        using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Row, $"{table.Id}.{Name}"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                        value.Serialize(fs);
                        fs.Flush();
                    }
                    break;
                }
        }
    }
}