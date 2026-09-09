using System.Collections.ObjectModel;

namespace OCGForge.Ignis.Model;

public readonly record struct OcgForgeCandidateOptionalPresenceV1(
    byte Choice,
    byte SourceReference,
    byte TargetReference,
    byte Phase,
    byte Position,
    byte SourceIndex,
    byte Amount);

public sealed class OcgForgeModelBatchSampleHeaderV1
{
    public OcgForgeModelBatchSampleHeaderV1(
        string schemaId,
        string cardVocabularyIdentity,
        string publicObservationDigest,
        byte perspectivePlayer,
        ulong decisionIndex,
        ushort? publicObservationContextKindCode,
        byte? publicObservationContextPlayer,
        OcgForgeEncodedGlobalsV1 globals,
        uint chainLength,
        OcgForgeEncodedMatchContextV1 matchContext,
        string? publicCandidateDomainDigest)
    {
        SchemaId = schemaId ?? throw new ArgumentNullException(nameof(schemaId));
        CardVocabularyIdentity = cardVocabularyIdentity ??
            throw new ArgumentNullException(nameof(cardVocabularyIdentity));
        PublicObservationDigest = publicObservationDigest ??
            throw new ArgumentNullException(nameof(publicObservationDigest));
        PerspectivePlayer = perspectivePlayer;
        DecisionIndex = decisionIndex;
        PublicObservationContextKindCode = publicObservationContextKindCode;
        PublicObservationContextPlayer = publicObservationContextPlayer;
        Globals = globals ?? throw new ArgumentNullException(nameof(globals));
        ChainLength = chainLength;
        MatchContext = matchContext ??
            throw new ArgumentNullException(nameof(matchContext));
        PublicCandidateDomainDigest = publicCandidateDomainDigest;
    }

    public string SchemaId { get; }

    public string CardVocabularyIdentity { get; }

    public string PublicObservationDigest { get; }

    public byte PerspectivePlayer { get; }

    public ulong DecisionIndex { get; }

    public ushort? PublicObservationContextKindCode { get; }

    public byte? PublicObservationContextPlayer { get; }

    public OcgForgeEncodedGlobalsV1 Globals { get; }

    public uint ChainLength { get; }

    public OcgForgeEncodedMatchContextV1 MatchContext { get; }

    public string? PublicCandidateDomainDigest { get; }
}

