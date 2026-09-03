using OCGForge.Ignis.Client;

internal sealed class ScriptedTransport : IByteTransport, IGameplayTransportV1
{
    private readonly Queue<byte[]> chunks;
    private byte[]? currentChunk;
    private int currentOffset;

    public ScriptedTransport(IEnumerable<byte[]> chunks)
    {
        this.chunks = new Queue<byte[]>(chunks.Select(chunk => chunk.ToArray()));
    }

    public List<byte[]> Writes { get; } = new();

    public int ReadCallCount { get; private set; }

    public int ConnectCallCount { get; private set; }

    public int CloseCallCount { get; private set; }

    public bool IsConnected { get; private set; }

    public bool IsClosed { get; private set; }

    public Exception? ConnectFailure { get; set; }

    public Exception? ReadFailure { get; set; }

    public Exception? WriteFailure { get; set; }

    public void Enqueue(params byte[][] additionalChunks)
    {
        foreach (byte[] chunk in additionalChunks)
        {
            chunks.Enqueue(chunk.ToArray());
        }
    }

    public ValueTask ConnectAsync(
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConnectCallCount++;
        if (ConnectFailure is not null)
        {
            throw ConnectFailure;
        }

        IsConnected = true;
        return ValueTask.CompletedTask;
    }

    public ValueTask<int> ReadAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadCallCount++;
        if (ReadFailure is not null)
        {
            throw ReadFailure;
        }

        while (currentChunk is null || currentOffset == currentChunk.Length)
        {
            if (chunks.Count == 0)
            {
                return ValueTask.FromResult(0);
            }

            currentChunk = chunks.Dequeue();
            currentOffset = 0;
        }

        int count = Math.Min(destination.Length, currentChunk.Length - currentOffset);
        currentChunk.AsMemory(currentOffset, count).CopyTo(destination);
        currentOffset += count;
        return ValueTask.FromResult(count);
    }

    public ValueTask WriteAsync(
        ReadOnlyMemory<byte> source,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (WriteFailure is not null)
        {
            throw WriteFailure;
        }

        Writes.Add(source.ToArray());
        return ValueTask.CompletedTask;
    }

    public ValueTask CloseAsync()
    {
        if (!IsClosed)
        {
            IsClosed = true;
            CloseCallCount++;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => CloseAsync();
}
