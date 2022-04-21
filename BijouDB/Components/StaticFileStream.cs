namespace BijouDB.Components
{
    public class StaticFileStream : Stream, IDisposable
    {
        private struct StaticFileStreamNode
        {
            public readonly FileStream _stream;
            public Stack<long> Positions { get; }
            public StaticFileStreamNode(FileStream stream)
            {
                _stream = stream;
                Positions = new Stack<long>();
            }
        }

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;
        public override long Position { get => _stream.Position; set => _stream.Position = value; }

        public override void Flush() => _stream.Flush();
        public override void SetLength(long value) => _stream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        private static readonly Dictionary<string, StaticFileStreamNode> _pool = new();

        public string Name { get; }
        private readonly FileStream _stream;

        private StaticFileStream(string name, FileStream stream)
        {
            Name = name;
            _stream = stream;
        }

        public StaticFileStream this[string path]
        {
            get
            {
                string name = Path.GetFullPath(path);
                if (_pool.TryGetValue(name, out StaticFileStreamNode node))
                {
                    StaticFileStream sfs = new(name, node._stream);
                    node.Positions.Push(sfs._stream.Position);
                    return sfs;
                }
                else
                {
                    FileStream fs = new(name, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    _pool.Add(name, new(fs));
                    return new(name, fs);
                }
            }
        }

        private bool disposedValue;
        protected new virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (_pool.TryGetValue(Name, out StaticFileStreamNode node))
                    {
                        if (node.Positions.TryPop(out long pos)) _stream.Position = pos;
                        else
                        {
                            _pool.Remove(Name);
                            _stream?.Dispose();
                        }
                    }
                }
                disposedValue = true;
            }
        }

        public new void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
