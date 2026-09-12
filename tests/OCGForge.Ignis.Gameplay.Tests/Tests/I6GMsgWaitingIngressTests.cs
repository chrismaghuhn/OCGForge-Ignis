using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using static OCGForge.Ignis.Gameplay.Tests.TransportFixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6GMsgWaitingIngressTests
{
    internal static void TestValidMsgWaitingIsConsumedBeforeGameplay()
    {
        SessionRunResult baseline = Run(NewTurn());
        SessionRunResult withWaiting = Run(MsgWaiting(), NewTurn());

        True(baseline.Result.IsSuccess, baseline.Result.Error.ToString());
        True(withWaiting.Result.IsSuccess, withWaiting.Result.Error.ToString());
        Equal(
            GameplayMessageKindV1.NewTurn,
            withWaiting.Result.Message!.Kind);
        Equal(1, withWaiting.PresentationMessagesConsumed);
        Equal((ulong)1, withWaiting.CurrentFrameOrdinal!.Value);
        True(withWaiting.HasCurrentAuthority);
        Equal(baseline.AfterSnapshot, withWaiting.AfterSnapshot);
        Equal(baseline.VisibleEventCount, withWaiting.VisibleEventCount);
    }

    internal static void TestRepeatedMsgWaitingMarkersArePresentationOnly()
    {
        SessionRunResult baseline = Run(NewTurn());
        SessionRunResult repeated = Run(
            MsgWaiting(),
            MsgWaiting(),
            MsgWaiting(),
            NewTurn());

        True(baseline.Result.IsSuccess, baseline.Result.Error.ToString());
        True(repeated.Result.IsSuccess, repeated.Result.Error.ToString());
        Equal(
            GameplayMessageKindV1.NewTurn,
            repeated.Result.Message!.Kind);
        Equal(3, repeated.PresentationMessagesConsumed);
        Equal((ulong)1, repeated.CurrentFrameOrdinal!.Value);
        True(repeated.HasCurrentAuthority);
        Equal(baseline.AfterSnapshot, repeated.AfterSnapshot);
        Equal(baseline.VisibleEventCount, repeated.VisibleEventCount);
    }

    internal static void TestMalformedMsgWaitingFailsClosed()
    {
        foreach (byte[] malformed in new[]
        {
            new byte[] { 3, 0 },
            new byte[] { 3, 0, 0 }
        })
        {
            SessionRunResult result = Run(malformed);

            False(result.Result.IsSuccess);
            Equal(GameplayErrorCode.MalformedGameMessage, result.Result.Error);
            Equal(0, result.PresentationMessagesConsumed);
            Equal(result.BeforeSnapshot, result.AfterSnapshot);
            Equal(0, result.VisibleEventCount);
            False(result.HasCurrentAuthority);
        }
    }

    internal static void TestDirectDecoderStillRejectsMsgWaiting()
    {
        GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(
            new StocGameMessagePayload(new byte[] { 3 }));

        False(result.IsSuccess);
        Equal(GameplayErrorCode.UnsupportedMessage, result.Error);
    }

    internal static void TestMsgWaitingAdvancesCaptureWireOrdinal()
    {
        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0));
        byte[] waitingFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            MsgWaiting());
        byte[] drawFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            DrawMessage(0, (0x10203040u, 0x05u)));

        GameplayMessageV1? message =
            I6C6CapturedGameplayMessageTraceV1.TryFindMessageAtOrdinal(
                GameplayPerspectiveV1.SelfIsPlayer0,
                startFrame,
                new[] { Join(waitingFrame, drawFrame) },
                2);

        NotNull(message);
        Equal(GameplayMessageKindV1.Draw, message!.Kind);
    }

    private static SessionRunResult Run(params byte[][] messages)
    {
        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0));
        byte[][] messageFrames = messages
            .Select(message => WireFrameCodec.EncodeStoc(
                StocPacketType.GameMsg,
                message))
            .ToArray();
        TestTransport transport = new(
            new[] { Join(new[] { startFrame }.Concat(messageFrames).ToArray()) });
        GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
            CreateHandoff(transport, Array.Empty<byte>()));
        True(acquired.IsSuccess, acquired.Error.ToString());

        GameplayPumpResult start = acquired.Consumer!.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(start.IsSuccess, start.Error.ToString());
        MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
            start.Message!,
            start.Perspective!);
        True(created.IsSuccess, created.Error.ToString());

        GameplayMirrorSessionV1 session = new(
            start.Session!,
            created.Mirror!);
        try
        {
            string beforeSnapshot =
                session.Mirror.Snapshot.ToDeterministicString();
            GameplayMirrorPumpResult result = session.PumpAsync(
                CancellationToken.None).GetAwaiter().GetResult();
            bool hasCurrentAuthority = session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? authority);
            return new(
                result,
                beforeSnapshot,
                session.Mirror.Snapshot.ToDeterministicString(),
                session.PresentationMessagesConsumed,
                hasCurrentAuthority,
                hasCurrentAuthority ? authority!.FrameInstanceOrdinal : null,
                session.Mirror.VisibleEvents.Count);
        }
        finally
        {
            session.DisposeAsync().GetAwaiter().GetResult();
            acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    private static byte[] MsgWaiting() => new byte[] { 3 };

    private static byte[] NewTurn() => new byte[] { 40, 0 };

    private readonly record struct SessionRunResult(
        GameplayMirrorPumpResult Result,
        string BeforeSnapshot,
        string AfterSnapshot,
        int PresentationMessagesConsumed,
        bool HasCurrentAuthority,
        ulong? CurrentFrameOrdinal,
        int VisibleEventCount);
}
