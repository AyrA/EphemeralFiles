using Microsoft.Win32.SafeHandles;
using System.Runtime.Versioning;

namespace EphemeralFiles;

/// <summary>
/// A <see cref="FileStream"/> derived class that provides ephemeral files
/// </summary>
public class EphemeralFileStream : FileStream
{
    private bool isEphemeral;
    private FileStream backend;

    /// <summary>
    /// Gets whether the file is currently ephemeral.
    /// </summary>
    /// <remarks>
    /// If the file is ephemeral, it will be erased, even if the system crashes.
    /// Use <see cref="MakeEphemeral"/> and <see cref="Restore()"/> to change the ephemeral state
    /// </remarks>
    public bool IsEphemeral => isEphemeral;

    #region Inherited Properties

    /// <inheritdoc/>
    public override SafeFileHandle SafeFileHandle => backend.SafeFileHandle;

    /// <inheritdoc/>
    [Obsolete("FileStream.Handle has been deprecated. Use FileStream's SafeFileHandle property instead.")]
    public override nint Handle => backend.Handle;

    /// <inheritdoc/>
    public override bool IsAsync => backend.IsAsync;

    /// <inheritdoc/>
    public override string Name => backend.Name;

    /// <inheritdoc/>
    public override bool CanTimeout => backend.CanTimeout;

    /// <inheritdoc/>
    public override int ReadTimeout { get => backend.ReadTimeout; set => backend.ReadTimeout = value; }

    /// <inheritdoc/>
    public override int WriteTimeout { get => backend.WriteTimeout; set => backend.WriteTimeout = value; }

    /// <inheritdoc/>
    public override bool CanRead => backend.CanRead;

    /// <inheritdoc/>
    public override bool CanSeek => backend.CanSeek;

    /// <inheritdoc/>
    public override bool CanWrite => backend.CanWrite;

    /// <inheritdoc/>
    public override long Length => backend.Length;

    /// <inheritdoc/>
    public override long Position { get => backend.Position; set => backend.Position = value; }

    #endregion

    /// <summary>
    /// Create a new instance and define whether the file is initially ephemeral or not.
    /// The file name of the underlying file will be random. Use <see cref="Name"/> to get the file path and name
    /// </summary>
    /// <param name="initialEphemeral">true if initially ephemeral, false otherwise</param>
    public EphemeralFileStream(bool initialEphemeral) : this(Path.GetTempFileName(), initialEphemeral)
    {
        //NOOP
    }

    /// <summary>
    /// Creates a new instance with the defined file path and initial ephemeral state
    /// </summary>
    /// <param name="path">File path. If the file exists, it is opened, otherwise a new file will be created</param>
    /// <param name="initialEphemeral">true if initially ephemeral, false otherwise</param>
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

    /// <summary>
    /// Makes the file ephemeral
    /// </summary>
    public void MakeEphemeral()
    {
        if (isEphemeral)
        {
            throw new InvalidOperationException("File is already ephemeral");
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

    /// <summary>
    /// Restores the file to the location specified in <see cref="Name"/>
    /// </summary>
    public void Restore()
    {
        if (!isEphemeral)
        {
            throw new InvalidOperationException("File is already not ephemeral");
        }

        Restore(backend.Name);
    }

    /// <summary>
    /// Restores the file to the given location,
    /// and updates <see cref="Name"/> to point to the new file
    /// </summary>
    /// <param name="target">Target location to restore the file to</param>
    public void Restore(string target)
    {
        if (!isEphemeral)
        {
            throw new InvalidOperationException("File is already not ephemeral");
        }

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

    /// <summary>
    /// Restores the file to the location specified in <see cref="Name"/>
    /// </summary>
    /// <param name="cancellationToken">Token to abort the restore operation</param>
    public Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        if (!isEphemeral)
        {
            throw new InvalidOperationException("File is already not ephemeral");
        }

        return RestoreAsync(backend.Name, cancellationToken);
    }

    /// <summary>
    /// Restores the file to the given location,
    /// and updates <see cref="Name"/> to point to the new file
    /// </summary>
    /// <param name="target">Target location to restore the file to</param>
    /// <param name="cancellationToken">Token to abort the restore operation</param>
    public async Task RestoreAsync(string target, CancellationToken cancellationToken = default)
    {
        if (!isEphemeral)
        {
            throw new InvalidOperationException("File is already not ephemeral");
        }

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

    /// <inheritdoc/>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("freebsd")]
    public override void Lock(long position, long length)
    {
        backend.Lock(position, length);
    }

    /// <inheritdoc/>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("macos")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("freebsd")]
    public override void Unlock(long position, long length)
    {
        backend.Unlock(position, length);
    }

    /// <inheritdoc/>
    public override void Flush(bool flushToDisk)
    {
        backend.Flush(flushToDisk);
    }

    /// <inheritdoc/>
    public override void Close()
    {
        backend?.Close();
        base.Close();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            backend?.Dispose();
        }
    }

    /// <inheritdoc/>
    public override void Flush()
    {
        backend.Flush();
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        return backend.Read(buffer, offset, count);
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        return backend.Seek(offset, origin);
    }

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        backend.SetLength(value);
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        backend.Write(buffer, offset, count);
    }

    /// <inheritdoc/>
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return backend.BeginRead(buffer, offset, count, callback, state);
    }

    /// <inheritdoc/>
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        return backend.BeginWrite(buffer, offset, count, callback, state);
    }

    /// <inheritdoc/>
    public override void CopyTo(Stream destination, int bufferSize)
    {
        backend.CopyTo(destination, bufferSize);
    }

    /// <inheritdoc/>
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        return backend.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    /// <inheritdoc/>
    public override ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        Dispose();
        return backend.DisposeAsync();
    }

    /// <inheritdoc/>
    public override int EndRead(IAsyncResult asyncResult)
    {
        return backend.EndRead(asyncResult);
    }

    /// <inheritdoc/>
    public override void EndWrite(IAsyncResult asyncResult)
    {
        backend.EndWrite(asyncResult);
    }

    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return backend.FlushAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer)
    {
        return backend.Read(buffer);
    }

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return backend.ReadAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return backend.ReadAsync(buffer, cancellationToken);
    }

    /// <inheritdoc/>
    public override int ReadByte()
    {
        return backend.ReadByte();
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        backend.Write(buffer);
    }

    /// <inheritdoc/>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return backend.WriteAsync(buffer, offset, count, cancellationToken);
    }

    /// <inheritdoc/>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return backend.WriteAsync(buffer, cancellationToken);
    }

    /// <inheritdoc/>
    public override void WriteByte(byte value)
    {
        backend.WriteByte(value);
    }

    /// <inheritdoc/>
    [Obsolete("This Remoting API is not supported and throws PlatformNotSupportedException.", DiagnosticId = "SYSLIB0010", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    public override object InitializeLifetimeService()
    {
        return backend.InitializeLifetimeService();
    }

    #endregion
}