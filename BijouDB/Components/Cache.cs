namespace BijouDB.Components;

internal sealed class Cache<D>
    where D : IDataType
{
    private readonly Dictionary<Guid, LinkedListNode<KeyValuePair<Guid, D>>> _entries = new();
    private readonly LinkedList<KeyValuePair<Guid, D>> _recency = new();
    private readonly object _sync = new();
    private readonly int _capacity;

    public Cache(int count) => _capacity = count > 0 ? count : throw new ArgumentOutOfRangeException(nameof(count));

    public bool TryGet(Guid key, out D? value)
    {
        lock (_sync)
        {
            if (!_entries.TryGetValue(key, out LinkedListNode<KeyValuePair<Guid, D>> node))
            {
                value = default;
                return false;
            }

            _recency.Remove(node);
            _recency.AddFirst(node);
            value = node.Value.Value;
            return true;
        }
    }

    public void Set(Guid key, D value)
    {
        lock (_sync)
        {
            if (_entries.TryGetValue(key, out LinkedListNode<KeyValuePair<Guid, D>> node))
            {
                node.Value = new(key, value);
                _recency.Remove(node);
                _recency.AddFirst(node);
                return;
            }

            if (_entries.Count == _capacity)
            {
                LinkedListNode<KeyValuePair<Guid, D>> oldest = _recency.Last!;
                _entries.Remove(oldest.Value.Key);
                _recency.RemoveLast();
            }

            _entries.Add(key, _recency.AddFirst(new KeyValuePair<Guid, D>(key, value)));
        }
    }

    public void Remove(Guid key)
    {
        lock (_sync)
        {
            if (_entries.TryGetValue(key, out LinkedListNode<KeyValuePair<Guid, D>> node))
            {
                _recency.Remove(node);
                _entries.Remove(key);
            }
        }
    }
}
