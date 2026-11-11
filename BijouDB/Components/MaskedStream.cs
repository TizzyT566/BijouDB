namespace BijouDB.Components;

/// <summary>
/// A non-owning sequential view of a stream using BijouDB's legacy XOR mask.
/// </summary>
internal sealed class MaskedStream : Stream
{
    private readonly Stream _stream;

    private int _state;
    private bool _disposed;

    public override bool CanRead => !_disposed && _stream.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => !_disposed && _stream.CanWrite;

    public override long Length
    {
        get
        {
            ThrowIfDisposed();
            return _stream.Length;
        }
    }

    public override long Position
    {
        get
        {
            ThrowIfDisposed();
            return _stream.Position;
        }

        set => throw new NotSupportedException("Masked streams support sequential access only.");
    }

    public MaskedStream(Stream stream, int seed = 0)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _state = seed;
    }

    public override void Flush()
    {
        ThrowIfDisposed();
        _stream.Flush();
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException("Masked streams support sequential access only.");

    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        _stream.SetLength(value);
    }

    private byte Next()
    {
        _state = unchecked(214013 * _state + 2531011);
        return (byte)(((_state >> 16) & 0x7FFF) % 256);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        ValidateBuffer(buffer, offset, count);
        int read = _stream.Read(buffer, offset, count);
        for (int i = 0; i < read; i++)
            buffer[offset + i] ^= Next();

        return read;
    }

    public override int ReadByte()
    {
        ThrowIfDisposed();
        int value = _stream.ReadByte();
        return value < 0 ? -1 : value ^ Next();
    }

    /// <summary>
    /// Applies a mask to array and writes it to the underlying stream.
    /// </summary>
    /// <param name="buffer">The array to write.</param>
    /// <param name="offset">Where in the array to start.</param>
    /// <param name="count">How many bytes to write.</param>
    /// <remarks>This method modifies the source array.</remarks>
    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        ValidateBuffer(buffer, offset, count);
        if (count == 0)
        {
            _stream.Write(buffer, offset, 0);
            return;
        }

        byte[] masked = new byte[Math.Min(count, 8192)];
        while (count > 0)
        {
            int chunk = Math.Min(count, masked.Length);
            for (int i = 0; i < chunk; i++)
                masked[i] = (byte)(buffer[offset + i] ^ Next());

            _stream.Write(masked, 0, chunk);
            offset += chunk;
            count -= chunk;
        }
    }

    public override void WriteByte(byte value)
    {
        ThrowIfDisposed();
        _stream.WriteByte((byte)(value ^ Next()));
    }

    protected override void Dispose(bool disposing)
    {
        _disposed = true;
        // The caller owns the underlying stream (Hash rewinds it after disposing this wrapper).
        base.Dispose(disposing);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MaskedStream));
    }

    private static void ValidateBuffer(byte[] buffer, int offset, int count)
    {
        if (buffer is null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (offset > buffer.Length || count > buffer.Length - offset) throw new ArgumentException("Offset and count exceed the buffer length.");
    }
}