public sealed class OcgForgeRaggedModelBatchV1
{
    public OcgForgeRaggedModelBatchV1(
        string schemaId,
        uint batchSize,
        IEnumerable<OcgForgeModelBatchSampleHeaderV1> samples,
        IEnumerable<ulong> candidateOffsets,
        IEnumerable<ulong> zoneOffsets,
        IEnumerable<ulong> entityOffsets,
        IEnumerable<ulong> relationshipOffsets,
        IEnumerable<ulong> chainLinkOffsets,
        IEnumerable<ulong> visibleEventOffsets,
        IEnumerable<ulong> decisionContextReferenceOffsets,
        IEnumerable<ulong> publicLocatorTokenOffsets,
        IEnumerable<ulong> lifePointOffsets,
        IEnumerable<ulong> ownDeckPasscodeOffsets,
        IEnumerable<ulong> opponentDeckPasscodeOffsets,
        IEnumerable<ulong> ownExtraDeckPasscodeOffsets,
        IEnumerable<ulong> opponentExtraDeckPasscodeOffsets,
        IEnumerable<OcgForgeEncodedCandidateV1> candidateRows,
        IEnumerable<OcgForgeCandidateOptionalPresenceV1> candidateOptionalPresenceMasks,
        IEnumerable<string> candidateRoutingKeys,
        IEnumerable<OcgForgeEncodedZoneV1> zones,
        IEnumerable<OcgForgeEncodedEntityV1> entities,
        IEnumerable<OcgForgeEncodedRelationshipV1> relationships,
        IEnumerable<OcgForgeEncodedChainLinkV1> chainLinks,
        IEnumerable<OcgForgeEncodedVisibleEventV1> visibleEvents,
        IEnumerable<uint> decisionContextReferenceOrdinals,
        IEnumerable<string> publicLocatorTokens,
        IEnumerable<uint> lifePoints,
        IEnumerable<uint> ownDeckPasscodeIds,
        IEnumerable<uint> opponentDeckPasscodeIds,
        IEnumerable<uint> ownExtraDeckPasscodeIds,
        IEnumerable<uint> opponentExtraDeckPasscodeIds)
    {
        SchemaId = schemaId ?? throw new ArgumentNullException(nameof(schemaId));
        BatchSize = batchSize;
        this.samples = Copy(samples, nameof(samples));
        this.candidateOffsets = Copy(candidateOffsets, nameof(candidateOffsets));
        this.zoneOffsets = Copy(zoneOffsets, nameof(zoneOffsets));
        this.entityOffsets = Copy(entityOffsets, nameof(entityOffsets));
        this.relationshipOffsets = Copy(relationshipOffsets, nameof(relationshipOffsets));
        this.chainLinkOffsets = Copy(chainLinkOffsets, nameof(chainLinkOffsets));
        this.visibleEventOffsets = Copy(visibleEventOffsets, nameof(visibleEventOffsets));
        this.decisionContextReferenceOffsets = Copy(
            decisionContextReferenceOffsets,
            nameof(decisionContextReferenceOffsets));
        this.publicLocatorTokenOffsets = Copy(
            publicLocatorTokenOffsets,
            nameof(publicLocatorTokenOffsets));
        this.lifePointOffsets = Copy(lifePointOffsets, nameof(lifePointOffsets));
        this.ownDeckPasscodeOffsets = Copy(
            ownDeckPasscodeOffsets,
            nameof(ownDeckPasscodeOffsets));
        this.opponentDeckPasscodeOffsets = Copy(
            opponentDeckPasscodeOffsets,
            nameof(opponentDeckPasscodeOffsets));
        this.ownExtraDeckPasscodeOffsets = Copy(
            ownExtraDeckPasscodeOffsets,
            nameof(ownExtraDeckPasscodeOffsets));
        this.opponentExtraDeckPasscodeOffsets = Copy(
            opponentExtraDeckPasscodeOffsets,
            nameof(opponentExtraDeckPasscodeOffsets));
        this.candidateRows = Copy(candidateRows, nameof(candidateRows));
        this.candidateOptionalPresenceMasks = Copy(
            candidateOptionalPresenceMasks,
            nameof(candidateOptionalPresenceMasks));
        this.candidateRoutingKeys = Copy(candidateRoutingKeys, nameof(candidateRoutingKeys));
        this.zones = Copy(zones, nameof(zones));
        this.entities = Copy(entities, nameof(entities));
        this.relationships = Copy(relationships, nameof(relationships));
        this.chainLinks = Copy(chainLinks, nameof(chainLinks));
        this.visibleEvents = Copy(visibleEvents, nameof(visibleEvents));
        this.decisionContextReferenceOrdinals = Copy(
            decisionContextReferenceOrdinals,
            nameof(decisionContextReferenceOrdinals));
        this.publicLocatorTokens = Copy(publicLocatorTokens, nameof(publicLocatorTokens));
        this.lifePoints = Copy(lifePoints, nameof(lifePoints));
        this.ownDeckPasscodeIds = Copy(ownDeckPasscodeIds, nameof(ownDeckPasscodeIds));
        this.opponentDeckPasscodeIds = Copy(
            opponentDeckPasscodeIds,
            nameof(opponentDeckPasscodeIds));
        this.ownExtraDeckPasscodeIds = Copy(
            ownExtraDeckPasscodeIds,
            nameof(ownExtraDeckPasscodeIds));
        this.opponentExtraDeckPasscodeIds = Copy(
            opponentExtraDeckPasscodeIds,
            nameof(opponentExtraDeckPasscodeIds));

        Samples = Array.AsReadOnly(this.samples);
        CandidateOffsets = Array.AsReadOnly(this.candidateOffsets);
        ZoneOffsets = Array.AsReadOnly(this.zoneOffsets);
        EntityOffsets = Array.AsReadOnly(this.entityOffsets);
        RelationshipOffsets = Array.AsReadOnly(this.relationshipOffsets);
        ChainLinkOffsets = Array.AsReadOnly(this.chainLinkOffsets);
        VisibleEventOffsets = Array.AsReadOnly(this.visibleEventOffsets);
        DecisionContextReferenceOffsets = Array.AsReadOnly(this.decisionContextReferenceOffsets);
        PublicLocatorTokenOffsets = Array.AsReadOnly(this.publicLocatorTokenOffsets);
        LifePointOffsets = Array.AsReadOnly(this.lifePointOffsets);
        OwnDeckPasscodeOffsets = Array.AsReadOnly(this.ownDeckPasscodeOffsets);
        OpponentDeckPasscodeOffsets = Array.AsReadOnly(this.opponentDeckPasscodeOffsets);
        OwnExtraDeckPasscodeOffsets = Array.AsReadOnly(this.ownExtraDeckPasscodeOffsets);
        OpponentExtraDeckPasscodeOffsets = Array.AsReadOnly(
            this.opponentExtraDeckPasscodeOffsets);
        CandidateRows = Array.AsReadOnly(this.candidateRows);
        CandidateOptionalPresenceMasks = Array.AsReadOnly(
            this.candidateOptionalPresenceMasks);
        CandidateRoutingKeys = Array.AsReadOnly(this.candidateRoutingKeys);
        Zones = Array.AsReadOnly(this.zones);
        Entities = Array.AsReadOnly(this.entities);
        Relationships = Array.AsReadOnly(this.relationships);
        ChainLinks = Array.AsReadOnly(this.chainLinks);
        VisibleEvents = Array.AsReadOnly(this.visibleEvents);
        DecisionContextReferenceOrdinals = Array.AsReadOnly(
            this.decisionContextReferenceOrdinals);
        PublicLocatorTokens = Array.AsReadOnly(this.publicLocatorTokens);
        LifePoints = Array.AsReadOnly(this.lifePoints);
        OwnDeckPasscodeIds = Array.AsReadOnly(this.ownDeckPasscodeIds);
        OpponentDeckPasscodeIds = Array.AsReadOnly(this.opponentDeckPasscodeIds);
        OwnExtraDeckPasscodeIds = Array.AsReadOnly(this.ownExtraDeckPasscodeIds);
        OpponentExtraDeckPasscodeIds = Array.AsReadOnly(
            this.opponentExtraDeckPasscodeIds);
    }

