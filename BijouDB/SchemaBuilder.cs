using System.Reflection;

namespace BijouDB;

public sealed class SchemaBuilder<R> : IDisposable
    where R : Record, new()
{
    private bool _disposed;

    internal long Length;
    internal int _count;

    internal readonly List<Func<Record, bool>> _references = new();
    internal readonly List<Action<Record>> _columns = new();

    private readonly List<Func<string>> _definitions = new();

    /// <summary>
    /// Adds an indexed column with optional value constraints and caching.
    /// </summary>
    /// <typeparam name="D">The IDataType of the Column.</typeparam>
    /// <param name="column">The Column to build.</param>
    /// <param name="Unique">Specifies whether values set in the column are unique.</param>
    /// <param name="Default">Specifies a default value for the column if a record doesn't have a value set.</param>
    /// <param name="Check">Value constraint logic.</param>
    /// <returns></returns>
    public static SchemaBuilder<R> Add<D>(out Column<D> column, bool Unique = false, Func<D> Default = default!, Func<D, bool> Check = null!, int Cache = 128)
        where D : IDataType, new()
    {
        SchemaBuilder<R> builder = new();
        return builder.Append(out column, Unique, Default, Check, Cache);
    }

    /// <summary>
    /// Adds a reference to a column in another record schema.
    /// </summary>
    public static SchemaBuilder<R> Add<RSource, D>(out Reference<D, RSource> column, Func<Column<D>> referenceColumn, bool restricted = true)
        where RSource : Record, new()
        where D : IDataType, new()
    {
        SchemaBuilder<R> builder = new();
        return builder.Append(out column, referenceColumn, restricted);
    }

    /// <summary>
    /// Adds one end of a reciprocal relationship, preserving the original two-argument API.
    /// </summary>
    public static SchemaBuilder<R> Add<R2>(out Relational<R, R2> column, Relational<R2, R>? _)
        where R2 : Record, new()
    {
        SchemaBuilder<R> builder = new();
        return builder.Append(out column, _, null);
    }

    /// <summary>
    /// Adds a named reciprocal relationship. Both ends must use the same name.
    /// </summary>
    public static SchemaBuilder<R> Add<R2>(out Relational<R, R2> column, Relational<R2, R>? reciprocal, string? name)
        where R2 : Record, new()
    {
        SchemaBuilder<R> builder = new();
        return builder.Append(out column, reciprocal, name);
    }

    /// <summary>
    /// Adds a named relationship without requiring circular field access.
    /// </summary>
    public static SchemaBuilder<R> Add<R2>(out Relational<R, R2> column, string name)
        where R2 : Record, new()
    {
        SchemaBuilder<R> builder = new();
        return builder.Append(out column, null, name);
    }

    internal SchemaBuilder<R> Append<D>(out Column<D> column, bool unique, Func<D> @default, Func<D, bool> check, int cache)
        where D : IDataType, new()
    {
        ThrowIfDisposed();
        int ordinal = _count++;
        long offset = Length;
        column = new(offset, $"{Globals.ColName}_{ordinal}", typeof(R), unique, @default, check, cache);
        Column<D> declared = column;
        _columns.Add(column.Remove);
        _definitions.Add(() => $"column|{ordinal}|{FieldName(declared)}|{offset}|{TypeName(typeof(D))}|unique={unique}");
        Length += 24;
        return this;
    }

    internal SchemaBuilder<R> Append<RSource, D>(out Reference<D, RSource> column, Func<Column<D>> referenceColumn, bool restricted)
        where RSource : Record, new()
        where D : IDataType, new()
    {
        ThrowIfDisposed();
        int ordinal = _count++;
        column = new(referenceColumn);
        Reference<D, RSource> declared = column;
        if (restricted)
            _references.Add(column.HasRecords<R>);
        _definitions.Add(() => $"reference|{ordinal}|{FieldName(declared)}|{TypeName(typeof(RSource))}|{TypeName(typeof(D))}|restricted={restricted}");
        return this;
    }

    internal SchemaBuilder<R> Append<R2>(out Relational<R, R2> column, Relational<R2, R>? reciprocal, string? name)
        where R2 : Record, new()
    {
        ThrowIfDisposed();
        int ordinal = _count++;
        column = new($"_{ordinal}", reciprocal, name);
        Relational<R, R2> declared = column;
        _columns.Add(column.Remove);
        _definitions.Add(() => $"relationship|{ordinal}|{FieldName(declared)}|{TypeName(typeof(R2))}|{declared.Name}");
        return this;
    }

    private static string FieldName(object column)
    {
        foreach (FieldInfo field in typeof(R).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            if (ReferenceEquals(field.GetValue(null), column)) return field.Name;

        return "<unbound>";
    }

    private static string TypeName(Type type) => type.IsGenericType ? type.GetGenericTypeDefinition().FullName + "[" + string.Join(",", type.GetGenericArguments()
        .Select(TypeName)) + "]" : type.FullName!;

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SchemaBuilder<R>));
    }

    public void Dispose()
    {
        if (_disposed) return;
        // Class initialization must not wait on the storage lock: another thread can
        // already hold it while waiting for this record's class constructor.
        Storage.DeclareSchema(typeof(R), _definitions.Select(describe => describe()).ToArray());
        Record.AddRemoveDefinition<R>(record => Storage.RemoveRecord(record, _columns, _references));
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public static SchemaBuilder<R> operator ~(SchemaBuilder<R> builder)
    {
        builder?.Dispose();
        return builder!;
    }
}

public static class SchemaBuilderExtensions
{
    public static SchemaBuilder<R> Add<R, D>(this SchemaBuilder<R> builder, out Column<D> column, bool Unique = false, Func<D> Default = default!, Func<D, bool> Check = null!, int Cache = 128)
        where R : Record, new()
        where D : IDataType, new() =>
        builder.Append(out column, Unique, Default, Check, Cache);

    public static SchemaBuilder<R> Add<R, RSource, D>(this SchemaBuilder<R> builder, out Reference<D, RSource> column, Func<Column<D>> referenceColumn, bool restricted = true)
        where R : Record, new()
        where RSource : Record, new()
        where D : IDataType, new() =>
        builder.Append(out column, referenceColumn, restricted);

    public static SchemaBuilder<R> Add<R, R2>(this SchemaBuilder<R> builder, out Relational<R, R2> column, Relational<R2, R>? _)
        where R : Record, new()
        where R2 : Record, new() =>
        builder.Append(out column, _, null);

    public static SchemaBuilder<R> Add<R, R2>(this SchemaBuilder<R> builder, out Relational<R, R2> column, Relational<R2, R>? reciprocal, string? name)
        where R : Record, new()
        where R2 : Record, new() =>
        builder.Append(out column, reciprocal, name);

    public static SchemaBuilder<R> Add<R, R2>(this SchemaBuilder<R> builder, out Relational<R, R2> column, string name)
        where R : Record, new()
        where R2 : Record, new() =>
        builder.Append(out column, null, name);
}
