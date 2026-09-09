using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using OCGForge.Ignis.Gameplay;

namespace OCGForge.Ignis.Model;

public enum OcgForgeLogicalModelProjectionErrorCodeV1 : byte
{
    InvalidPublicObservation = 0,
    PublicSafeStateDecodeFailure = 1,
    EmptyCandidateDomain = 2,
    InvalidPublicActionKey = 3,
    DuplicatePublicActionKey = 4,
    InvalidPublicCandidateDescriptor = 5,
    InvalidPublicReference = 6,
    CandidateDomainDigestFailure = 7,
    LocatorTableFailure = 8,
    InvalidContractBundle = 9,
    InternalFailure = 10
}

public readonly record struct OcgForgeLogicalModelProjectionErrorV1(
    OcgForgeLogicalModelProjectionErrorCodeV1 Code,
    string FieldPath);

public sealed class OcgForgeLogicalModelInputResultV1
{
    private OcgForgeLogicalModelInputResultV1(
        bool isSuccess,
        OcgForgeLogicalModelProjectionErrorV1? error,
        OcgForgeLogicalModelInputV1? value)
    {
        IsSuccess = isSuccess;
        Error = error;
        Value = value;
    }

    public bool IsSuccess { get; }

    public OcgForgeLogicalModelProjectionErrorV1? Error { get; }

    public OcgForgeLogicalModelInputV1? Value { get; }

    internal static OcgForgeLogicalModelInputResultV1 Success(
        OcgForgeLogicalModelInputV1 value) =>
        new(true, null, value);

    internal static OcgForgeLogicalModelInputResultV1 Failure(
        OcgForgeLogicalModelProjectionErrorCodeV1 code,
        string fieldPath) =>
        new(false, new(code, fieldPath), null);
}

public readonly record struct OcgForgeLogicalPublicLocatorV1(
    string Value,
    uint PublicLocatorOrdinal);

public readonly record struct OcgForgeLogicalCurrentReferenceV1(
    OcgForgeLogicalPublicLocatorV1 Locator,
    uint? CurrentEntityOrdinal);

public readonly record struct OcgForgeLogicalHistoricalReferenceV1(
    OcgForgeLogicalPublicLocatorV1 Locator);

public readonly record struct OcgForgeLogicalPublicCardReferenceV1(
    OcgForgePublicCardReferenceKindV1 Kind,
    OcgForgeLogicalCurrentReferenceV1 Reference);

public sealed class OcgForgeLogicalEntityV1
{
    internal OcgForgeLogicalEntityV1(
        PerspectiveSafeEntityV1 card,
        uint publicLocatorOrdinal,
        uint currentEntityOrdinal)
    {
        Card = card ?? throw new ArgumentNullException(nameof(card));
        PublicLocatorOrdinal = publicLocatorOrdinal;
        CurrentEntityOrdinal = currentEntityOrdinal;
    }

    public PerspectiveSafeEntityV1 Card { get; }

    public uint PublicLocatorOrdinal { get; }

    public uint CurrentEntityOrdinal { get; }
}

public sealed class OcgForgeLogicalRelationshipV1
{
    internal OcgForgeLogicalRelationshipV1(
        PerspectiveSafeRelationshipKindV1 kind,
        OcgForgeLogicalCurrentReferenceV1 source,
        OcgForgeLogicalCurrentReferenceV1 target)
    {
        Kind = kind;
        Source = source;
        Target = target;
    }

    public PerspectiveSafeRelationshipKindV1 Kind { get; }

    public OcgForgeLogicalCurrentReferenceV1 Source { get; }

    public OcgForgeLogicalCurrentReferenceV1 Target { get; }
}

public sealed class OcgForgeLogicalChainLinkV1
{
    private readonly OcgForgeLogicalCurrentReferenceV1[] targets;
    private readonly ReadOnlyCollection<OcgForgeLogicalCurrentReferenceV1>
        targetsView;

    internal OcgForgeLogicalChainLinkV1(
        uint index,
        byte? activatingPlayer,
        OcgForgeLogicalCurrentReferenceV1? source,
        PerspectiveSafeSemanticZoneV1? activationZone,
        ulong? effectDescription,
        IEnumerable<OcgForgeLogicalCurrentReferenceV1> targets)
    {
        Index = index;
        ActivatingPlayer = activatingPlayer;
        Source = source;
        ActivationZone = activationZone;
        EffectDescription = effectDescription;
        this.targets = (targets ?? throw new ArgumentNullException(nameof(targets)))
            .ToArray();
        targetsView = Array.AsReadOnly(this.targets);
    }

    public uint Index { get; }

    public byte? ActivatingPlayer { get; }

    public OcgForgeLogicalCurrentReferenceV1? Source { get; }

    public PerspectiveSafeSemanticZoneV1? ActivationZone { get; }

    public ulong? EffectDescription { get; }

    public IReadOnlyList<OcgForgeLogicalCurrentReferenceV1> Targets =>
        targetsView;
}

public sealed class OcgForgeLogicalChainStateV1
{
    private readonly OcgForgeLogicalChainLinkV1[] links;
    private readonly ReadOnlyCollection<OcgForgeLogicalChainLinkV1> linksView;

    internal OcgForgeLogicalChainStateV1(
        uint length,
        IEnumerable<OcgForgeLogicalChainLinkV1> links)
    {
        Length = length;
        this.links = (links ?? throw new ArgumentNullException(nameof(links)))
            .ToArray();
        linksView = Array.AsReadOnly(this.links);
    }

    public uint Length { get; }

    public IReadOnlyList<OcgForgeLogicalChainLinkV1> Links => linksView;
}

public sealed class OcgForgeLogicalVisibleEventV1
{
    private readonly OcgForgeLogicalHistoricalReferenceV1[] targets;
    private readonly ReadOnlyCollection<OcgForgeLogicalHistoricalReferenceV1>
        targetsView;

    internal OcgForgeLogicalVisibleEventV1(
        ulong eventIndex,
        PerspectiveSafeVisibleEventKindV1 kind,
        byte? player,
        OcgForgeLogicalHistoricalReferenceV1? entity,
        uint? publicPasscode,
        PerspectiveSafeSemanticZoneV1? fromZone,
        PerspectiveSafeSemanticZoneV1? toZone,
        uint? count,
        int? amount,
        uint? counterType,
        uint? phase,
        byte? winner,
        byte? winReason,
        ulong? effectDescription,
        IEnumerable<OcgForgeLogicalHistoricalReferenceV1> targets)
    {
        EventIndex = eventIndex;
        Kind = kind;
        Player = player;
        Entity = entity;
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
        this.targets = (targets ?? throw new ArgumentNullException(nameof(targets)))
            .ToArray();
        targetsView = Array.AsReadOnly(this.targets);
    }

    public ulong EventIndex { get; }

    public PerspectiveSafeVisibleEventKindV1 Kind { get; }

    public byte? Player { get; }

    public OcgForgeLogicalHistoricalReferenceV1? Entity { get; }

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

    public IReadOnlyList<OcgForgeLogicalHistoricalReferenceV1> Targets =>
        targetsView;
}

public sealed class OcgForgeLogicalPublicStateV1
{
    private readonly PerspectiveSafeZoneV1[] zones;
    private readonly OcgForgeLogicalEntityV1[] entities;
    private readonly OcgForgeLogicalRelationshipV1[] relationships;
    private readonly OcgForgeLogicalVisibleEventV1[] visibleEvents;
    private readonly ReadOnlyCollection<PerspectiveSafeZoneV1> zonesView;
    private readonly ReadOnlyCollection<OcgForgeLogicalEntityV1> entitiesView;
    private readonly ReadOnlyCollection<OcgForgeLogicalRelationshipV1>
        relationshipsView;
    private readonly ReadOnlyCollection<OcgForgeLogicalVisibleEventV1>
        visibleEventsView;

