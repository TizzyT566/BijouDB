using BijouDB.Exceptions;
using BijouDB.DataTypes;

namespace BijouDB;

public abstract partial class Table
{
    public sealed class ColumnBuilder<T> where T : Table, new()
    {
        private LengthRef Length = new();
        private int _count = 0;

        private readonly HashSet<string> _columnNames = new();

        private readonly List<Func<T, IReadOnlySet<Guid>>> persistantColumns = new();

        public ColumnBuilder<T> Indexs<D>(out IndexedColumn<T, D> column, ColumnType type = ColumnType.None, string? columnName = null) where D : IDataType, new()
        {
            if (columnName is not null) Misc.EnsureAlphaNumeric(columnName);
            columnName = $"{Globals.ColName}_{columnName ?? _count.ToString()}";
            if (!_columnNames.Add(columnName)) throw new DuplicateColumnException(columnName);
            column = new(type, Length, columnName, Length);
            Length += type == ColumnType.None ? D.Length : 32;
            _count++;
            return this;
        }

        public ColumnBuilder<T> Refers<T2>(out ReferencesColumn<T, T2> column, Func<IndexedColumn<T2, @record<T>>> referenceColumn, bool persistant = true) where T2 : Table, new()
        {
            ReferencesColumn<T, T2> newColumn = new(referenceColumn, persistant);
            column = newColumn;
            if (persistant) persistantColumns.Add(r => newColumn.Get(r));
            return this;
        }

        public void Remove(out Func<T, bool> remover)
        {
            remover = (r) =>
            {
                // child references check
                foreach (Func<T, IReadOnlySet<Guid>> f in persistantColumns)
                    if (f(r).Count > 0)
                        return false;

                ///// actual remove code here
                // 1) Go through all indexed columns and delete all record references
                // 2) Delete main record file

                return true;
            };


            remover = (record) => { return false; };
        }
    }
}
