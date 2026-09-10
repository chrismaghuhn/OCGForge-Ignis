namespace OCGForge.Ignis.Gameplay;

public enum I6DPrivateCrossLocatorHandoffErrorCodeV1 : byte
{
    None = 0,
    InvalidInput = 1,
    FrameMismatch = 2,
    ProjectionMismatch = 3,
    StaleFrame = 4,
    StalePrompt = 5,
    InvalidCandidate = 6,
    MissingBinding = 7,
    AmbiguousBinding = 8,
    TargetUnavailable = 9,
    CandidateDomainMismatch = 10
}

public readonly record struct I6DPrivateCrossLocatorHandoffErrorV1(
    I6DPrivateCrossLocatorHandoffErrorCodeV1 Code,
    string FieldPath);

/// <summary>
/// Opaque acceptance lease spanning the private frame and prompt authorities.
/// The Model assembly can hold and dispose it, but cannot inspect or construct
/// either authority.
/// </summary>
public sealed class I6DBoundaryAcceptanceLeaseV1 : IDisposable
{
    private PrivateGameplayFrameAuthorityLeaseV1? frameLease;
    private PrivateFlatPromptBindingLifetimeLeaseV1? promptLease;

    internal I6DBoundaryAcceptanceLeaseV1(
        PrivateGameplayFrameAuthorityLeaseV1 frameLease,
        PrivateFlatPromptBindingLifetimeLeaseV1 promptLease)
    {
        this.frameLease = frameLease ??
            throw new ArgumentNullException(nameof(frameLease));
        this.promptLease = promptLease ??
            throw new ArgumentNullException(nameof(promptLease));
    }

    public void Dispose()
    {
        PrivateFlatPromptBindingLifetimeLeaseV1? prompt =
            Interlocked.Exchange(ref promptLease, null);
        PrivateGameplayFrameAuthorityLeaseV1? frame =
            Interlocked.Exchange(ref frameLease, null);
        prompt?.Dispose();
        frame?.Dispose();
    }
}

internal sealed class PrivateCrossLocatorBindingV1
{
    internal PrivateCrossLocatorBindingV1(
        string candidateKey,
        FlatPromptSourceSectionV1 sourceSection,
        int sourceOrdinal,
        PrivateI4OccurrenceKeyV1 occurrenceKey,
        PublicSemanticLocatorV1 i4Locator,
        PublicSemanticLocatorV1 targetLocator)
    {
        I4LocalCandidateKey = candidateKey ??
            throw new ArgumentNullException(nameof(candidateKey));
        SourceSection = sourceSection;
        SourceOrdinal = sourceOrdinal;
        OccurrenceKey = occurrenceKey;
        I4Locator = i4Locator ?? throw new ArgumentNullException(nameof(i4Locator));
        TargetLocator = targetLocator ??
            throw new ArgumentNullException(nameof(targetLocator));
    }

    internal string I4LocalCandidateKey { get; }

    internal FlatPromptSourceSectionV1 SourceSection { get; }

    internal int SourceOrdinal { get; }

    internal PrivateI4OccurrenceKeyV1 OccurrenceKey { get; }

    internal PublicSemanticLocatorV1 I4Locator { get; }

    internal PublicSemanticLocatorV1 TargetLocator { get; }
}

/// <summary>
/// Public opaque bridge for an exact same-frame I4-to-I6C5 locator mapping.
/// Its constructor and all private provenance remain Gameplay-owned.
/// </summary>
public sealed class I6DPrivateCrossLocatorBindingHandoffV1
{
    private readonly PrivateGameplayFrameAuthorityV1 frameAuthority;
    private readonly PrivateFlatPromptBindingLifetimeAuthorityV1 promptAuthority;
    private readonly PerspectiveSafeFrameV1 acceptedFrame;
    private readonly FlatPromptProjectionResultV1 acceptedProjection;
    private readonly ulong frameInstanceOrdinal;
    private readonly ulong promptInstanceOrdinal;
    private readonly int continuationStep;
    private readonly string acceptedPublicProjectionId;
    private readonly FlatPublicCandidateDescriptorV1[] acceptedCandidates;
    private readonly Dictionary<string, PrivateCrossLocatorBindingV1>
        bindingByCandidateKey;

