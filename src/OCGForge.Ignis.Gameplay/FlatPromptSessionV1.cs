namespace OCGForge.Ignis.Gameplay;

public sealed class FlatPromptSessionV1
{
    private ulong nextPromptOrdinal;
    private CurrentFlatPromptBindingV1? currentBinding;

    public FlatPromptProjectionResultV1 TryAcceptPrompt(
        ReadOnlySpan<byte> completeInnerGameMessage)
    {
        if (!FlatPromptProjectionV1.TryProject(
                completeInnerGameMessage,
                out FlatPromptProjectionDraftV1? draft,
                out FlatPromptErrorCodeV1 projectionError) ||
            draft is null)
        {
            currentBinding = null;
            return FlatPromptProjectionResultV1.Failure(projectionError);
        }

        return CommitProjection(draft);
    }

    public FlatPromptProjectionResultV1 TryAcceptPrompt(
        ReadOnlySpan<byte> completeInnerGameMessage,
        PerspectiveStateMirrorV1? mirror,
        PublicStateProjectionResultV1? acceptedProjection)
    {
        if (!FlatPromptProjectionV1.TryParseWireDraft(
                completeInnerGameMessage,
                out FlatPromptWireDraftV1? wireDraft,
                out FlatPromptErrorCodeV1 parseError) ||
            wireDraft is null)
        {
            currentBinding = null;
            return FlatPromptProjectionResultV1.Failure(parseError);
        }

        if (acceptedProjection is null ||
            !acceptedProjection.IsSuccess ||
            acceptedProjection.Snapshot is null ||
            mirror is null)
        {
            currentBinding = null;
            return FlatPromptProjectionResultV1.Failure(
                FlatPromptErrorCodeV1.UnprovenPublicReference);
        }

        MirrorSnapshotV1 capturedMirror = mirror.Snapshot;
        PublicStateSnapshotV1 acceptedSnapshot = acceptedProjection.Snapshot;
        PublicStateProjectionResultV1 recomputedProjection =
            PublicStateProjectionV1.TryProject(
                capturedMirror,
                new PublicStateProjectionContextV1(
                    acceptedSnapshot.DuelFlags));
        ReadOnlyMemory<byte> acceptedCanonicalBytes =
            acceptedProjection.CanonicalBytes;
        ReadOnlyMemory<byte> recomputedCanonicalBytes =
            recomputedProjection.CanonicalBytes;
        if (!recomputedProjection.IsSuccess ||
            recomputedProjection.Snapshot is null ||
            !recomputedCanonicalBytes.Span.SequenceEqual(
                acceptedCanonicalBytes.Span) ||
            !string.Equals(
                recomputedProjection.Sha256,
                acceptedProjection.Sha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                recomputedProjection.PublicProjectionId,
                acceptedProjection.PublicProjectionId,
                StringComparison.Ordinal))
        {
            currentBinding = null;
            return FlatPromptProjectionResultV1.Failure(
                FlatPromptErrorCodeV1.AuthorityMismatch);
        }

        if (!FlatPromptProjectionV1.TryBuildProjectedDraft(
                wireDraft,
                new FlatPromptCardAuthorityContextV1(
                    capturedMirror,
                    acceptedSnapshot),
                out FlatPromptProjectionDraftV1? projected,
                out FlatPromptErrorCodeV1 projectionError) ||
            projected is null)
        {
            currentBinding = null;
            return FlatPromptProjectionResultV1.Failure(projectionError);
        }

        return CommitProjection(projected);
    }

    private FlatPromptProjectionResultV1 CommitProjection(
        FlatPromptProjectionDraftV1 draft)
    {
        ulong nextOrdinal;
        try
        {
            nextOrdinal = checked(nextPromptOrdinal + 1);
        }
        catch (OverflowException)
        {
            currentBinding = null;
            return FlatPromptProjectionResultV1.Failure(
                FlatPromptErrorCodeV1.ArithmeticFailure);
        }

        FlatPublicCandidateDescriptorV1[] candidates =
            draft.CopyCandidates();
        string[] localKeys = draft.CopyLocalKeys();
        int[] responses = draft.CopyResponses();
        if (!CurrentFlatPromptBindingV1.TryCreate(
                nextPromptOrdinal,
                draft.Context.PromptFamily,
                candidates,
                localKeys,
                responses,
                out CurrentFlatPromptBindingV1? binding,
                out FlatPromptErrorCodeV1 bindingError) ||
            binding is null)
        {
            currentBinding = null;
            return FlatPromptProjectionResultV1.Failure(bindingError);
        }

        FlatPromptProjectionResultV1 result =
            FlatPromptProjectionResultV1.Success(draft.Context, candidates);
        currentBinding = binding;
        nextPromptOrdinal = nextOrdinal;
        return result;
    }

