using OCGForge.Ignis.Gameplay;

namespace OCGForge.Ignis.Model;

public enum OcgForgePublicCandidateBridgeErrorCodeV1 : byte
{
    None = 0,
    InvalidInput = 1,
    InvalidDecisionActor = 2,
    DecisionActorMismatch = 3,
    UnsupportedCandidate = 4,
    PromptLocalCardCode = 5,
    InvalidPublicReference = 6,
    AmbiguousPublicReference = 7,
    InvalidPublicValue = 8,
    CandidateCountMismatch = 9,
    DuplicatePublicActionKey = 10,
    CanonicalizationFailure = 11,
    CandidateDomainFailure = 12
}

public readonly record struct OcgForgePublicCandidateBridgeErrorV1(
    OcgForgePublicCandidateBridgeErrorCodeV1 Code,
    string FieldPath);

public sealed class OcgForgePublicCandidateV1
{
    private readonly byte[] canonicalDescriptorBytes;

    internal OcgForgePublicCandidateV1(
        OcgForgePublicActionDescriptorV1 descriptor,
        byte[] canonicalDescriptorBytes,
        string publicActionKey,
        bool submitsEngineResponse)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        this.canonicalDescriptorBytes =
            (canonicalDescriptorBytes ?? throw new ArgumentNullException(nameof(canonicalDescriptorBytes)))
            .ToArray();
        PublicActionKey = publicActionKey ??
            throw new ArgumentNullException(nameof(publicActionKey));
        SubmitsEngineResponse = submitsEngineResponse;
    }

    public OcgForgePublicActionDescriptorV1 Descriptor { get; }

    public byte[] CanonicalDescriptorBytes => canonicalDescriptorBytes.ToArray();

    public string PublicActionKey { get; }

    /// <summary>
    /// Accepted I4/I5 transition metadata. It is intentionally excluded from
    /// the public-action identity descriptor and its canonical key.
    /// </summary>
    public bool SubmitsEngineResponse { get; }
}

public sealed class OcgForgeAcceptedDecisionIndexV1
{
    internal OcgForgeAcceptedDecisionIndexV1(ulong value)
    {
        Value = value;
    }

    public ulong Value { get; }
}

public enum OcgForgeAcceptedDecisionBoundaryProducerErrorCodeV1 : byte
{
    None = 0,
    InvalidInput = 1,
    InvalidProjection = 2,
    DecisionIndexExhausted = 3,
    HandoffRejected = 4
}

public readonly record struct OcgForgeAcceptedDecisionBoundaryProducerErrorV1(
    OcgForgeAcceptedDecisionBoundaryProducerErrorCodeV1 Code,
    string FieldPath);

public sealed class OcgForgeAcceptedDecisionBoundaryV1
{
    private readonly FlatPromptProjectionResultV1 projection;
    private readonly FlatPublicCandidateDescriptorV1[] candidates;
    private readonly IReadOnlyList<FlatPublicCandidateDescriptorV1> candidatesView;
    private readonly I6DPrivateCrossLocatorBindingHandoffV1?
        crossLocatorHandoff;

    internal OcgForgeAcceptedDecisionBoundaryV1(
        PerspectiveSafeFrameV1 frame,
        FlatPromptProjectionResultV1 projection,
        OcgForgeAcceptedDecisionIndexV1 decisionIndex,
        I6DPrivateCrossLocatorBindingHandoffV1? crossLocatorHandoff = null)
    {
        Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        this.projection = projection ??
            throw new ArgumentNullException(nameof(projection));
        ArgumentNullException.ThrowIfNull(decisionIndex);
        if (!projection.IsSuccess ||
            projection.Context is null ||
            projection.Candidates is null ||
            projection.Candidates.Count == 0 ||
            projection.Candidates.Any(candidate => candidate is null))
        {
            throw new ArgumentException(
                "The decision boundary must contain an accepted complete projection.",
                nameof(projection));
        }

        candidates = projection.Candidates.ToArray();
        candidatesView = Array.AsReadOnly(candidates);
        DecisionIndex = decisionIndex.Value;
        this.crossLocatorHandoff = crossLocatorHandoff;
    }

    public PerspectiveSafeFrameV1 Frame { get; }

    public ulong DecisionIndex { get; }

    public FlatPromptPublicContextV1 Decision => projection.Context!;

    public IReadOnlyList<FlatPublicCandidateDescriptorV1> Candidates => candidatesView;

    internal I6DPrivateCrossLocatorBindingHandoffV1? CrossLocatorHandoff =>
        crossLocatorHandoff;
}

public sealed class OcgForgeAcceptedDecisionBoundaryProducerV1
{
    private ulong nextDecisionIndex;
    private readonly object acceptanceGate = new();

    public OcgForgeAcceptedDecisionBoundaryProducerV1()
    {
    }

    public bool TryAccept(
        PerspectiveSafeFrameV1? frame,
        FlatPromptProjectionResultV1? projection,
        out OcgForgeAcceptedDecisionBoundaryV1? boundary,
        out OcgForgeAcceptedDecisionBoundaryProducerErrorV1? error)
    {
        lock (acceptanceGate)
        {
            return TryAcceptCore(
                frame,
                projection,
                null,
                i6DAware: false,
                out boundary,
                out error);
        }
    }

    public bool TryAccept(
        PerspectiveSafeFrameV1? frame,
        FlatPromptProjectionResultV1? projection,
        I6DPrivateCrossLocatorBindingHandoffV1? crossLocatorHandoff,
        out OcgForgeAcceptedDecisionBoundaryV1? boundary,
        out OcgForgeAcceptedDecisionBoundaryProducerErrorV1? error)
    {
        lock (acceptanceGate)
        {
            return TryAcceptCore(
                frame,
                projection,
                crossLocatorHandoff,
                i6DAware: true,
                out boundary,
                out error);
        }
    }

    private bool TryAcceptCore(
        PerspectiveSafeFrameV1? frame,
        FlatPromptProjectionResultV1? projection,
        I6DPrivateCrossLocatorBindingHandoffV1? crossLocatorHandoff,
        bool i6DAware,
        out OcgForgeAcceptedDecisionBoundaryV1? boundary,
        out OcgForgeAcceptedDecisionBoundaryProducerErrorV1? error)
    {
        boundary = null;
        error = null;
        if (frame is null)
        {
            error = new(
                OcgForgeAcceptedDecisionBoundaryProducerErrorCodeV1.InvalidInput,
                "frame");
            return false;
        }

        if (projection is null)
        {
            error = new(
                OcgForgeAcceptedDecisionBoundaryProducerErrorCodeV1.InvalidInput,
                "projection");
            return false;
        }

        if (!projection.IsSuccess ||
            projection.Context is null ||
            projection.Candidates is null ||
            projection.Candidates.Count == 0 ||
            projection.Candidates.Any(candidate => candidate is null))
        {
            error = new(
                OcgForgeAcceptedDecisionBoundaryProducerErrorCodeV1.InvalidProjection,
                "projection");
            return false;
        }

        if (nextDecisionIndex == ulong.MaxValue)
        {
            error = new(
                OcgForgeAcceptedDecisionBoundaryProducerErrorCodeV1.DecisionIndexExhausted,
                "decision_index");
            return false;
        }

        if (i6DAware &&
            crossLocatorHandoff is null &&
            !OcgForgePublicCandidateBridgeV1.TryValidateExactI6DReferences(
                frame,
                projection.Context!,
                projection.Candidates,
                out string missingHandoffPath))
        {
            error = new(
                OcgForgeAcceptedDecisionBoundaryProducerErrorCodeV1.HandoffRejected,
                missingHandoffPath);
            return false;
        }

        I6DBoundaryAcceptanceLeaseV1? acceptanceLease = null;
        if (crossLocatorHandoff is not null &&
            !crossLocatorHandoff.TryAcquireBoundaryAcceptanceLease(
                frame,
                projection,
                out acceptanceLease,
                out I6DPrivateCrossLocatorHandoffErrorV1? handoffError))
        {
            error = new(
                OcgForgeAcceptedDecisionBoundaryProducerErrorCodeV1.HandoffRejected,
                handoffError?.FieldPath ?? "handoff");
            return false;
        }

        using (acceptanceLease)
        {
            boundary = new OcgForgeAcceptedDecisionBoundaryV1(
                frame,
                projection,
                new OcgForgeAcceptedDecisionIndexV1(nextDecisionIndex),
                crossLocatorHandoff);
            nextDecisionIndex++;
            return true;
        }
    }
}

