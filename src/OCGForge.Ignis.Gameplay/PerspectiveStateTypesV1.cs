using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace OCGForge.Ignis.Gameplay;

public enum MirrorParticipantRoleV1 : byte
{
    Self = 0,
    Opponent = 1
}

public enum MirrorZoneV1 : byte
{
    MainDeck = 0,
    ExtraDeck = 1,
    Hand = 2,
    MonsterZone = 3,
    SpellTrapZone = 4,
    Graveyard = 5,
    Banished = 6
}

public enum MirrorProvenanceV1 : byte
{
    PublicProtocolFact = 0,
    PerspectivePrivateFact = 1,
    DerivedFromProvenPublicFacts = 2,
    UnknownRedacted = 3
}

public readonly record struct MirrorValueV1<T>(
    bool IsKnown,
    T Value,
    MirrorProvenanceV1 Provenance);

public static class MirrorValueV1
{
    public static MirrorValueV1<T> Known<T>(
        T value,
        MirrorProvenanceV1 provenance = MirrorProvenanceV1.PublicProtocolFact) =>
        new(true, value, provenance);

    public static MirrorValueV1<T> Unknown<T>() =>
        new(false, default!, MirrorProvenanceV1.UnknownRedacted);
}

[SuppressMessage(
    "Naming",
    "CA1720:IdentifiersShouldNotContainTypeNames",
    Justification = "The kind names describe the exact protocol scalar widths.")]
public enum MirrorQueryValueKindV1 : byte
{
    Unknown = 0,
    UInt8 = 1,
    UInt32 = 2,
    Int32 = 3,
    UInt64 = 4,
    UInt32Pair = 5,
    UInt32Vector = 6,
    PackedUInt32Vector = 7,
    EntityReference = 8,
    EntityReferenceVector = 9
}

public sealed class MirrorQueryValueV1 : IEquatable<MirrorQueryValueV1>
{
    private readonly uint[] uint32Values;
    private readonly ReadOnlyCollection<uint> uint32ValuesView;
    private readonly MirrorEntityIdV1[] entityReferences;
    private readonly ReadOnlyCollection<MirrorEntityIdV1> entityReferencesView;

    private MirrorQueryValueV1(
        MirrorQueryValueKindV1 kind,
        bool isKnown,
        MirrorProvenanceV1 provenance,
        byte uint8Value,
        uint uint32Value,
        int int32Value,
        ulong uint64Value,
        uint linkMarker,
        IEnumerable<uint>? uint32Values = null,
        IEnumerable<MirrorEntityIdV1>? entityReferences = null)
    {
        Kind = kind;
        IsKnown = isKnown;
        Provenance = provenance;
        UInt8Value = uint8Value;
        UInt32Value = uint32Value;
        Int32Value = int32Value;
        UInt64Value = uint64Value;
        LinkMarker = linkMarker;
        this.uint32Values = uint32Values?.ToArray() ?? Array.Empty<uint>();
        uint32ValuesView = Array.AsReadOnly(this.uint32Values);
        this.entityReferences = entityReferences?.ToArray() ??
            Array.Empty<MirrorEntityIdV1>();
        entityReferencesView = Array.AsReadOnly(this.entityReferences);
    }

    public MirrorQueryValueKindV1 Kind { get; }

    public bool IsKnown { get; }

    public MirrorProvenanceV1 Provenance { get; }

    public byte UInt8Value { get; }

    public uint UInt32Value { get; }

    public int Int32Value { get; }

    public ulong UInt64Value { get; }

    public uint LinkMarker { get; }

    public IReadOnlyList<uint> UInt32Values => uint32ValuesView;

    public int EntityReferenceCount => entityReferences.Length;

    internal IReadOnlyList<MirrorEntityIdV1> EntityReferences =>
        entityReferencesView;

    internal static MirrorQueryValueV1 Unknown() =>
        new(
            MirrorQueryValueKindV1.Unknown,
            false,
            MirrorProvenanceV1.UnknownRedacted,
            0,
            0,
            0,
            0,
            0);

