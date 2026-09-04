using System.Buffers.Binary;
using System.Reflection;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I4CIdleBattlePromptTests
{
    private static readonly byte[] BattleMixedVector =
    {
        0x0A, 0x00, 0x01, 0x00, 0x00, 0x00,
        0x44, 0x33, 0x22, 0x11, 0x00, 0x04,
        0x01, 0x00, 0x00, 0x00, 0x08, 0x07,
        0x06, 0x05, 0x04, 0x03, 0x02, 0x01, 0x00,
        0x02, 0x00, 0x00, 0x00,
        0xDD, 0xCC, 0xBB, 0xAA, 0x00, 0x04,
        0x00, 0x01,
        0x04, 0x03, 0x02, 0x01, 0x00, 0x04,
        0x01, 0x00, 0x01, 0x00
    };

    private static readonly byte[] IdleAllSectionsVector =
    {
        0x0B, 0x01,
        0x01, 0x00, 0x00, 0x00,
        0x04, 0x03, 0x02, 0x01, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00,
        0x08, 0x07, 0x06, 0x05, 0x01, 0x02, 0x01, 0x00, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00,
        0x0C, 0x0B, 0x0A, 0x09, 0x01, 0x04, 0x02,
        0x01, 0x00, 0x00, 0x00,
        0x10, 0x0F, 0x0E, 0x0D, 0x01, 0x02, 0x03,
        0x00, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00,
        0x14, 0x13, 0x12, 0x11, 0x01, 0x02, 0x04,
        0x00, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00,
        0x18, 0x17, 0x16, 0x15, 0x01, 0x04, 0x05,
        0x00, 0x00, 0x00,
        0x28, 0x27, 0x26, 0x25, 0x24, 0x23, 0x22, 0x21,
        0x02, 0x01, 0x01, 0x01
    };

    internal static void TestBattleExactWireAndContext()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(
                0xAABBCCDD,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 1, 0x05)));
        FlatPromptProjectionResultV1 result = AcceptBattle(
            authority,
            BattleMixedVector);

        AssertSuccess(result, FlatPromptFamilyV1.MsgSelectBattleCmd);
        Equal(4, result.Candidates!.Count);
        FlatBattleActivatablePublicCandidateBaseV1 activate =
            result.Candidates[0] as FlatBattleActivatablePublicCandidateBaseV1 ??
            throw new InvalidOperationException("expected BATTLE activate");
        Equal(FlatPromptChoiceKindV1.Activate, activate.ChoiceKind);
        Equal(FlatPromptSourceSectionV1.Activatable, activate.SourceSection);
        Equal(0, activate.SourceOrdinal);
        Equal("p0:MONSTER_ZONE:1", activate.PublicSemanticCardLocator.Value);
        Equal(0x0102030405060708ul, activate.DescriptionOrEffectId);
        Equal((byte)0, activate.ClientMode);
        True(activate is FlatBattleActivatableCardCodePublicCandidateV1);

        FlatBattleAttackPublicCandidateBaseV1 firstAttack =
            result.Candidates[1] as FlatBattleAttackPublicCandidateBaseV1 ??
            throw new InvalidOperationException("expected BATTLE attack");
        Equal(FlatPromptChoiceKindV1.Attack, firstAttack.ChoiceKind);
        Equal(FlatPromptSourceSectionV1.Attackable, firstAttack.SourceSection);
        Equal(0, firstAttack.SourceOrdinal);
        Equal("p0:MONSTER_ZONE:0", firstAttack.PublicSemanticCardLocator.Value);
        True(firstAttack.DirectAttackable);
        True(firstAttack is FlatBattleAttackCardCodePublicCandidateV1);

        FlatBattleAttackPublicCandidateBaseV1 secondAttack =
            result.Candidates[2] as FlatBattleAttackPublicCandidateBaseV1 ??
            throw new InvalidOperationException("expected BATTLE attack");
        Equal(1, secondAttack.SourceOrdinal);
        Equal("p0:MONSTER_ZONE:1", secondAttack.PublicSemanticCardLocator.Value);
        False(secondAttack.DirectAttackable);
        False(secondAttack is FlatBattleAttackCardCodePublicCandidateV1);

        FlatBattleToMainPhase2PublicCandidateV1 toM2 =
            result.Candidates[3] as FlatBattleToMainPhase2PublicCandidateV1 ??
            throw new InvalidOperationException("expected TO_M2");
        Equal("MAIN_PHASE_2", toM2.TransitionToken);
        False(result.Candidates.Any(
            candidate => candidate is FlatBattleToEndPhasePublicCandidateV1));
    }

    internal static void TestBattleMixedSectionsAndCompleteOrder()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(0x10000001, new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new CardSpec(0x10000002, new ModernLocInfoV1(0, 0x04, 1, 0x05)),
            new CardSpec(0x10000003, new ModernLocInfoV1(0, 0x04, 2, 0x05)));
        FlatPromptProjectionResultV1 result = AcceptBattle(
            authority,
            BattleMessage(
                0,
                new[]
                {
                    new BattleActivationSpec(
                        0x10000001,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        1,
                        0),
                    new BattleActivationSpec(
                        0x10000002,
                        new ModernLocInfoV1(0, 0x04, 1, 0),
                        2,
                        1)
                },
                new[]
                {
                    new BattleAttackSpec(
                        0x10000003,
                        new ModernLocInfoV1(0, 0x04, 2, 0),
                        true),
                    new BattleAttackSpec(
                        0x10000003,
                        new ModernLocInfoV1(0, 0x04, 2, 0),
                        true)
                },
                1,
                1));

        AssertSuccess(result, FlatPromptFamilyV1.MsgSelectBattleCmd);
        Equal(6, result.Candidates!.Count);
        True(
            new[]
            {
                "MSG_SELECT_BATTLECMD:ACTIVATE:0",
                "MSG_SELECT_BATTLECMD:ACTIVATE:1",
                "MSG_SELECT_BATTLECMD:ATTACK:0",
                "MSG_SELECT_BATTLECMD:ATTACK:1",
                "MSG_SELECT_BATTLECMD:TO_M2",
                "MSG_SELECT_BATTLECMD:TO_EP"
            }.SequenceEqual(
                result.Candidates.Select(candidate => candidate.I4LocalCandidateKey)));
        Equal(
            FlatPromptChoiceKindV1.Activate,
            result.Candidates[0].ChoiceKind);
        Equal(
            FlatPromptChoiceKindV1.Attack,
            result.Candidates[2].ChoiceKind);
        Equal(
            FlatPromptChoiceKindV1.ToM2,
            result.Candidates[4].ChoiceKind);
        Equal(
            FlatPromptChoiceKindV1.ToEp,
            result.Candidates[5].ChoiceKind);
        NotEqual(result.Candidates[2], result.Candidates[3]);
    }

    internal static void TestBattleResponseBindingsAndSectionOrdinals()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(0x10000001, new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new CardSpec(0x10000002, new ModernLocInfoV1(0, 0x04, 1, 0x05)));
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 result = session.TryAcceptPrompt(
            BattleMessage(
                0,
                new[]
                {
                    new BattleActivationSpec(
                        0x10000001,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        1,
                        0),
                    new BattleActivationSpec(
                        0x10000002,
                        new ModernLocInfoV1(0, 0x04, 1, 0),
                        2,
                        0)
                },
                new[]
                {
                    new BattleAttackSpec(
                        0x10000001,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        false),
                    new BattleAttackSpec(
                        0x10000002,
                        new ModernLocInfoV1(0, 0x04, 1, 0),
                        true)
                },
                1,
                1),
            authority.Mirror,
            authority.Projection);
        AssertSuccess(result, FlatPromptFamilyV1.MsgSelectBattleCmd);
        AssertResponse(session, "MSG_SELECT_BATTLECMD:ACTIVATE:0", 0);
        AssertResponse(session, "MSG_SELECT_BATTLECMD:ACTIVATE:1", 65536);
        AssertResponse(session, "MSG_SELECT_BATTLECMD:ATTACK:0", 1);
        AssertResponse(session, "MSG_SELECT_BATTLECMD:ATTACK:1", 65537);
        AssertResponse(session, "MSG_SELECT_BATTLECMD:TO_M2", 2);
        AssertResponse(session, "MSG_SELECT_BATTLECMD:TO_EP", 3);

        True(PublicSemanticLocatorV1.TryCreateIndexed(
            0,
            PublicSemanticZoneV1.MonsterZone,
            0,
            out PublicSemanticLocatorV1? zeroCodeLocator));
        FlatPublicCandidateDescriptorV1 zeroCodeCandidate =
            new FlatBattleActivatableCardCodePublicCandidateV1(
                "MSG_SELECT_BATTLECMD:ACTIVATE:0",
                0,
                zeroCodeLocator!,
                1,
                0,
                0);
        False(CurrentFlatPromptBindingV1.TryCreate(
            0,
            FlatPromptFamilyV1.MsgSelectBattleCmd,
            new[] { zeroCodeCandidate },
            new[] { "MSG_SELECT_BATTLECMD:ACTIVATE:0" },
            new[] { 0 },
            out CurrentFlatPromptBindingV1? invalidBinding,
            out FlatPromptErrorCodeV1 invalidBindingError));
        Null(invalidBinding);
        Equal(
            FlatPromptErrorCodeV1.InvalidResponseBinding,
            invalidBindingError);
    }

    internal static void TestBattleTransitionFlagsAndZeroDomain()
    {
        Authority authority = CreateAuthority(0, 0);
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            BattleMessage(0, Array.Empty<BattleActivationSpec>(),
                Array.Empty<BattleAttackSpec>(), 0, 0),
            FlatPromptErrorCodeV1.ZeroOptionDomain);

        FlatPromptProjectionResultV1 onlyM2 = AcceptBattle(
            authority,
            BattleMessage(0, Array.Empty<BattleActivationSpec>(),
                Array.Empty<BattleAttackSpec>(), 1, 0));
        AssertSuccess(onlyM2, FlatPromptFamilyV1.MsgSelectBattleCmd);
        Equal(1, onlyM2.Candidates!.Count);
        True(onlyM2.Candidates[0] is FlatBattleToMainPhase2PublicCandidateV1);

        FlatPromptProjectionResultV1 onlyEnd = AcceptBattle(
            authority,
            BattleMessage(0, Array.Empty<BattleActivationSpec>(),
                Array.Empty<BattleAttackSpec>(), 0, 1));
        AssertSuccess(onlyEnd, FlatPromptFamilyV1.MsgSelectBattleCmd);
        Equal(1, onlyEnd.Candidates!.Count);
        True(onlyEnd.Candidates[0] is FlatBattleToEndPhasePublicCandidateV1);
    }

    internal static void TestBattleIndexedCorrelationAndAcceptedLocator()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(0x11111111, new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new CardSpec(0x22222222, new ModernLocInfoV1(0, 0x08, 0, 0x05)));
        FlatPromptProjectionResultV1 result = AcceptBattle(
            authority,
            BattleMessage(
                0,
                new[]
                {
                    new BattleActivationSpec(
                        0x11111111,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        1,
                        0),
                    new BattleActivationSpec(
                        0x22222222,
                        new ModernLocInfoV1(0, 0x08, 0, 0),
                        2,
                        0)
                },
                Array.Empty<BattleAttackSpec>(),
                0,
                0));

        AssertSuccess(result, FlatPromptFamilyV1.MsgSelectBattleCmd);
        Equal(
            "p0:MONSTER_ZONE:0",
            ((FlatBattleActivatablePublicCandidateBaseV1)result.Candidates![0])
                .PublicSemanticCardLocator.Value);
        Equal(
            "p0:SPELL_TRAP_ZONE:0",
            ((FlatBattleActivatablePublicCandidateBaseV1)result.Candidates[1])
                .PublicSemanticCardLocator.Value);
        Equal(
            FlatPromptSourceSectionV1.Activatable,
            ((FlatBattleActivatablePublicCandidateBaseV1)result.Candidates[0])
                .SourceSection);
    }

    internal static void TestBattleCardCodeSafetyAndAmbiguity()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(0x11223344, new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        FlatPromptProjectionResultV1 zeroCode = AcceptBattle(
            authority,
            BattleMessage(
                0,
                new[]
                {
                    new BattleActivationSpec(
                        0,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        1,
                        0)
                },
                Array.Empty<BattleAttackSpec>(),
                0,
                0));
        AssertSuccess(zeroCode, FlatPromptFamilyV1.MsgSelectBattleCmd);
        False(zeroCode.Candidates![0]
            is FlatBattleActivatableCardCodePublicCandidateV1);

        FlatPromptProjectionResultV1 mismatchedCode = AcceptBattle(
            authority,
            BattleMessage(
                0,
                new[]
                {
                    new BattleActivationSpec(
                        0x55667788,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        1,
                        0)
                },
                Array.Empty<BattleAttackSpec>(),
                0,
                0));
        AssertSuccess(mismatchedCode, FlatPromptFamilyV1.MsgSelectBattleCmd);
        False(mismatchedCode.Candidates![0]
            is FlatBattleActivatableCardCodePublicCandidateV1);

        PublicCardStateV1 card = authority.Projection.Snapshot!.Cards.Single();
        PublicStateSnapshotV1 ambiguousSnapshot = WithCards(
            authority.Projection.Snapshot,
            new[]
            {
                card,
                new PublicCardStateV1(
                    card.Locator,
                    card.AbsolutePlayer,
                    card.Zone,
                    card.CardCode,
                    card.Position)
            });
        False(FlatPromptCardCorrelationV1.TryCorrelate(
            authority.Mirror.Snapshot,
            ambiguousSnapshot,
            0x11223344,
            new ModernLocInfoV1(0, 0x04, 0, 0),
            out FlatPromptCardCorrelationResultV1? correlation,
            out FlatPromptErrorCodeV1 correlationError));
        Null(correlation);
        Equal(
            FlatPromptErrorCodeV1.UnprovenPublicReference,
            correlationError);
    }

    internal static void TestBattleMalformedWireAndEnumValidation()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(0x11223344, new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            BattleMixedVector[..11],
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            Append(BattleMixedVector, 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            new byte[]
            {
                0x0A, 0x00, 0, 0, 0, 0, 0, 0, 0, 0, 0
            },
            FlatPromptErrorCodeV1.UnsupportedPromptLayout);

        byte[] invalidPlayer = BattleMixedVector.ToArray();
        invalidPlayer[1] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidPlayer,
            FlatPromptErrorCodeV1.InvalidParticipant);

        byte[] invalidMode = BattleMixedVector.ToArray();
        invalidMode[24] = 3;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidMode,
            FlatPromptErrorCodeV1.InvalidClientMode);

        byte[] invalidController = BattleMixedVector.ToArray();
        invalidController[10] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidController,
            FlatPromptErrorCodeV1.InvalidParticipant);

        byte[] invalidLocation = BattleMixedVector.ToArray();
        invalidLocation[11] = 0;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidLocation,
            FlatPromptErrorCodeV1.InvalidLocation);

        byte[] invalidDirect = BattleMixedVector.ToArray();
        invalidDirect[36] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidDirect,
            FlatPromptErrorCodeV1.InvalidBoolean);

        byte[] invalidTransition = BattleMixedVector.ToArray();
        invalidTransition[45] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidTransition,
            FlatPromptErrorCodeV1.InvalidBoolean);

        byte[] countOverflow = new byte[12];
        countOverflow[0] = 10;
        BinaryPrimitives.WriteUInt32LittleEndian(
            countOverflow.AsSpan(2, 4),
            65537);
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            countOverflow,
            FlatPromptErrorCodeV1.ArithmeticFailure);
    }

    internal static void TestBattleAuthorityAtomicityStalenessOwnershipPrivacy()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(0x11223344, new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        byte[] message = BattleMessage(
            0,
            new[]
            {
                new BattleActivationSpec(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    7,
                    0)
            },
            Array.Empty<BattleAttackSpec>(),
            0,
            0);
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 accepted = session.TryAcceptPrompt(
            message,
            authority.Mirror,
            authority.Projection);
        AssertSuccess(accepted, FlatPromptFamilyV1.MsgSelectBattleCmd);
        True(session.TryCaptureSelection(
            "MSG_SELECT_BATTLECMD:ACTIVATE:0",
            out FlatPromptSelectionHandleV1? oldHandle,
            out _));
        byte[] changedCanonical = authority.Projection.CanonicalBytes.ToArray();
        changedCanonical[0] ^= 0x01;
        PublicStateProjectionResultV1 changedProjection =
            PublicStateProjectionResultV1.Success(
                authority.Projection.Snapshot!,
                changedCanonical,
                authority.Projection.Sha256!);
        AssertFailure(
            session,
            authority.Mirror,
            changedProjection,
            message,
            FlatPromptErrorCodeV1.AuthorityMismatch);
        False(session.TryResolveSelection(
            oldHandle,
            out _,
            out FlatPromptErrorCodeV1 staleError));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, staleError);

        byte[] ownedMessage = message.ToArray();
        FlatPromptProjectionResultV1 owned = new FlatPromptSessionV1()
            .TryAcceptPrompt(ownedMessage, authority.Mirror, authority.Projection);
        AssertSuccess(owned, FlatPromptFamilyV1.MsgSelectBattleCmd);
        ownedMessage[0] = 0xFF;
        ownedMessage[6] = 0xEE;
        Equal(
            "MSG_SELECT_BATTLECMD:ACTIVATE:0",
            owned.Candidates![0].I4LocalCandidateKey);
        FlatPromptSessionV1 ownerSession = new();
        ownerSession.TryAcceptPrompt(
            message,
            authority.Mirror,
            authority.Projection);
        True(ownerSession.TryCaptureSelection(
            "MSG_SELECT_BATTLECMD:ACTIVATE:0",
            out FlatPromptSelectionHandleV1? ownedHandle,
            out _));
        True(ownerSession.TryResolveSelection(
            ownedHandle,
            out FlatPromptResponseResolutionV1 ownedResponse,
            out _));
        Equal(0, ownedResponse.ResponseI32);
    }

    internal static void TestIdleExactWireAndContext()
    {
        Authority authority = CreateIdleVectorAuthority();
        FlatPromptProjectionResultV1 result = AcceptIdle(
            authority,
            IdleAllSectionsVector,
            out _);

        AssertSuccess(result, FlatPromptFamilyV1.MsgSelectIdleCmd);
        Equal((byte)1, result.Context!.ActingPlayer);
        Equal(9, result.Candidates!.Count);
        True(result.Candidates[0]
            is FlatIdleSummonCardCodePublicCandidateV1);
        True(result.Candidates[1]
            is FlatIdleSpecialSummonCardCodePublicCandidateV1);
        True(result.Candidates[2]
            is FlatIdleRepositionCardCodePublicCandidateV1);
        True(result.Candidates[3]
            is FlatIdleMsetCardCodePublicCandidateV1);
        True(result.Candidates[4]
            is FlatIdleSsetCardCodePublicCandidateV1);
        True(result.Candidates[5]
            is FlatIdleActivatableCardCodePublicCandidateV1);
        False(result.Candidates.Any(
            candidate => candidate.GetType().GetProperty(
                "DirectAttackable",
                BindingFlags.Instance | BindingFlags.Public) is not null));
    }

    internal static void TestIdleAllSectionsAndCanonicalOrder()
    {
        Authority authority = CreateIdleVectorAuthority();
        FlatPromptProjectionResultV1 result = AcceptIdle(
            authority,
            IdleAllSectionsVector,
            out _);

        AssertSuccess(result, FlatPromptFamilyV1.MsgSelectIdleCmd);
        True(
            new[]
            {
                "MSG_SELECT_IDLECMD:SUMMON:0",
                "MSG_SELECT_IDLECMD:SPECIAL_SUMMON:0",
                "MSG_SELECT_IDLECMD:REPOSITION:0",
                "MSG_SELECT_IDLECMD:MSET:0",
                "MSG_SELECT_IDLECMD:SSET:0",
                "MSG_SELECT_IDLECMD:ACTIVATE:0",
                "MSG_SELECT_IDLECMD:TO_BP",
                "MSG_SELECT_IDLECMD:TO_EP",
                "MSG_SELECT_IDLECMD:SHUFFLE_HAND"
            }.SequenceEqual(
                result.Candidates!.Select(candidate => candidate.I4LocalCandidateKey)));
        Equal(
            FlatPromptSourceSectionV1.Summon,
            ((FlatIdleSummonPublicCandidateBaseV1)result.Candidates![0])
                .SourceSection);
        Equal(
            FlatPromptSourceSectionV1.SpecialSummon,
            ((FlatIdleSpecialSummonPublicCandidateBaseV1)result.Candidates[1])
                .SourceSection);
        Equal(
            FlatPromptSourceSectionV1.Reposition,
            ((FlatIdleRepositionPublicCandidateBaseV1)result.Candidates[2])
                .SourceSection);
        Equal(
            FlatPromptSourceSectionV1.Mset,
            ((FlatIdleMsetPublicCandidateBaseV1)result.Candidates[3])
                .SourceSection);
        Equal(
            FlatPromptSourceSectionV1.Sset,
            ((FlatIdleSsetPublicCandidateBaseV1)result.Candidates[4])
                .SourceSection);
        Equal(
            FlatPromptSourceSectionV1.Activate,
            ((FlatIdleActivatablePublicCandidateBaseV1)result.Candidates[5])
                .SourceSection);
        Equal(
            FlatPromptChoiceKindV1.ToBp,
            result.Candidates[6].ChoiceKind);
        Equal(
            FlatPromptChoiceKindV1.ToEp,
            result.Candidates[7].ChoiceKind);
        Equal(
            FlatPromptChoiceKindV1.ShuffleHand,
            result.Candidates[8].ChoiceKind);
    }

    internal static void TestIdlePerSectionResponseBindings()
    {
        Authority authority = CreateIdleVectorAuthority();
        FlatPromptSessionV1 session = new();
        AssertSuccess(
            session.TryAcceptPrompt(
                IdleAllSectionsVector,
                authority.Mirror,
                authority.Projection),
            FlatPromptFamilyV1.MsgSelectIdleCmd);
        AssertResponse(session, "MSG_SELECT_IDLECMD:SUMMON:0", 0);
        AssertResponse(session, "MSG_SELECT_IDLECMD:SPECIAL_SUMMON:0", 1);
        AssertResponse(session, "MSG_SELECT_IDLECMD:REPOSITION:0", 2);
        AssertResponse(session, "MSG_SELECT_IDLECMD:MSET:0", 3);
        AssertResponse(session, "MSG_SELECT_IDLECMD:SSET:0", 4);
        AssertResponse(session, "MSG_SELECT_IDLECMD:ACTIVATE:0", 5);
        AssertResponse(session, "MSG_SELECT_IDLECMD:TO_BP", 6);
        AssertResponse(session, "MSG_SELECT_IDLECMD:TO_EP", 7);
        AssertResponse(session, "MSG_SELECT_IDLECMD:SHUFFLE_HAND", 8);

        FlatPromptSelectionHandleV1? boundaryHandle = null;
        PublicSemanticLocatorV1.TryCreateIndexed(
            0,
            PublicSemanticZoneV1.MonsterZone,
            0,
            out PublicSemanticLocatorV1? boundaryLocator);
        NotNull(boundaryLocator);
        FlatPublicCandidateDescriptorV1 boundaryCandidate =
            new FlatIdleActivatablePublicCandidateV1(
                "MSG_SELECT_IDLECMD:ACTIVATE:65535",
                65535,
                boundaryLocator!,
                1,
                0);
        True(CurrentFlatPromptBindingV1.TryCreate(
            0,
            FlatPromptFamilyV1.MsgSelectIdleCmd,
            new[] { boundaryCandidate },
            new[] { "MSG_SELECT_IDLECMD:ACTIVATE:65535" },
            new[] { unchecked((int)0xFFFF0005) },
            out CurrentFlatPromptBindingV1? boundaryBinding,
            out FlatPromptErrorCodeV1 boundaryError));
        NotNull(boundaryBinding);
        Equal(FlatPromptErrorCodeV1.None, boundaryError);
        _ = boundaryHandle;
    }

    internal static void TestIdleTransitionFlagsAndZeroDomain()
    {
        Authority authority = CreateAuthority(0, 0);
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            IdleMessage(
                0,
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleActivationSpec>(),
                0,
                0,
                0),
            FlatPromptErrorCodeV1.ZeroOptionDomain);

        FlatPromptProjectionResultV1 onlyBattle = AcceptIdle(
            authority,
            IdleMessage(
                0,
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleActivationSpec>(),
                1,
                0,
                0),
            out _);
        AssertSuccess(onlyBattle, FlatPromptFamilyV1.MsgSelectIdleCmd);
        Equal(1, onlyBattle.Candidates!.Count);
        True(onlyBattle.Candidates[0]
            is FlatIdleToBattlePhasePublicCandidateV1);

        FlatPromptProjectionResultV1 onlyShuffle = AcceptIdle(
            authority,
            IdleMessage(
                0,
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleActivationSpec>(),
                0,
                0,
                1),
            out _);
        AssertSuccess(onlyShuffle, FlatPromptFamilyV1.MsgSelectIdleCmd);
        True(onlyShuffle.Candidates![0]
            is FlatIdleShuffleHandPublicCandidateV1);
    }

    internal static void TestIdleIndexedAndPileCorrelation()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(0x11111111, new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new CardSpec(0x22222222, new ModernLocInfoV1(0, 0x02, 0, 0x08)),
            new CardSpec(0x33333333, new ModernLocInfoV1(0, 0x40, 0, 0x08)));
        FlatPromptProjectionResultV1 result = AcceptIdle(
            authority,
            IdleMessage(
                0,
                new[]
                {
                    new IdleSimpleSpec(
                        0x22222222,
                        new ModernLocInfoV1(0, 0x02, 0, 0))
                },
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                new[]
                {
                    new IdleSimpleSpec(
                        0x33333333,
                        new ModernLocInfoV1(0, 0x40, 0, 0))
                },
                Array.Empty<IdleActivationSpec>(),
                0,
                0,
                0),
            out _);
        AssertSuccess(result, FlatPromptFamilyV1.MsgSelectIdleCmd);
        Equal(
            "p0:HAND:public:572662306:0",
            ((FlatIdleSummonPublicCandidateBaseV1)result.Candidates![0])
                .PublicSemanticCardLocator.Value);
        Equal(
            "p0:EXTRA_DECK:public:858993459:0",
            ((FlatIdleSsetPublicCandidateBaseV1)result.Candidates[1])
                .PublicSemanticCardLocator.Value);

        Authority mainDeck = CreateAuthority(
            0,
            0,
            new CardSpec(0x44444444, new ModernLocInfoV1(0, 0x01, 0, 0x01)));
        AssertFailureResult(
            AcceptIdle(
                mainDeck,
                IdleMessage(
                    0,
                    new[]
                    {
                        new IdleSimpleSpec(
                            0x44444444,
                            new ModernLocInfoV1(0, 0x01, 0, 0))
                    },
                    Array.Empty<IdleSimpleSpec>(),
                    Array.Empty<IdleSimpleSpec>(),
                    Array.Empty<IdleSimpleSpec>(),
                    Array.Empty<IdleSimpleSpec>(),
                    Array.Empty<IdleActivationSpec>(),
                    0,
                    0,
                    0),
                out _),
            FlatPromptErrorCodeV1.UnprovenPublicReference);
    }

    internal static void TestIdleCardCodeSafetyAndDuplicateAmbiguity()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(0x11223344, new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        FlatPromptProjectionResultV1 unsafeCode = AcceptIdle(
            authority,
            IdleMessage(
                0,
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                new[]
                {
                    new IdleActivationSpec(
                        0,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        7,
                        0)
                },
                0,
                0,
                0),
            out _);
        AssertSuccess(unsafeCode, FlatPromptFamilyV1.MsgSelectIdleCmd);
        False(unsafeCode.Candidates![0]
            is FlatIdleActivatableCardCodePublicCandidateV1);

        Authority duplicateHand = CreateAuthority(
            0,
            0,
            new CardSpec(0x55667788, new ModernLocInfoV1(0, 0x02, 0, 0x08)),
            new CardSpec(0x55667788, new ModernLocInfoV1(0, 0x02, 1, 0x08)));
        FlatPromptProjectionResultV1 ambiguous = AcceptIdle(
            duplicateHand,
            IdleMessage(
                0,
                new[]
                {
                    new IdleSimpleSpec(
                        0x55667788,
                        new ModernLocInfoV1(0, 0x02, 0, 0))
                },
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleSimpleSpec>(),
                Array.Empty<IdleActivationSpec>(),
                0,
                0,
                0),
            out _);
        AssertFailureResult(
            ambiguous,
            FlatPromptErrorCodeV1.UnprovenPublicReference);
    }

    internal static void TestIdleMalformedWireAndEnumValidation()
    {
        Authority authority = CreateIdleVectorAuthority();
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            IdleAllSectionsVector[..28],
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            Append(IdleAllSectionsVector, 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            new byte[]
            {
                0x0B, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00
            },
            FlatPromptErrorCodeV1.UnsupportedPromptLayout);

        byte[] invalidPlayer = IdleAllSectionsVector.ToArray();
        invalidPlayer[1] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidPlayer,
            FlatPromptErrorCodeV1.InvalidParticipant);

        byte[] invalidMode = IdleAllSectionsVector.ToArray();
        invalidMode[^4] = 3;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidMode,
            FlatPromptErrorCodeV1.InvalidClientMode);

        byte[] invalidController = IdleAllSectionsVector.ToArray();
        invalidController[10] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidController,
            FlatPromptErrorCodeV1.InvalidParticipant);

        byte[] invalidLocation = IdleAllSectionsVector.ToArray();
        invalidLocation[11] = 0;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidLocation,
            FlatPromptErrorCodeV1.InvalidLocation);

        byte[] invalidTransition = IdleAllSectionsVector.ToArray();
        invalidTransition[^3] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidTransition,
            FlatPromptErrorCodeV1.InvalidBoolean);

        byte[] countOverflow = new byte[29];
        countOverflow[0] = 11;
        BinaryPrimitives.WriteUInt32LittleEndian(
            countOverflow.AsSpan(2, 4),
            65537);
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            countOverflow,
            FlatPromptErrorCodeV1.ArithmeticFailure);
    }

    internal static void TestIdleAuthorityAtomicityStalenessOwnershipPrivacy()
    {
        Authority authority = CreateIdleVectorAuthority();
        byte[] source = IdleAllSectionsVector.ToArray();
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 accepted = session.TryAcceptPrompt(
            source,
            authority.Mirror,
            authority.Projection);
        AssertSuccess(accepted, FlatPromptFamilyV1.MsgSelectIdleCmd);
        True(session.TryCaptureSelection(
            "MSG_SELECT_IDLECMD:ACTIVATE:0",
            out FlatPromptSelectionHandleV1? oldHandle,
            out _));
        source[0] = 0xFF;
        source[2] = 0xAA;
        Equal(
            "MSG_SELECT_IDLECMD:SUMMON:0",
            accepted.Candidates![0].I4LocalCandidateKey);
        True(session.TryResolveSelection(
            oldHandle,
            out FlatPromptResponseResolutionV1 response,
            out _));
        Equal(5, response.ResponseI32);

        byte[] changedCanonical = authority.Projection.CanonicalBytes.ToArray();
        changedCanonical[0] ^= 0x01;
        PublicStateProjectionResultV1 changedProjection =
            PublicStateProjectionResultV1.Success(
                authority.Projection.Snapshot!,
                changedCanonical,
                authority.Projection.Sha256!);
        AssertFailure(
            session,
            authority.Mirror,
            changedProjection,
            IdleAllSectionsVector,
            FlatPromptErrorCodeV1.AuthorityMismatch);
        False(session.TryResolveSelection(
            oldHandle,
            out _,
            out FlatPromptErrorCodeV1 staleError));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, staleError);
    }

    internal static void TestI4CPublicPrivateBoundary()
    {
        Type[] publicTypes =
        {
            typeof(FlatBattleActivatablePublicCandidateBaseV1),
            typeof(FlatBattleActivatablePublicCandidateV1),
            typeof(FlatBattleActivatableCardCodePublicCandidateV1),
            typeof(FlatBattleAttackPublicCandidateBaseV1),
            typeof(FlatBattleAttackPublicCandidateV1),
            typeof(FlatBattleAttackCardCodePublicCandidateV1),
            typeof(FlatBattleToMainPhase2PublicCandidateV1),
            typeof(FlatBattleToEndPhasePublicCandidateV1),
            typeof(FlatIdleSummonPublicCandidateBaseV1),
            typeof(FlatIdleSummonPublicCandidateV1),
            typeof(FlatIdleSummonCardCodePublicCandidateV1),
            typeof(FlatIdleSpecialSummonPublicCandidateBaseV1),
            typeof(FlatIdleSpecialSummonPublicCandidateV1),
            typeof(FlatIdleSpecialSummonCardCodePublicCandidateV1),
            typeof(FlatIdleRepositionPublicCandidateBaseV1),
            typeof(FlatIdleRepositionPublicCandidateV1),
            typeof(FlatIdleRepositionCardCodePublicCandidateV1),
            typeof(FlatIdleMsetPublicCandidateBaseV1),
            typeof(FlatIdleMsetPublicCandidateV1),
            typeof(FlatIdleMsetCardCodePublicCandidateV1),
            typeof(FlatIdleSsetPublicCandidateBaseV1),
            typeof(FlatIdleSsetPublicCandidateV1),
            typeof(FlatIdleSsetCardCodePublicCandidateV1),
            typeof(FlatIdleActivatablePublicCandidateBaseV1),
            typeof(FlatIdleActivatablePublicCandidateV1),
            typeof(FlatIdleActivatableCardCodePublicCandidateV1),
            typeof(FlatIdleToBattlePhasePublicCandidateV1),
            typeof(FlatIdleToEndPhasePublicCandidateV1),
            typeof(FlatIdleShuffleHandPublicCandidateV1)
        };
        string[] forbidden =
        {
            "ResponseI32", "ResponseBody", "ModernLocInfo", "MirrorEntityId",
            "MirrorSnapshot", "ProtocolOffset", "Socket", "Network",
            "Timestamp", "Pid", "PromptInstanceOrdinal", "PublicActionKey"
        };
        foreach (Type type in publicTypes)
        {
            True(type.IsPublic, "expected public type " + type.Name);
            True(type.IsAbstract || type.IsSealed,
                "expected closed type " + type.Name);
            foreach (PropertyInfo property in type.GetProperties())
            {
                False(
                    forbidden.Contains(property.Name, StringComparer.Ordinal),
                    "private property exposed by " + type.Name + "." +
                    property.Name);
            }
        }
        False(typeof(FlatPromptProjectionV1).IsPublic);
        False(typeof(FlatPromptCardCorrelationV1).IsPublic);
        False(typeof(FlatPromptSessionV1).GetMethods().Any(
            method => method.IsPublic &&
                method.Name.Contains("Send", StringComparison.OrdinalIgnoreCase)));
    }

    internal static void TestI3I4AI4BRegressionBoundary()
    {
        I4AFlatPromptProjectionTests.TestYesNoExactDomain();
        I4AFlatPromptProjectionTests.TestExactResponseBindings();
        I4BEffectYnChainPromptTests.TestEffectYnExactWireAndContext();
        I4BEffectYnChainPromptTests.TestChainOptionalWireContextAndNoChain();

        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(0x11223344, new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        PublicStateProjectionResultV1 reprojection =
            PublicStateProjectionV1.TryProject(
                authority.Mirror.Snapshot,
                new PublicStateProjectionContextV1(
                    authority.Projection.Snapshot!.DuelFlags));
        True(reprojection.IsSuccess, reprojection.Error.ToString());
        BytesEqual(
            authority.Projection.CanonicalBytes.Span,
            reprojection.CanonicalBytes.Span);
        Equal(
            authority.Projection.PublicProjectionId,
            reprojection.PublicProjectionId);
    }

    private static Authority CreateIdleVectorAuthority()
    {
        return CreateAuthority(
            1,
            0,
            new CardSpec(
                0x01020304,
                new ModernLocInfoV1(1, 0x02, 0, 0x08)),
            new CardSpec(
                0x05060708,
                new ModernLocInfoV1(1, 0x02, 1, 0x08)),
            new CardSpec(
                0x31323334,
                new ModernLocInfoV1(1, 0x02, 2, 0x08)),
            new CardSpec(
                0x0D0E0F10,
                new ModernLocInfoV1(1, 0x02, 3, 0x08)),
            new CardSpec(
                0x11121314,
                new ModernLocInfoV1(1, 0x02, 4, 0x08)),
            new CardSpec(
                0x41424344,
                new ModernLocInfoV1(1, 0x04, 0, 0x05)),
            new CardSpec(
                0x45464748,
                new ModernLocInfoV1(1, 0x04, 1, 0x05)),
            new CardSpec(
                0x090A0B0C,
                new ModernLocInfoV1(1, 0x04, 2, 0x05)),
            new CardSpec(
                0x51525354,
                new ModernLocInfoV1(1, 0x04, 3, 0x05)),
            new CardSpec(
                0x55565758,
                new ModernLocInfoV1(1, 0x04, 4, 0x05)),
            new CardSpec(
                0x15161718,
                new ModernLocInfoV1(1, 0x04, 5, 0x05)),
            new CardSpec(
                0x21222324,
                new ModernLocInfoV1(1, 0x04, 6, 0x05)));
    }

    private static Authority CreateAuthority(
        byte playerType,
        ulong duelFlags,
        params CardSpec[] cards)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                playerType,
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
                new PublicStateProjectionContextV1(duelFlags));
        True(projection.IsSuccess, projection.Error.ToString());
        return new Authority(mirror, decoder, projection);
    }

    private static FlatPromptProjectionResultV1 AcceptBattle(
        Authority authority,
        byte[] message) =>
        new FlatPromptSessionV1().TryAcceptPrompt(
            message,
            authority.Mirror,
            authority.Projection);

    private static FlatPromptProjectionResultV1 AcceptIdle(
        Authority authority,
        byte[] message,
        out FlatPromptSessionV1 session)
    {
        session = new FlatPromptSessionV1();
        return session.TryAcceptPrompt(
            message,
            authority.Mirror,
            authority.Projection);
    }

    private static byte[] BattleMessage(
        byte player,
        IReadOnlyList<BattleActivationSpec> activatable,
        IReadOnlyList<BattleAttackSpec> attackable,
        byte toMainPhase2,
        byte toEndPhase)
    {
        List<byte[]> parts = new()
        {
            new[] { (byte)10, player },
            U32((uint)activatable.Count)
        };
        parts.AddRange(activatable.Select(entry => Join(
            U32(entry.CardCode),
            new[] { entry.Location.Controller, entry.Location.Location },
            U32(entry.Location.Sequence),
            U64(entry.Description),
            new[] { entry.ClientMode })));
        parts.Add(U32((uint)attackable.Count));
        parts.AddRange(attackable.Select(entry => Join(
            U32(entry.CardCode),
            new[] { entry.Location.Controller, entry.Location.Location },
            new[]
            {
                checked((byte)entry.Location.Sequence),
                entry.IsDirectAttackable ? (byte)1 : (byte)0
            })));
        parts.Add(new[] { toMainPhase2, toEndPhase });
        return Join(parts.ToArray());
    }

    private static byte[] IdleMessage(
        byte player,
        IReadOnlyList<IdleSimpleSpec> summon,
        IReadOnlyList<IdleSimpleSpec> specialSummon,
        IReadOnlyList<IdleSimpleSpec> reposition,
        IReadOnlyList<IdleSimpleSpec> mset,
        IReadOnlyList<IdleSimpleSpec> sset,
        IReadOnlyList<IdleActivationSpec> activatable,
        byte toBattlePhase,
        byte toEndPhase,
        byte shuffleHand)
    {
        List<byte[]> parts = new()
        {
            new[] { (byte)11, player }
        };
        AddIdleSimpleSection(parts, summon, wideSequence: true);
        AddIdleSimpleSection(parts, specialSummon, wideSequence: true);
        AddIdleSimpleSection(parts, reposition, wideSequence: false);
        AddIdleSimpleSection(parts, mset, wideSequence: true);
        AddIdleSimpleSection(parts, sset, wideSequence: true);
        parts.Add(U32((uint)activatable.Count));
        parts.AddRange(activatable.Select(entry => Join(
            U32(entry.CardCode),
            new[] { entry.Location.Controller, entry.Location.Location },
            U32(entry.Location.Sequence),
            U64(entry.Description),
            new[] { entry.ClientMode })));
        parts.Add(new[] { toBattlePhase, toEndPhase, shuffleHand });
        return Join(parts.ToArray());
    }

    private static void AddIdleSimpleSection(
        List<byte[]> parts,
        IReadOnlyList<IdleSimpleSpec> entries,
        bool wideSequence)
    {
        parts.Add(U32((uint)entries.Count));
        foreach (IdleSimpleSpec entry in entries)
        {
            parts.Add(Join(
                U32(entry.CardCode),
                new[] { entry.Location.Controller, entry.Location.Location },
                wideSequence
                    ? U32(entry.Location.Sequence)
                    : new[] { checked((byte)entry.Location.Sequence) }));
        }
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

    private static void AssertFailure(
        FlatPromptSessionV1 session,
        Authority authority,
        ReadOnlySpan<byte> message,
        FlatPromptErrorCodeV1 expectedError)
    {
        AssertFailureResult(
            session.TryAcceptPrompt(
                message,
                authority.Mirror,
                authority.Projection),
            expectedError);
    }

    private static void AssertFailure(
        FlatPromptSessionV1 session,
        PerspectiveStateMirrorV1? mirror,
        PublicStateProjectionResultV1? projection,
        ReadOnlySpan<byte> message,
        FlatPromptErrorCodeV1 expectedError)
    {
        AssertFailureResult(
            session.TryAcceptPrompt(message, mirror, projection),
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

    private static void AssertResponse(
        FlatPromptSessionV1 session,
        string key,
        int expected)
    {
        True(session.TryCaptureSelection(
            key,
            out FlatPromptSelectionHandleV1? handle,
            out FlatPromptErrorCodeV1 captureError),
            captureError.ToString());
        NotNull(handle);
        True(session.TryResolveSelection(
            handle,
            out FlatPromptResponseResolutionV1 response,
            out FlatPromptErrorCodeV1 resolveError),
            resolveError.ToString());
        Equal(expected, response.ResponseI32);
    }

    private static PublicStateSnapshotV1 WithCards(
        PublicStateSnapshotV1 source,
        IEnumerable<PublicCardStateV1> cards) =>
        new(
            source.PerspectivePlayer,
            source.DuelFlags,
            source.TurnCount,
            source.TurnPlayer,
            source.Phase,
            source.Terminal,
            source.Participants,
            cards);

    private static byte[] Append(byte[] source, byte value) =>
        source.Concat(new[] { value }).ToArray();

    private readonly record struct Authority(
        PerspectiveStateMirrorV1 Mirror,
        GameplayMessageDecoderV1 Decoder,
        PublicStateProjectionResultV1 Projection);

    private readonly record struct CardSpec(
        uint CardCode,
        ModernLocInfoV1 Location);

    private readonly record struct BattleActivationSpec(
        uint CardCode,
        ModernLocInfoV1 Location,
        ulong Description,
        byte ClientMode);

    private readonly record struct BattleAttackSpec(
        uint CardCode,
        ModernLocInfoV1 Location,
        bool IsDirectAttackable);

    private readonly record struct IdleSimpleSpec(
        uint CardCode,
        ModernLocInfoV1 Location);

    private readonly record struct IdleActivationSpec(
        uint CardCode,
        ModernLocInfoV1 Location,
        ulong Description,
        byte ClientMode);
}