    private readonly OcgForgeModelBatchSampleHeaderV1[] samples;
    private readonly ulong[] candidateOffsets;
    private readonly ulong[] zoneOffsets;
    private readonly ulong[] entityOffsets;
    private readonly ulong[] relationshipOffsets;
    private readonly ulong[] chainLinkOffsets;
    private readonly ulong[] visibleEventOffsets;
    private readonly ulong[] decisionContextReferenceOffsets;
    private readonly ulong[] publicLocatorTokenOffsets;
    private readonly ulong[] lifePointOffsets;
    private readonly ulong[] ownDeckPasscodeOffsets;
    private readonly ulong[] opponentDeckPasscodeOffsets;
    private readonly ulong[] ownExtraDeckPasscodeOffsets;
    private readonly ulong[] opponentExtraDeckPasscodeOffsets;
    private readonly OcgForgeEncodedCandidateV1[] candidateRows;
    private readonly OcgForgeCandidateOptionalPresenceV1[] candidateOptionalPresenceMasks;
    private readonly string[] candidateRoutingKeys;
    private readonly OcgForgeEncodedZoneV1[] zones;
    private readonly OcgForgeEncodedEntityV1[] entities;
    private readonly OcgForgeEncodedRelationshipV1[] relationships;
    private readonly OcgForgeEncodedChainLinkV1[] chainLinks;
    private readonly OcgForgeEncodedVisibleEventV1[] visibleEvents;
    private readonly uint[] decisionContextReferenceOrdinals;
    private readonly string[] publicLocatorTokens;
    private readonly uint[] lifePoints;
    private readonly uint[] ownDeckPasscodeIds;
    private readonly uint[] opponentDeckPasscodeIds;
    private readonly uint[] ownExtraDeckPasscodeIds;
    private readonly uint[] opponentExtraDeckPasscodeIds;

    public string SchemaId { get; }

    public uint BatchSize { get; }

    public IReadOnlyList<OcgForgeModelBatchSampleHeaderV1> Samples { get; }

    public IReadOnlyList<ulong> CandidateOffsets { get; }

    public IReadOnlyList<ulong> ZoneOffsets { get; }

    public IReadOnlyList<ulong> EntityOffsets { get; }

    public IReadOnlyList<ulong> RelationshipOffsets { get; }

    public IReadOnlyList<ulong> ChainLinkOffsets { get; }

    public IReadOnlyList<ulong> VisibleEventOffsets { get; }

    public IReadOnlyList<ulong> DecisionContextReferenceOffsets { get; }

    public IReadOnlyList<ulong> PublicLocatorTokenOffsets { get; }

    public IReadOnlyList<ulong> LifePointOffsets { get; }

    public IReadOnlyList<ulong> OwnDeckPasscodeOffsets { get; }

    public IReadOnlyList<ulong> OpponentDeckPasscodeOffsets { get; }

    public IReadOnlyList<ulong> OwnExtraDeckPasscodeOffsets { get; }

    public IReadOnlyList<ulong> OpponentExtraDeckPasscodeOffsets { get; }

    public IReadOnlyList<OcgForgeEncodedCandidateV1> CandidateRows { get; }

    public IReadOnlyList<OcgForgeCandidateOptionalPresenceV1>
        CandidateOptionalPresenceMasks { get; }

    public IReadOnlyList<string> CandidateRoutingKeys { get; }

    public IReadOnlyList<OcgForgeEncodedZoneV1> Zones { get; }

    public IReadOnlyList<OcgForgeEncodedEntityV1> Entities { get; }

    public IReadOnlyList<OcgForgeEncodedRelationshipV1> Relationships { get; }

    public IReadOnlyList<OcgForgeEncodedChainLinkV1> ChainLinks { get; }

    public IReadOnlyList<OcgForgeEncodedVisibleEventV1> VisibleEvents { get; }

    public IReadOnlyList<uint> DecisionContextReferenceOrdinals { get; }

    public IReadOnlyList<string> PublicLocatorTokens { get; }

    public IReadOnlyList<uint> LifePoints { get; }

    public IReadOnlyList<uint> OwnDeckPasscodeIds { get; }

    public IReadOnlyList<uint> OpponentDeckPasscodeIds { get; }

    public IReadOnlyList<uint> OwnExtraDeckPasscodeIds { get; }

    public IReadOnlyList<uint> OpponentExtraDeckPasscodeIds { get; }

    private static T[] Copy<T>(IEnumerable<T>? values, string name) =>
        (values ?? throw new ArgumentNullException(name)).ToArray();
}

