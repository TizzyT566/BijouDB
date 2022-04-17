using BijouDB.Exceptions;
using BijouDB.Primitives;

namespace BijouDB;

public class TableBuilder
{
    private long _offset = 0;
    private int _count = 0;

    private readonly HashSet<string> _columnNames = new();

    public TableBuilder Add<T>(out Column<T> column, ColumnType attribute) where T : IDataType, new() => Add(out column, null, attribute);
    public TableBuilder Add<T>(out Column<T> column, string? columnName = null, ColumnType type = ColumnType.None) where T : IDataType, new()
    {
        if (columnName is not null) Misc.EnsureAlphaNumeric(columnName);
        columnName = $"{Globals.ColName}_{columnName ?? _count.ToString()}";
        if (!_columnNames.Add(columnName)) throw new DuplicateColumnException(columnName);
        column = new(columnName, _offset, type);
        _offset += T.Length;
        _count++;
        return this;
    }
}
