using System.Net.Sockets;

namespace OCGForge.Ignis.Client;

public sealed class TcpClientTransport : IByteTransport
{
    private TcpClient? client;
    private NetworkStream? stream;
    private int closed;

    public async ValueTask ConnectAsync(
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            timeout,
            TimeSpan.Zero,
            nameof(timeout));

        if (client is not null || stream is not null || Volatile.Read(ref closed) != 0)
        {
            throw new InvalidOperationException("The transport can connect only once.");
        }

        TcpClient newClient = new();
        client = newClient;
        using CancellationTokenSource timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            await newClient.ConnectAsync(host, port, timeoutSource.Token)
                .ConfigureAwait(false);
            stream = newClient.GetStream();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await CloseAsync().ConfigureAwait(false);
            throw new TimeoutException("The TCP connection timed out.");
        }
        catch
        {
            await CloseAsync().ConfigureAwait(false);
            throw;
        }
    }

    public ValueTask<int> ReadAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken)
    {
        NetworkStream activeStream = stream ??
            throw new InvalidOperationException("The transport is not connected.");
        return activeStream.ReadAsync(destination, cancellationToken);
    }

    public ValueTask WriteAsync(
        ReadOnlyMemory<byte> source,
        CancellationToken cancellationToken)
    {
        NetworkStream activeStream = stream ??
            throw new InvalidOperationException("The transport is not connected.");
        return activeStream.WriteAsync(source, cancellationToken);
    }

    public ValueTask CloseAsync()
    {
        if (Interlocked.Exchange(ref closed, 1) == 0)
        {
            stream?.Dispose();
            client?.Dispose();
            stream = null;
            client = null;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => CloseAsync();
}