    internal static MirrorQueryValueV1 UInt8(
        byte value,
        MirrorProvenanceV1 provenance) =>
        new(
            MirrorQueryValueKindV1.UInt8,
            true,
            provenance,
            value,
            0,
            0,
            0,
            0);

    internal static MirrorQueryValueV1 UInt32(
        uint value,
        MirrorProvenanceV1 provenance) =>
        new(
            MirrorQueryValueKindV1.UInt32,
            true,
            provenance,
            0,
            value,
            0,
            0,
            0);

    internal static MirrorQueryValueV1 Int32(
        int value,
        MirrorProvenanceV1 provenance) =>
        new(
            MirrorQueryValueKindV1.Int32,
            true,
            provenance,
            0,
            0,
            value,
            0,
            0);

    internal static MirrorQueryValueV1 UInt64(
        ulong value,
        MirrorProvenanceV1 provenance) =>
        new(
            MirrorQueryValueKindV1.UInt64,
            true,
            provenance,
            0,
            0,
            0,
            value,
            0);

    internal static MirrorQueryValueV1 UInt32Pair(
        uint value,
        uint marker,
        MirrorProvenanceV1 provenance) =>
        new(
            MirrorQueryValueKindV1.UInt32Pair,
            true,
            provenance,
            0,
            value,
            0,
            0,
            marker);

    internal static MirrorQueryValueV1 UInt32Vector(
        IEnumerable<uint> values,
        MirrorProvenanceV1 provenance,
        bool packed) =>
        new(
            packed
                ? MirrorQueryValueKindV1.PackedUInt32Vector
                : MirrorQueryValueKindV1.UInt32Vector,
            true,
            provenance,
            0,
            0,
            0,
            0,
            0,
            values);

    internal static MirrorQueryValueV1 EntityReference(
        MirrorEntityIdV1 value) =>
        new(
            MirrorQueryValueKindV1.EntityReference,
            true,
            MirrorProvenanceV1.DerivedFromProvenPublicFacts,
            0,
            0,
            0,
            0,
            0,
            entityReferences: new[] { value });

    internal static MirrorQueryValueV1 EntityReferenceVector(
        IEnumerable<MirrorEntityIdV1> values) =>
        new(
            MirrorQueryValueKindV1.EntityReferenceVector,
            true,
            MirrorProvenanceV1.DerivedFromProvenPublicFacts,
            0,
            0,
            0,
            0,
            0,
            entityReferences: values);

    internal string ToDeterministicString()
    {
        StringBuilder builder = new();
        builder.Append((byte)Kind).Append('|')
            .Append(IsKnown ? 'K' : 'U').Append('|')
            .Append((byte)Provenance).Append('|')
            .Append(UInt8Value).Append('|')
            .Append(UInt32Value).Append('|')
            .Append(Int32Value).Append('|')
            .Append(UInt64Value).Append('|')
            .Append(LinkMarker).Append('|');
        foreach (uint value in uint32Values)
        {
            builder.Append(value).Append(',');
        }

        builder.Append('|');
        foreach (MirrorEntityIdV1 value in entityReferences)
        {
            builder.Append(value.Ordinal).Append(',');
        }

        return builder.ToString();
    }

    public bool Equals(MirrorQueryValueV1? other) =>
        other is not null &&
        Kind == other.Kind &&
        IsKnown == other.IsKnown &&
        Provenance == other.Provenance &&
        UInt8Value == other.UInt8Value &&
        UInt32Value == other.UInt32Value &&
        Int32Value == other.Int32Value &&
        UInt64Value == other.UInt64Value &&
        LinkMarker == other.LinkMarker &&
        uint32Values.AsSpan().SequenceEqual(other.uint32Values) &&
        entityReferences.AsSpan().SequenceEqual(other.entityReferences);

    public override bool Equals(object? obj) =>
        obj is MirrorQueryValueV1 other && Equals(other);

    public override int GetHashCode() =>
        MirrorHashV1.Stable(ToDeterministicString());
}

internal readonly record struct MirrorEntityIdV1(ulong Ordinal);

public readonly record struct MirrorTerminalSnapshotV1(
    bool IsTerminal,
    MirrorParticipantRoleV1? Winner,
    byte WinType);

