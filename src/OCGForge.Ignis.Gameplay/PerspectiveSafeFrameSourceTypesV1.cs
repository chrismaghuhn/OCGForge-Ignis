using System.Collections.ObjectModel;

namespace OCGForge.Ignis.Gameplay;

public enum PerspectiveSafeSourceSectionV1 : byte
{
    Input = 0,
    Globals = 1,
    Zones = 2,
    Entities = 3,
    Relationships = 4,
    Chain = 5,
    VisibleEvents = 6,
    MatchContext = 7
}

public enum PerspectiveSafeFrameSourceErrorCodeV1 : byte
{
    InvalidInput = 1,
    MissingGlobals = 2,
    MissingZones = 3,
    MissingEntities = 4,
    MissingRelationships = 5,
    MissingChain = 6,
    MissingVisibleEvents = 7,
    MissingMatchContext = 8,
    InvalidPlayer = 9,
    UnknownEnum = 10,
    InvalidLocator = 11,
    DuplicateLocator = 12,
    DuplicateEventIndex = 13,
    EventIndexNotIncreasing = 14,
    InvalidOrdering = 15,
    ContradictoryEntityState = 16,
    InvalidDeckState = 17,
    ChainLengthMismatch = 18,
    InvalidLifePointCardinality = 19,
    CrossSectionMismatch = 20,
    MissingMirror = 21,
    UnprovenMirrorValue = 22,
    InvalidMirrorSnapshot = 23
}

public readonly record struct PerspectiveSafeFrameSourceErrorV1(
    PerspectiveSafeFrameSourceErrorCodeV1 Code,
    PerspectiveSafeSourceSectionV1 Section);

public enum PerspectiveSafeSemanticZoneV1 : byte
{
    Unknown = 0,
    MainDeck = 1,
    Hand = 2,
    MonsterZone = 3,
    SpellTrapZone = 4,
    Graveyard = 5,
    Banished = 6,
    ExtraDeck = 7,
    FieldZone = 8,
    PendulumRelevant = 9,
    Overlay = 10
}

public enum PerspectiveSafePositionV1 : byte
{
    Unknown = 0,
    FaceUpAttack = 1,
    FaceDownAttack = 2,
    FaceUpDefense = 4,
    FaceDownDefense = 8
}

public enum PerspectiveSafeLinkMarkerV1 : byte
{
    BottomLeft = 0,
    Bottom = 1,
    BottomRight = 2,
    Left = 3,
    Right = 4,
    TopLeft = 5,
    Top = 6,
    TopRight = 7
}

public enum PerspectiveSafeRelationshipKindV1 : byte
{
    XyzMaterial = 0,
    Equip = 1,
    Target = 2
}

public enum PerspectiveSafeVisibleEventKindV1 : byte
{
    Unknown = 0,
    TurnStarted = 1,
    PhaseChanged = 2,
    CardMoved = 3,
    CardRevealed = 4,
    Summoned = 5,
    Set = 6,
    Draw = 7,
    Shuffle = 8,
    RandomizationBoundary = 9,
    LifePointsChanged = 10,
    ChainActivated = 11,
    ChainResolved = 12,
    ChainEnded = 13,
    CardDestroyed = 14,
    CardBanished = 15,
    CardReturned = 16,
    PositionChanged = 17,
    CounterChanged = 18,
    Equipped = 19,
    Unequipped = 20,
    Targeted = 21,
    Win = 22
}

public readonly record struct PerspectiveSafeCounterV1(uint Type, uint Count);

public readonly record struct PerspectiveSafeZoneV1(
    byte Player,
    PerspectiveSafeSemanticZoneV1 Kind,
    uint TotalCount,
    uint PublicIdentityCount,
    uint HiddenCount,
    bool PlayerObservableOrder);

public readonly record struct PerspectiveSafeKnowledgeV1(
    bool OwnDecklistKnown,
    bool OpponentDecklistKnown);

internal static class PerspectiveSafeCollectionV1
{
    internal static T[] Copy<T>(IEnumerable<T>? values) =>
        values is null ? Array.Empty<T>() : values.ToArray();

    internal static T[]? CopyOptional<T>(IEnumerable<T>? values) =>
        values?.ToArray();

    internal static ReadOnlyCollection<T> ReadOnly<T>(T[] values) =>
        Array.AsReadOnly(values);