public enum OcgForgeModelBatchLayoutErrorCodeV1 : byte
{
    EmptyBatch,
    InvalidEncodedSample,
    InvalidRaggedLayout,
    CandidateCountMismatch,
    OptionalPresenceMismatch,
    CountOverflow,
    InternalFailure
}

public readonly record struct OcgForgeModelBatchLayoutErrorV1(
    OcgForgeModelBatchLayoutErrorCodeV1 Code,
    string FieldPath);

public sealed class OcgForgeRaggedModelBatchResultV1
{
    private OcgForgeRaggedModelBatchResultV1(
        bool isSuccess,
        OcgForgeModelBatchLayoutErrorV1? error,
        OcgForgeRaggedModelBatchV1? value)
    {
        IsSuccess = isSuccess;
        Error = error;
        Value = value;
    }

    public bool IsSuccess { get; }

    public OcgForgeModelBatchLayoutErrorV1? Error { get; }

    public OcgForgeRaggedModelBatchV1? Value { get; }

    internal static OcgForgeRaggedModelBatchResultV1 Success(
        OcgForgeRaggedModelBatchV1 value) =>
        new(true, null, value);

    internal static OcgForgeRaggedModelBatchResultV1 Failure(
        OcgForgeModelBatchLayoutErrorCodeV1 code,
        string path) =>
        new(false, new(code, path), null);
}

public sealed class OcgForgeRaggedModelSampleResultV1
{
    private OcgForgeRaggedModelSampleResultV1(
        bool isSuccess,
        OcgForgeModelBatchLayoutErrorV1? error,
        OcgForgeEncodedModelInputV1? value)
    {
        IsSuccess = isSuccess;
        Error = error;
        Value = value;
    }

    public bool IsSuccess { get; }

    public OcgForgeModelBatchLayoutErrorV1? Error { get; }

    public OcgForgeEncodedModelInputV1? Value { get; }

    internal static OcgForgeRaggedModelSampleResultV1 Success(
        OcgForgeEncodedModelInputV1 value) =>
        new(true, null, value);

    internal static OcgForgeRaggedModelSampleResultV1 Failure(
        OcgForgeModelBatchLayoutErrorCodeV1 code,
        string path) =>
        new(false, new(code, path), null);
}

public static class OcgForgeModelBatchLayoutV1
{
    public const string SchemaId = "ocgforge.model_batch_layout.v1";

    public static OcgForgeRaggedModelBatchResultV1 TryCreate(
        IEnumerable<OcgForgeEncodedModelInputV1>? samples)
    {
        if (samples is null)
        {
            return OcgForgeRaggedModelBatchResultV1.Failure(
                OcgForgeModelBatchLayoutErrorCodeV1.EmptyBatch,
                "samples");
        }

        try
        {
            OcgForgeEncodedModelInputV1[] values = samples.ToArray();
            if (values.Length == 0)
            {
                return OcgForgeRaggedModelBatchResultV1.Failure(
                    OcgForgeModelBatchLayoutErrorCodeV1.EmptyBatch,
                    "samples");
            }

            OcgForgeRaggedModelBatchV1 batch = Build(values);
            Validate(batch);
            return OcgForgeRaggedModelBatchResultV1.Success(batch);
        }
        catch (OcgForgeLayoutFailure failure)
        {
            return OcgForgeRaggedModelBatchResultV1.Failure(
                failure.Code,
                failure.FieldPath);
        }
        catch (ArgumentException)
        {
            return OcgForgeRaggedModelBatchResultV1.Failure(
                OcgForgeModelBatchLayoutErrorCodeV1.InvalidEncodedSample,
                "samples");
        }
        catch (OverflowException)
        {
            return OcgForgeRaggedModelBatchResultV1.Failure(
                OcgForgeModelBatchLayoutErrorCodeV1.CountOverflow,
                "samples");
        }
        catch
        {
            return OcgForgeRaggedModelBatchResultV1.Failure(
                OcgForgeModelBatchLayoutErrorCodeV1.InternalFailure,
                "batch");
        }
    }

    public static OcgForgeRaggedModelBatchResultV1 MakeRagged(
        IEnumerable<OcgForgeEncodedModelInputV1>? samples) =>
        TryCreate(samples);

    public static OcgForgeRaggedModelSampleResultV1 TryReconstruct(
        OcgForgeRaggedModelBatchV1? batch,
        int sampleIndex)
    {
        if (batch is null)
        {
            return OcgForgeRaggedModelSampleResultV1.Failure(
                OcgForgeModelBatchLayoutErrorCodeV1.InvalidRaggedLayout,
                "batch");
        }

        try
        {
            Validate(batch);
            return OcgForgeRaggedModelSampleResultV1.Success(
                ReconstructUnchecked(batch, sampleIndex));
        }
        catch (OcgForgeLayoutFailure failure)
        {
            return OcgForgeRaggedModelSampleResultV1.Failure(
                failure.Code,
                failure.FieldPath);
        }
        catch (ArgumentException)
        {
            return OcgForgeRaggedModelSampleResultV1.Failure(
                OcgForgeModelBatchLayoutErrorCodeV1.InvalidEncodedSample,
                "sample");
        }
        catch (OverflowException)
        {
            return OcgForgeRaggedModelSampleResultV1.Failure(
                OcgForgeModelBatchLayoutErrorCodeV1.CountOverflow,
                "sample");
        }
        catch
        {
            return OcgForgeRaggedModelSampleResultV1.Failure(
                OcgForgeModelBatchLayoutErrorCodeV1.InvalidRaggedLayout,
                "sample");
        }
    }