public enum MirrorChainStatusV1 : byte
{
    Chained = 0,
    Solving = 1,
    Solved = 2,
    Negated = 3,
    Disabled = 4
}

public sealed class MirrorQueryFieldSnapshotV1 : IEquatable<MirrorQueryFieldSnapshotV1>
{
    internal MirrorQueryFieldSnapshotV1(
        QueryFlagV1 flag,
        MirrorQueryValueV1 value)
    {
        Flag = flag;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public QueryFlagV1 Flag { get; }

    public MirrorQueryValueV1 Value { get; }

    public bool IsKnown => Value.IsKnown;

    public MirrorProvenanceV1 Provenance => Value.Provenance;

    internal string ToDeterministicString()
    {
        return ((uint)Flag).ToString(CultureInfo.InvariantCulture) + "=" +
               Value.ToDeterministicString();
    }

    public bool Equals(MirrorQueryFieldSnapshotV1? other) =>
        other is not null && Flag == other.Flag && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is MirrorQueryFieldSnapshotV1 other && Equals(other);

    public override int GetHashCode() =>
        MirrorHashV1.Stable(ToDeterministicString());
}

public sealed class MirrorCardSnapshotV1 : IEquatable<MirrorCardSnapshotV1>
{
    private readonly MirrorQueryFieldSnapshotV1[] queryFields;
    private readonly ReadOnlyCollection<MirrorQueryFieldSnapshotV1> queryFieldsView;

    internal MirrorCardSnapshotV1(
        MirrorEntityIdV1 entityId,
        MirrorParticipantRoleV1 controller,
        MirrorValueV1<MirrorParticipantRoleV1> owner,
        MirrorZoneV1 zone,
        uint sequence,
        bool isOverlay,
        uint overlayIndex,
        MirrorValueV1<uint> position,
        MirrorValueV1<uint> cardCode,
        IEnumerable<MirrorQueryFieldSnapshotV1> queryFields)
    {
        EntityId = entityId;
        Controller = controller;
        Owner = owner;
        Zone = zone;
        Sequence = sequence;
        IsOverlay = isOverlay;
        OverlayIndex = overlayIndex;
        Position = position;
        CardCode = cardCode;
        this.queryFields = queryFields.ToArray();
        queryFieldsView = Array.AsReadOnly(this.queryFields);
    }

    /// <summary>
    /// Internal mirror identity only. This is not a public semantic locator.
    /// </summary>
    internal MirrorEntityIdV1 EntityId { get; }

    public MirrorParticipantRoleV1 Controller { get; }

    public MirrorValueV1<MirrorParticipantRoleV1> Owner { get; }

    public MirrorZoneV1 Zone { get; }

    public uint Sequence { get; }

    public bool IsOverlay { get; }

    public uint OverlayIndex { get; }

    public MirrorValueV1<uint> Position { get; }

    public MirrorValueV1<uint> CardCode { get; }

    public IReadOnlyList<MirrorQueryFieldSnapshotV1> QueryFields => queryFieldsView;

    public bool Equals(MirrorCardSnapshotV1? other) =>
        other is not null &&
        EntityId.Equals(other.EntityId) &&
        Controller == other.Controller &&
        Owner.Equals(other.Owner) &&
        Zone == other.Zone &&
        Sequence == other.Sequence &&
        IsOverlay == other.IsOverlay &&
        OverlayIndex == other.OverlayIndex &&
        Position.Equals(other.Position) &&
        CardCode.Equals(other.CardCode) &&
        queryFields.AsSpan().SequenceEqual(other.queryFields);

    public override bool Equals(object? obj) =>
        obj is MirrorCardSnapshotV1 other && Equals(other);

    public override int GetHashCode() =>
        MirrorHashV1.Stable(ToDeterministicString());