    internal static string[] CopyLocators(IEnumerable<string>? values) =>
        values is null
            ? Array.Empty<string>()
            : values.Select(value => value ?? string.Empty).ToArray();
}

public enum PerspectiveSafeI6C2SourceStatusV1 : byte
{
    Proven = 0,
    Blocked = 1,
    BlockedPendingI6C3 = 2,
    BlockedPendingI6C5 = 3,
    OutsideI6CPendingI6D = 4
}

public enum PerspectiveSafeI6C2ConstituentV1 : byte
{
    LifePoints = 0,
    TurnPlayer = 1,
    TurnCount = 2,
    Phase = 3,
    Terminal = 4,
    Winner = 5,
    WinReason = 6,
    DuelFlags = 7,
    PlayerToAct = 8,
    ChainLength = 9,
    Relationships = 10,
    Chain = 11,
    VisibleEvents = 12,
    EventIndex = 13,
    MatchContext = 14,
    MainDeckZone = 15,
    HandZone = 16,
    MonsterZone = 17,
    SpellTrapLayout = 18,
    GraveyardZone = 19,
    BanishedZone = 20,
    ExtraDeckZone = 21,
    OverlayZone = 22,
    EntityLocator = 23,
    EntityIdentity = 24,
    EntityOwner = 25,
    EntityController = 26,
    EntitySequence = 27,
    EntityPosition = 28,
    EntityCurrentProperties = 29,
    EntityPrintedProperties = 30
}

public readonly record struct PerspectiveSafeI6C2ConstituentStatusV1(
    PerspectiveSafeI6C2ConstituentV1 Constituent,
    PerspectiveSafeI6C2SourceStatusV1 Status);

public sealed class PerspectiveSafeI6C2GlobalsV1
{
    private readonly uint[] lifePoints;
    private readonly ReadOnlyCollection<uint> lifePointsView;

    internal PerspectiveSafeI6C2GlobalsV1(
        IEnumerable<uint> lifePoints,
        byte? turnPlayer,
        uint? turnCount,
        uint? phase,
        bool terminal,
        byte? winner,
        byte? winReason)
    {
        this.lifePoints = PerspectiveSafeCollectionV1.Copy(lifePoints);
        lifePointsView = PerspectiveSafeCollectionV1.ReadOnly(this.lifePoints);
        TurnPlayer = turnPlayer;
        TurnCount = turnCount;
        Phase = phase;
        Terminal = terminal;
        Winner = winner;
        WinReason = winReason;
    }

    public IReadOnlyList<uint> LifePoints => lifePointsView;

    public byte? TurnPlayer { get; }

    public uint? TurnCount { get; }

    public uint? Phase { get; }

    public bool Terminal { get; }

    public byte? Winner { get; }

    public byte? WinReason { get; }
}

/// <summary>
/// Partial I6C2 evidence from a committed Mirror snapshot. It is not a
/// complete public frame and does not establish OCGForge oracle acceptance.
/// </summary>
public sealed class PerspectiveSafeI6C2StateSourceV1
{
    private readonly PerspectiveSafeZoneV1[] zones;
    private readonly PerspectiveSafeEntityV1[] entities;
    private readonly PerspectiveSafeI6C2ConstituentStatusV1[] statuses;
    private readonly ReadOnlyCollection<PerspectiveSafeZoneV1> zonesView;
    private readonly ReadOnlyCollection<PerspectiveSafeEntityV1> entitiesView;
    private readonly ReadOnlyCollection<PerspectiveSafeI6C2ConstituentStatusV1>
        statusesView;

    internal PerspectiveSafeI6C2StateSourceV1(
        PerspectiveSafeI6C2GlobalsV1 globals,
        IEnumerable<PerspectiveSafeZoneV1> zones,
        IEnumerable<PerspectiveSafeEntityV1> entities,
        IEnumerable<PerspectiveSafeI6C2ConstituentStatusV1> statuses)
    {
        Globals = globals ?? throw new ArgumentNullException(nameof(globals));
        this.zones = PerspectiveSafeCollectionV1.Copy(zones);
        this.entities = PerspectiveSafeCollectionV1.Copy(entities);
        this.statuses = PerspectiveSafeCollectionV1.Copy(statuses);
        zonesView = PerspectiveSafeCollectionV1.ReadOnly(this.zones);
        entitiesView = PerspectiveSafeCollectionV1.ReadOnly(this.entities);
        statusesView = PerspectiveSafeCollectionV1.ReadOnly(this.statuses);
    }