public sealed class OcgForgePublicDecisionBoundaryContextV1
{
    private readonly string[] referencedEntities;
    private readonly IReadOnlyList<string> referencedEntitiesView;

    internal OcgForgePublicDecisionBoundaryContextV1(
        string kind,
        byte player,
        IEnumerable<string> referencedEntities)
    {
        Kind = kind ?? throw new ArgumentNullException(nameof(kind));
        this.referencedEntities =
            (referencedEntities ?? throw new ArgumentNullException(nameof(referencedEntities)))
            .ToArray();
        referencedEntitiesView = Array.AsReadOnly(this.referencedEntities);
        Player = player;
    }

    public string Kind { get; }

    public byte Player { get; }

    public IReadOnlyList<string> ReferencedEntities => referencedEntitiesView;
}

public sealed class OcgForgePublicDecisionContextV1
{
    private readonly OcgForgePublicCandidateV1[] candidates;
    private readonly IReadOnlyList<OcgForgePublicCandidateV1> candidatesView;

    internal OcgForgePublicDecisionContextV1(
        PerspectiveSafeFrameV1 frame,
        ulong decisionIndex,
        byte playerToAct,
        string requestKind,
        IEnumerable<OcgForgePublicCandidateV1> candidates,
        string publicCandidateDomainDigest,
        IEnumerable<string> referencedEntities)
    {
        Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        DecisionIndex = decisionIndex;
        PlayerToAct = playerToAct;
        RequestKind = requestKind ?? throw new ArgumentNullException(nameof(requestKind));
        this.candidates =
            (candidates ?? throw new ArgumentNullException(nameof(candidates))).ToArray();
        candidatesView = Array.AsReadOnly(this.candidates);
        PublicCandidateDomainDigest = publicCandidateDomainDigest ??
            throw new ArgumentNullException(nameof(publicCandidateDomainDigest));
        PublicDecisionContext = new OcgForgePublicDecisionBoundaryContextV1(
            RequestKind,
            PlayerToAct,
            referencedEntities);
    }

    public PerspectiveSafeFrameV1 Frame { get; }

    public ulong DecisionIndex { get; }

    public byte PlayerToAct { get; }

    public string RequestKind { get; }

    public OcgForgePublicDecisionBoundaryContextV1 PublicDecisionContext { get; }

    public byte GlobalsPlayerToAct => PlayerToAct;

    public IReadOnlyList<string> ReferencedEntities =>
        PublicDecisionContext.ReferencedEntities;

    public IReadOnlyList<OcgForgePublicCandidateV1> Candidates => candidatesView;

    public string PublicCandidateDomainDigest { get; }
}

public sealed class OcgForgePublicDecisionContextResultV1
{
    private OcgForgePublicDecisionContextResultV1(
        bool isSuccess,
        OcgForgePublicCandidateBridgeErrorV1? error,
        OcgForgePublicDecisionContextV1? context)
    {
        IsSuccess = isSuccess;
        Error = error;
        Context = context;
    }

    public bool IsSuccess { get; }

    public OcgForgePublicCandidateBridgeErrorV1? Error { get; }

    public OcgForgePublicDecisionContextV1? Context { get; }

    internal static OcgForgePublicDecisionContextResultV1 Success(
        OcgForgePublicDecisionContextV1 context) =>
        new(true, null, context);

    internal static OcgForgePublicDecisionContextResultV1 Failure(
        OcgForgePublicCandidateBridgeErrorCodeV1 code,
        string fieldPath) =>
        new(false, new(code, fieldPath), null);
}

public static class OcgForgePublicCandidateBridgeV1
{
    public static OcgForgePublicDecisionContextResultV1 TryCreate(
        OcgForgeAcceptedDecisionBoundaryV1? acceptedDecision)
    {
        if (acceptedDecision is null)
        {
            return Failure(
                OcgForgePublicCandidateBridgeErrorCodeV1.InvalidInput,
                "accepted_decision");
        }

        PerspectiveSafeFrameV1 frame = acceptedDecision.Frame;
        FlatPromptPublicContextV1 decision = acceptedDecision.Decision;
        IReadOnlyList<FlatPublicCandidateDescriptorV1> candidates =
            acceptedDecision.Candidates;

        if (frame.MatchContext.PerspectivePlayer != decision.ActingPlayer)
        {
            return Failure(
                OcgForgePublicCandidateBridgeErrorCodeV1.DecisionActorMismatch,
                "match_context.perspective_player");
        }

        if (decision.ActingPlayer > 1)
        {
            return Failure(
                OcgForgePublicCandidateBridgeErrorCodeV1.InvalidDecisionActor,
                "decision.player_to_act");
        }

        if (frame.Globals.PlayerToAct.HasValue &&
            frame.Globals.PlayerToAct.Value != decision.ActingPlayer)
        {
            return Failure(
                OcgForgePublicCandidateBridgeErrorCodeV1.DecisionActorMismatch,
                "globals.player_to_act");
        }

        if (candidates.Count == 0)
        {
            return Failure(
                OcgForgePublicCandidateBridgeErrorCodeV1.CandidateCountMismatch,
                "candidates");
        }

        if (decision is FlatPromptEffectYnCardCodePublicContextV1)
        {
            return Failure(
                OcgForgePublicCandidateBridgeErrorCodeV1.PromptLocalCardCode,
                "decision.effect_card_code");
        }

        string requestKind = RequestKind(decision.PromptFamily);
        if (requestKind.Length == 0)
        {
            return Failure(
                OcgForgePublicCandidateBridgeErrorCodeV1.UnsupportedCandidate,
                "decision.request_kind");
        }

        List<OcgForgePublicCandidateV1> mapped = new(candidates.Count);
        HashSet<string> publicKeys = new(StringComparer.Ordinal);
        for (int index = 0; index < candidates.Count; index++)
        {
            FlatPublicCandidateDescriptorV1? candidate = candidates[index];
            if (candidate is null)
            {
                return Failure(
                    OcgForgePublicCandidateBridgeErrorCodeV1.InvalidInput,
                    $"candidates[{index}]");
            }

            if (HasForbiddenPromptLocalCardCode(candidate))
            {
                return Failure(
                    OcgForgePublicCandidateBridgeErrorCodeV1.PromptLocalCardCode,
                    $"candidates[{index}].card_code");
            }

            if (!TryMapCandidate(
                    frame,
                    decision,
                    candidate,
                    acceptedDecision.CrossLocatorHandoff,
                    index,
                    out OcgForgePublicActionDescriptorV1? descriptor,
                    out OcgForgePublicCandidateBridgeErrorV1? mappingError))
            {
                OcgForgePublicCandidateBridgeErrorV1 safeError = mappingError ??
                    new(
                        OcgForgePublicCandidateBridgeErrorCodeV1.UnsupportedCandidate,
                        $"candidates[{index}]");
                return Failure(safeError.Code, safeError.FieldPath);
            }

            OcgForgePublicActionIdentityResultV1 identity =
                OcgForgePublicActionIdentityV1.TryCreate(descriptor);
            if (!identity.IsSuccess ||
                identity.CanonicalDescriptorBytes is null ||
                identity.PublicActionKey is null)
            {
                return Failure(
                    OcgForgePublicCandidateBridgeErrorCodeV1.CanonicalizationFailure,
                    $"candidates[{index}].public_action");
            }

            if (!publicKeys.Add(identity.PublicActionKey))
            {
                return Failure(
                    OcgForgePublicCandidateBridgeErrorCodeV1.DuplicatePublicActionKey,
                    $"candidates[{index}].public_action_key");
            }

            if (!TryGetSubmitsEngineResponse(
                    descriptor!.ContinuationOperation,
                    out bool submitsEngineResponse))
            {
                return Failure(
                    OcgForgePublicCandidateBridgeErrorCodeV1.UnsupportedCandidate,
                    $"candidates[{index}].continuation_operation");
            }

            mapped.Add(
                new OcgForgePublicCandidateV1(
                    descriptor!,
                    identity.CanonicalDescriptorBytes,
                    identity.PublicActionKey,
                    submitsEngineResponse));
        }

        OcgForgePublicCandidateDomainResultV1 domain =
            OcgForgePublicActionIdentityV1.TryCreateCandidateDomain(
                requestKind,
                mapped.Select(candidate => candidate.PublicActionKey).ToArray());
        if (!domain.IsSuccess || domain.Digest is null)
        {
            return Failure(
                OcgForgePublicCandidateBridgeErrorCodeV1.CandidateDomainFailure,
                domain.Error?.FieldPath ?? "candidates");
        }

        List<string> referencedEntities = mapped
            .SelectMany(candidate => new OcgForgePublicCardReferenceV1?[]
            {
                candidate.Descriptor.SourceReference,
                candidate.Descriptor.TargetReference
            })
            .Where(reference => reference.HasValue)
            .Select(reference => reference!.Value.ObservationLocator)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToList();

        return OcgForgePublicDecisionContextResultV1.Success(
            new OcgForgePublicDecisionContextV1(
                frame,
                acceptedDecision.DecisionIndex,
                decision.ActingPlayer,
                requestKind,
                mapped,
                domain.Digest,
                referencedEntities));
    }

