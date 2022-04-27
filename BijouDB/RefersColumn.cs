using BijouDB.DataTypes;

namespace BijouDB
{
    public sealed class RefersColumn<T, TResult> where T : Table, new() where TResult : Table, new()
    {
        private readonly Func<IndexsColumn<TResult, @record<T>>> _sourceColumn;
        
        /// <summary>
        /// If this column should prevent a record from being removed if it still holds child references.
        /// </summary>
        public bool Persistant { get; }

        public RefersColumn(Func<IndexsColumn<TResult, @record<T>>> sourceColumn, bool persistant)
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

        public bool HasRecords(record<T> data) => _sourceColumn().HasRecords(data);
    }
}
