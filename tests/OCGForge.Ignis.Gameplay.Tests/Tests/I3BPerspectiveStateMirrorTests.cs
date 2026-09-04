using System.Buffers.Binary;
using System.Reflection;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.ModernQueryFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TransportFixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I3BPerspectiveStateMirrorTests
{
    internal static void TestMirrorInitialization()
    {
        GameplayMessageDecoderV1 decoder = new();
        GameplayMessageDecodeResult start = decoder.Decode(
            new StocGameMessagePayload(CreateStartBytes(0x01)));
        True(start.IsSuccess);

        MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
            start.Message!,
            start.Perspective!);
        True(created.IsSuccess, created.Error.ToString());
        PerspectiveStateMirrorV1 mirror = created.Mirror!;
        MirrorSnapshotV1 snapshot = mirror.Snapshot;

        Equal(2, snapshot.Participants.Count);
        Equal(MirrorParticipantRoleV1.Self, snapshot.Participants[0].Role);
        Equal(MirrorParticipantRoleV1.Opponent, snapshot.Participants[1].Role);
        Equal(7000u, snapshot.Participants[0].LifePoints.Value);
        Equal(8000u, snapshot.Participants[1].LifePoints.Value);
        Equal(
            41u,
            snapshot.GetParticipant(MirrorParticipantRoleV1.Self)
                .GetZone(MirrorZoneV1.MainDeck).Count.Value);
        Equal(
            16u,
            snapshot.GetParticipant(MirrorParticipantRoleV1.Self)
                .GetZone(MirrorZoneV1.ExtraDeck).Count.Value);
        Equal(0ul, snapshot.TurnCount);
        False(snapshot.TurnPlayer.IsKnown);
        False(snapshot.Phase.IsKnown);
        False(snapshot.Terminal.IsTerminal);
        Equal(
            MirrorProvenanceV1.PublicProtocolFact,
            snapshot.Participants[0].LifePoints.Provenance);

        GameplayMessageV1 turn = DecodeMessage(decoder, new byte[] { 40, 1 });
        MirrorApplyResult turnResult = mirror.Apply(turn);
        True(turnResult.IsSuccess, turnResult.Error.ToString());
        Equal(1ul, mirror.Snapshot.TurnCount);
        Equal(MirrorParticipantRoleV1.Self, mirror.Snapshot.TurnPlayer.Value);

        GameplayMessageV1 phase = DecodeMessage(decoder, new byte[] { 41, 0x04, 0x00 });
        MirrorApplyResult phaseResult = mirror.Apply(phase);
        True(phaseResult.IsSuccess, phaseResult.Error.ToString());
        Equal((ushort)4, mirror.Snapshot.Phase.Value);
        AssertDoesNotContainForbidden(
            mirror.Snapshot.ToString(),
            new[] { "socket", "endpoint", "password", "pid", "timestamp", "thread" });
    }

    internal static void TestMirrorMovementAndRelations()
    {
        GameplayMessageDecoderV1 decoder = new();
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 activeDecoder) =
            CreateMirror(0x00);

        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 hand0 = new(0, 0x02, 0, 0x08);
        GameplayMessageV1 createFirst = DecodeMessage(
            activeDecoder,
            MoveMessage(0x11223344, empty, hand0, 0));
        MirrorApplyResult createResult = mirror.Apply(createFirst);
        True(createResult.IsSuccess, createResult.Error.ToString());
        Equal(1u, mirror.Snapshot.GetZone(MirrorParticipantRoleV1.Self, MirrorZoneV1.Hand).Count.Value);
        Equal(2u, mirror.Snapshot.GetZone(MirrorParticipantRoleV1.Self, MirrorZoneV1.MainDeck).Count.Value);
        string beforeInvalidPileSequence = mirror.Snapshot.ToDeterministicString();
        MirrorApplyResult invalidPileSequence = mirror.Apply(DecodeMessage(
            activeDecoder,
            MoveMessage(
                0x99999999,
                empty,
                new ModernLocInfoV1(0, 0x02, 9, 0x08),
                0)));
        False(invalidPileSequence.IsSuccess);
        Equal(GameplayErrorCode.StateCapacityExceeded, invalidPileSequence.Error);
        Equal(beforeInvalidPileSequence, mirror.Snapshot.ToDeterministicString());

        ModernLocInfoV1 monster0 = new(0, 0x04, 0, 0x01);
        MirrorApplyResult moveResult = mirror.Apply(
            DecodeMessage(activeDecoder, MoveMessage(0, hand0, monster0, 0)));
        True(moveResult.IsSuccess, moveResult.Error.ToString());

        ModernLocInfoV1 handA = new(0, 0x02, 0, 0x08);
        ModernLocInfoV1 handB = new(0, 0x02, 1, 0x08);
        True(mirror.Apply(DecodeMessage(
            activeDecoder,
            MoveMessage(0xaaaabbbb, empty, handA, 0))).IsSuccess);
        True(mirror.Apply(DecodeMessage(
            activeDecoder,
            MoveMessage(0xccccdddd, empty, handB, 0))).IsSuccess);
        True(mirror.Apply(DecodeMessage(
            activeDecoder,
            MoveMessage(
                0,
                handA,
                new ModernLocInfoV1(0, 0x10, 0, 0x04),
                0))).IsSuccess);
        MirrorCardSnapshotV1 shiftedHand = mirror.Snapshot.Cards.Single(
            card => card.Zone == MirrorZoneV1.Hand);
        Equal(0xccccddddu, shiftedHand.CardCode.Value);
        Equal(0u, shiftedHand.Sequence);
        ModernLocInfoV1 handC = new(0, 0x02, 1, 0x08);
        True(mirror.Apply(DecodeMessage(
            activeDecoder,
            MoveMessage(0xeeeeffff, empty, handC, 0))).IsSuccess);
        True(mirror.Apply(DecodeMessage(
            activeDecoder,
            MoveMessage(
                0,
                handC,
                new ModernLocInfoV1(0, 0x02, 0, 0x08),
                0))).IsSuccess);
        MirrorCardSnapshotV1 reorderedFirst = mirror.Snapshot.Cards.Single(
            card => card.Zone == MirrorZoneV1.Hand && card.Sequence == 0);
        Equal(0xeeeeffffu, reorderedFirst.CardCode.Value);

        MirrorApplyResult spellTrapSeven = mirror.Apply(DecodeMessage(
            activeDecoder,
            MoveMessage(
                0x12345678,
                empty,
                new ModernLocInfoV1(0, 0x08, 7, 0x04),
                0)));
        True(spellTrapSeven.IsSuccess, spellTrapSeven.Error.ToString());
        MirrorApplyResult monsterSix = mirror.Apply(DecodeMessage(
            activeDecoder,
            MoveMessage(
                0x23456789,
                empty,
                new ModernLocInfoV1(0, 0x04, 6, 0x04),
                0)));
        True(monsterSix.IsSuccess, monsterSix.Error.ToString());
        string beforeInvalidSpellTrap = mirror.Snapshot.ToDeterministicString();
        MirrorApplyResult invalidSpellTrap = mirror.Apply(DecodeMessage(
            activeDecoder,
            MoveMessage(
                0x87654321,
                empty,
                new ModernLocInfoV1(0, 0x08, 8, 0x04),
                0)));
        False(invalidSpellTrap.IsSuccess);
        Equal(GameplayErrorCode.StateCapacityExceeded, invalidSpellTrap.Error);
        Equal(beforeInvalidSpellTrap, mirror.Snapshot.ToDeterministicString());

        ModernQueryV1 updateQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Code, U32(0x55667788)),
            QueryRecord(QueryFlagV1.Position, U32(0x04)),
            QueryRecord(QueryFlagV1.Owner, new byte[] { 0 }),
            QueryEnd());
        MirrorApplyResult updateResult = mirror.Apply(
            DecodeMessage(activeDecoder, UpdateCardMessage(0, 0x04, 0, updateQuery)));
        True(updateResult.IsSuccess, updateResult.Error.ToString());
        MirrorCardSnapshotV1 updatedCard = mirror.Snapshot.Cards.Single(
            card => card.Zone == MirrorZoneV1.MonsterZone && card.Sequence == 0);
        Equal(0x55667788u, updatedCard.CardCode.Value);
        Equal((uint)0x04, updatedCard.Position.Value);
        Equal(MirrorParticipantRoleV1.Self, updatedCard.Owner.Value);

        ModernQueryV1 invalidOwnerQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Code, U32(0xdeadbeef)),
            QueryRecord(QueryFlagV1.Owner, new byte[] { 2 }),
            QueryEnd());
        string beforeInvalidQuery = mirror.Snapshot.ToDeterministicString();
        MirrorApplyResult invalidQuery = mirror.Apply(
            DecodeMessage(activeDecoder, UpdateCardMessage(0, 0x04, 0, invalidOwnerQuery)));
        False(invalidQuery.IsSuccess);
        Equal(GameplayErrorCode.InvalidParticipant, invalidQuery.Error);
        Equal(beforeInvalidQuery, mirror.Snapshot.ToDeterministicString());

        string beforeInvalidSequence = mirror.Snapshot.ToDeterministicString();
        MirrorApplyResult invalidSequence = mirror.Apply(
            DecodeMessage(activeDecoder, UpdateCardMessage(0, 0x04, 7, updateQuery)));
        False(invalidSequence.IsSuccess);
        Equal(GameplayErrorCode.StateCapacityExceeded, invalidSequence.Error);
        Equal(beforeInvalidSequence, mirror.Snapshot.ToDeterministicString());

        ModernLocInfoV1 monster1 = new(0, 0x04, 1, 0x04);
        MirrorApplyResult createSecondResult = mirror.Apply(
            DecodeMessage(activeDecoder, MoveMessage(0x99aabbcc, empty, monster1, 0)));
        True(createSecondResult.IsSuccess, createSecondResult.Error.ToString());
        Equal(7, mirror.Snapshot.Cards.Count);

        string beforeConflict = mirror.Snapshot.ToDeterministicString();
        MirrorApplyResult conflict = mirror.Apply(
            DecodeMessage(activeDecoder, MoveMessage(0xabcdef01, empty, monster0, 0)));
        False(conflict.IsSuccess);
        Equal(GameplayErrorCode.ConflictingSlotOccupancy, conflict.Error);
        Equal(beforeConflict, mirror.Snapshot.ToDeterministicString());

        MirrorApplyResult swapResult = mirror.Apply(
            DecodeMessage(activeDecoder, SwapMessage(monster0, monster1)));
        True(swapResult.IsSuccess, swapResult.Error.ToString());
        MirrorCardSnapshotV1 atSlot0 = mirror.Snapshot.Cards.Single(
            card => card.Zone == MirrorZoneV1.MonsterZone && card.Sequence == 0);
        Equal(0x99aabbccu, atSlot0.CardCode.Value);

        MirrorApplyResult positionResult = mirror.Apply(
            DecodeMessage(
                activeDecoder,
                PosChangeMessage(0, 0x04, 0, 0x04, 0x08)));
        True(positionResult.IsSuccess, positionResult.Error.ToString());

        string beforeSet = mirror.Snapshot.ToDeterministicString();
        MirrorApplyResult setResult = mirror.Apply(
            DecodeMessage(activeDecoder, SetMessage(0x10203040, monster0)));
        True(setResult.IsSuccess, setResult.Error.ToString());
        Equal(beforeSet, mirror.Snapshot.ToDeterministicString());

        MirrorApplyResult targetResult = mirror.Apply(
            DecodeMessage(activeDecoder, CardTargetMessage(monster0, monster1)));
        True(targetResult.IsSuccess, targetResult.Error.ToString());
        Equal(1, mirror.Snapshot.TargetRelations.Count);
        MirrorApplyResult cancelResult = mirror.Apply(
            DecodeMessage(activeDecoder, CardTargetMessage(monster0, monster1, true)));
        True(cancelResult.IsSuccess, cancelResult.Error.ToString());
        Equal(0, mirror.Snapshot.TargetRelations.Count);

        MirrorApplyResult equipResult = mirror.Apply(
            DecodeMessage(activeDecoder, EquipMessage(monster0, monster1)));
        True(equipResult.IsSuccess, equipResult.Error.ToString());
        Equal(1, mirror.Snapshot.EquipmentRelations.Count);
        ModernLocInfoV1 monster2 = new(0, 0x04, 2, 0x04);
        True(mirror.Apply(DecodeMessage(
            activeDecoder,
            MoveMessage(0x55660011, empty, monster2, 0))).IsSuccess);
        MirrorApplyResult retargetResult = mirror.Apply(
            DecodeMessage(activeDecoder, EquipMessage(monster0, monster2)));
        True(retargetResult.IsSuccess, retargetResult.Error.ToString());
        Equal(1, mirror.Snapshot.EquipmentRelations.Count);
        MirrorApplyResult unequipResult = mirror.Apply(
            DecodeMessage(activeDecoder, UnequipMessage(monster0)));
        True(unequipResult.IsSuccess, unequipResult.Error.ToString());
        Equal(0, mirror.Snapshot.EquipmentRelations.Count);

        MirrorApplyResult chaining = mirror.Apply(
            DecodeMessage(activeDecoder, ChainingMessage(monster0, 1, 0)));
        True(chaining.IsSuccess, chaining.Error.ToString());
        NotEqual(beforeSet, mirror.Snapshot.ToDeterministicString());
        True(mirror.Snapshot.PendingChain.IsKnown);
        MirrorApplyResult chained = mirror.Apply(
            DecodeMessage(activeDecoder, new byte[] { 71, 1 }));
        True(chained.IsSuccess, chained.Error.ToString());
        False(mirror.Snapshot.Chains[0].CardCode.IsKnown);
        MirrorApplyResult target = mirror.Apply(
            DecodeMessage(activeDecoder, BecomeTargetMessage(monster1)));
        True(target.IsSuccess, target.Error.ToString());
        Equal(1, mirror.Snapshot.ChainTargetRelations.Count);
        True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 72, 1 })).IsSuccess);
        True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 75, 1 })).IsSuccess);
        False(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 75, 1 })).IsSuccess);
        True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 74 })).IsSuccess);
        Equal(0, mirror.Snapshot.Chains.Count);

        MirrorApplyResult secondChaining = mirror.Apply(
            DecodeMessage(activeDecoder, ChainingMessage(monster1, 1, 0)));
        True(secondChaining.IsSuccess, secondChaining.Error.ToString());
        True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 71, 1 })).IsSuccess);
        True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 72, 1 })).IsSuccess);
        True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 76, 1 })).IsSuccess);
        True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 73, 1 })).IsSuccess);
        True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 74 })).IsSuccess);

        True(mirror.Apply(DecodeMessage(
            activeDecoder,
            MoveMessage(
                0,
                empty,
                new ModernLocInfoV1(0, 0x84, 0, 0),
                0))).IsSuccess);
        MirrorApplyResult overlayParentSwap = mirror.Apply(
            DecodeMessage(activeDecoder, SwapMessage(monster0, monster1)));
        False(overlayParentSwap.IsSuccess);
        Equal(GameplayErrorCode.InvalidRelation, overlayParentSwap.Error);
    }

    internal static void TestDrawLpAndTerminal()
    {
        (PerspectiveStateMirrorV1 knownDeckMirror, GameplayMessageDecoderV1 knownDeckDecoder) =
            CreateMirror(0x00, deckCount0: 2, deckCount1: 2);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        True(knownDeckMirror.Apply(DecodeMessage(
            knownDeckDecoder,
            MoveMessage(
                0x01020304,
                empty,
                new ModernLocInfoV1(0, 0x01, 0, 0x01),
                0))).IsSuccess);
        string beforeKnownDeckDraw = knownDeckMirror.Snapshot.ToDeterministicString();
        MirrorApplyResult knownDeckDraw = knownDeckMirror.Apply(DecodeMessage(
            knownDeckDecoder,
            DrawMessage(0, (0x11223344u, 0x00000004u))));
        False(knownDeckDraw.IsSuccess);
        Equal(GameplayErrorCode.UnknownMirrorReference, knownDeckDraw.Error);
        Equal(beforeKnownDeckDraw, knownDeckMirror.Snapshot.ToDeterministicString());

        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0x00, deckCount0: 3, deckCount1: 3);

        GameplayMessageV1 draw = DecodeMessage(
            decoder,
            DrawMessage(0, (0x11223344u, 0x00000004u)));
        MirrorApplyResult drawResult = mirror.Apply(draw);
        True(drawResult.IsSuccess, drawResult.Error.ToString());
        Equal(2u, mirror.Snapshot.GetZone(MirrorParticipantRoleV1.Self, MirrorZoneV1.MainDeck).Count.Value);
        Equal(1u, mirror.Snapshot.GetZone(MirrorParticipantRoleV1.Self, MirrorZoneV1.Hand).Count.Value);
        Equal(0x11223344u, mirror.Snapshot.Cards.Single().CardCode.Value);

        GameplayMessageDecodeResult twoDraws = decoder.Decode(
            new StocGameMessagePayload(
                DrawMessage(
                    0,
                    (0x11223344u, 0x00000004u),
                    (0xaabbccddu, 0x00000008u))));
        True(twoDraws.IsSuccess, twoDraws.Error.ToString());
        Equal(2, twoDraws.Message!.Draw!.Cards.Count);
        Equal(0x11223344u, twoDraws.Message.Draw.Cards[0].CardCode);
        Equal(0x00000004u, twoDraws.Message.Draw.Cards[0].Position);
        Equal(0xaabbccddu, twoDraws.Message.Draw.Cards[1].CardCode);
        Equal(0x00000008u, twoDraws.Message.Draw.Cards[1].Position);

        True(mirror.Apply(DecodeMessage(decoder, new byte[] { 91, 0, 0xf4, 0x01, 0, 0 })).IsSuccess);
        Equal(7500u, mirror.Snapshot.Participants[0].LifePoints.Value);
        True(mirror.Apply(DecodeMessage(decoder, new byte[] { 92, 0, 0xfa, 0, 0, 0 })).IsSuccess);
        Equal(7750u, mirror.Snapshot.Participants[0].LifePoints.Value);
        True(mirror.Apply(DecodeMessage(decoder, new byte[] { 94, 1, 0x70, 0x17, 0, 0 })).IsSuccess);
        Equal(6000u, mirror.Snapshot.Participants[1].LifePoints.Value);
        True(mirror.Apply(DecodeMessage(decoder, new byte[] { 100, 1, 0xf4, 0x01, 0, 0 })).IsSuccess);
        Equal(5500u, mirror.Snapshot.Participants[1].LifePoints.Value);

        string beforeLpOverflow = mirror.Snapshot.ToDeterministicString();
        MirrorApplyResult lpOverflow = mirror.Apply(DecodeMessage(
            decoder,
            new byte[] { 92, 0, 0xff, 0xff, 0xff, 0xff }));
        False(lpOverflow.IsSuccess);
        Equal(GameplayErrorCode.ArithmeticFailure, lpOverflow.Error);
        Equal(beforeLpOverflow, mirror.Snapshot.ToDeterministicString());

        GameplayMessageDecodeResult zeroDraw = decoder.Decode(
            new StocGameMessagePayload(DrawMessage(0)));
        False(zeroDraw.IsSuccess);
        Equal(GameplayErrorCode.InvalidDrawCount, zeroDraw.Error);

        GameplayMessageV1 terminal = DecodeMessage(decoder, new byte[] { 5, 2, 0x07 });
        MirrorApplyResult terminalResult = mirror.Apply(terminal);
        True(terminalResult.IsSuccess, terminalResult.Error.ToString());
        True(mirror.Snapshot.Terminal.IsTerminal);
        Null(mirror.Snapshot.Terminal.Winner);
        Equal((byte)0x07, mirror.Snapshot.Terminal.WinType);

        MirrorApplyResult duplicateTerminal = mirror.Apply(
            DecodeMessage(decoder, new byte[] { 5, 2, 0x07 }));
        False(duplicateTerminal.IsSuccess);
        Equal(GameplayErrorCode.TerminalStateMutation, duplicateTerminal.Error);
        MirrorApplyResult afterTerminal = mirror.Apply(
            DecodeMessage(decoder, new byte[] { 40, 0 }));
        False(afterTerminal.IsSuccess);
        Equal(GameplayErrorCode.TerminalStateMutation, afterTerminal.Error);
    }

    internal static void TestProvenanceAndLocatorSafety()
    {
        (PerspectiveStateMirrorV1 selfMirror, GameplayMessageDecoderV1 selfDecoder) =
            CreateMirror(0x00, deckCount0: 3, deckCount1: 3);
        True(selfMirror.Apply(DecodeMessage(
            selfDecoder,
            DrawMessage(0, (0x11223344u, 0x00000008u)))).IsSuccess);
        MirrorCardSnapshotV1 selfHand = selfMirror.Snapshot.Cards.Single(
            card => card.Zone == MirrorZoneV1.Hand);
        Equal(MirrorProvenanceV1.PerspectivePrivateFact, selfHand.CardCode.Provenance);

        ModernQueryV1 selfQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Code, U32(0xaabbccddu)),
            QueryRecord(QueryFlagV1.Position, U32(0x00000008u)),
            QueryEnd());
        True(selfMirror.Apply(DecodeMessage(
            selfDecoder,
            UpdateCardMessage(0, 0x02, 0, selfQuery))).IsSuccess);
        MirrorCardSnapshotV1 updatedSelfHand = selfMirror.Snapshot.Cards.Single(
            card => card.Zone == MirrorZoneV1.Hand);
        Equal(MirrorProvenanceV1.PerspectivePrivateFact, updatedSelfHand.CardCode.Provenance);
        Equal(
            MirrorProvenanceV1.PerspectivePrivateFact,
            updatedSelfHand.QueryFields.Single(field => field.Flag == QueryFlagV1.Code).Provenance);

        (PerspectiveStateMirrorV1 publicMirror, GameplayMessageDecoderV1 publicDecoder) =
            CreateMirror(0x00);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 publicSlot = new(0, 0x04, 0, 0x04);
        True(publicMirror.Apply(DecodeMessage(
            publicDecoder,
            MoveMessage(0, empty, publicSlot, 0))).IsSuccess);
        ModernQueryV1 publicQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Code, U32(0x12345678u)),
            QueryRecord(QueryFlagV1.Position, U32(0x00000004u)),
            QueryEnd());
        True(publicMirror.Apply(DecodeMessage(
            publicDecoder,
            UpdateCardMessage(0, 0x04, 0, publicQuery))).IsSuccess);
        MirrorCardSnapshotV1 publicCard = publicMirror.Snapshot.Cards.Single();
        Equal(MirrorProvenanceV1.PublicProtocolFact, publicCard.CardCode.Provenance);
        Equal(
            MirrorProvenanceV1.PublicProtocolFact,
            publicCard.QueryFields.Single(field => field.Flag == QueryFlagV1.Code).Provenance);

        (PerspectiveStateMirrorV1 firstOrderMirror, GameplayMessageDecoderV1 firstOrderDecoder) =
            CreateMirror(0x00);
        True(firstOrderMirror.Apply(DecodeMessage(
            firstOrderDecoder,
            MoveMessage(0, empty, publicSlot, 0))).IsSuccess);
        ModernQueryV1 firstOrderQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Code, U32(0x10203040u)),
            QueryRecord(QueryFlagV1.Position, U32(0x00000004u)),
            QueryEnd());
        True(firstOrderMirror.Apply(DecodeMessage(
            firstOrderDecoder,
            UpdateCardMessage(0, 0x04, 0, firstOrderQuery))).IsSuccess);

        (PerspectiveStateMirrorV1 secondOrderMirror, GameplayMessageDecoderV1 secondOrderDecoder) =
            CreateMirror(0x00);
        True(secondOrderMirror.Apply(DecodeMessage(
            secondOrderDecoder,
            MoveMessage(0, empty, publicSlot, 0))).IsSuccess);
        ModernQueryV1 secondOrderQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Position, U32(0x00000004u)),
            QueryRecord(QueryFlagV1.Code, U32(0x10203040u)),
            QueryEnd());
        True(secondOrderMirror.Apply(DecodeMessage(
            secondOrderDecoder,
            UpdateCardMessage(0, 0x04, 0, secondOrderQuery))).IsSuccess);
        Equal(
            firstOrderMirror.Snapshot.Cards.Single().CardCode.Provenance,
            secondOrderMirror.Snapshot.Cards.Single().CardCode.Provenance);

        (PerspectiveStateMirrorV1 referenceMirror, GameplayMessageDecoderV1 referenceDecoder) =
            CreateMirror(0x00);
        ModernLocInfoV1 referenceSource = new(0, 0x04, 0, 0x04);
        ModernLocInfoV1 referenceTarget = new(0, 0x04, 1, 0x04);
        True(referenceMirror.Apply(DecodeMessage(
            referenceDecoder,
            MoveMessage(0, empty, referenceSource, 0))).IsSuccess);
        True(referenceMirror.Apply(DecodeMessage(
            referenceDecoder,
            MoveMessage(0, empty, referenceTarget, 0))).IsSuccess);
        ModernQueryV1 referenceQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Position, U32(0x00000004u)),
            QueryRecord(QueryFlagV1.ReasonCard, LocInfo(0, 0x04, 1, 0x04)),
            QueryRecord(
                QueryFlagV1.TargetCard,
                Join(U32(1), LocInfo(0, 0x04, 1, 0x04))),
            QueryEnd());
        True(referenceMirror.Apply(DecodeMessage(
            referenceDecoder,
            UpdateCardMessage(0, 0x04, 0, referenceQuery))).IsSuccess);
        MirrorCardSnapshotV1 referenceCard = referenceMirror.Snapshot.Cards.Single(
            card => card.Zone == MirrorZoneV1.MonsterZone && card.Sequence == 0);
        MirrorQueryFieldSnapshotV1 reasonField = referenceCard.QueryFields.Single(
            field => field.Flag == QueryFlagV1.ReasonCard);
        MirrorQueryFieldSnapshotV1 targetField = referenceCard.QueryFields.Single(
            field => field.Flag == QueryFlagV1.TargetCard);
        Equal(MirrorQueryValueKindV1.EntityReference, reasonField.Value.Kind);
        Equal(MirrorQueryValueKindV1.EntityReferenceVector, targetField.Value.Kind);
        Equal(1, reasonField.Value.EntityReferenceCount);
        Equal(1, targetField.Value.EntityReferenceCount);

        string beforeUnknownReference = referenceMirror.Snapshot.ToDeterministicString();
        ModernQueryV1 unknownReferenceQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.ReasonCard, LocInfo(0, 0x04, 2, 0x04)),
            QueryEnd());
        MirrorApplyResult unknownReference = referenceMirror.Apply(DecodeMessage(
            referenceDecoder,
            UpdateCardMessage(0, 0x04, 0, unknownReferenceQuery)));
        False(unknownReference.IsSuccess);
        Equal(GameplayErrorCode.UnknownMirrorReference, unknownReference.Error);
        Equal(beforeUnknownReference, referenceMirror.Snapshot.ToDeterministicString());

        (PerspectiveStateMirrorV1 opponentMirror, GameplayMessageDecoderV1 opponentDecoder) =
            CreateMirror(0x00);
        ModernLocInfoV1 opponentHand = new(1, 0x02, 0, 0x08);
        True(opponentMirror.Apply(DecodeMessage(
            opponentDecoder,
            MoveMessage(0x55667788, empty, opponentHand, 0))).IsSuccess);
        MirrorCardSnapshotV1 hiddenOpponent = opponentMirror.Snapshot.Cards.Single();
        False(hiddenOpponent.CardCode.IsKnown);
        Equal(MirrorProvenanceV1.UnknownRedacted, hiddenOpponent.CardCode.Provenance);
        ModernQueryV1 hiddenOpponentQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Code, U32(0x99887766u)),
            QueryRecord(QueryFlagV1.Position, U32(0x00000008u)),
            QueryEnd());
        True(opponentMirror.Apply(DecodeMessage(
            opponentDecoder,
            UpdateCardMessage(1, 0x02, 0, hiddenOpponentQuery))).IsSuccess);
        MirrorCardSnapshotV1 updatedHiddenOpponent = opponentMirror.Snapshot.Cards.Single();
        False(updatedHiddenOpponent.CardCode.IsKnown);
        Equal(MirrorProvenanceV1.UnknownRedacted, updatedHiddenOpponent.CardCode.Provenance);
        Equal(
            MirrorProvenanceV1.UnknownRedacted,
            updatedHiddenOpponent.QueryFields.Single(field => field.Flag == QueryFlagV1.Code)
                .Provenance);

        Null(typeof(MirrorChainSnapshotV1).GetProperty(
            "Location",
            BindingFlags.Instance | BindingFlags.Public));
        Null(typeof(MirrorQueryFieldSnapshotV1).GetProperty(
            "Field",
            BindingFlags.Instance | BindingFlags.Public));
        False(typeof(MirrorQueryFieldSnapshotV1).GetProperties().Any(
            property => typeof(ModernQueryPayloadV1).IsAssignableFrom(property.PropertyType)));
        False(typeof(MirrorSnapshotV1).GetProperties().Any(
            property => property.PropertyType == typeof(ModernLocInfoV1)));
    }

    internal static void TestVisibilityFlagOverlap()
    {
        ModernLocInfoV1 empty = new(0, 0, 0, 0);

        (PerspectiveStateMirrorV1 faceUpMirror, GameplayMessageDecoderV1 faceUpDecoder) =
            CreateMirror(0x00);
        ModernLocInfoV1 faceUpSlot = new(0, 0x04, 0, 0x04);
        True(faceUpMirror.Apply(DecodeMessage(
            faceUpDecoder,
            MoveMessage(0, empty, faceUpSlot, 0))).IsSuccess);
        ModernQueryV1 faceUpHiddenQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Code, U32(0x10203040u)),
            QueryRecord(QueryFlagV1.Position, U32(0x00000004u)),
            QueryRecord(QueryFlagV1.IsHidden, new byte[] { 1 }),
            QueryEnd());
        MirrorApplyResult faceUpHidden = faceUpMirror.Apply(DecodeMessage(
            faceUpDecoder,
            UpdateCardMessage(0, 0x04, 0, faceUpHiddenQuery)));
        True(faceUpHidden.IsSuccess, faceUpHidden.Error.ToString());
        Equal(
            MirrorProvenanceV1.PublicProtocolFact,
            faceUpMirror.Snapshot.Cards.Single().CardCode.Provenance);

        (PerspectiveStateMirrorV1 publicHiddenMirror,
            GameplayMessageDecoderV1 publicHiddenDecoder) = CreateMirror(0x00);
        ModernLocInfoV1 hiddenSlot = new(0, 0x02, 0, 0x08);
        True(publicHiddenMirror.Apply(DecodeMessage(
            publicHiddenDecoder,
            MoveMessage(0, empty, hiddenSlot, 0))).IsSuccess);
        ModernQueryV1 publicHiddenQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Code, U32(0x50607080u)),
            QueryRecord(QueryFlagV1.Position, U32(0x00000008u)),
            QueryRecord(QueryFlagV1.IsPublic, new byte[] { 1 }),
            QueryRecord(QueryFlagV1.IsHidden, new byte[] { 1 }),
            QueryEnd());
        MirrorApplyResult publicHidden = publicHiddenMirror.Apply(DecodeMessage(
            publicHiddenDecoder,
            UpdateCardMessage(0, 0x02, 0, publicHiddenQuery)));
        True(publicHidden.IsSuccess, publicHidden.Error.ToString());
        Equal(
            MirrorProvenanceV1.PublicProtocolFact,
            publicHiddenMirror.Snapshot.Cards.Single().CardCode.Provenance);

        (PerspectiveStateMirrorV1 opponentMirror, GameplayMessageDecoderV1 opponentDecoder) =
            CreateMirror(0x00);
        ModernLocInfoV1 opponentHiddenSlot = new(1, 0x02, 0, 0x08);
        True(opponentMirror.Apply(DecodeMessage(
            opponentDecoder,
            MoveMessage(0, empty, opponentHiddenSlot, 0))).IsSuccess);
        ModernQueryV1 opponentHiddenQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Code, U32(0x90a0b0c0u)),
            QueryRecord(QueryFlagV1.IsHidden, new byte[] { 1 }),
            QueryRecord(QueryFlagV1.Position, U32(0x00000008u)),
            QueryEnd());
        MirrorApplyResult opponentHidden = opponentMirror.Apply(DecodeMessage(
            opponentDecoder,
            UpdateCardMessage(1, 0x02, 0, opponentHiddenQuery)));
        True(opponentHidden.IsSuccess, opponentHidden.Error.ToString());
        MirrorCardSnapshotV1 opponentCard = opponentMirror.Snapshot.Cards.Single();
        False(opponentCard.CardCode.IsKnown);
        Equal(MirrorProvenanceV1.UnknownRedacted, opponentCard.CardCode.Provenance);

        (PerspectiveStateMirrorV1 firstOrderMirror, GameplayMessageDecoderV1 firstOrderDecoder) =
            CreateMirror(0x00);
        True(firstOrderMirror.Apply(DecodeMessage(
            firstOrderDecoder,
            MoveMessage(0, empty, hiddenSlot, 0))).IsSuccess);
        ModernQueryV1 firstOrderQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Code, U32(0x01020304u)),
            QueryRecord(QueryFlagV1.Position, U32(0x00000008u)),
            QueryRecord(QueryFlagV1.IsHidden, new byte[] { 1 }),
            QueryRecord(QueryFlagV1.IsPublic, new byte[] { 1 }),
            QueryEnd());
        True(firstOrderMirror.Apply(DecodeMessage(
            firstOrderDecoder,
            UpdateCardMessage(0, 0x02, 0, firstOrderQuery))).IsSuccess);

        (PerspectiveStateMirrorV1 secondOrderMirror, GameplayMessageDecoderV1 secondOrderDecoder) =
            CreateMirror(0x00);
        True(secondOrderMirror.Apply(DecodeMessage(
            secondOrderDecoder,
            MoveMessage(0, empty, hiddenSlot, 0))).IsSuccess);
        ModernQueryV1 secondOrderQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.IsPublic, new byte[] { 1 }),
            QueryRecord(QueryFlagV1.IsHidden, new byte[] { 1 }),
            QueryRecord(QueryFlagV1.Position, U32(0x00000008u)),
            QueryRecord(QueryFlagV1.Code, U32(0x01020304u)),
            QueryEnd());
        True(secondOrderMirror.Apply(DecodeMessage(
            secondOrderDecoder,
            UpdateCardMessage(0, 0x02, 0, secondOrderQuery))).IsSuccess);
        Equal(
            firstOrderMirror.Snapshot.Cards.Single().CardCode.Provenance,
            secondOrderMirror.Snapshot.Cards.Single().CardCode.Provenance);

        string beforeFailure = firstOrderMirror.Snapshot.ToDeterministicString();
        ModernQueryV1 unknownReferenceQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Code, U32(0xa1b2c3d4u)),
            QueryRecord(QueryFlagV1.ReasonCard, LocInfo(0, 0x04, 0, 0x04)),
            QueryEnd());
        MirrorApplyResult failed = firstOrderMirror.Apply(DecodeMessage(
            firstOrderDecoder,
            UpdateCardMessage(0, 0x02, 0, unknownReferenceQuery)));
        False(failed.IsSuccess);
        Equal(GameplayErrorCode.UnknownMirrorReference, failed.Error);
        Equal(beforeFailure, firstOrderMirror.Snapshot.ToDeterministicString());
    }

    internal static void TestFaceDownTransition()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0x00);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 first = new(0, 0x04, 0, 0x04);
        ModernLocInfoV1 second = new(0, 0x04, 1, 0x04);
        True(mirror.Apply(DecodeMessage(
            decoder,
            MoveMessage(0x11223344, empty, first, 0))).IsSuccess);
        True(mirror.Apply(DecodeMessage(
            decoder,
            MoveMessage(0x55667788, empty, second, 0))).IsSuccess);
        True(mirror.Apply(DecodeMessage(
            decoder,
            CardTargetMessage(first, second))).IsSuccess);
        ModernQueryV1 query = DecodeQuery(
            QueryRecord(QueryFlagV1.Code, U32(0x11223344)),
            QueryRecord(QueryFlagV1.Type, U32(0x01)),
            QueryEnd());
        True(mirror.Apply(DecodeMessage(
            decoder,
            UpdateCardMessage(0, 0x04, 0, query))).IsSuccess);

        MirrorApplyResult faceDown = mirror.Apply(DecodeMessage(
            decoder,
            PosChangeMessage(0, 0x04, 0, 0x04, 0x08)));
        True(faceDown.IsSuccess, faceDown.Error.ToString());
        Equal(0, mirror.Snapshot.TargetRelations.Count);
        MirrorCardSnapshotV1 hidden = mirror.Snapshot.Cards.Single(
            card => card.Zone == MirrorZoneV1.MonsterZone && card.Sequence == 0);
        False(hidden.CardCode.IsKnown);
        Equal(0, hidden.QueryFields.Count);
    }

    internal static void TestUpdateDataWireOrder()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0x00);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        True(mirror.Apply(DecodeMessage(
            decoder,
            MoveMessage(0x100, empty, new ModernLocInfoV1(0, 0x02, 0, 0x08), 0))).IsSuccess);
        True(mirror.Apply(DecodeMessage(
            decoder,
            MoveMessage(0x200, empty, new ModernLocInfoV1(0, 0x02, 1, 0x08), 0))).IsSuccess);

        ModernQueryV1 orderedQuery = DecodeQuery(
            QueryRecord(QueryFlagV1.Position, U32(0x04)),
            QueryRecord(QueryFlagV1.Code, U32(0xabcdef01)),
            QueryEnd());
        byte[] emptyQuery = QueryEnd();
        GameplayMessageV1 updateData = DecodeMessage(
            decoder,
            UpdateDataMessage(0, 0x02, Join(orderedQuery.RawBytes.ToArray(), emptyQuery)));
        MirrorApplyResult result = mirror.Apply(updateData);
        True(result.IsSuccess, result.Error.ToString());
        MirrorCardSnapshotV1 first = mirror.Snapshot.Cards.Single(
            card => card.Zone == MirrorZoneV1.Hand && card.Sequence == 0);
        Equal(0xabcdef01u, first.CardCode.Value);
        Equal(2, first.QueryFields.Count);
        Equal(QueryFlagV1.Position, first.QueryFields[0].Flag);
        Equal(QueryFlagV1.Code, first.QueryFields[1].Flag);
    }

    internal static void TestMirrorChunking()
    {
        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0x00));
        byte[] moveFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            MoveMessage(
                0x11223344,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x02, 0, 0x08),
                0));
        byte[] turnFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            new byte[] { 40, 0 });
        byte[] phaseFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            new byte[] { 41, 4, 0 });
        byte[] transcript = Join(startFrame, moveFrame, turnFrame, phaseFrame);

        string whole = RunMirrorTranscript(new[] { transcript });
        string oneByte = RunMirrorTranscript(
            transcript.Select(value => new[] { value }).ToArray());
        string irregular = RunMirrorTranscript(
            Split(transcript, new[] { 1, 2, 7, 3, 11 }));
        Equal(whole, oneByte);
        Equal(whole, irregular);
    }
}
