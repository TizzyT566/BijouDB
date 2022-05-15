namespace BijouDB.Components;

internal class MaskedStream : Stream
{
    private readonly Stream _stream;
    private readonly int _seed;
    private Random _random;

    public override bool CanRead => _stream.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => _stream.CanWrite;
    public override long Length => _stream.Length;
    public override long Position
    {
        get => _stream.Position;
        set
        {
            if (value == _stream.Position) return;
            _stream.Position = value;
            _random = new(_seed);
        }
    }

    public MaskedStream(Stream stream, int seed = 0)
    {
        _seed = seed;
        _stream = stream;
        _random = new Random(seed);
    }

    public override void Flush() => _stream.Flush();

    public override long Seek(long offset, SeekOrigin origin)
    {
        long ret = _stream.Seek(offset, origin);
        _random = new(_seed);
        return ret;
    }

    public override void SetLength(long value)
    {
        if (value < _stream.Length) return;
        _stream.SetLength(value);
        _random = new(_seed);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int ret = _stream.Read(buffer, offset, count);
        for (int i = 0; i < count; i++)
            buffer[offset + i] ^= (byte)(_random.Next() % (byte.MaxValue + 1));
        return ret;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        byte[] bytes = new byte[count];
        Array.Copy(buffer, offset, bytes, 0, count);
        for (int i = 0; i < count; i++)
            bytes[i] ^= (byte)(_random.Next() % (byte.MaxValue + 1));
        _stream.Write(bytes, 0, bytes.Length);
    }
}
