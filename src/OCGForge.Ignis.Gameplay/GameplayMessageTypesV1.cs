using System.Collections.ObjectModel;
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
    InvalidState = 18,
    MalformedQuery = 19,
    UnsupportedQueryFlag = 20,
    DuplicateQueryFlag = 21,
    QueryLengthMismatch = 22,
    QueryCountOverflow = 23,
    InvalidParticipant = 24,
    InvalidLocation = 25,
    UnknownMirrorReference = 26,
    ConflictingSlotOccupancy = 27,
    StateCapacityExceeded = 28,
    ArithmeticFailure = 29,
    TerminalStateMutation = 30,
    InvalidDrawCount = 31,
    InvalidChainState = 32,
    InvalidRelation = 33,
    InvalidStateTransition = 34
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

public enum GameplayMessageKindV1 : byte
{
    Start = 0,
    Win = 1,
    UpdateData = 2,
    UpdateCard = 3,
    NewTurn = 4,
    NewPhase = 5,
    Move = 6,
    PosChange = 7,
    Set = 8,
    Swap = 9,
    Chaining = 10,
    Chained = 11,
    ChainSolving = 12,
    ChainSolved = 13,
    ChainEnd = 14,
    ChainNegated = 15,
    ChainDisabled = 16,
    BecomeTarget = 17,
    Draw = 18,
    Damage = 19,
    Recover = 20,
    Equip = 21,
    LpUpdate = 22,
    Unequip = 23,
    CardTarget = 24,
    CancelTarget = 25,
    PayLpCost = 26
}

public readonly record struct GameplayWinPayloadV1(byte Player, byte WinType);

public readonly record struct GameplayUpdateCardPayloadV1(
    byte Player,
    byte Location,
    byte Sequence,
    ModernQueryV1 Query);

public sealed class GameplayUpdateDataPayloadV1
{
    private readonly ModernQueryV1[] queries;
    private readonly ReadOnlyCollection<ModernQueryV1> queriesView;

    internal GameplayUpdateDataPayloadV1(
        byte player,
        byte location,
        IEnumerable<ModernQueryV1> queries)
    {
        Player = player;
        Location = location;
        this.queries = queries.ToArray();
        queriesView = Array.AsReadOnly(this.queries);
    }

    public byte Player { get; }

    public byte Location { get; }

    public IReadOnlyList<ModernQueryV1> Queries => queriesView;
}

public readonly record struct GameplayNewTurnPayloadV1(byte Player);

public readonly record struct GameplayNewPhasePayloadV1(ushort Phase);

public readonly record struct GameplayMovePayloadV1(
    uint CardCode,
    ModernLocInfoV1 Previous,
    ModernLocInfoV1 Current,
    uint Reason);

public readonly record struct GameplayPositionChangePayloadV1(
    uint CardCode,
    byte Controller,
    byte Location,
    byte Sequence,
    byte PreviousPosition,
    byte CurrentPosition);

public readonly record struct GameplaySetPayloadV1(
    uint CardCode,
    ModernLocInfoV1 Location);

public readonly record struct GameplaySwapPayloadV1(
    uint CardCode0,
    ModernLocInfoV1 Location0,
    uint CardCode1,
    ModernLocInfoV1 Location1);

public readonly record struct GameplayChainingPayloadV1(
    uint CardCode,
    ModernLocInfoV1 Location,
    byte TriggeringController,
    byte TriggeringLocation,
    uint TriggeringSequence,
    ulong Description,
    uint ChainSize);

public readonly record struct GameplayChainSizePayloadV1(byte ChainSize);

public sealed class GameplayBecomeTargetPayloadV1
{
    private readonly ModernLocInfoV1[] targets;
    private readonly ReadOnlyCollection<ModernLocInfoV1> targetsView;

    internal GameplayBecomeTargetPayloadV1(IEnumerable<ModernLocInfoV1> targets)
    {
        this.targets = targets.ToArray();
        targetsView = Array.AsReadOnly(this.targets);
    }