    internal OcgForgeLogicalPublicStateV1(
        PerspectiveSafeGlobalsV1 globals,
        IEnumerable<PerspectiveSafeZoneV1> zones,
        IEnumerable<OcgForgeLogicalEntityV1> entities,
        IEnumerable<OcgForgeLogicalRelationshipV1> relationships,
        OcgForgeLogicalChainStateV1 chain,
        IEnumerable<OcgForgeLogicalVisibleEventV1> visibleEvents,
        PerspectiveSafeMatchContextV1 matchContext)
    {
        Globals = globals ?? throw new ArgumentNullException(nameof(globals));
        this.zones = (zones ?? throw new ArgumentNullException(nameof(zones)))
            .ToArray();
        this.entities =
            (entities ?? throw new ArgumentNullException(nameof(entities))).ToArray();
        this.relationships =
            (relationships ?? throw new ArgumentNullException(nameof(relationships)))
            .ToArray();
        Chain = chain ?? throw new ArgumentNullException(nameof(chain));
        this.visibleEvents =
            (visibleEvents ?? throw new ArgumentNullException(nameof(visibleEvents)))
            .ToArray();
        MatchContext = matchContext ??
            throw new ArgumentNullException(nameof(matchContext));
        zonesView = Array.AsReadOnly(this.zones);
        entitiesView = Array.AsReadOnly(this.entities);
        relationshipsView = Array.AsReadOnly(this.relationships);
        visibleEventsView = Array.AsReadOnly(this.visibleEvents);
    }

    public PerspectiveSafeGlobalsV1 Globals { get; }

    public IReadOnlyList<PerspectiveSafeZoneV1> Zones => zonesView;

    public IReadOnlyList<OcgForgeLogicalEntityV1> Entities => entitiesView;

    public IReadOnlyList<OcgForgeLogicalRelationshipV1> Relationships =>
        relationshipsView;

    public OcgForgeLogicalChainStateV1 Chain { get; }

    public IReadOnlyList<OcgForgeLogicalVisibleEventV1> VisibleEvents =>
        visibleEventsView;

    public PerspectiveSafeMatchContextV1 MatchContext { get; }
}

public sealed class OcgForgeLogicalCandidateV1
{
    internal OcgForgeLogicalCandidateV1(
        string actionKind,
        OcgForgePublicChoiceV1? choice,
        OcgForgeLogicalPublicCardReferenceV1? sourceReference,
        OcgForgeLogicalPublicCardReferenceV1? targetReference,
        uint? phase,
        byte? position,
        uint? sourceIndex,
        int? amount,
        string continuationOperation,
        bool submitsEngineResponse)
    {
        ActionKind = actionKind ?? throw new ArgumentNullException(nameof(actionKind));
        Choice = choice;
        SourceReference = sourceReference;
        TargetReference = targetReference;
        Phase = phase;
        Position = position;
        SourceIndex = sourceIndex;
        Amount = amount;
        ContinuationOperation = continuationOperation ??
            throw new ArgumentNullException(nameof(continuationOperation));
        SubmitsEngineResponse = submitsEngineResponse;
    }

    public string ActionKind { get; }

    public OcgForgePublicChoiceV1? Choice { get; }

    public OcgForgeLogicalPublicCardReferenceV1? SourceReference { get; }

    public OcgForgeLogicalPublicCardReferenceV1? TargetReference { get; }

    public uint? Phase { get; }

    public byte? Position { get; }

    public uint? SourceIndex { get; }

    public int? Amount { get; }

    public string ContinuationOperation { get; }

    public bool SubmitsEngineResponse { get; }
}

public sealed class OcgForgeLogicalCandidateRoutingV1
{
    internal OcgForgeLogicalCandidateRoutingV1(string publicActionKey)
    {
        PublicActionKey = publicActionKey ??
            throw new ArgumentNullException(nameof(publicActionKey));
    }

    public string PublicActionKey { get; }
}

public sealed class OcgForgeLogicalModelInputV1
{
    private readonly OcgForgeLogicalPublicLocatorV1[] referencedPublicEntities;
    private readonly OcgForgeLogicalPublicLocatorV1[] publicLocatorTable;
    private readonly OcgForgeLogicalCandidateRoutingV1[] candidateRouting;
    private readonly OcgForgeLogicalCandidateV1[] candidateFeatures;
    private readonly ReadOnlyCollection<OcgForgeLogicalPublicLocatorV1>
        referencedPublicEntitiesView;
    private readonly ReadOnlyCollection<OcgForgeLogicalPublicLocatorV1>
        publicLocatorTableView;
    private readonly ReadOnlyCollection<OcgForgeLogicalCandidateRoutingV1>
        candidateRoutingView;
    private readonly ReadOnlyCollection<OcgForgeLogicalCandidateV1>
        candidateFeaturesView;

    internal OcgForgeLogicalModelInputV1(
        string schemaId,
        string publicObservationDigest,
        string? publicCandidateDomainDigest,
        byte perspectivePlayer,
        ulong decisionIndex,
        string? publicObservationContextKind,
        byte? publicObservationContextPlayer,
        IEnumerable<OcgForgeLogicalPublicLocatorV1> referencedPublicEntities,
        IEnumerable<OcgForgeLogicalPublicLocatorV1> publicLocatorTable,
        OcgForgeLogicalPublicStateV1 publicSafeState,
        IEnumerable<OcgForgeLogicalCandidateRoutingV1> candidateRouting,
        IEnumerable<OcgForgeLogicalCandidateV1> candidateFeatures)
    {
        SchemaIdValue = schemaId ?? throw new ArgumentNullException(nameof(schemaId));
        PublicObservationDigest = publicObservationDigest ??
            throw new ArgumentNullException(nameof(publicObservationDigest));
        PublicCandidateDomainDigest = publicCandidateDomainDigest;
        PerspectivePlayer = perspectivePlayer;
        DecisionIndex = decisionIndex;
        PublicObservationContextKind = publicObservationContextKind;
        PublicObservationContextPlayer = publicObservationContextPlayer;
        this.referencedPublicEntities = (referencedPublicEntities ??
            throw new ArgumentNullException(nameof(referencedPublicEntities))).ToArray();
        this.publicLocatorTable = (publicLocatorTable ??
            throw new ArgumentNullException(nameof(publicLocatorTable))).ToArray();
        PublicSafeState = publicSafeState ??
            throw new ArgumentNullException(nameof(publicSafeState));
        this.candidateRouting = (candidateRouting ??
            throw new ArgumentNullException(nameof(candidateRouting))).ToArray();
        this.candidateFeatures = (candidateFeatures ??
            throw new ArgumentNullException(nameof(candidateFeatures))).ToArray();
        referencedPublicEntitiesView = Array.AsReadOnly(this.referencedPublicEntities);
        publicLocatorTableView = Array.AsReadOnly(this.publicLocatorTable);
        candidateRoutingView = Array.AsReadOnly(this.candidateRouting);
        candidateFeaturesView = Array.AsReadOnly(this.candidateFeatures);
    }

    public const string SchemaId = "ocgforge.model_logical_input.v1";

    public string SchemaIdValue { get; }

    public string PublicObservationDigest { get; }

    public string? PublicCandidateDomainDigest { get; }

    public byte PerspectivePlayer { get; }

    public ulong DecisionIndex { get; }

    public string? PublicObservationContextKind { get; }

    public byte? PublicObservationContextPlayer { get; }

    public IReadOnlyList<OcgForgeLogicalPublicLocatorV1> ReferencedPublicEntities =>
        referencedPublicEntitiesView;

    public IReadOnlyList<OcgForgeLogicalPublicLocatorV1> PublicLocatorTable =>
        publicLocatorTableView;

    public OcgForgeLogicalPublicStateV1 PublicSafeState { get; }

    public IReadOnlyList<OcgForgeLogicalCandidateRoutingV1> CandidateRouting =>
        candidateRoutingView;

    public IReadOnlyList<OcgForgeLogicalCandidateV1> CandidateFeatures =>
        candidateFeaturesView;

    public int CandidateCount => candidateFeatures.Length;

    public byte[] CanonicalBytes => OcgForgeI6ECanonicalV1.CanonicalLogicalBytes(this);
}

public static class OcgForgeLogicalModelInputBridgeV1
{
    private const string FrozenSourceCommit =
        "f929de0b4d4157327dba003067d2e21e42f7ad75";
    private const string FrozenP5AcceptanceHead =
        "3c99e86c487361fc4e0f5f12678b4867e59232b7";
    private const string FrozenBundleDomain =
        "ocgforge-ignis.i6.model-contract-bundle.v1";

    private static readonly string[] RequiredContractIds =
    {
        OcgForgeLogicalModelInputV1.SchemaId,
        OcgForgeEncodedModelInputV1.SchemaId,
        OcgForgeCardVocabularyV1.SchemaId,
        OcgForgeModelInputIdentityV1.SchemaId
    };

