using System.Collections.ObjectModel;
using System.Security.Cryptography;
using OCGForge.Ignis.Gameplay;

namespace OCGForge.Ignis.Model;

public sealed class OcgForgeEncodedCardPropertiesV1
{
    private readonly byte[] linkMarkerCodes;
    private readonly OcgForgeEncodedCounterV1[] counters;
    private readonly ReadOnlyCollection<byte> linkMarkerCodesView;
    private readonly ReadOnlyCollection<OcgForgeEncodedCounterV1> countersView;

    internal OcgForgeEncodedCardPropertiesV1(
        uint? type,
        uint? attribute,
        ulong? race,
        int? attack,
        int? defense,
        int? baseAttack,
        int? baseDefense,
        uint? level,
        uint? rank,
        uint? linkRating,
        IEnumerable<byte> linkMarkerCodes,
        uint? leftScale,
        uint? rightScale,
        uint? statusFlags,
        IEnumerable<OcgForgeEncodedCounterV1> counters)
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
        this.linkMarkerCodes = (linkMarkerCodes ??
            throw new ArgumentNullException(nameof(linkMarkerCodes))).ToArray();
        LeftScale = leftScale;
        RightScale = rightScale;
        StatusFlags = statusFlags;
        this.counters = (counters ?? throw new ArgumentNullException(nameof(counters)))
            .ToArray();
        linkMarkerCodesView = Array.AsReadOnly(this.linkMarkerCodes);
        countersView = Array.AsReadOnly(this.counters);
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

    public IReadOnlyList<byte> LinkMarkerCodes => linkMarkerCodesView;

    public uint? LeftScale { get; }

    public uint? RightScale { get; }

    public uint? StatusFlags { get; }

    public IReadOnlyList<OcgForgeEncodedCounterV1> Counters => countersView;
}

public readonly record struct OcgForgeEncodedCounterV1(uint Type, uint Count);

public sealed class OcgForgeEncodedGlobalsV1
{
    private readonly uint[] lifePoints;
    private readonly ReadOnlyCollection<uint> lifePointsView;

