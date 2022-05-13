namespace BijouDB.Columns;

public sealed class References<R, D> where R : Record, new() where D : IDataType, new()
{
    private readonly Func<Column<D>> _sourceColumn;

    public References(Func<Column<D>> sourceColumn) => _sourceColumn = sourceColumn;

    /// <summary>
    /// Gets a collection of all child records referenced by this column.
    /// </summary>
    /// <param name="value">The parent record.</param>
    /// <returns>A collection with all child records.</returns>
    public R[] For(D value) => _sourceColumn().RecordsWithValue<R>(value);

    /// <summary>
    /// Checks whether a record exists which references the current record.
    /// </summary>
    /// <param name="record"></param>
    /// <returns>returns true if a record exists, otherwise false.</returns>
    public bool HasRecords(D value) => _sourceColumn().HasRecords<R>(value);
}
