using System.IO;

namespace ScreenToGif.Util;

public class SubStream : Stream
{
    private readonly Stream _baseStream;
    private readonly long _offset;
    private readonly long _length;
    private long _position;

    public SubStream(Stream baseStream, long offset, long length)
    {
        if (baseStream == null)
            throw new ArgumentNullException(nameof(baseStream));

        if (!baseStream.CanRead)
            throw new ArgumentException("Impossible to read base stream.");

        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (length < 1)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");

        _baseStream = baseStream;
        _offset = offset;
        _length = length;

        baseStream.Seek(offset, SeekOrigin.Begin);
    }

    private void CheckDisposed()
    {
        if (_baseStream == null)
            throw new ObjectDisposedException(GetType().Name);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        CheckDisposed();

        var remaining = _length - _position;

        if (remaining <= 0)
            return 0;

        if (remaining < count)
            count = (int)remaining;

        var read = _baseStream.Read(buffer, offset, count);
        _position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var pos = _position;

        if (origin == SeekOrigin.Begin)
            pos = offset;
        else if (origin == SeekOrigin.End)
            pos = _length + offset;
        else if (origin == SeekOrigin.Current)
            pos += offset;

        if (pos < 0)
            pos = 0;

        else if (pos >= _length)
            pos = _length - 1;

        _position = _baseStream.Seek(_offset + pos, SeekOrigin.Begin) - _offset;
        return pos;
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => _position = Seek(value, SeekOrigin.Begin);
    }

    public override void Flush()
    {
        CheckDisposed();

        _baseStream.Flush();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}