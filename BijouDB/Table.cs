using BijouDB.Exceptions;
using BijouDB.DataTypes;

namespace BijouDB;

public abstract class Table
{
    private Guid? _guid;
    public Guid Id => _guid ?? Guid.Empty;
    public bool Active => Id != Guid.Empty;

    public void Assign() => _guid ??= IncrementalGuid.NextGuid();

    public static bool TryGet<T>(Guid id, out T? record) where T : Table, new()
    {
        try
        {
            using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Rec, $"{id}.{Globals.Rec}"), FileMode.Open, FileAccess.Read, FileShare.Read);
            record = new();
            record._guid = id;
            return true;
        }
        catch (Exception ex)
        {
            if (Globals.Logging) Console.WriteLine(ex.ToString());
            record = null;
            return false;
        }
    }

    //public abstract bool Remove();

    public sealed class ColumnBuilder
    {
        private LengthRef Length = new();
        private int _count = 0;

        private readonly HashSet<string> _columnNames = new();

        public ColumnBuilder Indexs<T, D>(out IndexedColumn<T, D> column, ColumnType type = ColumnType.None, string? columnName = null) where T : Table, new() where D : IDataType, new()
        {
            //if (type.HasFlag(ColumnType.Protected) && typeof(D).IsValueType)
            //    throw new InvalidProtectedDataTypeException();

            if (columnName is not null) Misc.EnsureAlphaNumeric(columnName);
            columnName = $"{Globals.ColName}_{columnName ?? _count.ToString()}";
            if (!_columnNames.Add(columnName)) throw new DuplicateColumnException(columnName);
            column = new(type, Length, columnName, Length);
            Length += type == ColumnType.None ? D.Length : 32;
            _count++;
            return this;
        }

        public ColumnBuilder Refers<T1, T2>(out ReferencesColumn<T1, T2> column, Func<IndexedColumn<T2, @record<T1>>> referenceColumn) where T1 : Table, new() where T2 : Table, new()
        {
            //if (type.HasFlag(ColumnType.Protected) && typeof(D).IsValueType)
            //    throw new InvalidProtectedDataTypeException();

            //if (columnName is not null) Misc.EnsureAlphaNumeric(columnName);
            //columnName = $"{Globals.ColName}_{columnName ?? _count.ToString()}";
            //if (!_columnNames.Add(columnName)) throw new DuplicateColumnException(columnName);
            //column = new(type, Length, columnName, Length);
            //Length += type == ColumnType.None ? T2.Length : 32;
            //_count++;
            column = new(referenceColumn);
            return this;
        }

        public void Remove<T>(out Func<T, bool> remover) where T : Table
        {



            remover = (record) => { return false; };
        }
    }
}
