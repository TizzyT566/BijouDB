using BijouDB.Columns;

namespace BijouDB;

public sealed class SchemaBuilder<R> where R : Record, new()
{
    private LengthRef Length = new();
    private int _count = 0;

    // normal unindexed column
    public SchemaBuilder<R> Column<D>(out Column<D> column) where D : IDataType
    {
        column = default!;

        return this;
    }

    // indexed column
    public SchemaBuilder<R> Indexed<D>(out IndexedColumn<R, D> column) where D : IDataType, new()
    {
        string columnName = $"{Globals.ColName}_{_count}";
        column = new(Length, columnName);
        Length += 32;
        _count++;
        return this;
    }

    // column which references other records which are related
    public SchemaBuilder<R> Reference<D>(Func<IndexedColumn<R, D>> referenceColumn, out ReferenceColumn<R, D> column) where D : IDataType, new()
    {
        ReferenceColumn<R, D> newColumn = new(referenceColumn);
        column = newColumn;
        return this;
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