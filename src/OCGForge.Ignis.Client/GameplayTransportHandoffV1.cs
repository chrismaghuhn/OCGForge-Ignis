namespace OCGForge.Ignis.Client;

public sealed class GameplayTransportHandoffV1
{
    private readonly byte[] pendingBytes;
    private int claimed;
    private int transportClosed;

    public GameplayTransportHandoffV1(
        IByteTransport transport,
        PreDuelSessionV1 publicSession,
        ReadOnlySpan<byte> pendingBytes)
    {
        Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        PublicSession = publicSession ??
            throw new ArgumentNullException(nameof(publicSession));
        this.pendingBytes = pendingBytes.ToArray();
    }

    public IByteTransport Transport { get; }

    public PreDuelSessionV1 PublicSession { get; }

    public ReadOnlyMemory<byte> PendingBytes => pendingBytes;

    public I2Result Claim()
    {
        if (Interlocked.Exchange(ref claimed, 1) != 0)
        {
            return I2Result.Failure(I2ErrorCode.TransportOwnershipError);
        }

        return I2Result.Success();
    }

    public async ValueTask<I2Result> CloseOwnedTransportAsync()
    {
        if (Volatile.Read(ref claimed) == 0)
        {
            return I2Result.Failure(I2ErrorCode.TransportOwnershipError);
        }

        if (Interlocked.Exchange(ref transportClosed, 1) != 0)
        {
            return I2Result.Success();
        }

        try
        {
            await Transport.CloseAsync().ConfigureAwait(false);
        }
        catch
        {
        }

        try
        {
            await Transport.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
        }

        return I2Result.Success();
    }
}
