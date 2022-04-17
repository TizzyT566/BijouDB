namespace BijouDB;

public abstract class Table
{
    private Guid? _guid;
    public Guid Id => _guid ?? Guid.Empty;

    public void Assign() => _guid ??= IncrementalGuid.NextGuid();

    public static T Load<T>(Guid id) where T : Table, new()
    {
        using FileStream fs = new(Path.Combine(Globals.DB_Path, typeof(T).FullName!, Globals.RowName, id.ToString()), FileMode.Open, FileAccess.Read, FileShare.Read);
        T loadSchema = new();
        loadSchema._guid = id;
        return loadSchema;
    }
}