    private I6DPrivateCrossLocatorBindingHandoffV1(
        PrivateGameplayFrameAuthorityV1 frameAuthority,
        PrivateFlatPromptBindingLifetimeAuthorityV1 promptAuthority,
        PerspectiveSafeFrameV1 acceptedFrame,
        FlatPromptProjectionResultV1 acceptedProjection,
        CurrentFlatPromptBindingV1 promptBinding,
        string acceptedPublicProjectionId,
        IEnumerable<FlatPublicCandidateDescriptorV1> acceptedCandidates,
        IEnumerable<PrivateCrossLocatorBindingV1> bindings)
    {
        this.frameAuthority = frameAuthority ??
            throw new ArgumentNullException(nameof(frameAuthority));
        this.promptAuthority = promptAuthority ??
            throw new ArgumentNullException(nameof(promptAuthority));
        this.acceptedFrame = acceptedFrame ??
            throw new ArgumentNullException(nameof(acceptedFrame));
        this.acceptedProjection = acceptedProjection ??
            throw new ArgumentNullException(nameof(acceptedProjection));
        ArgumentNullException.ThrowIfNull(promptBinding);
        this.frameInstanceOrdinal = frameAuthority.FrameInstanceOrdinal;
        promptInstanceOrdinal = promptBinding.PromptInstanceOrdinal;
        continuationStep = promptBinding.ContinuationStep;
        this.acceptedPublicProjectionId =
            acceptedPublicProjectionId ??
            throw new ArgumentNullException(nameof(acceptedPublicProjectionId));
        this.acceptedCandidates = acceptedCandidates.ToArray();
        bindingByCandidateKey = new Dictionary<string,
            PrivateCrossLocatorBindingV1>(StringComparer.Ordinal);
        foreach (PrivateCrossLocatorBindingV1 binding in bindings)
        {
            if (!bindingByCandidateKey.TryAdd(
                    binding.I4LocalCandidateKey,
                    binding))
            {
                throw new ArgumentException(
                    "Cross-locator candidate keys must be unique.",
                    nameof(bindings));
            }
        }
    }

    /// <summary>
    /// Acquires the current frame and prompt leases and keeps both held for
    /// the caller's boundary-construction transaction.
    /// </summary>
    public bool TryAcquireBoundaryAcceptanceLease(
        PerspectiveSafeFrameV1? acceptedPublicFrame,
        FlatPromptProjectionResultV1? acceptedPublicProjection,
        out I6DBoundaryAcceptanceLeaseV1? lease,
        out I6DPrivateCrossLocatorHandoffErrorV1? error)
    {
        lease = null;
        error = null;
        if (acceptedPublicFrame is null || acceptedPublicProjection is null)
        {
            error = Failure(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.InvalidInput,
                "accepted_public_boundary");
            return false;
        }

        if (!ReferenceEquals(acceptedPublicFrame, acceptedFrame))
        {
            error = Failure(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.FrameMismatch,
                "accepted_public_frame");
            return false;
        }

        if (!ReferenceEquals(acceptedPublicProjection, acceptedProjection))
        {
            error = Failure(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.ProjectionMismatch,
                "accepted_public_projection");
            return false;
        }

        if (!TryAcquireAuthorities(
                out PrivateGameplayFrameAuthorityLeaseV1? frameLease,
                out PrivateFlatPromptBindingLifetimeLeaseV1? promptLease,
                out I6DPrivateCrossLocatorHandoffErrorV1? acquireError))
        {
            error = acquireError;
            return false;
        }

        if (!ValidateStoredProjection())
        {
            promptLease!.Dispose();
            frameLease!.Dispose();
            error = Failure(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.CandidateDomainMismatch,
                "binding");
            return false;
        }

        lease = new I6DBoundaryAcceptanceLeaseV1(
            frameLease!,
            promptLease!);
        return true;
    }