    private static OcgForgeRaggedModelBatchV1 Build(
        OcgForgeEncodedModelInputV1[] values)
    {
        List<OcgForgeModelBatchSampleHeaderV1> headers = new(values.Length);
        List<ulong> candidateOffsets = new() { 0 };
        List<ulong> zoneOffsets = new() { 0 };
        List<ulong> entityOffsets = new() { 0 };
        List<ulong> relationshipOffsets = new() { 0 };
        List<ulong> chainLinkOffsets = new() { 0 };
        List<ulong> visibleEventOffsets = new() { 0 };
        List<ulong> decisionContextReferenceOffsets = new() { 0 };
        List<ulong> publicLocatorTokenOffsets = new() { 0 };
        List<ulong> lifePointOffsets = new() { 0 };
        List<ulong> ownDeckPasscodeOffsets = new() { 0 };
        List<ulong> opponentDeckPasscodeOffsets = new() { 0 };
        List<ulong> ownExtraDeckPasscodeOffsets = new() { 0 };
        List<ulong> opponentExtraDeckPasscodeOffsets = new() { 0 };
        List<OcgForgeEncodedCandidateV1> candidateRows = new();
        List<OcgForgeCandidateOptionalPresenceV1> candidatePresence = new();
        List<string> candidateRoutingKeys = new();
        List<OcgForgeEncodedZoneV1> zones = new();
        List<OcgForgeEncodedEntityV1> entities = new();
        List<OcgForgeEncodedRelationshipV1> relationships = new();
        List<OcgForgeEncodedChainLinkV1> chainLinks = new();
        List<OcgForgeEncodedVisibleEventV1> visibleEvents = new();
        List<uint> decisionContextReferenceOrdinals = new();
        List<string> publicLocatorTokens = new();
        List<uint> lifePoints = new();
        List<uint> ownDeckPasscodeIds = new();
        List<uint> opponentDeckPasscodeIds = new();
        List<uint> ownExtraDeckPasscodeIds = new();
        List<uint> opponentExtraDeckPasscodeIds = new();

        for (int index = 0; index < values.Length; index++)
        {
            OcgForgeEncodedModelInputV1 sample = values[index] ??
                throw new OcgForgeLayoutFailure(
                    OcgForgeModelBatchLayoutErrorCodeV1.InvalidEncodedSample,
                    $"samples[{index}]");
            _ = sample.CanonicalBytes;
            headers.Add(MakeHeader(sample));
            candidateRows.AddRange(sample.CandidateFeatures);
            candidateRoutingKeys.AddRange(sample.RoutingKeys);
            candidatePresence.AddRange(sample.CandidateFeatures.Select(Presence));
            zones.AddRange(sample.Zones);
            entities.AddRange(sample.Entities);
            relationships.AddRange(sample.Relationships);
            chainLinks.AddRange(sample.Chain.Links);
            visibleEvents.AddRange(sample.VisibleEvents);
            decisionContextReferenceOrdinals.AddRange(
                sample.ObservationContextReferenceOrdinals);
            publicLocatorTokens.AddRange(sample.PublicLocatorTable);
            lifePoints.AddRange(sample.Globals.LifePoints);
            ownDeckPasscodeIds.AddRange(sample.MatchContext.OwnDeck.MainDeck);
            opponentDeckPasscodeIds.AddRange(sample.MatchContext.OpponentDeck.MainDeck);
            ownExtraDeckPasscodeIds.AddRange(sample.MatchContext.OwnDeck.ExtraDeck);
            opponentExtraDeckPasscodeIds.AddRange(sample.MatchContext.OpponentDeck.ExtraDeck);
            AppendOffset(candidateOffsets, candidateRows.Count, "candidate_offsets");
            AppendOffset(zoneOffsets, zones.Count, "zone_offsets");
            AppendOffset(entityOffsets, entities.Count, "entity_offsets");
            AppendOffset(relationshipOffsets, relationships.Count, "relationship_offsets");
            AppendOffset(chainLinkOffsets, chainLinks.Count, "chain_link_offsets");
            AppendOffset(visibleEventOffsets, visibleEvents.Count, "visible_event_offsets");
            AppendOffset(
                decisionContextReferenceOffsets,
                decisionContextReferenceOrdinals.Count,
                "decision_context_reference_offsets");
            AppendOffset(
                publicLocatorTokenOffsets,
                publicLocatorTokens.Count,
                "public_locator_token_offsets");
            AppendOffset(lifePointOffsets, lifePoints.Count, "life_point_offsets");
            AppendOffset(
                ownDeckPasscodeOffsets,
                ownDeckPasscodeIds.Count,
                "own_deck_passcode_offsets");
            AppendOffset(
                opponentDeckPasscodeOffsets,
                opponentDeckPasscodeIds.Count,
                "opponent_deck_passcode_offsets");
            AppendOffset(
                ownExtraDeckPasscodeOffsets,
                ownExtraDeckPasscodeIds.Count,
                "own_extra_deck_passcode_offsets");
            AppendOffset(
                opponentExtraDeckPasscodeOffsets,
                opponentExtraDeckPasscodeIds.Count,
                "opponent_extra_deck_passcode_offsets");
        }

        return new OcgForgeRaggedModelBatchV1(
            SchemaId,
            checked((uint)values.Length),
            headers,
            candidateOffsets,
            zoneOffsets,
            entityOffsets,
            relationshipOffsets,
            chainLinkOffsets,
            visibleEventOffsets,
            decisionContextReferenceOffsets,
            publicLocatorTokenOffsets,
            lifePointOffsets,
            ownDeckPasscodeOffsets,
            opponentDeckPasscodeOffsets,
            ownExtraDeckPasscodeOffsets,
            opponentExtraDeckPasscodeOffsets,
            candidateRows,
            candidatePresence,
            candidateRoutingKeys,
            zones,
            entities,
            relationships,
            chainLinks,
            visibleEvents,
            decisionContextReferenceOrdinals,
            publicLocatorTokens,
            lifePoints,
            ownDeckPasscodeIds,
            opponentDeckPasscodeIds,
            ownExtraDeckPasscodeIds,
            opponentExtraDeckPasscodeIds);
    }

