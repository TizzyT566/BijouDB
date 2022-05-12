using BijouDB.DataTypes;

namespace BijouDB
{
    public sealed class ReferenceColumn<T, TResult> where T : Schema, new() where TResult : Schema, new()
    {
        private readonly Func<IndexedColumn<TResult, @record<T>>> _sourceColumn;
        
        /// <summary>
        /// If this column should prevent a record from being removed if it still holds child references.
        /// </summary>
        public bool Persistant { get; }

        public ReferenceColumn(Func<IndexedColumn<TResult, @record<T>>> sourceColumn, bool persistant)
        {
            _sourceColumn = sourceColumn;
            Persistant = persistant;
        }

        /// <summary>
        /// Gets a collection of all child records referenced by this column.
        /// </summary>
        /// <param name="record">The parent record.</param>
        /// <returns>A collection with all child records.</returns>
        public IReadOnlySet<Guid> Get(T record) => _sourceColumn().RecordsWithValue(record);

        /// <summary>
        /// Checks whether a record exists which references the current record.
        /// </summary>
        /// <param name="record"></param>
        /// <returns>returns true if a record exists, otherwise false.</returns>
        public bool HasRecords(record<T> record) => _sourceColumn().HasRecords(record);
    }
}
