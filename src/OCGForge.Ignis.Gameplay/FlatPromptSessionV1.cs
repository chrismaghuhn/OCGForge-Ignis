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

    public FlatPromptProjectionResultV1 TryAcceptI5Prompt(
        ReadOnlySpan<byte> completeInnerGameMessage)
    {
        if (!FlatPromptProjectionV1.TryParseI5WireDraft(
                completeInnerGameMessage,
                out FlatPromptWireDraftV1? wireDraft,
                out FlatPromptErrorCodeV1 parseError) ||
            wireDraft is null)
        {
            currentBinding = null;
            return FlatPromptProjectionResultV1.Failure(parseError);
        }

        if (!FlatPromptProjectionV1.TryBuildProjectedDraft(
                wireDraft,
                null,
                out FlatPromptProjectionDraftV1? projected,
                out FlatPromptErrorCodeV1 projectionError) ||
            projected is null)
        {
            currentBinding = null;
            return FlatPromptProjectionResultV1.Failure(projectionError);
        }

        return CommitProjection(projected);
    }

    public FlatPromptProjectionResultV1 TryAcceptI5Prompt(
        ReadOnlySpan<byte> completeInnerGameMessage,
        PerspectiveStateMirrorV1? mirror,
        PublicStateProjectionResultV1? acceptedProjection)
    {
        if (!FlatPromptProjectionV1.TryParseI5WireDraft(
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
        if (!recomputedProjection.IsSuccess ||
            recomputedProjection.Snapshot is null ||
            !recomputedProjection.CanonicalBytes.Span.SequenceEqual(
                acceptedProjection.CanonicalBytes.Span) ||
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

    internal FlatPromptContinuationStepResultV1 TryApplySelection(
        FlatPromptSelectionHandleV1? handle)
    {
        if (handle is null)
        {
            return FlatPromptContinuationStepResultV1.Failure(
                FlatPromptErrorCodeV1.InvalidResponseBinding);
        }

        if (currentBinding is null ||
            handle.PromptInstanceOrdinal != currentBinding.PromptInstanceOrdinal ||
            handle.Family != currentBinding.Family)
        {
            return FlatPromptContinuationStepResultV1.Failure(
                FlatPromptErrorCodeV1.InvalidContinuationInstance);
        }

        if (handle.ContinuationStep != currentBinding.ContinuationStep)
        {
            return FlatPromptContinuationStepResultV1.Failure(
                FlatPromptErrorCodeV1.StaleContinuationStep);
        }

        if (!DomainsEqual(handle.OrderedDomain, currentBinding.Candidates))
        {
            return FlatPromptContinuationStepResultV1.Failure(
                FlatPromptErrorCodeV1.InvalidContinuationInstance);
        }

        if (!currentBinding.TryGetCandidate(
                handle.I4LocalCandidateKey,
                out FlatPublicCandidateDescriptorV1? candidate) ||
            candidate is null)
        {
            currentBinding = null;
            return FlatPromptContinuationStepResultV1.Failure(
                FlatPromptErrorCodeV1.InvalidI4LocalCandidateKey);
        }

        if (currentBinding.ContinuationState is null)
        {
            if (currentBinding.TryGetResponseBody(
                    handle.I4LocalCandidateKey,
                    out byte[] responseBody))
            {
                currentBinding = null;
                return FlatPromptContinuationStepResultV1.Terminal(
                    responseBody);
            }

            currentBinding = null;
            return FlatPromptContinuationStepResultV1.Failure(
                FlatPromptErrorCodeV1.InvalidContinuationAction);
        }

        FlatPromptCardContinuationStateV1 state =
            currentBinding.ContinuationState;
        if (candidate is FlatPromptCardSelectionCandidateBaseV1 cardCandidate &&
            state.Family == FlatPromptFamilyValueV1.MsgSelectCard &&
            cardCandidate.ChoiceKind == FlatPromptChoiceKindV1.Pick)
        {
            return ApplyCardPick(state, cardCandidate.SourceOrdinal);
        }

        if (candidate is FlatPromptTributeSelectionCandidateBaseV1 tributeCandidate &&
            state.Family == FlatPromptFamilyValueV1.MsgSelectTribute &&
            tributeCandidate.ChoiceKind == FlatPromptChoiceKindV1.Pick)
        {
            return ApplyCardPick(state, tributeCandidate.SourceOrdinal);
        }

        if (candidate is FlatPromptFinishPublicCandidateV1 finish &&
            finish.ChoiceKind == FlatPromptChoiceKindV1.Finish &&
            state.CanFinish)
        {
            if (!FlatPromptProjectionV1.TryEncodeCardIndexResponse(
                    state.SelectedOrdinals,
                    out byte[] responseBody,
                    out FlatPromptErrorCodeV1 error))
            {
                currentBinding = null;
                return FlatPromptContinuationStepResultV1.Failure(error);
            }

            currentBinding = null;
            return FlatPromptContinuationStepResultV1.Terminal(responseBody);
        }

        if (candidate is FlatPromptCancelPublicCandidateV1 cancel &&
            cancel.ChoiceKind == FlatPromptChoiceKindV1.Cancel &&
            state.Cancelable)
        {
            currentBinding = null;
            return FlatPromptContinuationStepResultV1.Terminal(
                CreateInt32Response(-1));
        }

        currentBinding = null;
        return FlatPromptContinuationStepResultV1.Failure(
            FlatPromptErrorCodeV1.InvalidContinuationAction);
    }

    private FlatPromptContinuationStepResultV1 ApplyCardPick(
        FlatPromptCardContinuationStateV1 state,
        int sourceOrdinal)
    {
        if (!FlatPromptProjectionV1.TryAdvanceCardContinuation(
                state,
                sourceOrdinal,
                out FlatPromptProjectionDraftV1? nextDraft,
                out FlatPromptErrorCodeV1 error) ||
            nextDraft is null ||
            currentBinding is null)
        {
            currentBinding = null;
            return FlatPromptContinuationStepResultV1.Failure(error);
        }

        CurrentFlatPromptBindingV1 binding = currentBinding;

        FlatPublicCandidateDescriptorV1[] candidates =
            nextDraft.CopyCandidates();
        string[] localKeys = nextDraft.CopyLocalKeys();
        int[] responses = nextDraft.CopyResponses();
        if (!CurrentFlatPromptBindingV1.TryCreate(
                binding.PromptInstanceOrdinal,
                nextDraft.Context.PromptFamily,
                candidates,
                localKeys,
                responses,
                out CurrentFlatPromptBindingV1? nextBinding,
                out FlatPromptErrorCodeV1 bindingError,
                nextDraft.CopyResponseBodies(),
                nextDraft.ContinuationState) ||
            nextBinding is null)
        {
            currentBinding = null;
            return FlatPromptContinuationStepResultV1.Failure(bindingError);
        }

        FlatPromptProjectionResultV1 projection =
            FlatPromptProjectionResultV1.Success(
                nextDraft.Context,
                candidates);
        currentBinding = nextBinding;
        return FlatPromptContinuationStepResultV1.Intermediate(projection);
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
                out FlatPromptErrorCodeV1 bindingError,
                draft.CopyResponseBodies(),
                draft.ContinuationState) ||
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
            currentBinding.Candidates,
            currentBinding.ContinuationStep);
        return true;
    }

    private static byte[] CreateInt32Response(int value)
    {
        byte[] body = new byte[sizeof(int)];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(
            body,
            value);
        return body;
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
            handle.ContinuationStep != currentBinding.ContinuationStep ||
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
            (FlatBattleActivatablePublicCandidateV1 left,
                FlatBattleActivatablePublicCandidateV1 right) =>
                BattleActivatableEqual(left, right),
            (FlatBattleActivatableCardCodePublicCandidateV1 left,
                FlatBattleActivatableCardCodePublicCandidateV1 right) =>
                BattleActivatableEqual(left, right) &&
                left.CardCode == right.CardCode,
            (FlatBattleAttackPublicCandidateV1 left,
                FlatBattleAttackPublicCandidateV1 right) =>
                BattleAttackEqual(left, right),
            (FlatBattleAttackCardCodePublicCandidateV1 left,
                FlatBattleAttackCardCodePublicCandidateV1 right) =>
                BattleAttackEqual(left, right) &&
                left.CardCode == right.CardCode,
            (FlatBattleToMainPhase2PublicCandidateV1 left,
                FlatBattleToMainPhase2PublicCandidateV1 right) =>
                left.TransitionToken == right.TransitionToken,
            (FlatBattleToEndPhasePublicCandidateV1 left,
                FlatBattleToEndPhasePublicCandidateV1 right) =>
                left.TransitionToken == right.TransitionToken,
            (FlatIdleSummonPublicCandidateV1 left,
                FlatIdleSummonPublicCandidateV1 right) =>
                IdleCardActionEqual(left, right),
            (FlatIdleSummonCardCodePublicCandidateV1 left,
                FlatIdleSummonCardCodePublicCandidateV1 right) =>
                IdleCardActionEqual(left, right) &&
                left.CardCode == right.CardCode,
            (FlatIdleSpecialSummonPublicCandidateV1 left,
                FlatIdleSpecialSummonPublicCandidateV1 right) =>
                IdleCardActionEqual(left, right),
            (FlatIdleSpecialSummonCardCodePublicCandidateV1 left,
                FlatIdleSpecialSummonCardCodePublicCandidateV1 right) =>
                IdleCardActionEqual(left, right) &&
                left.CardCode == right.CardCode,
            (FlatIdleRepositionPublicCandidateV1 left,
                FlatIdleRepositionPublicCandidateV1 right) =>
                IdleCardActionEqual(left, right),
            (FlatIdleRepositionCardCodePublicCandidateV1 left,
                FlatIdleRepositionCardCodePublicCandidateV1 right) =>
                IdleCardActionEqual(left, right) &&
                left.CardCode == right.CardCode,
            (FlatIdleMsetPublicCandidateV1 left,
                FlatIdleMsetPublicCandidateV1 right) =>
                IdleCardActionEqual(left, right),
            (FlatIdleMsetCardCodePublicCandidateV1 left,
                FlatIdleMsetCardCodePublicCandidateV1 right) =>
                IdleCardActionEqual(left, right) &&
                left.CardCode == right.CardCode,
            (FlatIdleSsetPublicCandidateV1 left,
                FlatIdleSsetPublicCandidateV1 right) =>
                IdleCardActionEqual(left, right),
            (FlatIdleSsetCardCodePublicCandidateV1 left,
                FlatIdleSsetCardCodePublicCandidateV1 right) =>
                IdleCardActionEqual(left, right) &&
                left.CardCode == right.CardCode,
            (FlatIdleActivatablePublicCandidateV1 left,
                FlatIdleActivatablePublicCandidateV1 right) =>
                IdleActivatableEqual(left, right),
            (FlatIdleActivatableCardCodePublicCandidateV1 left,
                FlatIdleActivatableCardCodePublicCandidateV1 right) =>
                IdleActivatableEqual(left, right) &&
                left.CardCode == right.CardCode,
            (FlatIdleToBattlePhasePublicCandidateV1 left,
                FlatIdleToBattlePhasePublicCandidateV1 right) =>
                left.TransitionToken == right.TransitionToken,
            (FlatIdleToEndPhasePublicCandidateV1 left,
                FlatIdleToEndPhasePublicCandidateV1 right) =>
                left.TransitionToken == right.TransitionToken,
            (FlatIdleShuffleHandPublicCandidateV1 left,
                FlatIdleShuffleHandPublicCandidateV1 right) =>
                left.TransitionToken == right.TransitionToken,
            (FlatPromptCardSelectionAnonymousCandidateV1 left,
                FlatPromptCardSelectionAnonymousCandidateV1 right) =>
                CardSelectionEqual(left, right),
            (FlatPromptCardSelectionPromptCodeCandidateV1 left,
                FlatPromptCardSelectionPromptCodeCandidateV1 right) =>
                CardSelectionEqual(left, right) &&
                left.PromptLocalCardCode == right.PromptLocalCardCode,
            (FlatPromptCardSelectionLocatorCandidateV1 left,
                FlatPromptCardSelectionLocatorCandidateV1 right) =>
                CardSelectionEqual(left, right) &&
                left.PublicSemanticCardLocator ==
                    right.PublicSemanticCardLocator,
            (FlatPromptCardSelectionLocatorPromptCodeCandidateV1 left,
                FlatPromptCardSelectionLocatorPromptCodeCandidateV1 right) =>
                CardSelectionEqual(left, right) &&
                left.PublicSemanticCardLocator ==
                    right.PublicSemanticCardLocator &&
                left.PromptLocalCardCode == right.PromptLocalCardCode,
            (FlatPromptTributeSelectionAnonymousCandidateV1 left,
                FlatPromptTributeSelectionAnonymousCandidateV1 right) =>
                TributeSelectionEqual(left, right),
            (FlatPromptTributeSelectionPromptCodeCandidateV1 left,
                FlatPromptTributeSelectionPromptCodeCandidateV1 right) =>
                TributeSelectionEqual(left, right) &&
                left.PromptLocalCardCode == right.PromptLocalCardCode,
            (FlatPromptTributeSelectionLocatorCandidateV1 left,
                FlatPromptTributeSelectionLocatorCandidateV1 right) =>
                TributeSelectionEqual(left, right) &&
                left.PublicSemanticCardLocator ==
                    right.PublicSemanticCardLocator,
            (FlatPromptTributeSelectionLocatorPromptCodeCandidateV1 left,
                FlatPromptTributeSelectionLocatorPromptCodeCandidateV1 right) =>
                TributeSelectionEqual(left, right) &&
                left.PublicSemanticCardLocator ==
                    right.PublicSemanticCardLocator &&
                left.PromptLocalCardCode == right.PromptLocalCardCode,
            (FlatPromptFinishPublicCandidateV1,
                FlatPromptFinishPublicCandidateV1) => true,
            (FlatPromptCancelPublicCandidateV1,
                FlatPromptCancelPublicCandidateV1) => true,
            (FlatPromptFinishOrCancelPublicCandidateV1,
                FlatPromptFinishOrCancelPublicCandidateV1) => true,
            (FlatPromptSelectUnselectAnonymousCandidateV1 left,
                FlatPromptSelectUnselectAnonymousCandidateV1 right) =>
                SelectUnselectEqual(left, right),
            (FlatPromptSelectUnselectPromptCodeCandidateV1 left,
                FlatPromptSelectUnselectPromptCodeCandidateV1 right) =>
                SelectUnselectEqual(left, right) &&
                left.PromptLocalCardCode == right.PromptLocalCardCode,
            (FlatPromptSelectUnselectLocatorCandidateV1 left,
                FlatPromptSelectUnselectLocatorCandidateV1 right) =>
                SelectUnselectEqual(left, right) &&
                left.PublicSemanticCardLocator ==
                    right.PublicSemanticCardLocator,
            (FlatPromptSelectUnselectLocatorPromptCodeCandidateV1 left,
                FlatPromptSelectUnselectLocatorPromptCodeCandidateV1 right) =>
                SelectUnselectEqual(left, right) &&
                left.PublicSemanticCardLocator ==
                    right.PublicSemanticCardLocator &&
                left.PromptLocalCardCode == right.PromptLocalCardCode,
            (FlatPromptAnnounceNumberPublicCandidateV1 left,
                FlatPromptAnnounceNumberPublicCandidateV1 right) =>
                left.SourceSection == right.SourceSection &&
                left.SourceOrdinal == right.SourceOrdinal &&
                left.NumberValue == right.NumberValue,
            _ => false
        };
    }

    private static bool CardSelectionEqual(
        FlatPromptCardSelectionCandidateBaseV1 first,
        FlatPromptCardSelectionCandidateBaseV1 second) =>
        first.SourceSection == second.SourceSection &&
        first.SourceOrdinal == second.SourceOrdinal;

    private static bool TributeSelectionEqual(
        FlatPromptTributeSelectionCandidateBaseV1 first,
        FlatPromptTributeSelectionCandidateBaseV1 second) =>
        first.SourceSection == second.SourceSection &&
        first.SourceOrdinal == second.SourceOrdinal;

    private static bool SelectUnselectEqual(
        FlatPromptSelectUnselectCardCandidateBaseV1 first,
        FlatPromptSelectUnselectCardCandidateBaseV1 second) =>
        first.SourceSection == second.SourceSection &&
        first.SourceOrdinal == second.SourceOrdinal;

    private static bool BattleActivatableEqual(
        FlatBattleActivatablePublicCandidateBaseV1 first,
        FlatBattleActivatablePublicCandidateBaseV1 second) =>
        first.SourceSection == second.SourceSection &&
        first.SourceOrdinal == second.SourceOrdinal &&
        first.PublicSemanticCardLocator == second.PublicSemanticCardLocator &&
        first.DescriptionOrEffectId == second.DescriptionOrEffectId &&
        first.ClientMode == second.ClientMode;

    private static bool BattleAttackEqual(
        FlatBattleAttackPublicCandidateBaseV1 first,
        FlatBattleAttackPublicCandidateBaseV1 second) =>
        first.SourceSection == second.SourceSection &&
        first.SourceOrdinal == second.SourceOrdinal &&
        first.PublicSemanticCardLocator == second.PublicSemanticCardLocator &&
        first.DirectAttackable == second.DirectAttackable;

    private static bool IdleCardActionEqual(
        FlatIdleCardActionPublicCandidateBaseV1 first,
        FlatIdleCardActionPublicCandidateBaseV1 second) =>
        first.SourceSection == second.SourceSection &&
        first.SourceOrdinal == second.SourceOrdinal &&
        first.PublicSemanticCardLocator == second.PublicSemanticCardLocator;

    private static bool IdleActivatableEqual(
        FlatIdleActivatablePublicCandidateBaseV1 first,
        FlatIdleActivatablePublicCandidateBaseV1 second) =>
        first.SourceSection == second.SourceSection &&
        first.SourceOrdinal == second.SourceOrdinal &&
        first.PublicSemanticCardLocator == second.PublicSemanticCardLocator &&
        first.DescriptionOrEffectId == second.DescriptionOrEffectId &&
        first.ClientMode == second.ClientMode;
}
