using Microsoft.Win32.SafeHandles;
using System.Runtime.Versioning;

namespace EphemeralFiles;

public class EphemeralFileStream : FileStream
{
    private bool isEphemeral;
    private FileStream backend;

    public bool IsEphemeral => isEphemeral;

    #region Inherited Properties

    public override SafeFileHandle SafeFileHandle => backend.SafeFileHandle;

    [Obsolete("FileStream.Handle has been deprecated. Use FileStream's SafeFileHandle property instead.")]
    public override nint Handle => backend.Handle;

    public override bool IsAsync => backend.IsAsync;

    public override string Name => backend.Name;

    public override bool CanTimeout => backend.CanTimeout;

    public override int ReadTimeout { get => backend.ReadTimeout; set => backend.ReadTimeout = value; }

    public override int WriteTimeout { get => backend.WriteTimeout; set => backend.WriteTimeout = value; }

    public override bool CanRead => backend.CanRead;

    public override bool CanSeek => backend.CanSeek;

    public override bool CanWrite => backend.CanWrite;

    public override long Length => backend.Length;

    public override long Position { get => backend.Position; set => backend.Position = value; }

    #endregion

    public EphemeralFileStream(bool initialEphemeral) : this(Path.GetTempFileName(), initialEphemeral)
    {
        //NOOP
    }

    public EphemeralFileStream(string path, bool initialEphemeral) : base(
        Environment.ProcessPath ?? Path.GetTempFileName(),
        FileMode.Open, FileAccess.Read)
    {
        backend = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Delete);
        if (initialEphemeral)
        {
            MakeEphemeral();
        }
    }

    public void MakeEphemeral()
    {
        if (isEphemeral)
        {
            return;
        }
        try
        {
            File.Delete(Name);
        }
        catch (Exception ex)
        {
            throw new IOException("Failed to make file ephemeral. See inner exception for details", ex);
        }
        isEphemeral = true;
    }

    public void Restore() => Restore(backend.Name);

    public void Restore(string target)
    {
        var copy = File.Create(target);
        try
        {
            var pos = backend.Position;
            backend.Seek(0, SeekOrigin.Begin);
            copy.SetLength(Length);
            backend.CopyTo(copy);
            backend = copy;
            backend.Position = pos;
            isEphemeral = false;
        }
        catch
        {
            try
            {
                File.Delete(target);
            }
            catch
            {
                //NOOP
            }
            copy.Dispose();
            throw;
        }
    }

    public Task RestoreAsync(CancellationToken cancellationToken = default) => RestoreAsync(backend.Name, cancellationToken);

    public async Task RestoreAsync(string target, CancellationToken cancellationToken = default)
    {
        var copy = File.Create(target);
        try
        {
            var pos = backend.Position;
            backend.Seek(0, SeekOrigin.Begin);
            copy.SetLength(Length);
            await backend.CopyToAsync(copy, cancellationToken);
            backend = copy;
            backend.Position = pos;
            isEphemeral = false;
        }
        catch
        {
            copy.Dispose();
            try
            {
                File.Delete(target);
            }
            catch
            {
                //NOOP
            }
            throw;
        }
    }

    #region Inherited Methods

    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("freebsd")]
    public override void Lock(long position, long length)
    {
        backend.Lock(position, length);
    }

    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("freebsd")]
    public override void Unlock(long position, long length)
    {
        backend.Unlock(position, length);
    }

    public override void Flush(bool flushToDisk)
    {
        backend.Flush(flushToDisk);
    }

    public override void Close()
    {
        backend?.Close();
        base.Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            backend?.Dispose();
        }
    }

    public override void Flush()
    {
        backend.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return backend.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return backend.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        backend.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        backend.Write(buffer, offset, count);
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return backend.BeginRead(buffer, offset, count, callback, state);
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return backend.BeginWrite(buffer, offset, count, callback, state);
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        backend.CopyTo(destination, bufferSize);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        return backend.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    public override ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        Dispose();
        return backend.DisposeAsync();
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return backend.EndRead(asyncResult);
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        backend.EndWrite(asyncResult);
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return backend.FlushAsync(cancellationToken);
    }

    public override int Read(Span<byte> buffer)
    {
        return backend.Read(buffer);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return backend.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return backend.ReadAsync(buffer, cancellationToken);
    }

    public override int ReadByte()
    {
        return backend.ReadByte();
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        backend.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return backend.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return backend.WriteAsync(buffer, cancellationToken);
    }

    public override void WriteByte(byte value)
    {
        backend.WriteByte(value);
    }

    [Obsolete("This Remoting API is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0010", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    public override object InitializeLifetimeService()
    {
        return backend.InitializeLifetimeService();
    }

    #endregion
}