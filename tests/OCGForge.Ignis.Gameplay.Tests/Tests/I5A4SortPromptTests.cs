using System.Reflection;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I5A4SortPromptTests
{
    internal static void TestSortPrompts()
    {
        Run("SORT_CARD basic", AssertSortCardBasic);
        Run("SORT_CHAIN family isolation", AssertSortChainBasicAndFamilyIsolation);
        Run("N1 and cancellation", AssertN1AndCancellation);
        Run("three-source permutation", AssertThreeSourcePermutationAndRemainingDomain);
        Run("duplicate and authority", AssertDuplicateOccurrencesAndAuthority);
        Run("count and wire failures", AssertCountAndWireFailures);
        Run("atomicity and boundaries", AssertAtomicityStalenessOwnershipAndBoundaries);
        Run("public boundary", AssertPublicBoundary);
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                name + ": " + exception.Message,
                exception);
        }
    }

    private static void AssertSortCardBasic()
    {
        byte[] message = SortMessage(
            25,
            0,
            new SortEntry(0x11223344, 0, 0x01, 0),
            new SortEntry(0x55667788, 0, 0x01, 1));
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 result =
            session.TryAcceptI5Prompt(message);
        AssertSuccess(result, FlatPromptFamilyValueV1.MsgSortCard);
        FlatPromptSortSelectionPublicContextV1 context =
            (FlatPromptSortSelectionPublicContextV1)result.Context!;
        Equal(FlatPromptSortKindV1.SortCard, context.SortKind);
        Equal(2, context.SourceCount);
        Equal(2, context.Sources.Count);
        Equal(0, context.Sources[0].SourceOrdinal);
        Equal(1, context.Sources[1].SourceOrdinal);
        Equal(0x11223344u,
            ((FlatPromptSortSourcePromptCodePublicDescriptorV1)
                context.Sources[0]).PromptLocalCardCode);
        Equal(0x55667788u,
            ((FlatPromptSortSourcePromptCodePublicDescriptorV1)
                context.Sources[1]).PromptLocalCardCode);
        AssertKeys(
            new[]
            {
                "MSG_SORT_CARD:PLACE:0",
                "MSG_SORT_CARD:PLACE:1",
                "MSG_SORT_CARD:CANCEL"
            },
            result.Candidates!);
        True(result.Candidates![0]
            is FlatPromptSortPromptCodePublicCandidateV1);
        False(result.Candidates!.Any(candidate =>
            candidate is FlatPromptFinishPublicCandidateV1),
            "SORT domain must not expose FINISH");

        FlatPromptSelectionHandleV1 handle = Capture(
            session,
            "MSG_SORT_CARD:PLACE:0");
        False(session.TryResolveSelection(
            handle,
            out _,
            out FlatPromptErrorCodeV1 scalarError),
            "SORT scalar resolver must be blocked");
        Equal(FlatPromptErrorCodeV1.InvalidContinuationAction, scalarError);
        FlatPromptContinuationStepResultV1 intermediate =
            session.TryApplySelection(handle);
        True(intermediate.IsSuccess, intermediate.Error.ToString());
        False(intermediate.IsTerminal, "SORT PLACE must be intermediate");
        Equal(0, intermediate.TerminalResponseBody.Count);
        AssertKeys(
            new[]
            {
                "MSG_SORT_CARD:PLACE:1",
                "MSG_SORT_CARD:CANCEL"
            },
            intermediate.Projection!.Candidates!);
    }

    private static void AssertSortChainBasicAndFamilyIsolation()
    {
        byte[] message = SortMessage(
            21,
            1,
            new SortEntry(0xAABBCCDD, 1, 0x04, 0),
            new SortEntry(0xEEFF0011, 1, 0x04, 1));
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 result =
            session.TryAcceptI5Prompt(message);
        AssertSuccess(result, FlatPromptFamilyValueV1.MsgSortChain);
        FlatPromptSortSelectionPublicContextV1 context =
            (FlatPromptSortSelectionPublicContextV1)result.Context!;
        Equal(FlatPromptSortKindV1.SortChain, context.SortKind);
        Equal(1, context.ActingPlayer);
        AssertKeys(
            new[]
            {
                "MSG_SORT_CHAIN:PLACE:0",
                "MSG_SORT_CHAIN:PLACE:1",
                "MSG_SORT_CHAIN:CANCEL"
            },
            result.Candidates!);
        True(result.Candidates![0]
            is FlatPromptSortPromptCodePublicCandidateV1);
        Equal(
            FlatPromptSourceSectionV1.SortChainSources,
            ((FlatPromptSortPublicCandidateBaseV1)result.Candidates![0])
                .SourceSection);

        FlatPromptSelectionHandleV1 chainHandle = Capture(
            session,
            "MSG_SORT_CHAIN:PLACE:0");
        FlatPromptSelectionHandleV1 forgedCardFamilyHandle = new(
            chainHandle.PromptInstanceOrdinal,
            FlatPromptFamilyValueV1.MsgSortCard,
            chainHandle.I4LocalCandidateKey,
            chainHandle.OrderedDomain,
            chainHandle.ContinuationStep);
        FlatPromptContinuationStepResultV1 wrongFamily =
            session.TryApplySelection(forgedCardFamilyHandle);
        False(wrongFamily.IsSuccess, "cross-family SORT handle was accepted");
        Equal(
            FlatPromptErrorCodeV1.InvalidContinuationInstance,
            wrongFamily.Error);
    }

    private static void AssertN1AndCancellation()
    {
        byte[] one = SortMessage(
            25,
            0,
            new SortEntry(7, 0, 0x01, 0));
        FlatPromptSessionV1 oneSession = new();
        FlatPromptProjectionResultV1 oneResult =
            oneSession.TryAcceptI5Prompt(one);
        AssertSuccess(oneResult, FlatPromptFamilyValueV1.MsgSortCard);
        AssertKeys(
            new[]
            {
                "MSG_SORT_CARD:PLACE:0",
                "MSG_SORT_CARD:CANCEL"
            },
            oneResult.Candidates!);
        FlatPromptContinuationStepResultV1 oneTerminal =
            oneSession.TryApplySelection(Capture(
                oneSession,
                "MSG_SORT_CARD:PLACE:0"));
        True(oneTerminal.IsTerminal, oneTerminal.Error.ToString());
        BytesEqual(new byte[] { 0x00 }, oneTerminal.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 cancelInitialSession = new();
        AssertSuccess(
            cancelInitialSession.TryAcceptI5Prompt(one),
            FlatPromptFamilyValueV1.MsgSortCard);
        FlatPromptContinuationStepResultV1 cancelInitial =
            cancelInitialSession.TryApplySelection(Capture(
                cancelInitialSession,
                "MSG_SORT_CARD:CANCEL"));
        AssertCancel(cancelInitial);

        byte[] three = SortMessage(
            25,
            0,
            new SortEntry(1, 0, 0x01, 0),
            new SortEntry(2, 0, 0x01, 1),
            new SortEntry(3, 0, 0x01, 2));
        FlatPromptSessionV1 cancelAfterPartialSession = new();
        AssertSuccess(
            cancelAfterPartialSession.TryAcceptI5Prompt(three),
            FlatPromptFamilyValueV1.MsgSortCard);
        FlatPromptContinuationStepResultV1 afterPartial =
            cancelAfterPartialSession.TryApplySelection(Capture(
                cancelAfterPartialSession,
                "MSG_SORT_CARD:PLACE:1"));
        True(afterPartial.IsSuccess, afterPartial.Error.ToString());
        AssertKeys(
            new[]
            {
                "MSG_SORT_CARD:PLACE:0",
                "MSG_SORT_CARD:PLACE:2",
                "MSG_SORT_CARD:CANCEL"
            },
            afterPartial.Projection!.Candidates!);
        FlatPromptContinuationStepResultV1 cancelAfterPartial =
            cancelAfterPartialSession.TryApplySelection(Capture(
                cancelAfterPartialSession,
                "MSG_SORT_CARD:CANCEL"));
        AssertCancel(cancelAfterPartial);
    }

    private static void AssertThreeSourcePermutationAndRemainingDomain()
    {
        byte[] message = SortMessage(
            25,
            0,
            new SortEntry(1, 0, 0x01, 0),
            new SortEntry(2, 0, 0x01, 1),
            new SortEntry(3, 0, 0x01, 2));
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 initial =
            session.TryAcceptI5Prompt(message);
        AssertSuccess(initial, FlatPromptFamilyValueV1.MsgSortCard);
        AssertKeys(
            new[]
            {
                "MSG_SORT_CARD:PLACE:0",
                "MSG_SORT_CARD:PLACE:1",
                "MSG_SORT_CARD:PLACE:2",
                "MSG_SORT_CARD:CANCEL"
            },
            initial.Candidates!);
        FlatPromptSelectionHandleV1 first = Capture(
            session,
            "MSG_SORT_CARD:PLACE:2");
        FlatPromptContinuationStepResultV1 afterTwo =
            session.TryApplySelection(first);
        True(afterTwo.IsSuccess, afterTwo.Error.ToString());
        AssertKeys(
            new[]
            {
                "MSG_SORT_CARD:PLACE:0",
                "MSG_SORT_CARD:PLACE:1",
                "MSG_SORT_CARD:CANCEL"
            },
            afterTwo.Projection!.Candidates!);
        FlatPromptContinuationStepResultV1 stale =
            session.TryApplySelection(first);
        False(stale.IsSuccess, "stale SORT step was accepted");
        Equal(FlatPromptErrorCodeV1.StaleContinuationStep, stale.Error);

        FlatPromptSelectionHandleV1 second = Capture(
            session,
            "MSG_SORT_CARD:PLACE:0");
        FlatPromptContinuationStepResultV1 afterZero =
            session.TryApplySelection(second);
        True(afterZero.IsSuccess, afterZero.Error.ToString());
        AssertKeys(
            new[]
            {
                "MSG_SORT_CARD:PLACE:1",
                "MSG_SORT_CARD:CANCEL"
            },
            afterZero.Projection!.Candidates!);
        FlatPromptContinuationStepResultV1 terminal =
            session.TryApplySelection(Capture(
                session,
                "MSG_SORT_CARD:PLACE:1"));
        True(terminal.IsTerminal, terminal.Error.ToString());
        BytesEqual(
            new byte[] { 0x01, 0x02, 0x00 },
            terminal.TerminalResponseBody.ToArray());
        FlatPromptContinuationStepResultV1 reuse =
            session.TryApplySelection(second);
        False(reuse.IsSuccess, "terminal SORT handle was reused");
        Equal(FlatPromptErrorCodeV1.InvalidContinuationInstance, reuse.Error);
    }

    private static void AssertDuplicateOccurrencesAndAuthority()
    {
        byte[] duplicateMessage = SortMessage(
            25,
            0,
            new SortEntry(0x12345678, 0, 0x04, 0),
            new SortEntry(0x12345678, 0, 0x04, 0));
        FlatPromptProjectionResultV1 duplicate =
            new FlatPromptSessionV1().TryAcceptI5Prompt(duplicateMessage);
        AssertSuccess(duplicate, FlatPromptFamilyValueV1.MsgSortCard);
        Equal(2, duplicate.Candidates!.Count(candidate =>
            candidate is FlatPromptSortPublicCandidateBaseV1));
        Equal(0, ((FlatPromptSortPublicCandidateBaseV1)duplicate.Candidates![0])
            .SourceOrdinal);
        Equal(1, ((FlatPromptSortPublicCandidateBaseV1)duplicate.Candidates![1])
            .SourceOrdinal);

        Authority oneCardAuthority = CreateAuthority(
            new CardSpec(
                0x12345678,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        FlatPromptProjectionResultV1 duplicateLocator =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                SortMessage(
                    25,
                    0,
                    new SortEntry(0x12345678, 0, 0x04, 0),
                    new SortEntry(0x12345678, 0, 0x04, 0)),
                oneCardAuthority.Mirror,
                oneCardAuthority.Projection);
        AssertSuccess(duplicateLocator, FlatPromptFamilyValueV1.MsgSortCard);
        Equal(2, duplicateLocator.Candidates!.Count(candidate =>
            candidate is FlatPromptSortLocatorPromptCodePublicCandidateV1));
        Equal(
            "p0:MONSTER_ZONE:0",
            ((FlatPromptSortLocatorPromptCodePublicCandidateV1)
                duplicateLocator.Candidates![0]).PublicSemanticCardLocator.Value);
        Equal(
            "p0:MONSTER_ZONE:0",
            ((FlatPromptSortLocatorPromptCodePublicCandidateV1)
                duplicateLocator.Candidates![1]).PublicSemanticCardLocator.Value);

        Authority authority = CreateAuthority(
            new CardSpec(
                0xABCDEF01,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new CardSpec(
                0x10203040,
                new ModernLocInfoV1(0, 0x04, 1, 0x05)));
        FlatPromptProjectionResultV1 field =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                SortMessage(
                    25,
                    0,
                    new SortEntry(
                        0xABCDEF01,
                        0,
                        0x04,
                        0),
                    new SortEntry(
                        0x10203040,
                        0,
                        0x04,
                        1)),
                authority.Mirror,
                authority.Projection);
        AssertSuccess(field, FlatPromptFamilyValueV1.MsgSortCard);
        True(field.Candidates![0]
            is FlatPromptSortLocatorPromptCodePublicCandidateV1);
        Equal(
            "p0:MONSTER_ZONE:0",
            ((FlatPromptSortLocatorPromptCodePublicCandidateV1)
                field.Candidates[0]).PublicSemanticCardLocator.Value);
        Equal(
            "p0:MONSTER_ZONE:1",
            ((FlatPromptSortLocatorPromptCodePublicCandidateV1)
                field.Candidates[1]).PublicSemanticCardLocator.Value);

        FlatPromptProjectionResultV1 mainDeck =
            new FlatPromptSessionV1().TryAcceptI5Prompt(SortMessage(
                25,
                0,
                new SortEntry(0x55667788, 0, 0x01, 0)));
        AssertSuccess(mainDeck, FlatPromptFamilyValueV1.MsgSortCard);
        True(mainDeck.Candidates![0]
            is FlatPromptSortPromptCodePublicCandidateV1);
        Null(typeof(FlatPromptSortPromptCodePublicCandidateV1)
            .GetProperty("PublicSemanticCardLocator"));

        FlatPromptSessionV1 continuitySession = new();
        FlatPromptProjectionResultV1 firstMainDeck =
            continuitySession.TryAcceptI5Prompt(SortMessage(
                25,
                0,
                new SortEntry(0x55667788, 0, 0x01, 0)));
        AssertSuccess(firstMainDeck, FlatPromptFamilyValueV1.MsgSortCard);
        FlatPromptSelectionHandleV1 firstMainDeckHandle = Capture(
            continuitySession,
            "MSG_SORT_CARD:PLACE:0");
        FlatPromptProjectionResultV1 secondMainDeck =
            continuitySession.TryAcceptI5Prompt(SortMessage(
                25,
                0,
                new SortEntry(0x55667788, 0, 0x01, 1)));
        AssertSuccess(secondMainDeck, FlatPromptFamilyValueV1.MsgSortCard);
        FlatPromptContinuationStepResultV1 oldPromptUse =
            continuitySession.TryApplySelection(firstMainDeckHandle);
        False(oldPromptUse.IsSuccess,
            "prompt-local CardCode created cross-prompt continuity");
        Equal(
            FlatPromptErrorCodeV1.InvalidContinuationInstance,
            oldPromptUse.Error);
        True(secondMainDeck.Candidates![0]
            is FlatPromptSortPromptCodePublicCandidateV1);

        FlatPromptProjectionResultV1 anonymous =
            new FlatPromptSessionV1().TryAcceptI5Prompt(SortMessage(
                25,
                0,
                new SortEntry(0, 0, 0x01, 0)));
        AssertSuccess(anonymous, FlatPromptFamilyValueV1.MsgSortCard);
        True(anonymous.Candidates![0]
            is FlatPromptSortAnonymousPublicCandidateV1);
    }

    private static void AssertCountAndWireFailures()
    {
        byte[] valid = SortMessage(
            25,
            0,
            new SortEntry(1, 0, 0x01, 0),
            new SortEntry(2, 0, 0x01, 1));
        AssertFailure(
            new FlatPromptSessionV1(),
            new byte[] { 21 },
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            new byte[] { 25 },
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            new byte[] { 21, 0, 0 },
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            new byte[] { 25, 0, 0, 0, 0 },
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            SortMessage(25, 0),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            new FlatPromptSessionV1(),
            SortEntries(25, 0, 129),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            new FlatPromptSessionV1(),
            SortEntries(21, 0, 130),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            new FlatPromptSessionV1(),
            valid[..^1],
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            Append(valid, 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            SetByte(valid, 1, 2),
            FlatPromptErrorCodeV1.InvalidParticipant);
        AssertFailure(
            new FlatPromptSessionV1(),
            SetByte(valid, 10, 2),
            FlatPromptErrorCodeV1.InvalidParticipant);
        AssertFailure(
            new FlatPromptSessionV1(),
            SetU32(valid, 11, 0x100),
            FlatPromptErrorCodeV1.InvalidLocation);
        AssertFailure(
            new FlatPromptSessionV1(),
            SetU32(valid, 11, 0x03),
            FlatPromptErrorCodeV1.InvalidLocation);
        AssertFailure(
            new FlatPromptSessionV1(),
            SetU32(valid, 11, 0x80),
            FlatPromptErrorCodeV1.UnprovenPublicReference);
        AssertFailure(
            new FlatPromptSessionV1(),
            SetU32(SetU32(valid, 11, 0x04), 15, 7),
            FlatPromptErrorCodeV1.InvalidLocation);
        AssertFailure(
            new FlatPromptSessionV1(),
            SetU32(valid, 2, uint.MaxValue),
            FlatPromptErrorCodeV1.ArithmeticFailure);
        AssertFailureResult(
            new FlatPromptSessionV1().TryAcceptI5Prompt(new byte[] { 23 }),
            FlatPromptErrorCodeV1.UnsupportedPromptFamily);
        AssertFailureResult(
            new FlatPromptSessionV1().TryAcceptI5Prompt(new byte[] { 142 }),
            FlatPromptErrorCodeV1.UnsupportedPromptLayout);
    }

    private static void AssertAtomicityStalenessOwnershipAndBoundaries()
    {
        byte[] original = SortMessage(
            25,
            0,
            new SortEntry(1, 0, 0x01, 0),
            new SortEntry(2, 0, 0x01, 1));
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 accepted =
            session.TryAcceptI5Prompt(original);
        AssertSuccess(accepted, FlatPromptFamilyValueV1.MsgSortCard);
        FlatPromptSelectionHandleV1 oldHandle = Capture(
            session,
            "MSG_SORT_CARD:PLACE:0");
        byte[] source = original.ToArray();
        source[0] = 0xFF;
        source[6] = 0xFF;
        Equal(
            1u,
            ((FlatPromptSortPromptCodePublicCandidateV1)
                accepted.Candidates![0]).PromptLocalCardCode);
        Equal(
            1u,
            ((FlatPromptSortSourcePromptCodePublicDescriptorV1)
                ((FlatPromptSortSelectionPublicContextV1)accepted.Context!)
                    .Sources[0]).PromptLocalCardCode);
        AssertFailureResult(
            session.TryAcceptI5Prompt(Append(original, 0xAA)),
            FlatPromptErrorCodeV1.MalformedPrompt);
        False(session.TryResolveSelection(
            oldHandle,
            out _,
            out FlatPromptErrorCodeV1 staleAfterFailure),
            "failed SORT prompt preserved old binding");
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, staleAfterFailure);
        FlatPromptContinuationStepResultV1 staleInstance =
            session.TryApplySelection(oldHandle);
        False(staleInstance.IsSuccess,
            "old SORT handle survived a new prompt instance");
        Equal(
            FlatPromptErrorCodeV1.InvalidContinuationInstance,
            staleInstance.Error);
        FlatPromptProjectionResultV1 reaccepted =
            session.TryAcceptI5Prompt(original);
        AssertSuccess(reaccepted, FlatPromptFamilyValueV1.MsgSortCard);
        FlatPromptSelectionHandleV1 reacceptedHandle = Capture(
            session,
            "MSG_SORT_CARD:PLACE:0");
        Equal(1UL, reacceptedHandle.PromptInstanceOrdinal);

        FlatPromptSessionV1 crossFamilySession = new();
        AssertSuccess(
            crossFamilySession.TryAcceptI5Prompt(original),
            FlatPromptFamilyValueV1.MsgSortCard);
        FlatPromptSelectionHandleV1 cardHandle = Capture(
            crossFamilySession,
            "MSG_SORT_CARD:PLACE:0");
        AssertSuccess(
            crossFamilySession.TryAcceptI5Prompt(SortMessage(
                21,
                0,
                new SortEntry(1, 0, 0x01, 0),
                new SortEntry(2, 0, 0x01, 1))),
            FlatPromptFamilyValueV1.MsgSortChain);
        FlatPromptContinuationStepResultV1 wrongFamily =
            crossFamilySession.TryApplySelection(cardHandle);
        False(wrongFamily.IsSuccess, "cross-family SORT handle was accepted");
        Equal(
            FlatPromptErrorCodeV1.InvalidContinuationInstance,
            wrongFamily.Error);

        FlatPromptProjectionResultV1 n128 =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                SortEntries(25, 0, 128));
        AssertSuccess(n128, FlatPromptFamilyValueV1.MsgSortCard);
        FlatPromptSortSelectionPublicContextV1 n128Context =
            (FlatPromptSortSelectionPublicContextV1)n128.Context!;
        Equal(128, n128Context.SourceCount);
        Equal(129, n128.Candidates!.Count);
        Equal(
            "MSG_SORT_CARD:PLACE:127",
            n128.Candidates[127].I4LocalCandidateKey);
        Equal("MSG_SORT_CARD:CANCEL", n128.Candidates[128].I4LocalCandidateKey);

        FlatPromptSessionV1 n128TerminalSession = new();
        AssertSuccess(
            n128TerminalSession.TryAcceptI5Prompt(SortEntries(25, 0, 128)),
            FlatPromptFamilyValueV1.MsgSortCard);
        for (int sourceOrdinal = 1; sourceOrdinal < 128; sourceOrdinal++)
        {
            FlatPromptContinuationStepResultV1 partial =
                n128TerminalSession.TryApplySelection(Capture(
                    n128TerminalSession,
                    "MSG_SORT_CARD:PLACE:" + sourceOrdinal));
            True(partial.IsSuccess, partial.Error.ToString());
            False(partial.IsTerminal,
                "n=128 became terminal before every source was placed");
        }

        FlatPromptContinuationStepResultV1 n128Terminal =
            n128TerminalSession.TryApplySelection(Capture(
                n128TerminalSession,
                "MSG_SORT_CARD:PLACE:0"));
        True(n128Terminal.IsTerminal, n128Terminal.Error.ToString());
        Equal(128, n128Terminal.TerminalResponseBody.Count);
        for (int sourceOrdinal = 0; sourceOrdinal < 128; sourceOrdinal++)
        {
            Equal(
                (byte)(sourceOrdinal == 0 ? 127 : sourceOrdinal - 1),
                n128Terminal.TerminalResponseBody[sourceOrdinal]);
        }
    }

    private static void AssertPublicBoundary()
    {
        Type[] publicTypes =
        {
            typeof(FlatPromptSortSelectionPublicContextV1),
            typeof(FlatPromptSortSourcePublicDescriptorBaseV1),
            typeof(FlatPromptSortSourceAnonymousPublicDescriptorV1),
            typeof(FlatPromptSortSourcePromptCodePublicDescriptorV1),
            typeof(FlatPromptSortSourceLocatorPublicDescriptorV1),
            typeof(FlatPromptSortSourceLocatorPromptCodePublicDescriptorV1),
            typeof(FlatPromptSortPublicCandidateBaseV1),
            typeof(FlatPromptSortAnonymousPublicCandidateV1),
            typeof(FlatPromptSortPromptCodePublicCandidateV1),
            typeof(FlatPromptSortLocatorPublicCandidateV1),
            typeof(FlatPromptSortLocatorPromptCodePublicCandidateV1)
        };
        string[] forbidden =
        {
            "ResponseI32", "ResponseBody", "ModernLocInfo", "MirrorSnapshot",
            "MirrorEntityId", "ProtocolOffset", "SourceCardCode",
            "SourceLocation", "Raw", "Socket", "Network", "Timestamp", "Pid",
            "PrivateResponse", "ContinuationStep", "PromptInstanceOrdinal",
            "PlacedSourceOrdinals"
        };
        foreach (Type type in publicTypes)
        {
            True(type.IsPublic, "expected public sort type " + type.Name);
            True(type.IsAbstract || type.IsSealed,
                "expected closed sort type " + type.Name);
            foreach (PropertyInfo property in type.GetProperties())
            {
                False(
                    forbidden.Contains(property.Name, StringComparer.Ordinal),
                    "private sort property exposed by " + type.Name + "." +
                    property.Name);
            }
        }

        Null(typeof(FlatPromptSortSourcePromptCodePublicDescriptorV1)
            .GetProperty("SourceCardCode"));
        Null(typeof(FlatPromptSortPromptCodePublicCandidateV1)
            .GetProperty("PublicSemanticCardLocator"));
        False(typeof(FlatPromptSortContinuationStateV1).IsPublic,
            "sort continuation state must stay internal");
        False(typeof(CurrentFlatPromptBindingV1).IsPublic,
            "current binding must stay internal");
        False(typeof(FlatPromptProjectionV1).IsPublic,
            "sort parser must stay internal");
        False(typeof(FlatPromptSessionV1).GetMethods().Any(method =>
            method.IsPublic && method.Name.Contains(
                "Send",
                StringComparison.OrdinalIgnoreCase)),
            "sort session must not send network responses");

        FlatPromptSortPublicCandidateBaseV1[] sourceCandidates =
        {
            new FlatPromptSortPromptCodePublicCandidateV1(
                "MSG_SORT_CARD:PLACE:0",
                FlatPromptFamilyValueV1.MsgSortCard,
                0,
                1),
            new FlatPromptSortPromptCodePublicCandidateV1(
                "MSG_SORT_CARD:PLACE:1",
                FlatPromptFamilyValueV1.MsgSortCard,
                1,
                2)
        };
        bool invalidPermutationRejected = false;
        try
        {
            _ = new FlatPromptSortContinuationStateV1(
                FlatPromptFamilyValueV1.MsgSortCard,
                0,
                sourceCandidates,
                new[] { 0, 0 },
                2);
        }
        catch (ArgumentException)
        {
            invalidPermutationRejected = true;
        }

        True(invalidPermutationRejected,
            "duplicate source placement must be rejected");
    }

    private static void AssertCancel(FlatPromptContinuationStepResultV1 result)
    {
        True(result.IsSuccess, result.Error.ToString());
        True(result.IsTerminal, result.Error.ToString());
        Null(result.Projection);
        BytesEqual(
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
            result.TerminalResponseBody.ToArray());
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
        Equal(
            "ocgforge-ignis.combinatorial-prompt-continuation.v1",
            result.Context.ContractId);
    }

    private static void AssertFailure(
        FlatPromptSessionV1 session,
        byte[] message,
        FlatPromptErrorCodeV1 expectedError) =>
        AssertFailureResult(
            session.TryAcceptI5Prompt(message),
            expectedError);

    private static void AssertFailureResult(
        FlatPromptProjectionResultV1 result,
        FlatPromptErrorCodeV1 expectedError)
    {
        False(result.IsSuccess, "unexpected successful result: " + result.Error);
        Equal(expectedError, result.Error);
        Null(result.Context);
        Null(result.Candidates);
    }

    private static void AssertKeys(
        IReadOnlyList<string> expected,
        IReadOnlyList<FlatPublicCandidateDescriptorV1> candidates)
    {
        string[] actual = candidates
            .Select(candidate => candidate.I4LocalCandidateKey)
            .ToArray();
        True(expected.SequenceEqual(actual),
            $"expected keys [{string.Join(",", expected)}]; actual keys " +
            $"[{string.Join(",", actual)}]");
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

    private static byte[] SortMessage(
        byte messageId,
        byte actingPlayer,
        params SortEntry[] entries)
    {
        List<byte[]> parts = new()
        {
            new[] { messageId, actingPlayer },
            U32((uint)entries.Length)
        };
        parts.AddRange(entries.Select(entry => Join(
            U32(entry.CardCode),
            new[] { entry.Controller },
            U32(entry.Location),
            U32(entry.Sequence))));
        return Join(parts.ToArray());
    }

    private static byte[] SortEntries(
        byte messageId,
        byte actingPlayer,
        int count)
    {
        SortEntry[] entries = Enumerable.Range(0, count)
            .Select(index => new SortEntry(
                checked((uint)(index + 1)),
                0,
                0x01,
                checked((uint)index)))
            .ToArray();
        return SortMessage(messageId, actingPlayer, entries);
    }

    private static byte[] SetByte(byte[] source, int index, byte value)
    {
        byte[] copy = source.ToArray();
        copy[index] = value;
        return copy;
    }

    private static byte[] SetU32(byte[] source, int offset, uint value)
    {
        byte[] copy = source.ToArray();
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(
            copy.AsSpan(offset, sizeof(uint)),
            value);
        return copy;
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

    private readonly record struct SortEntry(
        uint CardCode,
        byte Controller,
        uint Location,
        uint Sequence);
}