    /// <summary>
    /// Resolves only an already accepted public candidate to its existing safe
    /// I6C5 locator. Lifecycle coordinates and private occurrence data never
    /// cross this method boundary.
    /// </summary>
    public bool TryGetValidatedTarget(
        FlatPublicCandidateDescriptorV1? acceptedPublicCandidate,
        PerspectiveSafeFrameV1? currentAcceptedPublicFrame,
        out PublicSemanticLocatorV1? acceptedI6C5TargetLocator,
        out I6DPrivateCrossLocatorHandoffErrorV1? error)
    {
        acceptedI6C5TargetLocator = null;
        error = null;
        if (acceptedPublicCandidate is null || currentAcceptedPublicFrame is null)
        {
            error = Failure(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.InvalidInput,
                "candidate_or_frame");
            return false;
        }

        if (!ReferenceEquals(currentAcceptedPublicFrame, acceptedFrame))
        {
            error = Failure(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.FrameMismatch,
                "current_accepted_public_frame");
            return false;
        }

        if (!TryAcquireAuthorities(
                out PrivateGameplayFrameAuthorityLeaseV1? frameLease,
                out PrivateFlatPromptBindingLifetimeLeaseV1? promptLease,
                out I6DPrivateCrossLocatorHandoffErrorV1? acquireError))
        {
            error = acquireError;
            return false;
        }

        using (frameLease!)
        using (promptLease!)
        {
            if (!ValidateStoredProjection())
            {
                error = Failure(
                    I6DPrivateCrossLocatorHandoffErrorCodeV1.CandidateDomainMismatch,
                    "binding");
                return false;
            }

            PrivateCrossLocatorBindingV1? binding =
                FindCandidateBinding(acceptedPublicCandidate);
            if (binding is null)
            {
                error = Failure(
                    I6DPrivateCrossLocatorHandoffErrorCodeV1.MissingBinding,
                    "candidate");
                return false;
            }

            if (!TryFindUniqueFrameLocator(
                    currentAcceptedPublicFrame,
                    binding.TargetLocator,
                    out _,
                    out bool ambiguous))
            {
                error = Failure(
                    ambiguous
                        ? I6DPrivateCrossLocatorHandoffErrorCodeV1.AmbiguousBinding
                        : I6DPrivateCrossLocatorHandoffErrorCodeV1.TargetUnavailable,
                    "candidate.target_reference");
                return false;
            }

            acceptedI6C5TargetLocator = binding.TargetLocator;
            return true;
        }
    }

    internal static bool TryCreate(
        PrivateGameplayFrameAuthorityV1 frameAuthority,
        CurrentFlatPromptBindingV1 binding,
        FlatPromptProjectionResultV1 acceptedProjection,
        PublicStateProjectionResultV1 acceptedI4Projection,
        PerspectiveSafeFrameV1 acceptedPublicFrame,
        MirrorSnapshotV1 mirrorSnapshot,
        PrivateI6C3LocatorMapV1 locatorMap,
        out I6DPrivateCrossLocatorBindingHandoffV1? handoff,
        out I6DPrivateCrossLocatorHandoffErrorV1? error)
    {
        handoff = null;
        error = null;
        if (frameAuthority is null ||
            binding is null ||
            acceptedProjection is null ||
            acceptedI4Projection is null ||
            acceptedPublicFrame is null ||
            mirrorSnapshot is null ||
            locatorMap is null)
        {
            error = Failure(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.InvalidInput,
                "composition");
            return false;
        }

        PrivateFlatPromptBindingLifetimeAuthorityV1? promptAuthority =
            binding.PromptLifetimeAuthority;
        if (promptAuthority is null ||
            !ReferenceEquals(binding.FrameAuthority, frameAuthority) ||
            !binding.MatchesPublicProjection(acceptedProjection) ||
            !acceptedProjection.IsSuccess ||
            acceptedProjection.Candidates is null ||
            !acceptedI4Projection.IsSuccess ||
            acceptedI4Projection.Snapshot is null ||
            acceptedI4Projection.PrivateOccurrenceSidecar is null ||
            !string.Equals(
                acceptedI4Projection.PrivateOccurrenceSidecar
                    .AcceptedPublicProjectionId,
                acceptedI4Projection.PublicProjectionId,
                StringComparison.Ordinal) ||
            acceptedI4Projection.PrivateOccurrenceSidecar.FrameInstanceOrdinal !=
                frameAuthority.FrameInstanceOrdinal)
        {
            error = Failure(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.CandidateDomainMismatch,
                "composition");
            return false;
        }

        PrivateFlatPromptBindingLifetimeLeaseV1? promptLease =
            promptAuthority.TryAcquire();
        if (promptLease is null)
        {
            error = Failure(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.StalePrompt,
                "prompt");
            return false;
        }

        try
        {
            if (!TryBuildBindings(
                    acceptedProjection.Candidates,
                    acceptedProjection.Context!,
                    acceptedI4Projection,
                    acceptedPublicFrame,
                    mirrorSnapshot,
                    locatorMap,
                    out List<PrivateCrossLocatorBindingV1> bindings,
                    out error))
            {
                return false;
            }

            if (bindings.Count == 0)
            {
                return true;
            }

            handoff = new I6DPrivateCrossLocatorBindingHandoffV1(
                frameAuthority,
                promptAuthority,
                acceptedPublicFrame,
                acceptedProjection,
                binding,
                acceptedI4Projection.PublicProjectionId!,
                acceptedProjection.Candidates,
                bindings);
            return true;
        }
        finally
        {
            promptLease.Dispose();
        }
    }

