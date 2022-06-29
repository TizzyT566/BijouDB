﻿namespace BijouDB;

public sealed class SchemaBuilder<R> : IDisposable
    where R : Record, new()
{
    private bool disposedValue;
    internal long Length = 0;
    internal int _count = 0;
    internal readonly List<Func<Record, bool>> _references = new();
    internal readonly List<Action<Record>> _columns = new();

    // indexed column
    public static SchemaBuilder<R> Add<D>(out Column<D> column, bool Unique = false, Func<D> Default = default!, Func<D, bool> Check = null!)
        where D : IDataType, new()
    {
        SchemaBuilder<R> builder = new();

        column = new(builder.Length, $"{Globals.ColName}_{builder._count++}", typeof(R), Unique, Default, Check);
        builder._columns.Add(column.Remove);
        builder.Length += 24;
        return builder;
    }

    // column which references other records which are related
    public static SchemaBuilder<R> Add<RSource, D>(out Reference<D, RSource> column, Func<Column<D>> referenceColumn, bool restricted = true)
        where RSource : Record, new()
        where D : IDataType, new()
    {
        SchemaBuilder<R> builder = new();
        builder._count++;
        column = new(referenceColumn);
        if (restricted) builder._references.Add(column.HasRecords<R>);
        return builder;
    }

    // relational column
    public static SchemaBuilder<R> Add<R2>(out Relational<R, R2> column, Relational<R2, R> _)
        where R2 : Record, new()
    {
        SchemaBuilder<R> builder = new();
        column = new($"_{builder._count++}");
        builder._columns.Add(column.Remove);
        return builder;
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
                Record.AddRemoveDefinition<R>(record =>
                {
                    foreach (Func<Record, bool> referenceCheck in _references)
                        if (referenceCheck(record))
                            return;

                    try
                    {
                        foreach (Action<Record> remove in _columns) remove(record);
                        string recordPath = Path.Combine(Globals.DatabasePath, typeof(R).FullName!, Globals.Rec, $"{record.Id}.{Globals.Rec}");
                        File.Delete(recordPath);
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
                });

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public static SchemaBuilder<R> operator ~(SchemaBuilder<R> a)
    {
        a?.Dispose();
        return a!;
    }
}

public static class SchemaBuilderExtensions
{
    // indexed column
    public static SchemaBuilder<R> Add<R, D>(this SchemaBuilder<R> @this, out Column<D> column, bool Unique = false, Func<D> Default = default!, Func<D, bool> Check = null!)
        where R : Record, new()
        where D : IDataType, new()
    {
        column = new(@this.Length, $"{Globals.ColName}_{@this._count++}", typeof(R), Unique, Default, Check);
        @this._columns.Add(column.Remove);
        @this.Length += 24;
        return @this;
    }

    // column which references other records which are related
    public static SchemaBuilder<R> Add<R, RSource, D>(this SchemaBuilder<R> @this, out Reference<D, RSource> column, Func<Column<D>> referenceColumn, bool restricted = true)
        where R : Record, new()
        where RSource : Record, new()
        where D : IDataType, new()
    {
        column = new(referenceColumn);
        @this._count++;
        if (restricted) @this._references.Add(column.HasRecords<R>);
        return @this;
    }

    // relational column
    public static SchemaBuilder<R> Add<R, R2>(this SchemaBuilder<R> @this, out Relational<R, R2> column, Relational<R2, R> _)
        where R : Record, new()
        where R2 : Record, new()
    {
        column = new($"_{@this._count++}");
        @this._columns.Add(column.Remove);
        return @this;
    }
}