    private static bool TryMapCandidate(
        PerspectiveSafeFrameV1 frame,
        FlatPromptPublicContextV1 decision,
        FlatPublicCandidateDescriptorV1 candidate,
        I6DPrivateCrossLocatorBindingHandoffV1? crossLocatorHandoff,
        int index,
        out OcgForgePublicActionDescriptorV1? descriptor,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        descriptor = null;
        error = null;
        string path = $"candidates[{index}]";

        switch (candidate)
        {
            case FlatYesNoPublicCandidateDescriptorV1 yesNo:
                if (!IsFamily(decision, 13) ||
                    !TryBooleanChoice(
                        yesNo.ChoiceKind,
                        OcgForgePublicChoiceKindV1.YesNo,
                        path + ".choice",
                        out OcgForgePublicChoiceV1 choice,
                        out error))
                {
                    return false;
                }

                descriptor = CreateDescriptor("yes_no", choice);
                return true;

            case FlatEffectYnPublicCandidateDescriptorV1 effectYn:
                if (decision is not FlatPromptEffectYnPublicContextV1 effectContext ||
                    !TryMapReferenceWithHandoff(
                        frame,
                        effectContext.EffectCardLocator,
                        effectYn,
                        crossLocatorHandoff,
                        path + ".source_reference",
                        out OcgForgePublicCardReferenceV1 effectReference,
                        out error) ||
                    !TryBooleanChoice(
                        effectYn.ChoiceKind,
                        OcgForgePublicChoiceKindV1.EffectYesNo,
                        path + ".choice",
                        out OcgForgePublicChoiceV1 effectChoice,
                        out error))
                {
                    return false;
                }

                descriptor = CreateDescriptor(
                    "yes_no",
                    effectChoice,
                    sourceReference: effectReference);
                return true;

            case FlatOptionPublicCandidateDescriptorV1 option:
                if (!IsFamily(decision, 14) ||
                    option.SourceSection != FlatPromptSourceSectionV1.Options ||
                    !TryUInt32(
                        option.SourceOrdinal,
                        path + ".source_index",
                        out uint optionIndex,
                        out error))
                {
                    return false;
                }

                descriptor = CreateDescriptor(
                    "option",
                    new OcgForgePublicChoiceV1(
                        OcgForgePublicChoiceKindV1.OptionValue,
                        option.OptionValue,
                        optionIndex));
                return true;

            case FlatPositionPublicCandidateDescriptorV1 position:
                if (!IsFamily(decision, 19) ||
                    !TryPosition(
                        position.ChoiceKind,
                        position.PositionValue,
                        path + ".position",
                        out byte positionValue,
                        out error))
                {
                    return false;
                }

                descriptor = CreateDescriptor(
                    "position",
                    position: positionValue);
                return true;

            case FlatChainNoChainPublicCandidateDescriptorV1:
                if (!IsFamily(decision, 16))
                {
                    return Unsupported(path, out descriptor, out error);
                }

                descriptor = CreateDescriptor("chain", phase: 1);
                return true;

            case FlatChainEntryPublicCandidateDescriptorBaseV1 chain:
                if (!IsFamily(decision, 16) ||
                    chain.SourceSection != FlatPromptSourceSectionV1.ChainChoices ||
                    !TryMapReferenceWithHandoff(
                        frame,
                        chain.PublicSemanticCardLocator,
                        chain,
                        crossLocatorHandoff,
                        path + ".source_reference",
                        out OcgForgePublicCardReferenceV1 chainReference,
                        out error) ||
                    !TryUInt32(
                        chain.SourceOrdinal,
                        path + ".source_index",
                        out uint chainIndex,
                        out error))
                {
                    return false;
                }

                descriptor = CreateDescriptor(
                    "chain",
                    new OcgForgePublicChoiceV1(
                        OcgForgePublicChoiceKindV1.EffectChoice,
                        chainIndex,
                        null),
                    sourceReference: chainReference,
                    phase: 0);
                return true;

            case FlatBattleActivatablePublicCandidateBaseV1 battleActivate:
                if (!IsFamily(decision, 10) ||
                    battleActivate.SourceSection != FlatPromptSourceSectionV1.Activatable ||
                    !TryMapReferenceWithHandoff(
                        frame,
                        battleActivate.PublicSemanticCardLocator,
                        battleActivate,
                        crossLocatorHandoff,
                        path + ".source_reference",
                        out OcgForgePublicCardReferenceV1 battleActivateReference,
                        out error) ||
                    !TryUInt32(
                        battleActivate.SourceOrdinal,
                        path + ".source_index",
                        out uint battleActivateIndex,
                        out error))
                {
                    return false;
                }

                descriptor = CreateDescriptor(
                    "battle_command",
                    new OcgForgePublicChoiceV1(
                        OcgForgePublicChoiceKindV1.EffectChoice,
                        battleActivateIndex,
                        null),
                    sourceReference: battleActivateReference,
                    phase: 0);
                return true;

            case FlatBattleAttackPublicCandidateBaseV1 battleAttack:
                if (!IsFamily(decision, 10) ||
                    battleAttack.SourceSection != FlatPromptSourceSectionV1.Attackable ||
                    !TryMapReferenceWithHandoff(
                        frame,
                        battleAttack.PublicSemanticCardLocator,
                        battleAttack,
                        crossLocatorHandoff,
                        path + ".source_reference",
                        out OcgForgePublicCardReferenceV1 battleAttackReference,
                        out error) ||
                    !TryUInt32(
                        battleAttack.SourceOrdinal,
                        path + ".source_index",
                        out uint battleAttackIndex,
                        out error))
                {
                    return false;
                }

                descriptor = CreateDescriptor(
                    "battle_command",
                    new OcgForgePublicChoiceV1(
                        OcgForgePublicChoiceKindV1.EffectChoice,
                        battleAttackIndex,
                        null),
                    sourceReference: battleAttackReference,
                    phase: 1);
                return true;

            case FlatBattleToMainPhase2PublicCandidateV1:
                if (!IsFamily(decision, 10))
                {
                    return Unsupported(path, out descriptor, out error);
                }

                descriptor = CreateDescriptor("battle_command", phase: 2);
                return true;

            case FlatBattleToEndPhasePublicCandidateV1:
                if (!IsFamily(decision, 10))
                {
                    return Unsupported(path, out descriptor, out error);
                }

                descriptor = CreateDescriptor("battle_command", phase: 3);
                return true;

            case FlatIdleSummonPublicCandidateBaseV1 idleSummon:
                return TryMapIdleCard(
                    frame,
                    decision,
                    idleSummon,
                    crossLocatorHandoff,
                    0,
                    path,
                    out descriptor,
                    out error);

            case FlatIdleSpecialSummonPublicCandidateBaseV1 idleSpecialSummon:
                return TryMapIdleCard(
                    frame,
                    decision,
                    idleSpecialSummon,
                    crossLocatorHandoff,
                    1,
                    path,
                    out descriptor,
                    out error);

            case FlatIdleRepositionPublicCandidateBaseV1 idleReposition:
                return TryMapIdleCard(
                    frame,
                    decision,
                    idleReposition,
                    crossLocatorHandoff,
                    2,
                    path,
                    out descriptor,
                    out error);

            case FlatIdleMsetPublicCandidateBaseV1 idleMset:
                return TryMapIdleCard(
                    frame,
                    decision,
                    idleMset,
                    crossLocatorHandoff,
                    3,
                    path,
                    out descriptor,
                    out error);

            case FlatIdleSsetPublicCandidateBaseV1 idleSset:
                return TryMapIdleCard(
                    frame,
                    decision,
                    idleSset,
                    crossLocatorHandoff,
                    4,
                    path,
                    out descriptor,
                    out error);

            case FlatIdleActivatablePublicCandidateBaseV1 idleActivate:
                return TryMapIdleActivate(
                    frame,
                    decision,
                    idleActivate,
                    crossLocatorHandoff,
                    path,
                    out descriptor,
                    out error);

            case FlatIdleToBattlePhasePublicCandidateV1:
                if (!IsFamily(decision, 11))
                {
                    return Unsupported(path, out descriptor, out error);
                }

                descriptor = CreateDescriptor("idle_command", phase: 6);
                return true;

            case FlatIdleToEndPhasePublicCandidateV1:
                if (!IsFamily(decision, 11))
                {
                    return Unsupported(path, out descriptor, out error);
                }

                descriptor = CreateDescriptor("idle_command", phase: 7);
                return true;

            case FlatIdleShuffleHandPublicCandidateV1:
                if (!IsFamily(decision, 11))
                {
                    return Unsupported(path, out descriptor, out error);
                }

                descriptor = CreateDescriptor("idle_command", phase: 8);
                return true;

            case FlatPromptCardSelectionAnonymousCandidateV1 cardAnonymous:
                return TryMapCardSelection(
                    decision,
                    cardAnonymous.SourceOrdinal,
                    null,
                    cardAnonymous.SourceSection,
                    path,
                    out descriptor,
                    out error);

            case FlatPromptCardSelectionLocatorCandidateV1 cardLocator:
                return TryMapCardSelection(
                    frame,
                    decision,
                    cardLocator.SourceOrdinal,
                    cardLocator.PublicSemanticCardLocator,
                    cardLocator.SourceSection,
                    path,
                    cardLocator,
                    crossLocatorHandoff,
                    out descriptor,
                    out error);

            case FlatPromptTributeSelectionAnonymousCandidateV1 tributeAnonymous:
                return TryMapContinuationCard(
                    decision,
                    tributeAnonymous.SourceOrdinal,
                    null,
                    tributeAnonymous.SourceSection,
                    path,
                    out descriptor,
                    out error);

            case FlatPromptTributeSelectionLocatorCandidateV1 tributeLocator:
                return TryMapContinuationCard(
                    frame,
                    decision,
                    tributeLocator.SourceOrdinal,
                    tributeLocator.PublicSemanticCardLocator,
                    tributeLocator.SourceSection,
                    path,
                    tributeLocator,
                    crossLocatorHandoff,
                    out descriptor,
                    out error);

            case FlatPromptSelectUnselectAnonymousCandidateV1 selectAnonymous:
                return TryMapSelectUnselect(
                    decision,
                    selectAnonymous.SourceOrdinal,
                    null,
                    selectAnonymous.SourceSection,
                    selectAnonymous.ChoiceKind,
                    path,
                    out descriptor,
                    out error);

            case FlatPromptSelectUnselectLocatorCandidateV1 selectLocator:
                return TryMapSelectUnselect(
                    frame,
                    decision,
                    selectLocator.SourceOrdinal,
                    selectLocator.PublicSemanticCardLocator,
                    selectLocator.SourceSection,
                    selectLocator.ChoiceKind,
                    path,
                    selectLocator,
                    crossLocatorHandoff,
                    out descriptor,
                    out error);

            case FlatPromptFinishOrCancelPublicCandidateV1:
                if (!IsFamily(decision, 26))
                {
                    return Unsupported(path, out descriptor, out error);
                }

                descriptor = CreateDescriptor("finish");
                return true;

            case FlatPromptFinishPublicCandidateV1:
                return TryMapFinishOrCancel(
                    decision,
                    "finish",
                    path,
                    out descriptor,
                    out error);

            case FlatPromptCancelPublicCandidateV1:
                return TryMapFinishOrCancel(
                    decision,
                    "cancel",
                    path,
                    out descriptor,
                    out error);

            case FlatPromptAnnounceNumberPublicCandidateV1 number:
                if (decision is not FlatPromptAnnounceNumberPublicContextV1 numberContext ||
                    !IsFamily(decision, 143) ||
                    number.SourceSection != FlatPromptSourceSectionV1.NumberOptions ||
                    number.SourceOrdinal < 0 ||
                    number.SourceOrdinal >= numberContext.OptionCount)
                {
                    return Unsupported(path + ".source_index", out descriptor, out error);
                }

                if (!TryUInt32(
                        number.SourceOrdinal,
                        path + ".source_index",
                        out uint numberIndex,
                        out error))
                {
                    return false;
                }

                descriptor = CreateDescriptor(
                    "announcement",
                    new OcgForgePublicChoiceV1(
                        OcgForgePublicChoiceKindV1.AnnouncementNumber,
                        number.NumberValue,
                        numberIndex));
                return true;

            case FlatPromptFieldPlacePublicCandidateV1 place:
                if (decision is not FlatPromptPlaceSelectionPublicContextBaseV1 placeContext ||
                    !IsFamily(decision, 18) && !IsFamily(decision, 24) ||
                    !IsEligiblePlace(placeContext, place))
                {
                    return Unsupported(path + ".source_index", out descriptor, out error);
                }

                if (!TryPlaceIndex(
                        placeContext.ActingPlayer,
                        place,
                        path + ".source_index",
                        out uint placeIndex,
                        out error))
                {
                    return false;
                }

                descriptor = placeContext.RequiredPlaceCount == 1
                    ? CreateDescriptor("place", sourceIndex: placeIndex)
                    : CreateDescriptor(
                        "pick",
                        sourceIndex: placeIndex,
                        continuationOperation: "pick");
                return true;

            case FlatPromptMaskBitPublicCandidateV1 mask:
                ulong availableMask = decision switch
                {
                    FlatPromptRaceSelectionPublicContextV1 race => race.AvailableRaceMask,
                    FlatPromptAttributeSelectionPublicContextV1 attribute =>
                        attribute.AvailableAttributeMask,
                    _ => 0UL
                };
                if ((!IsFamily(decision, 140) && !IsFamily(decision, 141)) ||
                    mask.BitIndex is < 0 or > 63 ||
                    !IsAllowedMaskBit(decision, mask.BitIndex) ||
                    mask.BitValue != 1UL << mask.BitIndex ||
                    (availableMask & mask.BitValue) == 0 ||
                    !TryUInt32(
                        mask.BitIndex,
                        path + ".source_index",
                        out uint maskIndex,
                        out error))
                {
                    if (error is null)
                    {
                        error = new(
                            OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicValue,
                            path + ".bit_value");
                    }

                    return false;
                }

                bool directMask = decision switch
                {
                    FlatPromptRaceSelectionPublicContextV1 race =>
                        race.RequiredBitCount == 1,
                    FlatPromptAttributeSelectionPublicContextV1 attribute =>
                        attribute.RequiredBitCount == 1,
                    _ => false
                };
                descriptor = directMask
                    ? CreateDescriptor(
                        "announcement",
                        sourceIndex: maskIndex)
                    : CreateDescriptor(
                        "pick",
                        sourceIndex: maskIndex,
                        continuationOperation: "pick");
                return true;

            case FlatPromptCounterAmountPublicCandidateV1 counter:
                return TryMapCounter(
                    frame,
                    decision,
                    counter,
                    crossLocatorHandoff,
                    path,
                    out descriptor,
                    out error);

            case FlatPromptSortAnonymousPublicCandidateV1 sortAnonymous:
                return TryMapSort(
                    frame,
                    decision,
                    sortAnonymous.SourceOrdinal,
                    null,
                    sortAnonymous.SourceSection,
                    path,
                    sortAnonymous,
                    crossLocatorHandoff,
                    out descriptor,
                    out error);

            case FlatPromptSortLocatorPublicCandidateV1 sortLocator:
                return TryMapSort(
                    frame,
                    decision,
                    sortLocator.SourceOrdinal,
                    sortLocator.PublicSemanticCardLocator,
                    sortLocator.SourceSection,
                    path,
                    sortLocator,
                    crossLocatorHandoff,
                    out descriptor,
                    out error);

            default:
                return Unsupported(path, out descriptor, out error);
        }
    }