    public PerspectiveSafeI6C2GlobalsV1 Globals { get; }

    public IReadOnlyList<PerspectiveSafeZoneV1> Zones => zonesView;

    public IReadOnlyList<PerspectiveSafeEntityV1> Entities => entitiesView;

    public IReadOnlyList<PerspectiveSafeI6C2ConstituentStatusV1> Statuses =>
        statusesView;

    public bool IsComplete =>
        statuses.All(status =>
            status.Status == PerspectiveSafeI6C2SourceStatusV1.Proven);

    public PerspectiveSafeI6C2SourceStatusV1 GetStatus(
        PerspectiveSafeI6C2ConstituentV1 constituent)
    {
        foreach (PerspectiveSafeI6C2ConstituentStatusV1 status in statuses)
        {
            if (status.Constituent == constituent)
            {
                return status.Status;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(constituent));
    }
}

public sealed class PerspectiveSafeI6C2SourceResultV1
{
    private PerspectiveSafeI6C2SourceResultV1(
        PerspectiveSafeI6C2StateSourceV1? source,
        PerspectiveSafeFrameSourceErrorV1? error)
    {
        if ((source is null) == (error is null))
        {
            throw new ArgumentException(
                "An I6C2 result must contain exactly one outcome.");
        }

        Source = source;
        Error = error;
    }

    public PerspectiveSafeI6C2StateSourceV1? Source { get; }

    public PerspectiveSafeFrameSourceErrorV1? Error { get; }

    public bool IsSuccess => Source is not null && Error is null;

    internal static PerspectiveSafeI6C2SourceResultV1 Success(
        PerspectiveSafeI6C2StateSourceV1 source) =>
        new(source ?? throw new ArgumentNullException(nameof(source)), null);

    internal static PerspectiveSafeI6C2SourceResultV1 Failure(
        PerspectiveSafeFrameSourceErrorV1 error) =>
        new(null, error);
}

public enum PerspectiveSafeI6C3SourceStatusV1 : byte
{
    Proven = 0,
    Blocked = 1,
    BlockedPendingI6C5 = 2
}

public enum PerspectiveSafeI6C3ConstituentV1 : byte
{
    OverlayZone = 0,
    OverlayEntities = 1,
    OverlayLocators = 2,
    OverlayIdentity = 3,
    OverlayCurrentProperties = 4,
    XyzMaterialRelationships = 5,
    EquipRelationships = 6,
    TargetRelationships = 7,
    RelationshipEndpoints = 8,
    RelationshipOrdering = 9,
    ChainTriggerMetadata = 10,
    ChainLength = 11,
    ChainIndexMapping = 12,
    ChainLinkOrder = 13,
    ChainActivatingPlayer = 14,
    ChainSourceLocator = 15,
    ChainActivationZone = 16,
    ChainEffectDescription = 17,
    ChainTargets = 18
}

public readonly record struct PerspectiveSafeI6C3ConstituentStatusV1(
    PerspectiveSafeI6C3ConstituentV1 Constituent,
    PerspectiveSafeI6C3SourceStatusV1 Status);

/// <summary>
/// Partial I6C3 evidence layered over the accepted I6C2 source. It contains
/// current relation, overlay, and chain values only; it is not a complete
/// OCGForge public frame or an event-history source.
/// </summary>
public sealed class PerspectiveSafeI6C3StateSourceV1
{
    private readonly PerspectiveSafeZoneV1[] zones;
    private readonly PerspectiveSafeEntityV1[] entities;
    private readonly PerspectiveSafeRelationshipV1[] relationships;
    private readonly PerspectiveSafeI6C3ConstituentStatusV1[] statuses;
    private readonly ReadOnlyCollection<PerspectiveSafeZoneV1> zonesView;
    private readonly ReadOnlyCollection<PerspectiveSafeEntityV1> entitiesView;
    private readonly ReadOnlyCollection<PerspectiveSafeRelationshipV1>
        relationshipsView;
    private readonly ReadOnlyCollection<PerspectiveSafeI6C3ConstituentStatusV1>
        statusesView;

