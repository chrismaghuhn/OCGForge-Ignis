using System.Buffers.Binary;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6GSingleResponseProofTests
{
    internal static void TestToEpResponseUsesExistingI4Binding()
    {
        (PerspectiveStateMirrorV1 mirror, _) = CreateMirror(0);
        PublicStateProjectionResultV1 projection =
            PublicStateProjectionV1.TryProject(
                mirror.Snapshot,
                new PublicStateProjectionContextV1(0));
        True(projection.IsSuccess, projection.Error.ToString());

        byte[] prompt = IdleTransitionOnly();
        FlatPromptSessionV1 expectedSession = new();
        FlatPromptProjectionResultV1 expectedProjection =
            expectedSession.TryAcceptPrompt(
                prompt,
                mirror,
                projection);
        True(expectedProjection.IsSuccess, expectedProjection.Error.ToString());
        FlatPublicCandidateDescriptorV1 expectedToEp = expectedProjection
            .Candidates!
            .Single(candidate =>
                candidate.ChoiceKind == FlatPromptChoiceKindV1.ToEp);
        True(expectedSession.TryCaptureSelection(
            expectedToEp.I4LocalCandidateKey,
            out FlatPromptSelectionHandleV1? expectedHandle,
            out FlatPromptErrorCodeV1 captureError));
        Equal(FlatPromptErrorCodeV1.None, captureError);
        True(expectedSession.TryResolveSelection(
            expectedHandle,
            out _,
            out FlatPromptErrorCodeV1 resolveError));
        Equal(FlatPromptErrorCodeV1.None, resolveError);
        True(expectedSession.TryResolveSelection(
            expectedHandle,
            out FlatPromptResponseResolutionV1 expectedResponse,
            out FlatPromptErrorCodeV1 expectedResolveError));
        Equal(FlatPromptErrorCodeV1.None, expectedResolveError);
        byte[] expectedResponseBody = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(
            expectedResponseBody,
            expectedResponse.ResponseI32);

        I6GSingleResponseBindingResultV1 actual =
            I6GSingleResponseBindingV1.TryResolveToEp(
                prompt,
                mirror,
                projection,
                expectedPerspective: 0);

        True(actual.IsSuccess, actual.Error.ToString());
        Equal(FlatPromptFamilyV1.MsgSelectIdleCmd, actual.PromptFamily);
        Equal((byte)0, actual.ActingPlayer);
        Equal(3, actual.CandidateCount);
        Equal(1, actual.ToEpMatchCount);
        True(actual.CompleteDomain);
        True(actual.SelectionCapturePassed);
        True(actual.SelectionResolutionPassed);
        True(actual.ResponseResolutionPassed);
        True(actual.IsTerminal);
        BytesEqual(
            expectedResponseBody,
            actual.ResponseBody.ToArray());
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
