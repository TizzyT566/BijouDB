namespace BijouDB.Components;

internal sealed class FileBackedStream : Stream
{
    private readonly int _thresholdSize;

    private bool _disposed, _inMemory = true;
    private Stream _stream = new MemoryStream();

    public FileBackedStream(int thresholdSize = 1048576)
    {
        if (thresholdSize < 0) throw new ArgumentOutOfRangeException(nameof(thresholdSize));
        _thresholdSize = thresholdSize;
    }

    public override bool CanRead => !_disposed && _stream.CanRead;

    public override bool CanSeek => !_disposed && _stream.CanSeek;

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

        set
        {
            ThrowIfDisposed();
            _stream.Position = value;
        }
    }

    public override void Flush()
    {
        ThrowIfDisposed();
        _stream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        return _stream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        return _stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        EnsureCapacity(value);
        _stream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        if (buffer is null) throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (offset > buffer.Length || count > buffer.Length - offset) throw new ArgumentException("Offset and count exceed the buffer length.");
        if (count > 0)
            EnsureCapacity(checked(_stream.Position + count));
        _stream.Write(buffer, offset, count);
    }

    private void EnsureCapacity(long desiredLength)
    {
        if (!_inMemory || desiredLength <= _thresholdSize) return;

        long position = _stream.Position;
        FileStream replacement = Misc.GetTemporaryFileStream();

        try
        {
            _stream.Position = 0;
            _stream.CopyTo(replacement);
            replacement.Position = position;
        }
        catch
        {
            replacement.Dispose();
            throw;
        }
        finally
        {
            _stream.Position = position;
        }

        Stream previous = _stream;
        _stream = replacement;
        _inMemory = false;
        previous.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
                _stream.Dispose();
            _disposed = true;
        }

        base.Dispose(disposing);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(FileBackedStream));
    }
}
