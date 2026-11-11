namespace BijouDB;

public sealed class Reference<D, R>
    where D : IDataType, new()
    where R : Record, new()
{
    private readonly Func<Column<D>> _sourceColumn;

    internal Column<D> SourceColumn => _sourceColumn() ?? throw new InvalidOperationException("The referenced column has not been initialized.");

    internal Reference(Func<Column<D>> sourceColumn) => _sourceColumn = sourceColumn ?? throw new ArgumentNullException(nameof(sourceColumn));

    /// <summary>
    /// Gets child records whose referenced column matches the supplied value.
    /// </summary>
    /// <param name="value">The parent record.</param>
    /// <returns>A collection with all child records.</returns>
    public IEnumerable<R> To(D value)
    {
        lock (Storage.SyncRoot)
        {
            Storage.EnsureInitialized();
            return SourceColumn.WithValue<R>(value).ToArray();
        }
    }

    /// <summary>
    /// Checks whether any existing child record refers to this parent.
    /// </summary>
    /// <typeparam name="S">The child column type.</typeparam>
    /// <param name="record">The parent record to check for references.</param>
    /// <returns>true if record has child records referencing it, otherwise false.</returns>
    public bool HasRecords<S>(Record record)
        where S : Record, new()
    {
        if (record is not S parent) throw new ArgumentException("The parent record has an incompatible type.", nameof(record));

        lock (Storage.SyncRoot)
        {
            Storage.EnsureInitialized();
            if (typeof(D) == typeof(@record<S>)) return SourceColumn.WithValue<R>((D)(object)(@record<S>)parent).Any();
            if (typeof(D) == typeof(@record<S>.nullable)) return SourceColumn.WithValue<R>((D)(object)(@record<S>.nullable)parent).Any();
            return false;
        }
    }
}