    private bool TryAcquireAuthorities(
        out PrivateGameplayFrameAuthorityLeaseV1? frameLease,
        out PrivateFlatPromptBindingLifetimeLeaseV1? promptLease,
        out I6DPrivateCrossLocatorHandoffErrorV1? error)
    {
        frameLease = frameAuthority.TryAcquire();
        promptLease = null;
        error = null;
        if (frameLease is null)
        {
            error = Failure(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.StaleFrame,
                "frame");
            return false;
        }

        promptLease = promptAuthority.TryAcquire();
        if (promptLease is null)
        {
            frameLease.Dispose();
            frameLease = null;
            error = Failure(
                I6DPrivateCrossLocatorHandoffErrorCodeV1.StalePrompt,
                "prompt");
            return false;
        }

        return true;
    }

    private bool ValidateStoredProjection()
    {
        return acceptedProjection.IsSuccess &&
            acceptedProjection.Context is not null &&
            acceptedProjection.Candidates is not null &&
            acceptedProjection.Candidates.Count == acceptedCandidates.Length &&
            frameAuthority.FrameInstanceOrdinal == frameInstanceOrdinal &&
            !string.IsNullOrEmpty(acceptedPublicProjectionId) &&
            acceptedProjection.Candidates.SequenceEqual(acceptedCandidates);
    }

    private PrivateCrossLocatorBindingV1? FindCandidateBinding(
        FlatPublicCandidateDescriptorV1 candidate)
    {
        if (!acceptedCandidates.Any(
                accepted => ReferenceEquals(accepted, candidate)))
        {
            return null;
        }

        if (!bindingByCandidateKey.TryGetValue(
                candidate.I4LocalCandidateKey,
                out PrivateCrossLocatorBindingV1? binding) ||
            !string.Equals(
                binding.I4LocalCandidateKey,
                candidate.I4LocalCandidateKey,
                StringComparison.Ordinal))
        {
            return null;
        }

        return binding;
    }