    internal bool TryCaptureSelection(
        string? i4LocalCandidateKey,
        out FlatPromptSelectionHandleV1? handle,
        out FlatPromptErrorCodeV1 error)
    {
        handle = null;
        error = FlatPromptErrorCodeV1.None;
        if (string.IsNullOrEmpty(i4LocalCandidateKey) ||
            currentBinding is null ||
            !currentBinding.TryGetResponse(i4LocalCandidateKey, out _))
        {
            error = FlatPromptErrorCodeV1.InvalidI4LocalCandidateKey;
            return false;
        }

        handle = new FlatPromptSelectionHandleV1(
            currentBinding.PromptInstanceOrdinal,
            currentBinding.Family,
            i4LocalCandidateKey,
            currentBinding.Candidates);
        return true;
    }

    internal bool TryResolveSelection(
        FlatPromptSelectionHandleV1? handle,
        out FlatPromptResponseResolutionV1 response,
        out FlatPromptErrorCodeV1 error)
    {
        response = default;
        error = FlatPromptErrorCodeV1.None;
        if (handle is null)
        {
            error = FlatPromptErrorCodeV1.InvalidResponseBinding;
            return false;
        }

        if (currentBinding is null ||
            handle.PromptInstanceOrdinal != currentBinding.PromptInstanceOrdinal ||
            handle.Family != currentBinding.Family ||
            !DomainsEqual(handle.OrderedDomain, currentBinding.Candidates))
        {
            error = FlatPromptErrorCodeV1.StalePromptBinding;
            return false;
        }

        if (string.IsNullOrEmpty(handle.I4LocalCandidateKey) ||
            !currentBinding.TryGetResponse(
                handle.I4LocalCandidateKey,
                out int responseI32))
        {
            error = FlatPromptErrorCodeV1.InvalidResponseBinding;
            return false;
        }

        response = new FlatPromptResponseResolutionV1(responseI32);
        return true;
    }

    private static bool DomainsEqual(
        IReadOnlyList<FlatPublicCandidateDescriptorV1> first,
        IReadOnlyList<FlatPublicCandidateDescriptorV1> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (int index = 0; index < first.Count; index++)
        {
            if (!DescriptorsEqual(first[index], second[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool DescriptorsEqual(
        FlatPublicCandidateDescriptorV1? first,
        FlatPublicCandidateDescriptorV1? second)
    {
        if (first is null || second is null)
        {
            return false;
        }

        if (first.GetType() != second.GetType() ||
            !string.Equals(
                first.I4LocalCandidateKey,
                second.I4LocalCandidateKey,
                StringComparison.Ordinal) ||
            first.ChoiceKind != second.ChoiceKind)
        {
            return false;
        }

        return (first, second) switch
        {
            (FlatYesNoPublicCandidateDescriptorV1,
                FlatYesNoPublicCandidateDescriptorV1) => true,
            (FlatOptionPublicCandidateDescriptorV1 left,
                FlatOptionPublicCandidateDescriptorV1 right) =>
                left.SourceSection == right.SourceSection &&
                left.SourceOrdinal == right.SourceOrdinal &&
                left.OptionValue == right.OptionValue,
            (FlatPositionPublicCandidateDescriptorV1 left,
                FlatPositionPublicCandidateDescriptorV1 right) =>
                left.PositionValue == right.PositionValue,
            (FlatEffectYnPublicCandidateDescriptorV1,
                FlatEffectYnPublicCandidateDescriptorV1) => true,
            (FlatChainNoChainPublicCandidateDescriptorV1,
                FlatChainNoChainPublicCandidateDescriptorV1) => true,
            (FlatChainPublicCandidateDescriptorV1 left,
                FlatChainPublicCandidateDescriptorV1 right) =>
                left.SourceSection == right.SourceSection &&
                left.SourceOrdinal == right.SourceOrdinal &&
                left.PublicSemanticCardLocator == right.PublicSemanticCardLocator &&
                left.DescriptionOrEffectId == right.DescriptionOrEffectId &&
                left.ClientMode == right.ClientMode,
            (FlatChainCardCodePublicCandidateDescriptorV1 left,
                FlatChainCardCodePublicCandidateDescriptorV1 right) =>
                left.SourceSection == right.SourceSection &&
                left.SourceOrdinal == right.SourceOrdinal &&
                left.PublicSemanticCardLocator == right.PublicSemanticCardLocator &&
                left.DescriptionOrEffectId == right.DescriptionOrEffectId &&
                left.ClientMode == right.ClientMode &&
                left.CardCode == right.CardCode,
            _ => false
        };
    }
}