    public static OcgForgeLogicalModelInputResultV1 TryCreate(
        OcgForgeModelContractBundleV1? bundle,
        OcgForgePublicDecisionContextV1? acceptedDecision)
    {
        if (!IsExactBundle(bundle))
        {
            return OcgForgeLogicalModelInputResultV1.Failure(
                OcgForgeLogicalModelProjectionErrorCodeV1.InvalidContractBundle,
                "bundle");
        }

        if (acceptedDecision is null)
        {
            return OcgForgeLogicalModelInputResultV1.Failure(
                OcgForgeLogicalModelProjectionErrorCodeV1.InvalidPublicObservation,
                "accepted_decision");
        }

        try
        {
            return OcgForgeLogicalModelInputResultV1.Success(Build(acceptedDecision));
        }
        catch (I6EProjectionFailure failure)
        {
            return OcgForgeLogicalModelInputResultV1.Failure(
                failure.Code,
                failure.FieldPath);
        }
        catch (EncoderFallbackException)
        {
            return OcgForgeLogicalModelInputResultV1.Failure(
                OcgForgeLogicalModelProjectionErrorCodeV1.InvalidPublicObservation,
                "public_observation");
        }
        catch (ArgumentException)
        {
            return OcgForgeLogicalModelInputResultV1.Failure(
                OcgForgeLogicalModelProjectionErrorCodeV1.InternalFailure,
                "logical_input");
        }
        catch (OverflowException)
        {
            return OcgForgeLogicalModelInputResultV1.Failure(
                OcgForgeLogicalModelProjectionErrorCodeV1.LocatorTableFailure,
                "logical_input");
        }
    }

    private static bool IsExactBundle(OcgForgeModelContractBundleV1? bundle)
    {
        if (bundle is null ||
            bundle.IdentityDomain != FrozenBundleDomain ||
            bundle.OcgForgeSourceCommit != FrozenSourceCommit ||
            bundle.P5AcceptanceExecutionHead != FrozenP5AcceptanceHead)
        {
            return false;
        }

        return RequiredContractIds.All(required =>
            bundle.Registry.Any(entry =>
                entry.ContractId == required &&
                entry.OwnerRepositoryId == "ocgforge"));
    }

    private static OcgForgeLogicalModelInputV1 Build(
        OcgForgePublicDecisionContextV1 acceptedDecision)
    {
        PerspectiveSafeFrameV1 frame = acceptedDecision.Frame;
        if (frame.MatchContext.PerspectivePlayer != acceptedDecision.PlayerToAct ||
            acceptedDecision.PlayerToAct > 1 ||
            (frame.Globals.PlayerToAct.HasValue &&
             frame.Globals.PlayerToAct.Value != acceptedDecision.PlayerToAct))
        {
            throw Failure(
                OcgForgeLogicalModelProjectionErrorCodeV1.InvalidPublicObservation,
                "decision_boundary");
        }

        if (acceptedDecision.Candidates.Count == 0)
        {
            throw Failure(
                OcgForgeLogicalModelProjectionErrorCodeV1.EmptyCandidateDomain,
                "candidates");
        }

        string? contextKind = acceptedDecision.PublicDecisionContext.Kind;
        if (contextKind != acceptedDecision.RequestKind ||
            !OcgForgeI6ECanonicalV1.IsKnownRequestKind(contextKind))
        {
            throw Failure(
                OcgForgeLogicalModelProjectionErrorCodeV1.InvalidPublicObservation,
                "decision_context.kind");
        }

        if (acceptedDecision.PublicDecisionContext.Player > 1 ||
            acceptedDecision.PublicDecisionContext.Player != acceptedDecision.PlayerToAct)
        {
            throw Failure(
                OcgForgeLogicalModelProjectionErrorCodeV1.InvalidPublicObservation,
                "decision_context.player");
        }

        List<string> locatorValues = CollectLocators(frame, acceptedDecision);
        locatorValues.Sort(OcgForgeI6ECanonicalV1.ByteComparer.Instance);
        locatorValues = locatorValues
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (locatorValues.Count > int.MaxValue)
        {
            throw Failure(
                OcgForgeLogicalModelProjectionErrorCodeV1.LocatorTableFailure,
                "public_locator_table");
        }

        OcgForgeLocatorTable table = new(locatorValues);
        OcgForgeLogicalPublicLocatorV1[] publicLocatorTable = locatorValues
            .Select((value, index) => new OcgForgeLogicalPublicLocatorV1(
                value,
                checked((uint)index)))
            .ToArray();

        OcgForgeLogicalPublicLocatorV1[] referencedEntities =
            acceptedDecision.ReferencedEntities
                .OrderBy(value => value, OcgForgeI6ECanonicalV1.ByteComparer.Instance)
                .Select(value => table.PublicLocator(value, "decision_context.referenced_entities"))
                .ToArray();
        if (referencedEntities.Zip(
                referencedEntities.Skip(1),
                (previous, current) =>
                    OcgForgeI6ECanonicalV1.ByteComparer.Instance.Compare(
                        previous.Value,
                        current.Value) >= 0)
            .Any(value => value))
        {
            throw Failure(
                OcgForgeLogicalModelProjectionErrorCodeV1.InvalidPublicReference,
                "decision_context.referenced_entities");
        }

        OcgForgeLogicalPublicStateV1 state = BuildState(frame, table);
        List<OcgForgeLogicalCandidateV1> candidates = new(acceptedDecision.Candidates.Count);
        List<OcgForgeLogicalCandidateRoutingV1> routing =
            new(acceptedDecision.Candidates.Count);
        HashSet<string> keys = new(StringComparer.Ordinal);

        foreach (OcgForgePublicCandidateV1 source in acceptedDecision.Candidates)
        {
            OcgForgePublicActionIdentityResultV1 identity =
                OcgForgePublicActionIdentityV1.TryCreate(source.Descriptor);
            if (!identity.IsSuccess || identity.PublicActionKey != source.PublicActionKey)
            {
                throw Failure(
                    OcgForgeLogicalModelProjectionErrorCodeV1.InvalidPublicActionKey,
                    "candidates.public_action_key");
            }

            if (!keys.Add(source.PublicActionKey))
            {
                throw Failure(
                    OcgForgeLogicalModelProjectionErrorCodeV1.DuplicatePublicActionKey,
                    "candidates.public_action_key");
            }

            OcgForgePublicActionDescriptorV1 descriptor = source.Descriptor;
            OcgForgeLogicalPublicCardReferenceV1? sourceReference =
                MapReference(descriptor.SourceReference, frame, table, "candidate.source_reference");
            OcgForgeLogicalPublicCardReferenceV1? targetReference =
                MapReference(descriptor.TargetReference, frame, table, "candidate.target_reference");

            candidates.Add(new OcgForgeLogicalCandidateV1(
                descriptor.ActionKind,
                descriptor.Choice,
                sourceReference,
                targetReference,
                descriptor.Phase,
                descriptor.Position,
                descriptor.SourceIndex,
                descriptor.Amount,
                descriptor.ContinuationOperation,
                DeriveSubmitsEngineResponse(descriptor.ContinuationOperation)));
            routing.Add(new OcgForgeLogicalCandidateRoutingV1(source.PublicActionKey));
        }

        OcgForgePublicCandidateDomainResultV1 domain =
            OcgForgePublicActionIdentityV1.TryCreateCandidateDomain(
                acceptedDecision.RequestKind,
                routing.Select(value => value.PublicActionKey).ToArray());
        if (!domain.IsSuccess ||
            domain.Digest is null ||
            domain.Digest != acceptedDecision.PublicCandidateDomainDigest)
        {
            throw Failure(
                OcgForgeLogicalModelProjectionErrorCodeV1.CandidateDomainDigestFailure,
                "public_candidate_domain_digest");
        }

        string observationDigest =
            OcgForgeI6ECanonicalV1.PublicObservationDigest(frame, acceptedDecision);
        return new OcgForgeLogicalModelInputV1(
            OcgForgeLogicalModelInputV1.SchemaId,
            observationDigest,
            acceptedDecision.PublicCandidateDomainDigest,
            acceptedDecision.PlayerToAct,
            acceptedDecision.DecisionIndex,
            contextKind,
            acceptedDecision.PublicDecisionContext.Player,
            referencedEntities,
            publicLocatorTable,
            state,
            routing,
            candidates);
    }

