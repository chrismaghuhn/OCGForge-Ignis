namespace OCGForge.Ignis.Client;

public interface IGameplayTransportV1 : IAsyncDisposable
{
    ValueTask<int> ReadAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken);

    ValueTask CloseAsync();
}

public sealed class GameplayHandoffOfferV1
{
    private IGameplayTransportV1? transport;
    private readonly byte[] pendingBytes;
    private readonly PreDuelSessionV1 publicSession;
    private int claimed;

    public GameplayHandoffOfferV1(
        IGameplayTransportV1 transport,
        PreDuelSessionV1 publicSession,
        ReadOnlySpan<byte> pendingBytes)
    {
        this.transport = transport ??
            throw new ArgumentNullException(nameof(transport));
        this.publicSession = publicSession ??
            throw new ArgumentNullException(nameof(publicSession));
        this.pendingBytes = pendingBytes.ToArray();
    }

    internal GameplayHandoffOfferV1(
        IByteTransport transport,
        PreDuelSessionV1 publicSession,
        ReadOnlySpan<byte> pendingBytes)
        : this(
            new I2GameplayTransportAdapter(transport),
            publicSession,
            pendingBytes)
    {
    }

    public GameplayHandoffClaimResult TryClaim()
    {
        if (Interlocked.Exchange(ref claimed, 1) != 0)
        {
            return GameplayHandoffClaimResult.Failure(
                I2ErrorCode.TransportOwnershipError);
        }

        IGameplayTransportV1? claimedTransport =
            Interlocked.Exchange(ref transport, null);
        if (claimedTransport is null)
        {
            return GameplayHandoffClaimResult.Failure(
                I2ErrorCode.TransportOwnershipError);
        }

        return GameplayHandoffClaimResult.Success(
            new GameplayHandoffLeaseV1(
                claimedTransport,
                publicSession,
                pendingBytes));
    }
}

public readonly record struct GameplayHandoffClaimResult(
    bool IsSuccess,
    I2ErrorCode Error,
    GameplayHandoffLeaseV1? Lease)
{
    internal static GameplayHandoffClaimResult Success(
        GameplayHandoffLeaseV1 lease) =>
        new(true, I2ErrorCode.None, lease);

    internal static GameplayHandoffClaimResult Failure(
        I2ErrorCode error) =>
        new(false, error, null);
}

public sealed class GameplayHandoffLeaseV1
{
    private readonly IGameplayTransportV1 transport;
    private readonly byte[] pendingBytes;
    private readonly PreDuelSessionV1 publicSession;
    private int pendingOffset;
    private int transportClosed;

    internal GameplayHandoffLeaseV1(
        IGameplayTransportV1 transport,
        PreDuelSessionV1 publicSession,
        ReadOnlySpan<byte> pendingBytes)
    {
        this.transport = transport ??
            throw new ArgumentNullException(nameof(transport));
        this.publicSession = publicSession ??
            throw new ArgumentNullException(nameof(publicSession));
        this.pendingBytes = pendingBytes.ToArray();
    }

    public PreDuelSessionV1 PublicSession => publicSession;

    public ReadOnlyMemory<byte> PendingBytes =>
        pendingBytes.AsMemory(pendingOffset);

    public ValueTask<int> ReadAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken)
    {
        if (destination.IsEmpty)
        {
            return ValueTask.FromResult(0);
        }

        int pendingCount = pendingBytes.Length - pendingOffset;
        if (pendingCount > 0)
        {
            int count = Math.Min(destination.Length, pendingCount);
            pendingBytes.AsMemory(pendingOffset, count).CopyTo(destination);
            pendingOffset += count;
            return ValueTask.FromResult(count);
        }

        return transport.ReadAsync(destination, cancellationToken);
    }

    public async ValueTask<I2Result> CloseOwnedTransportAsync()
    {
        if (Interlocked.Exchange(ref transportClosed, 1) != 0)
        {
            return I2Result.Success();
        }

        try
        {
            await transport.CloseAsync().ConfigureAwait(false);
        }
        catch
        {
        }

        try
        {
            await transport.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
        }

        return I2Result.Success();
    }
}

internal sealed class I2GameplayTransportAdapter : IGameplayTransportV1
{
    private readonly IByteTransport transport;

    internal I2GameplayTransportAdapter(IByteTransport transport)
    {
        this.transport = transport ??
            throw new ArgumentNullException(nameof(transport));
    }

    public ValueTask<int> ReadAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken) =>
        transport.ReadAsync(destination, cancellationToken);

    public ValueTask CloseAsync() => transport.CloseAsync();

    public ValueTask DisposeAsync() => transport.DisposeAsync();
}
