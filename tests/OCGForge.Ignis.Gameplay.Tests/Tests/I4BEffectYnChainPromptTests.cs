using System.Buffers.Binary;
using System.Reflection;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.ModernQueryFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I4BEffectYnChainPromptTests
{
    private const ulong PendulumDuelFlag = 0x800;
    private const uint PendulumSpellType = 0x01000002;

    internal static void TestEffectYnExactWireAndContext()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(0x01000001, new ModernLocInfoV1(0, 0x02, 0, 0x08)),
            new CardSpec(0x01000002, new ModernLocInfoV1(0, 0x02, 1, 0x08)),
            new CardSpec(0x01000003, new ModernLocInfoV1(0, 0x02, 2, 0x08)),
            new CardSpec(0x12345678, new ModernLocInfoV1(0, 0x02, 3, 0x0A)));
        byte[] message = EffectYnMessage(
            0,
            0x12345678,
            new ModernLocInfoV1(0, 0x02, 3, 0x0A),
            0x1122334455667788);

        Equal(24, message.Length);
        FlatPromptProjectionResultV1 result =
            new FlatPromptSessionV1().TryAcceptPrompt(
                message,
                authority.Mirror,
                authority.Projection);

        AssertSuccess(result, FlatPromptFamilyV1.MsgSelectEffectYn);
        FlatPromptEffectYnCardCodePublicContextV1 context =
            result.Context as FlatPromptEffectYnCardCodePublicContextV1 ??
            throw new InvalidOperationException("expected EFFECTYN card-code context");
        Equal((byte)0, context.ActingPlayer);
        Equal(
            "p0:HAND:public:305419896:0",
            context.EffectCardLocator.Value);
        Equal(0x12345678u, context.EffectCardCode);
        Equal(0x1122334455667788ul, context.EffectDescriptionId);
        Equal(2, result.Candidates!.Count);
    }

    internal static void TestEffectYnDomainOrderAndResponses()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x10203040,
                new ModernLocInfoV1(0, 0x04, 2, 0x05)));
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 result = session.TryAcceptPrompt(
            EffectYnMessage(
                0,
                0x10203040,
                new ModernLocInfoV1(0, 0x04, 2, 0x05),
                7),
            authority.Mirror,
            authority.Projection);

        AssertSuccess(result, FlatPromptFamilyV1.MsgSelectEffectYn);
        True(result.Context is FlatPromptEffectYnCardCodePublicContextV1);
        FlatEffectYnPublicCandidateDescriptorV1 no =
            result.Candidates![0] as FlatEffectYnPublicCandidateDescriptorV1 ??
            throw new InvalidOperationException("expected EFFECTYN NO candidate");
        FlatEffectYnPublicCandidateDescriptorV1 yes =
            result.Candidates[1] as FlatEffectYnPublicCandidateDescriptorV1 ??
            throw new InvalidOperationException("expected EFFECTYN YES candidate");
        Equal("MSG_SELECT_EFFECTYN:NO", no.I4LocalCandidateKey);
        Equal(FlatPromptChoiceKindV1.No, no.ChoiceKind);
        Equal("MSG_SELECT_EFFECTYN:YES", yes.I4LocalCandidateKey);
        Equal(FlatPromptChoiceKindV1.Yes, yes.ChoiceKind);
        AssertResponse(session, no.I4LocalCandidateKey, 0);
        AssertResponse(session, yes.I4LocalCandidateKey, 1);
    }

    internal static void TestEffectYnMalformedWireFailures()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        FlatPromptSessionV1 session = new();
        byte[] valid = EffectYnMessage(
            0,
            0x11223344,
            new ModernLocInfoV1(0, 0x04, 0, 0x05),
            1);

        AssertFailure(session, authority, valid[..23], FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            session,
            authority,
            Append(valid, 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);

        byte[] invalidPlayer = valid.ToArray();
        invalidPlayer[1] = 2;
        AssertFailure(
            session,
            authority,
            invalidPlayer,
            FlatPromptErrorCodeV1.InvalidParticipant);

        byte[] invalidController = valid.ToArray();
        invalidController[6] = 2;
        AssertFailure(
            session,
            authority,
            invalidController,
            FlatPromptErrorCodeV1.InvalidParticipant);

        byte[] invalidLocation = valid.ToArray();
        invalidLocation[7] = 0;
        AssertFailure(
            session,
            authority,
            invalidLocation,
            FlatPromptErrorCodeV1.InvalidLocation);
    }

    internal static void TestEffectYnAuthorityValidationFailures()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        byte[] message = EffectYnMessage(
            0,
            0x11223344,
            new ModernLocInfoV1(0, 0x04, 0, 0x05),
            1);

        AssertFailure(
            new FlatPromptSessionV1(),
            authority.Mirror,
            null,
            FlatPromptErrorCodeV1.UnprovenPublicReference);
        AssertFailure(
            new FlatPromptSessionV1(),
            null,
            authority.Projection,
            FlatPromptErrorCodeV1.UnprovenPublicReference);
        AssertFailure(
            new FlatPromptSessionV1(),
            authority.Mirror,
            PublicStateProjectionResultV1.Failure(
                PublicStateProjectionErrorV1.InvalidSnapshot),
            FlatPromptErrorCodeV1.UnprovenPublicReference);

        byte[] changedCanonical = authority.Projection.CanonicalBytes.ToArray();
        changedCanonical[0] ^= 0x01;
        PublicStateProjectionResultV1 changedProjection =
            PublicStateProjectionResultV1.Success(
                authority.Projection.Snapshot!,
                changedCanonical,
                authority.Projection.Sha256!);
        AssertFailure(
            new FlatPromptSessionV1(),
            authority.Mirror,
            changedProjection,
            FlatPromptErrorCodeV1.AuthorityMismatch);

        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 accepted = session.TryAcceptPrompt(
            message,
            authority.Mirror,
            authority.Projection);
        AssertSuccess(accepted, FlatPromptFamilyV1.MsgSelectEffectYn);
        True(session.TryCaptureSelection(
            "MSG_SELECT_EFFECTYN:YES",
            out FlatPromptSelectionHandleV1? oldHandle,
            out FlatPromptErrorCodeV1 captureError));
        Equal(FlatPromptErrorCodeV1.None, captureError);
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
        AssertSuccess(
            session.TryAcceptPrompt(
                message,
                authority.Mirror,
                authority.Projection),
            FlatPromptFamilyV1.MsgSelectEffectYn);
        Equal(
            1ul,
            CaptureOrdinal(session, "MSG_SELECT_EFFECTYN:YES"));
    }

    internal static void TestEffectYnIndexedCorrelation()
    {
        (MirrorZoneV1 MirrorZone, PublicSemanticZoneV1 PublicZone)[] allowed =
        {
            (MirrorZoneV1.MonsterZone, PublicSemanticZoneV1.MonsterZone),
            (MirrorZoneV1.Graveyard, PublicSemanticZoneV1.Graveyard),
            (MirrorZoneV1.Banished, PublicSemanticZoneV1.Banished),
            (MirrorZoneV1.SpellTrapZone, PublicSemanticZoneV1.SpellTrapZone),
            (MirrorZoneV1.SpellTrapZone, PublicSemanticZoneV1.FieldZone),
            (MirrorZoneV1.SpellTrapZone,
                PublicSemanticZoneV1.PendulumRelevantState)
        };

        foreach ((MirrorZoneV1 mirrorZone, PublicSemanticZoneV1 publicZone)
                 in allowed)
        {
            AssertIndexedPair(mirrorZone, publicZone, true);
        }

        PublicSemanticZoneV1[] publicZones =
        {
            PublicSemanticZoneV1.MonsterZone,
            PublicSemanticZoneV1.SpellTrapZone,
            PublicSemanticZoneV1.FieldZone,
            PublicSemanticZoneV1.PendulumRelevantState,
            PublicSemanticZoneV1.Graveyard,
            PublicSemanticZoneV1.Banished
        };
        MirrorZoneV1[] mirrorZones =
        {
            MirrorZoneV1.MonsterZone,
            MirrorZoneV1.Graveyard,
            MirrorZoneV1.Banished,
            MirrorZoneV1.SpellTrapZone
        };
        foreach (MirrorZoneV1 mirrorZone in mirrorZones)
        {
            foreach (PublicSemanticZoneV1 publicZone in publicZones)
            {
                if (!IsAllowedPair(mirrorZone, publicZone))
                {
                    AssertIndexedPair(mirrorZone, publicZone, false);
                }
            }
        }
    }

    internal static void TestEffectYnPileAndOverlayCorrelation()
    {
        Authority hand = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x11112222,
                new ModernLocInfoV1(0, 0x02, 0, 0x08)));
        FlatPromptEffectYnPublicContextBaseV1 handContext =
            AcceptEffect(
                hand,
                0x11112222,
                new ModernLocInfoV1(0, 0x02, 0, 0x08))
                .Context as FlatPromptEffectYnPublicContextBaseV1 ??
            throw new InvalidOperationException("expected hand context");
        Equal(
            "p0:HAND:public:286335522:0",
            handContext.EffectCardLocator.Value);

        Authority extra = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x33334444,
                new ModernLocInfoV1(0, 0x40, 0, 0x08)));
        FlatPromptEffectYnPublicContextBaseV1 extraContext =
            AcceptEffect(
                extra,
                0x33334444,
                new ModernLocInfoV1(0, 0x40, 0, 0x08))
                .Context as FlatPromptEffectYnPublicContextBaseV1 ??
            throw new InvalidOperationException("expected extra context");
        Equal(
            "p0:EXTRA_DECK:public:858997828:0",
            extraContext.EffectCardLocator.Value);

        Authority overlay = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x01010101,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new CardSpec(
                0x02020202,
                new ModernLocInfoV1(0, 0x84, 0, 0)));
        FlatPromptEffectYnPublicContextBaseV1 overlayContext =
            AcceptEffect(
                overlay,
                0x02020202,
                new ModernLocInfoV1(0, 0x84, 0, 0))
                .Context as FlatPromptEffectYnPublicContextBaseV1 ??
            throw new InvalidOperationException("expected overlay context");
        Equal("p0:OVERLAY:0:0", overlayContext.EffectCardLocator.Value);

        Authority mainDeck = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x03030303,
                new ModernLocInfoV1(0, 0x01, 0, 0x01)));
        FlatPromptProjectionResultV1 mainDeckResult = AcceptEffect(
            mainDeck,
            0x03030303,
            new ModernLocInfoV1(0, 0x01, 0, 0x01));
        AssertFailureResult(
            mainDeckResult,
            FlatPromptErrorCodeV1.UnprovenPublicReference);
    }

    internal static void TestEffectYnCardCodeSafetyAndAmbiguity()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        FlatPromptProjectionResultV1 safe = AcceptEffect(
            authority,
            0x11223344,
            new ModernLocInfoV1(0, 0x04, 0, 0x05));
        True(safe.Context is FlatPromptEffectYnCardCodePublicContextV1);

        FlatPromptProjectionResultV1 zeroSource = AcceptEffect(
            authority,
            0,
            new ModernLocInfoV1(0, 0x04, 0, 0x05));
        True(zeroSource.Context is FlatPromptEffectYnPublicContextV1);
        False(zeroSource.Context is FlatPromptEffectYnCardCodePublicContextV1);

        FlatPromptProjectionResultV1 mismatchingSource = AcceptEffect(
            authority,
            0x55667788,
            new ModernLocInfoV1(0, 0x04, 0, 0x05));
        True(mismatchingSource.Context is FlatPromptEffectYnPublicContextV1);
        False(mismatchingSource.Context is FlatPromptEffectYnCardCodePublicContextV1);

        PublicCardStateV1 card = authority.Projection.Snapshot!.Cards.Single();
        PublicStateSnapshotV1 ambiguousSnapshot = WithCards(
            authority.Projection.Snapshot!,
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
            new ModernLocInfoV1(0, 0x04, 0, 0x05),
            out FlatPromptCardCorrelationResultV1? correlation,
            out FlatPromptErrorCodeV1 correlationError));
        Null(correlation);
        Equal(
            FlatPromptErrorCodeV1.UnprovenPublicReference,
            correlationError);
    }

    internal static void TestEffectYnPrivacyAndStaleness()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 first = session.TryAcceptPrompt(
            EffectYnMessage(
                0,
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05),
                1),
            authority.Mirror,
            authority.Projection);
        AssertSuccess(first, FlatPromptFamilyV1.MsgSelectEffectYn);
        True(session.TryCaptureSelection(
            "MSG_SELECT_EFFECTYN:YES",
            out FlatPromptSelectionHandleV1? oldHandle,
            out _));
        AssertSuccess(
            session.TryAcceptPrompt(
                EffectYnMessage(
                    0,
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0x05),
                    1),
                authority.Mirror,
                authority.Projection),
            FlatPromptFamilyV1.MsgSelectEffectYn);
        False(session.TryResolveSelection(
            oldHandle,
            out _,
            out FlatPromptErrorCodeV1 staleError));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, staleError);

        AssertPublicTypesDoNotExposePrivateData(
            typeof(FlatPromptEffectYnPublicContextBaseV1),
            typeof(FlatPromptEffectYnPublicContextV1),
            typeof(FlatPromptEffectYnCardCodePublicContextV1),
            typeof(FlatEffectYnPublicCandidateDescriptorV1));
    }

    internal static void TestChainOptionalWireContextAndNoChain()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x01020304,
                new ModernLocInfoV1(0, 0x04, 2, 0x05)),
            new CardSpec(
                0xAABBCCDD,
                new ModernLocInfoV1(0, 0x10, 0, 0)));
        byte[] message = ChainMessage(
            0,
            2,
            false,
            0x01020304,
            0x05060708,
            new ChainEntrySpec(
                0x01020304,
                new ModernLocInfoV1(0, 0x04, 2, 0x05),
                0x1122334455667788,
                0),
            new ChainEntrySpec(
                0xAABBCCDD,
                new ModernLocInfoV1(0, 0x10, 0, 0),
                0x99AABBCCDDEEFF00,
                2));
        Equal(62, message.Length);
        FlatPromptProjectionResultV1 result = AcceptChain(
            authority,
            message,
            out FlatPromptSessionV1 session);

        AssertSuccess(result, FlatPromptFamilyV1.MsgSelectChain);
        FlatPromptChainPublicContextV1 context =
            result.Context as FlatPromptChainPublicContextV1 ??
            throw new InvalidOperationException("expected CHAIN context");
        Equal((byte)2, context.ChainSpeCount);
        False(context.ChainForced);
        Equal(0x01020304u, context.ChainHintTimingForPlayer);
        Equal(0x05060708u, context.ChainHintTimingForOtherPlayer);
        Equal(3, result.Candidates!.Count);
        True(result.Candidates[0] is FlatChainCardCodePublicCandidateDescriptorV1);
        True(result.Candidates[1] is FlatChainCardCodePublicCandidateDescriptorV1);
        True(result.Candidates[2] is FlatChainNoChainPublicCandidateDescriptorV1);
        AssertResponse(session, "MSG_SELECT_CHAIN:CHAIN_ENTRY:0", 0);
        AssertResponse(session, "MSG_SELECT_CHAIN:CHAIN_ENTRY:1", 1);
        AssertResponse(session, "MSG_SELECT_CHAIN:NO_CHAIN", -1);
    }

    internal static void TestChainForcedMarkerAndSingleEntry()
    {
        Authority authority = CreateAuthority(
            1,
            1,
            new CardSpec(
                0x0A0B0C0D,
                new ModernLocInfoV1(1, 0x04, 0, 0x05)));
        FlatPromptProjectionResultV1 result = AcceptChain(
            authority,
            ChainMessage(
                1,
                0x7F,
                true,
                0,
                0,
                new ChainEntrySpec(
                    0x0A0B0C0D,
                    new ModernLocInfoV1(1, 0x04, 0, 0x01),
                    0x0102030405060708,
                    1)),
            out FlatPromptSessionV1 session);

        AssertSuccess(result, FlatPromptFamilyV1.MsgSelectChain);
        FlatPromptChainPublicContextV1 context =
            result.Context as FlatPromptChainPublicContextV1 ??
            throw new InvalidOperationException("expected CHAIN context");
        Equal((byte)0x7F, context.ChainSpeCount);
        True(context.ChainForced);
        Equal(1, result.Candidates!.Count);
        True(result.Candidates[0] is FlatChainCardCodePublicCandidateDescriptorV1);
        AssertResponse(session, "MSG_SELECT_CHAIN:CHAIN_ENTRY:0", 0);
        False(result.Candidates.Any(
            candidate => candidate is FlatChainNoChainPublicCandidateDescriptorV1));
    }

    internal static void TestChainOptionalEmptyDomain()
    {
        Authority authority = CreateAuthority(0, 0);
        FlatPromptProjectionResultV1 result = AcceptChain(
            authority,
            ChainMessage(0, 0, false, 0, 0),
            out FlatPromptSessionV1 session);

        AssertSuccess(result, FlatPromptFamilyV1.MsgSelectChain);
        Equal(1, result.Candidates!.Count);
        True(result.Candidates[0] is FlatChainNoChainPublicCandidateDescriptorV1);
        AssertResponse(session, "MSG_SELECT_CHAIN:NO_CHAIN", -1);
        Equal(0ul, CaptureOrdinal(session, "MSG_SELECT_CHAIN:NO_CHAIN"));
    }

    internal static void TestChainEntryOrderDuplicatesAndValues()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        FlatPromptProjectionResultV1 result = AcceptChain(
            authority,
            ChainMessage(
                0,
                2,
                false,
                0x11111111,
                0x22222222,
                new ChainEntrySpec(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0x05),
                    0x0102030405060708,
                    2),
                new ChainEntrySpec(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0x05),
                    0x0102030405060708,
                    2)),
            out _);

        AssertSuccess(result, FlatPromptFamilyV1.MsgSelectChain);
        Equal(3, result.Candidates!.Count);
        FlatChainEntryPublicCandidateDescriptorBaseV1 first =
            result.Candidates[0] as FlatChainEntryPublicCandidateDescriptorBaseV1 ??
            throw new InvalidOperationException("expected first CHAIN entry");
        FlatChainEntryPublicCandidateDescriptorBaseV1 second =
            result.Candidates[1] as FlatChainEntryPublicCandidateDescriptorBaseV1 ??
            throw new InvalidOperationException("expected second CHAIN entry");
        Equal(0, first.SourceOrdinal);
        Equal(1, second.SourceOrdinal);
        Equal(first.DescriptionOrEffectId, second.DescriptionOrEffectId);
        Equal(first.PublicSemanticCardLocator, second.PublicSemanticCardLocator);
        Equal((byte)2, first.ClientMode);
        Equal((byte)2, second.ClientMode);
        NotEqual(first.I4LocalCandidateKey, second.I4LocalCandidateKey);
    }

    internal static void TestChainNoChainAuthority()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        FlatPromptProjectionResultV1 optional = AcceptChain(
            authority,
            ChainMessage(
                0,
                0,
                false,
                0,
                0,
                new ChainEntrySpec(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0x05),
                    1,
                    0)),
            out _);
        AssertSuccess(optional, FlatPromptFamilyV1.MsgSelectChain);
        True(optional.Candidates!.Last()
            is FlatChainNoChainPublicCandidateDescriptorV1);

        FlatPromptProjectionResultV1 forced = AcceptChain(
            authority,
            ChainMessage(
                0,
                0,
                true,
                0,
                0,
                new ChainEntrySpec(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0x05),
                    1,
                    0)),
            out _);
        AssertSuccess(forced, FlatPromptFamilyV1.MsgSelectChain);
        False(forced.Candidates!.Any(
            candidate => candidate is FlatChainNoChainPublicCandidateDescriptorV1));
    }

    internal static void TestChainMalformedWireAndEnumeration()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        byte[] valid = ChainMessage(
            0,
            1,
            false,
            0,
            0,
            new ChainEntrySpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05),
                1,
                0));

        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            valid[..15],
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            Append(valid, 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);

        byte[] countOverflow = new byte[16];
        countOverflow[0] = 16;
        BinaryPrimitives.WriteUInt32LittleEndian(
            countOverflow.AsSpan(12, 4),
            uint.MaxValue);
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            countOverflow,
            FlatPromptErrorCodeV1.ArithmeticFailure);

        byte[] invalidForced = ChainMessage(0, 0, true, 0, 0);
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidForced,
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);

        byte[] invalidBoolean = ChainMessage(0, 0, false, 0, 0);
        invalidBoolean[3] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidBoolean,
            FlatPromptErrorCodeV1.InvalidBoolean);

        byte[] invalidMode = valid.ToArray();
        invalidMode[^1] = 3;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidMode,
            FlatPromptErrorCodeV1.InvalidClientMode);

        byte[] invalidPlayer = valid.ToArray();
        invalidPlayer[1] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidPlayer,
            FlatPromptErrorCodeV1.InvalidParticipant);

        byte[] invalidController = valid.ToArray();
        invalidController[16 + 4] = 2;
        AssertFailure(
            new FlatPromptSessionV1(),
            authority,
            invalidController,
            FlatPromptErrorCodeV1.InvalidParticipant);
    }

    internal static void TestChainCorrelationAuthorityAndCardCodeSafety()
    {
        AssertIndexedPair(MirrorZoneV1.MonsterZone, PublicSemanticZoneV1.MonsterZone, true);
        AssertIndexedPair(MirrorZoneV1.Graveyard, PublicSemanticZoneV1.Graveyard, true);
        AssertIndexedPair(MirrorZoneV1.Banished, PublicSemanticZoneV1.Banished, true);
        AssertIndexedPair(
            MirrorZoneV1.SpellTrapZone,
            PublicSemanticZoneV1.SpellTrapZone,
            true);
        AssertIndexedPair(
            MirrorZoneV1.SpellTrapZone,
            PublicSemanticZoneV1.FieldZone,
            true);
        AssertIndexedPair(
            MirrorZoneV1.SpellTrapZone,
            PublicSemanticZoneV1.PendulumRelevantState,
            true);
        AssertIndexedPair(
            MirrorZoneV1.MonsterZone,
            PublicSemanticZoneV1.SpellTrapZone,
            false);

        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        FlatPromptProjectionResultV1 unsafeCode = AcceptChain(
            authority,
            ChainMessage(
                0,
                1,
                true,
                0,
                0,
                new ChainEntrySpec(
                    0x55667788,
                    new ModernLocInfoV1(0, 0x04, 0, 0x05),
                    7,
                    1)),
            out _);
        AssertSuccess(unsafeCode, FlatPromptFamilyV1.MsgSelectChain);
        True(
            unsafeCode.Candidates![0]
                is FlatChainPublicCandidateDescriptorV1);
        False(
            unsafeCode.Candidates[0]
                is FlatChainCardCodePublicCandidateDescriptorV1);
    }

    internal static void TestChainAtomicityStalenessAndOwnership()
    {
        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        byte[] valid = ChainMessage(
            0,
            1,
            true,
            0,
            0,
            new ChainEntrySpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05),
                7,
                0));
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 accepted = session.TryAcceptPrompt(
            valid,
            authority.Mirror,
            authority.Projection);
        AssertSuccess(accepted, FlatPromptFamilyV1.MsgSelectChain);
        True(session.TryCaptureSelection(
            "MSG_SELECT_CHAIN:CHAIN_ENTRY:0",
            out FlatPromptSelectionHandleV1? oldHandle,
            out _));

        byte[] malformed = valid.ToArray();
        malformed[malformed.Length - 1] = 3;
        AssertFailure(
            session,
            authority,
            malformed,
            FlatPromptErrorCodeV1.InvalidClientMode);
        False(session.TryResolveSelection(
            oldHandle,
            out _,
            out FlatPromptErrorCodeV1 staleError));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, staleError);

        byte[] sourceCopy = valid.ToArray();
        AssertSuccess(
            session.TryAcceptPrompt(
                sourceCopy,
                authority.Mirror,
                authority.Projection),
            FlatPromptFamilyV1.MsgSelectChain);
        sourceCopy[4] = 0xFF;
        AssertResponse(session, "MSG_SELECT_CHAIN:CHAIN_ENTRY:0", 0);
        Equal(1ul, CaptureOrdinal(session, "MSG_SELECT_CHAIN:CHAIN_ENTRY:0"));

        FlatPromptProjectionResultV1 optional = AcceptChain(
            authority,
            ChainMessage(
                0,
                1,
                false,
                0,
                0,
                new ChainEntrySpec(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0x05),
                    7,
                    0)),
            out _);
        FlatPublicCandidateDescriptorV1[] candidates =
            optional.Candidates!.ToArray();
        string[] swappedKeys = candidates
            .Select(candidate => candidate.I4LocalCandidateKey)
            .Reverse()
            .ToArray();
        int[] swappedResponses = { -1, 0 };
        False(CurrentFlatPromptBindingV1.TryCreate(
            0,
            FlatPromptFamilyV1.MsgSelectChain,
            candidates,
            swappedKeys,
            swappedResponses,
            out CurrentFlatPromptBindingV1? swapped,
            out FlatPromptErrorCodeV1 swappedError));
        Null(swapped);
        Equal(FlatPromptErrorCodeV1.InvalidResponseBinding, swappedError);

        PublicSemanticLocatorV1.TryCreateIndexed(
            0,
            PublicSemanticZoneV1.MonsterZone,
            0,
            out PublicSemanticLocatorV1? fakeLocator);
        NotNull(fakeLocator);
        FlatPublicCandidateDescriptorV1 fakeCandidate =
            new FakeChainCandidate(
                "MSG_SELECT_CHAIN:CHAIN_ENTRY:0",
                0,
                fakeLocator!,
                7,
                0);
        False(CurrentFlatPromptBindingV1.TryCreate(
            0,
            FlatPromptFamilyV1.MsgSelectChain,
            new[] { fakeCandidate },
            new[] { "MSG_SELECT_CHAIN:CHAIN_ENTRY:0" },
            new[] { 0 },
            out CurrentFlatPromptBindingV1? fakeBinding,
            out FlatPromptErrorCodeV1 fakeError));
        Null(fakeBinding);
        Equal(FlatPromptErrorCodeV1.InvalidResponseBinding, fakeError);
    }

    internal static void TestI4BPublicPrivateBoundary()
    {
        AssertPublicTypesDoNotExposePrivateData(
            typeof(FlatPromptChainPublicContextV1),
            typeof(FlatChainNoChainPublicCandidateDescriptorV1),
            typeof(FlatChainEntryPublicCandidateDescriptorBaseV1),
            typeof(FlatChainPublicCandidateDescriptorV1),
            typeof(FlatChainCardCodePublicCandidateDescriptorV1));

        False(typeof(FlatPromptCardCorrelationV1).IsPublic);
        False(typeof(FlatPromptCardCorrelationResultV1).IsPublic);
        False(typeof(MirrorAddressNormalizationV1).IsPublic);
        False(typeof(FlatPromptWireDraftV1).IsPublic);
        False(typeof(FlatPromptCardAuthorityContextV1).IsPublic);
        False(typeof(FlatPromptSessionV1).GetMethods().Any(
            method => method.IsPublic &&
                method.Name.Contains("Send", StringComparison.OrdinalIgnoreCase)));
    }

    internal static void TestI4AAndI3RegressionBoundary()
    {
        FlatPromptProjectionResultV1 i4a =
            new FlatPromptSessionV1().TryAcceptPrompt(
                new byte[]
                {
                    0x0D, 0x00, 0x08, 0x07, 0x06,
                    0x05, 0x04, 0x03, 0x02, 0x01
                });
        AssertSuccess(i4a, FlatPromptFamilyV1.MsgSelectYesNo);
        Equal(
            0x0102030405060708ul,
            ((FlatPromptYesNoPublicContextV1)i4a.Context!).YesNoDescriptionId);

        Authority authority = CreateAuthority(
            0,
            0,
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
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
            if (card.Type.HasValue)
            {
                ModernQueryV1 query = DecodeQuery(
                    QueryRecord(QueryFlagV1.Type, U32(card.Type.Value)),
                    QueryEnd());
                MirrorApplyResult updated = mirror.Apply(DecodeMessage(
                    decoder,
                    UpdateCardMessage(
                        card.Location.Controller,
                        (byte)(card.Location.Location & 0x7F),
                        checked((byte)card.Location.Sequence),
                        query)));
                True(updated.IsSuccess, updated.Error.ToString());
            }
        }

        PublicStateProjectionResultV1 projection =
            PublicStateProjectionV1.TryProject(
                mirror.Snapshot,
                new PublicStateProjectionContextV1(duelFlags));
        True(projection.IsSuccess, projection.Error.ToString());
        return new Authority(mirror, decoder, projection);
    }

    private static FlatPromptProjectionResultV1 AcceptEffect(
        Authority authority,
        uint sourceCardCode,
        ModernLocInfoV1 location)
    {
        return new FlatPromptSessionV1().TryAcceptPrompt(
            EffectYnMessage(0, sourceCardCode, location, 7),
            authority.Mirror,
            authority.Projection);
    }

    private static FlatPromptProjectionResultV1 AcceptChain(
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

    private static void AssertIndexedPair(
        MirrorZoneV1 mirrorZone,
        PublicSemanticZoneV1 publicZone,
        bool expectedSuccess)
    {
        const uint cardCode = 0x11223344;
        byte location = MirrorLocation(mirrorZone);
        uint sequence = IndexedTestSequence(publicZone, mirrorZone);
        ulong duelFlags = publicZone == PublicSemanticZoneV1.PendulumRelevantState
            ? 0x1800ul
            : 0ul;
        Authority authority = CreateAuthority(
            0,
            duelFlags,
            new CardSpec(
                cardCode,
                new ModernLocInfoV1(0, location, sequence, 0x05)));
        True(PublicSemanticLocatorV1.TryCreateIndexed(
            0,
            publicZone,
            sequence,
            out PublicSemanticLocatorV1? locator));
        NotNull(locator);
        PublicCardStateV1 replacement = new(
            locator!,
            0,
            publicZone,
            cardCode,
            0x05);
        PublicStateSnapshotV1 snapshot = IsAllowedPair(mirrorZone, publicZone)
            ? authority.Projection.Snapshot!
            : WithCards(authority.Projection.Snapshot!, new[] { replacement });
        bool actual = FlatPromptCardCorrelationV1.TryCorrelate(
            authority.Mirror.Snapshot,
            snapshot,
            cardCode,
            new ModernLocInfoV1(0, location, sequence, 0x05),
            out FlatPromptCardCorrelationResultV1? result,
            out FlatPromptErrorCodeV1 error);
        Equal(expectedSuccess, actual);
        if (expectedSuccess)
        {
            Equal(FlatPromptErrorCodeV1.None, error);
            NotNull(result);
            Equal(locator, result!.AcceptedLocator);
        }
        else
        {
            Null(result);
            Equal(
                FlatPromptErrorCodeV1.UnprovenPublicReference,
                error);
        }
    }

    private static uint IndexedTestSequence(
        PublicSemanticZoneV1 publicZone,
        MirrorZoneV1 mirrorZone) =>
        mirrorZone == MirrorZoneV1.SpellTrapZone &&
        publicZone == PublicSemanticZoneV1.FieldZone
            ? 5u
            : mirrorZone == MirrorZoneV1.SpellTrapZone &&
              publicZone == PublicSemanticZoneV1.PendulumRelevantState
                ? 6u
                : 0u;

    private static bool IsAllowedPair(
        MirrorZoneV1 mirrorZone,
        PublicSemanticZoneV1 publicZone) =>
        (mirrorZone, publicZone) switch
        {
            (MirrorZoneV1.MonsterZone, PublicSemanticZoneV1.MonsterZone) => true,
            (MirrorZoneV1.Graveyard, PublicSemanticZoneV1.Graveyard) => true,
            (MirrorZoneV1.Banished, PublicSemanticZoneV1.Banished) => true,
            (MirrorZoneV1.SpellTrapZone,
                PublicSemanticZoneV1.SpellTrapZone) => true,
            (MirrorZoneV1.SpellTrapZone,
                PublicSemanticZoneV1.FieldZone) => true,
            (MirrorZoneV1.SpellTrapZone,
                PublicSemanticZoneV1.PendulumRelevantState) => true,
            _ => false
        };

    private static byte MirrorLocation(MirrorZoneV1 zone) =>
        zone switch
        {
            MirrorZoneV1.MainDeck => 0x01,
            MirrorZoneV1.ExtraDeck => 0x40,
            MirrorZoneV1.Hand => 0x02,
            MirrorZoneV1.MonsterZone => 0x04,
            MirrorZoneV1.SpellTrapZone => 0x08,
            MirrorZoneV1.Graveyard => 0x10,
            MirrorZoneV1.Banished => 0x20,
            _ => throw new ArgumentOutOfRangeException(nameof(zone))
        };

    private static byte[] EffectYnMessage(
        byte player,
        uint sourceCardCode,
        ModernLocInfoV1 location,
        ulong description)
    {
        return Join(
            new[] { (byte)12, player },
            U32(sourceCardCode),
            LocInfo(
                location.Controller,
                location.Location,
                location.Sequence,
                location.Position),
            U64(description));
    }

    private static byte[] ChainMessage(
        byte player,
        byte speCount,
        bool forced,
        uint hintTimingForPlayer,
        uint hintTimingForOtherPlayer,
        params ChainEntrySpec[] entries)
    {
        List<byte[]> parts =
        [
            new[] { (byte)16, player, speCount, forced ? (byte)1 : (byte)0 },
            U32(hintTimingForPlayer),
            U32(hintTimingForOtherPlayer),
            U32((uint)entries.Length)
        ];
        parts.AddRange(entries.Select(entry => Join(
            U32(entry.SourceCardCode),
            LocInfo(
                entry.Location.Controller,
                entry.Location.Location,
                entry.Location.Sequence,
                entry.Location.Position),
            U64(entry.DescriptionOrEffectId),
            new[] { entry.ClientMode })));
        return Join(parts.ToArray());
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

    private static void AssertFailure(
        FlatPromptSessionV1 session,
        PerspectiveStateMirrorV1? mirror,
        PublicStateProjectionResultV1? projection,
        FlatPromptErrorCodeV1 expectedError)
    {
        AssertFailure(
            session,
            mirror,
            projection,
            EffectYnMessage(
                0,
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05),
                1),
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

    private static ulong CaptureOrdinal(
        FlatPromptSessionV1 session,
        string key)
    {
        True(session.TryCaptureSelection(
            key,
            out FlatPromptSelectionHandleV1? handle,
            out FlatPromptErrorCodeV1 error),
            error.ToString());
        NotNull(handle);
        return handle!.PromptInstanceOrdinal;
    }

    private static void AssertPublicTypesDoNotExposePrivateData(
        params Type[] types)
    {
        string[] forbidden =
        {
            "PromptInstanceOrdinal", "ResponseI32", "ResponseBody",
            "RawBytes", "MirrorEntityId", "ModernLocInfo", "ProtocolOffset",
            "Socket", "Path", "Timestamp", "Pid", "SourceCardCode"
        };
        foreach (Type type in types)
        {
            foreach (PropertyInfo property in type.GetProperties())
            {
                False(
                    forbidden.Contains(property.Name, StringComparer.Ordinal),
                    "forbidden public property " + type.Name + "." +
                    property.Name);
            }
        }
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
        ModernLocInfoV1 Location,
        uint? Type = null);

    private readonly record struct ChainEntrySpec(
        uint SourceCardCode,
        ModernLocInfoV1 Location,
        ulong DescriptionOrEffectId,
        byte ClientMode);

    private sealed record FakeChainCandidate
        : FlatChainEntryPublicCandidateDescriptorBaseV1
    {
        internal FakeChainCandidate(
            string i4LocalCandidateKey,
            int sourceOrdinal,
            PublicSemanticLocatorV1 publicSemanticCardLocator,
            ulong descriptionOrEffectId,
            byte clientMode)
            : base(
                i4LocalCandidateKey,
                sourceOrdinal,
                publicSemanticCardLocator,
                descriptionOrEffectId,
                clientMode)
        {
        }
    }
}