    private static bool TryBuildBindings(
        IReadOnlyList<FlatPublicCandidateDescriptorV1> candidates,
        FlatPromptPublicContextV1 decision,
        PublicStateProjectionResultV1 acceptedI4Projection,
        PerspectiveSafeFrameV1 acceptedPublicFrame,
        MirrorSnapshotV1 mirrorSnapshot,
        PrivateI6C3LocatorMapV1 locatorMap,
        out List<PrivateCrossLocatorBindingV1> bindings,
        out I6DPrivateCrossLocatorHandoffErrorV1? error)
    {
        bindings = new List<PrivateCrossLocatorBindingV1>();
        error = null;
        PrivateI4OccurrencePublicLocatorSidecarV1 sidecar =
            acceptedI4Projection.PrivateOccurrenceSidecar!;
        Dictionary<string, PrivateI4OccurrenceKeyV1> mappedTargetOccurrences =
            new(StringComparer.Ordinal);
        HashSet<string> mappedCandidateKeys =
            new(StringComparer.Ordinal);
        foreach (FlatPublicCandidateDescriptorV1 candidate in candidates)
        {
            if (!TryGetCandidateLocator(
                    candidate,
                    out PublicSemanticLocatorV1? i4Locator,
                    out FlatPromptSourceSectionV1 sourceSection,
                    out int sourceOrdinal,
                    decision))
            {
                continue;
            }

            if (!mappedCandidateKeys.Add(candidate.I4LocalCandidateKey))
            {
                error = Failure(
                    I6DPrivateCrossLocatorHandoffErrorCodeV1.AmbiguousBinding,
                    "candidate.i4_local_candidate_key");
                return false;
            }

            PrivateI4OccurrencePublicLocatorSidecarEntryV1[] entries = sidecar
                .Entries
                .Where(entry => entry.AcceptedI4PublicLocator.Equals(i4Locator))
                .ToArray();
            if (entries.Length == 0)
            {
                if (!TryFindUniqueFrameLocator(
                        acceptedPublicFrame,
                        i4Locator!,
                        out _,
                        out bool exactLocatorAmbiguous))
                {
                    error = Failure(
                        exactLocatorAmbiguous
                            ? I6DPrivateCrossLocatorHandoffErrorCodeV1
                                .AmbiguousBinding
                            : I6DPrivateCrossLocatorHandoffErrorCodeV1
                                .MissingBinding,
                        "candidate.source_reference");
                    return false;
                }

                continue;
            }

            if (entries.Length != 1)
            {
                error = Failure(
                    I6DPrivateCrossLocatorHandoffErrorCodeV1.AmbiguousBinding,
                    "candidate.source_reference");
                return false;
            }

            PrivateI4OccurrencePublicLocatorSidecarEntryV1 entry = entries[0];
            if (!TryFindMirrorOccurrence(
                    mirrorSnapshot,
                    entry,
                    out MirrorCardSnapshotV1? card))
            {
                error = Failure(
                    I6DPrivateCrossLocatorHandoffErrorCodeV1.TargetUnavailable,
                    "candidate.source_occurrence");
                return false;
            }

            MirrorCardSnapshotV1 occurrence = card!;
            if (!locatorMap.TryGet(occurrence.EntityId, out PublicSemanticLocatorV1? target) ||
                target is null)
            {
                error = Failure(
                    I6DPrivateCrossLocatorHandoffErrorCodeV1.TargetUnavailable,
                    "candidate.target_reference");
                return false;
            }

            if (!TryFindUniqueFrameLocator(
                    acceptedPublicFrame,
                    target,
                    out _,
                    out bool ambiguous))
            {
                error = Failure(
                    ambiguous
                        ? I6DPrivateCrossLocatorHandoffErrorCodeV1.AmbiguousBinding
                        : I6DPrivateCrossLocatorHandoffErrorCodeV1.TargetUnavailable,
                    "candidate.target_reference");
                return false;
            }

            if (mappedTargetOccurrences.TryGetValue(
                    target.Value,
                    out PrivateI4OccurrenceKeyV1 existingOccurrence) &&
                existingOccurrence != entry.OccurrenceKey)
            {
                error = Failure(
                    I6DPrivateCrossLocatorHandoffErrorCodeV1.AmbiguousBinding,
                    "candidate.target_reference");
                return false;
            }

            // A single current occurrence may support several distinct legal
            // actions. Only a target reused by a different occurrence is a
            // mapping collision.
            mappedTargetOccurrences[target.Value] = entry.OccurrenceKey;

            if (target.Equals(i4Locator))
            {
                continue;
            }

            bindings.Add(
                new PrivateCrossLocatorBindingV1(
                    candidate.I4LocalCandidateKey,
                    sourceSection,
                    sourceOrdinal,
                    entry.OccurrenceKey,
                    i4Locator!,
                    target));
        }

        return true;
    }