    public IReadOnlyList<ModernLocInfoV1> Targets => targetsView;
}

public readonly record struct GameplayDrawCardRecordV1(
    uint CardCode,
    uint Position);

public sealed class GameplayDrawPayloadV1
{
    private readonly GameplayDrawCardRecordV1[] cards;
    private readonly ReadOnlyCollection<GameplayDrawCardRecordV1> cardsView;

    internal GameplayDrawPayloadV1(
        byte player,
        IEnumerable<GameplayDrawCardRecordV1> cards)
    {
        Player = player;
        this.cards = cards.ToArray();
        cardsView = Array.AsReadOnly(this.cards);
    }

    public byte Player { get; }

    public IReadOnlyList<GameplayDrawCardRecordV1> Cards => cardsView;
}

public readonly record struct GameplayLifePointPayloadV1(
    byte Player,
    uint Amount);

public readonly record struct GameplayEquipPayloadV1(
    ModernLocInfoV1 Card,
    ModernLocInfoV1 Target);

public readonly record struct GameplayUnequipPayloadV1(ModernLocInfoV1 Card);

public readonly record struct GameplayCardTargetPayloadV1(
    ModernLocInfoV1 Source,
    ModernLocInfoV1 Target);

public sealed class GameplayMessageV1
{
    private GameplayMessageV1(
        byte id,
        GameplayMessageKindV1 kind,
        GameplayStartPayloadV1 start = default,
        GameplayWinPayloadV1 win = default,
        GameplayUpdateCardPayloadV1 updateCard = default,
        GameplayNewTurnPayloadV1 newTurn = default,
        GameplayNewPhasePayloadV1 newPhase = default,
        GameplayMovePayloadV1 move = default,
        GameplayPositionChangePayloadV1 positionChange = default,
        GameplaySetPayloadV1 set = default,
        GameplaySwapPayloadV1 swap = default,
        GameplayChainingPayloadV1 chaining = default,
        GameplayChainSizePayloadV1 chainSize = default,
        GameplayLifePointPayloadV1 lifePoints = default,
        GameplayEquipPayloadV1 equip = default,
        GameplayUnequipPayloadV1 unequip = default,
        GameplayCardTargetPayloadV1 cardTarget = default)
    {
        Id = id;
        Kind = kind;
        Start = start;
        Win = win;
        UpdateCard = updateCard;
        NewTurn = newTurn;
        NewPhase = newPhase;
        Move = move;
        PositionChange = positionChange;
        Set = set;
        Swap = swap;
        Chaining = chaining;
        ChainSize = chainSize;
        LifePoints = lifePoints;
        Equip = equip;
        Unequip = unequip;
        CardTarget = cardTarget;
    }

    public const byte MessageId = 4;

    public byte Id { get; }

    public GameplayMessageKindV1 Kind { get; }

    public GameplayStartPayloadV1 Start { get; }

    public GameplayWinPayloadV1 Win { get; }

    public GameplayUpdateDataPayloadV1? UpdateData { get; internal init; }

    public GameplayUpdateCardPayloadV1 UpdateCard { get; }

    public GameplayNewTurnPayloadV1 NewTurn { get; }

    public GameplayNewPhasePayloadV1 NewPhase { get; }

    public GameplayMovePayloadV1 Move { get; }

    public GameplayPositionChangePayloadV1 PositionChange { get; }

    public GameplaySetPayloadV1 Set { get; }

    public GameplaySwapPayloadV1 Swap { get; }

    public GameplayChainingPayloadV1 Chaining { get; }

    public GameplayChainSizePayloadV1 ChainSize { get; }

    public GameplayBecomeTargetPayloadV1? BecomeTarget { get; internal init; }

    public GameplayDrawPayloadV1? Draw { get; internal init; }

    public GameplayLifePointPayloadV1 LifePoints { get; }

