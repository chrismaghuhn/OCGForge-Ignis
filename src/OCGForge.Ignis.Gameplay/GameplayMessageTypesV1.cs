using OCGForge.Ignis.Client;

namespace OCGForge.Ignis.Gameplay;

public enum GameplayErrorCode : byte
{
    None = 0,
    MalformedGameMessage = 1,
    UnsupportedMessage = 2,
    UnknownMessageId = 3,
    UnsupportedPerspective = 4,
    InvalidPerspectiveRole = 5,
    DuplicatePerspective = 6,
    ConflictingPerspective = 7,
    PerspectiveEstablishmentTooLate = 8,
    PerspectiveNotEstablished = 9,
    HandoffAlreadyClaimed = 10,
    InvalidHandoff = 11,
    MalformedOuterFrame = 12,
    UnsupportedOuterPacket = 13,
    TruncatedStream = 14,
    RemoteClosed = 15,
    TransportReadFailed = 16,
    Cancelled = 17,
    InvalidState = 18
}

public enum GameplayPerspectiveKind : byte
{
    SelfIsPlayer0 = 0,
    SelfIsPlayer1 = 1
}

public sealed record GameplayPerspectiveV1
{
    private GameplayPerspectiveV1(
        GameplayPerspectiveKind kind,
        byte playerType)
    {
        Kind = kind;
        PlayerType = playerType;
    }

    public static GameplayPerspectiveV1 SelfIsPlayer0 { get; } =
        new(GameplayPerspectiveKind.SelfIsPlayer0, 0x00);

    public static GameplayPerspectiveV1 SelfIsPlayer1 { get; } =
        new(GameplayPerspectiveKind.SelfIsPlayer1, 0x01);

    public GameplayPerspectiveKind Kind { get; }

    public byte PlayerType { get; }

    internal static bool TryCreate(
        byte playerType,
        out GameplayPerspectiveV1 perspective)
    {
        switch (playerType)
        {
            case 0x00:
                perspective = SelfIsPlayer0;
                return true;
            case 0x01:
                perspective = SelfIsPlayer1;
                return true;
            default:
                perspective = null!;
                return false;
        }
    }
}

public readonly record struct GameplayStartPayloadV1(
    byte PlayerType,
    uint LifePoints0,
    uint LifePoints1,
    ushort DeckCount0,
    ushort ExtraCount0,
    ushort DeckCount1,
    ushort ExtraCount1);

public sealed record GameplayMessageV1
{
    private GameplayMessageV1(GameplayStartPayloadV1 start)
    {
        Start = start;
    }

    public const byte MessageId = 4;

    public GameplayStartPayloadV1 Start { get; }

    internal static GameplayMessageV1 FromStart(
        GameplayStartPayloadV1 start) =>
        new(start);
}

public readonly record struct GameplayMessageDecodeResult(
    bool IsSuccess,
    GameplayErrorCode Error,
    GameplayMessageV1? Message,
    GameplayPerspectiveV1? Perspective)
{
    internal static GameplayMessageDecodeResult Success(
        GameplayMessageV1 message,
        GameplayPerspectiveV1 perspective) =>
        new(true, GameplayErrorCode.None, message, perspective);

    internal static GameplayMessageDecodeResult Failure(
        GameplayErrorCode error,
        GameplayPerspectiveV1? perspective) =>
        new(false, error, null, perspective);
}

public readonly record struct ModernLocInfoV1(
    byte Controller,
    byte Location,
    uint Sequence,
    uint Position);

public readonly record struct GameplayHandoffAcquireResult(
    bool IsSuccess,
    GameplayErrorCode Error,
    GameplayHandoffConsumerV1? Consumer)
{
    internal static GameplayHandoffAcquireResult Success(
        GameplayHandoffConsumerV1 consumer) =>
        new(true, GameplayErrorCode.None, consumer);

    internal static GameplayHandoffAcquireResult Failure(
        GameplayErrorCode error) =>
        new(false, error, null);
}

public sealed class GameplayPumpResult
{
    private GameplayPumpResult(
        bool isSuccess,
        GameplayErrorCode error,
        GameplayMessageV1? message,
        GameplayPerspectiveV1? perspective,
        GameplaySessionV1? session)
    {
        IsSuccess = isSuccess;
        Error = error;
        Message = message;
        Perspective = perspective;
        Session = session;
    }

    public bool IsSuccess { get; }

    public GameplayErrorCode Error { get; }

    public GameplayMessageV1? Message { get; }

    public GameplayPerspectiveV1? Perspective { get; }

    public GameplaySessionV1? Session { get; }

    internal static GameplayPumpResult Success(
        GameplayMessageV1 message,
        GameplayPerspectiveV1 perspective,
        GameplaySessionV1 session) =>
        new(
            true,
            GameplayErrorCode.None,
            message,
            perspective,
            session);

    internal static GameplayPumpResult Failure(GameplayErrorCode error) =>
        new(false, error, null, null, null);
}

public sealed class GameplaySessionV1 : IAsyncDisposable
{
    private readonly GameplayHandoffLeaseV1 ownership;
    private readonly byte[] pendingBytes;

    internal GameplaySessionV1(
        GameplayHandoffLeaseV1 ownership,
        GameplayPerspectiveV1 perspective,
        PreDuelSessionV1 publicSession,
        ReadOnlySpan<byte> pendingBytes)
    {
        this.ownership = ownership ??
            throw new ArgumentNullException(nameof(ownership));
        Perspective = perspective ??
            throw new ArgumentNullException(nameof(perspective));
        PublicSession = publicSession ??
            throw new ArgumentNullException(nameof(publicSession));
        this.pendingBytes = pendingBytes.ToArray();
    }

    public GameplayPerspectiveV1 Perspective { get; }

    public PreDuelSessionV1 PublicSession { get; }

    public ValueTask<int> ReadAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken) =>
        ReadPendingOrTransportAsync(destination, cancellationToken);

    public ReadOnlyMemory<byte> PendingBytes => pendingBytes;

    public ValueTask<I2Result> CloseOwnedTransportAsync() =>
        ownership.CloseOwnedTransportAsync();

    public async ValueTask DisposeAsync()
    {
        await CloseOwnedTransportAsync().ConfigureAwait(false);
    }

    private ValueTask<int> ReadPendingOrTransportAsync(
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

        return ownership.ReadAsync(destination, cancellationToken);
    }

    private int pendingOffset;
}
