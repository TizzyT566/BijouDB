namespace BijouDB.Components;

internal class Cache<D> where D : IDataType
{
    private readonly Dictionary<Guid, Node> _dict = new();
    private readonly int _count;
    private Node? _head;
    private int _lock;

    public Cache(int count) => _count = count > 0 ? count : throw new InvalidOperationException("Unexpected Error, Cache must be greater than 0.");

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

    public void Set(Guid key, D value)
    {
        while (Interlocked.Exchange(ref _lock, 1) == 1) ;

        if (_dict.TryGetValue(key, out Node node))
        {
            node._value = value;
            MoveToHead(node);
        }
        else
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

            Node newNode = new(key, value);
            if (_head is null)
            {
                newNode._prev = newNode;
            }
            else
            {
                newNode._prev = _head._prev;
                newNode._next = _head;
                _head._prev = newNode;
            }
            _dict[key] = _head = newNode;
        }

        Interlocked.Exchange(ref _lock, 0);
    }

    public void Remove(Guid key)
    {
        while (Interlocked.Exchange(ref _lock, 1) == 1) ;

        if (_dict.TryGetValue(key, out Node node))
        {
            if (node == _head)
            {
                if (_head._next is null)
                {
                    _head = null;
                    _dict.Clear();
                }
                else
                {
                    _head._next._prev = _head._prev;
                    _head = _head._next;
                }
            }
            else if (node == _head!._prev)
            {
                if (node == _head._next)
                {
                    _head._next = null;
                    _head._prev = _head;
                }
                else
                {
                    _head._prev = _head._prev._prev;
                    _head._prev!._next = null;
                }
            }
            else
            {
                node._prev!._next = node._next;
                node._next!._prev = node._prev;
            }

            _dict.Remove(key);
        }

        Interlocked.Exchange(ref _lock, 0);
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

    public class Node
    {
        public Guid _key;
        public D? _value;
        public Node _prev;
        public Node? _next;

        public Node(Guid key, D? value, Node Prev = null!)
        {
            _key = key;
            _value = value;
            _prev = Prev;
        }
    }
}