    public GameplayEquipPayloadV1 Equip { get; }

    public GameplayUnequipPayloadV1 Unequip { get; }

    public GameplayCardTargetPayloadV1 CardTarget { get; }

    internal static GameplayMessageV1 FromStart(
        GameplayStartPayloadV1 start) =>
        new(4, GameplayMessageKindV1.Start, start: start);

    internal static GameplayMessageV1 FromWin(
        GameplayWinPayloadV1 win) =>
        new(5, GameplayMessageKindV1.Win, win: win);

    internal static GameplayMessageV1 FromUpdateData(
        GameplayUpdateDataPayloadV1 updateData)
    {
        return new GameplayMessageV1(6, GameplayMessageKindV1.UpdateData)
        {
            UpdateData = updateData
        };
    }

    internal static GameplayMessageV1 FromUpdateCard(
        GameplayUpdateCardPayloadV1 updateCard) =>
        new(7, GameplayMessageKindV1.UpdateCard, updateCard: updateCard);

    internal static GameplayMessageV1 FromNewTurn(
        GameplayNewTurnPayloadV1 newTurn) =>
        new(40, GameplayMessageKindV1.NewTurn, newTurn: newTurn);

    internal static GameplayMessageV1 FromNewPhase(
        GameplayNewPhasePayloadV1 newPhase) =>
        new(41, GameplayMessageKindV1.NewPhase, newPhase: newPhase);

    internal static GameplayMessageV1 FromMove(
        GameplayMovePayloadV1 move) =>
        new(50, GameplayMessageKindV1.Move, move: move);

    internal static GameplayMessageV1 FromPositionChange(
        GameplayPositionChangePayloadV1 positionChange) =>
        new(53, GameplayMessageKindV1.PosChange, positionChange: positionChange);

    internal static GameplayMessageV1 FromSet(
        GameplaySetPayloadV1 set) =>
        new(54, GameplayMessageKindV1.Set, set: set);

    internal static GameplayMessageV1 FromSwap(
        GameplaySwapPayloadV1 swap) =>
        new(55, GameplayMessageKindV1.Swap, swap: swap);

    internal static GameplayMessageV1 FromChaining(
        GameplayChainingPayloadV1 chaining) =>
        new(70, GameplayMessageKindV1.Chaining, chaining: chaining);

    internal static GameplayMessageV1 FromChain(
        byte id,
        GameplayMessageKindV1 kind,
        GameplayChainSizePayloadV1 chainSize) =>
        new(id, kind, chainSize: chainSize);

    internal static GameplayMessageV1 FromBecomeTarget(
        GameplayBecomeTargetPayloadV1 becomeTarget)
    {
        return new GameplayMessageV1(83, GameplayMessageKindV1.BecomeTarget)
        {
            BecomeTarget = becomeTarget
        };
    }

    internal static GameplayMessageV1 FromDraw(
        GameplayDrawPayloadV1 draw)
    {
        return new GameplayMessageV1(90, GameplayMessageKindV1.Draw)
        {
            Draw = draw
        };
    }

    internal static GameplayMessageV1 FromLifePoints(
        byte id,
        GameplayMessageKindV1 kind,
        GameplayLifePointPayloadV1 lifePoints) =>
        new(id, kind, lifePoints: lifePoints);

    internal static GameplayMessageV1 FromEquip(
        GameplayEquipPayloadV1 equip) =>
        new(93, GameplayMessageKindV1.Equip, equip: equip);

    internal static GameplayMessageV1 FromUnequip(
        GameplayUnequipPayloadV1 unequip) =>
        new(95, GameplayMessageKindV1.Unequip, unequip: unequip);

    internal static GameplayMessageV1 FromCardTarget(
        byte id,
        GameplayMessageKindV1 kind,
        GameplayCardTargetPayloadV1 cardTarget) =>
        new(id, kind, cardTarget: cardTarget);
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