    internal PerspectiveSafeI6C3StateSourceV1(
        PerspectiveSafeI6C2StateSourceV1 baseSource,
        IEnumerable<PerspectiveSafeZoneV1> zones,
        IEnumerable<PerspectiveSafeEntityV1> entities,
        IEnumerable<PerspectiveSafeRelationshipV1> relationships,
        PerspectiveSafeChainStateV1 chain,
        IEnumerable<PerspectiveSafeI6C3ConstituentStatusV1> statuses)
    {
        BaseSource = baseSource ?? throw new ArgumentNullException(nameof(baseSource));
        this.zones = PerspectiveSafeCollectionV1.Copy(zones);
        this.entities = PerspectiveSafeCollectionV1.Copy(entities);
        this.relationships = PerspectiveSafeCollectionV1.Copy(relationships);
        Chain = chain ?? throw new ArgumentNullException(nameof(chain));
        this.statuses = PerspectiveSafeCollectionV1.Copy(statuses);
        zonesView = PerspectiveSafeCollectionV1.ReadOnly(this.zones);
        entitiesView = PerspectiveSafeCollectionV1.ReadOnly(this.entities);
        relationshipsView = PerspectiveSafeCollectionV1.ReadOnly(this.relationships);
        statusesView = PerspectiveSafeCollectionV1.ReadOnly(this.statuses);
    }

    public PerspectiveSafeI6C2StateSourceV1 BaseSource { get; }

    public PerspectiveSafeI6C2GlobalsV1 Globals => BaseSource.Globals;

    public IReadOnlyList<PerspectiveSafeZoneV1> Zones => zonesView;

    public IReadOnlyList<PerspectiveSafeEntityV1> Entities => entitiesView;

    public IReadOnlyList<PerspectiveSafeRelationshipV1> Relationships =>
        relationshipsView;

    public PerspectiveSafeChainStateV1 Chain { get; }

    public IReadOnlyList<PerspectiveSafeI6C3ConstituentStatusV1> Statuses =>
        statusesView;

    public bool IsComplete =>
        BaseSource.IsComplete &&
        statuses.All(status =>
            status.Status == PerspectiveSafeI6C3SourceStatusV1.Proven);

    public PerspectiveSafeI6C3SourceStatusV1 GetStatus(
        PerspectiveSafeI6C3ConstituentV1 constituent)
    {
        foreach (PerspectiveSafeI6C3ConstituentStatusV1 status in statuses)
        {
            if (status.Constituent == constituent)
            {
                return status.Status;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(constituent));
    }
}

public sealed class PerspectiveSafeI6C3SourceResultV1
{
    private PerspectiveSafeI6C3SourceResultV1(
        PerspectiveSafeI6C3StateSourceV1? source,
        PerspectiveSafeFrameSourceErrorV1? error)
    {
        if ((source is null) == (error is null))
        {
            throw new ArgumentException(
                "An I6C3 result must contain exactly one outcome.");
        }

        Source = source;
        Error = error;
    }

    public PerspectiveSafeI6C3StateSourceV1? Source { get; }

    public PerspectiveSafeFrameSourceErrorV1? Error { get; }

    public bool IsSuccess => Source is not null && Error is null;

    internal static PerspectiveSafeI6C3SourceResultV1 Success(
        PerspectiveSafeI6C3StateSourceV1 source) =>
        new(source ?? throw new ArgumentNullException(nameof(source)), null);

    internal static PerspectiveSafeI6C3SourceResultV1 Failure(
        PerspectiveSafeFrameSourceErrorV1 error) =>
        new(null, error);
}

public sealed class PerspectiveSafeGlobalsV1
{
    private readonly uint[] lifePoints;
    private readonly ReadOnlyCollection<uint> lifePointsView;

    public PerspectiveSafeGlobalsV1(
        ulong duelFlags,
        IEnumerable<uint>? lifePoints,
        byte? playerToAct = null,
        byte? turnPlayer = null,
        uint? turnCount = null,
        uint? phase = null,
        uint chainLength = 0,
        byte? winner = null,
        byte? winReason = null,
        bool terminal = false)
    {
        DuelFlags = duelFlags;
        this.lifePoints = PerspectiveSafeCollectionV1.Copy(lifePoints);
        lifePointsView = PerspectiveSafeCollectionV1.ReadOnly(this.lifePoints);
        PlayerToAct = playerToAct;
        TurnPlayer = turnPlayer;
        TurnCount = turnCount;
        Phase = phase;
        ChainLength = chainLength;
        Winner = winner;
        WinReason = winReason;
        Terminal = terminal;
    }