    private static bool TryMapIdleCard(
        PerspectiveSafeFrameV1 frame,
        FlatPromptPublicContextV1 decision,
        FlatIdleCardActionPublicCandidateBaseV1 candidate,
        I6DPrivateCrossLocatorBindingHandoffV1? crossLocatorHandoff,
        uint phase,
        string path,
        out OcgForgePublicActionDescriptorV1? descriptor,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        descriptor = null;
        error = null;
        if (!IsFamily(decision, 11) ||
            candidate.SourceSection != ExpectedIdleSourceSection(phase))
        {
            return Unsupported(path + ".source_section", out descriptor, out error);
        }

        if (!TryMapIdleReference(
                frame,
                candidate.PublicSemanticCardLocator,
                candidate,
                crossLocatorHandoff,
                path + ".source_reference",
                out OcgForgePublicCardReferenceV1 reference,
                out error) ||
            !TryUInt32(
                candidate.SourceOrdinal,
                path + ".source_index",
                out uint sourceIndex,
                out error))
        {
            return false;
        }

        descriptor = CreateDescriptor(
            "idle_command",
            new OcgForgePublicChoiceV1(
                OcgForgePublicChoiceKindV1.EffectChoice,
                sourceIndex,
                null),
            sourceReference: reference,
            phase: phase);
        return true;
    }