    private static OcgForgeModelBatchSampleHeaderV1 MakeHeader(
        OcgForgeEncodedModelInputV1 sample)
    {
        OcgForgeEncodedGlobalsV1 globals = new(
            sample.Globals.DuelFlags,
            Array.Empty<uint>(),
            sample.Globals.PlayerToAct,
            sample.Globals.TurnPlayer,
            sample.Globals.TurnCount,
            sample.Globals.Phase,
            sample.Globals.ChainLength,
            sample.Globals.Winner,
            sample.Globals.WinReason,
            sample.Globals.Terminal);
        OcgForgeEncodedMatchContextV1 matchContext = new(
            sample.MatchContext.PerspectivePlayer,
            sample.MatchContext.DuelFlags,
            sample.MatchContext.OwnDecklistKnown,
            sample.MatchContext.OpponentDecklistKnown,
            new OcgForgeEncodedDeckV1(
                sample.MatchContext.OwnDeck.Known,
                Array.Empty<uint>(),
                Array.Empty<uint>()),
            new OcgForgeEncodedDeckV1(
                sample.MatchContext.OpponentDeck.Known,
                Array.Empty<uint>(),
                Array.Empty<uint>()));
        return new OcgForgeModelBatchSampleHeaderV1(
            sample.SchemaIdValue,
            sample.CardVocabularyIdentity,
            sample.PublicObservationDigest,
            sample.PerspectivePlayer,
            sample.DecisionIndex,
            sample.PublicObservationContextKindCode,
            sample.PublicObservationContextPlayer,
            globals,
            sample.Chain.Length,
            matchContext,
            sample.PublicCandidateDomainDigest);
    }