    public ulong DuelFlags { get; }

    public IReadOnlyList<uint> LifePoints => lifePointsView;

    public byte? PlayerToAct { get; }

    public byte? TurnPlayer { get; }

    public uint? TurnCount { get; }

    public uint? Phase { get; }

    public uint ChainLength { get; }

    public byte? Winner { get; }

    public byte? WinReason { get; }

    public bool Terminal { get; }
}

public sealed class PerspectiveSafeCardPropertiesV1
{
    private readonly PerspectiveSafeLinkMarkerV1[] linkMarkers;
    private readonly ReadOnlyCollection<PerspectiveSafeLinkMarkerV1> linkMarkersView;
    private readonly PerspectiveSafeCounterV1[] counters;
    private readonly ReadOnlyCollection<PerspectiveSafeCounterV1> countersView;

    public PerspectiveSafeCardPropertiesV1(
        uint? type = null,
        uint? attribute = null,
        ulong? race = null,
        int? attack = null,
        int? defense = null,
        int? baseAttack = null,
        int? baseDefense = null,
        uint? level = null,
        uint? rank = null,
        uint? linkRating = null,
        IEnumerable<PerspectiveSafeLinkMarkerV1>? linkMarkers = null,
        uint? leftScale = null,
        uint? rightScale = null,
        uint? statusFlags = null,
        IEnumerable<PerspectiveSafeCounterV1>? counters = null)
    {
        Type = type;
        Attribute = attribute;
        Race = race;
        Attack = attack;
        Defense = defense;
        BaseAttack = baseAttack;
        BaseDefense = baseDefense;
        Level = level;
        Rank = rank;
        LinkRating = linkRating;
        this.linkMarkers = PerspectiveSafeCollectionV1.Copy(linkMarkers);
        linkMarkersView = PerspectiveSafeCollectionV1.ReadOnly(this.linkMarkers);
        LeftScale = leftScale;
        RightScale = rightScale;
        StatusFlags = statusFlags;
        this.counters = PerspectiveSafeCollectionV1.Copy(counters);
        countersView = PerspectiveSafeCollectionV1.ReadOnly(this.counters);
    }

    public uint? Type { get; }

    public uint? Attribute { get; }

    public ulong? Race { get; }

    public int? Attack { get; }

    public int? Defense { get; }

    public int? BaseAttack { get; }

    public int? BaseDefense { get; }

    public uint? Level { get; }

    public uint? Rank { get; }

    public uint? LinkRating { get; }

    public IReadOnlyList<PerspectiveSafeLinkMarkerV1> LinkMarkers =>
        linkMarkersView;

    public uint? LeftScale { get; }

    public uint? RightScale { get; }

    public uint? StatusFlags { get; }

    public IReadOnlyList<PerspectiveSafeCounterV1> Counters => countersView;
}

public sealed class PerspectiveSafeEntityV1
{
    private readonly string locator;

    public PerspectiveSafeEntityV1(
        string? locator,
        bool identityKnown,
        uint? passcode,
        byte? owner,
        byte? controller,
        PerspectiveSafeSemanticZoneV1 zone,
        uint? sequence,
        uint? overlaySequence,
        PerspectiveSafePositionV1 position,
        bool faceUp,
        bool faceDown,
        PerspectiveSafeCardPropertiesV1? printed = null,
        PerspectiveSafeCardPropertiesV1? current = null)
    {
        this.locator = locator ?? string.Empty;
        IdentityKnown = identityKnown;
        Passcode = passcode;
        Owner = owner;
        Controller = controller;
        Zone = zone;
        Sequence = sequence;
        OverlaySequence = overlaySequence;
        Position = position;
        FaceUp = faceUp;
        FaceDown = faceDown;
        Printed = printed;
        Current = current;
    }

    public string Locator => locator;

    public bool IdentityKnown { get; }

    public uint? Passcode { get; }

    public byte? Owner { get; }

    public byte? Controller { get; }

    public PerspectiveSafeSemanticZoneV1 Zone { get; }

    public uint? Sequence { get; }

