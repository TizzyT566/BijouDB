namespace BijouDB.Components;

internal class MaskedStream : Stream
{
    private readonly Stream _stream;
    private readonly int _seed;
    private int _state;

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
            _state = _seed;
        }
    }

    public MaskedStream(Stream stream, int seed = 0)
    {
        _stream = stream;
        _state = _seed = seed;
    }

    public override void Flush() => _stream.Flush();

    public override long Seek(long offset, SeekOrigin origin)
    {
        long ret = _stream.Seek(offset, origin);
        _state = _seed;
        return ret;
    }

    public override void SetLength(long value)
    {
        if (value < _stream.Length) return;
        _stream.SetLength(value);
        _state = _seed;
    }

    // FastRand prng
    private byte Next()
    {
        _state = 214013 * _state + 2531011;
        return (byte)(((_state >> 16) & 0x7FFF) % 256);
    }

    public unsafe override int Read(byte[] buffer, int offset, int count)
    {
        int ret = _stream.Read(buffer, offset, count);
        fixed (byte* ptr = buffer)
            for (int i = 0; i < count; i++)
                ptr[offset + i] ^= Next();
        return ret;
    }

    /// <summary>
    /// Applies a mask to array and writes it to the underlying stream.
    /// </summary>
    /// <param name="buffer">The array to write.</param>
    /// <param name="offset">Where in the array to start.</param>
    /// <param name="count">How many bytes to write.</param>
    /// <remarks>This method modifies the source array.</remarks>
    public unsafe override void Write(byte[] buffer, int offset, int count)
    {
        fixed (byte* ptr = buffer)
            for (int i = 0; i < count; i++)
                ptr[i] ^= Next();
        _stream.Write(buffer, offset, count);
    }
}
