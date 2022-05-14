namespace BijouDB;

public sealed class SchemaBuilder<R>
    where R : Record, new()
{
    internal LengthRef Length = new();
    internal int _count = 0;
    internal bool _built = false;

    internal readonly List<Func<Record, bool>> _references = new();
    internal readonly List<Action<Record>> _columns = new();

    // indexed column
    public static SchemaBuilder<R> Add<D>(out Column<D> column, bool Unique = false, Func<D> Default = default!, Func<D, bool> Check = null!)
        where D : IDataType, new()
    {
        SchemaBuilder<R> builder = new();
        string columnName = $"{Globals.ColName}_{builder._count}";
        column = new(builder.Length, columnName, typeof(R), Unique, Default, Check);
        builder._columns.Add(column.Remove);
        builder.Length += 32;
        builder._count++;
        return builder;
    }

    // column which references other records which are related
    public static SchemaBuilder<R> Add<RSource, D>(out References<RSource, D> column, Func<Column<D>> referenceColumn)
        where RSource : Record, new()
        where D : IDataType, new()
    {
        SchemaBuilder<R> builder = new();
        column = new(referenceColumn);
        builder._references.Add(column.HasRecords<R>);
        return builder;
    }

    public void Build()
    {
        if (_built) throw new InvalidOperationException();
        Record.AddRemoveDefinition<R>(record =>
        {
            foreach (Func<Record, bool> referenceCheck in _references)
                if (referenceCheck(record))
                    throw new Exception("There are references to the current record.");

            foreach (Action<Record> remove in _columns)
                remove(record);

            string recordPath = Path.Combine(Globals.DB_Path, typeof(R).FullName!, Globals.Rec, $"{record.Id}.{Globals.Rec}");
            if(File.Exists(recordPath))
                File.Delete(recordPath);
        });
        _built = true;
    }
}

public static class SchemaBuilderExtensions
{
    // indexed column
    public static SchemaBuilder<R> Add<R, D>(this SchemaBuilder<R> @this, out Column<D> column, bool Unique = false, Func<D> Default = default!, Func<D, bool> Check = null!)
        where R : Record, new()
        where D : IDataType, new()
    {
        if (@this._built) throw new InvalidOperationException();
        string columnName = $"{Globals.ColName}_{@this._count}";
        column = new(@this.Length, columnName, typeof(R), Unique, Default, Check);
        @this._columns.Add(column.Remove);
        @this.Length += 32;
        @this._count++;
        return @this;
    }

    // column which references other records which are related
    public static SchemaBuilder<R> Add<R, RSource, D>(this SchemaBuilder<R> @this, out References<RSource, D> column, Func<Column<D>> referenceColumn)
        where R : Record, new()
        where RSource : Record, new()
        where D : IDataType, new()
    {
        if (@this._built) throw new Exception();
        column = new(referenceColumn);
        @this._references.Add(column.HasRecords<R>);
        return @this;
    }
}