    private static OcgForgeEncodedModelInputV1 ReconstructUnchecked(
        OcgForgeRaggedModelBatchV1 batch,
        int sampleIndex)
    {
        if (sampleIndex < 0 || sampleIndex >= batch.Samples.Count)
        {
            throw new OcgForgeLayoutFailure(
                OcgForgeModelBatchLayoutErrorCodeV1.InvalidRaggedLayout,
                "sample_index");
        }

        OcgForgeModelBatchSampleHeaderV1 header = batch.Samples[sampleIndex];
        OcgForgeEncodedGlobalsV1 globals = new(
            header.Globals.DuelFlags,
            Slice(batch.LifePoints, batch.LifePointOffsets, sampleIndex),
            header.Globals.PlayerToAct,
            header.Globals.TurnPlayer,
            header.Globals.TurnCount,
            header.Globals.Phase,
            header.Globals.ChainLength,
            header.Globals.Winner,
            header.Globals.WinReason,
            header.Globals.Terminal);
        OcgForgeEncodedDeckV1 ownDeck = new(
            header.MatchContext.OwnDeck.Known,
            Slice(batch.OwnDeckPasscodeIds, batch.OwnDeckPasscodeOffsets, sampleIndex),
            Slice(batch.OwnExtraDeckPasscodeIds, batch.OwnExtraDeckPasscodeOffsets, sampleIndex));
        OcgForgeEncodedDeckV1 opponentDeck = new(
            header.MatchContext.OpponentDeck.Known,
            Slice(
                batch.OpponentDeckPasscodeIds,
                batch.OpponentDeckPasscodeOffsets,
                sampleIndex),
            Slice(
                batch.OpponentExtraDeckPasscodeIds,
                batch.OpponentExtraDeckPasscodeOffsets,
                sampleIndex));
        OcgForgeEncodedMatchContextV1 matchContext = new(
            header.MatchContext.PerspectivePlayer,
            header.MatchContext.DuelFlags,
            header.MatchContext.OwnDecklistKnown,
            header.MatchContext.OpponentDecklistKnown,
            ownDeck,
            opponentDeck);
        return new OcgForgeEncodedModelInputV1(
            header.SchemaId,
            header.CardVocabularyIdentity,
            header.PublicObservationDigest,
            header.PerspectivePlayer,
            header.DecisionIndex,
            Slice(batch.PublicLocatorTokens, batch.PublicLocatorTokenOffsets, sampleIndex),
            header.PublicObservationContextKindCode,
            header.PublicObservationContextPlayer,
            Slice(
                batch.DecisionContextReferenceOrdinals,
                batch.DecisionContextReferenceOffsets,
                sampleIndex),
            globals,
            Slice(batch.Zones, batch.ZoneOffsets, sampleIndex),
            Slice(batch.Entities, batch.EntityOffsets, sampleIndex),
            Slice(batch.Relationships, batch.RelationshipOffsets, sampleIndex),
            new OcgForgeEncodedChainStateV1(
                header.ChainLength,
                Slice(batch.ChainLinks, batch.ChainLinkOffsets, sampleIndex)),
            Slice(batch.VisibleEvents, batch.VisibleEventOffsets, sampleIndex),
            matchContext,
            header.PublicCandidateDomainDigest,
            Slice(batch.CandidateRows, batch.CandidateOffsets, sampleIndex),
            Slice(batch.CandidateRoutingKeys, batch.CandidateOffsets, sampleIndex));
    }

    private static T[] Slice<T>(
        IReadOnlyList<T> values,
        IReadOnlyList<ulong> offsets,
        int sampleIndex)
    {
        ulong beginValue = offsets[sampleIndex];
        ulong endValue = offsets[sampleIndex + 1];
        if (beginValue > int.MaxValue || endValue > int.MaxValue ||
            endValue < beginValue)
        {
            throw new OcgForgeLayoutFailure(
                OcgForgeModelBatchLayoutErrorCodeV1.InvalidRaggedLayout,
                "offsets");
        }

        int begin = checked((int)beginValue);
        int end = checked((int)endValue);
        if (end > values.Count)
        {
            throw new OcgForgeLayoutFailure(
                OcgForgeModelBatchLayoutErrorCodeV1.InvalidRaggedLayout,
                "offsets");
        }

        T[] output = new T[end - begin];
        for (int index = 0; index < output.Length; index++)
        {
            output[index] = values[begin + index];
        }

        return output;
    }

    private static void Validate(OcgForgeRaggedModelBatchV1 batch)
    {
        if (batch.SchemaId != SchemaId ||
            batch.BatchSize == 0 ||
            batch.Samples.Count != batch.BatchSize)
        {
            throw new OcgForgeLayoutFailure(
                OcgForgeModelBatchLayoutErrorCodeV1.EmptyBatch,
                "batch");
        }

        foreach (OcgForgeModelBatchSampleHeaderV1 sample in batch.Samples)
        {
            if (sample.SchemaId != OcgForgeEncodedModelInputV1.SchemaId ||
                sample.Globals.LifePoints.Count != 0 ||
                sample.MatchContext.OwnDeck.MainDeck.Count != 0 ||
                sample.MatchContext.OwnDeck.ExtraDeck.Count != 0 ||
                sample.MatchContext.OpponentDeck.MainDeck.Count != 0 ||
                sample.MatchContext.OpponentDeck.ExtraDeck.Count != 0)
            {
                throw new OcgForgeLayoutFailure(
                    OcgForgeModelBatchLayoutErrorCodeV1.InvalidRaggedLayout,
                    "samples");
            }
        }

        ValidateOffsets(batch.CandidateOffsets, batch.BatchSize, batch.CandidateRows.Count);
        ValidateOffsets(batch.ZoneOffsets, batch.BatchSize, batch.Zones.Count);
        ValidateOffsets(batch.EntityOffsets, batch.BatchSize, batch.Entities.Count);
        ValidateOffsets(
            batch.RelationshipOffsets,
            batch.BatchSize,
            batch.Relationships.Count);
        ValidateOffsets(batch.ChainLinkOffsets, batch.BatchSize, batch.ChainLinks.Count);
        ValidateOffsets(
            batch.VisibleEventOffsets,
            batch.BatchSize,
            batch.VisibleEvents.Count);
        ValidateOffsets(
            batch.DecisionContextReferenceOffsets,
            batch.BatchSize,
            batch.DecisionContextReferenceOrdinals.Count);
        ValidateOffsets(
            batch.PublicLocatorTokenOffsets,
            batch.BatchSize,
            batch.PublicLocatorTokens.Count);
        ValidateOffsets(batch.LifePointOffsets, batch.BatchSize, batch.LifePoints.Count);
        ValidateOffsets(
            batch.OwnDeckPasscodeOffsets,
            batch.BatchSize,
            batch.OwnDeckPasscodeIds.Count);
        ValidateOffsets(
            batch.OpponentDeckPasscodeOffsets,
            batch.BatchSize,
            batch.OpponentDeckPasscodeIds.Count);
        ValidateOffsets(
            batch.OwnExtraDeckPasscodeOffsets,
            batch.BatchSize,
            batch.OwnExtraDeckPasscodeIds.Count);
        ValidateOffsets(
            batch.OpponentExtraDeckPasscodeOffsets,
            batch.BatchSize,
            batch.OpponentExtraDeckPasscodeIds.Count);

        if (batch.CandidateRows.Count != batch.CandidateRoutingKeys.Count ||
            batch.CandidateRows.Count != batch.CandidateOptionalPresenceMasks.Count)
        {
            throw new OcgForgeLayoutFailure(
                OcgForgeModelBatchLayoutErrorCodeV1.CandidateCountMismatch,
                "candidate_rows");
        }

        for (int index = 0; index < batch.CandidateRows.Count; index++)
        {
            if (!SamePresence(
                    batch.CandidateOptionalPresenceMasks[index],
                    Presence(batch.CandidateRows[index])))
            {
                throw new OcgForgeLayoutFailure(
                    OcgForgeModelBatchLayoutErrorCodeV1.OptionalPresenceMismatch,
                    $"candidate_optional_presence_masks[{index}]");
            }
        }

        for (int sampleIndex = 0; sampleIndex < batch.Samples.Count; sampleIndex++)
        {
            try
            {
                _ = ReconstructUnchecked(batch, sampleIndex).CanonicalBytes;
            }
            catch (OcgForgeLayoutFailure)
            {
                throw;
            }
            catch
            {
                throw new OcgForgeLayoutFailure(
                    OcgForgeModelBatchLayoutErrorCodeV1.InvalidEncodedSample,
                    $"samples[{sampleIndex}]");
            }
        }
    }

