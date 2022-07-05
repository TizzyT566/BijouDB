namespace BijouDB.Components;

internal class Cache<D> where D : IDataType
{
    private readonly Dictionary<Guid, Node> _dict = new();
    private readonly int _count;
    private Node? _head;
    private int _lock;

    public Cache(int count) => _count = count > 0 ? count : throw new InvalidOperationException("Unexpected Error, Cache must be greater than 0.");

    public void Set(Guid key, D value)
    {
        while (Interlocked.Exchange(ref _lock, 1) == 1) ;

        if (_dict.TryGetValue(key, out Node node))
        {
            node._value = value;

            MoveToHead(node);

            Interlocked.Exchange(ref _lock, 0);
            return;
        }

        Evict();

        // Add new node
        Node newNode = new(key, value) { _next = _head };
        newNode._prev = _head is null ? newNode : _head._prev;
        _dict[key] = _head = newNode;

        Interlocked.Exchange(ref _lock, 0);
    }

    private void Evict()
    {
        while (_dict.Count >= _count)
        {
            switch (_dict.Count)
            {
                case 1:
                    {
                        _dict.Remove(_head!._key);
                        _head = null;
                        break;
                    }
                case 2:
                    {
                        _dict.Remove(_head!._prev!._key);
                        _head!._next = null;
                        _head._prev = _head;
                        break;
                    }
                default:
                    {
                        _dict.Remove(_head!._prev!._key);
                        _head!._prev = _head!._prev!._prev;
                        break;
                    }
            }
        }
    }

    private void MoveToHead(Node node)
    {
        if (node == _head) return;

        node._prev!._next = node._next;

        if (node._next is not null)
            node._next._prev = node._prev;
        if (_head!._prev != node)
            node._prev = _head._prev;

        node._next = _head;
        _head = node;
    }

    public void Remove(Guid key)
    {
        while (Interlocked.Exchange(ref _lock, 1) == 1) ;

        if (_dict.TryGetValue(key, out Node node))
        {
            _dict.Remove(key);

            if (node._prev is not null)
                node._prev._next = node._next;
            if (node._next is not null)
                node._next._prev = node._prev;
        }

        Interlocked.Exchange(ref _lock, 0);
    }

    public bool TryGet(Guid key, out D? value)
    {
        while (Interlocked.Exchange(ref _lock, 1) == 1) ;

        if (!_dict.TryGetValue(key, out Node node))
        {
            Interlocked.Exchange(ref _lock, 0);
            value = default;
            return false;
        }

        value = node._value;

        MoveToHead(node);

        Interlocked.Exchange(ref _lock, 0);
        return true;
    }

    public class Node
    {
        public Guid _key;
        public D? _value;
        public Node? _next, _prev;

        public Node(Guid key, D? value, Node? Next = null, Node? Prev = null)
        {
            _key = key;
            _value = value;
            _next = Next;
            _prev = Prev;
        }
    }
}
