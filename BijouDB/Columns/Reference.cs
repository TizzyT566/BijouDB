using BijouDB.Components;

namespace BijouDB;

public sealed class Reference<R, RReturn>
    where R : Record, new()
    where RReturn : Record, new()
{
    private readonly Func<Column<@record<R>>> _sourceColumn;

    internal Column<@record<R>> SourceColumn => _sourceColumn();

    internal Reference(Func<Column<@record<R>>> sourceColumn) => _sourceColumn = sourceColumn;

    /// <summary>
    /// Gets all child records referenced by this column.
    /// </summary>
    /// <param name="value">The parent record.</param>
    /// <returns>A collection with all child records.</returns>
    public RReturn[] To(R value) => _sourceColumn().WithValue<RReturn>(value);

    /// <summary>
    /// Checks to see if a record has any child references.
    /// </summary>
    /// <typeparam name="S">The child column type.</typeparam>
    /// <param name="record">The parent record to check for references.</param>
    /// <returns>true if record has child records referencing it, otherwise false.</returns>
    public bool HasRecords<S>(Record record)
        where S : Record, new()
    {
        Type target = typeof(R);

        using FileBackedStream ms = new();

        ulong hash;

        // ugly check, refactor in the future ...
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
        string hashDir;
        try
        {
            hashDir = Path.Combine(Globals.DatabasePath, typeof(RReturn).FullName!, Globals.Index, _sourceColumn()._name, hash.PaddedString());
        }
        catch (Exception ex)
        {
            ex.Log();
            return false;
        }

        try
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
                            if (Guid.TryParse(Path.GetFileNameWithoutExtension(reference), out Guid id) && Record.TryGet<RReturn>(id, out _))
                                return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ex?.Log();
        }
        return false;
    }
}