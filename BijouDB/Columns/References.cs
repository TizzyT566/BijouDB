using BijouDB.Components;
using BijouDB.DataTypes;

namespace BijouDB;

public sealed class References<R, D>
    where R : Record, new()
    where D : IDataType, new()
{
    private readonly Func<Column<D>> _sourceColumn;

    public Column<D> SourceColumn => _sourceColumn();

    public References(Func<Column<D>> sourceColumn) => _sourceColumn = sourceColumn;

    /// <summary>
    /// Gets a collection of all child records referenced by this column.
    /// </summary>
    /// <param name="value">The parent record.</param>
    /// <returns>A collection with all child records.</returns>
    public R[] For(D value) => _sourceColumn().RecordsWithValue<R>(value);

    internal bool HasRecords<S>(Record record)
        where S : Record, new()
    {
        Type target = typeof(D);

        using FileBackedStream ms = new();

        Guid hash;

        if (typeof(@record<S>) == target)
        {
            @record<S> r = (@record<S>)record!;
            hash = r.Hash(ms);
        }
        else if (typeof(@record<S>.nullable) == target)
        {
            @record<S>.nullable r = (@record<S>.nullable)record!;
            hash = r.Hash(ms);
        }
        else return false;

        // hash lookup
        string hashDir = Path.Combine(Globals.DB_Path, typeof(R).FullName!, Globals.Index, _sourceColumn()._name, hash.ToString());
        if (Directory.Exists(hashDir))
        {
            foreach (string hashCollision in Directory.EnumerateDirectories(hashDir))
            {
                string collisionName = Path.GetFileNameWithoutExtension(hashCollision);
                string binFilePath = Path.Combine(hashCollision, Globals.BinFile);
                if (Guid.TryParse(collisionName, out Guid index) && File.Exists(binFilePath))
                {
                    using FileStream fs = new(binFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    ms.Position = 0;
                    if (Misc.StreamCompare(ms, fs))
                    {
                        string dataMatchPath = Path.Combine(hashDir, index.ToString());
                        foreach (string reference in Directory.EnumerateFiles(dataMatchPath, Globals.RefPattern))
                            if (Guid.TryParse(Path.GetFileNameWithoutExtension(reference), out Guid id) && Record.TryGet<R>(id, out _))
                                return true;
                    }
                }
            }
        }
        return false;
    }
}