    private static bool TryMapIdleReference(
        PerspectiveSafeFrameV1 frame,
        PublicSemanticLocatorV1 locator,
        FlatPublicCandidateDescriptorV1 candidate,
        I6DPrivateCrossLocatorBindingHandoffV1? crossLocatorHandoff,
        string path,
        out OcgForgePublicCardReferenceV1 reference,
        out OcgForgePublicCandidateBridgeErrorV1? error)
        => TryMapReferenceWithHandoff(
            frame,
            locator,
            candidate,
            crossLocatorHandoff,
            path,
            out reference,
            out error);

    private static bool TryMapIdleActivate(
        PerspectiveSafeFrameV1 frame,
        FlatPromptPublicContextV1 decision,
        FlatIdleActivatablePublicCandidateBaseV1 candidate,
        I6DPrivateCrossLocatorBindingHandoffV1? crossLocatorHandoff,
        string path,
        out OcgForgePublicActionDescriptorV1? descriptor,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        descriptor = null;
        error = null;
        if (!IsFamily(decision, 11) ||
            candidate.SourceSection != FlatPromptSourceSectionV1.Activate)
        {
            return Unsupported(path + ".source_section", out descriptor, out error);
        }

        if (!TryMapIdleReference(
                frame,
                candidate.PublicSemanticCardLocator,
                candidate,
                crossLocatorHandoff,
                path + ".source_reference",
                out OcgForgePublicCardReferenceV1 reference,
                out error) ||
            !TryUInt32(
                candidate.SourceOrdinal,
                path + ".source_index",
                out uint sourceIndex,
                out error))
        {
            return false;
        }

        descriptor = CreateDescriptor(
            "idle_command",
            new OcgForgePublicChoiceV1(
                OcgForgePublicChoiceKindV1.EffectChoice,
                sourceIndex,
                null),
            sourceReference: reference,
            phase: 5);
        return true;
    }

    private static bool TryMapCardSelection(
        FlatPromptPublicContextV1 decision,
        int sourceOrdinal,
        PublicSemanticLocatorV1? locator,
        FlatPromptSourceSectionV1 sourceSection,
        string path,
        out OcgForgePublicActionDescriptorV1? descriptor,
        out OcgForgePublicCandidateBridgeErrorV1? error) =>
        TryMapCardSelection(
            null,
            decision,
            sourceOrdinal,
            locator,
            sourceSection,
            path,
            null,
            null,
            out descriptor,
            out error);

    private static bool TryMapCardSelection(
        PerspectiveSafeFrameV1? frame,
        FlatPromptPublicContextV1 decision,
        int sourceOrdinal,
        PublicSemanticLocatorV1? locator,
        FlatPromptSourceSectionV1 sourceSection,
        string path,
        FlatPublicCandidateDescriptorV1? candidate,
        I6DPrivateCrossLocatorBindingHandoffV1? crossLocatorHandoff,
        out OcgForgePublicActionDescriptorV1? descriptor,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        descriptor = null;
        error = null;
        if (decision is not FlatPromptCardSelectionPublicContextV1 cardContext ||
            sourceSection != FlatPromptSourceSectionV1.SelectCard)
        {
            return Unsupported(path + ".source_section", out descriptor, out error);
        }

        if (!TryUInt32(
                sourceOrdinal,
                path + ".source_index",
                out uint sourceIndex,
                out error) ||
            !TryOptionalReferenceWithHandoff(
                frame,
                locator,
                candidate,
                crossLocatorHandoff,
                path + ".source_reference",
                out OcgForgePublicCardReferenceV1? reference,
                out error))
        {
            return false;
        }

        bool direct = cardContext.MinimumCount == 1 &&
            cardContext.MaximumCount == 1;
        descriptor = direct
            ? CreateDescriptor(
                "card_selection",
                sourceReference: reference,
                sourceIndex: sourceIndex)
            : CreateDescriptor(
                "pick",
                sourceReference: reference,
                sourceIndex: sourceIndex,
                continuationOperation: "pick");
        return true;
    }

    private static bool TryMapContinuationCard(
        FlatPromptPublicContextV1 decision,
        int sourceOrdinal,
        PublicSemanticLocatorV1? locator,
        FlatPromptSourceSectionV1 sourceSection,
        string path,
        out OcgForgePublicActionDescriptorV1? descriptor,
        out OcgForgePublicCandidateBridgeErrorV1? error) =>
        TryMapContinuationCard(
            null,
            decision,
            sourceOrdinal,
            locator,
            sourceSection,
            path,
            null,
            null,
            out descriptor,
            out error);

    private static bool TryMapContinuationCard(
        PerspectiveSafeFrameV1? frame,
        FlatPromptPublicContextV1 decision,
        int sourceOrdinal,
        PublicSemanticLocatorV1? locator,
        FlatPromptSourceSectionV1 sourceSection,
        string path,
        FlatPublicCandidateDescriptorV1? candidate,
        I6DPrivateCrossLocatorBindingHandoffV1? crossLocatorHandoff,
        out OcgForgePublicActionDescriptorV1? descriptor,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        descriptor = null;
        error = null;
        if (decision is not FlatPromptTributeSelectionPublicContextV1 ||
            sourceSection != FlatPromptSourceSectionV1.SelectTribute)
        {
            return Unsupported(path + ".source_section", out descriptor, out error);
        }

        if (!TryUInt32(
                sourceOrdinal,
                path + ".source_index",
                out uint sourceIndex,
                out error) ||
            !TryOptionalReferenceWithHandoff(
                frame,
                locator,
                candidate,
                crossLocatorHandoff,
                path + ".source_reference",
                out OcgForgePublicCardReferenceV1? reference,
                out error))
        {
            return false;
        }

        descriptor = CreateDescriptor(
            "pick",
            sourceReference: reference,
            sourceIndex: sourceIndex,
            continuationOperation: "pick");
        return true;
    }

