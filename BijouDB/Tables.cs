using BijouDB.Exceptions;

namespace BijouDB;

public interface ITable<I, T> where T : Tables
{
    public I AsRecord { get; }
}
public abstract class Tables
{
    private Guid? _guid;
    public Guid Id => _guid ?? Guid.Empty;

    public void Assign() => _guid ??= IncrementalGuid.NextGuid();

    public static bool TryGet<T>(Guid id, out T? record) where T : Tables, new()
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

    public abstract bool Remove();

    public sealed class ColumnBuilder
    {
        private LengthRef Length = new();
        private int _count = 0;

        private readonly HashSet<string> _columnNames = new();

        public ColumnBuilder Column<T, D>(out Column<T, D> column, ColumnType type = ColumnType.None, string? columnName = null) where T : Tables, new() where D : IDataType, new()
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

        public void Remove<T>(out Func<T, bool> remover) where T : Tables
        {



            remover = (record) => { return false; };
        }
    }
}
