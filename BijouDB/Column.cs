using BijouDB.Primitives;

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

    public D Get<T>(T table) where T : Table
    {
        switch (Type)
        {
            case ColumnType.Indexed:
                {
                    if (D.Length > 0)
                    {
                        using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.RowName, $"{table.Id}.{Globals.RecName}"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    }
                    else // Is reference
                    {

                    }
                    break;
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
                        using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.RowName, $"{table.Id}.{Globals.RecName}"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                        fs.Position = Offset;
                        D obj = new();
                        obj.Deserialize(fs);
                        return obj;
                    }
                    else // Is reference
                    {
                        using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.RowName, $"{table.Id}.{Name}"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                        D obj = new();
                        obj.Deserialize(fs);
                        return obj;
                    }
                }
        }
        return default;
    }

    public void Set<T>(T table, D value) where T : Table
    {
        switch (Type)
        {
            case ColumnType.Indexed:
                {
                    if (D.Length > 0)
                    {
                        using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.RowName, $"{table.Id}.{Globals.RecName}"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    }
                    else // Is reference
                    {

                    }
                    break;
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
                        using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.RowName, $"{table.Id}.{Globals.RecName}"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                        fs.Position = Offset;
                        value.Serialize(fs);
                        fs.Flush();
                    }
                    else // Is reference
                    {
                        using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.RowName, $"{table.Id}.{Name}"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                        value.Serialize(fs);
                        fs.Flush();
                    }
                    break;
                }
        }
    }
}