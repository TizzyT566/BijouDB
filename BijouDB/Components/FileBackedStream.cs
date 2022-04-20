namespace BijouDB.Components;

public class FileBackedStream : Stream, IDisposable
{
    private readonly int _thresholdSize;
    private bool disposedValue, _check = true;
    private Stream _stream;

    public override bool CanRead => _stream.CanRead;

    public override bool CanSeek => _stream.CanSeek;

    public override bool CanWrite => _stream.CanWrite;

    public override long Length => _stream.Length;

    public override long Position { get => _stream.Position; set => _stream.Position = value; }

    public override void Flush() => _stream.Flush();

    public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

    public override void SetLength(long value) => _stream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (count <= 0) return;

        if (_check && ((count + _stream.Position) > _thresholdSize))
        {
            // Fail over to a file
            _stream.Flush();
            Stream originalStream = _stream;

            _stream = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, _thresholdSize, FileOptions.DeleteOnClose);

            originalStream.Position = 0;
            originalStream.CopyTo(_stream);
            originalStream.Flush();

            _stream.Position = originalStream.Position;

            originalStream?.Close();
            originalStream?.Dispose();

            _check = false;
        }

        _stream.Write(buffer, offset, count);
    }

    public FileBackedStream(int thresholdSize = 1048576)
    {
        _thresholdSize = thresholdSize;
        _stream = new MemoryStream();
    }

    protected new virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing) _stream?.Dispose();
            disposedValue = true;
        }
    }

    public new void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