    internal string ToDeterministicString()
    {
        StringBuilder builder = new();
        builder.Append(EntityId.Ordinal).Append('|')
            .Append((byte)Controller).Append('|')
            .Append((byte)Zone).Append('|')
            .Append(Sequence).Append('|')
            .Append(IsOverlay ? 1 : 0).Append('|')
            .Append(OverlayIndex).Append('|')
            .Append(Position.IsKnown ? 'K' : 'U').Append(':')
            .Append(Position.Value).Append(':')
            .Append((byte)Position.Provenance).Append('|')
            .Append(CardCode.IsKnown ? 'K' : 'U').Append(':')
            .Append(CardCode.Value).Append(':')
            .Append((byte)CardCode.Provenance).Append('|')
            .Append(Owner.IsKnown ? 'K' : 'U').Append(':')
            .Append((byte)Owner.Value).Append(':')
            .Append((byte)Owner.Provenance).Append('|');
        foreach (MirrorQueryFieldSnapshotV1 field in queryFields)
        {
            builder.Append(field.ToDeterministicString()).Append(';');
        }

        return builder.ToString();
    }
}

public sealed class MirrorZoneSnapshotV1 : IEquatable<MirrorZoneSnapshotV1>
{
    private readonly MirrorCardSnapshotV1[] cards;
    private readonly ReadOnlyCollection<MirrorCardSnapshotV1> cardsView;

    internal MirrorZoneSnapshotV1(
        MirrorZoneV1 zone,
        MirrorValueV1<uint> count,
        IEnumerable<MirrorCardSnapshotV1> cards)
    {
        Zone = zone;
        Count = count;
        this.cards = cards.ToArray();
        cardsView = Array.AsReadOnly(this.cards);
    }

    public MirrorZoneV1 Zone { get; }

    public MirrorValueV1<uint> Count { get; }

    public IReadOnlyList<MirrorCardSnapshotV1> Cards => cardsView;

    public bool Equals(MirrorZoneSnapshotV1? other) =>
        other is not null &&
        Zone == other.Zone &&
        Count.Equals(other.Count) &&
        cards.AsSpan().SequenceEqual(other.cards);

    public override bool Equals(object? obj) =>
        obj is MirrorZoneSnapshotV1 other && Equals(other);

    public override int GetHashCode() =>
        MirrorHashV1.Stable(ToDeterministicString());

    internal string ToDeterministicString()
    {
        StringBuilder builder = new();
        builder.Append((byte)Zone).Append('|')
            .Append(Count.IsKnown ? 'K' : 'U').Append(':')
            .Append(Count.Value).Append(':')
            .Append((byte)Count.Provenance).Append('|');
        foreach (MirrorCardSnapshotV1 card in cards)
        {
            builder.Append(card.ToDeterministicString()).Append(';');
        }

        return builder.ToString();
    }
}

public sealed class MirrorParticipantSnapshotV1 : IEquatable<MirrorParticipantSnapshotV1>
{
    private readonly MirrorZoneSnapshotV1[] zones;
    private readonly ReadOnlyCollection<MirrorZoneSnapshotV1> zonesView;

    internal MirrorParticipantSnapshotV1(
        MirrorParticipantRoleV1 role,
        MirrorValueV1<uint> lifePoints,
        IEnumerable<MirrorZoneSnapshotV1> zones)
    {
        Role = role;
        LifePoints = lifePoints;
        this.zones = zones.ToArray();
        zonesView = Array.AsReadOnly(this.zones);
    }

    public MirrorParticipantRoleV1 Role { get; }

    public MirrorValueV1<uint> LifePoints { get; }

    public IReadOnlyList<MirrorZoneSnapshotV1> Zones => zonesView;