    public uint? OverlaySequence { get; }

    public PerspectiveSafePositionV1 Position { get; }

    public bool FaceUp { get; }

    public bool FaceDown { get; }

    public PerspectiveSafeCardPropertiesV1? Printed { get; }

    public PerspectiveSafeCardPropertiesV1? Current { get; }
}

public sealed class PerspectiveSafeRelationshipV1
{
    private readonly string source;
    private readonly string target;

    public PerspectiveSafeRelationshipV1(
        PerspectiveSafeRelationshipKindV1 kind,
        string? source,
        string? target)
    {
        Kind = kind;
        this.source = source ?? string.Empty;
        this.target = target ?? string.Empty;
    }

    public PerspectiveSafeRelationshipKindV1 Kind { get; }

    public string Source => source;

    public string Target => target;
}

public sealed class PerspectiveSafeChainLinkV1
{
    private readonly string? source;
    private readonly string[] targets;
    private readonly ReadOnlyCollection<string> targetsView;

    public PerspectiveSafeChainLinkV1(
        uint index,
        byte? activatingPlayer = null,
        string? source = null,
        PerspectiveSafeSemanticZoneV1? activationZone = null,
        ulong? effectDescription = null,
        IEnumerable<string>? targets = null)
    {
        Index = index;
        ActivatingPlayer = activatingPlayer;
        this.source = source;
        ActivationZone = activationZone;
        EffectDescription = effectDescription;
        this.targets = PerspectiveSafeCollectionV1.CopyLocators(targets);
        targetsView = PerspectiveSafeCollectionV1.ReadOnly(this.targets);
    }

    public uint Index { get; }

    public byte? ActivatingPlayer { get; }

    public string? Source => source;

    public PerspectiveSafeSemanticZoneV1? ActivationZone { get; }

    public ulong? EffectDescription { get; }

    public IReadOnlyList<string> Targets => targetsView;
}

public sealed class PerspectiveSafeChainStateV1
{
    private readonly PerspectiveSafeChainLinkV1[] links;
    private readonly ReadOnlyCollection<PerspectiveSafeChainLinkV1> linksView;

    public PerspectiveSafeChainStateV1(
        uint length,
        IEnumerable<PerspectiveSafeChainLinkV1>? links)
    {
        Length = length;
        this.links = PerspectiveSafeCollectionV1.Copy(links);
        linksView = PerspectiveSafeCollectionV1.ReadOnly(this.links);
    }

    public uint Length { get; }

    public IReadOnlyList<PerspectiveSafeChainLinkV1> Links => linksView;
}

public sealed class PerspectiveSafeVisibleEventV1
{
    private readonly string? entityLocator;
    private readonly string[] targets;
    private readonly ReadOnlyCollection<string> targetsView;

    public PerspectiveSafeVisibleEventV1(
        ulong eventIndex,
        PerspectiveSafeVisibleEventKindV1 kind,
        byte? player = null,
        string? entityLocator = null,
        uint? publicPasscode = null,
        PerspectiveSafeSemanticZoneV1? fromZone = null,
        PerspectiveSafeSemanticZoneV1? toZone = null,
        uint? count = null,
        int? amount = null,
        uint? counterType = null,
        uint? phase = null,
        byte? winner = null,
        byte? winReason = null,
        ulong? effectDescription = null,
        IEnumerable<string>? targets = null)
    {
        EventIndex = eventIndex;
        Kind = kind;
        Player = player;
        this.entityLocator = entityLocator;
        PublicPasscode = publicPasscode;
        FromZone = fromZone;
        ToZone = toZone;
        Count = count;
        Amount = amount;
        CounterType = counterType;
        Phase = phase;
        Winner = winner;
        WinReason = winReason;
        EffectDescription = effectDescription;
        this.targets = PerspectiveSafeCollectionV1.CopyLocators(targets);
        targetsView = PerspectiveSafeCollectionV1.ReadOnly(this.targets);
    }

    public ulong EventIndex { get; }

    public PerspectiveSafeVisibleEventKindV1 Kind { get; }

    public byte? Player { get; }

    public string? EntityLocator => entityLocator;

    public uint? PublicPasscode { get; }

    public PerspectiveSafeSemanticZoneV1? FromZone { get; }

    public PerspectiveSafeSemanticZoneV1? ToZone { get; }