    // I6D deliberately exposes only the public descriptor.  In the accepted
    // I6 public mapping, the continuation token is the complete public
    // transition class: pick/amount are intermediate actions and the empty,
    // finish, cancel, and bypass tokens are terminal/atomic actions.  The
    // native P5 boolean is therefore recovered from that already accepted
    // descriptor without crossing private response state.
    private static bool DeriveSubmitsEngineResponse(string continuationOperation) =>
        continuationOperation is not ("pick" or "amount");

    private static List<string> CollectLocators(
        PerspectiveSafeFrameV1 frame,
        OcgForgePublicDecisionContextV1 decision)
    {
        List<string> values = new();
        void Add(string? value, string path)
        {
            if (!OcgForgeI6ECanonicalV1.IsPublicLocator(value))
            {
                throw Failure(
                    OcgForgeLogicalModelProjectionErrorCodeV1.InvalidPublicReference,
                    path);
            }

            values.Add(value!);
        }

        foreach (string value in decision.ReferencedEntities)
        {
            Add(value, "decision_context.referenced_entities");
        }

        foreach (PerspectiveSafeEntityV1 entity in frame.Entities)
        {
            Add(entity.Locator, "entities.locator");
        }

        foreach (PerspectiveSafeRelationshipV1 relationship in frame.Relationships)
        {
            Add(relationship.Source, "relationships.source");
            Add(relationship.Target, "relationships.target");
        }

        foreach (PerspectiveSafeChainLinkV1 link in frame.Chain.Links)
        {
            if (link.Source is not null)
            {
                Add(link.Source, "chain.source");
            }

            foreach (string target in link.Targets)
            {
                Add(target, "chain.targets");
            }
        }

        foreach (PerspectiveSafeVisibleEventV1 @event in frame.VisibleEvents)
        {
            if (@event.EntityLocator is not null)
            {
                Add(@event.EntityLocator, "visible_events.entity");
            }

            foreach (string target in @event.Targets)
            {
                Add(target, "visible_events.targets");
            }
        }

        foreach (OcgForgePublicCandidateV1 candidate in decision.Candidates)
        {
            AddReference(candidate.Descriptor.SourceReference, Add, "candidate.source_reference");
            AddReference(candidate.Descriptor.TargetReference, Add, "candidate.target_reference");
        }

        return values;
    }

    private static void AddReference(
        OcgForgePublicCardReferenceV1? reference,
        Action<string?, string> add,
        string path)
    {
        if (reference.HasValue)
        {
            add(reference.Value.ObservationLocator, path);
        }
    }

    private static OcgForgeLogicalPublicStateV1 BuildState(
        PerspectiveSafeFrameV1 frame,
        OcgForgeLocatorTable table)
    {
        OcgForgeLogicalEntityV1[] entities = frame.Entities
            .Select((entity, index) => new OcgForgeLogicalEntityV1(
                entity,
                table.Ordinal(entity.Locator, "entities.locator"),
                checked((uint)index)))
            .ToArray();

        OcgForgeLogicalRelationshipV1[] relationships = frame.Relationships
            .Select((relationship, index) => new OcgForgeLogicalRelationshipV1(
                relationship.Kind,
                CurrentReference(table, frame, relationship.Source, $"relationships[{index}].source"),
                CurrentReference(table, frame, relationship.Target, $"relationships[{index}].target")))
            .ToArray();

        OcgForgeLogicalChainLinkV1[] chainLinks = frame.Chain.Links
            .Select((link, index) => new OcgForgeLogicalChainLinkV1(
                link.Index,
                link.ActivatingPlayer,
                link.Source is null
                    ? null
                    : CurrentReference(table, frame, link.Source, $"chain[{index}].source"),
                link.ActivationZone,
                link.EffectDescription,
                link.Targets.Select((target, targetIndex) => CurrentReference(
                    table,
                    frame,
                    target,
                    $"chain[{index}].targets[{targetIndex}]"))))
            .ToArray();

        OcgForgeLogicalVisibleEventV1[] events = frame.VisibleEvents
            .Select((@event, index) => new OcgForgeLogicalVisibleEventV1(
                @event.EventIndex,
                @event.Kind,
                @event.Player,
                @event.EntityLocator is null
                    ? null
                    : HistoricalReference(
                        table,
                        @event.EntityLocator,
                        $"visible_events[{index}].entity"),
                @event.PublicPasscode,
                @event.FromZone,
                @event.ToZone,
                @event.Count,
                @event.Amount,
                @event.CounterType,
                @event.Phase,
                @event.Winner,
                @event.WinReason,
                @event.EffectDescription,
                @event.Targets.Select((target, targetIndex) => HistoricalReference(
                    table,
                    target,
                    $"visible_events[{index}].targets[{targetIndex}]"))))
            .ToArray();

        return new OcgForgeLogicalPublicStateV1(
            frame.Globals,
            frame.Zones,
            entities,
            relationships,
            new OcgForgeLogicalChainStateV1(frame.Chain.Length, chainLinks),
            events,
            frame.MatchContext);
    }

    private static OcgForgeLogicalPublicCardReferenceV1? MapReference(
        OcgForgePublicCardReferenceV1? reference,
        PerspectiveSafeFrameV1 frame,
        OcgForgeLocatorTable table,
        string path)
    {
        if (!reference.HasValue)
        {
            return null;
        }

        if (reference.Value.Kind is not
            (OcgForgePublicCardReferenceKindV1.VisibleCard or
             OcgForgePublicCardReferenceKindV1.RedactedSlot))
        {
            throw Failure(
                OcgForgeLogicalModelProjectionErrorCodeV1.InvalidPublicReference,
                path);
        }

        return new OcgForgeLogicalPublicCardReferenceV1(
            reference.Value.Kind,
            CurrentReference(table, frame, reference.Value.ObservationLocator, path));
    }

    private static OcgForgeLogicalCurrentReferenceV1 CurrentReference(
        OcgForgeLocatorTable table,
        PerspectiveSafeFrameV1 frame,
        string locator,
        string path) =>
        new(
            table.PublicLocator(locator, path),
            CurrentEntityOrdinal(frame, locator, path));

    private static uint? CurrentEntityOrdinal(
        PerspectiveSafeFrameV1 frame,
        string locator,
        string path)
    {
        uint? result = null;
        for (int index = 0; index < frame.Entities.Count; index++)
        {
            if (frame.Entities[index].Locator != locator)
            {
                continue;
            }

            if (result.HasValue)
            {
                throw Failure(
                    OcgForgeLogicalModelProjectionErrorCodeV1.LocatorTableFailure,
                    path);
            }

            result = checked((uint)index);
        }

        return result;
    }

    private static OcgForgeLogicalHistoricalReferenceV1 HistoricalReference(
        OcgForgeLocatorTable table,
        string locator,
        string path) =>
        new(table.PublicLocator(locator, path));

    private static I6EProjectionFailure Failure(
        OcgForgeLogicalModelProjectionErrorCodeV1 code,
        string path) =>
        new(code, path);

    private sealed class OcgForgeLocatorTable
    {
        private readonly IReadOnlyList<string> values;

        internal OcgForgeLocatorTable(IReadOnlyList<string> values)
        {
            this.values = values;
        }

        internal uint Ordinal(string value, string path)
        {
            int index = Array.BinarySearch(
                values.ToArray(),
                value,
                OcgForgeI6ECanonicalV1.ByteComparer.Instance);
            if (index < 0)
            {
                throw Failure(
                    OcgForgeLogicalModelProjectionErrorCodeV1.LocatorTableFailure,
                    path);
            }

            return checked((uint)index);
        }

        internal OcgForgeLogicalPublicLocatorV1 PublicLocator(
            string value,
            string path) =>
            new(value, Ordinal(value, path));
    }

    private sealed class I6EProjectionFailure : Exception
    {
        internal I6EProjectionFailure(
            OcgForgeLogicalModelProjectionErrorCodeV1 code,
            string fieldPath)
        {
            Code = code;
            FieldPath = fieldPath;
        }

        internal OcgForgeLogicalModelProjectionErrorCodeV1 Code { get; }

        internal string FieldPath { get; }
    }
}

internal static class OcgForgeI6ECanonicalV1
{
    internal sealed class ByteComparer : IComparer<string>
    {
        internal static readonly ByteComparer Instance = new();