    public MirrorZoneSnapshotV1 GetZone(MirrorZoneV1 zone)
    {
        foreach (MirrorZoneSnapshotV1 value in zones)
        {
            if (value.Zone == zone)
            {
                return value;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(zone));
    }

    public bool Equals(MirrorParticipantSnapshotV1? other) =>
        other is not null &&
        Role == other.Role &&
        LifePoints.Equals(other.LifePoints) &&
        zones.AsSpan().SequenceEqual(other.zones);

    public override bool Equals(object? obj) =>
        obj is MirrorParticipantSnapshotV1 other && Equals(other);

    public override int GetHashCode() =>
        MirrorHashV1.Stable(ToDeterministicString());

    internal string ToDeterministicString()
    {
        StringBuilder builder = new();
        builder.Append((byte)Role).Append('|')
            .Append(LifePoints.IsKnown ? 'K' : 'U').Append(':')
            .Append(LifePoints.Value).Append(':')
            .Append((byte)LifePoints.Provenance).Append('|');
        foreach (MirrorZoneSnapshotV1 zone in zones)
        {
            builder.Append(zone.ToDeterministicString()).Append(';');
        }

        return builder.ToString();
    }
}

internal readonly record struct MirrorRelationSnapshotV1(
    MirrorEntityIdV1 Source,
    MirrorEntityIdV1 Target,
    ulong Ordinal);

public sealed class MirrorChainSnapshotV1 : IEquatable<MirrorChainSnapshotV1>
{
    private readonly MirrorEntityIdV1[] targets;
    private readonly ReadOnlyCollection<MirrorEntityIdV1> targetsView;

    internal MirrorChainSnapshotV1(
        uint chainSize,
        MirrorEntityIdV1 card,
        MirrorValueV1<uint> cardCode,
        ulong description,
        MirrorChainStatusV1 status,
        byte triggeringController,
        byte triggeringLocation,
        uint triggeringSequence,
        IEnumerable<MirrorEntityIdV1> targets)
    {
        ChainSize = chainSize;
        Card = card;
        CardCode = cardCode;
        Description = description;
        Status = status;
        TriggeringController = triggeringController;
        TriggeringLocation = triggeringLocation;
        TriggeringSequence = triggeringSequence;
        this.targets = targets.ToArray();
        targetsView = Array.AsReadOnly(this.targets);
    }

    public uint ChainSize { get; }

    internal MirrorEntityIdV1 Card { get; }

    public MirrorValueV1<uint> CardCode { get; }

    public ulong Description { get; }

    public MirrorChainStatusV1 Status { get; }

    internal byte TriggeringController { get; }

    internal byte TriggeringLocation { get; }

    internal uint TriggeringSequence { get; }

    internal IReadOnlyList<MirrorEntityIdV1> Targets => targetsView;

    public bool Equals(MirrorChainSnapshotV1? other) =>
        other is not null &&
        ChainSize == other.ChainSize &&
        Card.Equals(other.Card) &&
            CardCode == other.CardCode &&
            Description == other.Description &&
            Status == other.Status &&
            TriggeringController == other.TriggeringController &&
            TriggeringLocation == other.TriggeringLocation &&
            TriggeringSequence == other.TriggeringSequence &&
            targets.AsSpan().SequenceEqual(other.targets);

    public override bool Equals(object? obj) =>
        obj is MirrorChainSnapshotV1 other && Equals(other);

    public override int GetHashCode() =>
        MirrorHashV1.Stable(ToDeterministicString());

    internal string ToDeterministicString()
    {
        StringBuilder builder = new();
        builder.Append(ChainSize).Append('|')
            .Append(Card.Ordinal).Append('|')
            .Append(CardCode.IsKnown ? 'K' : 'U').Append(':')
            .Append(CardCode.Value).Append(':')
            .Append((byte)CardCode.Provenance).Append('|')
            .Append(Description).Append('|')
            .Append((byte)Status).Append('|')
            .Append(TriggeringController).Append('|')
            .Append(TriggeringLocation).Append('|')
            .Append(TriggeringSequence).Append('|');
        foreach (MirrorEntityIdV1 target in targets)
        {
            builder.Append(target.Ordinal).Append(',');
        }

        return builder.ToString();
    }
}

public sealed class MirrorSnapshotV1 : IEquatable<MirrorSnapshotV1>
{
    private readonly MirrorParticipantSnapshotV1[] participants;
    private readonly MirrorCardSnapshotV1[] cards;
    private readonly MirrorChainSnapshotV1[] chains;
    private readonly MirrorRelationSnapshotV1[] targetRelations;
    private readonly MirrorRelationSnapshotV1[] chainTargetRelations;
    private readonly MirrorRelationSnapshotV1[] equipmentRelations;
    private readonly MirrorRelationSnapshotV1[] overlayRelations;
    private readonly ReadOnlyCollection<MirrorParticipantSnapshotV1> participantsView;
    private readonly ReadOnlyCollection<MirrorCardSnapshotV1> cardsView;
    private readonly ReadOnlyCollection<MirrorChainSnapshotV1> chainsView;
    private readonly ReadOnlyCollection<MirrorRelationSnapshotV1> targetRelationsView;
    private readonly ReadOnlyCollection<MirrorRelationSnapshotV1> chainTargetRelationsView;
    private readonly ReadOnlyCollection<MirrorRelationSnapshotV1> equipmentRelationsView;
    private readonly ReadOnlyCollection<MirrorRelationSnapshotV1> overlayRelationsView;