    public uint? Count { get; }

    public int? Amount { get; }

    public uint? CounterType { get; }

    public uint? Phase { get; }

    public byte? Winner { get; }

    public byte? WinReason { get; }

    public ulong? EffectDescription { get; }

    public IReadOnlyList<string> Targets => targetsView;
}

public sealed class PerspectiveSafeDeckV1
{
    private readonly uint[] mainDeck;
    private readonly uint[] extraDeck;
    private readonly ReadOnlyCollection<uint> mainDeckView;
    private readonly ReadOnlyCollection<uint> extraDeckView;

    public PerspectiveSafeDeckV1(
        bool known,
        IEnumerable<uint>? mainDeck = null,
        IEnumerable<uint>? extraDeck = null)
    {
        Known = known;
        this.mainDeck = PerspectiveSafeCollectionV1.Copy(mainDeck);
        this.extraDeck = PerspectiveSafeCollectionV1.Copy(extraDeck);
        mainDeckView = PerspectiveSafeCollectionV1.ReadOnly(this.mainDeck);
        extraDeckView = PerspectiveSafeCollectionV1.ReadOnly(this.extraDeck);
    }

    public bool Known { get; }

    public IReadOnlyList<uint> MainDeck => mainDeckView;

    public IReadOnlyList<uint> ExtraDeck => extraDeckView;
}

public sealed class PerspectiveSafeMatchContextV1
{
    public PerspectiveSafeMatchContextV1(
        byte perspectivePlayer,
        ulong duelFlags,
        PerspectiveSafeKnowledgeV1 knowledge,
        PerspectiveSafeDeckV1 ownDeck,
        PerspectiveSafeDeckV1 opponentDeck)
    {
        PerspectivePlayer = perspectivePlayer;
        DuelFlags = duelFlags;
        Knowledge = knowledge;
        OwnDeck = ownDeck ?? throw new ArgumentNullException(nameof(ownDeck));
        OpponentDeck = opponentDeck ??
            throw new ArgumentNullException(nameof(opponentDeck));
    }

    public byte PerspectivePlayer { get; }

    public ulong DuelFlags { get; }

    public PerspectiveSafeKnowledgeV1 Knowledge { get; }

    public PerspectiveSafeDeckV1 OwnDeck { get; }

    public PerspectiveSafeDeckV1 OpponentDeck { get; }
}

public sealed class PerspectiveSafeFrameSourceInputV1
{
    private readonly PerspectiveSafeZoneV1[]? zones;
    private readonly PerspectiveSafeEntityV1[]? entities;
    private readonly PerspectiveSafeRelationshipV1[]? relationships;
    private readonly PerspectiveSafeVisibleEventV1[]? visibleEvents;
    private readonly ReadOnlyCollection<PerspectiveSafeZoneV1>? zonesView;
    private readonly ReadOnlyCollection<PerspectiveSafeEntityV1>? entitiesView;
    private readonly ReadOnlyCollection<PerspectiveSafeRelationshipV1>?
        relationshipsView;
    private readonly ReadOnlyCollection<PerspectiveSafeVisibleEventV1>?
        visibleEventsView;

    public PerspectiveSafeFrameSourceInputV1(
        PerspectiveSafeGlobalsV1? globals,
        IEnumerable<PerspectiveSafeZoneV1>? zones,
        IEnumerable<PerspectiveSafeEntityV1>? entities,
        IEnumerable<PerspectiveSafeRelationshipV1>? relationships,
        PerspectiveSafeChainStateV1? chain,
        IEnumerable<PerspectiveSafeVisibleEventV1>? visibleEvents,
        PerspectiveSafeMatchContextV1? matchContext)
    {
        Globals = globals;
        this.zones = PerspectiveSafeCollectionV1.CopyOptional(zones);
        this.entities = PerspectiveSafeCollectionV1.CopyOptional(entities);
        this.relationships =
            PerspectiveSafeCollectionV1.CopyOptional(relationships);
        Chain = chain;
        this.visibleEvents =
            PerspectiveSafeCollectionV1.CopyOptional(visibleEvents);
        MatchContext = matchContext;
        zonesView = this.zones is null
            ? null
            : PerspectiveSafeCollectionV1.ReadOnly(this.zones);
        entitiesView = this.entities is null
            ? null
            : PerspectiveSafeCollectionV1.ReadOnly(this.entities);
        relationshipsView = this.relationships is null
            ? null
            : PerspectiveSafeCollectionV1.ReadOnly(this.relationships);
        visibleEventsView = this.visibleEvents is null
            ? null
            : PerspectiveSafeCollectionV1.ReadOnly(this.visibleEvents);
    }

