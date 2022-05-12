using BijouDB.Exceptions;
using BijouDB.DataTypes;

namespace BijouDB;

public abstract partial class Schema
{
    public sealed class ColumnBuilder<T> where T : Schema, new()
    {
        private LengthRef Length = new();
        private int _count = 0;

        private readonly HashSet<string> _columnNames = new();

        private readonly List<Func<T, IReadOnlySet<Guid>>> persistantColumns = new();
        private readonly List<IndexedColumn> indexedColumns = new();

        public ColumnBuilder<T> Index<D>(out IndexedColumn<T, D> column, ColumnType type = ColumnType.None, string? columnName = null) where D : IDataType, new()
        {
            if (columnName is not null) Misc.EnsureAlphaNumeric(columnName);
            columnName = $"{Globals.ColName}_{columnName ?? _count.ToString()}";
            if (!_columnNames.Add(columnName)) throw new DuplicateColumnException(columnName);
            column = new(type, Length, columnName, Length);
            indexedColumns.Add(column);
            Length += type == ColumnType.None ? D.Length : 32;
            _count++;
            return this;
        }

        public ColumnBuilder<T> Refer<T2>(out ReferenceColumn<T, T2> column, Func<IndexedColumn<T2, @record<T>>> referenceColumn, bool persistant = true) where T2 : Schema, new()
        {
            ReferenceColumn<T, T2> newColumn = new(referenceColumn, persistant);
            column = newColumn;
            if (persistant) persistantColumns.Add(r => newColumn.Get(r));
            return this;
        }

        public void Remove(out Func<T, bool> remover)
        {
            remover = (r) =>
            {
                // Child references check
                // If there are persistant chilren, prevent deletion
                foreach (Func<T, IReadOnlySet<Guid>> f in persistantColumns)
                    if (f(r).Count > 0)
                        return false;

                // Go through all indexed columns and delete all record references
                foreach (IndexedColumn col in indexedColumns)
                    col.Remove(r.Id);

                // Delete main record file
                try
                {
                    string recordPath = Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Rec, $"{r.Id}.{Globals.Rec}");
                    File.Delete(recordPath);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            };
        }
    }
}
