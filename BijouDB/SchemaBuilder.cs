using BijouDB.Columns;

namespace BijouDB;

public sealed class SchemaBuilder<R> where R : Record, new()
{
    private LengthRef Length = new();
    private int _count = 0;

    // indexed column
    public SchemaBuilder<R> Add<D>(out Column<D> column) where D : IDataType, new()
    {
        string columnName = $"{Globals.ColName}_{_count}";
        column = new(Length, columnName, typeof(R));
        Length += 32;
        _count++;
        return this;
    }

    // column which references other records which are related
    public SchemaBuilder<R> Add<D>(Func<Column<D>> referenceColumn, out References<R, D> column) where D : IDataType, new()
    {
        References<R, D> newColumn = new(referenceColumn);
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