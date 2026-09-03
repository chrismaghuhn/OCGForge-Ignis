namespace OCGForge.Ignis.Client;

public interface IByteTransport : IAsyncDisposable
{
    ValueTask ConnectAsync(
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken);

    ValueTask<int> ReadAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken);

    ValueTask WriteAsync(
        ReadOnlyMemory<byte> source,
        CancellationToken cancellationToken);

    ValueTask CloseAsync();
}
