using System.Buffers.Binary;
using System.Reflection;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I5A2PlaceAndMaskPromptTests
{
    internal static void TestPlaceAndDisfield()
    {
        FlatPromptSessionV1 minimalSession = new();
        FlatPromptProjectionResultV1 result =
            minimalSession.TryAcceptI5Prompt(SelectPlaceMinimal);
        AssertSuccess(result, FlatPromptFamilyValueV1.MsgSelectPlace);
        FlatPromptPlaceSelectionPublicContextV1 context =
            result.Context as FlatPromptPlaceSelectionPublicContextV1 ??
            throw new InvalidOperationException("expected PLACE context");
        Equal((byte)0, context.ActingPlayer);
        Equal((byte)1, context.RequiredPlaceCount);
        AssertPlaces(
            new[]
            {
                new FlatPromptFieldPlaceV1(
                    0,
                    FlatPromptFieldZoneV1.MonsterZone,
                    0)
            },
            context.EligiblePlaces);
        AssertKeys(
            new[] { "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:0" },
            result.Candidates!);
        AssertPlaceCandidate(
            result.Candidates![0],
            0,
            FlatPromptFieldZoneV1.MonsterZone,
            0);
        False(result.Candidates.Any(candidate =>
            candidate is FlatPromptFinishPublicCandidateV1 or
                FlatPromptCancelPublicCandidateV1));

        FlatPromptSelectionHandleV1 minimalHandle = Capture(
            minimalSession,
            "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:0");
        False(minimalSession.TryResolveSelection(
            minimalHandle,
            out _,
            out FlatPromptErrorCodeV1 scalarError));
        Equal(FlatPromptErrorCodeV1.InvalidContinuationAction, scalarError);
        FlatPromptContinuationStepResultV1 minimalTerminal =
            minimalSession.TryApplySelection(minimalHandle);
        True(minimalTerminal.IsSuccess, minimalTerminal.Error.ToString());
        True(minimalTerminal.IsTerminal);
        Null(minimalTerminal.Projection);
        BytesEqual(
            new byte[] { 0x00, 0x04, 0x00 },
            minimalTerminal.TerminalResponseBody.ToArray());
        FlatPromptContinuationStepResultV1 reusedTerminal =
            minimalSession.TryApplySelection(minimalHandle);
        False(reusedTerminal.IsSuccess);
        Equal(
            FlatPromptErrorCodeV1.InvalidContinuationInstance,
            reusedTerminal.Error);

        FlatPromptSessionV1 multiSession = new();
        FlatPromptProjectionResultV1 multi =
            multiSession.TryAcceptI5Prompt(SelectPlaceMulti);
        AssertSuccess(multi, FlatPromptFamilyValueV1.MsgSelectPlace);
        FlatPromptPlaceSelectionPublicContextV1 multiContext =
            multi.Context as FlatPromptPlaceSelectionPublicContextV1 ??
            throw new InvalidOperationException("expected multi-place context");
        AssertPlaces(
            new[]
            {
                new FlatPromptFieldPlaceV1(
                    0,
                    FlatPromptFieldZoneV1.MonsterZone,
                    0),
                new FlatPromptFieldPlaceV1(
                    1,
                    FlatPromptFieldZoneV1.SpellTrapZone,
                    2)
            },
            multiContext.EligiblePlaces);
        AssertKeys(
            new[] { "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:0" },
            multi.Candidates!);
        FlatPromptSelectionHandleV1 firstMultiHandle = Capture(
            multiSession,
            "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:0");
        FlatPromptContinuationStepResultV1 afterFirst =
            multiSession.TryApplySelection(firstMultiHandle);
        True(afterFirst.IsSuccess, afterFirst.Error.ToString());
        False(afterFirst.IsTerminal);
        Equal(0, afterFirst.TerminalResponseBody.Count);
        AssertKeys(
            new[] { "MSG_SELECT_PLACE:PICK:1:SPELL_TRAP_ZONE:2" },
            afterFirst.Projection!.Candidates!);
        FlatPromptSelectionHandleV1 nextHandle = Capture(
            multiSession,
            "MSG_SELECT_PLACE:PICK:1:SPELL_TRAP_ZONE:2");
        Equal(0UL, nextHandle.PromptInstanceOrdinal);
        Equal(1, nextHandle.ContinuationStep);
        FlatPromptContinuationStepResultV1 staleStep =
            multiSession.TryApplySelection(firstMultiHandle);
        False(staleStep.IsSuccess);
        Equal(FlatPromptErrorCodeV1.StaleContinuationStep, staleStep.Error);
        FlatPromptContinuationStepResultV1 multiTerminal =
            multiSession.TryApplySelection(Capture(
                multiSession,
                "MSG_SELECT_PLACE:PICK:1:SPELL_TRAP_ZONE:2"));
        True(multiTerminal.IsSuccess, multiTerminal.Error.ToString());
        True(multiTerminal.IsTerminal);
        BytesEqual(
            new byte[] { 0x00, 0x04, 0x00, 0x01, 0x08, 0x02 },
            multiTerminal.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 disfieldSession = new();
        FlatPromptProjectionResultV1 disfield =
            disfieldSession.TryAcceptI5Prompt(SelectDisfieldMinimal);
        AssertSuccess(
            disfield,
            FlatPromptFamilyValueV1.MsgSelectDisfield);
        True(disfield.Context is
            FlatPromptDisfieldSelectionPublicContextV1);
        AssertKeys(
            new[] { "MSG_SELECT_DISFIELD:PICK:1:MONSTER_ZONE:0" },
            disfield.Candidates!);
        FlatPromptContinuationStepResultV1 disfieldTerminal =
            disfieldSession.TryApplySelection(Capture(
                disfieldSession,
                "MSG_SELECT_DISFIELD:PICK:1:MONSTER_ZONE:0"));
        True(disfieldTerminal.IsTerminal,
            disfieldTerminal.Error.ToString());
        BytesEqual(
            new byte[] { 0x01, 0x04, 0x00 },
            disfieldTerminal.TerminalResponseBody.ToArray());

        AssertPlaceRelativeMapping();
        AssertPlaceCompleteness();
        AssertPlaceFailuresOwnershipAndBoundary();
        AssertPublicBoundary();
    }

    internal static void TestRaceAndAttribute()
    {
        FlatPromptSessionV1 raceSession = new();
        FlatPromptProjectionResultV1 race =
            raceSession.TryAcceptI5Prompt(AnnounceRaceMultiBit);
        AssertSuccess(race, FlatPromptFamilyValueV1.MsgAnnounceRace);
        FlatPromptRaceSelectionPublicContextV1 raceContext =
            race.Context as FlatPromptRaceSelectionPublicContextV1 ??
            throw new InvalidOperationException("expected RACE context");
        Equal((byte)0, raceContext.ActingPlayer);
        Equal((byte)2, raceContext.RequiredBitCount);
        Equal(0x0000000000000005UL, raceContext.AvailableRaceMask);
        AssertKeys(
            new[] { "MSG_ANNOUNCE_RACE:PICK:0" },
            race.Candidates!);
        AssertMaskCandidate(race.Candidates![0], 0, 1UL);
        False(race.Candidates.Any(candidate =>
            candidate is FlatPromptFinishPublicCandidateV1 or
                FlatPromptCancelPublicCandidateV1));
        FlatPromptSelectionHandleV1 raceHandle = Capture(
            raceSession,
            "MSG_ANNOUNCE_RACE:PICK:0");
        False(raceSession.TryResolveSelection(
            raceHandle,
            out _,
            out FlatPromptErrorCodeV1 raceScalarError));
        Equal(FlatPromptErrorCodeV1.InvalidContinuationAction,
            raceScalarError);
        FlatPromptContinuationStepResultV1 raceAfterFirst =
            raceSession.TryApplySelection(raceHandle);
        True(raceAfterFirst.IsSuccess, raceAfterFirst.Error.ToString());
        False(raceAfterFirst.IsTerminal);
        Equal(0, raceAfterFirst.TerminalResponseBody.Count);
        AssertKeys(
            new[] { "MSG_ANNOUNCE_RACE:PICK:2" },
            raceAfterFirst.Projection!.Candidates!);
        FlatPromptContinuationStepResultV1 raceTerminal =
            raceSession.TryApplySelection(Capture(
                raceSession,
                "MSG_ANNOUNCE_RACE:PICK:2"));
        True(raceTerminal.IsSuccess, raceTerminal.Error.ToString());
        True(raceTerminal.IsTerminal);
        BytesEqual(
            new byte[] { 0x05, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00 },
            raceTerminal.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 attributeSession = new();
        FlatPromptProjectionResultV1 attribute =
            attributeSession.TryAcceptI5Prompt(AnnounceAttributeMultiBit);
        AssertSuccess(
            attribute,
            FlatPromptFamilyValueV1.MsgAnnounceAttrib);
        FlatPromptAttributeSelectionPublicContextV1 attributeContext =
            attribute.Context as FlatPromptAttributeSelectionPublicContextV1 ??
            throw new InvalidOperationException("expected attribute context");
        Equal((byte)1, attributeContext.ActingPlayer);
        Equal((byte)2, attributeContext.RequiredBitCount);
        Equal(0x00000005u, attributeContext.AvailableAttributeMask);
        AssertKeys(
            new[] { "MSG_ANNOUNCE_ATTRIB:PICK:0" },
            attribute.Candidates!);
        FlatPromptSelectionHandleV1 attributeHandle = Capture(
            attributeSession,
            "MSG_ANNOUNCE_ATTRIB:PICK:0");
        False(attributeSession.TryResolveSelection(
            attributeHandle,
            out _,
            out FlatPromptErrorCodeV1 attributeScalarError));
        Equal(
            FlatPromptErrorCodeV1.InvalidContinuationAction,
            attributeScalarError);
        False(attribute.Candidates!.Any(candidate =>
            candidate is FlatPromptFinishPublicCandidateV1 or
                FlatPromptCancelPublicCandidateV1));
        FlatPromptContinuationStepResultV1 attributeAfterFirst =
            attributeSession.TryApplySelection(attributeHandle);
        True(attributeAfterFirst.IsSuccess,
            attributeAfterFirst.Error.ToString());
        AssertKeys(
            new[] { "MSG_ANNOUNCE_ATTRIB:PICK:2" },
            attributeAfterFirst.Projection!.Candidates!);
        FlatPromptContinuationStepResultV1 attributeTerminal =
            attributeSession.TryApplySelection(Capture(
                attributeSession,
                "MSG_ANNOUNCE_ATTRIB:PICK:2"));
        True(attributeTerminal.IsSuccess,
            attributeTerminal.Error.ToString());
        True(attributeTerminal.IsTerminal);
        BytesEqual(
            new byte[] { 0x05, 0x00, 0x00, 0x00 },
            attributeTerminal.TerminalResponseBody.ToArray());

        AssertRaceEdgeCases();
        AssertAttributeEdgeCases();
        AssertMaskCompletenessAndLifecycle();
        AssertLaterFamilyBoundaries();
        AssertMaskOwnershipAndBoundary();
    }

    private static void AssertPlaceRelativeMapping()
    {
        uint fourGroups = ClearKnownPlaceBits(0, 8, 16, 24);
        FlatPromptProjectionResultV1 playerZero =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                PlaceMessage(0, 1, fourGroups));
        AssertSuccess(playerZero, FlatPromptFamilyValueV1.MsgSelectPlace);
        FlatPromptPlaceSelectionPublicContextV1 playerZeroContext =
            (FlatPromptPlaceSelectionPublicContextV1)playerZero.Context!;
        AssertPlaces(
            new[]
            {
                new FlatPromptFieldPlaceV1(0,
                    FlatPromptFieldZoneV1.MonsterZone, 0),
                new FlatPromptFieldPlaceV1(0,
                    FlatPromptFieldZoneV1.SpellTrapZone, 0),
                new FlatPromptFieldPlaceV1(1,
                    FlatPromptFieldZoneV1.MonsterZone, 0),
                new FlatPromptFieldPlaceV1(1,
                    FlatPromptFieldZoneV1.SpellTrapZone, 0)
            },
            playerZeroContext.EligiblePlaces);
        AssertKeys(
            new[]
            {
                "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:0",
                "MSG_SELECT_PLACE:PICK:0:SPELL_TRAP_ZONE:0",
                "MSG_SELECT_PLACE:PICK:1:MONSTER_ZONE:0",
                "MSG_SELECT_PLACE:PICK:1:SPELL_TRAP_ZONE:0"
            },
            playerZero.Candidates!);

        FlatPromptProjectionResultV1 playerOne =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                PlaceMessage(1, 1, fourGroups));
        AssertSuccess(playerOne, FlatPromptFamilyValueV1.MsgSelectPlace);
        AssertPlaces(
            new[]
            {
                new FlatPromptFieldPlaceV1(1,
                    FlatPromptFieldZoneV1.MonsterZone, 0),
                new FlatPromptFieldPlaceV1(1,
                    FlatPromptFieldZoneV1.SpellTrapZone, 0),
                new FlatPromptFieldPlaceV1(0,
                    FlatPromptFieldZoneV1.MonsterZone, 0),
                new FlatPromptFieldPlaceV1(0,
                    FlatPromptFieldZoneV1.SpellTrapZone, 0)
            },
            ((FlatPromptPlaceSelectionPublicContextV1)playerOne.Context!)
                .EligiblePlaces);

        FlatPromptSessionV1 absolutePlayerZeroSession = new();
        AssertSuccess(
            absolutePlayerZeroSession.TryAcceptI5Prompt(
                PlaceMessage(0, 1, ClearKnownPlaceBits(16))),
            FlatPromptFamilyValueV1.MsgSelectPlace);
        FlatPromptContinuationStepResultV1 responseZero =
            absolutePlayerZeroSession.TryApplySelection(Capture(
                absolutePlayerZeroSession,
                "MSG_SELECT_PLACE:PICK:1:MONSTER_ZONE:0"));
        True(responseZero.IsSuccess, responseZero.Error.ToString());
        BytesEqual(new byte[] { 0x01, 0x04, 0x00 },
            responseZero.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 absolutePlayerOneSession = new();
        AssertSuccess(
            absolutePlayerOneSession.TryAcceptI5Prompt(
                PlaceMessage(1, 1, ClearKnownPlaceBits(16))),
            FlatPromptFamilyValueV1.MsgSelectPlace);
        FlatPromptContinuationStepResultV1 responseOne =
            absolutePlayerOneSession.TryApplySelection(Capture(
                absolutePlayerOneSession,
                "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:0"));
        True(responseOne.IsSuccess, responseOne.Error.ToString());
        BytesEqual(new byte[] { 0x00, 0x04, 0x00 },
            responseOne.TerminalResponseBody.ToArray());
    }

    private static void AssertPlaceCompleteness()
    {
        byte[] message = PlaceMessage(
            0,
            2,
            ClearKnownPlaceBits(0, 1, 2));
        FlatPromptProjectionResultV1 result =
            new FlatPromptSessionV1().TryAcceptI5Prompt(message);
        AssertSuccess(result, FlatPromptFamilyValueV1.MsgSelectPlace);
        AssertKeys(
            new[]
            {
                "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:0",
                "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:1"
            },
            result.Candidates!);

        FlatPromptSessionV1 zeroThenOne = new();
        AssertSuccess(
            zeroThenOne.TryAcceptI5Prompt(message),
            FlatPromptFamilyValueV1.MsgSelectPlace);
        FlatPromptContinuationStepResultV1 afterZero =
            zeroThenOne.TryApplySelection(Capture(
                zeroThenOne,
                "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:0"));
        AssertKeys(
            new[]
            {
                "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:1",
                "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:2"
            },
            afterZero.Projection!.Candidates!);
        FlatPromptContinuationStepResultV1 zeroOneTerminal =
            zeroThenOne.TryApplySelection(Capture(
                zeroThenOne,
                "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:1"));
        True(zeroOneTerminal.IsTerminal,
            zeroOneTerminal.Error.ToString());
        BytesEqual(
            new byte[] { 0x00, 0x04, 0x00, 0x00, 0x04, 0x01 },
            zeroOneTerminal.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 zeroThenTwo = new();
        AssertSuccess(
            zeroThenTwo.TryAcceptI5Prompt(message),
            FlatPromptFamilyValueV1.MsgSelectPlace);
        zeroThenTwo.TryApplySelection(Capture(
            zeroThenTwo,
            "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:0"));
        FlatPromptContinuationStepResultV1 zeroTwoTerminal =
            zeroThenTwo.TryApplySelection(Capture(
                zeroThenTwo,
                "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:2"));
        True(zeroTwoTerminal.IsTerminal,
            zeroTwoTerminal.Error.ToString());
        BytesEqual(
            new byte[] { 0x00, 0x04, 0x00, 0x00, 0x04, 0x02 },
            zeroTwoTerminal.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 oneThenTwo = new();
        AssertSuccess(
            oneThenTwo.TryAcceptI5Prompt(message),
            FlatPromptFamilyValueV1.MsgSelectPlace);
        FlatPromptContinuationStepResultV1 afterOne =
            oneThenTwo.TryApplySelection(Capture(
                oneThenTwo,
                "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:1"));
        AssertKeys(
            new[] { "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:2" },
            afterOne.Projection!.Candidates!);
        FlatPromptContinuationStepResultV1 oneTwoTerminal =
            oneThenTwo.TryApplySelection(Capture(
                oneThenTwo,
                "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:2"));
        True(oneTwoTerminal.IsTerminal,
            oneTwoTerminal.Error.ToString());
        BytesEqual(
            new byte[] { 0x00, 0x04, 0x01, 0x00, 0x04, 0x02 },
            oneTwoTerminal.TerminalResponseBody.ToArray());
    }

    private static void AssertPlaceFailuresOwnershipAndBoundary()
    {
        AssertFailure(
            PlaceMessage(0, 0, ClearKnownPlaceBits(0)),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            PlaceMessage(0, 2, ClearKnownPlaceBits(0)),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            PlaceMessage(0, 1, uint.MaxValue),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            PlaceMessage(2, 1, ClearKnownPlaceBits(0)),
            FlatPromptErrorCodeV1.InvalidParticipant);
        AssertFailure(
            SelectPlaceMinimal.AsSpan(0, 6).ToArray(),
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            Append(SelectPlaceMinimal, 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            PlaceMessage(
                0,
                1,
                uint.MaxValue & ~(1u << 7) & ~(1u << 23)),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);

        FlatPromptSessionV1 ownershipSession = new();
        byte[] source = SelectPlaceMulti.ToArray();
        FlatPromptProjectionResultV1 owned =
            ownershipSession.TryAcceptI5Prompt(source);
        AssertSuccess(owned, FlatPromptFamilyValueV1.MsgSelectPlace);
        FlatPromptPlaceSelectionPublicContextV1 ownedContext =
            (FlatPromptPlaceSelectionPublicContextV1)owned.Context!;
        source[0] = 0xFF;
        source[3] = 0x00;
        Equal(2, ownedContext.EligiblePlaces.Count);
        Equal(
            "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:0",
            owned.Candidates![0].I4LocalCandidateKey);

        FlatPromptSessionV1 failedSession = new();
        AssertSuccess(
            failedSession.TryAcceptI5Prompt(SelectPlaceMinimal),
            FlatPromptFamilyValueV1.MsgSelectPlace);
        FlatPromptSelectionHandleV1 oldHandle = Capture(
            failedSession,
            "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:0");
        AssertFailureResult(
            failedSession.TryAcceptI5Prompt(Append(
                SelectPlaceMinimal,
                0xAA)),
            FlatPromptErrorCodeV1.MalformedPrompt);
        FlatPromptContinuationStepResultV1 oldAfterFailure =
            failedSession.TryApplySelection(oldHandle);
        False(oldAfterFailure.IsSuccess);
        Equal(
            FlatPromptErrorCodeV1.InvalidContinuationInstance,
            oldAfterFailure.Error);
        AssertSuccess(
            failedSession.TryAcceptI5Prompt(SelectPlaceMinimal),
            FlatPromptFamilyValueV1.MsgSelectPlace);
        FlatPromptContinuationStepResultV1 replacedOldHandle =
            failedSession.TryApplySelection(oldHandle);
        False(replacedOldHandle.IsSuccess);
        Equal(
            FlatPromptErrorCodeV1.InvalidContinuationInstance,
            replacedOldHandle.Error);
        Equal(
            1UL,
            Capture(
                failedSession,
                "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:0")
                .PromptInstanceOrdinal);
    }

    private static void AssertRaceEdgeCases()
    {
        FlatPromptSessionV1 bit31And62Session = new();
        FlatPromptProjectionResultV1 bit31And62 =
            bit31And62Session.TryAcceptI5Prompt(
                RaceMessage(0, 2, (1UL << 31) | (1UL << 62)));
        AssertSuccess(bit31And62, FlatPromptFamilyValueV1.MsgAnnounceRace);
        AssertKeys(
            new[] { "MSG_ANNOUNCE_RACE:PICK:31" },
            bit31And62.Candidates!);
        FlatPromptMaskBitPublicCandidateV1 bit31Candidate =
            bit31And62.Candidates![0] as FlatPromptMaskBitPublicCandidateV1 ??
            throw new InvalidOperationException("expected bit 31 candidate");
        Equal(31, bit31Candidate.BitIndex);
        Equal(1UL << 31, bit31Candidate.BitValue);
        FlatPromptContinuationStepResultV1 after31 =
            bit31And62Session.TryApplySelection(Capture(
                bit31And62Session,
                "MSG_ANNOUNCE_RACE:PICK:31"));
        AssertKeys(
            new[] { "MSG_ANNOUNCE_RACE:PICK:62" },
            after31.Projection!.Candidates!);
        FlatPromptMaskBitPublicCandidateV1 bit62Candidate =
            after31.Projection!.Candidates![0] as
                FlatPromptMaskBitPublicCandidateV1 ??
            throw new InvalidOperationException("expected bit 62 candidate");
        Equal(62, bit62Candidate.BitIndex);
        Equal(1UL << 62, bit62Candidate.BitValue);
        FlatPromptContinuationStepResultV1 bit62Terminal =
            bit31And62Session.TryApplySelection(Capture(
                bit31And62Session,
                "MSG_ANNOUNCE_RACE:PICK:62"));
        True(bit62Terminal.IsSuccess, bit62Terminal.Error.ToString());
        BytesEqual(
            new byte[] { 0x00, 0x00, 0x00, 0x80,
                0x00, 0x00, 0x00, 0x40 },
            bit62Terminal.TerminalResponseBody.ToArray());

        FlatPromptProjectionResultV1 kOne =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                RaceMessage(0, 1, 1UL | (1UL << 31) | (1UL << 62)));
        AssertSuccess(kOne, FlatPromptFamilyValueV1.MsgAnnounceRace);
        AssertKeys(
            new[]
            {
                "MSG_ANNOUNCE_RACE:PICK:0",
                "MSG_ANNOUNCE_RACE:PICK:31",
                "MSG_ANNOUNCE_RACE:PICK:62"
            },
            kOne.Candidates!);

        AssertFailure(
            RaceMessage(0, 1, 1UL << 32),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            RaceMessage(0, 1, 1UL << 33),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            RaceMessage(0, 1, 0),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            RaceMessage(0, 0, 1),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            RaceMessage(0, 2, 1),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            RaceMessage(2, 1, 1),
            FlatPromptErrorCodeV1.InvalidParticipant);
        AssertFailure(
            RaceMessage(0, 1, 1UL).AsSpan(0, 10).ToArray(),
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            Append(RaceMessage(0, 1, 1UL), 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);
    }

    private static void AssertAttributeEdgeCases()
    {
        AssertFailure(
            AttributeMessage(0, 1, 1u << 7),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            AttributeMessage(0, 1, 0),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            AttributeMessage(0, 0, 1),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            AttributeMessage(0, 2, 1),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            AttributeMessage(2, 1, 1),
            FlatPromptErrorCodeV1.InvalidParticipant);
        AssertFailure(
            AttributeMessage(0, 1, 1u).AsSpan(0, 6).ToArray(),
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            Append(AttributeMessage(0, 1, 1u), 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);

        FlatPromptProjectionResultV1 kOne =
            new FlatPromptSessionV1().TryAcceptI5Prompt(
                AttributeMessage(0, 1, 0x07));
        AssertSuccess(
            kOne,
            FlatPromptFamilyValueV1.MsgAnnounceAttrib);
        AssertKeys(
            new[]
            {
                "MSG_ANNOUNCE_ATTRIB:PICK:0",
                "MSG_ANNOUNCE_ATTRIB:PICK:1",
                "MSG_ANNOUNCE_ATTRIB:PICK:2"
            },
            kOne.Candidates!);
        AssertMaskCandidate(kOne.Candidates![0], 0, 1UL);
    }

    private static void AssertMaskCompletenessAndLifecycle()
    {
        FlatPromptSessionV1 race = new();
        byte[] message = RaceMessage(0, 2, 0x07);
        AssertSuccess(race.TryAcceptI5Prompt(message),
            FlatPromptFamilyValueV1.MsgAnnounceRace);
        FlatPromptProjectionResultV1 secondInitial =
            race.TryAcceptI5Prompt(message);
        AssertSuccess(secondInitial, FlatPromptFamilyValueV1.MsgAnnounceRace);
        AssertKeys(
            new[]
            {
                "MSG_ANNOUNCE_RACE:PICK:0",
                "MSG_ANNOUNCE_RACE:PICK:1"
            },
            secondInitial.Candidates!);

        FlatPromptSessionV1 zeroThenTwo = new();
        AssertSuccess(zeroThenTwo.TryAcceptI5Prompt(message),
            FlatPromptFamilyValueV1.MsgAnnounceRace);
        FlatPromptContinuationStepResultV1 afterZero =
            zeroThenTwo.TryApplySelection(Capture(
                zeroThenTwo,
                "MSG_ANNOUNCE_RACE:PICK:0"));
        AssertKeys(
            new[]
            {
                "MSG_ANNOUNCE_RACE:PICK:1",
                "MSG_ANNOUNCE_RACE:PICK:2"
            },
            afterZero.Projection!.Candidates!);
        FlatPromptContinuationStepResultV1 zeroTwo =
            zeroThenTwo.TryApplySelection(Capture(
                zeroThenTwo,
                "MSG_ANNOUNCE_RACE:PICK:2"));
        True(zeroTwo.IsTerminal, zeroTwo.Error.ToString());
        BytesEqual(new byte[] { 0x05, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00 },
            zeroTwo.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 oneThenTwo = new();
        AssertSuccess(oneThenTwo.TryAcceptI5Prompt(message),
            FlatPromptFamilyValueV1.MsgAnnounceRace);
        FlatPromptContinuationStepResultV1 afterOne =
            oneThenTwo.TryApplySelection(Capture(
                oneThenTwo,
                "MSG_ANNOUNCE_RACE:PICK:1"));
        AssertKeys(
            new[] { "MSG_ANNOUNCE_RACE:PICK:2" },
            afterOne.Projection!.Candidates!);
        FlatPromptContinuationStepResultV1 oneTwo =
            oneThenTwo.TryApplySelection(Capture(
                oneThenTwo,
                "MSG_ANNOUNCE_RACE:PICK:2"));
        True(oneTwo.IsTerminal, oneTwo.Error.ToString());
        BytesEqual(new byte[] { 0x06, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00 },
            oneTwo.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 lifecycle = new();
        AssertSuccess(lifecycle.TryAcceptI5Prompt(message),
            FlatPromptFamilyValueV1.MsgAnnounceRace);
        FlatPromptSelectionHandleV1 old = Capture(
            lifecycle,
            "MSG_ANNOUNCE_RACE:PICK:0");
        FlatPromptContinuationStepResultV1 next =
            lifecycle.TryApplySelection(old);
        True(next.IsSuccess, next.Error.ToString());
        FlatPromptContinuationStepResultV1 stale =
            lifecycle.TryApplySelection(old);
        False(stale.IsSuccess);
        Equal(FlatPromptErrorCodeV1.StaleContinuationStep, stale.Error);
        FlatPromptContinuationStepResultV1 stillUsable =
            lifecycle.TryApplySelection(Capture(
                lifecycle,
                "MSG_ANNOUNCE_RACE:PICK:1"));
        True(stillUsable.IsSuccess, stillUsable.Error.ToString());

        AssertSuccess(
            lifecycle.TryAcceptI5Prompt(AttributeMessage(0, 1, 1)),
            FlatPromptFamilyValueV1.MsgAnnounceAttrib);
        FlatPromptContinuationStepResultV1 crossFamily =
            lifecycle.TryApplySelection(old);
        False(crossFamily.IsSuccess);
        Equal(
            FlatPromptErrorCodeV1.InvalidContinuationInstance,
            crossFamily.Error);
    }

    private static void AssertLaterFamilyBoundaries()
    {
        foreach (byte id in new byte[] { 22, 21, 25 })
        {
            AssertFailure(
                new[] { id },
                FlatPromptErrorCodeV1.UnsupportedPromptLayout);
        }

        AssertFailure(
            new byte[] { 23 },
            FlatPromptErrorCodeV1.UnsupportedPromptFamily);
        AssertFailure(
            new byte[] { 142 },
            FlatPromptErrorCodeV1.UnsupportedPromptLayout);
    }

    private static void AssertMaskOwnershipAndBoundary()
    {
        FlatPromptSessionV1 session = new();
        byte[] source = AnnounceRaceMultiBit.ToArray();
        FlatPromptProjectionResultV1 result =
            session.TryAcceptI5Prompt(source);
        AssertSuccess(result, FlatPromptFamilyValueV1.MsgAnnounceRace);
        FlatPromptRaceSelectionPublicContextV1 context =
            (FlatPromptRaceSelectionPublicContextV1)result.Context!;
        source[0] = 0xFF;
        source[3] = 0xFF;
        Equal(0x0000000000000005UL, context.AvailableRaceMask);
        Equal(
            "MSG_ANNOUNCE_RACE:PICK:0",
            result.Candidates![0].I4LocalCandidateKey);

        FlatPromptSelectionHandleV1 handle = Capture(
            session,
            "MSG_ANNOUNCE_RACE:PICK:0");
        FlatPromptContinuationStepResultV1 terminal =
            session.TryApplySelection(handle);
        True(terminal.IsSuccess, terminal.Error.ToString());
        False(session.TryResolveSelection(
            handle,
            out _,
            out FlatPromptErrorCodeV1 error));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, error);
    }

    private static void AssertPublicBoundary()
    {
        Type[] publicTypes =
        {
            typeof(FlatPromptFieldPlaceV1),
            typeof(FlatPromptPlaceSelectionPublicContextBaseV1),
            typeof(FlatPromptPlaceSelectionPublicContextV1),
            typeof(FlatPromptDisfieldSelectionPublicContextV1),
            typeof(FlatPromptMaskSelectionPublicContextBaseV1),
            typeof(FlatPromptRaceSelectionPublicContextV1),
            typeof(FlatPromptAttributeSelectionPublicContextV1),
            typeof(FlatPromptFieldPlacePublicCandidateV1),
            typeof(FlatPromptMaskBitPublicCandidateV1)
        };
        string[] forbidden =
        {
            "ResponseI32", "ResponseBody", "ModernLocInfo", "MirrorSnapshot",
            "MirrorEntityId", "ProtocolOffset", "SourceBytes", "Socket",
            "Network", "Timestamp", "Pid", "ContinuationStep",
            "PromptInstanceOrdinal", "PrivateResponse", "CanonicalIndex",
            "FieldFlag"
        };
        foreach (Type type in publicTypes)
        {
            True(type.IsPublic, "expected public I5A2 type " + type.Name);
            True(type.IsAbstract || type.IsSealed,
                "expected closed I5A2 type " + type.Name);
            foreach (PropertyInfo property in type.GetProperties())
            {
                False(
                    forbidden.Contains(property.Name, StringComparer.Ordinal),
                    "private property exposed by " + type.Name + "." +
                    property.Name);
            }
        }

        False(typeof(FlatPromptContinuationStateV1).IsPublic);
        False(typeof(FlatPromptPlaceContinuationStateV1).IsPublic);
        False(typeof(FlatPromptMaskContinuationStateV1).IsPublic);
        False(typeof(FlatPromptFieldPlacePublicCandidateV1)
            .GetProperty("CanonicalIndex") is not null);
        FlatPromptProjectionResultV1 placeResult =
            new FlatPromptSessionV1().TryAcceptI5Prompt(SelectPlaceMinimal);
        AssertSuccess(placeResult, FlatPromptFamilyValueV1.MsgSelectPlace);
        FlatPublicCandidateDescriptorV1 placeCandidate =
            placeResult.Candidates![0];
        False(CurrentFlatPromptBindingV1.TryCreate(
            0,
            FlatPromptFamilyValueV1.MsgAnnounceRace,
            new[] { placeCandidate },
            new[] { placeCandidate.I4LocalCandidateKey },
            new[] { 0 },
            out CurrentFlatPromptBindingV1? wrongFamilyBinding,
            out FlatPromptErrorCodeV1 wrongFamilyError));
        Null(wrongFamilyBinding);
        Equal(
            FlatPromptErrorCodeV1.InvalidResponseBinding,
            wrongFamilyError);
        Equal(
            "ocgforge-ignis.combinatorial-prompt-continuation.v1",
            placeResult.Context!.ContractId);
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
        byte[] message,
        FlatPromptErrorCodeV1 expectedError) =>
        AssertFailureResult(
            new FlatPromptSessionV1().TryAcceptI5Prompt(message),
            expectedError);

    private static void AssertFailureResult(
        FlatPromptProjectionResultV1 result,
        FlatPromptErrorCodeV1 expectedError)
    {
        False(
            result.IsSuccess,
            $"expected failure {expectedError}, got success");
        Equal(expectedError, result.Error);
        Null(result.Context);
        Null(result.Candidates);
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

    private static void AssertPlaceCandidate(
        FlatPublicCandidateDescriptorV1 candidate,
        byte absolutePlayer,
        FlatPromptFieldZoneV1 zone,
        byte sequence)
    {
        FlatPromptFieldPlacePublicCandidateV1 place =
            candidate as FlatPromptFieldPlacePublicCandidateV1 ??
            throw new InvalidOperationException("expected place candidate");
        Equal(absolutePlayer, place.AbsolutePlayer);
        Equal(zone, place.Zone);
        Equal(sequence, place.Sequence);
        Equal(FlatPromptChoiceKindV1.Pick, place.ChoiceKind);
    }

    private static void AssertMaskCandidate(
        FlatPublicCandidateDescriptorV1 candidate,
        int bitIndex,
        ulong bitValue)
    {
        FlatPromptMaskBitPublicCandidateV1 bit =
            candidate as FlatPromptMaskBitPublicCandidateV1 ??
            throw new InvalidOperationException("expected mask candidate");
        Equal(bitIndex, bit.BitIndex);
        Equal(bitValue, bit.BitValue);
        Equal(FlatPromptChoiceKindV1.Pick, bit.ChoiceKind);
    }

    private static void AssertPlaces(
        IReadOnlyList<FlatPromptFieldPlaceV1> expected,
        IReadOnlyList<FlatPromptFieldPlaceV1> actual)
    {
        Equal(expected.Count, actual.Count);
        for (int index = 0; index < expected.Count; index++)
        {
            Equal(expected[index], actual[index]);
        }
    }

    private static void AssertKeys(
        IReadOnlyList<string> expected,
        IReadOnlyList<FlatPublicCandidateDescriptorV1> actual)
    {
        True(expected.SequenceEqual(actual.Select(
                candidate => candidate.I4LocalCandidateKey)),
            $"expected keys [{string.Join(",", expected)}]; actual keys " +
            $"[{string.Join(",", actual.Select(
                candidate => candidate.I4LocalCandidateKey))}]");
    }

    private static byte[] PlaceMessage(
        byte actingPlayer,
        byte requiredPlaceCount,
        uint fieldFlag) =>
        Join(
            new[] { (byte)18, actingPlayer, requiredPlaceCount },
            U32(fieldFlag));

    private static byte[] RaceMessage(
        byte actingPlayer,
        byte requiredBitCount,
        ulong availableMask) =>
        Join(
            new[] { (byte)140, actingPlayer, requiredBitCount },
            U64(availableMask));

    private static byte[] AttributeMessage(
        byte actingPlayer,
        byte requiredBitCount,
        uint availableMask) =>
        Join(
            new[] { (byte)141, actingPlayer, requiredBitCount },
            U32(availableMask));

    private static uint ClearKnownPlaceBits(params int[] bits)
    {
        uint fieldFlag = uint.MaxValue;
        foreach (int bit in bits)
        {
            fieldFlag &= ~(1u << bit);
        }

        return fieldFlag;
    }

    private static byte[] U32(uint value)
    {
        byte[] bytes = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        return bytes;
    }

    private static byte[] U64(ulong value)
    {
        byte[] bytes = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        return bytes;
    }

    private static byte[] Join(params byte[][] parts) =>
        parts.SelectMany(part => part).ToArray();

    private static byte[] Append(byte[] source, byte value) =>
        source.Concat(new[] { value }).ToArray();

    private static readonly byte[] SelectPlaceMinimal =
    {
        0x12, 0x00, 0x01, 0xFE, 0xFF, 0xFF, 0xFF
    };

    private static readonly byte[] SelectPlaceMulti =
    {
        0x12, 0x00, 0x02, 0xFE, 0xFF, 0xFF, 0xFB
    };

    private static readonly byte[] SelectDisfieldMinimal =
    {
        0x18, 0x01, 0x01, 0xFE, 0xFF, 0xFF, 0xFF
    };

    private static readonly byte[] AnnounceRaceMultiBit =
    {
        0x8C, 0x00, 0x02, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
    };

    private static readonly byte[] AnnounceAttributeMultiBit =
    {
        0x8D, 0x01, 0x02, 0x05, 0x00, 0x00, 0x00
    };
}