    private static bool TryMapSelectUnselect(
        FlatPromptPublicContextV1 decision,
        int sourceOrdinal,
        PublicSemanticLocatorV1? locator,
        FlatPromptSourceSectionV1 sourceSection,
        FlatPromptChoiceKindV1 choiceKind,
        string path,
        out OcgForgePublicActionDescriptorV1? descriptor,
        out OcgForgePublicCandidateBridgeErrorV1? error) =>
        TryMapSelectUnselect(
            null,
            decision,
            sourceOrdinal,
            locator,
            sourceSection,
            choiceKind,
            path,
            null,
            null,
            out descriptor,
            out error);

    private static bool TryMapSelectUnselect(
        PerspectiveSafeFrameV1? frame,
        FlatPromptPublicContextV1 decision,
        int sourceOrdinal,
        PublicSemanticLocatorV1? locator,
        FlatPromptSourceSectionV1 sourceSection,
        FlatPromptChoiceKindV1 choiceKind,
        string path,
        FlatPublicCandidateDescriptorV1? candidate,
        I6DPrivateCrossLocatorBindingHandoffV1? crossLocatorHandoff,
        out OcgForgePublicActionDescriptorV1? descriptor,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        descriptor = null;
        error = null;
        if (decision is not FlatPromptSelectUnselectCardPublicContextV1 selectContext ||
            !IsFamily(decision, 26) ||
            choiceKind is not
                (FlatPromptChoiceKindV1.Select or FlatPromptChoiceKindV1.Unselect) ||
            (choiceKind == FlatPromptChoiceKindV1.Select &&
                sourceSection != FlatPromptSourceSectionV1.Selectable) ||
            (choiceKind == FlatPromptChoiceKindV1.Unselect &&
                sourceSection != FlatPromptSourceSectionV1.Unselectable))
        {
            return Unsupported(path + ".source_section", out descriptor, out error);
        }

        int sourceCount = choiceKind == FlatPromptChoiceKindV1.Select
            ? selectContext.SelectableCount
            : selectContext.UnselectableCount;
        if (sourceOrdinal < 0 || sourceCount < 0 || sourceOrdinal >= sourceCount)
        {
            error = new(
                OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicValue,
                path + ".source_index");
            return false;
        }

        int publicSourceIndex = sourceOrdinal;
        if (choiceKind == FlatPromptChoiceKindV1.Unselect)
        {
            if (selectContext.SelectableCount > int.MaxValue - sourceOrdinal)
            {
                error = new(
                    OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicValue,
                    path + ".source_index");
                return false;
            }

            publicSourceIndex = selectContext.SelectableCount + sourceOrdinal;
        }

        if (!TryUInt32(
                publicSourceIndex,
                path + ".source_index",
                out uint sourceIndex,
                out error) ||
            !TryOptionalReferenceWithHandoff(
                frame,
                locator,
                candidate,
                crossLocatorHandoff,
                path + ".source_reference",
                out OcgForgePublicCardReferenceV1? reference,
                out error))
        {
            return false;
        }

        descriptor = CreateDescriptor(
            "card_selection",
            sourceReference: reference,
            sourceIndex: sourceIndex);
        return true;
    }

    private static bool TryMapFinishOrCancel(
        FlatPromptPublicContextV1 decision,
        string actionKind,
        string path,
        out OcgForgePublicActionDescriptorV1? descriptor,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        descriptor = null;
        error = null;
        bool direct = decision is FlatPromptSelectUnselectCardPublicContextV1 ||
            decision is FlatPromptCardSelectionPublicContextV1 cardContext &&
            cardContext.MinimumCount == 1 && cardContext.MaximumCount == 1;
        bool continuation = decision is FlatPromptCardSelectionPublicContextV1 ||
            decision is FlatPromptTributeSelectionPublicContextV1 ||
            decision is FlatPromptSelectUnselectCardPublicContextV1 ||
            decision is FlatPromptSortSelectionPublicContextV1;
        if (!direct && !continuation)
        {
            return Unsupported(path, out descriptor, out error);
        }

        descriptor = direct
            ? CreateDescriptor(actionKind)
            : CreateDescriptor(
                actionKind,
                continuationOperation: actionKind == "cancel" &&
                    decision is FlatPromptSortSelectionPublicContextV1
                    ? "bypass"
                    : actionKind);
        return true;
    }

    private static bool TryMapCounter(
        PerspectiveSafeFrameV1 frame,
        FlatPromptPublicContextV1 decision,
        FlatPromptCounterAmountPublicCandidateV1 candidate,
        I6DPrivateCrossLocatorBindingHandoffV1? crossLocatorHandoff,
        string path,
        out OcgForgePublicActionDescriptorV1? descriptor,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        descriptor = null;
        error = null;
        if (decision is not FlatPromptCounterSelectionPublicContextV1 counterContext ||
            candidate.SourceSection != FlatPromptSourceSectionV1.CounterSources ||
            candidate.SourceOrdinal < 0 ||
            candidate.SourceOrdinal >= counterContext.Sources.Count ||
            counterContext.Sources[candidate.SourceOrdinal].SourceOrdinal !=
                candidate.SourceOrdinal ||
            candidate.Amount < 0 ||
            candidate.Amount > counterContext.Sources[candidate.SourceOrdinal].Capacity ||
            !TryMapReferenceWithHandoff(
                frame,
                counterContext.Sources[candidate.SourceOrdinal].PublicSemanticCardLocator,
                candidate,
                crossLocatorHandoff,
                path + ".source_reference",
                out OcgForgePublicCardReferenceV1 reference,
                out error) ||
            !TryUInt32(
                candidate.SourceOrdinal,
                path + ".source_index",
                out uint sourceIndex,
                out error))
        {
            if (error is null)
            {
                error = new(
                    OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicValue,
                    path);
            }

            return false;
        }

        descriptor = CreateDescriptor(
            "assign_amount",
            sourceReference: reference,
            sourceIndex: sourceIndex,
            amount: candidate.Amount,
            continuationOperation: "amount");
        return true;
    }

    private static bool TryMapSort(
        PerspectiveSafeFrameV1 frame,
        FlatPromptPublicContextV1 decision,
        int sourceOrdinal,
        PublicSemanticLocatorV1? locator,
        FlatPromptSourceSectionV1 sourceSection,
        string path,
        FlatPublicCandidateDescriptorV1? candidate,
        I6DPrivateCrossLocatorBindingHandoffV1? crossLocatorHandoff,
        out OcgForgePublicActionDescriptorV1? descriptor,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        descriptor = null;
        error = null;
        if (decision is not FlatPromptSortSelectionPublicContextV1 sortContext)
        {
            return Unsupported(path, out descriptor, out error);
        }

        if ((sortContext.SortKind == FlatPromptSortKindV1.SortCard &&
                sourceSection != FlatPromptSourceSectionV1.SortCardSources) ||
            (sortContext.SortKind == FlatPromptSortKindV1.SortChain &&
                sourceSection != FlatPromptSourceSectionV1.SortChainSources))
        {
            return Unsupported(path + ".source_section", out descriptor, out error);
        }

        if (sourceOrdinal < 0 || sourceOrdinal >= sortContext.SourceCount ||
            sortContext.Sources[sourceOrdinal].SourceOrdinal != sourceOrdinal)
        {
            error = new(
                OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicValue,
                path + ".source_index");
            return false;
        }

        if (!TryUInt32(
                sourceOrdinal,
                path + ".source_index",
                out uint sourceIndex,
                out error) ||
            !TryOptionalReferenceWithHandoff(
                frame,
                locator,
                candidate,
                crossLocatorHandoff,
                path + ".source_reference",
                out OcgForgePublicCardReferenceV1? reference,
                out error))
        {
            return false;
        }

        descriptor = CreateDescriptor(
            "pick",
            sourceReference: reference,
            sourceIndex: sourceIndex,
            continuationOperation: "pick");
        return true;
    }

