using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
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
}
