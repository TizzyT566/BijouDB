using BijouDB.Columns;

namespace BijouDB;

public sealed class SchemaBuilder<R> where R : Record, new()
{
    internal LengthRef Length = new();
    internal int _count = 0;

    // indexed column
    public static SchemaBuilder<R> AddCol<D>(out Column<D> column, bool Unique = false, Func<D> Default = default!, Func<D, bool> Check = null!) where D : IDataType, new()
    {
        SchemaBuilder<R> builder = new();
        string columnName = $"{Globals.ColName}_{builder._count}";
        column = new(builder.Length, columnName, typeof(R), Unique, Default, Check);
        builder.Length += 32;
        builder._count++;
        return builder;
    }

    // column which references other records which are related
    public static SchemaBuilder<R> AddRef<RSource, D>(out References<RSource, D> column, Func<Column<D>> referenceColumn) where RSource : Record, new() where D : IDataType, new()
    {
        SchemaBuilder<R> builder = new();
        References<RSource, D> newColumn = new(referenceColumn);
        column = newColumn;
        return builder;
    }

    //public void Remove(out Func<T, bool> remover)
    //{
    //    remover = (r) =>
    //    {
    //            // Child references check
    //            // If there are persistant chilren, prevent deletion
    //        foreach (Func<T, IReadOnlySet<Guid>> f in persistantColumns)
    //            if (f(r).Count > 0)
    //                return false;

    //            // Go through all indexed columns and delete all record references
    //        foreach (IndexedColumn col in indexedColumns)
    //            col.Remove(r.Id);

    //            // Delete main record file
    //        try
    //        {
    //            string recordPath = Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.Rec, $"{r.Id}.{Globals.Rec}");
    //            File.Delete(recordPath);
    //            return true;
    //        }
    //        catch (Exception)
    //        {
    //            return false;
    //        }
    //    };
    //}
}

public static class SchemaBuilderExtensions
{
    // indexed column
    public static SchemaBuilder<R> AddCol<R, D>(this SchemaBuilder<R> @this, out Column<D> column, bool Unique = false, Func<D> Default = default!, Func<D, bool> Check = null!) where R : Record, new() where D : IDataType, new()
    {
        string columnName = $"{Globals.ColName}_{@this._count}";
        column = new(@this.Length, columnName, typeof(R), Unique, Default, Check);
        @this.Length += 32;
        @this._count++;
        return @this;
    }

    // column which references other records which are related
    public static SchemaBuilder<R> AddRef<R, RSource, D>(this SchemaBuilder<R> @this, out References<RSource, D> column, Func<Column<D>> referenceColumn) where R : Record, new() where RSource : Record, new() where D : IDataType, new()
    {
        References<RSource, D> newColumn = new(referenceColumn);
        column = newColumn;
        return @this;
    }
}