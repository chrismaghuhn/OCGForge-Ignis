using System.Buffers.Binary;
using System.Reflection;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.ModernQueryFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using static OCGForge.Ignis.Gameplay.Tests.TransportFixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6GRealI4PromptBoundaryTests
{
    internal static void TestExistingIdleBoundaryBindsEveryCandidate()
    {
        (PerspectiveStateMirrorV1 mirror, _) = CreateMirror(0);
        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0));
        byte[] promptFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            IdleTransitionOnly());

        I6GRealI4PromptBoundaryEvidenceV1 result =
            I6GRealI4PromptBoundaryV1.TryEvaluate(
                GameplayPerspectiveV1.SelfIsPlayer0,
                ReadOnlyMemory<byte>.Empty,
                new[] { Join(startFrame, promptFrame) },
                1,
                mirror,
                0);

        True(result.IsSuccess, result.Error.ToString());
        Equal((byte)11, result.PromptId);
        Equal(FlatPromptFamilyV1.MsgSelectIdleCmd, result.PromptFamily);
        Equal((byte)0, result.ActingPlayer);
        True(result.ActingPlayerMatchesPerspective);
        True(result.PublicStateProjectionPassed);
        True(result.PromptProjectionPassed);
        Equal(3, result.LegalCandidateCount);
        True(result.CompleteDomain);
        True(result.AllCandidatesResponseBound);
        True(result.ToBattlePhasePresent);
        True(result.ToEndPhasePresent);
        True(result.ShuffleHandPresent);
        True(result.ChoiceKinds.SequenceEqual(new[]
        {
            FlatPromptChoiceKindV1.ToBp,
            FlatPromptChoiceKindV1.ToEp,
            FlatPromptChoiceKindV1.ShuffleHand
        }));
    }

    internal static void TestExistingChainBoundaryDescribesCompletePublicDomain()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                deckCount0: 8,
                extraCount0: 8,
                deckCount1: 8,
                extraCount1: 8);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 chainLocation = new(0, 0x02, 0, 0x08);
        True(
            mirror.Apply(DecodeMessage(
                decoder,
                MoveMessage(12345678, empty, chainLocation, 0))).IsSuccess);

        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0, 8, 8, 8, 8));
        byte[] promptFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            ChainMessage(
                0,
                2,
                false,
                1,
                2,
                new ChainEntrySpec(
                    0x55667788,
                    chainLocation,
                    42,
                    0)));

        I6GRealI4PromptBoundaryEvidenceV1 result =
            I6GRealI4PromptBoundaryV1.TryEvaluate(
                GameplayPerspectiveV1.SelfIsPlayer0,
                ReadOnlyMemory<byte>.Empty,
                new[] { Join(startFrame, promptFrame) },
                1,
                mirror,
                0x1234,
                CreateFullFrame());

        True(
            result.IsSuccess,
            $"{result.Error}; family={result.PromptFamily}; " +
            $"acting={result.ActingPlayer}; digest={result.PublicCandidateDomainDigest}");
        Equal((byte)16, result.PromptId);
        Equal(FlatPromptFamilyV1.MsgSelectChain, result.PromptFamily);
        Equal((byte)0, result.ActingPlayer);
        Equal((byte)0, result.PerspectivePlayer);
        True(result.ActingPlayerMatchesPerspective);
        True(result.PublicStateProjectionPassed);
        True(result.PromptProjectionPassed);
        Equal(2, result.LegalCandidateCount);
        True(result.CompleteDomain);
        True(result.AllCandidatesResponseBound);
        True(result.ChoiceKinds.SequenceEqual(new[]
        {
            FlatPromptChoiceKindV1.ChainEntry,
            FlatPromptChoiceKindV1.NoChain
        }));
        NotNull(result.PublicCandidateDomainDigest);
        True(
            result.PublicCandidateDomainDigest!.Length == 64 &&
            result.PublicCandidateDomainDigest.All(character =>
                character is >= '0' and <= '9' or >= 'a' and <= 'f'));
        True(result.ChainForced is false);
        Equal(2, result.ChainSpeCount);
        Equal(1, result.ChainEntryCount);
        Equal(1, result.NoChainCount);
    }

    internal static void TestChainBoundaryFailsClosedForWrongPerspective()
    {
        (PerspectiveStateMirrorV1 mirror, _) = CreateMirror(1);
        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(1));
        byte[] promptFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            ChainMessage(0, 0, false, 0, 0));

        I6GRealI4PromptBoundaryEvidenceV1 result =
            I6GRealI4PromptBoundaryV1.TryEvaluate(
                GameplayPerspectiveV1.SelfIsPlayer1,
                ReadOnlyMemory<byte>.Empty,
                new[] { Join(startFrame, promptFrame) },
                1,
                mirror,
                0);

        False(result.IsSuccess);
        Equal(FlatPromptErrorCodeV1.InvalidParticipant, result.Error);
        Equal((byte)0, result.ActingPlayer);
        Equal((byte)1, result.PerspectivePlayer);
        False(result.ActingPlayerMatchesPerspective);
    }

    internal static void TestChainBoundaryFailsClosedForUnprovenReference()
    {
        (PerspectiveStateMirrorV1 mirror, _) = CreateMirror(0);
        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0));
        byte[] promptFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            ChainMessage(
                0,
                1,
                true,
                0,
                0,
                new ChainEntrySpec(
                    0x12345678,
                    new ModernLocInfoV1(0, 0x02, 0, 0x08),
                    42,
                    0)));

        I6GRealI4PromptBoundaryEvidenceV1 result =
            I6GRealI4PromptBoundaryV1.TryEvaluate(
                GameplayPerspectiveV1.SelfIsPlayer0,
                ReadOnlyMemory<byte>.Empty,
                new[] { Join(startFrame, promptFrame) },
                1,
                mirror,
                0);

        False(result.IsSuccess);
        Equal(FlatPromptErrorCodeV1.UnprovenPublicReference, result.Error);
        Equal(FlatPromptFamilyV1.MsgSelectChain, result.PromptFamily);
        Equal((byte)0, result.ActingPlayer);
        Equal((byte)0, result.PerspectivePlayer);
        True(result.PublicStateProjectionPassed);
        False(result.PromptProjectionPassed);
    }

    internal static void TestChainBoundaryRejectsMalformedAndUnsupportedFamilies()
    {
        (PerspectiveStateMirrorV1 mirror, _) = CreateMirror(0);
        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0));

        I6GRealI4PromptBoundaryEvidenceV1 malformed =
            I6GRealI4PromptBoundaryV1.TryEvaluate(
                GameplayPerspectiveV1.SelfIsPlayer0,
                ReadOnlyMemory<byte>.Empty,
                new[]
                {
                    Join(
                        startFrame,
                        WireFrameCodec.EncodeStoc(
                            StocPacketType.GameMsg,
                            new byte[] { 16, 0, 0 }))
                },
                1,
                mirror,
                0);
        False(malformed.IsSuccess);
        Equal(FlatPromptErrorCodeV1.MalformedPrompt, malformed.Error);

        byte[] effectYn = Join(
            new byte[] { 12, 0 },
            U32(12345678),
            LocInfo(0, 0x02, 0, 0x08),
            U64(42));
        I6GRealI4PromptBoundaryEvidenceV1 unsupported =
            I6GRealI4PromptBoundaryV1.TryEvaluate(
                GameplayPerspectiveV1.SelfIsPlayer0,
                ReadOnlyMemory<byte>.Empty,
                new[]
                {
                    Join(
                        startFrame,
                        WireFrameCodec.EncodeStoc(
                            StocPacketType.GameMsg,
                            effectYn))
                },
                1,
                mirror,
                0);
        False(unsupported.IsSuccess);
        Equal(
            FlatPromptErrorCodeV1.UnsupportedPromptLayout,
            unsupported.Error);
        Equal(FlatPromptFamilyV1.MsgSelectEffectYn, unsupported.PromptFamily);
    }

    private static byte[] IdleTransitionOnly()
    {
        byte[] bytes = new byte[29];
        bytes[0] = 11;
        bytes[1] = 0;
        bytes[26] = 1;
        bytes[27] = 1;
        bytes[28] = 1;
        return bytes;
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

    private static PerspectiveSafeFrameV1 CreateFullFrame()
    {
        MethodInfo method = typeof(I6C6NativeOracleTests).GetMethod(
                "CreateFullFrame",
                BindingFlags.NonPublic | BindingFlags.Static) ??
            throw new InvalidOperationException("full frame factory missing");
        return (PerspectiveSafeFrameV1)method.Invoke(
            null,
            new object[] { false, false })!;
    }

    private readonly record struct ChainEntrySpec(
        uint SourceCardCode,
        ModernLocInfoV1 Location,
        ulong DescriptionOrEffectId,
        byte ClientMode);
}
