using BijouDB.Exceptions;

namespace BijouDB;

public class ColumnBuilder
{
    private LengthRef Length = new();
    private int _count = 0;

    private readonly HashSet<string> _columnNames = new();

    public ColumnBuilder Add<T, D>(out Column<T, D> column, ColumnType type = ColumnType.None, string? columnName = null) where T : Tables, new() where D : IDataType, new()
    {
        if (columnName is not null) Misc.EnsureAlphaNumeric(columnName);
        columnName = $"{Globals.ColName}_{columnName ?? _count.ToString()}";
        if (!_columnNames.Add(columnName)) throw new DuplicateColumnException(columnName);
        column = new(type, Length, columnName, Length);
        Length += type == ColumnType.None ? D.Length : 32;
        _count++;
        return this;
    }

    public class LengthRef
    {
        private long _length = 0;
        public static implicit operator long(LengthRef lengthRef) => lengthRef._length;
        public static LengthRef operator +(LengthRef src, long amnt)
        {
            src._length += amnt;
            return src;
        }
    }
}