        public int Compare(string? left, string? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            byte[] leftBytes = StrictUtf8.GetBytes(left);
            byte[] rightBytes = StrictUtf8.GetBytes(right);
            int length = Math.Min(leftBytes.Length, rightBytes.Length);
            for (int index = 0; index < length; index++)
            {
                int comparison = leftBytes[index].CompareTo(rightBytes[index]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return leftBytes.Length.CompareTo(rightBytes.Length);
        }
    }

    private static readonly Encoding StrictUtf8 =
        new UTF8Encoding(false, true);

    internal static bool IsLowerToken(string? value) =>
        !string.IsNullOrEmpty(value) && value.All(character =>
            character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');

    internal static bool IsKnownRequestKind(string? value) =>
        value is "idle_command" or "battle_command" or "chain" or "option" or
            "card_selection" or "tribute" or "sum" or "place" or "counter" or
            "ordering" or "announcement" or "unselect_card" or "position" or
            "yes_no";

    internal static bool IsPublicLocator(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        try
        {
            byte[] bytes = StrictUtf8.GetBytes(value);
            return bytes.All(valueByte => valueByte >= 0x20 && valueByte != 0x7f);
        }
        catch (EncoderFallbackException)
        {
            return false;
        }
    }

    internal static bool IsLowerHexDigest(string? value) =>
        value is not null && value.Length == 64 && value.All(character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');

    internal static byte[] CanonicalLogicalBytes(
        OcgForgeLogicalModelInputV1 input)
    {
        ValidateLogical(input);
        Writer writer = new();
        writer.String(OcgForgeLogicalModelInputV1.SchemaId);
        writer.String(OcgForgeLogicalModelInputV1.SchemaId);
        writer.String(input.PublicObservationDigest);
        writer.U8(input.PerspectivePlayer);
        writer.U64(input.DecisionIndex);
        writer.OptionalString(input.PublicObservationContextKind);
        writer.OptionalByte(input.PublicObservationContextPlayer);
        writer.Count(input.ReferencedPublicEntities.Count);
        foreach (OcgForgeLogicalPublicLocatorV1 reference in
                 input.ReferencedPublicEntities)
        {
            writer.String(reference.Value);
        }

        writer.Count(input.PublicLocatorTable.Count);
        foreach (OcgForgeLogicalPublicLocatorV1 locator in input.PublicLocatorTable)
        {
            writer.String(locator.Value);
        }

        Writer safeStateWriter = new();
        WriteLogicalSafeState(safeStateWriter, input.PublicSafeState);
        writer.Bytes(safeStateWriter.ToArray());

        writer.Count(input.CandidateFeatures.Count);
        for (int index = 0; index < input.CandidateFeatures.Count; index++)
        {
            WriteLogicalCandidate(
                writer,
                input.CandidateFeatures[index],
                input.CandidateRouting[index].PublicActionKey);
        }

        writer.OptionalString(input.PublicCandidateDomainDigest);
        return writer.ToArray();
    }

    internal static string PublicObservationDigest(
        PerspectiveSafeFrameV1 frame,
        OcgForgePublicDecisionContextV1 decision)
    {
        Writer safeState = new();
        WriteSafeFrame(safeState, frame);
        Writer observation = new();
        const string schema = "ocgforge.public_environment_observation.v1";
        observation.String(schema);
        observation.String(schema);
        observation.U8(frame.MatchContext.PerspectivePlayer);
        observation.U64(decision.DecisionIndex);
        observation.Bytes(safeState.ToArray());
        observation.OptionalString(decision.RequestKind);
        observation.OptionalByte(decision.PlayerToAct);
        string[] references = decision.ReferencedEntities
            .OrderBy(value => value, ByteComparer.Instance)
            .ToArray();
        observation.Count(references.Length);
        foreach (string reference in references)
        {
            observation.String(reference);
        }

        return Convert.ToHexString(SHA256.HashData(observation.ToArray()))
            .ToLowerInvariant();
    }

    internal static byte[] CanonicalSafeFrameBytes(PerspectiveSafeFrameV1 frame)
    {
        Writer writer = new();
        WriteSafeFrame(writer, frame);
        return writer.ToArray();
    }

    private static void WriteSafeFrame(
        Writer writer,
        PerspectiveSafeFrameV1 frame)
    {
        const string schema = "ocgforge.public_safe_state.v1";
        writer.String(schema);
        writer.String(schema);
        WriteGlobals(writer, frame.Globals);

        PerspectiveSafeZoneV1[] zones = frame.Zones
            .OrderBy(value => value.Player)
            .ThenBy(value => (byte)value.Kind)
            .ThenBy(value => value.TotalCount)
            .ThenBy(value => value.PublicIdentityCount)
            .ThenBy(value => value.HiddenCount)
            .ThenBy(value => value.PlayerObservableOrder)
            .ToArray();
        writer.Count(zones.Length);
        foreach (PerspectiveSafeZoneV1 zone in zones)
        {
            writer.U8(zone.Player);
            writer.U8((byte)zone.Kind);
            writer.U32(zone.TotalCount);
            writer.U32(zone.PublicIdentityCount);
            writer.U32(zone.HiddenCount);
            writer.Bool(zone.PlayerObservableOrder);
        }

        PerspectiveSafeEntityV1[] entities = frame.Entities
            .OrderBy(value => value.Locator, ByteComparer.Instance)
            .ToArray();
        writer.Count(entities.Length);
        foreach (PerspectiveSafeEntityV1 entity in entities)
        {
            WriteEntity(writer, entity);
        }

        PerspectiveSafeRelationshipV1[] relationships = frame.Relationships
            .OrderBy(value => (byte)value.Kind)
            .ThenBy(value => value.Source, ByteComparer.Instance)
            .ThenBy(value => value.Target, ByteComparer.Instance)
            .ToArray();
        writer.Count(relationships.Length);
        foreach (PerspectiveSafeRelationshipV1 relationship in relationships)
        {
            writer.U8((byte)relationship.Kind);
            writer.String(relationship.Source);
            writer.String(relationship.Target);
        }

        writer.U32(frame.Chain.Length);
        writer.Count(frame.Chain.Links.Count);
        foreach (PerspectiveSafeChainLinkV1 link in frame.Chain.Links)
        {
            writer.U32(link.Index);
            writer.OptionalByte(link.ActivatingPlayer);
            writer.OptionalString(link.Source);
            writer.OptionalByte(link.ActivationZone is null
                ? null
                : (byte?)link.ActivationZone.Value);
            writer.OptionalU64(link.EffectDescription);
            string[] targets = link.Targets
                .OrderBy(value => value, ByteComparer.Instance)
                .ToArray();
            writer.Count(targets.Length);
            foreach (string target in targets)
            {
                writer.String(target);
            }
        }

        PerspectiveSafeVisibleEventV1[] events = frame.VisibleEvents
            .OrderBy(value => value.EventIndex)
            .ToArray();
        writer.Count(events.Length);
        foreach (PerspectiveSafeVisibleEventV1 @event in events)
        {
            writer.U64(@event.EventIndex);
            writer.U8((byte)@event.Kind);
            writer.OptionalByte(@event.Player);
            writer.OptionalString(@event.EntityLocator);
            writer.OptionalU32(@event.PublicPasscode);
            writer.OptionalByte(@event.FromZone is null
                ? null
                : (byte?)@event.FromZone.Value);
            writer.OptionalByte(@event.ToZone is null
                ? null
                : (byte?)@event.ToZone.Value);
            writer.OptionalU32(@event.Count);
            writer.OptionalI32(@event.Amount);
            writer.OptionalU32(@event.CounterType);
            writer.OptionalU32(@event.Phase);
            writer.OptionalByte(@event.Winner);
            writer.OptionalByte(@event.WinReason);
            writer.OptionalU64(@event.EffectDescription);
            string[] targets = @event.Targets
                .OrderBy(value => value, ByteComparer.Instance)
                .ToArray();
            writer.Count(targets.Length);
            foreach (string target in targets)
            {
                writer.String(target);
            }
        }

        WriteMatchContext(writer, frame.MatchContext);
    }

    private static void WriteLogicalSafeState(
        Writer writer,
        OcgForgeLogicalPublicStateV1 state)
    {
        const string schema = "ocgforge.public_safe_state.v1";
        writer.String(schema);
        writer.String(schema);
        WriteGlobals(writer, state.Globals);

        PerspectiveSafeZoneV1[] zones = state.Zones
            .OrderBy(value => value.Player)
            .ThenBy(value => (byte)value.Kind)
            .ThenBy(value => value.TotalCount)
            .ThenBy(value => value.PublicIdentityCount)
            .ThenBy(value => value.HiddenCount)
            .ThenBy(value => value.PlayerObservableOrder)
            .ToArray();
        writer.Count(zones.Length);
        foreach (PerspectiveSafeZoneV1 zone in zones)
        {
            writer.U8(zone.Player);
            writer.U8((byte)zone.Kind);
            writer.U32(zone.TotalCount);
            writer.U32(zone.PublicIdentityCount);
            writer.U32(zone.HiddenCount);
            writer.Bool(zone.PlayerObservableOrder);
        }

        OcgForgeLogicalEntityV1[] entities = state.Entities
            .OrderBy(value => value.Card.Locator, ByteComparer.Instance)
            .ToArray();
        writer.Count(entities.Length);
        foreach (OcgForgeLogicalEntityV1 entity in entities)
        {
            WriteEntity(writer, entity.Card);
        }

        OcgForgeLogicalRelationshipV1[] relationships = state.Relationships
            .OrderBy(value => (byte)value.Kind)
            .ThenBy(value => value.Source.Locator.Value, ByteComparer.Instance)
            .ThenBy(value => value.Target.Locator.Value, ByteComparer.Instance)
            .ToArray();
        writer.Count(relationships.Length);
        foreach (OcgForgeLogicalRelationshipV1 relationship in relationships)
        {
            writer.U8((byte)relationship.Kind);
            writer.String(relationship.Source.Locator.Value);
            writer.String(relationship.Target.Locator.Value);
        }

        writer.U32(state.Chain.Length);
        writer.Count(state.Chain.Links.Count);
        foreach (OcgForgeLogicalChainLinkV1 link in state.Chain.Links)
        {
            writer.U32(link.Index);
            writer.OptionalByte(link.ActivatingPlayer);
            writer.OptionalString(link.Source?.Locator.Value);
            writer.OptionalByte(link.ActivationZone is null
                ? null
                : (byte?)link.ActivationZone.Value);
            writer.OptionalU64(link.EffectDescription);
            OcgForgeLogicalCurrentReferenceV1[] targets = link.Targets
                .OrderBy(value => value.Locator.Value, ByteComparer.Instance)
                .ToArray();
            writer.Count(targets.Length);
            foreach (OcgForgeLogicalCurrentReferenceV1 target in targets)
            {
                writer.String(target.Locator.Value);
            }
        }

        OcgForgeLogicalVisibleEventV1[] events = state.VisibleEvents
            .OrderBy(value => value.EventIndex)
            .ToArray();
        writer.Count(events.Length);
        foreach (OcgForgeLogicalVisibleEventV1 @event in events)
        {
            writer.U64(@event.EventIndex);
            writer.U8((byte)@event.Kind);
            writer.OptionalByte(@event.Player);
            writer.OptionalString(@event.Entity?.Locator.Value);
            writer.OptionalU32(@event.PublicPasscode);
            writer.OptionalByte(@event.FromZone is null
                ? null
                : (byte?)@event.FromZone.Value);
            writer.OptionalByte(@event.ToZone is null
                ? null
                : (byte?)@event.ToZone.Value);
            writer.OptionalU32(@event.Count);
            writer.OptionalI32(@event.Amount);
            writer.OptionalU32(@event.CounterType);
            writer.OptionalU32(@event.Phase);
            writer.OptionalByte(@event.Winner);
            writer.OptionalByte(@event.WinReason);
            writer.OptionalU64(@event.EffectDescription);
            OcgForgeLogicalHistoricalReferenceV1[] targets = @event.Targets
                .OrderBy(value => value.Locator.Value, ByteComparer.Instance)
                .ToArray();
            writer.Count(targets.Length);
            foreach (OcgForgeLogicalHistoricalReferenceV1 target in targets)
            {
                writer.String(target.Locator.Value);
            }
        }

        WriteMatchContext(writer, state.MatchContext);
    }

    private static void WriteEntity(Writer writer, PerspectiveSafeEntityV1 entity)
    {
        writer.String(entity.Locator);
        writer.Bool(entity.IdentityKnown);
        writer.OptionalU32(entity.Passcode);
        writer.OptionalByte(entity.Owner);
        writer.OptionalByte(entity.Controller);
        writer.U8((byte)entity.Zone);
        writer.OptionalU32(entity.Sequence);
        writer.OptionalU32(entity.OverlaySequence);
        writer.U8((byte)entity.Position);
        writer.Bool(entity.FaceUp);
        writer.Bool(entity.FaceDown);
        WriteProperties(writer, entity.Printed);
        WriteProperties(writer, entity.Current);
    }

    private static void WriteProperties(
        Writer writer,
        PerspectiveSafeCardPropertiesV1? properties)
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
        PerspectiveSafeLinkMarkerV1[] markers = properties.LinkMarkers
            .OrderBy(value => (byte)value)
            .ToArray();
        writer.Count(markers.Length);
        foreach (PerspectiveSafeLinkMarkerV1 marker in markers)
        {
            writer.U8((byte)marker);
        }

        writer.OptionalU32(properties.LeftScale);
        writer.OptionalU32(properties.RightScale);
        writer.OptionalU32(properties.StatusFlags);
        PerspectiveSafeCounterV1[] counters = properties.Counters
            .OrderBy(value => value.Type)
            .ThenBy(value => value.Count)
            .ToArray();
        writer.Count(counters.Length);
        foreach (PerspectiveSafeCounterV1 counter in counters)
        {
            writer.U32(counter.Type);
            writer.U32(counter.Count);
        }
    }

    private static void WriteGlobals(Writer writer, PerspectiveSafeGlobalsV1 globals)
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

    private static void WriteMatchContext(
        Writer writer,
        PerspectiveSafeMatchContextV1 context)
    {
        writer.U8(context.PerspectivePlayer);
        writer.U64(context.DuelFlags);
        writer.Bool(context.Knowledge.OwnDecklistKnown);
        writer.Bool(context.Knowledge.OpponentDecklistKnown);
        WriteDeck(writer, context.OwnDeck);
        WriteDeck(writer, context.OpponentDeck);
    }

    private static void WriteDeck(Writer writer, PerspectiveSafeDeckV1 deck)
    {
        writer.Bool(deck.Known);
        uint[] mainDeck = deck.MainDeck.OrderBy(value => value).ToArray();
        uint[] extraDeck = deck.ExtraDeck.OrderBy(value => value).ToArray();
        writer.Count(mainDeck.Length);
        foreach (uint passcode in mainDeck)
        {
            writer.U32(passcode);
        }

        writer.Count(extraDeck.Length);
        foreach (uint passcode in extraDeck)
        {
            writer.U32(passcode);
        }
    }

    private static void WriteLogicalCandidate(
        Writer writer,
        OcgForgeLogicalCandidateV1 candidate,
        string publicActionKey)
    {
        writer.String(candidate.ActionKind);
        writer.String(publicActionKey);
        writer.Bool(candidate.Choice.HasValue);
        if (candidate.Choice.HasValue)
        {
            writer.U8((byte)candidate.Choice.Value.Kind);
            writer.U64(candidate.Choice.Value.Value);
            writer.OptionalU32(candidate.Choice.Value.ResponseIndex);
        }

        WriteLogicalCardReference(writer, candidate.SourceReference);
        WriteLogicalCardReference(writer, candidate.TargetReference);
        writer.OptionalU32(candidate.Phase);
        writer.OptionalByte(candidate.Position);
        writer.OptionalU32(candidate.SourceIndex);
        writer.OptionalI32(candidate.Amount);
        writer.String(candidate.ContinuationOperation);
        writer.Bool(candidate.SubmitsEngineResponse);
    }

    private static void WriteLogicalCardReference(
        Writer writer,
        OcgForgeLogicalPublicCardReferenceV1? reference)
    {
        writer.Bool(reference.HasValue);
        if (reference.HasValue)
        {
            writer.U8((byte)reference.Value.Kind);
            writer.String(reference.Value.Reference.Locator.Value);
        }
    }

    private static void ValidateLogical(OcgForgeLogicalModelInputV1 input)
    {
        if (input is null ||
            input.SchemaIdValue != OcgForgeLogicalModelInputV1.SchemaId ||
            !IsLowerHexDigest(input.PublicObservationDigest) ||
            input.PerspectivePlayer > 1 ||
            input.CandidateFeatures.Count == 0 ||
            input.CandidateFeatures.Count != input.CandidateRouting.Count)
        {
            throw new ArgumentException("invalid logical model input");
        }

        if (input.PublicCandidateDomainDigest is not null &&
            !IsLowerHexDigest(input.PublicCandidateDomainDigest))
        {
            throw new ArgumentException("invalid candidate-domain digest");
        }

        if (input.PublicObservationContextKind is not null &&
            !IsKnownRequestKind(input.PublicObservationContextKind))
        {
            throw new ArgumentException("invalid observation context kind");
        }

        if (input.PublicObservationContextPlayer is > 1)
        {
            throw new ArgumentException("invalid observation context player");
        }

        for (int index = 0; index < input.PublicLocatorTable.Count; index++)
        {
            OcgForgeLogicalPublicLocatorV1 locator = input.PublicLocatorTable[index];
            if (!IsPublicLocator(locator.Value) || locator.PublicLocatorOrdinal != index ||
                (index > 0 && ByteComparer.Instance.Compare(
                    input.PublicLocatorTable[index - 1].Value,
                    locator.Value) >= 0))
            {
                throw new ArgumentException("invalid locator table");
            }
        }

        ValidateReferencedLocators(input);
        ValidateState(input);
        ValidateCandidates(input);
    }

    private static void ValidateReferencedLocators(
        OcgForgeLogicalModelInputV1 input)
    {
        for (int index = 0; index < input.ReferencedPublicEntities.Count; index++)
        {
            OcgForgeLogicalPublicLocatorV1 reference =
                input.ReferencedPublicEntities[index];
            if (!IsPublicLocator(reference.Value) ||
                reference.PublicLocatorOrdinal >= input.PublicLocatorTable.Count ||
                input.PublicLocatorTable[(int)reference.PublicLocatorOrdinal].Value !=
                reference.Value ||
                (index > 0 && ByteComparer.Instance.Compare(
                    input.ReferencedPublicEntities[index - 1].Value,
                    reference.Value) >= 0))
            {
                throw new ArgumentException("invalid referenced locator");
            }
        }
    }

    private static void ValidateState(OcgForgeLogicalModelInputV1 input)
    {
        OcgForgeLogicalPublicStateV1 state = input.PublicSafeState;
        if (state.MatchContext.PerspectivePlayer != input.PerspectivePlayer ||
            state.Globals.DuelFlags != state.MatchContext.DuelFlags ||
            state.Globals.ChainLength != state.Chain.Length ||
            state.Globals.LifePoints.Count != 2 ||
            state.Globals.PlayerToAct is > 1 || state.Globals.TurnPlayer is > 1 ||
            state.MatchContext.OwnDeck is null || state.MatchContext.OpponentDeck is null ||
            (!state.MatchContext.OwnDeck.Known &&
             (state.MatchContext.OwnDeck.MainDeck.Count != 0 ||
              state.MatchContext.OwnDeck.ExtraDeck.Count != 0)) ||
            (!state.MatchContext.OpponentDeck.Known &&
             (state.MatchContext.OpponentDeck.MainDeck.Count != 0 ||
              state.MatchContext.OpponentDeck.ExtraDeck.Count != 0)))
        {
            throw new ArgumentException("invalid logical state");
        }

        for (int index = 0; index < state.Zones.Count; index++)
        {
            PerspectiveSafeZoneV1 zone = state.Zones[index];
            if (zone.Player > 1 || zone.Kind > PerspectiveSafeSemanticZoneV1.Overlay ||
                (index > 0 && CompareZones(state.Zones[index - 1], zone) > 0))
            {
                throw new ArgumentException("invalid logical zones");
            }
        }

        for (int index = 0; index < state.Entities.Count; index++)
        {
            OcgForgeLogicalEntityV1 entity = state.Entities[index];
            PerspectiveSafeEntityV1 card = entity.Card;
            if (!IsPublicLocator(card.Locator) ||
                entity.CurrentEntityOrdinal != index ||
                entity.PublicLocatorOrdinal >= input.PublicLocatorTable.Count ||
                input.PublicLocatorTable[(int)entity.PublicLocatorOrdinal].Value != card.Locator ||
                (index > 0 && ByteComparer.Instance.Compare(
                    state.Entities[index - 1].Card.Locator,
                    card.Locator) >= 0) ||
                card.Owner is > 1 || card.Controller is > 1 ||
                card.Zone > PerspectiveSafeSemanticZoneV1.Overlay ||
                card.Position is not (PerspectiveSafePositionV1.Unknown or
                    PerspectiveSafePositionV1.FaceUpAttack or
                    PerspectiveSafePositionV1.FaceDownAttack or
                    PerspectiveSafePositionV1.FaceUpDefense or
                    PerspectiveSafePositionV1.FaceDownDefense) ||
                card.FaceUp && card.FaceDown ||
                !card.IdentityKnown &&
                (card.Passcode.HasValue || card.Printed is not null || card.Current is not null))
            {
                throw new ArgumentException("invalid logical entity");
            }

            ValidateProperties(card.Printed);
            ValidateProperties(card.Current);
        }

        foreach (OcgForgeLogicalRelationshipV1 relationship in state.Relationships)
        {
            if (relationship.Kind > PerspectiveSafeRelationshipKindV1.Target)
            {
                throw new ArgumentException("invalid logical relationship");
            }

            ValidateCurrentReference(input, state, relationship.Source);
            ValidateCurrentReference(input, state, relationship.Target);
        }

        for (int index = 0; index < state.Relationships.Count; index++)
        {
            if (index > 0 && CompareRelationships(
                    state.Relationships[index - 1],
                    state.Relationships[index]) > 0)
            {
                throw new ArgumentException("logical relationships are not canonical");
            }
        }

        foreach (OcgForgeLogicalChainLinkV1 link in state.Chain.Links)
        {
            if (link.ActivatingPlayer is > 1 ||
                link.ActivationZone is > PerspectiveSafeSemanticZoneV1.Overlay)
            {
                throw new ArgumentException("invalid logical chain");
            }

            if (link.Source.HasValue)
            {
                ValidateCurrentReference(input, state, link.Source.Value);
            }

            for (int index = 0; index < link.Targets.Count; index++)
            {
                ValidateCurrentReference(input, state, link.Targets[index]);
                if (index > 0 && ByteComparer.Instance.Compare(
                    link.Targets[index - 1].Locator.Value,
                    link.Targets[index].Locator.Value) > 0)
                {
                    throw new ArgumentException("logical chain targets are not canonical");
                }
            }
        }

        for (int index = 0; index < state.VisibleEvents.Count; index++)
        {
            OcgForgeLogicalVisibleEventV1 @event = state.VisibleEvents[index];
            if (@event.Player is > 1 ||
                @event.FromZone is > PerspectiveSafeSemanticZoneV1.Overlay ||
                @event.ToZone is > PerspectiveSafeSemanticZoneV1.Overlay ||
                (index > 0 && @event.EventIndex <= state.VisibleEvents[index - 1].EventIndex))
            {
                throw new ArgumentException("invalid logical visible event");
            }

            if (@event.Entity.HasValue)
            {
                ValidateHistoricalReference(input, @event.Entity.Value);
            }

            for (int target = 0; target < @event.Targets.Count; target++)
            {
                ValidateHistoricalReference(input, @event.Targets[target]);
                if (target > 0 && ByteComparer.Instance.Compare(
                    @event.Targets[target - 1].Locator.Value,
                    @event.Targets[target].Locator.Value) > 0)
                {
                    throw new ArgumentException("logical event targets are not canonical");
                }
            }
        }

        ValidateSortedDeck(state.MatchContext.OwnDeck);
        ValidateSortedDeck(state.MatchContext.OpponentDeck);
    }

    private static void ValidateProperties(PerspectiveSafeCardPropertiesV1? properties)
    {
        if (properties is null)
        {
            return;
        }

        for (int index = 1; index < properties.LinkMarkers.Count; index++)
        {
            if (properties.LinkMarkers[index - 1] > properties.LinkMarkers[index])
            {
                throw new ArgumentException("link markers are not canonical");
            }
        }

        for (int index = 1; index < properties.Counters.Count; index++)
        {
            PerspectiveSafeCounterV1 previous = properties.Counters[index - 1];
            PerspectiveSafeCounterV1 current = properties.Counters[index];
            if (previous.Type > current.Type ||
                previous.Type == current.Type && previous.Count > current.Count)
            {
                throw new ArgumentException("counters are not canonical");
            }
        }
    }

    private static void ValidateSortedDeck(PerspectiveSafeDeckV1 deck)
    {
        if (!deck.MainDeck.SequenceEqual(deck.MainDeck.OrderBy(value => value)) ||
            !deck.ExtraDeck.SequenceEqual(deck.ExtraDeck.OrderBy(value => value)))
        {
            throw new ArgumentException("deck is not canonical");
        }
    }

    private static void ValidateCurrentReference(
        OcgForgeLogicalModelInputV1 input,
        OcgForgeLogicalPublicStateV1 state,
        OcgForgeLogicalCurrentReferenceV1 reference)
    {
        if (!IsReferenceInTable(input, reference.Locator) ||
            reference.CurrentEntityOrdinal is > int.MaxValue)
        {
            throw new ArgumentException("invalid current reference");
        }

        if (reference.CurrentEntityOrdinal.HasValue)
        {
            int index = checked((int)reference.CurrentEntityOrdinal.Value);
            if (index >= state.Entities.Count ||
                state.Entities[index].Card.Locator != reference.Locator.Value)
            {
                throw new ArgumentException("current reference entity mismatch");
            }
        }
    }

    private static void ValidateHistoricalReference(
        OcgForgeLogicalModelInputV1 input,
        OcgForgeLogicalHistoricalReferenceV1 reference)
    {
        if (!IsReferenceInTable(input, reference.Locator))
        {
            throw new ArgumentException("invalid historical reference");
        }
    }

    private static bool IsReferenceInTable(
        OcgForgeLogicalModelInputV1 input,
        OcgForgeLogicalPublicLocatorV1 locator) =>
        locator.PublicLocatorOrdinal < input.PublicLocatorTable.Count &&
        input.PublicLocatorTable[(int)locator.PublicLocatorOrdinal].Value == locator.Value &&
        IsPublicLocator(locator.Value);

    private static void ValidateCandidates(OcgForgeLogicalModelInputV1 input)
    {
        HashSet<string> keys = new(StringComparer.Ordinal);
        for (int index = 0; index < input.CandidateFeatures.Count; index++)
        {
            OcgForgeLogicalCandidateV1 candidate = input.CandidateFeatures[index];
            string key = input.CandidateRouting[index].PublicActionKey;
            if (!OcgForgePublicActionIdentityV1.IsCanonicalPublicActionKey(key) ||
                !keys.Add(key) ||
                !IsLowerToken(candidate.ActionKind) ||
                !IsValidContinuation(candidate.ContinuationOperation))
            {
                throw new ArgumentException("invalid logical candidate");
            }

            OcgForgePublicActionDescriptorV1 descriptor = new(
                candidate.ActionKind,
                candidate.Choice,
                ToPublicReference(candidate.SourceReference),
                ToPublicReference(candidate.TargetReference),
                candidate.Phase,
                candidate.Position,
                candidate.SourceIndex,
                candidate.Amount,
                candidate.ContinuationOperation);
            OcgForgePublicActionIdentityResultV1 identity =
                OcgForgePublicActionIdentityV1.TryCreate(descriptor);
            if (!identity.IsSuccess || identity.PublicActionKey != key)
            {
                throw new ArgumentException("logical candidate key mismatch");
            }

            ValidateLogicalCardReference(input, input.PublicSafeState, candidate.SourceReference);
            ValidateLogicalCardReference(input, input.PublicSafeState, candidate.TargetReference);
        }

        if (input.PublicObservationContextKind is null)
        {
            if (input.PublicCandidateDomainDigest is not null)
            {
                throw new ArgumentException("unexpected candidate-domain digest");
            }
        }
        else
        {
            OcgForgePublicCandidateDomainResultV1 domain =
                OcgForgePublicActionIdentityV1.TryCreateCandidateDomain(
                    input.PublicObservationContextKind,
                    input.CandidateRouting.Select(value => value.PublicActionKey).ToArray());
            if (!domain.IsSuccess || domain.Digest != input.PublicCandidateDomainDigest)
            {
                throw new ArgumentException("candidate-domain digest mismatch");
            }
        }
    }

    private static void ValidateLogicalCardReference(
        OcgForgeLogicalModelInputV1 input,
        OcgForgeLogicalPublicStateV1 state,
        OcgForgeLogicalPublicCardReferenceV1? reference)
    {
        if (!reference.HasValue ||
            reference.Value.Kind is not
                (OcgForgePublicCardReferenceKindV1.VisibleCard or
                 OcgForgePublicCardReferenceKindV1.RedactedSlot))
        {
            if (reference.HasValue)
            {
                throw new ArgumentException("invalid logical card reference");
            }

            return;
        }

        ValidateCurrentReference(input, state, reference.Value.Reference);
    }

    private static OcgForgePublicCardReferenceV1? ToPublicReference(
        OcgForgeLogicalPublicCardReferenceV1? reference) =>
        reference.HasValue
            ? new OcgForgePublicCardReferenceV1(
                reference.Value.Kind,
                reference.Value.Reference.Locator.Value)
            : null;

    private static bool IsValidContinuation(string value) =>
        value is "" or "pick" or "amount" or "finish" or "cancel" or "bypass";

    private static int CompareZones(
        PerspectiveSafeZoneV1 left,
        PerspectiveSafeZoneV1 right) =>
        Comparer<(byte, byte, uint, uint, uint, bool)>.Default.Compare(
            (left.Player, (byte)left.Kind, left.TotalCount, left.PublicIdentityCount,
                left.HiddenCount, left.PlayerObservableOrder),
            (right.Player, (byte)right.Kind, right.TotalCount, right.PublicIdentityCount,
                right.HiddenCount, right.PlayerObservableOrder));

    private static int CompareRelationships(
        OcgForgeLogicalRelationshipV1 left,
        OcgForgeLogicalRelationshipV1 right)
    {
        int kind = ((byte)left.Kind).CompareTo((byte)right.Kind);
        if (kind != 0)
        {
            return kind;
        }

        int source = ByteComparer.Instance.Compare(
            left.Source.Locator.Value,
            right.Source.Locator.Value);
        return source != 0
            ? source
            : ByteComparer.Instance.Compare(
                left.Target.Locator.Value,
                right.Target.Locator.Value);
    }

    internal sealed class Writer
    {
        private readonly List<byte> bytes = new();

        internal void U8(byte value) => bytes.Add(value);

        internal void Bool(bool value) => U8(value ? (byte)1 : (byte)0);

        internal void U16(ushort value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
            bytes.AddRange(buffer.ToArray());
        }

        internal void U32(uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
            bytes.AddRange(buffer.ToArray());
        }

        internal void U64(ulong value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
            bytes.AddRange(buffer.ToArray());
        }

        internal void I32(int value) => U32(unchecked((uint)value));

        internal void Count(int count)
        {
            if (count < 0)
            {
                throw new OverflowException();
            }

            U32(checked((uint)count));
        }

        internal void String(string value)
        {
            byte[] encoded = StrictUtf8.GetBytes(value ?? throw new ArgumentNullException(nameof(value)));
            Count(encoded.Length);
            bytes.AddRange(encoded);
        }

        internal void Bytes(byte[] value)
        {
            Count(value.Length);
            bytes.AddRange(value);
        }

        internal void OptionalString(string? value)
        {
            Bool(value is not null);
            if (value is not null)
            {
                String(value);
            }
        }

        internal void OptionalByte(byte? value)
        {
            Bool(value.HasValue);
            if (value.HasValue)
            {
                U8(value.Value);
            }
        }

        internal void OptionalU16(ushort? value)
        {
            Bool(value.HasValue);
            if (value.HasValue)
            {
                U16(value.Value);
            }
        }

        internal void OptionalU32(uint? value)
        {
            Bool(value.HasValue);
            if (value.HasValue)
            {
                U32(value.Value);
            }
        }

        internal void OptionalU64(ulong? value)
        {
            Bool(value.HasValue);
            if (value.HasValue)
            {
                U64(value.Value);
            }
        }

        internal void OptionalI32(int? value)
        {
            Bool(value.HasValue);
            if (value.HasValue)
            {
                I32(value.Value);
            }
        }

        internal byte[] ToArray() => bytes.ToArray();
    }
}

public static class OcgForgeLogicalModelInputCanonicalV1
{
    public static byte[] CanonicalBytes(OcgForgeLogicalModelInputV1 input) =>
        OcgForgeI6ECanonicalV1.CanonicalLogicalBytes(input);
}