    internal MirrorSnapshotV1(
        GameplayPerspectiveV1 perspective,
        IEnumerable<MirrorParticipantSnapshotV1> participants,
        IEnumerable<MirrorCardSnapshotV1> cards,
        ulong turnCount,
        MirrorValueV1<MirrorParticipantRoleV1> turnPlayer,
        MirrorValueV1<ushort> phase,
        MirrorTerminalSnapshotV1 terminal,
        MirrorValueV1<MirrorEntityIdV1> pendingChain,
        IEnumerable<MirrorChainSnapshotV1> chains,
        IEnumerable<MirrorRelationSnapshotV1> targetRelations,
        IEnumerable<MirrorRelationSnapshotV1> chainTargetRelations,
        IEnumerable<MirrorRelationSnapshotV1> equipmentRelations,
        IEnumerable<MirrorRelationSnapshotV1> overlayRelations,
        MirrorChainSnapshotV1? pendingChainSource = null)
    {
        Perspective = perspective ?? throw new ArgumentNullException(nameof(perspective));
        this.participants = participants.ToArray();
        this.cards = cards.ToArray();
        this.chains = chains.ToArray();
        this.targetRelations = targetRelations.ToArray();
        this.chainTargetRelations = chainTargetRelations.ToArray();
        this.equipmentRelations = equipmentRelations.ToArray();
        this.overlayRelations = overlayRelations.ToArray();
        participantsView = Array.AsReadOnly(this.participants);
        cardsView = Array.AsReadOnly(this.cards);
        chainsView = Array.AsReadOnly(this.chains);
        targetRelationsView = Array.AsReadOnly(this.targetRelations);
        chainTargetRelationsView = Array.AsReadOnly(this.chainTargetRelations);
        equipmentRelationsView = Array.AsReadOnly(this.equipmentRelations);
        overlayRelationsView = Array.AsReadOnly(this.overlayRelations);
        TurnCount = turnCount;
        TurnPlayer = turnPlayer;
        Phase = phase;
        Terminal = terminal;
        PendingChain = pendingChain;
        PendingChainSource = pendingChainSource;
    }

    public GameplayPerspectiveV1 Perspective { get; }

    public IReadOnlyList<MirrorParticipantSnapshotV1> Participants => participantsView;

    public IReadOnlyList<MirrorCardSnapshotV1> Cards => cardsView;

    public ulong TurnCount { get; }

    public MirrorValueV1<MirrorParticipantRoleV1> TurnPlayer { get; }

    public MirrorValueV1<ushort> Phase { get; }

    public MirrorTerminalSnapshotV1 Terminal { get; }

    internal MirrorValueV1<MirrorEntityIdV1> PendingChain { get; }

    internal MirrorChainSnapshotV1? PendingChainSource { get; }

    public IReadOnlyList<MirrorChainSnapshotV1> Chains => chainsView;

    internal IReadOnlyList<MirrorRelationSnapshotV1> TargetRelations => targetRelationsView;

    internal IReadOnlyList<MirrorRelationSnapshotV1> ChainTargetRelations =>
        chainTargetRelationsView;

    internal IReadOnlyList<MirrorRelationSnapshotV1> EquipmentRelations => equipmentRelationsView;

    internal IReadOnlyList<MirrorRelationSnapshotV1> OverlayRelations => overlayRelationsView;