    private static void ValidateOffsets(
        IReadOnlyList<ulong> offsets,
        uint batchSize,
        int flatLength)
    {
        if ((ulong)offsets.Count != (ulong)batchSize + 1UL ||
            offsets.Count == 0 ||
            offsets[0] != 0 ||
            offsets[^1] != (ulong)flatLength)
        {
            throw new OcgForgeLayoutFailure(
                OcgForgeModelBatchLayoutErrorCodeV1.InvalidRaggedLayout,
                "offsets");
        }

        for (int index = 1; index < offsets.Count; index++)
        {
            if (offsets[index - 1] > offsets[index] || offsets[index] > int.MaxValue)
            {
                throw new OcgForgeLayoutFailure(
                    OcgForgeModelBatchLayoutErrorCodeV1.InvalidRaggedLayout,
                    "offsets");
            }
        }
    }

    private static void AppendOffset(List<ulong> offsets, int flatLength, string path)
    {
        if (flatLength < 0)
        {
            throw new OcgForgeLayoutFailure(
                OcgForgeModelBatchLayoutErrorCodeV1.CountOverflow,
                path);
        }

        ulong value = checked((ulong)flatLength);
        if (value < offsets[^1])
        {
            throw new OcgForgeLayoutFailure(
                OcgForgeModelBatchLayoutErrorCodeV1.CountOverflow,
                path);
        }

        offsets.Add(value);
    }

    private static OcgForgeCandidateOptionalPresenceV1 Presence(
        OcgForgeEncodedCandidateV1 candidate) =>
        new(
            (byte)(candidate.Choice.HasValue ? 1 : 0),
            (byte)(candidate.SourceReference.HasValue ? 1 : 0),
            (byte)(candidate.TargetReference.HasValue ? 1 : 0),
            (byte)(candidate.Phase.HasValue ? 1 : 0),
            (byte)(candidate.Position.HasValue ? 1 : 0),
            (byte)(candidate.SourceIndex.HasValue ? 1 : 0),
            (byte)(candidate.Amount.HasValue ? 1 : 0));

    private static bool SamePresence(
        OcgForgeCandidateOptionalPresenceV1 left,
        OcgForgeCandidateOptionalPresenceV1 right) =>
        left.Choice <= 1 &&
        left.SourceReference <= 1 &&
        left.TargetReference <= 1 &&
        left.Phase <= 1 &&
        left.Position <= 1 &&
        left.SourceIndex <= 1 &&
        left.Amount <= 1 &&
        left == right;

    private sealed class OcgForgeLayoutFailure : Exception
    {
        internal OcgForgeLayoutFailure(
            OcgForgeModelBatchLayoutErrorCodeV1 code,
            string fieldPath)
        {
            Code = code;
            FieldPath = fieldPath;
        }

        internal OcgForgeModelBatchLayoutErrorCodeV1 Code { get; }

        internal string FieldPath { get; }
    }
}