    internal OcgForgeEncodedGlobalsV1(
        ulong duelFlags,
        IEnumerable<uint> lifePoints,
        byte? playerToAct,
        byte? turnPlayer,
        uint? turnCount,
        uint? phase,
        uint chainLength,
        byte? winner,
        byte? winReason,
        bool terminal)
    {
        DuelFlags = duelFlags;
        this.lifePoints = (lifePoints ?? throw new ArgumentNullException(nameof(lifePoints)))
            .ToArray();
        lifePointsView = Array.AsReadOnly(this.lifePoints);
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

public readonly record struct OcgForgeEncodedZoneV1(
    byte Player,
    byte KindCode,
    uint TotalCount,
    uint PublicIdentityCount,
    uint HiddenCount,
    bool PlayerObservableOrder);

public sealed class OcgForgeEncodedEntityV1
{
    internal OcgForgeEncodedEntityV1(
        uint publicLocatorOrdinal,
        bool identityKnown,
        uint cardVocabularyId,
        byte? owner,
        byte? controller,
        byte zoneCode,
        uint? sequence,
        uint? overlaySequence,
        byte positionCode,
        bool faceUp,
        bool faceDown,
        OcgForgeEncodedCardPropertiesV1? printed,
        OcgForgeEncodedCardPropertiesV1? current)
    {
        PublicLocatorOrdinal = publicLocatorOrdinal;
        IdentityKnown = identityKnown;
        CardVocabularyId = cardVocabularyId;
        Owner = owner;
        Controller = controller;
        ZoneCode = zoneCode;
        Sequence = sequence;
        OverlaySequence = overlaySequence;
        PositionCode = positionCode;
        FaceUp = faceUp;
        FaceDown = faceDown;
        Printed = printed;
        Current = current;
    }

    public uint PublicLocatorOrdinal { get; }

    public bool IdentityKnown { get; }

    public uint CardVocabularyId { get; }

    public byte? Owner { get; }

    public byte? Controller { get; }

    public byte ZoneCode { get; }

    public uint? Sequence { get; }

    public uint? OverlaySequence { get; }

    public byte PositionCode { get; }

    public bool FaceUp { get; }

    public bool FaceDown { get; }

    public OcgForgeEncodedCardPropertiesV1? Printed { get; }

    public OcgForgeEncodedCardPropertiesV1? Current { get; }
}

public readonly record struct OcgForgeEncodedCurrentReferenceV1(
    uint PublicLocatorOrdinal,
    uint? CurrentEntityOrdinal);

public readonly record struct OcgForgeEncodedCardReferenceV1(
    byte KindCode,
    OcgForgeEncodedCurrentReferenceV1 Reference);

public readonly record struct OcgForgeEncodedRelationshipV1(
    byte KindCode,
    OcgForgeEncodedCurrentReferenceV1 Source,
    OcgForgeEncodedCurrentReferenceV1 Target);

public sealed class OcgForgeEncodedChainLinkV1
{
    private readonly OcgForgeEncodedCurrentReferenceV1[] targets;
    private readonly ReadOnlyCollection<OcgForgeEncodedCurrentReferenceV1> targetsView;

    internal OcgForgeEncodedChainLinkV1(
        uint index,
        byte? activatingPlayer,
        OcgForgeEncodedCurrentReferenceV1? source,
        byte? activationZoneCode,
        ulong? effectDescription,
        IEnumerable<OcgForgeEncodedCurrentReferenceV1> targets)
    {
        Index = index;
        ActivatingPlayer = activatingPlayer;
        Source = source;
        ActivationZoneCode = activationZoneCode;
        EffectDescription = effectDescription;
        this.targets = (targets ?? throw new ArgumentNullException(nameof(targets)))
            .ToArray();
        targetsView = Array.AsReadOnly(this.targets);
    }

    public uint Index { get; }

    public byte? ActivatingPlayer { get; }

    public OcgForgeEncodedCurrentReferenceV1? Source { get; }

    public byte? ActivationZoneCode { get; }

    public ulong? EffectDescription { get; }

    public IReadOnlyList<OcgForgeEncodedCurrentReferenceV1> Targets => targetsView;
}

public sealed class OcgForgeEncodedChainStateV1
{
    private readonly OcgForgeEncodedChainLinkV1[] links;
    private readonly ReadOnlyCollection<OcgForgeEncodedChainLinkV1> linksView;

    internal OcgForgeEncodedChainStateV1(
        uint length,
        IEnumerable<OcgForgeEncodedChainLinkV1> links)
    {
        Length = length;
        this.links = (links ?? throw new ArgumentNullException(nameof(links)))
            .ToArray();
        linksView = Array.AsReadOnly(this.links);
    }

    public uint Length { get; }

    public IReadOnlyList<OcgForgeEncodedChainLinkV1> Links => linksView;
}

public sealed class OcgForgeEncodedVisibleEventV1
{
    private readonly uint[] targetPublicLocatorOrdinals;
    private readonly ReadOnlyCollection<uint> targetPublicLocatorOrdinalsView;

    internal OcgForgeEncodedVisibleEventV1(
        ulong eventIndex,
        byte kindCode,
        byte? player,
        uint? publicLocatorOrdinal,
        uint? publicCardVocabularyId,
        byte? fromZoneCode,
        byte? toZoneCode,
        uint? count,
        int? amount,
        uint? counterType,
        uint? phase,
        byte? winner,
        byte? winReason,
        ulong? effectDescription,
        IEnumerable<uint> targetPublicLocatorOrdinals)
    {
        EventIndex = eventIndex;
        KindCode = kindCode;
        Player = player;
        PublicLocatorOrdinal = publicLocatorOrdinal;
        PublicCardVocabularyId = publicCardVocabularyId;
        FromZoneCode = fromZoneCode;
        ToZoneCode = toZoneCode;
        Count = count;
        Amount = amount;
        CounterType = counterType;
        Phase = phase;
        Winner = winner;
        WinReason = winReason;
        EffectDescription = effectDescription;
        this.targetPublicLocatorOrdinals = (targetPublicLocatorOrdinals ??
            throw new ArgumentNullException(nameof(targetPublicLocatorOrdinals))).ToArray();
        targetPublicLocatorOrdinalsView =
            Array.AsReadOnly(this.targetPublicLocatorOrdinals);
    }

    public ulong EventIndex { get; }

    public byte KindCode { get; }

    public byte? Player { get; }

    public uint? PublicLocatorOrdinal { get; }

    public uint? PublicCardVocabularyId { get; }

    public byte? FromZoneCode { get; }

    public byte? ToZoneCode { get; }

    public uint? Count { get; }

    public int? Amount { get; }

    public uint? CounterType { get; }

    public uint? Phase { get; }

    public byte? Winner { get; }

    public byte? WinReason { get; }

    public ulong? EffectDescription { get; }

    public IReadOnlyList<uint> TargetPublicLocatorOrdinals =>
        targetPublicLocatorOrdinalsView;
}

public sealed class OcgForgeEncodedDeckV1
{
    private readonly uint[] mainDeck;
    private readonly uint[] extraDeck;
    private readonly ReadOnlyCollection<uint> mainDeckView;
    private readonly ReadOnlyCollection<uint> extraDeckView;

    internal OcgForgeEncodedDeckV1(
        bool known,
        IEnumerable<uint> mainDeck,
        IEnumerable<uint> extraDeck)
    {
        Known = known;
        this.mainDeck = (mainDeck ?? throw new ArgumentNullException(nameof(mainDeck)))
            .ToArray();
        this.extraDeck = (extraDeck ?? throw new ArgumentNullException(nameof(extraDeck)))
            .ToArray();
        mainDeckView = Array.AsReadOnly(this.mainDeck);
        extraDeckView = Array.AsReadOnly(this.extraDeck);
    }

    public bool Known { get; }

    public IReadOnlyList<uint> MainDeck => mainDeckView;

    public IReadOnlyList<uint> ExtraDeck => extraDeckView;
}

public sealed class OcgForgeEncodedMatchContextV1
{
    internal OcgForgeEncodedMatchContextV1(
        byte perspectivePlayer,
        ulong duelFlags,
        bool ownDecklistKnown,
        bool opponentDecklistKnown,
        OcgForgeEncodedDeckV1 ownDeck,
        OcgForgeEncodedDeckV1 opponentDeck)
    {
        PerspectivePlayer = perspectivePlayer;
        DuelFlags = duelFlags;
        OwnDecklistKnown = ownDecklistKnown;
        OpponentDecklistKnown = opponentDecklistKnown;
        OwnDeck = ownDeck ?? throw new ArgumentNullException(nameof(ownDeck));
        OpponentDeck = opponentDeck ??
            throw new ArgumentNullException(nameof(opponentDeck));
    }

    public byte PerspectivePlayer { get; }

    public ulong DuelFlags { get; }

    public bool OwnDecklistKnown { get; }

    public bool OpponentDecklistKnown { get; }

    public OcgForgeEncodedDeckV1 OwnDeck { get; }

    public OcgForgeEncodedDeckV1 OpponentDeck { get; }
}

public readonly record struct OcgForgeEncodedChoiceV1(
    byte KindCode,
    ulong Value,
    uint? ResponseIndex);

public readonly record struct OcgForgeEncodedCandidateV1(
    ushort ActionKindCode,
    OcgForgeEncodedChoiceV1? Choice,
    OcgForgeEncodedCardReferenceV1? SourceReference,
    OcgForgeEncodedCardReferenceV1? TargetReference,
    uint? Phase,
    byte? Position,
    uint? SourceIndex,
    int? Amount,
    byte ContinuationOperationCode,
    bool SubmitsEngineResponse);

public sealed class OcgForgeEncodedModelInputV1
{
    private readonly string[] publicLocatorTable;
    private readonly uint[] observationContextReferenceOrdinals;
    private readonly OcgForgeEncodedZoneV1[] zones;
    private readonly OcgForgeEncodedEntityV1[] entities;
    private readonly OcgForgeEncodedRelationshipV1[] relationships;
    private readonly OcgForgeEncodedVisibleEventV1[] visibleEvents;
    private readonly OcgForgeEncodedCandidateV1[] candidateFeatures;
    private readonly string[] routingKeys;
    private readonly ReadOnlyCollection<string> publicLocatorTableView;
    private readonly ReadOnlyCollection<uint> observationContextReferenceOrdinalsView;
    private readonly ReadOnlyCollection<OcgForgeEncodedZoneV1> zonesView;
    private readonly ReadOnlyCollection<OcgForgeEncodedEntityV1> entitiesView;
    private readonly ReadOnlyCollection<OcgForgeEncodedRelationshipV1> relationshipsView;
    private readonly ReadOnlyCollection<OcgForgeEncodedVisibleEventV1> visibleEventsView;
    private readonly ReadOnlyCollection<OcgForgeEncodedCandidateV1> candidateFeaturesView;
    private readonly ReadOnlyCollection<string> routingKeysView;

    internal OcgForgeEncodedModelInputV1(
        string schemaId,
        string cardVocabularyIdentity,
        string publicObservationDigest,
        byte perspectivePlayer,
        ulong decisionIndex,
        IEnumerable<string> publicLocatorTable,
        ushort? publicObservationContextKindCode,
        byte? publicObservationContextPlayer,
        IEnumerable<uint> observationContextReferenceOrdinals,
        OcgForgeEncodedGlobalsV1 globals,
        IEnumerable<OcgForgeEncodedZoneV1> zones,
        IEnumerable<OcgForgeEncodedEntityV1> entities,
        IEnumerable<OcgForgeEncodedRelationshipV1> relationships,
        OcgForgeEncodedChainStateV1 chain,
        IEnumerable<OcgForgeEncodedVisibleEventV1> visibleEvents,
        OcgForgeEncodedMatchContextV1 matchContext,
        string? publicCandidateDomainDigest,
        IEnumerable<OcgForgeEncodedCandidateV1> candidateFeatures,
        IEnumerable<string> routingKeys)
    {
        SchemaIdValue = schemaId ?? throw new ArgumentNullException(nameof(schemaId));
        CardVocabularyIdentity = cardVocabularyIdentity ??
            throw new ArgumentNullException(nameof(cardVocabularyIdentity));
        PublicObservationDigest = publicObservationDigest ??
            throw new ArgumentNullException(nameof(publicObservationDigest));
        PerspectivePlayer = perspectivePlayer;
        DecisionIndex = decisionIndex;
        this.publicLocatorTable = (publicLocatorTable ??
            throw new ArgumentNullException(nameof(publicLocatorTable))).ToArray();
        PublicObservationContextKindCode = publicObservationContextKindCode;
        PublicObservationContextPlayer = publicObservationContextPlayer;
        this.observationContextReferenceOrdinals = (observationContextReferenceOrdinals ??
            throw new ArgumentNullException(nameof(observationContextReferenceOrdinals)))
            .ToArray();
        Globals = globals ?? throw new ArgumentNullException(nameof(globals));
        this.zones = (zones ?? throw new ArgumentNullException(nameof(zones))).ToArray();
        this.entities = (entities ?? throw new ArgumentNullException(nameof(entities))).ToArray();
        this.relationships = (relationships ??
            throw new ArgumentNullException(nameof(relationships))).ToArray();
        Chain = chain ?? throw new ArgumentNullException(nameof(chain));
        this.visibleEvents = (visibleEvents ??
            throw new ArgumentNullException(nameof(visibleEvents))).ToArray();
        MatchContext = matchContext ??
            throw new ArgumentNullException(nameof(matchContext));
        PublicCandidateDomainDigest = publicCandidateDomainDigest;
        this.candidateFeatures = (candidateFeatures ??
            throw new ArgumentNullException(nameof(candidateFeatures))).ToArray();
        this.routingKeys = (routingKeys ?? throw new ArgumentNullException(nameof(routingKeys)))
            .ToArray();
        publicLocatorTableView = Array.AsReadOnly(this.publicLocatorTable);
        observationContextReferenceOrdinalsView =
            Array.AsReadOnly(this.observationContextReferenceOrdinals);
        zonesView = Array.AsReadOnly(this.zones);
        entitiesView = Array.AsReadOnly(this.entities);
        relationshipsView = Array.AsReadOnly(this.relationships);
        visibleEventsView = Array.AsReadOnly(this.visibleEvents);
        candidateFeaturesView = Array.AsReadOnly(this.candidateFeatures);
        routingKeysView = Array.AsReadOnly(this.routingKeys);
    }

    public const string SchemaId = "ocgforge.model_encoded_input.v1";

    public string SchemaIdValue { get; }

    public string CardVocabularyIdentity { get; }

    public string PublicObservationDigest { get; }

    public byte PerspectivePlayer { get; }

    public ulong DecisionIndex { get; }

    public IReadOnlyList<string> PublicLocatorTable => publicLocatorTableView;

    public ushort? PublicObservationContextKindCode { get; }

    public byte? PublicObservationContextPlayer { get; }

    public IReadOnlyList<uint> ObservationContextReferenceOrdinals =>
        observationContextReferenceOrdinalsView;

    public OcgForgeEncodedGlobalsV1 Globals { get; }

    public IReadOnlyList<OcgForgeEncodedZoneV1> Zones => zonesView;

    public IReadOnlyList<OcgForgeEncodedEntityV1> Entities => entitiesView;

    public IReadOnlyList<OcgForgeEncodedRelationshipV1> Relationships =>
        relationshipsView;

    public OcgForgeEncodedChainStateV1 Chain { get; }

    public IReadOnlyList<OcgForgeEncodedVisibleEventV1> VisibleEvents =>
        visibleEventsView;

    public OcgForgeEncodedMatchContextV1 MatchContext { get; }

    public string? PublicCandidateDomainDigest { get; }

    public IReadOnlyList<OcgForgeEncodedCandidateV1> CandidateFeatures =>
        candidateFeaturesView;

    public IReadOnlyList<string> RoutingKeys => routingKeysView;

    public int CandidateCount => candidateFeatures.Length;

    public byte[] CanonicalBytes => OcgForgeI6EEncodedCodecV1.CanonicalBytes(this);
}

public enum OcgForgeEncodedModelInputErrorCodeV1 : byte
{
    InvalidLogicalModelInput = 0,
    UnknownPublicPasscode = 1,
    InvalidEncodedModelInput = 2,
    InternalFailure = 3
}

public readonly record struct OcgForgeEncodedModelInputErrorV1(
    OcgForgeEncodedModelInputErrorCodeV1 Code,
    string FieldPath);

public sealed class OcgForgeEncodedModelInputResultV1
{
    private OcgForgeEncodedModelInputResultV1(
        bool isSuccess,
        OcgForgeEncodedModelInputErrorV1? error,
        OcgForgeEncodedModelInputV1? value)
    {
        IsSuccess = isSuccess;
        Error = error;
        Value = value;
    }

    public bool IsSuccess { get; }

    public OcgForgeEncodedModelInputErrorV1? Error { get; }

    public OcgForgeEncodedModelInputV1? Value { get; }

    internal static OcgForgeEncodedModelInputResultV1 Success(
        OcgForgeEncodedModelInputV1 value) =>
        new(true, null, value);

    internal static OcgForgeEncodedModelInputResultV1 Failure(
        OcgForgeEncodedModelInputErrorCodeV1 code,
        string fieldPath) =>
        new(false, new(code, fieldPath), null);
}

public static class OcgForgeEncodedModelInputBridgeV1
{
    public static OcgForgeEncodedModelInputResultV1 TryCreate(
        OcgForgeLogicalModelInputV1? logical,
        OcgForgeCardVocabularyV1? vocabulary)
    {
        if (logical is null)
        {
            return OcgForgeEncodedModelInputResultV1.Failure(
                OcgForgeEncodedModelInputErrorCodeV1.InvalidLogicalModelInput,
                "logical");
        }

        if (vocabulary is null)
        {
            return OcgForgeEncodedModelInputResultV1.Failure(
                OcgForgeEncodedModelInputErrorCodeV1.InvalidLogicalModelInput,
                "vocabulary");
        }

        try
        {
            _ = logical.CanonicalBytes;
            OcgForgeEncodedModelInputV1 encoded = Encode(logical, vocabulary);
            _ = encoded.CanonicalBytes;
            return OcgForgeEncodedModelInputResultV1.Success(encoded);
        }
        catch (I6EUnknownPasscode failure)
        {
            return OcgForgeEncodedModelInputResultV1.Failure(
                OcgForgeEncodedModelInputErrorCodeV1.UnknownPublicPasscode,
                failure.FieldPath);
        }
        catch (ArgumentException)
        {
            return OcgForgeEncodedModelInputResultV1.Failure(
                OcgForgeEncodedModelInputErrorCodeV1.InvalidLogicalModelInput,
                "logical");
        }
        catch (OverflowException)
        {
            return OcgForgeEncodedModelInputResultV1.Failure(
                OcgForgeEncodedModelInputErrorCodeV1.InvalidEncodedModelInput,
                "encoded");
        }
    }

    private static OcgForgeEncodedModelInputV1 Encode(
        OcgForgeLogicalModelInputV1 logical,
        OcgForgeCardVocabularyV1 vocabulary)
    {
        OcgForgeLogicalPublicStateV1 state = logical.PublicSafeState;
        OcgForgeEncodedEntityV1[] entities = state.Entities
            .Select(entity => new OcgForgeEncodedEntityV1(
                entity.PublicLocatorOrdinal,
                entity.Card.IdentityKnown,
                VocabularyId(entity.Card, vocabulary, $"entities[{entity.CurrentEntityOrdinal}].passcode"),
                entity.Card.Owner,
                entity.Card.Controller,
                (byte)entity.Card.Zone,
                entity.Card.Sequence,
                entity.Card.OverlaySequence,
                (byte)entity.Card.Position,
                entity.Card.FaceUp,
                entity.Card.FaceDown,
                EncodeProperties(entity.Card.Printed),
                EncodeProperties(entity.Card.Current)))
            .ToArray();

        OcgForgeEncodedVisibleEventV1[] events = state.VisibleEvents
            .Select((@event, index) => new OcgForgeEncodedVisibleEventV1(
                @event.EventIndex,
                (byte)@event.Kind,
                @event.Player,
                @event.Entity?.Locator.PublicLocatorOrdinal,
                @event.PublicPasscode is null
                    ? null
                    : VocabularyId(
                        @event.PublicPasscode.Value,
                        vocabulary,
                        $"visible_events[{index}].public_passcode"),
                @event.FromZone is null ? null : (byte?)@event.FromZone.Value,
                @event.ToZone is null ? null : (byte?)@event.ToZone.Value,
                @event.Count,
                @event.Amount,
                @event.CounterType,
                @event.Phase,
                @event.Winner,
                @event.WinReason,
                @event.EffectDescription,
                @event.Targets.Select(target => target.Locator.PublicLocatorOrdinal)))
            .ToArray();

        OcgForgeEncodedDeckV1 ownDeck = EncodeDeck(
            state.MatchContext.OwnDeck,
            vocabulary,
            "match_context.own_deck");
        OcgForgeEncodedDeckV1 opponentDeck = EncodeDeck(
            state.MatchContext.OpponentDeck,
            vocabulary,
            "match_context.opponent_deck");

        OcgForgeEncodedCandidateV1[] candidates = logical.CandidateFeatures
            .Select(candidate => new OcgForgeEncodedCandidateV1(
                ActionKindCode(candidate.ActionKind),
                candidate.Choice is null ? null : EncodeChoice(candidate.Choice.Value),
                EncodeCardReference(candidate.SourceReference),
                EncodeCardReference(candidate.TargetReference),
                candidate.Phase,
                candidate.Position,
                candidate.SourceIndex,
                candidate.Amount,
                ContinuationCode(candidate.ContinuationOperation),
                candidate.SubmitsEngineResponse))
            .ToArray();

        return new OcgForgeEncodedModelInputV1(
            OcgForgeEncodedModelInputV1.SchemaId,
            vocabulary.Identity,
            logical.PublicObservationDigest,
            logical.PerspectivePlayer,
            logical.DecisionIndex,
            logical.PublicLocatorTable.Select(value => value.Value),
            logical.PublicObservationContextKind is null
                ? null
                : RequestKindCode(logical.PublicObservationContextKind),
            logical.PublicObservationContextPlayer,
            logical.ReferencedPublicEntities.Select(value => value.PublicLocatorOrdinal),
            new OcgForgeEncodedGlobalsV1(
                state.Globals.DuelFlags,
                state.Globals.LifePoints,
                state.Globals.PlayerToAct,
                state.Globals.TurnPlayer,
                state.Globals.TurnCount,
                state.Globals.Phase,
                state.Globals.ChainLength,
                state.Globals.Winner,
                state.Globals.WinReason,
                state.Globals.Terminal),
            state.Zones.Select(value => new OcgForgeEncodedZoneV1(
                value.Player,
                (byte)value.Kind,
                value.TotalCount,
                value.PublicIdentityCount,
                value.HiddenCount,
                value.PlayerObservableOrder)),
            entities,
            state.Relationships.Select(value => new OcgForgeEncodedRelationshipV1(
                (byte)value.Kind,
                EncodeCurrentReference(value.Source),
                EncodeCurrentReference(value.Target))),
            new OcgForgeEncodedChainStateV1(
                state.Chain.Length,
                state.Chain.Links.Select(link => new OcgForgeEncodedChainLinkV1(
                    link.Index,
                    link.ActivatingPlayer,
                    link.Source.HasValue
                        ? EncodeCurrentReference(link.Source.Value)
                        : null,
                    link.ActivationZone is null
                        ? null
                        : (byte?)link.ActivationZone.Value,
                    link.EffectDescription,
                    link.Targets.Select(EncodeCurrentReference)))),
            events,
            new OcgForgeEncodedMatchContextV1(
                state.MatchContext.PerspectivePlayer,
                state.MatchContext.DuelFlags,
                state.MatchContext.Knowledge.OwnDecklistKnown,
                state.MatchContext.Knowledge.OpponentDecklistKnown,
                ownDeck,
                opponentDeck),
            logical.PublicCandidateDomainDigest,
            candidates,
            logical.CandidateRouting.Select(value => value.PublicActionKey));
    }

    private static OcgForgeEncodedCardPropertiesV1? EncodeProperties(
        PerspectiveSafeCardPropertiesV1? properties)
    {
        if (properties is null)
        {
            return null;
        }

        return new OcgForgeEncodedCardPropertiesV1(
            properties.Type,
            properties.Attribute,
            properties.Race,
            properties.Attack,
            properties.Defense,
            properties.BaseAttack,
            properties.BaseDefense,
            properties.Level,
            properties.Rank,
            properties.LinkRating,
            properties.LinkMarkers.Select(value => (byte)value),
            properties.LeftScale,
            properties.RightScale,
            properties.StatusFlags,
            properties.Counters.Select(value => new OcgForgeEncodedCounterV1(
                value.Type,
                value.Count)));
    }

    private static uint VocabularyId(
        PerspectiveSafeEntityV1 entity,
        OcgForgeCardVocabularyV1 vocabulary,
        string path)
    {
        if (!entity.IdentityKnown)
        {
            if (entity.Passcode.HasValue || entity.Printed is not null || entity.Current is not null)
            {
                throw new ArgumentException("hidden entity contains identity data");
            }

            return OcgForgeCardVocabularyV1.UnknownOrRedactedId;
        }

        if (!entity.Passcode.HasValue)
        {
            throw new ArgumentException("known entity has no passcode");
        }

        return VocabularyId(entity.Passcode.Value, vocabulary, path);
    }

    private static uint VocabularyId(
        uint passcode,
        OcgForgeCardVocabularyV1 vocabulary,
        string path)
    {
        uint? id = vocabulary.IdForPublicPasscode(passcode);
        if (!id.HasValue || id.Value < 2)
        {
            throw new I6EUnknownPasscode(path);
        }

        return id.Value;
    }

    private static OcgForgeEncodedDeckV1 EncodeDeck(
        PerspectiveSafeDeckV1 deck,
        OcgForgeCardVocabularyV1 vocabulary,
        string path)
    {
        if (!deck.Known)
        {
            if (deck.MainDeck.Count != 0 || deck.ExtraDeck.Count != 0)
            {
                throw new ArgumentException("unknown deck contains identity data");
            }

            return new OcgForgeEncodedDeckV1(false, Array.Empty<uint>(), Array.Empty<uint>());
        }

        return new OcgForgeEncodedDeckV1(
            true,
            deck.MainDeck.Select((value, index) => VocabularyId(
                value,
                vocabulary,
                $"{path}.main_deck[{index}]")),
            deck.ExtraDeck.Select((value, index) => VocabularyId(
                value,
                vocabulary,
                $"{path}.extra_deck[{index}]")));
    }

    private static OcgForgeEncodedCurrentReferenceV1 EncodeCurrentReference(
        OcgForgeLogicalCurrentReferenceV1 reference) =>
        new(reference.Locator.PublicLocatorOrdinal, reference.CurrentEntityOrdinal);

    private static OcgForgeEncodedCardReferenceV1? EncodeCardReference(
        OcgForgeLogicalPublicCardReferenceV1? reference) =>
        reference.HasValue
            ? new OcgForgeEncodedCardReferenceV1(
                (byte)reference.Value.Kind,
                EncodeCurrentReference(reference.Value.Reference))
            : null;

    private static OcgForgeEncodedChoiceV1 EncodeChoice(
        OcgForgePublicChoiceV1 choice) =>
        new((byte)choice.Kind, choice.Value, choice.ResponseIndex);

    internal static ushort RequestKindCode(string value) =>
        value switch
        {
            "idle_command" => 1,
            "battle_command" => 2,
            "chain" => 3,
            "option" => 4,
            "card_selection" => 5,
            "tribute" => 6,
            "sum" => 7,
            "place" => 8,
            "counter" => 9,
            "ordering" => 10,
            "announcement" => 11,
            "unselect_card" => 12,
            "position" => 13,
            "yes_no" => 14,
            _ => throw new ArgumentException("unsupported request kind")
        };

    internal static ushort ActionKindCode(string value) =>
        value switch
        {
            "idle_command" => 1,
            "battle_command" => 2,
            "chain" => 3,
            "option" => 4,
            "card_selection" => 5,
            "announcement" => 6,
            "place" => 7,
            "position" => 8,
            "yes_no" => 9,
            "pick" => 10,
            "finish" => 11,
            "cancel" => 12,
            "assign_amount" => 13,
            _ => throw new ArgumentException("unsupported action kind")
        };

    internal static string ActionKindToken(ushort code) =>
        code switch
        {
            1 => "idle_command",
            2 => "battle_command",
            3 => "chain",
            4 => "option",
            5 => "card_selection",
            6 => "announcement",
            7 => "place",
            8 => "position",
            9 => "yes_no",
            10 => "pick",
            11 => "finish",
            12 => "cancel",
            13 => "assign_amount",
            _ => throw new ArgumentException("unsupported action kind code")
        };

    internal static byte ContinuationCode(string value) =>
        value switch
        {
            "" => 0,
            "pick" => 1,
            "amount" => 2,
            "finish" => 3,
            "cancel" => 4,
            "bypass" => 5,
            _ => throw new ArgumentException("unsupported continuation")
        };

    internal static string ContinuationToken(byte code) =>
        code switch
        {
            0 => string.Empty,
            1 => "pick",
            2 => "amount",
            3 => "finish",
            4 => "cancel",
            5 => "bypass",
            _ => throw new ArgumentException("unsupported continuation code")
        };
}

internal sealed class I6EUnknownPasscode : Exception
{
    internal I6EUnknownPasscode(string fieldPath)
    {
        FieldPath = fieldPath;
    }

    internal string FieldPath { get; }
}

public sealed class OcgForgeModelInputIdentityResultV1
{
    private OcgForgeModelInputIdentityResultV1(
        bool isSuccess,
        string? identity,
        string? fieldPath)
    {
        IsSuccess = isSuccess;
        Identity = identity;
        FieldPath = fieldPath;
    }

    public bool IsSuccess { get; }

    public string? Identity { get; }

    public string? FieldPath { get; }

    internal static OcgForgeModelInputIdentityResultV1 Success(string identity) =>
        new(true, identity, null);

    internal static OcgForgeModelInputIdentityResultV1 Failure(string fieldPath) =>
        new(false, null, fieldPath);
}

public static class OcgForgeModelInputIdentityV1
{
    public const string SchemaId = "ocgforge.model_input_identity.v1";

    public const string Prefix = "model_input.v1.";

    public static OcgForgeModelInputIdentityResultV1 TryCreate(
        OcgForgeLogicalModelInputV1? logical,
        OcgForgeEncodedModelInputV1? encoded,
        OcgForgeCardVocabularyV1? vocabulary)
    {
        if (logical is null || encoded is null || vocabulary is null)
        {
            return OcgForgeModelInputIdentityResultV1.Failure("input");
        }

        try
        {
            if (!OcgForgeEncodedModelInputV1.SchemaId.Equals(
                    encoded.SchemaIdValue,
                    StringComparison.Ordinal) ||
                encoded.CardVocabularyIdentity != vocabulary.Identity ||
                encoded.PublicObservationDigest != logical.PublicObservationDigest ||
                encoded.PerspectivePlayer != logical.PerspectivePlayer ||
                encoded.DecisionIndex != logical.DecisionIndex ||
                encoded.PublicCandidateDomainDigest != logical.PublicCandidateDomainDigest ||
                encoded.CandidateFeatures.Count != logical.CandidateFeatures.Count ||
                encoded.RoutingKeys.Count != logical.CandidateRouting.Count)
            {
                return OcgForgeModelInputIdentityResultV1.Failure("binding");
            }

            OcgForgeEncodedModelInputResultV1 expectedResult =
                OcgForgeEncodedModelInputBridgeV1.TryCreate(logical, vocabulary);
            if (!expectedResult.IsSuccess || expectedResult.Value is null ||
                !expectedResult.Value.CanonicalBytes.SequenceEqual(encoded.CanonicalBytes))
            {
                return OcgForgeModelInputIdentityResultV1.Failure("binding");
            }

            for (int index = 0; index < logical.CandidateRouting.Count; index++)
            {
                if (logical.CandidateRouting[index].PublicActionKey !=
                    encoded.RoutingKeys[index])
                {
                    return OcgForgeModelInputIdentityResultV1.Failure(
                        $"candidate_routing[{index}]");
                }
            }

            byte[] bytes = CanonicalBytes(logical, encoded);
            return OcgForgeModelInputIdentityResultV1.Success(
                Prefix + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        }
        catch (ArgumentException)
        {
            return OcgForgeModelInputIdentityResultV1.Failure("input");
        }
    }

    public static string Create(
        OcgForgeLogicalModelInputV1 logical,
        OcgForgeEncodedModelInputV1 encoded,
        OcgForgeCardVocabularyV1 vocabulary)
    {
        OcgForgeModelInputIdentityResultV1 result = TryCreate(logical, encoded, vocabulary);
        if (!result.IsSuccess || result.Identity is null)
        {
            throw new ArgumentException("logical, encoded, and vocabulary are detached");
        }

        return result.Identity;
    }

    public static byte[] CanonicalBytes(
        OcgForgeLogicalModelInputV1 logical,
        OcgForgeEncodedModelInputV1 encoded)
    {
        if (logical is null || encoded is null ||
            encoded.PublicObservationDigest != logical.PublicObservationDigest ||
            encoded.PerspectivePlayer != logical.PerspectivePlayer ||
            encoded.DecisionIndex != logical.DecisionIndex ||
            encoded.CandidateFeatures.Count != logical.CandidateFeatures.Count ||
            encoded.RoutingKeys.Count != logical.CandidateRouting.Count)
        {
            throw new ArgumentException("logical and encoded inputs are detached");
        }

        _ = logical.CanonicalBytes;
        _ = encoded.CanonicalBytes;
        OcgForgeI6ECanonicalV1.Writer writer = new();
        writer.String(SchemaId);
        writer.String(SchemaId);
        writer.String(OcgForgeLogicalModelInputV1.SchemaId);
        writer.String(OcgForgeEncodedModelInputV1.SchemaId);
        writer.String(encoded.CardVocabularyIdentity);
        writer.Bytes(logical.CanonicalBytes);
        writer.Bytes(encoded.CanonicalBytes);
        return writer.ToArray();
    }

    public static byte[] CanonicalModelInputIdentityBytes(
        OcgForgeLogicalModelInputV1 logical,
        OcgForgeEncodedModelInputV1 encoded) =>
        CanonicalBytes(logical, encoded);

    public static string ModelInputIdentity(
        OcgForgeLogicalModelInputV1 logical,
        OcgForgeEncodedModelInputV1 encoded,
        OcgForgeCardVocabularyV1 vocabulary) =>
        Create(logical, encoded, vocabulary);
}

internal static class OcgForgeI6EEncodedCodecV1
{
    internal static byte[] CanonicalBytes(OcgForgeEncodedModelInputV1 input)
    {
        Validate(input);
        OcgForgeI6ECanonicalV1.Writer writer = new();
        writer.String(OcgForgeEncodedModelInputV1.SchemaId);
        writer.String(OcgForgeEncodedModelInputV1.SchemaId);
        writer.String(OcgForgeLogicalModelInputV1.SchemaId);
        writer.String(input.CardVocabularyIdentity);
        writer.String(input.PublicObservationDigest);
        writer.U8(input.PerspectivePlayer);
        writer.U64(input.DecisionIndex);
        writer.Count(input.PublicLocatorTable.Count);
        foreach (string locator in input.PublicLocatorTable)
        {
            writer.String(locator);
        }

        writer.OptionalU16(input.PublicObservationContextKindCode);
        writer.OptionalByte(input.PublicObservationContextPlayer);
        writer.Count(input.ObservationContextReferenceOrdinals.Count);
        foreach (uint ordinal in input.ObservationContextReferenceOrdinals)
        {
            writer.U32(ordinal);
        }

        WriteGlobals(writer, input.Globals);
        writer.Count(input.Zones.Count);
        foreach (OcgForgeEncodedZoneV1 zone in input.Zones)
        {
            writer.U8(zone.Player);
            writer.U8(zone.KindCode);
            writer.U32(zone.TotalCount);
            writer.U32(zone.PublicIdentityCount);
            writer.U32(zone.HiddenCount);
            writer.Bool(zone.PlayerObservableOrder);
        }

        writer.Count(input.Entities.Count);
        foreach (OcgForgeEncodedEntityV1 entity in input.Entities)
        {
            writer.U32(entity.PublicLocatorOrdinal);
            writer.Bool(entity.IdentityKnown);
            writer.U32(entity.CardVocabularyId);
            writer.OptionalByte(entity.Owner);
            writer.OptionalByte(entity.Controller);
            writer.U8(entity.ZoneCode);
            writer.OptionalU32(entity.Sequence);
            writer.OptionalU32(entity.OverlaySequence);
            writer.U8(entity.PositionCode);
            writer.Bool(entity.FaceUp);
            writer.Bool(entity.FaceDown);
            WriteEncodedProperties(writer, entity.Printed);
            WriteEncodedProperties(writer, entity.Current);
        }

        writer.Count(input.Relationships.Count);
        foreach (OcgForgeEncodedRelationshipV1 relationship in input.Relationships)
        {
            writer.U8(relationship.KindCode);
            WriteCurrentReference(writer, relationship.Source);
            WriteCurrentReference(writer, relationship.Target);
        }

        writer.U32(input.Chain.Length);
        writer.Count(input.Chain.Links.Count);
        foreach (OcgForgeEncodedChainLinkV1 link in input.Chain.Links)
        {
            writer.U32(link.Index);
            writer.OptionalByte(link.ActivatingPlayer);
            OptionalCurrentReference(writer, link.Source);
            writer.OptionalByte(link.ActivationZoneCode);
            writer.OptionalU64(link.EffectDescription);
            writer.Count(link.Targets.Count);
            foreach (OcgForgeEncodedCurrentReferenceV1 target in link.Targets)
            {
                WriteCurrentReference(writer, target);
            }
        }

        writer.Count(input.VisibleEvents.Count);
        foreach (OcgForgeEncodedVisibleEventV1 @event in input.VisibleEvents)
        {
            writer.U64(@event.EventIndex);
            writer.U8(@event.KindCode);
            writer.OptionalByte(@event.Player);
            writer.OptionalU32(@event.PublicLocatorOrdinal);
            writer.OptionalU32(@event.PublicCardVocabularyId);
            writer.OptionalByte(@event.FromZoneCode);
            writer.OptionalByte(@event.ToZoneCode);
            writer.OptionalU32(@event.Count);
            writer.OptionalI32(@event.Amount);
            writer.OptionalU32(@event.CounterType);
            writer.OptionalU32(@event.Phase);
            writer.OptionalByte(@event.Winner);
            writer.OptionalByte(@event.WinReason);
            writer.OptionalU64(@event.EffectDescription);
            writer.Count(@event.TargetPublicLocatorOrdinals.Count);
            foreach (uint ordinal in @event.TargetPublicLocatorOrdinals)
            {
                writer.U32(ordinal);
            }
        }

        WriteMatchContext(writer, input.MatchContext);
        writer.OptionalString(input.PublicCandidateDomainDigest);
        writer.Count(input.CandidateFeatures.Count);
        foreach (OcgForgeEncodedCandidateV1 candidate in input.CandidateFeatures)
        {
            writer.U16(candidate.ActionKindCode);
            WriteChoice(writer, candidate.Choice);
            WriteCardReference(writer, candidate.SourceReference);
            WriteCardReference(writer, candidate.TargetReference);
            writer.OptionalU32(candidate.Phase);
            writer.OptionalByte(candidate.Position);
            writer.OptionalU32(candidate.SourceIndex);
            writer.OptionalI32(candidate.Amount);
            writer.U8(candidate.ContinuationOperationCode);
            writer.Bool(candidate.SubmitsEngineResponse);
        }

        writer.Count(input.RoutingKeys.Count);
        foreach (string routingKey in input.RoutingKeys)
        {
            writer.String(routingKey);
        }

        return writer.ToArray();
    }

    private static void WriteGlobals(
        OcgForgeI6ECanonicalV1.Writer writer,
        OcgForgeEncodedGlobalsV1 globals)
    {
        writer.U64(globals.DuelFlags);
        writer.Count(globals.LifePoints.Count);
        foreach (uint lifePoints in globals.LifePoints)
        {
            writer.U32(lifePoints);
        }

        writer.OptionalByte(globals.PlayerToAct);
        writer.OptionalByte(globals.TurnPlayer);
        writer.OptionalU32(globals.TurnCount);
        writer.OptionalU32(globals.Phase);
        writer.U32(globals.ChainLength);
        writer.OptionalByte(globals.Winner);
        writer.OptionalByte(globals.WinReason);
        writer.Bool(globals.Terminal);
    }

    private static void WriteEncodedProperties(
        OcgForgeI6ECanonicalV1.Writer writer,
        OcgForgeEncodedCardPropertiesV1? properties)
    {
        writer.Bool(properties is not null);
        if (properties is null)
        {
            return;
        }

        writer.OptionalU32(properties.Type);
        writer.OptionalU32(properties.Attribute);
        writer.OptionalU64(properties.Race);
        writer.OptionalI32(properties.Attack);
        writer.OptionalI32(properties.Defense);
        writer.OptionalI32(properties.BaseAttack);
        writer.OptionalI32(properties.BaseDefense);
        writer.OptionalU32(properties.Level);
        writer.OptionalU32(properties.Rank);
        writer.OptionalU32(properties.LinkRating);
        writer.Count(properties.LinkMarkerCodes.Count);
        foreach (byte marker in properties.LinkMarkerCodes)
        {
            writer.U8(marker);
        }

        writer.OptionalU32(properties.LeftScale);
        writer.OptionalU32(properties.RightScale);
        writer.OptionalU32(properties.StatusFlags);
        writer.Count(properties.Counters.Count);
        foreach (OcgForgeEncodedCounterV1 counter in properties.Counters)
        {
            writer.U32(counter.Type);
            writer.U32(counter.Count);
        }
    }

    private static void WriteCurrentReference(
        OcgForgeI6ECanonicalV1.Writer writer,
        OcgForgeEncodedCurrentReferenceV1 reference)
    {
        writer.U32(reference.PublicLocatorOrdinal);
        writer.OptionalU32(reference.CurrentEntityOrdinal);
    }

    private static void OptionalCurrentReference(
        OcgForgeI6ECanonicalV1.Writer writer,
        OcgForgeEncodedCurrentReferenceV1? reference)
    {
        writer.Bool(reference.HasValue);
        if (reference.HasValue)
        {
            WriteCurrentReference(writer, reference.Value);
        }
    }

    private static void WriteChoice(
        OcgForgeI6ECanonicalV1.Writer writer,
        OcgForgeEncodedChoiceV1? choice)
    {
        writer.Bool(choice.HasValue);
        if (choice.HasValue)
        {
            writer.U8(choice.Value.KindCode);
            writer.U64(choice.Value.Value);
            writer.OptionalU32(choice.Value.ResponseIndex);
        }
    }

    private static void WriteCardReference(
        OcgForgeI6ECanonicalV1.Writer writer,
        OcgForgeEncodedCardReferenceV1? reference)
    {
        writer.Bool(reference.HasValue);
        if (reference.HasValue)
        {
            writer.U8(reference.Value.KindCode);
            WriteCurrentReference(writer, reference.Value.Reference);
        }
    }

    private static void WriteMatchContext(
        OcgForgeI6ECanonicalV1.Writer writer,
        OcgForgeEncodedMatchContextV1 context)
    {
        writer.U8(context.PerspectivePlayer);
        writer.U64(context.DuelFlags);
        writer.Bool(context.OwnDecklistKnown);
        writer.Bool(context.OpponentDecklistKnown);
        WriteDeck(writer, context.OwnDeck);
        WriteDeck(writer, context.OpponentDeck);
    }

    private static void WriteDeck(
        OcgForgeI6ECanonicalV1.Writer writer,
        OcgForgeEncodedDeckV1 deck)
    {
        writer.Bool(deck.Known);
        writer.Count(deck.MainDeck.Count);
        foreach (uint id in deck.MainDeck)
        {
            writer.U32(id);
        }

        writer.Count(deck.ExtraDeck.Count);
        foreach (uint id in deck.ExtraDeck)
        {
            writer.U32(id);
        }
    }

    private static void Validate(OcgForgeEncodedModelInputV1 input)
    {
        if (input is null ||
            input.SchemaIdValue != OcgForgeEncodedModelInputV1.SchemaId ||
            !input.CardVocabularyIdentity.StartsWith(
                OcgForgeCardVocabularyV1.IdentityPrefix,
                StringComparison.Ordinal) ||
            input.CardVocabularyIdentity.Length !=
                OcgForgeCardVocabularyV1.IdentityPrefix.Length + 64 ||
            !OcgForgeI6ECanonicalV1.IsLowerHexDigest(
                input.CardVocabularyIdentity[OcgForgeCardVocabularyV1.IdentityPrefix.Length..]) ||
            !OcgForgeI6ECanonicalV1.IsLowerHexDigest(input.PublicObservationDigest) ||
            input.PerspectivePlayer > 1 ||
            input.CandidateFeatures.Count != input.RoutingKeys.Count ||
            input.CandidateFeatures.Count == 0)
        {
            throw new ArgumentException("invalid encoded model input");
        }

        for (int index = 0; index < input.PublicLocatorTable.Count; index++)
        {
            string locator = input.PublicLocatorTable[index];
            if (!OcgForgeI6ECanonicalV1.IsPublicLocator(locator) ||
                (index > 0 && OcgForgeI6ECanonicalV1.ByteComparer.Instance.Compare(
                    input.PublicLocatorTable[index - 1],
                    locator) >= 0))
            {
                throw new ArgumentException("invalid encoded locator table");
            }
        }

        if (input.PublicObservationContextKindCode is 0 or > 14 ||
            input.PublicObservationContextPlayer is > 1 ||
            input.MatchContext.PerspectivePlayer != input.PerspectivePlayer ||
            input.Globals.PlayerToAct is > 1 ||
            input.Globals.TurnPlayer is > 1 ||
            input.Globals.Winner is > 1 ||
            input.Globals.WinReason is > 1 ||
            input.Globals.ChainLength != input.Chain.Length ||
            input.MatchContext.OwnDeck is null ||
            input.MatchContext.OpponentDeck is null)
        {
            throw new ArgumentException("invalid encoded context");
        }

        foreach (uint ordinal in input.ObservationContextReferenceOrdinals)
        {
            if (ordinal >= input.PublicLocatorTable.Count)
            {
                throw new ArgumentException("invalid encoded context reference");
            }
        }

        for (int index = 0; index < input.Zones.Count; index++)
        {
            OcgForgeEncodedZoneV1 zone = input.Zones[index];
            if (zone.Player > 1 || zone.KindCode > 10 ||
                (index > 0 && CompareZones(input.Zones[index - 1], zone) > 0))
            {
                throw new ArgumentException("invalid encoded zone");
            }
        }

        for (int index = 0; index < input.Entities.Count; index++)
        {
            OcgForgeEncodedEntityV1 entity = input.Entities[index];
            if (entity.PublicLocatorOrdinal >= input.PublicLocatorTable.Count ||
                (index > 0 && OcgForgeI6ECanonicalV1.ByteComparer.Instance.Compare(
                    input.PublicLocatorTable[
                        (int)input.Entities[index - 1].PublicLocatorOrdinal],
                    input.PublicLocatorTable[(int)entity.PublicLocatorOrdinal]) >= 0) ||
                entity.IdentityKnown && entity.CardVocabularyId < 2 ||
                !entity.IdentityKnown && entity.CardVocabularyId !=
                    OcgForgeCardVocabularyV1.UnknownOrRedactedId ||
                entity.ZoneCode > 10 ||
                entity.PositionCode is not (0 or 1 or 2 or 4 or 8) ||
                entity.Owner is > 1 || entity.Controller is > 1 ||
                entity.FaceUp && entity.FaceDown ||
                !entity.IdentityKnown &&
                (entity.Printed is not null || entity.Current is not null))
            {
                throw new ArgumentException("invalid encoded entity");
            }

            ValidateProperties(entity.Printed);
            ValidateProperties(entity.Current);
        }

        foreach (OcgForgeEncodedRelationshipV1 relationship in input.Relationships)
        {
            if (relationship.KindCode > 2)
            {
                throw new ArgumentException("invalid encoded relationship");
            }

            ValidateReference(input, relationship.Source);
            ValidateReference(input, relationship.Target);
        }

        foreach (OcgForgeEncodedChainLinkV1 link in input.Chain.Links)
        {
            if (link.ActivatingPlayer is > 1 || link.ActivationZoneCode is > 10)
            {
                throw new ArgumentException("invalid encoded chain");
            }

            if (link.Source.HasValue)
            {
                ValidateReference(input, link.Source.Value);
            }

            foreach (OcgForgeEncodedCurrentReferenceV1 target in link.Targets)
            {
                ValidateReference(input, target);
            }
        }

        for (int index = 0; index < input.VisibleEvents.Count; index++)
        {
            OcgForgeEncodedVisibleEventV1 @event = input.VisibleEvents[index];
            if (@event.KindCode > 22 || @event.Player is > 1 ||
                @event.FromZoneCode is > 10 || @event.ToZoneCode is > 10 ||
                (index > 0 && @event.EventIndex <= input.VisibleEvents[index - 1].EventIndex))
            {
                throw new ArgumentException("invalid encoded event");
            }

            if (@event.PublicLocatorOrdinal is uint locator &&
                locator >= input.PublicLocatorTable.Count)
            {
                throw new ArgumentException("invalid encoded event locator");
            }

            if (@event.PublicCardVocabularyId is uint vocabularyId && vocabularyId < 2)
            {
                throw new ArgumentException("invalid encoded event vocabulary id");
            }

            foreach (uint target in @event.TargetPublicLocatorOrdinals)
            {
                if (target >= input.PublicLocatorTable.Count)
                {
                    throw new ArgumentException("invalid encoded event target");
                }
            }
        }

        ValidateDeck(input.MatchContext.OwnDeck);
        ValidateDeck(input.MatchContext.OpponentDeck);
        ValidateCandidates(input);
    }

    private static void ValidateProperties(OcgForgeEncodedCardPropertiesV1? properties)
    {
        if (properties is null)
        {
            return;
        }

        for (int index = 0; index < properties.LinkMarkerCodes.Count; index++)
        {
            if (properties.LinkMarkerCodes[index] > 7 ||
                (index > 0 && properties.LinkMarkerCodes[index - 1] >
                    properties.LinkMarkerCodes[index]))
            {
                throw new ArgumentException("invalid encoded link markers");
            }
        }

        for (int index = 1; index < properties.Counters.Count; index++)
        {
            OcgForgeEncodedCounterV1 previous = properties.Counters[index - 1];
            OcgForgeEncodedCounterV1 current = properties.Counters[index];
            if (previous.Type > current.Type ||
                previous.Type == current.Type && previous.Count > current.Count)
            {
                throw new ArgumentException("invalid encoded counters");
            }
        }
    }

    private static void ValidateReference(
        OcgForgeEncodedModelInputV1 input,
        OcgForgeEncodedCurrentReferenceV1 reference)
    {
        if (reference.PublicLocatorOrdinal >= input.PublicLocatorTable.Count ||
            reference.CurrentEntityOrdinal is uint current &&
            (current >= input.Entities.Count ||
             input.Entities[(int)current].PublicLocatorOrdinal !=
                 reference.PublicLocatorOrdinal))
        {
            throw new ArgumentException("invalid encoded current reference");
        }
    }

    private static void ValidateDeck(OcgForgeEncodedDeckV1 deck)
    {
        if (!deck.Known && (deck.MainDeck.Count != 0 || deck.ExtraDeck.Count != 0) ||
            deck.MainDeck.Any(id => id < 2) || deck.ExtraDeck.Any(id => id < 2) ||
            !deck.MainDeck.SequenceEqual(deck.MainDeck.OrderBy(id => id)) ||
            !deck.ExtraDeck.SequenceEqual(deck.ExtraDeck.OrderBy(id => id)))
        {
            throw new ArgumentException("invalid encoded deck");
        }
    }

    private static void ValidateCandidates(OcgForgeEncodedModelInputV1 input)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        for (int index = 0; index < input.CandidateFeatures.Count; index++)
        {
            OcgForgeEncodedCandidateV1 candidate = input.CandidateFeatures[index];
            string key = input.RoutingKeys[index];
            if (!OcgForgePublicActionIdentityV1.IsCanonicalPublicActionKey(key) ||
                !seen.Add(key) ||
                candidate.ActionKindCode is < 1 or > 13 ||
                candidate.ContinuationOperationCode > 5)
            {
                throw new ArgumentException("invalid encoded candidate");
            }

            if (candidate.Choice.HasValue)
            {
                ValidateChoice(candidate.Choice.Value);
            }

            ValidateCardReference(input, candidate.SourceReference);
            ValidateCardReference(input, candidate.TargetReference);

            OcgForgePublicActionDescriptorV1 descriptor = new(
                OcgForgeEncodedModelInputBridgeV1.ActionKindToken(candidate.ActionKindCode),
                candidate.Choice.HasValue
                    ? new OcgForgePublicChoiceV1(
                        (OcgForgePublicChoiceKindV1)candidate.Choice.Value.KindCode,
                        candidate.Choice.Value.Value,
                        candidate.Choice.Value.ResponseIndex)
                    : null,
                ToPublicReference(input, candidate.SourceReference),
                ToPublicReference(input, candidate.TargetReference),
                candidate.Phase,
                candidate.Position,
                candidate.SourceIndex,
                candidate.Amount,
                OcgForgeEncodedModelInputBridgeV1.ContinuationToken(
                    candidate.ContinuationOperationCode));
            OcgForgePublicActionIdentityResultV1 identity =
                OcgForgePublicActionIdentityV1.TryCreate(descriptor);
            if (!identity.IsSuccess || identity.PublicActionKey != key)
            {
                throw new ArgumentException("encoded candidate key mismatch");
            }
        }

        if (input.PublicObservationContextKindCode.HasValue)
        {
            string requestKind = RequestKindToken(input.PublicObservationContextKindCode.Value);
            OcgForgePublicCandidateDomainResultV1 domain =
                OcgForgePublicActionIdentityV1.TryCreateCandidateDomain(
                    requestKind,
                    input.RoutingKeys);
            if (!domain.IsSuccess || domain.Digest != input.PublicCandidateDomainDigest)
            {
                throw new ArgumentException("encoded candidate-domain digest mismatch");
            }
        }
        else if (input.PublicCandidateDomainDigest is not null)
        {
            throw new ArgumentException("unexpected encoded candidate-domain digest");
        }
    }

    private static void ValidateChoice(OcgForgeEncodedChoiceV1 choice)
    {
        bool valid = choice.KindCode switch
        {
            (byte)OcgForgePublicChoiceKindV1.YesNo or
            (byte)OcgForgePublicChoiceKindV1.EffectYesNo =>
                choice.Value <= 1 && !choice.ResponseIndex.HasValue,
            (byte)OcgForgePublicChoiceKindV1.EffectChoice =>
                !choice.ResponseIndex.HasValue,
            (byte)OcgForgePublicChoiceKindV1.OptionValue or
            (byte)OcgForgePublicChoiceKindV1.AnnouncementNumber =>
                choice.ResponseIndex.HasValue,
            _ => false
        };
        if (!valid)
        {
            throw new ArgumentException("invalid encoded choice");
        }
    }

    private static void ValidateCardReference(
        OcgForgeEncodedModelInputV1 input,
        OcgForgeEncodedCardReferenceV1? reference)
    {
        if (!reference.HasValue)
        {
            return;
        }

        if (reference.Value.KindCode > 1)
        {
            throw new ArgumentException("invalid encoded card reference kind");
        }

        ValidateReference(input, reference.Value.Reference);
    }

    private static OcgForgePublicCardReferenceV1? ToPublicReference(
        OcgForgeEncodedModelInputV1 input,
        OcgForgeEncodedCardReferenceV1? reference) =>
        reference.HasValue
            ? new OcgForgePublicCardReferenceV1(
                (OcgForgePublicCardReferenceKindV1)reference.Value.KindCode,
                input.PublicLocatorTable[
                    (int)reference.Value.Reference.PublicLocatorOrdinal])
            : null;

    private static string RequestKindToken(ushort code) =>
        code switch
        {
            1 => "idle_command",
            2 => "battle_command",
            3 => "chain",
            4 => "option",
            5 => "card_selection",
            6 => "tribute",
            7 => "sum",
            8 => "place",
            9 => "counter",
            10 => "ordering",
            11 => "announcement",
            12 => "unselect_card",
            13 => "position",
            14 => "yes_no",
            _ => throw new ArgumentException("invalid request kind code")
        };

    private static int CompareZones(
        OcgForgeEncodedZoneV1 left,
        OcgForgeEncodedZoneV1 right) =>
        Comparer<(byte, byte, uint, uint, uint, bool)>.Default.Compare(
            (left.Player, left.KindCode, left.TotalCount, left.PublicIdentityCount,
                left.HiddenCount, left.PlayerObservableOrder),
            (right.Player, right.KindCode, right.TotalCount, right.PublicIdentityCount,
                right.HiddenCount, right.PlayerObservableOrder));
}

public static class OcgForgeEncodedModelInputCanonicalV1
{
    public static byte[] CanonicalBytes(OcgForgeEncodedModelInputV1 input) =>
        OcgForgeI6EEncodedCodecV1.CanonicalBytes(input);
}
