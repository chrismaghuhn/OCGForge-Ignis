namespace OCGForge.Ignis.Client;

internal sealed class GameplayTransportHandoffV1
{
    private readonly byte[] pendingBytes;
    private int claimed;

    internal GameplayTransportHandoffV1(
        IByteTransport transport,
        PreDuelSessionV1 publicSession,
        ReadOnlySpan<byte> pendingBytes)
    {
        Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        PublicSession = publicSession ??
            throw new ArgumentNullException(nameof(publicSession));
        this.pendingBytes = pendingBytes.ToArray();
    }

    internal IByteTransport Transport { get; }

    internal PreDuelSessionV1 PublicSession { get; }

    internal ReadOnlyMemory<byte> PendingBytes => pendingBytes;

    internal I2Result Claim()
    {
        if (Interlocked.Exchange(ref claimed, 1) != 0)
        {
            return I2Result.Failure(I2ErrorCode.TransportOwnershipError);
        }

        return I2Result.Success();
    }
}
