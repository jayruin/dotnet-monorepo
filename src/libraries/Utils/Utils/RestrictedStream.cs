using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Utils;

internal sealed class RestrictedStream : Stream
{
    private readonly Stream _stream;
    private readonly bool _restrictSeek;
    private readonly bool _restrictRead;
    private readonly bool _restrictWrite;

    public RestrictedStream(Stream stream, bool restrictSeek, bool restrictRead, bool restrictWrite)
    {
        _stream = stream;
        _restrictSeek = restrictSeek;
        _restrictRead = restrictRead;
        _restrictWrite = restrictWrite;
    }

    public override bool CanRead => !_restrictRead && _stream.CanRead;
    public override bool CanSeek => !_restrictSeek && _stream.CanSeek;
    public override bool CanTimeout => _stream.CanTimeout;
    public override bool CanWrite => !_restrictWrite && _stream.CanWrite;
    public override long Length => _stream.Length;
    public override long Position
    {
        get
        {
            ThrowIfNotSeekable();
            return _stream.Position;
        }

        set
        {
            ThrowIfNotSeekable();
            _stream.Position = value;
        }
    }
    public override int ReadTimeout
    {
        get
        {
            ThrowIfNotReadable();
            return _stream.ReadTimeout;
        }

        set
        {
            ThrowIfNotReadable();
            _stream.ReadTimeout = value;
        }
    }
    public override int WriteTimeout
    {
        get
        {
            ThrowIfNotWritable();
            return _stream.WriteTimeout;
        }

        set
        {
            ThrowIfNotWritable();
            _stream.WriteTimeout = value;
        }
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        ThrowIfNotReadable();
        return _stream.BeginRead(buffer, offset, count, callback, state);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        ThrowIfNotWritable();
        return _stream.BeginWrite(buffer, offset, count, callback, state);
    }

    public override void Close() => _stream.Close();

    public override void CopyTo(Stream destination, int bufferSize)
    {
        ThrowIfNotReadable();
        _stream.CopyTo(destination, bufferSize);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        ThrowIfNotReadable();
        return _stream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    public override ValueTask DisposeAsync() => _stream.DisposeAsync();

    public override int EndRead(IAsyncResult asyncResult)
    {
        ThrowIfNotReadable();
        return _stream.EndRead(asyncResult);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        ThrowIfNotWritable();
        _stream.EndWrite(asyncResult);
    }

    public override void Flush() => _stream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => _stream.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfNotReadable();
        return _stream.Read(buffer, offset, count);
    }

    public override int Read(Span<byte> buffer)
    {
        ThrowIfNotReadable();
        return _stream.Read(buffer);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowIfNotReadable();
        return _stream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfNotReadable();
        return _stream.ReadAsync(buffer, cancellationToken);
    }

    public override int ReadByte()
    {
        ThrowIfNotReadable();
        return _stream.ReadByte();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfNotSeekable();
        return _stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        ThrowIfNotSeekable();
        ThrowIfNotWritable();
        _stream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfNotWritable();
        _stream.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfNotWritable();
        _stream.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowIfNotWritable();
        return _stream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfNotWritable();
        return _stream.WriteAsync(buffer, cancellationToken);
    }

    public override void WriteByte(byte value)
    {
        ThrowIfNotWritable();
        _stream.WriteByte(value);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ThrowIfNotReadable()
    {
        if (!CanRead)
        {
            throw new NotSupportedException("Stream does not support reading.");
        }
    }

    private void ThrowIfNotSeekable()
    {
        if (!CanSeek)
        {
            throw new NotSupportedException("Stream does not support seeking.");
        }
    }

    private void ThrowIfNotWritable()
    {
        if (!CanWrite)
        {
            throw new NotSupportedException("Stream does not support writing.");
        }
    }
}
