using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using static OCGForge.Ignis.Gameplay.Tests.TransportFixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6GMsgHintIngressTests
{
    internal static void TestValidMsgHintIsConsumedWithoutMirrorMutation()
    {
        SessionRunResult withoutHint = Run(
            new byte[] { 40, 0 });
        SessionRunResult withHint = Run(
            MsgHint(0x0102030405060708),
            new byte[] { 40, 0 });

        True(withoutHint.Result.IsSuccess, withoutHint.Result.Error.ToString());
        True(withHint.Result.IsSuccess, withHint.Result.Error.ToString());
        Equal(
            GameplayMessageKindV1.NewTurn,
            withoutHint.Result.Message!.Kind);
        Equal(
            GameplayMessageKindV1.NewTurn,
            withHint.Result.Message!.Kind);
        Equal(0, withoutHint.PresentationMessagesConsumed);
        Equal(1, withHint.PresentationMessagesConsumed);
        Equal(withoutHint.AfterSnapshot, withHint.AfterSnapshot);
        Equal(withoutHint.VisibleEventCount, withHint.VisibleEventCount);
    }

    internal static void TestShortMsgHintFailsClosed()
    {
        SessionRunResult result = Run(
            new byte[] { 2, 1, 0, 0, 0, 0, 0, 0, 0, 0 });

        False(result.Result.IsSuccess);
        Equal(GameplayErrorCode.MalformedGameMessage, result.Result.Error);
        Equal(0, result.PresentationMessagesConsumed);
        Equal(result.BeforeSnapshot, result.AfterSnapshot);
        Equal(0, result.VisibleEventCount);
    }

    internal static void TestOverlongMsgHintFailsClosed()
    {
        SessionRunResult result = Run(
            Join(
                MsgHint(0x1112131415161718),
                new byte[] { 0xff }));

        False(result.Result.IsSuccess);
        Equal(GameplayErrorCode.MalformedGameMessage, result.Result.Error);
        Equal(0, result.PresentationMessagesConsumed);
        Equal(result.BeforeSnapshot, result.AfterSnapshot);
        Equal(0, result.VisibleEventCount);
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
        GameplayHandoffAcquireResult acquired =
            GameplayHandoffConsumerV1.TryCreate(
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
            return new(
                result,
                beforeSnapshot,
                session.Mirror.Snapshot.ToDeterministicString(),
                session.PresentationMessagesConsumed,
                session.Mirror.VisibleEvents.Count);
        }
        finally
        {
            session.DisposeAsync().GetAwaiter().GetResult();
            acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    private static byte[] MsgHint(ulong data) =>
        Join(new byte[] { 2, 1, 0 }, U64(data));

    private readonly record struct SessionRunResult(
        GameplayMirrorPumpResult Result,
        string BeforeSnapshot,
        string AfterSnapshot,
        int PresentationMessagesConsumed,
        int VisibleEventCount);
}
