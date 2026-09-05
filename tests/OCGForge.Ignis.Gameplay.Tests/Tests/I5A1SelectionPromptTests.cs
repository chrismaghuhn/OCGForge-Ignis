using System.Buffers.Binary;
using System.Reflection;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I5A1SelectionPromptTests
{
    internal static void TestSelectCard()
    {
        FlatPromptSessionV1 session = new();
        byte[] source = SelectCardMinimal.ToArray();
        FlatPromptProjectionResultV1 result =
            session.TryAcceptI5Prompt(source);
        AssertSuccess(result, FlatPromptFamilyValueV1.MsgSelectCard);
        FlatPromptCardSelectionPublicContextV1 context =
            result.Context as FlatPromptCardSelectionPublicContextV1 ??
            throw new InvalidOperationException("expected SELECT_CARD context");
        Equal(0u, context.ActingPlayer);
        Equal(1u, context.MinimumCount);
        Equal(1u, context.MaximumCount);
        False(context.EffectiveCancellation);
        EqualKeys(
            new[] { "MSG_SELECT_CARD:PICK:0" },
            result.Candidates!.Select(candidate => candidate.I4LocalCandidateKey)
                .ToArray());
        FlatPromptCardSelectionPromptCodeCandidateV1 candidate =
            result.Candidates![0] as
                FlatPromptCardSelectionPromptCodeCandidateV1 ??
            throw new InvalidOperationException("expected prompt-local code");
        Equal(0x11223344u, candidate.PromptLocalCardCode);
        Null(typeof(FlatPromptCardSelectionPromptCodeCandidateV1).GetProperty(
            "PublicSemanticCardLocator"));

        FlatPromptSelectionHandleV1 initialHandle = Capture(
            session,
            "MSG_SELECT_CARD:PICK:0");
        Equal(0ul, initialHandle.PromptInstanceOrdinal);
        Equal(0, initialHandle.ContinuationStep);
        FlatPromptContinuationStepResultV1 picked =
            session.TryApplySelection(initialHandle);
        True(picked.IsSuccess, picked.Error.ToString());
        False(picked.IsTerminal);
        NotNull(picked.Projection);
        EqualKeys(
            new[] { "MSG_SELECT_CARD:FINISH" },
            picked.Projection!.Candidates!
                .Select(value => value.I4LocalCandidateKey)
                .ToArray());
        False(session.TryResolveSelection(
            initialHandle,
            out _,
            out FlatPromptErrorCodeV1 staleAfterPick));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, staleAfterPick);
        FlatPromptContinuationStepResultV1 staleStep =
            session.TryApplySelection(initialHandle);
        False(staleStep.IsSuccess);
        Equal(FlatPromptErrorCodeV1.StaleContinuationStep, staleStep.Error);

        FlatPromptSelectionHandleV1 finishHandle = Capture(
            session,
            "MSG_SELECT_CARD:FINISH");
        Equal(1, finishHandle.ContinuationStep);
        FlatPromptContinuationStepResultV1 finished =
            session.TryApplySelection(finishHandle);
        True(finished.IsSuccess, finished.Error.ToString());
        True(finished.IsTerminal);
        BytesEqual(
            new byte[] { 0x03, 0x00, 0x00, 0x00, 0x01 },
            finished.TerminalResponseBody.ToArray());
        False(session.TryResolveSelection(
            finishHandle,
            out _,
            out FlatPromptErrorCodeV1 staleAfterFinish));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, staleAfterFinish);
        FlatPromptContinuationStepResultV1 terminalReuse =
            session.TryApplySelection(finishHandle);
        False(terminalReuse.IsSuccess);
        Equal(
            FlatPromptErrorCodeV1.InvalidContinuationInstance,
            terminalReuse.Error);

        FlatPromptProjectionResultV1 mainDeck =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                SelectCardMainDeck.ToArray());
        AssertSuccess(mainDeck, FlatPromptFamilyValueV1.MsgSelectCard);
        FlatPromptCardSelectionPromptCodeCandidateV1 mainDeckCandidate =
            mainDeck.Candidates![0] as
                FlatPromptCardSelectionPromptCodeCandidateV1 ??
            throw new InvalidOperationException("expected main-deck code");
        Equal(0xDEADBEEFu, mainDeckCandidate.PromptLocalCardCode);
        Null(typeof(FlatPromptCardSelectionPromptCodeCandidateV1).GetProperty(
            "PublicSemanticCardLocator"));

        Authority authority = CreateAuthority(
            new CardSpec(
                0x55667788,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        FlatPromptProjectionResultV1 addressed =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                SelectCardMessage(
                    0,
                    false,
                    1,
                    1,
                    (0x55667788u,
                        new ModernLocInfoV1(0, 0x04, 0, 0)) ),
                authority.Mirror,
                authority.Projection);
        AssertSuccess(addressed, FlatPromptFamilyValueV1.MsgSelectCard);
        FlatPromptCardSelectionLocatorPromptCodeCandidateV1 addressedCandidate =
            addressed.Candidates![0] as
                FlatPromptCardSelectionLocatorPromptCodeCandidateV1 ??
            throw new InvalidOperationException("expected accepted locator");
        Equal("p0:MONSTER_ZONE:0",
            addressedCandidate.PublicSemanticCardLocator.Value);
        Equal(0x55667788u, addressedCandidate.PromptLocalCardCode);

        FlatPromptProjectionResultV1 mismatching =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                SelectCardMessage(
                    0,
                    false,
                    1,
                    1,
                    (0xDEADBEEFu,
                        new ModernLocInfoV1(0, 0x04, 0, 0))),
                authority.Mirror,
                authority.Projection);
        AssertSuccess(mismatching, FlatPromptFamilyValueV1.MsgSelectCard);
        FlatPromptCardSelectionLocatorPromptCodeCandidateV1 mismatchCandidate =
            mismatching.Candidates![0] as
                FlatPromptCardSelectionLocatorPromptCodeCandidateV1 ??
            throw new InvalidOperationException("expected prompt-local code");
        Equal("p0:MONSTER_ZONE:0",
            mismatchCandidate.PublicSemanticCardLocator.Value);
        Equal(0xDEADBEEFu, mismatchCandidate.PromptLocalCardCode);

        FlatPromptProjectionResultV1 anonymous =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                SelectCardMessage(
                    0,
                    false,
                    1,
                    1,
                    (0u, new ModernLocInfoV1(0, 0, 0, 0))));
        AssertSuccess(anonymous, FlatPromptFamilyValueV1.MsgSelectCard);
        True(anonymous.Candidates![0]
            is FlatPromptCardSelectionAnonymousCandidateV1);
        False(anonymous.Candidates[0].GetType().GetProperties().Any(property =>
            property.Name.Contains("CardCode", StringComparison.Ordinal)));

        FlatPromptSessionV1 duplicateSession = new();
        FlatPromptProjectionResultV1 duplicates =
            duplicateSession.TryAcceptI5Prompt(
                SelectCardDuplicates.ToArray());
        AssertSuccess(duplicates, FlatPromptFamilyValueV1.MsgSelectCard);
        Equal(3, duplicates.Candidates!.Count);
        EqualKeys(
            new[]
            {
                "MSG_SELECT_CARD:PICK:0",
                "MSG_SELECT_CARD:PICK:1",
                "MSG_SELECT_CARD:CANCEL"
            },
            duplicates.Candidates.Select(value => value.I4LocalCandidateKey)
                .ToArray());
        NotEqual(duplicates.Candidates[0], duplicates.Candidates[1]);
        FlatPromptContinuationStepResultV1 cancelled = duplicateSession
            .TryApplySelection(Capture(
                duplicateSession,
                "MSG_SELECT_CARD:CANCEL"));
        True(cancelled.IsSuccess, cancelled.Error.ToString());
        True(cancelled.IsTerminal);
        BytesEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
            cancelled.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 duplicateFinishSession = new();
        AssertSuccess(
            duplicateFinishSession.TryAcceptI5Prompt(
                SelectCardDuplicates.ToArray()),
            FlatPromptFamilyValueV1.MsgSelectCard);
        FlatPromptContinuationStepResultV1 duplicatePicked =
            duplicateFinishSession.TryApplySelection(Capture(
                duplicateFinishSession,
                "MSG_SELECT_CARD:PICK:1"));
        True(duplicatePicked.IsSuccess, duplicatePicked.Error.ToString());
        FlatPromptContinuationStepResultV1 duplicateFinished =
            duplicateFinishSession.TryApplySelection(Capture(
                duplicateFinishSession,
                "MSG_SELECT_CARD:FINISH"));
        True(duplicateFinished.IsSuccess, duplicateFinished.Error.ToString());
        BytesEqual(
            new byte[] { 0x03, 0x00, 0x00, 0x00, 0x02 },
            duplicateFinished.TerminalResponseBody.ToArray());

        AssertCardIndexCodecs();
        AssertSelectCardFailuresAndOwnership();
        AssertSelectCardAuthorityFailures(authority);
    }

    internal static void TestSelectTribute()
    {
        FlatPromptSessionV1 session = new();
        byte[] source = SelectTributeMinimal.ToArray();
        FlatPromptProjectionResultV1 result =
            session.TryAcceptI5Prompt(source);
        AssertSuccess(result, FlatPromptFamilyValueV1.MsgSelectTribute);
        FlatPromptTributeSelectionPublicContextV1 context =
            result.Context as FlatPromptTributeSelectionPublicContextV1 ??
            throw new InvalidOperationException("expected SELECT_TRIBUTE context");
        Equal(0u, context.ActingPlayer);
        Equal(1u, context.MinimumRequiredTributeValue);
        Equal(2u, context.MaximumSelectedCardCount);
        False(context.EffectiveCancellation);
        EqualKeys(
            new[]
            {
                "MSG_SELECT_TRIBUTE:PICK:0",
                "MSG_SELECT_TRIBUTE:PICK:1"
            },
            result.Candidates!.Select(value => value.I4LocalCandidateKey)
                .ToArray());
        Equal(0x11111111u,
            ((FlatPromptTributeSelectionPromptCodeCandidateV1)
                result.Candidates![0]).PromptLocalCardCode);
        Equal(0x22222222u,
            ((FlatPromptTributeSelectionPromptCodeCandidateV1)
                result.Candidates![1]).PromptLocalCardCode);
        False(typeof(FlatPromptTributeSelectionPromptCodeCandidateV1)
            .GetProperties()
            .Any(property => property.Name.Contains(
                "Release",
                StringComparison.Ordinal)));

        Authority authority = CreateAuthority(
            new CardSpec(
                0x11111111,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new CardSpec(
                0x22222222,
                new ModernLocInfoV1(0, 0x04, 1, 0x05)));
        FlatPromptProjectionResultV1 addressed =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                SelectTributeMessage(
                    0,
                    false,
                    1,
                    1,
                    (0x11111111u,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        (byte)1)),
                authority.Mirror,
                authority.Projection);
        AssertSuccess(addressed, FlatPromptFamilyValueV1.MsgSelectTribute);
        FlatPromptTributeSelectionLocatorPromptCodeCandidateV1 addressedCandidate =
            addressed.Candidates![0] as
                FlatPromptTributeSelectionLocatorPromptCodeCandidateV1 ??
            throw new InvalidOperationException("expected tribute locator");
        Equal("p0:MONSTER_ZONE:0",
            addressedCandidate.PublicSemanticCardLocator.Value);

        FlatPromptContinuationStepResultV1 firstPick = session
            .TryApplySelection(Capture(
                session,
                "MSG_SELECT_TRIBUTE:PICK:0"));
        True(firstPick.IsSuccess, firstPick.Error.ToString());
        EqualKeys(
            new[]
            {
                "MSG_SELECT_TRIBUTE:PICK:1",
                "MSG_SELECT_TRIBUTE:FINISH"
            },
            firstPick.Projection!.Candidates!
                .Select(value => value.I4LocalCandidateKey)
                .ToArray());
        FlatPromptContinuationStepResultV1 secondPick = session
            .TryApplySelection(Capture(
                session,
                "MSG_SELECT_TRIBUTE:PICK:1"));
        True(secondPick.IsSuccess, secondPick.Error.ToString());
        EqualKeys(
            new[] { "MSG_SELECT_TRIBUTE:FINISH" },
            secondPick.Projection!.Candidates!
                .Select(value => value.I4LocalCandidateKey)
                .ToArray());
        FlatPromptContinuationStepResultV1 finished = session
            .TryApplySelection(Capture(
                session,
                "MSG_SELECT_TRIBUTE:FINISH"));
        True(finished.IsSuccess, finished.Error.ToString());
        BytesEqual(
            new byte[] { 0x03, 0x00, 0x00, 0x00, 0x03 },
            finished.TerminalResponseBody.ToArray());

        FlatPromptProjectionResultV1 weighted =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                SelectTributeMessage(
                    0,
                    false,
                    3,
                    2,
                    (0x01010101u, new ModernLocInfoV1(0, 0x04, 0, 0),
                        (byte)2),
                    (0x02020202u, new ModernLocInfoV1(0, 0x04, 1, 0),
                        (byte)1),
                    (0x03030303u, new ModernLocInfoV1(0, 0x04, 2, 0),
                        (byte)1)));
        AssertSuccess(weighted, FlatPromptFamilyValueV1.MsgSelectTribute);
        EqualKeys(
            new[] { "MSG_SELECT_TRIBUTE:PICK:0" },
            weighted.Candidates!.Select(value => value.I4LocalCandidateKey)
                .ToArray());

        FlatPromptSessionV1 weightedSession = new();
        AssertSuccess(
            weightedSession.TryAcceptI5Prompt(
                SelectTributeMessage(
                    0,
                    false,
                    3,
                    2,
                    (0x01010101u,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        (byte)2),
                    (0x02020202u,
                        new ModernLocInfoV1(0, 0x04, 1, 0),
                        (byte)1),
                    (0x03030303u,
                        new ModernLocInfoV1(0, 0x04, 2, 0),
                        (byte)1))),
            FlatPromptFamilyValueV1.MsgSelectTribute);
        FlatPromptContinuationStepResultV1 weightedFirst =
            weightedSession.TryApplySelection(Capture(
                weightedSession,
                "MSG_SELECT_TRIBUTE:PICK:0"));
        True(weightedFirst.IsSuccess, weightedFirst.Error.ToString());
        EqualKeys(
            new[]
            {
                "MSG_SELECT_TRIBUTE:PICK:1",
                "MSG_SELECT_TRIBUTE:PICK:2"
            },
            weightedFirst.Projection!.Candidates!
                .Select(value => value.I4LocalCandidateKey)
                .ToArray());
        FlatPromptContinuationStepResultV1 weightedSecond =
            weightedSession.TryApplySelection(Capture(
                weightedSession,
                "MSG_SELECT_TRIBUTE:PICK:1"));
        True(weightedSecond.IsSuccess, weightedSecond.Error.ToString());
        FlatPromptContinuationStepResultV1 weightedFinish =
            weightedSession.TryApplySelection(Capture(
                weightedSession,
                "MSG_SELECT_TRIBUTE:FINISH"));
        True(weightedFinish.IsSuccess, weightedFinish.Error.ToString());
        BytesEqual(
            new byte[] { 0x03, 0x00, 0x00, 0x00, 0x03 },
            weightedFinish.TerminalResponseBody.ToArray());

        FlatPromptProjectionResultV1 overlay =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                SelectTributeMessage(
                    0,
                    false,
                    1,
                    1,
                    (0x04040404u,
                        new ModernLocInfoV1(0, 0x84, 0, 0),
                        (byte)1)));
        AssertFailureResult(
            overlay,
            FlatPromptErrorCodeV1.UnprovenPublicReference);

        FlatPromptSessionV1 ownershipSession = new();
        byte[] owned = SelectTributeMinimal.ToArray();
        FlatPromptProjectionResultV1 ownedResult =
            ownershipSession.TryAcceptI5Prompt(owned);
        AssertSuccess(ownedResult, FlatPromptFamilyValueV1.MsgSelectTribute);
        string originalKey = ownedResult.Candidates![0].I4LocalCandidateKey;
        owned[0] = 0xFF;
        owned[15] = 0xAA;
        Equal(originalKey, ownedResult.Candidates[0].I4LocalCandidateKey);
        FlatPromptContinuationStepResultV1 ownedPick = ownershipSession
            .TryApplySelection(Capture(ownershipSession, originalKey));
        True(ownedPick.IsSuccess, ownedPick.Error.ToString());

        FlatPromptSessionV1 cancelSession = new();
        AssertSuccess(
            cancelSession.TryAcceptI5Prompt(
                SelectTributeMessage(
                    0,
                    true,
                    1,
                    1,
                    (0x05050505u,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        (byte)1))),
            FlatPromptFamilyValueV1.MsgSelectTribute);
        FlatPromptContinuationStepResultV1 tributeCancelled =
            cancelSession.TryApplySelection(Capture(
                cancelSession,
                "MSG_SELECT_TRIBUTE:CANCEL"));
        True(tributeCancelled.IsSuccess, tributeCancelled.Error.ToString());
        BytesEqual(
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
            tributeCancelled.TerminalResponseBody.ToArray());

        AssertFailure(
            new FlatPromptSessionV1(),
            SelectTributeMessage(
                0,
                false,
                1,
                1),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            new FlatPromptSessionV1(),
            SelectTributeMessage(
                0,
                false,
                1,
                1,
                (0x06060606u,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    (byte)0)),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            new FlatPromptSessionV1(),
            SelectTributeMessage(
                0,
                false,
                1,
                6,
                (0x07070707u,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    (byte)1)),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            new FlatPromptSessionV1(),
            SelectTributeMessage(
                0,
                false,
                1,
                1,
                (0x08080808u,
                    new ModernLocInfoV1(0, 0x03, 0, 0),
                    (byte)1)),
            FlatPromptErrorCodeV1.InvalidLocation);
        AssertFailure(
            new FlatPromptSessionV1(),
            Append(SelectTributeMinimal, 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            SelectTributeMessage(
                0,
                false,
                1,
                1,
                (0x0C0C0C0Cu,
                    new ModernLocInfoV1(2, 0x04, 0, 0),
                    (byte)1)),
            FlatPromptErrorCodeV1.InvalidParticipant);

        AssertFailure(
            new FlatPromptSessionV1(),
            SelectTributeMessage(
                0,
                false,
                0,
                1,
                (0x0A0A0A0Au,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    (byte)1)),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        FlatPromptSessionV1 zeroMinimumSession = new();
        FlatPromptProjectionResultV1 zeroMinimum =
            zeroMinimumSession.TryAcceptI5Prompt(
                SelectTributeMessage(
                    0,
                    true,
                    0,
                    1,
                    (0x0B0B0B0Bu,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        (byte)1)));
        AssertSuccess(
            zeroMinimum,
            FlatPromptFamilyValueV1.MsgSelectTribute);
        True(zeroMinimum.Candidates!.Any(value =>
            value is FlatPromptFinishPublicCandidateV1));

        FlatPromptSessionV1 singletonSession = new();
        FlatPromptProjectionResultV1 singleton =
            singletonSession.TryAcceptI5Prompt(
                SelectTributeMessage(
                    0,
                    false,
                    1,
                    1,
                    (0x09090909u,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        (byte)1)));
        AssertSuccess(singleton, FlatPromptFamilyValueV1.MsgSelectTribute);
        Equal(1, singleton.Candidates!.Count);
        FlatPromptSelectionHandleV1 singletonHandle = Capture(
            singletonSession,
            "MSG_SELECT_TRIBUTE:PICK:0");
        True(singletonSession.TryResolveSelection(
            singletonHandle,
            out FlatPromptResponseResolutionV1 singletonResponse,
            out FlatPromptErrorCodeV1 singletonError),
            singletonError.ToString());
        Equal(0, singletonResponse.ResponseI32);
    }

    internal static void TestSelectUnselectAndAnnounceNumber()
    {
        FlatPromptSessionV1 selectUnselectSession = new();
        FlatPromptProjectionResultV1 selectUnselect =
            selectUnselectSession.TryAcceptI5Prompt(
                SelectUnselectMinimal.ToArray());
        AssertSuccess(
            selectUnselect,
            FlatPromptFamilyValueV1.MsgSelectUnselectCard);
        FlatPromptSelectUnselectCardPublicContextV1 context =
            selectUnselect.Context as FlatPromptSelectUnselectCardPublicContextV1 ??
            throw new InvalidOperationException(
                "expected SELECT_UNSELECT context");
        Equal(0u, context.ActingPlayer);
        True(context.Finishable);
        True(context.Cancelable);
        Equal(1u, context.MinimumCount);
        Equal(2u, context.MaximumCount);
        Equal(1, context.SelectableCount);
        Equal(1, context.UnselectableCount);
        EqualKeys(
            new[]
            {
                "MSG_SELECT_UNSELECT_CARD:SELECT:0",
                "MSG_SELECT_UNSELECT_CARD:UNSELECT:0",
                "MSG_SELECT_UNSELECT_CARD:FINISH_OR_CANCEL"
            },
            selectUnselect.Candidates!
                .Select(value => value.I4LocalCandidateKey)
                .ToArray());
        FlatPromptSelectUnselectCardCandidateBaseV1 selectCandidate =
            selectUnselect.Candidates![0] as
                FlatPromptSelectUnselectCardCandidateBaseV1 ??
            throw new InvalidOperationException("expected select candidate");
        Equal(FlatPromptChoiceKindV1.Select, selectCandidate.ChoiceKind);
        Equal(FlatPromptSourceSectionV1.Selectable,
            selectCandidate.SourceSection);
        Equal(1u,
            ((FlatPromptSelectUnselectPromptCodeCandidateV1)
                selectCandidate).PromptLocalCardCode);
        FlatPromptSelectUnselectCardCandidateBaseV1 unselectCandidate =
            selectUnselect.Candidates![1] as
                FlatPromptSelectUnselectCardCandidateBaseV1 ??
            throw new InvalidOperationException("expected unselect candidate");
        Equal(FlatPromptChoiceKindV1.Unselect, unselectCandidate.ChoiceKind);
        Equal(FlatPromptSourceSectionV1.Unselectable,
            unselectCandidate.SourceSection);
        Equal(1u,
            ((FlatPromptSelectUnselectPromptCodeCandidateV1)
                unselectCandidate).PromptLocalCardCode);
        False(selectCandidate.GetType().GetProperties().Any(property =>
            property.Name.Contains("Combined", StringComparison.Ordinal)));

        string[] selectUnselectKeys = selectUnselect.Candidates!
            .Select(value => value.I4LocalCandidateKey)
            .ToArray();
        False(CurrentFlatPromptBindingV1.TryCreate(
            0,
            FlatPromptFamilyValueV1.MsgSelectUnselectCard,
            selectUnselect.Candidates.ToArray(),
            selectUnselectKeys,
            new[] { 0, 1, -1 },
            out CurrentFlatPromptBindingV1? swappedBinding,
            out FlatPromptErrorCodeV1 swappedError,
            new byte[][]
            {
                new byte[] { 0x01, 0x00, 0x00, 0x00,
                    0x01, 0x00, 0x00, 0x00 },
                new byte[] { 0x01, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00 },
                new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }
            }));
        Null(swappedBinding);
        Equal(FlatPromptErrorCodeV1.InvalidResponseBinding, swappedError);

        FlatPromptSelectionHandleV1 selectHandle = Capture(
            selectUnselectSession,
            "MSG_SELECT_UNSELECT_CARD:SELECT:0");
        True(selectUnselectSession.TryResolveSelection(
            selectHandle,
            out FlatPromptResponseResolutionV1 selectResponse,
            out FlatPromptErrorCodeV1 selectResponseError),
            selectResponseError.ToString());
        Equal(0, selectResponse.ResponseI32);
        FlatPromptContinuationStepResultV1 selected =
            selectUnselectSession.TryApplySelection(selectHandle);
        True(selected.IsSuccess, selected.Error.ToString());
        True(selected.IsTerminal);
        BytesEqual(
            new byte[] { 0x01, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00 },
            selected.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 unselectSession = new();
        AssertSuccess(
            unselectSession.TryAcceptI5Prompt(SelectUnselectMinimal),
            FlatPromptFamilyValueV1.MsgSelectUnselectCard);
        FlatPromptContinuationStepResultV1 unselected =
            unselectSession.TryApplySelection(Capture(
                unselectSession,
                "MSG_SELECT_UNSELECT_CARD:UNSELECT:0"));
        True(unselected.IsSuccess, unselected.Error.ToString());
        BytesEqual(
            new byte[] { 0x01, 0x00, 0x00, 0x00,
                0x01, 0x00, 0x00, 0x00 },
            unselected.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 finishOrCancelSession = new();
        AssertSuccess(
            finishOrCancelSession.TryAcceptI5Prompt(SelectUnselectMinimal),
            FlatPromptFamilyValueV1.MsgSelectUnselectCard);
        FlatPromptContinuationStepResultV1 finishOrCancel =
            finishOrCancelSession.TryApplySelection(Capture(
                finishOrCancelSession,
                "MSG_SELECT_UNSELECT_CARD:FINISH_OR_CANCEL"));
        True(finishOrCancel.IsSuccess, finishOrCancel.Error.ToString());
        BytesEqual(
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
            finishOrCancel.TerminalResponseBody.ToArray());

        AssertSelectUnselectTerminalFlags();
        AssertSelectUnselectFailuresAndAuthority();

        FlatPromptSessionV1 numberSession = new();
        FlatPromptProjectionResultV1 numbers =
            numberSession.TryAcceptI5Prompt(AnnounceNumberMinimal);
        AssertSuccess(numbers, FlatPromptFamilyValueV1.MsgAnnounceNumber);
        FlatPromptAnnounceNumberPublicContextV1 numberContext =
            numbers.Context as FlatPromptAnnounceNumberPublicContextV1 ??
            throw new InvalidOperationException(
                "expected ANNOUNCE_NUMBER context");
        Equal(0u, numberContext.ActingPlayer);
        Equal(1, numberContext.OptionCount);
        FlatPromptAnnounceNumberPublicCandidateV1 numberCandidate =
            numbers.Candidates![0] as
                FlatPromptAnnounceNumberPublicCandidateV1 ??
            throw new InvalidOperationException("expected number candidate");
        Equal(0, numberCandidate.SourceOrdinal);
        Equal(7ul, numberCandidate.NumberValue);
        Equal(
            "MSG_ANNOUNCE_NUMBER:OPTION:0",
            numberCandidate.I4LocalCandidateKey);
        FlatPromptSelectionHandleV1 numberSelection = Capture(
            numberSession,
            "MSG_ANNOUNCE_NUMBER:OPTION:0");
        True(numberSession.TryResolveSelection(
            numberSelection,
            out FlatPromptResponseResolutionV1 numberResponse,
            out FlatPromptErrorCodeV1 numberResponseError),
            numberResponseError.ToString());
        Equal(0, numberResponse.ResponseI32);
        FlatPromptContinuationStepResultV1 numberResult = numberSession
            .TryApplySelection(numberSelection);
        True(numberResult.IsSuccess, numberResult.Error.ToString());
        True(numberResult.IsTerminal);
        BytesEqual(new byte[] { 0x00, 0x00, 0x00, 0x00 },
            numberResult.TerminalResponseBody.ToArray());

        FlatPromptProjectionResultV1 duplicateNumbers =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                AnnounceNumberDuplicates);
        AssertSuccess(
            duplicateNumbers,
            FlatPromptFamilyValueV1.MsgAnnounceNumber);
        Equal(3, duplicateNumbers.Candidates!.Count);
        Equal(7ul,
            ((FlatPromptAnnounceNumberPublicCandidateV1)
                duplicateNumbers.Candidates[0]).NumberValue);
        Equal(7ul,
            ((FlatPromptAnnounceNumberPublicCandidateV1)
                duplicateNumbers.Candidates[1]).NumberValue);
        NotEqual(
            duplicateNumbers.Candidates[0].I4LocalCandidateKey,
            duplicateNumbers.Candidates[1].I4LocalCandidateKey);
        FlatPromptSessionV1 duplicateNumberSession = new();
        AssertSuccess(
            duplicateNumberSession.TryAcceptI5Prompt(AnnounceNumberDuplicates),
            FlatPromptFamilyValueV1.MsgAnnounceNumber);
        FlatPromptContinuationStepResultV1 selectedNumber =
            duplicateNumberSession.TryApplySelection(Capture(
                duplicateNumberSession,
                "MSG_ANNOUNCE_NUMBER:OPTION:2"));
        BytesEqual(new byte[] { 0x02, 0x00, 0x00, 0x00 },
            selectedNumber.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 ordinalSession = new();
        AssertSuccess(
            ordinalSession.TryAcceptI5Prompt(AnnounceNumberMinimal),
            FlatPromptFamilyValueV1.MsgAnnounceNumber);
        FlatPromptSelectionHandleV1 numberHandle = Capture(
            ordinalSession,
            "MSG_ANNOUNCE_NUMBER:OPTION:0");
        AssertSuccess(
            ordinalSession.TryAcceptI5Prompt(AnnounceNumberMinimal),
            FlatPromptFamilyValueV1.MsgAnnounceNumber);
        False(ordinalSession.TryResolveSelection(
            numberHandle,
            out _,
            out FlatPromptErrorCodeV1 stale));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, stale);
        Equal(1ul, Capture(
            ordinalSession,
            "MSG_ANNOUNCE_NUMBER:OPTION:0").PromptInstanceOrdinal);

        AssertPublicBoundary();
        AssertAnnounceNumberFailures();
    }

    private static void AssertCardIndexCodecs()
    {
        True(FlatPromptProjectionV1.TryEncodeCardIndexResponse(
                new[] { 0 },
                out byte[] type3,
                out FlatPromptErrorCodeV1 type3Error),
            type3Error.ToString());
        BytesEqual(new byte[] { 0x03, 0x00, 0x00, 0x00, 0x01 }, type3);

        True(FlatPromptProjectionV1.TryEncodeCardIndexResponse(
                new[] { 255 },
                out byte[] type1,
                out FlatPromptErrorCodeV1 type1Error),
            type1Error.ToString());
        Equal((byte)1, type1[0]);
        Equal(10, type1.Length);
        Equal(255, BinaryPrimitives.ReadUInt16LittleEndian(
            type1.AsSpan(8, 2)));

        True(FlatPromptProjectionV1.TryEncodeCardIndexResponse(
                new[] { 65535 },
                out byte[] type0,
                out FlatPromptErrorCodeV1 type0Error),
            type0Error.ToString());
        Equal((byte)0, type0[0]);
        Equal(12, type0.Length);
        Equal(65535u, BinaryPrimitives.ReadUInt32LittleEndian(
            type0.AsSpan(8, 4)));

        False(FlatPromptProjectionV1.TryEncodeCardIndexResponse(
            new[] { 1, 0 },
            out _,
            out FlatPromptErrorCodeV1 invalidOrderError));
        Equal(FlatPromptErrorCodeV1.InvalidResponseBinding, invalidOrderError);

        True(FlatPromptProjectionV1.TryEncodeCardIndexResponse(
                new[] { 8 },
                out byte[] type2,
                out FlatPromptErrorCodeV1 type2Error),
            type2Error.ToString());
        Equal((byte)2, type2[0]);
        Equal(9, type2.Length);
        Equal(8, type2[8]);

        True(FlatPromptProjectionV1.TryEncodeCardIndexResponse(
                Array.Empty<int>(),
                out byte[] empty,
                out FlatPromptErrorCodeV1 emptyError),
            emptyError.ToString());
        Equal((byte)2, empty[0]);
        Equal(8, empty.Length);
        Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(
            empty.AsSpan(4, 4)));

        FlatOptionPublicCandidateDescriptorV1 wrongType =
            new(
                FlatPromptKeyV1.SelectCardPickPrefix + "0",
                0,
                1);
        False(CurrentFlatPromptBindingV1.TryCreate(
            0,
            FlatPromptFamilyValueV1.MsgSelectCard,
            new FlatPublicCandidateDescriptorV1[] { wrongType },
            new[] { wrongType.I4LocalCandidateKey },
            new[] { 0 },
            out CurrentFlatPromptBindingV1? wrongTypeBinding,
            out FlatPromptErrorCodeV1 wrongTypeError));
        Null(wrongTypeBinding);
        Equal(
            FlatPromptErrorCodeV1.InvalidResponseBinding,
            wrongTypeError);
    }

    private static void AssertSelectCardFailuresAndOwnership()
    {
        AssertFailure(
            new FlatPromptSessionV1(),
            SelectCardMinimal[..28],
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            Append(SelectCardMinimal, 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);

        byte[] invalidPlayer = SelectCardMinimal.ToArray();
        invalidPlayer[1] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            invalidPlayer,
            FlatPromptErrorCodeV1.InvalidParticipant);

        byte[] invalidFlag = SelectCardMinimal.ToArray();
        invalidFlag[2] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            invalidFlag,
            FlatPromptErrorCodeV1.InvalidBoolean);

        byte[] invalidMaximum = SelectCardMinimal.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(
            invalidMaximum.AsSpan(7, 4),
            2);
        AssertFailure(
            new FlatPromptSessionV1(),
            invalidMaximum,
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);

        FlatPromptSessionV1 ownershipSession = new();
        byte[] ownedSource = SelectCardMinimal.ToArray();
        FlatPromptProjectionResultV1 owned =
            ownershipSession.TryAcceptI5Prompt(ownedSource);
        AssertSuccess(owned, FlatPromptFamilyValueV1.MsgSelectCard);
        ownedSource[15] = 0xFF;
        Equal(0x11223344u,
            ((FlatPromptCardSelectionPromptCodeCandidateV1)
                owned.Candidates![0]).PromptLocalCardCode);
        True(ownershipSession.TryApplySelection(Capture(
                ownershipSession,
                "MSG_SELECT_CARD:PICK:0")).IsSuccess);
        FlatPromptContinuationStepResultV1 ownedFinish =
            ownershipSession.TryApplySelection(Capture(
                ownershipSession,
                "MSG_SELECT_CARD:FINISH"));
        BytesEqual(
            new byte[] { 0x03, 0x00, 0x00, 0x00, 0x01 },
            ownedFinish.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 failedSession = new();
        AssertSuccess(
            failedSession.TryAcceptI5Prompt(SelectCardMinimal),
            FlatPromptFamilyValueV1.MsgSelectCard);
        FlatPromptSelectionHandleV1 oldHandle = Capture(
            failedSession,
            "MSG_SELECT_CARD:PICK:0");
        AssertFailureResult(
            failedSession.TryAcceptI5Prompt(Append(SelectCardMinimal, 0xAA)),
            FlatPromptErrorCodeV1.MalformedPrompt);
        False(failedSession.TryResolveSelection(
            oldHandle,
            out _,
            out FlatPromptErrorCodeV1 failureStale));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, failureStale);
        AssertSuccess(
            failedSession.TryAcceptI5Prompt(SelectCardMinimal),
            FlatPromptFamilyValueV1.MsgSelectCard);
        Equal(1ul, Capture(
            failedSession,
            "MSG_SELECT_CARD:PICK:0").PromptInstanceOrdinal);

        FlatPromptSessionV1 familySession = new();
        AssertSuccess(
            familySession.TryAcceptI5Prompt(SelectCardMinimal),
            FlatPromptFamilyValueV1.MsgSelectCard);
        FlatPromptSelectionHandleV1 cardHandle = Capture(
            familySession,
            "MSG_SELECT_CARD:PICK:0");
        FlatPromptSelectionHandleV1 wrongFamilyHandle =
            new(
                cardHandle.PromptInstanceOrdinal,
                FlatPromptFamilyValueV1.MsgSelectTribute,
                cardHandle.I4LocalCandidateKey,
                cardHandle.OrderedDomain,
                cardHandle.ContinuationStep);
        FlatPromptContinuationStepResultV1 wrongFamily =
            familySession.TryApplySelection(wrongFamilyHandle);
        False(wrongFamily.IsSuccess);
        Equal(
            FlatPromptErrorCodeV1.InvalidContinuationInstance,
            wrongFamily.Error);

        FlatPromptSessionV1 crossFamilySession = new();
        AssertSuccess(
            crossFamilySession.TryAcceptI5Prompt(SelectCardMinimal),
            FlatPromptFamilyValueV1.MsgSelectCard);
        FlatPromptSelectionHandleV1 staleCardHandle = Capture(
            crossFamilySession,
            "MSG_SELECT_CARD:PICK:0");
        AssertSuccess(
            crossFamilySession.TryAcceptI5Prompt(AnnounceNumberMinimal),
            FlatPromptFamilyValueV1.MsgAnnounceNumber);
        FlatPromptContinuationStepResultV1 crossFamily =
            crossFamilySession.TryApplySelection(staleCardHandle);
        False(crossFamily.IsSuccess);
        Equal(
            FlatPromptErrorCodeV1.InvalidContinuationInstance,
            crossFamily.Error);
        True(crossFamilySession.TryCaptureSelection(
            "MSG_ANNOUNCE_NUMBER:OPTION:0",
            out _,
            out FlatPromptErrorCodeV1 currentFamilyError),
            currentFamilyError.ToString());

        FlatPromptSessionV1 invalidActionSession = new();
        AssertSuccess(
            invalidActionSession.TryAcceptI5Prompt(SelectCardMinimal),
            FlatPromptFamilyValueV1.MsgSelectCard);
        FlatPromptSelectionHandleV1 validHandle = Capture(
            invalidActionSession,
            "MSG_SELECT_CARD:PICK:0");
        FlatPromptSelectionHandleV1 forgedKeyHandle = new(
            validHandle.PromptInstanceOrdinal,
            validHandle.Family,
            "MSG_SELECT_CARD:FINISH",
            validHandle.OrderedDomain,
            validHandle.ContinuationStep);
        FlatPromptContinuationStepResultV1 invalidAction =
            invalidActionSession.TryApplySelection(forgedKeyHandle);
        False(invalidAction.IsSuccess);
        Equal(
            FlatPromptErrorCodeV1.InvalidI4LocalCandidateKey,
            invalidAction.Error);
        AssertSuccess(
            invalidActionSession.TryAcceptI5Prompt(SelectCardMinimal),
            FlatPromptFamilyValueV1.MsgSelectCard);
        Equal(1ul, Capture(
            invalidActionSession,
            "MSG_SELECT_CARD:PICK:0").PromptInstanceOrdinal);

        AssertFailure(
            new FlatPromptSessionV1(),
            SelectCardMessage(0, false, 1, 0),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            new FlatPromptSessionV1(),
            SelectCardMessage(0, false, 2, 1,
                (1u, new ModernLocInfoV1(0, 0, 0, 0))),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            new FlatPromptSessionV1(),
            SelectCardMessage(0, false, 1, 2,
                (1u, new ModernLocInfoV1(0, 0, 0, 0))),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);

        AssertFailure(
            new FlatPromptSessionV1(),
            SelectCardMessage(0, false, 1, 1,
                (1u, new ModernLocInfoV1(0, 0x03, 0, 0))),
            FlatPromptErrorCodeV1.InvalidLocation);
        AssertFailure(
            new FlatPromptSessionV1(),
            SelectCardMessage(
                0,
                false,
                0,
                1,
                (1u, new ModernLocInfoV1(0, 0, 0, 0))),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        FlatPromptSessionV1 zeroMinimumSession = new();
        FlatPromptProjectionResultV1 zeroMinimum =
            zeroMinimumSession.TryAcceptI5Prompt(
                SelectCardMessage(
                    0,
                    true,
                    0,
                    1,
                    (1u, new ModernLocInfoV1(0, 0, 0, 0))));
        AssertSuccess(zeroMinimum, FlatPromptFamilyValueV1.MsgSelectCard);
        EqualKeys(
            new[]
            {
                "MSG_SELECT_CARD:PICK:0",
                "MSG_SELECT_CARD:FINISH",
                "MSG_SELECT_CARD:CANCEL"
            },
            zeroMinimum.Candidates!.Select(value => value.I4LocalCandidateKey)
                .ToArray());
        FlatPromptContinuationStepResultV1 emptyFinish =
            zeroMinimumSession.TryApplySelection(Capture(
                zeroMinimumSession,
                "MSG_SELECT_CARD:FINISH"));
        True(emptyFinish.IsSuccess, emptyFinish.Error.ToString());
        BytesEqual(
            new byte[]
            {
                0x02, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            },
            emptyFinish.TerminalResponseBody.ToArray());

        byte[] countOverflow = SelectCardMinimal.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(
            countOverflow.AsSpan(11, 4),
            65537);
        AssertFailure(
            new FlatPromptSessionV1(),
            countOverflow,
            FlatPromptErrorCodeV1.ArithmeticFailure);

        FlatPromptSessionV1 monotonicSession = new();
        AssertSuccess(
            monotonicSession.TryAcceptI5Prompt(SelectCardDuplicates),
            FlatPromptFamilyValueV1.MsgSelectCard);
        FlatPromptContinuationStepResultV1 monotonicPick =
            monotonicSession.TryApplySelection(Capture(
                monotonicSession,
                "MSG_SELECT_CARD:PICK:1"));
        True(monotonicPick.IsSuccess, monotonicPick.Error.ToString());
        False(monotonicSession.TryCaptureSelection(
            "MSG_SELECT_CARD:PICK:0",
            out _,
            out FlatPromptErrorCodeV1 decreasingError));
        Equal(FlatPromptErrorCodeV1.InvalidI4LocalCandidateKey,
            decreasingError);

        FlatPromptSelectionHandleV1 currentFinish = Capture(
            monotonicSession,
            "MSG_SELECT_CARD:FINISH");
        FlatPromptContinuationStepResultV1 currentFinished =
            monotonicSession.TryApplySelection(currentFinish);
        True(currentFinished.IsSuccess, currentFinished.Error.ToString());
        IList<byte> terminalBody =
            (IList<byte>)currentFinished.TerminalResponseBody;
        try
        {
            terminalBody[0] = 0xFF;
            throw new InvalidOperationException(
                "terminal response body was mutable");
        }
        catch (NotSupportedException)
        {
        }
    }

    private static void AssertSelectCardAuthorityFailures(Authority authority)
    {
        byte[] validMessage = SelectCardMessage(
            0,
            false,
            1,
            1,
            (0x55667788u, new ModernLocInfoV1(0, 0x04, 0, 0)));
        AssertFailureResult(
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                validMessage,
                null,
                authority.Projection),
            FlatPromptErrorCodeV1.UnprovenPublicReference);
        AssertFailureResult(
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                validMessage,
                authority.Mirror,
                PublicStateProjectionResultV1.Failure(
                    PublicStateProjectionErrorV1.InvalidSnapshot)),
            FlatPromptErrorCodeV1.UnprovenPublicReference);
        FlatPromptSessionV1 session = new();
        AssertSuccess(
            session.TryAcceptI5Prompt(
                validMessage,
                authority.Mirror,
                authority.Projection),
            FlatPromptFamilyValueV1.MsgSelectCard);
        FlatPromptSelectionHandleV1 oldHandle = Capture(
            session,
            "MSG_SELECT_CARD:PICK:0");

        byte[] changedCanonical = authority.Projection.CanonicalBytes.ToArray();
        changedCanonical[0] ^= 0x01;
        PublicStateProjectionResultV1 changedProjection =
            PublicStateProjectionResultV1.Success(
                authority.Projection.Snapshot!,
                changedCanonical,
                authority.Projection.Sha256!);
        AssertFailureResult(
            session.TryAcceptI5Prompt(
                validMessage,
                authority.Mirror,
                changedProjection),
            FlatPromptErrorCodeV1.AuthorityMismatch);
        False(session.TryResolveSelection(
            oldHandle,
            out _,
            out FlatPromptErrorCodeV1 staleAfterCanonicalMismatch));
        Equal(
            FlatPromptErrorCodeV1.StalePromptBinding,
            staleAfterCanonicalMismatch);

        AssertSuccess(
            session.TryAcceptI5Prompt(
                validMessage,
                authority.Mirror,
                authority.Projection),
            FlatPromptFamilyValueV1.MsgSelectCard);
        Equal(1ul, Capture(
            session,
            "MSG_SELECT_CARD:PICK:0").PromptInstanceOrdinal);

        PublicStateProjectionResultV1 wrongSha =
            PublicStateProjectionResultV1.Success(
                authority.Projection.Snapshot!,
                authority.Projection.CanonicalBytes.ToArray(),
                "00");
        AssertFailureResult(
            session.TryAcceptI5Prompt(
                validMessage,
                authority.Mirror,
                wrongSha),
            FlatPromptErrorCodeV1.AuthorityMismatch);
    }

    private static void AssertSelectUnselectTerminalFlags()
    {
        byte[] oneCard = SelectUnselectMessage(
            0,
            true,
            false,
            1,
            1,
            new[]
            {
                (0x11111111u, new ModernLocInfoV1(0, 0x04, 0, 0))
            },
            Array.Empty<(uint, ModernLocInfoV1)>());
        FlatPromptProjectionResultV1 finishOnly =
            new FlatPromptSessionV1().TryAcceptI5Prompt(oneCard);
        AssertSuccess(
            finishOnly,
            FlatPromptFamilyValueV1.MsgSelectUnselectCard);
        True(finishOnly.Candidates!.Any(value =>
            value is FlatPromptFinishPublicCandidateV1));
        False(finishOnly.Candidates!.Any(value =>
            value is FlatPromptCancelPublicCandidateV1));

        FlatPromptProjectionResultV1 cancelOnly =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                SelectUnselectMessage(
                    0,
                    false,
                    true,
                    1,
                    1,
                    new[]
                    {
                        (0x11111111u,
                            new ModernLocInfoV1(0, 0x04, 0, 0))
                    },
                    Array.Empty<(uint, ModernLocInfoV1)>()));
        AssertSuccess(
            cancelOnly,
            FlatPromptFamilyValueV1.MsgSelectUnselectCard);
        True(cancelOnly.Candidates!.Any(value =>
            value is FlatPromptCancelPublicCandidateV1));
        False(cancelOnly.Candidates!.Any(value =>
            value is FlatPromptFinishPublicCandidateV1));

        FlatPromptProjectionResultV1 neither =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                SelectUnselectMessage(
                    0,
                    false,
                    false,
                    1,
                    1,
                    new[]
                    {
                        (0x11111111u,
                            new ModernLocInfoV1(0, 0x04, 0, 0))
                    },
                    Array.Empty<(uint, ModernLocInfoV1)>()));
        AssertSuccess(
            neither,
            FlatPromptFamilyValueV1.MsgSelectUnselectCard);
        False(neither.Candidates!.Any(value =>
            value is FlatPromptFinishPublicCandidateV1 or
                FlatPromptCancelPublicCandidateV1 or
                FlatPromptFinishOrCancelPublicCandidateV1));
    }

    private static void AssertSelectUnselectFailuresAndAuthority()
    {
        AssertFailure(
            new FlatPromptSessionV1(),
            SelectUnselectMessage(
                0,
                false,
                false,
                1,
                1,
                Array.Empty<(uint, ModernLocInfoV1)>(),
                Array.Empty<(uint, ModernLocInfoV1)>()),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);

        byte[] invalidFlag = SelectUnselectMinimal.ToArray();
        invalidFlag[2] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            invalidFlag,
            FlatPromptErrorCodeV1.InvalidBoolean);

        byte[] invalidPlayer = SelectUnselectMinimal.ToArray();
        invalidPlayer[1] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            invalidPlayer,
            FlatPromptErrorCodeV1.InvalidParticipant);

        byte[] invalidLocation = SelectUnselectMinimal.ToArray();
        invalidLocation[21] = 0x03;
        AssertFailure(
            new FlatPromptSessionV1(),
            invalidLocation,
            FlatPromptErrorCodeV1.InvalidLocation);

        byte[] invalidMinimum = SelectUnselectMinimal.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(
            invalidMinimum.AsSpan(4, 4),
            3);
        AssertFailure(
            new FlatPromptSessionV1(),
            invalidMinimum,
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);

        AssertFailure(
            new FlatPromptSessionV1(),
            Append(SelectUnselectMinimal, 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);

        byte[] countOverflow = SelectUnselectMinimal.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(
            countOverflow.AsSpan(12, 4),
            65537);
        AssertFailure(
            new FlatPromptSessionV1(),
            countOverflow,
            FlatPromptErrorCodeV1.ArithmeticFailure);

        Authority authority = CreateAuthority(
            new CardSpec(
                0xABCDEF01,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        FlatPromptSessionV1 addressedSession = new();
        byte[] addressedMessage = SelectUnselectMessage(
            0,
            false,
            false,
            1,
            1,
            new[]
            {
                (0xABCDEF01u, new ModernLocInfoV1(0, 0x04, 0, 0))
            },
            Array.Empty<(uint, ModernLocInfoV1)>());
        FlatPromptProjectionResultV1 addressed =
            addressedSession.TryAcceptI5Prompt(
                addressedMessage,
                authority.Mirror,
                authority.Projection);
        AssertSuccess(
            addressed,
            FlatPromptFamilyValueV1.MsgSelectUnselectCard);
        True(addressed.Candidates![0]
            is FlatPromptSelectUnselectLocatorPromptCodeCandidateV1);
        FlatPromptSelectionHandleV1 addressedHandle = Capture(
            addressedSession,
            "MSG_SELECT_UNSELECT_CARD:SELECT:0");
        True(addressedSession.TryResolveSelection(
            addressedHandle,
            out FlatPromptResponseResolutionV1 response,
            out FlatPromptErrorCodeV1 responseError),
            responseError.ToString());
        Equal(0, response.ResponseI32);
    }

    private static void AssertAnnounceNumberFailures()
    {
        AssertFailure(
            new FlatPromptSessionV1(),
            new byte[] { 0x8F, 0x00, 0x00 },
            FlatPromptErrorCodeV1.ZeroOptionDomain);
        AssertFailure(
            new FlatPromptSessionV1(),
            Append(AnnounceNumberMinimal, 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);

        byte[] invalidPlayer = AnnounceNumberMinimal.ToArray();
        invalidPlayer[1] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            invalidPlayer,
            FlatPromptErrorCodeV1.InvalidParticipant);

        FlatPromptSessionV1 ownershipSession = new();
        byte[] source = AnnounceNumberDuplicates.ToArray();
        FlatPromptProjectionResultV1 owned =
            ownershipSession.TryAcceptI5Prompt(source);
        AssertSuccess(owned, FlatPromptFamilyValueV1.MsgAnnounceNumber);
        ulong originalValue =
            ((FlatPromptAnnounceNumberPublicCandidateV1)
                owned.Candidates![0]).NumberValue;
        source[3] = 0xFF;
        source[4] = 0xFF;
        Equal(
            originalValue,
            ((FlatPromptAnnounceNumberPublicCandidateV1)
                owned.Candidates[0]).NumberValue);

        byte[] maxCount = AnnounceNumberMessage(
            0,
            Enumerable.Range(0, byte.MaxValue)
                .Select(value => (ulong)value)
                .ToArray());
        FlatPromptProjectionResultV1 maxCountResult =
            new FlatPromptSessionV1().TryAcceptI5Prompt(maxCount);
        AssertSuccess(maxCountResult, FlatPromptFamilyValueV1.MsgAnnounceNumber);
        Equal(255, maxCountResult.Candidates!.Count);
    }

    private static void AssertPublicBoundary()
    {
        Type[] publicTypes =
        {
            typeof(FlatPromptCardSelectionPublicContextV1),
            typeof(FlatPromptTributeSelectionPublicContextV1),
            typeof(FlatPromptSelectUnselectCardPublicContextV1),
            typeof(FlatPromptAnnounceNumberPublicContextV1),
            typeof(FlatPromptCardSelectionCandidateBaseV1),
            typeof(FlatPromptCardSelectionAnonymousCandidateV1),
            typeof(FlatPromptCardSelectionPromptCodeCandidateV1),
            typeof(FlatPromptCardSelectionLocatorCandidateV1),
            typeof(FlatPromptCardSelectionLocatorPromptCodeCandidateV1),
            typeof(FlatPromptTributeSelectionCandidateBaseV1),
            typeof(FlatPromptTributeSelectionAnonymousCandidateV1),
            typeof(FlatPromptTributeSelectionPromptCodeCandidateV1),
            typeof(FlatPromptTributeSelectionLocatorCandidateV1),
            typeof(FlatPromptTributeSelectionLocatorPromptCodeCandidateV1),
            typeof(FlatPromptSelectUnselectCardCandidateBaseV1),
            typeof(FlatPromptSelectUnselectAnonymousCandidateV1),
            typeof(FlatPromptSelectUnselectPromptCodeCandidateV1),
            typeof(FlatPromptSelectUnselectLocatorCandidateV1),
            typeof(FlatPromptSelectUnselectLocatorPromptCodeCandidateV1),
            typeof(FlatPromptFinishPublicCandidateV1),
            typeof(FlatPromptCancelPublicCandidateV1),
            typeof(FlatPromptFinishOrCancelPublicCandidateV1),
            typeof(FlatPromptAnnounceNumberPublicCandidateV1)
        };
        string[] forbidden =
        {
            "ResponseI32", "ResponseBody", "ModernLocInfo", "MirrorSnapshot",
            "MirrorEntityId", "ProtocolOffset", "Socket", "Network",
            "Timestamp", "Pid", "CombinedIndex", "ReleaseValue",
            "PrivateResponse", "ContinuationStep", "PromptInstanceOrdinal"
        };
        foreach (Type type in publicTypes)
        {
            True(type.IsPublic, "expected public I5A1 type " + type.Name);
            True(type.IsAbstract || type.IsSealed,
                "expected closed I5A1 type " + type.Name);
            foreach (PropertyInfo property in type.GetProperties())
            {
                False(
                    forbidden.Contains(property.Name, StringComparer.Ordinal),
                    "private property exposed by " + type.Name + "." +
                    property.Name);
            }
        }

        False(typeof(FlatPromptCardContinuationStateV1).IsPublic);
        False(typeof(FlatPromptContinuationStepResultV1).IsPublic);
        False(typeof(FlatPromptFamilyValueV1).IsPublic);
        False(typeof(FlatPromptProjectionV1).IsPublic);
        False(typeof(FlatPromptSelectionHandleV1).IsPublic);
        False(typeof(CurrentFlatPromptBindingV1).IsPublic);
        False(typeof(FlatPromptSessionV1).GetMethods().Any(method =>
            method.IsPublic && method.Name.Contains(
                "Send",
                StringComparison.OrdinalIgnoreCase)));

        AssertFailure(
            new FlatPromptSessionV1(),
            new byte[] { 23 },
            FlatPromptErrorCodeV1.UnsupportedPromptFamily);

        HashSet<byte> supported = new() { 15, 20, 26, 143 };
        for (int rawId = byte.MinValue; rawId <= byte.MaxValue; rawId++)
        {
            byte id = (byte)rawId;
            if (supported.Contains(id))
            {
                continue;
            }

            FlatPromptProjectionResultV1 unsupported =
                new FlatPromptSessionV1().TryAcceptI5Prompt(new[] { id });
            FlatPromptErrorCodeV1 expected = id == 23
                ? FlatPromptErrorCodeV1.UnsupportedPromptFamily
                : FlatPromptErrorCodeV1.UnsupportedPromptLayout;
            AssertFailureResult(unsupported, expected);
        }
    }

    private static FlatPromptSelectionHandleV1 Capture(
        FlatPromptSessionV1 session,
        string key)
    {
        True(session.TryCaptureSelection(
                key,
                out FlatPromptSelectionHandleV1? handle,
                out FlatPromptErrorCodeV1 error),
            error.ToString());
        NotNull(handle);
        return handle!;
    }

    private static void AssertSuccess(
        FlatPromptProjectionResultV1 result,
        FlatPromptFamilyV1 family)
    {
        True(result.IsSuccess, result.Error.ToString());
        Equal(FlatPromptErrorCodeV1.None, result.Error);
        NotNull(result.Context);
        NotNull(result.Candidates);
        Equal(family, result.Context!.PromptFamily);
    }

    private static void EqualKeys(
        IReadOnlyList<string> expected,
        IReadOnlyList<string> actual)
    {
        True(
            expected.SequenceEqual(actual),
            $"expected keys [{string.Join(",", expected)}]; actual keys " +
            $"[{string.Join(",", actual)}]");
    }

    private static void AssertFailure(
        FlatPromptSessionV1 session,
        byte[] message,
        FlatPromptErrorCodeV1 expectedError)
    {
        AssertFailureResult(
            session.TryAcceptI5Prompt(message),
            expectedError);
    }

    private static void AssertFailureResult(
        FlatPromptProjectionResultV1 result,
        FlatPromptErrorCodeV1 expectedError)
    {
        False(result.IsSuccess);
        Equal(expectedError, result.Error);
        Null(result.Context);
        Null(result.Candidates);
    }

    private static byte[] SelectCardMessage(
        byte actingPlayer,
        bool cancelable,
        uint minimum,
        uint maximum,
        params (uint Code, ModernLocInfoV1 Location)[] entries)
    {
        List<byte[]> parts = new()
        {
            new[]
            {
                (byte)15,
                actingPlayer,
                cancelable ? (byte)1 : (byte)0
            },
            U32(minimum),
            U32(maximum),
            U32((uint)entries.Length)
        };
        parts.AddRange(entries.Select(entry => Join(
            U32(entry.Code),
            LocInfo(
                entry.Location.Controller,
                entry.Location.Location,
                entry.Location.Sequence,
                entry.Location.Position))));
        return Join(parts.ToArray());
    }

    private static byte[] SelectTributeMessage(
        byte actingPlayer,
        bool cancelable,
        uint minimum,
        uint maximum,
        params (uint Code, ModernLocInfoV1 Location, byte ReleaseValue)[]
            entries)
    {
        List<byte[]> parts = new()
        {
            new[]
            {
                (byte)20,
                actingPlayer,
                cancelable ? (byte)1 : (byte)0
            },
            U32(minimum),
            U32(maximum),
            U32((uint)entries.Length)
        };
        parts.AddRange(entries.Select(entry => Join(
            U32(entry.Code),
            new[]
            {
                entry.Location.Controller,
                entry.Location.Location
            },
            U32(entry.Location.Sequence),
            new[] { entry.ReleaseValue })));
        return Join(parts.ToArray());
    }

    private static byte[] SelectUnselectMessage(
        byte actingPlayer,
        bool finishable,
        bool cancelable,
        uint minimum,
        uint maximum,
        IReadOnlyList<(uint Code, ModernLocInfoV1 Location)> selectable,
        IReadOnlyList<(uint Code, ModernLocInfoV1 Location)> unselectable)
    {
        List<byte[]> parts = new()
        {
            new[]
            {
                (byte)26,
                actingPlayer,
                finishable ? (byte)1 : (byte)0,
                cancelable ? (byte)1 : (byte)0
            },
            U32(minimum),
            U32(maximum),
            U32((uint)selectable.Count)
        };
        parts.AddRange(selectable.Select(entry => Join(
            U32(entry.Code),
            LocInfo(
                entry.Location.Controller,
                entry.Location.Location,
                entry.Location.Sequence,
                entry.Location.Position))));
        parts.Add(U32((uint)unselectable.Count));
        parts.AddRange(unselectable.Select(entry => Join(
            U32(entry.Code),
            LocInfo(
                entry.Location.Controller,
                entry.Location.Location,
                entry.Location.Sequence,
                entry.Location.Position))));
        return Join(parts.ToArray());
    }

    private static byte[] AnnounceNumberMessage(
        byte actingPlayer,
        params ulong[] values)
    {
        List<byte[]> parts = new()
        {
            new[]
            {
                (byte)143,
                actingPlayer,
                checked((byte)values.Length)
            }
        };
        parts.AddRange(values.Select(U64));
        return Join(parts.ToArray());
    }

    private static byte[] Append(byte[] source, byte value) =>
        source.Concat(new[] { value }).ToArray();

    private static Authority CreateAuthority(params CardSpec[] cards)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                deckCount0: 8,
                extraCount0: 8,
                deckCount1: 8,
                extraCount1: 8);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        foreach (CardSpec card in cards)
        {
            MirrorApplyResult moved = mirror.Apply(DecodeMessage(
                decoder,
                MoveMessage(card.CardCode, empty, card.Location, 0)));
            True(moved.IsSuccess, moved.Error.ToString());
        }

        PublicStateProjectionResultV1 projection =
            PublicStateProjectionV1.TryProject(
                mirror.Snapshot,
                new PublicStateProjectionContextV1(0));
        True(projection.IsSuccess, projection.Error.ToString());
        return new Authority(mirror, projection);
    }

    private readonly record struct Authority(
        PerspectiveStateMirrorV1 Mirror,
        PublicStateProjectionResultV1 Projection);

    private readonly record struct CardSpec(
        uint CardCode,
        ModernLocInfoV1 Location);

    private static readonly byte[] SelectCardMinimal =
    {
        0x0F, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00,
        0x44, 0x33, 0x22, 0x11,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };

    private static readonly byte[] SelectCardMainDeck =
    {
        0x0F, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00,
        0xEF, 0xBE, 0xAD, 0xDE,
        0x00, 0x01, 0x05, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00
    };

    private static readonly byte[] SelectCardDuplicates =
    {
        0x0F, 0x00, 0x01,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00,
        0x02, 0x00, 0x00, 0x00,
        0x78, 0x56, 0x34, 0x12,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x78, 0x56, 0x34, 0x12,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };

    private static readonly byte[] SelectTributeMinimal =
    {
        0x14, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00,
        0x02, 0x00, 0x00, 0x00,
        0x02, 0x00, 0x00, 0x00,
        0x11, 0x11, 0x11, 0x11, 0x00, 0x04,
        0x00, 0x00, 0x00, 0x00, 0x01,
        0x22, 0x22, 0x22, 0x22, 0x00, 0x04,
        0x01, 0x00, 0x00, 0x00, 0x02
    };

    private static readonly byte[] SelectUnselectMinimal =
    {
        0x1A, 0x00, 0x01, 0x01,
        0x01, 0x00, 0x00, 0x00,
        0x02, 0x00, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00, 0x00, 0x04,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x04,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };

    private static readonly byte[] AnnounceNumberMinimal =
    {
        0x8F, 0x00, 0x01,
        0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };

    private static readonly byte[] AnnounceNumberDuplicates =
    {
        0x8F, 0x00, 0x03,
        0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x2A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };
}