    private static bool IsEligiblePlace(
        FlatPromptPlaceSelectionPublicContextBaseV1 context,
        FlatPromptFieldPlacePublicCandidateV1 candidate) =>
        context.EligiblePlaces.Any(place =>
            place.AbsolutePlayer == candidate.AbsolutePlayer &&
            place.Zone == candidate.Zone &&
            place.Sequence == candidate.Sequence);

    private static bool IsAllowedMaskBit(
        FlatPromptPublicContextV1 context,
        int bitIndex) =>
        context switch
        {
            FlatPromptRaceSelectionPublicContextV1 =>
                bitIndex is >= 0 and <= 31 or 62,
            FlatPromptAttributeSelectionPublicContextV1 =>
                bitIndex is >= 0 and <= 6,
            _ => false
        };

    private static FlatPromptSourceSectionV1 ExpectedIdleSourceSection(
        uint phase) =>
        phase switch
        {
            0 => FlatPromptSourceSectionV1.Summon,
            1 => FlatPromptSourceSectionV1.SpecialSummon,
            2 => FlatPromptSourceSectionV1.Reposition,
            3 => FlatPromptSourceSectionV1.Mset,
            4 => FlatPromptSourceSectionV1.Sset,
            _ => (FlatPromptSourceSectionV1)byte.MaxValue
        };

    private static bool TryPlaceIndex(
        byte actingPlayer,
        FlatPromptFieldPlacePublicCandidateV1 candidate,
        string path,
        out uint sourceIndex,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        sourceIndex = 0;
        error = null;
        if (actingPlayer > 1 || candidate.AbsolutePlayer > 1)
        {
            error = new(
                OcgForgePublicCandidateBridgeErrorCodeV1.InvalidDecisionActor,
                path);
            return false;
        }

        int offset;
        if (candidate.Zone == FlatPromptFieldZoneV1.MonsterZone &&
            candidate.Sequence <= 6)
        {
            offset = candidate.AbsolutePlayer == actingPlayer ? 0 : 16;
        }
        else if (candidate.Zone == FlatPromptFieldZoneV1.SpellTrapZone &&
            candidate.Sequence <= 7)
        {
            offset = candidate.AbsolutePlayer == actingPlayer ? 8 : 24;
        }
        else
        {
            error = new(
                OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicValue,
                path);
            return false;
        }

        sourceIndex = checked((uint)(offset + candidate.Sequence));
        return true;
    }

    private static bool TryPosition(
        FlatPromptChoiceKindV1 choiceKind,
        byte position,
        string path,
        out byte value,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        value = position;
        error = null;
        byte expected = choiceKind switch
        {
            FlatPromptChoiceKindV1.FaceupAttack => 0x01,
            FlatPromptChoiceKindV1.FacedownAttack => 0x02,
            FlatPromptChoiceKindV1.FaceupDefense => 0x04,
            FlatPromptChoiceKindV1.FacedownDefense => 0x08,
            _ => (byte)0
        };
        if (expected == 0 || position != expected)
        {
            error = new(
                OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicValue,
                path);
            return false;
        }

        return true;
    }

    private static bool TryBooleanChoice(
        FlatPromptChoiceKindV1 choiceKind,
        OcgForgePublicChoiceKindV1 targetKind,
        string path,
        out OcgForgePublicChoiceV1 choice,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        error = null;
        choice = default;
        ulong value = choiceKind switch
        {
            FlatPromptChoiceKindV1.No => 0,
            FlatPromptChoiceKindV1.Yes => 1,
            _ => ulong.MaxValue
        };
        if (value == ulong.MaxValue)
        {
            error = new(
                OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicValue,
                path);
            return false;
        }

        choice = new(targetKind, value, null);
        return true;
    }

    private static bool TryUInt32(
        int value,
        string path,
        out uint result,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        result = 0;
        error = null;
        if (value < 0)
        {
            error = new(
                OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicValue,
                path);
            return false;
        }

        try
        {
            result = checked((uint)value);
            return true;
        }
        catch (OverflowException)
        {
            error = new(
                OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicValue,
                path);
            return false;
        }
    }

    private static bool TryOptionalReference(
        PerspectiveSafeFrameV1? frame,
        PublicSemanticLocatorV1? locator,
        string path,
        out OcgForgePublicCardReferenceV1? reference,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        reference = null;
        error = null;
        if (locator is null)
        {
            return true;
        }

        if (frame is null ||
            !TryMapReference(
                frame,
                locator,
                path,
                out OcgForgePublicCardReferenceV1 mapped,
                out error))
        {
            return false;
        }

        reference = mapped;
        return true;
    }

    private static bool TryOptionalReferenceWithHandoff(
        PerspectiveSafeFrameV1? frame,
        PublicSemanticLocatorV1? locator,
        FlatPublicCandidateDescriptorV1? candidate,
        I6DPrivateCrossLocatorBindingHandoffV1? crossLocatorHandoff,
        string path,
        out OcgForgePublicCardReferenceV1? reference,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        reference = null;
        error = null;
        if (locator is null)
        {
            return true;
        }

        if (!TryMapReferenceWithHandoff(
                frame,
                locator,
                candidate,
                crossLocatorHandoff,
                path,
                out OcgForgePublicCardReferenceV1 mapped,
                out error))
        {
            return false;
        }

        reference = mapped;
        return true;
    }

    private static bool TryMapReferenceWithHandoff(
        PerspectiveSafeFrameV1? frame,
        PublicSemanticLocatorV1 locator,
        FlatPublicCandidateDescriptorV1? candidate,
        I6DPrivateCrossLocatorBindingHandoffV1? crossLocatorHandoff,
        string path,
        out OcgForgePublicCardReferenceV1 reference,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        reference = default;
        error = null;
        PublicSemanticLocatorV1? target = null;
        I6DPrivateCrossLocatorHandoffErrorV1? handoffError = null;
        if (crossLocatorHandoff is not null && candidate is not null &&
            crossLocatorHandoff.TryGetValidatedTarget(
                candidate,
                frame,
                out target,
                out handoffError))
        {
            return target is not null &&
                TryMapReference(
                    frame!,
                    target,
                    path,
                    out reference,
                    out error);
        }

        if (crossLocatorHandoff is not null &&
            handoffError is not null &&
            handoffError.Value.Code !=
                I6DPrivateCrossLocatorHandoffErrorCodeV1.MissingBinding)
        {
            error = new(
                handoffError.Value.Code ==
                    I6DPrivateCrossLocatorHandoffErrorCodeV1.AmbiguousBinding
                    ? OcgForgePublicCandidateBridgeErrorCodeV1
                        .AmbiguousPublicReference
                    : OcgForgePublicCandidateBridgeErrorCodeV1
                        .InvalidPublicReference,
                path);
            return false;
        }

        // MissingBinding is the intentional exact-token fast path. Any other
        // handoff result is authoritative and must not be bypassed by mapping
        // the I4 locator directly.
        if (frame is null)
        {
            error = new(
                OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicReference,
                path);
            return false;
        }

        return TryMapReference(frame, locator, path, out reference, out error);
    }

    internal static bool TryValidateExactI6DReferences(
        PerspectiveSafeFrameV1 frame,
        FlatPromptPublicContextV1 decision,
        IReadOnlyList<FlatPublicCandidateDescriptorV1> candidates,
        out string missingHandoffPath)
    {
        missingHandoffPath = "candidates";
        for (int index = 0; index < candidates.Count; index++)
        {
            if (!TryGetCandidateReference(
                    decision,
                    candidates[index],
                    out PublicSemanticLocatorV1? locator) ||
                locator is null)
            {
                continue;
            }

            string path = $"candidates[{index}].source_reference";
            if (!TryMapReference(
                    frame,
                    locator,
                    path,
                    out _,
                    out _))
            {
                missingHandoffPath = path;
                return false;
            }
        }

        return true;
    }