    private static bool TryGetCandidateLocator(
        FlatPublicCandidateDescriptorV1 candidate,
        out PublicSemanticLocatorV1? locator,
        out FlatPromptSourceSectionV1 sourceSection,
        out int sourceOrdinal,
        FlatPromptPublicContextV1 context)
    {
        sourceSection = default;
        sourceOrdinal = default;
        locator = candidate switch
        {
            FlatIdleCardActionPublicCandidateBaseV1 card =>
                SetCandidateCoordinates(
                    card.PublicSemanticCardLocator,
                    card.SourceSection,
                    card.SourceOrdinal,
                    out sourceSection,
                    out sourceOrdinal),
            FlatIdleActivatablePublicCandidateBaseV1 activate =>
                SetCandidateCoordinates(
                    activate.PublicSemanticCardLocator,
                    activate.SourceSection,
                    activate.SourceOrdinal,
                    out sourceSection,
                    out sourceOrdinal),
            FlatChainEntryPublicCandidateDescriptorBaseV1 chain =>
                SetCandidateCoordinates(
                    chain.PublicSemanticCardLocator,
                    chain.SourceSection,
                    chain.SourceOrdinal,
                    out sourceSection,
                    out sourceOrdinal),
            FlatBattleActivatablePublicCandidateBaseV1 battleActivate =>
                SetCandidateCoordinates(
                    battleActivate.PublicSemanticCardLocator,
                    battleActivate.SourceSection,
                    battleActivate.SourceOrdinal,
                    out sourceSection,
                    out sourceOrdinal),
            FlatBattleAttackPublicCandidateBaseV1 battleAttack =>
                SetCandidateCoordinates(
                    battleAttack.PublicSemanticCardLocator,
                    battleAttack.SourceSection,
                    battleAttack.SourceOrdinal,
                    out sourceSection,
                    out sourceOrdinal),
            FlatPromptCardSelectionLocatorCandidateV1 cardSelection =>
                SetCandidateCoordinates(
                    cardSelection.PublicSemanticCardLocator,
                    cardSelection.SourceSection,
                    cardSelection.SourceOrdinal,
                    out sourceSection,
                    out sourceOrdinal),
            FlatPromptCardSelectionLocatorPromptCodeCandidateV1
                cardSelectionPromptCode =>
                SetCandidateCoordinates(
                    cardSelectionPromptCode.PublicSemanticCardLocator,
                    cardSelectionPromptCode.SourceSection,
                    cardSelectionPromptCode.SourceOrdinal,
                    out sourceSection,
                    out sourceOrdinal),
            FlatPromptTributeSelectionLocatorCandidateV1 tributeSelection =>
                SetCandidateCoordinates(
                    tributeSelection.PublicSemanticCardLocator,
                    tributeSelection.SourceSection,
                    tributeSelection.SourceOrdinal,
                    out sourceSection,
                    out sourceOrdinal),
            FlatPromptTributeSelectionLocatorPromptCodeCandidateV1
                tributeSelectionPromptCode =>
                SetCandidateCoordinates(
                    tributeSelectionPromptCode.PublicSemanticCardLocator,
                    tributeSelectionPromptCode.SourceSection,
                    tributeSelectionPromptCode.SourceOrdinal,
                    out sourceSection,
                    out sourceOrdinal),
            FlatPromptSelectUnselectLocatorCandidateV1 selectUnselect =>
                SetCandidateCoordinates(
                    selectUnselect.PublicSemanticCardLocator,
                    selectUnselect.SourceSection,
                    selectUnselect.SourceOrdinal,
                    out sourceSection,
                    out sourceOrdinal),
            FlatPromptSelectUnselectLocatorPromptCodeCandidateV1
                selectUnselectPromptCode =>
                SetCandidateCoordinates(
                    selectUnselectPromptCode.PublicSemanticCardLocator,
                    selectUnselectPromptCode.SourceSection,
                    selectUnselectPromptCode.SourceOrdinal,
                    out sourceSection,
                    out sourceOrdinal),
            FlatPromptSortLocatorPublicCandidateV1 sort =>
                SetCandidateCoordinates(
                    sort.PublicSemanticCardLocator,
                    sort.SourceSection,
                    sort.SourceOrdinal,
                    out sourceSection,
                    out sourceOrdinal),
            FlatPromptSortLocatorPromptCodePublicCandidateV1
                sortPromptCode =>
                SetCandidateCoordinates(
                    sortPromptCode.PublicSemanticCardLocator,
                    sortPromptCode.SourceSection,
                    sortPromptCode.SourceOrdinal,
                    out sourceSection,
                    out sourceOrdinal),
            FlatEffectYnPublicCandidateDescriptorV1
                when context is FlatPromptEffectYnPublicContextBaseV1 effect =>
                SetCandidateCoordinates(
                    effect.EffectCardLocator,
                    FlatPromptSourceSectionV1.Activatable,
                    0,
                    out sourceSection,
                    out sourceOrdinal),
            FlatPromptCounterAmountPublicCandidateV1 counter
                when context is FlatPromptCounterSelectionPublicContextV1
                    counterContext &&
                counter.SourceOrdinal >= 0 &&
                counter.SourceOrdinal < counterContext.Sources.Count =>
                SetCandidateCoordinates(
                    counterContext.Sources[counter.SourceOrdinal]
                        .PublicSemanticCardLocator,
                    FlatPromptSourceSectionV1.CounterSources,
                    counter.SourceOrdinal,
                    out sourceSection,
                    out sourceOrdinal),
            _ => null
        };
        return locator is not null;
    }