    public PerspectiveSafeGlobalsV1? Globals { get; }

    public IReadOnlyList<PerspectiveSafeZoneV1>? Zones => zonesView;

    public IReadOnlyList<PerspectiveSafeEntityV1>? Entities => entitiesView;

    public IReadOnlyList<PerspectiveSafeRelationshipV1>? Relationships =>
        relationshipsView;

    public PerspectiveSafeChainStateV1? Chain { get; }

    public IReadOnlyList<PerspectiveSafeVisibleEventV1>? VisibleEvents =>
        visibleEventsView;

    public PerspectiveSafeMatchContextV1? MatchContext { get; }
}

/// <summary>
/// A structurally accepted, immutable source container. This type does not
/// establish runtime provenance or OCGForge oracle compatibility.
/// </summary>
public sealed class PerspectiveSafeFrameV1
{
    private readonly PerspectiveSafeZoneV1[] zones;
    private readonly PerspectiveSafeEntityV1[] entities;
    private readonly PerspectiveSafeRelationshipV1[] relationships;
    private readonly PerspectiveSafeVisibleEventV1[] visibleEvents;
    private readonly ReadOnlyCollection<PerspectiveSafeZoneV1> zonesView;
    private readonly ReadOnlyCollection<PerspectiveSafeEntityV1> entitiesView;
    private readonly ReadOnlyCollection<PerspectiveSafeRelationshipV1>
        relationshipsView;
    private readonly ReadOnlyCollection<PerspectiveSafeVisibleEventV1>
        visibleEventsView;

    internal PerspectiveSafeFrameV1(PerspectiveSafeFrameSourceInputV1 input)
    {
        Globals = input.Globals!;
        zones = input.Zones!.ToArray();
        entities = input.Entities!.ToArray();
        relationships = input.Relationships!.ToArray();
        Chain = input.Chain!;
        visibleEvents = input.VisibleEvents!.ToArray();
        MatchContext = input.MatchContext!;
        zonesView = PerspectiveSafeCollectionV1.ReadOnly(zones);
        entitiesView = PerspectiveSafeCollectionV1.ReadOnly(entities);
        relationshipsView = PerspectiveSafeCollectionV1.ReadOnly(relationships);
        visibleEventsView = PerspectiveSafeCollectionV1.ReadOnly(visibleEvents);
    }

    public PerspectiveSafeGlobalsV1 Globals { get; }

    public IReadOnlyList<PerspectiveSafeZoneV1> Zones => zonesView;

    public IReadOnlyList<PerspectiveSafeEntityV1> Entities => entitiesView;

    public IReadOnlyList<PerspectiveSafeRelationshipV1> Relationships =>
        relationshipsView;

    public PerspectiveSafeChainStateV1 Chain { get; }

    public IReadOnlyList<PerspectiveSafeVisibleEventV1> VisibleEvents =>
        visibleEventsView;

    public PerspectiveSafeMatchContextV1 MatchContext { get; }
}

public sealed class PerspectiveSafeFrameSourceResultV1
{
    private PerspectiveSafeFrameSourceResultV1(
        PerspectiveSafeFrameV1? frame,
        PerspectiveSafeFrameSourceErrorV1? error)
    {
        if ((frame is null) == (error is null))
        {
            throw new ArgumentException(
                "A source result must contain exactly one outcome.");
        }

        Frame = frame;
        Error = error;
    }

    public PerspectiveSafeFrameV1? Frame { get; }

    public PerspectiveSafeFrameSourceErrorV1? Error { get; }

    public bool IsSuccess => Frame is not null && Error is null;

    internal static PerspectiveSafeFrameSourceResultV1 Success(
        PerspectiveSafeFrameV1 frame) =>
        new(frame ?? throw new ArgumentNullException(nameof(frame)), null);

    internal static PerspectiveSafeFrameSourceResultV1 Failure(
        PerspectiveSafeFrameSourceErrorV1 error) =>
        new(null, error);
}
