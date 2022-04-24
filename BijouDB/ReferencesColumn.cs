using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BijouDB.DataTypes;

namespace BijouDB
{
    public sealed class ReferencesColumn<T, TResult> where T : Table, new() where TResult : Table, new()
    {
        private readonly Func<IndexedColumn<TResult, @record<T>>> _sourceColumn;

        public ReferencesColumn(Func<IndexedColumn<TResult, @record<T>>> sourceColumn) => _sourceColumn = sourceColumn;

        public ColumnType Type => throw new NotImplementedException();

        public TResult[] Get(T record)
        {
            throw new NotImplementedException();
        }

        public bool HasRecordsWithIndexedValue(record<T> data)
        {
            throw new NotImplementedException();
        }

        public bool IndexedValueExists(record<T> data, out Guid hash, out Guid value)
        {
            throw new NotImplementedException();
        }

        public ReadOnlyDictionary<Guid, T> RecordsWithIndexedValue(record<T> data)
        {
            throw new NotImplementedException();
        }
    }
}
