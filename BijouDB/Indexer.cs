namespace BijouDB;

public sealed class Indexer<R, D>
    where D : IDataType, new()
    where R : Record, new()
{
    private readonly Func<D, R[]> _get;

    /// <summary>
    /// The underlying indexer.
    /// </summary>
    /// <param name="index">Index value.</param>
    public R this[D index]
    {
        get
        {
            R[] ret = _get(index);
            if (ret.Length > 0) return ret[0];
            throw new IndexOutOfRangeException("Record with index doesn't exist.");
        }
    }

    /// <summary>
    /// Constructs a new Indexer.
    /// </summary>
    /// <param name="get">The get accessor.</param>
    public Indexer(Func<D, R[]> get)
    {
        if(get is null) throw new ArgumentNullException(nameof(get));
        _get = get;
    }
}