    public MirrorParticipantSnapshotV1 GetParticipant(MirrorParticipantRoleV1 role) =>
        participants[(int)role];

    public MirrorZoneSnapshotV1 GetZone(
        MirrorParticipantRoleV1 role,
        MirrorZoneV1 zone) =>
        GetParticipant(role).GetZone(zone);

    internal string ToDeterministicString()
    {
        StringBuilder builder = new();
        builder.Append((byte)Perspective.Kind).Append('|')
            .Append(TurnCount).Append('|')
            .Append(TurnPlayer.IsKnown ? 'K' : 'U').Append(':')
            .Append((byte)TurnPlayer.Value).Append(':')
            .Append((byte)TurnPlayer.Provenance).Append('|')
            .Append(Phase.IsKnown ? 'K' : 'U').Append(':')
            .Append(Phase.Value).Append(':')
            .Append((byte)Phase.Provenance).Append('|')
            .Append(Terminal.IsTerminal ? 'T' : 'N').Append(':')
            .Append(Terminal.Winner.HasValue ? (byte)Terminal.Winner.Value : 255).Append(':')
            .Append(Terminal.WinType).Append('|');
        builder.Append(PendingChain.IsKnown ? 'K' : 'U').Append(':')
            .Append(PendingChain.Value.Ordinal).Append(':')
            .Append((byte)PendingChain.Provenance).Append('|');
        if (PendingChainSource is not null)
        {
            builder.Append(PendingChainSource.ToDeterministicString()).Append('|');
        }
        foreach (MirrorParticipantSnapshotV1 participant in participants)
        {
            builder.Append(participant.ToDeterministicString()).Append(';');
        }

        foreach (MirrorCardSnapshotV1 card in cards)
        {
            builder.Append(card.ToDeterministicString()).Append(';');
        }

        foreach (MirrorChainSnapshotV1 chain in chains)
        {
            builder.Append(chain.ToDeterministicString()).Append(';');
        }

        AppendRelations(builder, targetRelations);
        AppendRelations(builder, chainTargetRelations);
        AppendRelations(builder, equipmentRelations);
        AppendRelations(builder, overlayRelations);
        return builder.ToString();
    }

    public bool Equals(MirrorSnapshotV1? other) =>
        other is not null &&
        ToDeterministicString() == other.ToDeterministicString();

    public override bool Equals(object? obj) =>
        obj is MirrorSnapshotV1 other && Equals(other);

    public override int GetHashCode() =>
        MirrorHashV1.Stable(ToDeterministicString());

    private static void AppendRelations(
        StringBuilder builder,
        IEnumerable<MirrorRelationSnapshotV1> relations)
    {
        foreach (MirrorRelationSnapshotV1 relation in relations)
        {
            builder.Append(relation.Ordinal).Append(':')
                .Append(relation.Source.Ordinal).Append('>')
                .Append(relation.Target.Ordinal).Append(';');
        }
    }
}

public readonly record struct MirrorCreateResult(
    bool IsSuccess,
    GameplayErrorCode Error,
    PerspectiveStateMirrorV1? Mirror)
{
    internal static MirrorCreateResult Success(PerspectiveStateMirrorV1 mirror) =>
        new(true, GameplayErrorCode.None, mirror);

    internal static MirrorCreateResult Failure(GameplayErrorCode error) =>
        new(false, error, null);
}

public readonly record struct MirrorApplyResult(
    bool IsSuccess,
    GameplayErrorCode Error,
    MirrorSnapshotV1 Snapshot)
{
    internal static MirrorApplyResult Success(MirrorSnapshotV1 snapshot) =>
        new(true, GameplayErrorCode.None, snapshot);

    internal static MirrorApplyResult Failure(
        GameplayErrorCode error,
        MirrorSnapshotV1 snapshot) =>
        new(false, error, snapshot);
}

internal static class MirrorHashV1
{
    internal static int Stable(string value)
    {
        uint hash = 2166136261;
        foreach (byte character in Encoding.UTF8.GetBytes(value))
        {
            hash ^= character;
            hash *= 16777619;
        }

        return unchecked((int)hash);
    }
}