    private static bool TryGetCandidateReference(
        FlatPromptPublicContextV1 decision,
        FlatPublicCandidateDescriptorV1 candidate,
        out PublicSemanticLocatorV1? locator)
    {
        locator = candidate switch
        {
            FlatChainEntryPublicCandidateDescriptorBaseV1 chain =>
                chain.PublicSemanticCardLocator,
            FlatBattleActivatablePublicCandidateBaseV1 activate =>
                activate.PublicSemanticCardLocator,
            FlatBattleAttackPublicCandidateBaseV1 attack =>
                attack.PublicSemanticCardLocator,
            FlatIdleCardActionPublicCandidateBaseV1 idle =>
                idle.PublicSemanticCardLocator,
            FlatIdleActivatablePublicCandidateBaseV1 idleActivate =>
                idleActivate.PublicSemanticCardLocator,
            FlatPromptCardSelectionLocatorCandidateV1 cardSelection =>
                cardSelection.PublicSemanticCardLocator,
            FlatPromptCardSelectionLocatorPromptCodeCandidateV1
                cardSelectionPromptCode =>
                cardSelectionPromptCode.PublicSemanticCardLocator,
            FlatPromptTributeSelectionLocatorCandidateV1 tributeSelection =>
                tributeSelection.PublicSemanticCardLocator,
            FlatPromptTributeSelectionLocatorPromptCodeCandidateV1
                tributeSelectionPromptCode =>
                tributeSelectionPromptCode.PublicSemanticCardLocator,
            FlatPromptSelectUnselectLocatorCandidateV1 selectUnselect =>
                selectUnselect.PublicSemanticCardLocator,
            FlatPromptSelectUnselectLocatorPromptCodeCandidateV1
                selectUnselectPromptCode =>
                selectUnselectPromptCode.PublicSemanticCardLocator,
            FlatPromptSortLocatorPublicCandidateV1 sort =>
                sort.PublicSemanticCardLocator,
            FlatPromptSortLocatorPromptCodePublicCandidateV1 sortPromptCode =>
                sortPromptCode.PublicSemanticCardLocator,
            FlatEffectYnPublicCandidateDescriptorV1
                when decision is FlatPromptEffectYnPublicContextBaseV1 effect =>
                effect.EffectCardLocator,
            FlatPromptCounterAmountPublicCandidateV1 counter
                when decision is FlatPromptCounterSelectionPublicContextV1
                    counterContext &&
                counter.SourceOrdinal >= 0 &&
                counter.SourceOrdinal < counterContext.Sources.Count =>
                counterContext.Sources[counter.SourceOrdinal]
                    .PublicSemanticCardLocator,
            _ => null
        };
        return locator is not null;
    }

    private static bool TryMapReference(
        PerspectiveSafeFrameV1 frame,
        PublicSemanticLocatorV1 locator,
        string path,
        out OcgForgePublicCardReferenceV1 reference,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        reference = default;
        error = null;
        if (!PublicSemanticLocatorV1.TryParse(locator.Value, out PublicSemanticLocatorV1? parsed) ||
            parsed is null)
        {
            error = new(
                OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicReference,
                path);
            return false;
        }

        PerspectiveSafeEntityV1[] matches = frame.Entities
            .Where(entity => string.Equals(
                entity.Locator,
                parsed.Value,
                StringComparison.Ordinal))
            .ToArray();
        if (matches.Length == 0)
        {
            error = new(
                OcgForgePublicCandidateBridgeErrorCodeV1.InvalidPublicReference,
                path);
            return false;
        }

        if (matches.Length != 1)
        {
            error = new(
                OcgForgePublicCandidateBridgeErrorCodeV1.AmbiguousPublicReference,
                path);
            return false;
        }

        reference = new(
            matches[0].IdentityKnown
                ? OcgForgePublicCardReferenceKindV1.VisibleCard
                : OcgForgePublicCardReferenceKindV1.RedactedSlot,
            parsed.Value);
        return true;
    }

    private static bool IsFamily(
        FlatPromptPublicContextV1 context,
        byte family) =>
        (byte)context.PromptFamily == family;

    private static string RequestKind(FlatPromptFamilyV1 family) =>
        (byte)family switch
        {
            10 => "battle_command",
            11 => "idle_command",
            12 or 13 => "yes_no",
            14 => "option",
            15 => "card_selection",
            16 => "chain",
            18 or 24 => "place",
            19 => "position",
            20 => "tribute",
            21 or 25 => "ordering",
            22 => "counter",
            26 => "unselect_card",
            140 or 141 or 143 => "announcement",
            _ => string.Empty
        };

    private static bool HasForbiddenPromptLocalCardCode(
        FlatPublicCandidateDescriptorV1 candidate) =>
        candidate switch
        {
            FlatChainCardCodePublicCandidateDescriptorV1 => true,
            FlatBattleActivatableCardCodePublicCandidateV1 => true,
            FlatBattleAttackCardCodePublicCandidateV1 => true,
            // These six variants already carry the safe public locator used by
            // TryMapIdleCard/TryMapIdleActivate. The prompt-local CardCode is
            // deliberately ignored by the public descriptor/key mapping.
            FlatIdleSummonCardCodePublicCandidateV1 => false,
            FlatIdleSpecialSummonCardCodePublicCandidateV1 => false,
            FlatIdleRepositionCardCodePublicCandidateV1 => false,
            FlatIdleMsetCardCodePublicCandidateV1 => false,
            FlatIdleSsetCardCodePublicCandidateV1 => false,
            FlatIdleActivatableCardCodePublicCandidateV1 => false,
            FlatPromptCardSelectionPromptCodeCandidateV1 => true,
            FlatPromptCardSelectionLocatorPromptCodeCandidateV1 => true,
            FlatPromptTributeSelectionPromptCodeCandidateV1 => true,
            FlatPromptTributeSelectionLocatorPromptCodeCandidateV1 => true,
            FlatPromptSelectUnselectPromptCodeCandidateV1 => true,
            FlatPromptSelectUnselectLocatorPromptCodeCandidateV1 => true,
            FlatPromptSortPromptCodePublicCandidateV1 => true,
            FlatPromptSortLocatorPromptCodePublicCandidateV1 => true,
            _ => false
        };

    private static OcgForgePublicActionDescriptorV1 CreateDescriptor(
        string actionKind,
        OcgForgePublicChoiceV1? choice = null,
        OcgForgePublicCardReferenceV1? sourceReference = null,
        OcgForgePublicCardReferenceV1? targetReference = null,
        uint? phase = null,
        byte? position = null,
        uint? sourceIndex = null,
        int? amount = null,
        string continuationOperation = "") =>
        new(
            actionKind,
            choice,
            sourceReference,
            targetReference,
            phase,
            position,
            sourceIndex,
            amount,
            continuationOperation);

    private static bool Unsupported(
        string path,
        out OcgForgePublicActionDescriptorV1? descriptor,
        out OcgForgePublicCandidateBridgeErrorV1? error)
    {
        descriptor = null;
        error = new(
            OcgForgePublicCandidateBridgeErrorCodeV1.UnsupportedCandidate,
            path);
        return false;
    }

    private static OcgForgePublicDecisionContextResultV1 Failure(
        OcgForgePublicCandidateBridgeErrorCodeV1 code,
        string path) =>
        OcgForgePublicDecisionContextResultV1.Failure(code, path);

    // Accepted I4/I5 continuation semantics expose exactly two intermediate
    // operations. All other admitted operations are atomic or terminal. This
    // is the complete public transition classification needed by P5; no
    // private response bytes or continuation instance crosses the boundary.
    private static bool TryGetSubmitsEngineResponse(
        string continuationOperation,
        out bool submitsEngineResponse)
    {
        switch (continuationOperation)
        {
            case "pick":
            case "amount":
                submitsEngineResponse = false;
                return true;
            case "":
            case "finish":
            case "cancel":
            case "bypass":
                submitsEngineResponse = true;
                return true;
            default:
                submitsEngineResponse = false;
                return false;
        }
    }
}