    private static PublicSemanticLocatorV1 SetCandidateCoordinates(
        PublicSemanticLocatorV1 locator,
        FlatPromptSourceSectionV1 sourceSectionValue,
        int sourceOrdinalValue,
        out FlatPromptSourceSectionV1 sourceSection,
        out int sourceOrdinal)
    {
        sourceSection = sourceSectionValue;
        sourceOrdinal = sourceOrdinalValue;
        return locator;
    }

    private static bool TryFindMirrorOccurrence(
        MirrorSnapshotV1 snapshot,
        PrivateI4OccurrencePublicLocatorSidecarEntryV1 entry,
        out MirrorCardSnapshotV1? card)
    {
        card = null;
        MirrorCardSnapshotV1[] matches = snapshot.Cards
            .Where(candidate =>
                candidate.Zone == entry.NormalizedZone &&
                candidate.Sequence == entry.SourceSequence &&
                candidate.IsOverlay == entry.IsOverlay &&
                (!entry.IsOverlay ||
                 candidate.OverlayIndex == entry.OverlayIndex!.Value) &&
                PublicSemanticLocatorV1.TryGetAbsolutePlayer(
                    snapshot.Perspective,
                    candidate.Controller,
                    out byte absoluteController) &&
                absoluteController == entry.AbsoluteController)
            .ToArray();
        if (matches.Length != 1)
        {
            return false;
        }

        card = matches[0];
        return true;
    }

    private static bool TryFindUniqueFrameLocator(
        PerspectiveSafeFrameV1 frame,
        PublicSemanticLocatorV1 locator,
        out PerspectiveSafeEntityV1? entity,
        out bool ambiguous)
    {
        entity = null;
        ambiguous = false;
        PerspectiveSafeEntityV1[] matches = frame.Entities
            .Where(candidate => string.Equals(
                candidate.Locator,
                locator.Value,
                StringComparison.Ordinal))
            .ToArray();
        if (matches.Length == 0)
        {
            return false;
        }

        if (matches.Length != 1)
        {
            ambiguous = true;
            return false;
        }

        entity = matches[0];
        return true;
    }

    private static I6DPrivateCrossLocatorHandoffErrorV1 Failure(
        I6DPrivateCrossLocatorHandoffErrorCodeV1 code,
        string fieldPath) =>
        new(code, fieldPath);
}
