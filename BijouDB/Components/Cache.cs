namespace BijouDB.Components;

internal class Cache<D> where D : IDataType
{
    private readonly Dictionary<Guid, Node> _dict = new();
    private readonly int _count;
    private Node? _head, _tail;
    private int _lock;

    public Cache(int count) => _count = count;

    internal void Set(Guid key, D? value)
    {
        while (Interlocked.Exchange(ref _lock, 1) == 1) ;

        if (_dict.TryGetValue(key, out Node node))
        {
            node._value = value;

            if (node == _head)
            {
                Interlocked.Exchange(ref _lock, 0);
                return;
            }

            if (node == _tail)
            {
                _tail = node._prev;

                if (node._prev is not null)
                    node._prev._next = null;

                node._next = _head;
                node._prev = null;

                _head = node;

                Interlocked.Exchange(ref _lock, 0);
                return;
            }

            if (node._prev is not null) node._prev._next = node._next;
            if (node._next is not null) node._next._prev = node._prev;

            node._next = _head;
            node._prev = null;

            if (_head is null) _head = _tail = node;
            else _head = node;

            Interlocked.Exchange(ref _lock, 0);
            return;
        }
        else
        {
            // make sure there is enough space for the new node
            while (_dict.Count > 0 && _dict.Count >= _count)
            {
                Node? tail = _tail;
                if (tail is null) return;
                _tail = tail._prev;
                _dict.Remove(tail._key);
            }

            Node newNode = new Node(key, value, _head);

            _head = newNode;
            _dict.Add(key, newNode);

            if (_head._next is not null) _head._next = _head;
            Interlocked.Exchange(ref _lock, 0);
        }
    }

    internal void Remove(Guid key)
    {
        while (Interlocked.Exchange(ref _lock, 1) == 1) ;

        if (_dict.TryGetValue(key, out Node node))
        {
            if (node._prev is not null)
                node._prev._next = node._next;
            if (node._next is not null)
                node._next._prev = node._prev;
            _dict.Remove(key);
        }

        Interlocked.Exchange(ref _lock, 0);
    }

    internal bool TryGet(Guid key, out D? value)
    {
        while (Interlocked.Exchange(ref _lock, 1) == 1) ;

        if (_dict.TryGetValue(key, out Node node))
        {
            value = node._value;

            if (node == _head)
            {
                Interlocked.Exchange(ref _lock, 0);
                return true;
            }

            if (node == _tail)
            {
                _tail = node._prev;

                if (node._prev is not null)
                    node._prev._next = null;

                node._next = _head;
                node._prev = null;

                _head = node;

                Interlocked.Exchange(ref _lock, 0);
                return true;
            }

            if (node._prev is not null) node._prev._next = node._next;
            if (node._next is not null) node._next._prev = node._prev;

            node._next = _head;
            node._prev = null;

            if (_head is null) _head = _tail = node;
            else _head = node;

            Interlocked.Exchange(ref _lock, 0);
            return true;
        }

        Interlocked.Exchange(ref _lock, 0);

        value = default;
        return false;
    }

    public class Node
    {
        public readonly Guid _key;
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
