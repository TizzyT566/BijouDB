using BijouDB.Exceptions;

namespace BijouDB;

public class TableBuilder
{
    private long Length = 0;
    private int _count = 0;

    private readonly HashSet<string> _columnNames = new();

    public TableBuilder Add<D>(out Column<D> column, ColumnType type = ColumnType.None, string? columnName = null) where D : IDataType, new()
    {
        if (columnName is not null) Misc.EnsureAlphaNumeric(columnName);
        columnName = $"{Globals.ColName}_{columnName ?? _count.ToString()}";
        if (!_columnNames.Add(columnName)) throw new DuplicateColumnException(columnName);
        column = new(type, Length, columnName);
        Length += type switch
        {
            ColumnType.Indexed => 32,
            ColumnType.Unique => 32,
            _ => D.Length
        };
        _count++;
        return this;
    }
}
