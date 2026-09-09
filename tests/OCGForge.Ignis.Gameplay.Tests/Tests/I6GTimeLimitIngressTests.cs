using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using static OCGForge.Ignis.Gameplay.Tests.TransportFixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6GTimeLimitIngressTests
{
    internal static void TestPlayerZeroTimeLimitContinuesWithoutMutation()
    {
        SessionRunResult withoutTimeLimit = Run(NewTurnFrame());
        SessionRunResult withTimeLimit = Run(
            TimeLimitFrame(0, 120),
            NewTurnFrame());

        True(withoutTimeLimit.Result.IsSuccess,
            withoutTimeLimit.Result.Error.ToString());
        True(withTimeLimit.Result.IsSuccess,
            withTimeLimit.Result.Error.ToString());
        Equal(
            GameplayMessageKindV1.NewTurn,
            withoutTimeLimit.Result.Message!.Kind);
        Equal(
            GameplayMessageKindV1.NewTurn,
            withTimeLimit.Result.Message!.Kind);
        Equal(withoutTimeLimit.AfterSnapshot, withTimeLimit.AfterSnapshot);
        Equal(withoutTimeLimit.VisibleEventCount, withTimeLimit.VisibleEventCount);
    }

    internal static void TestPlayerOneTimeLimitContinuesWithoutMutation()
    {
        SessionRunResult withoutTimeLimit = Run(NewTurnFrame());
        SessionRunResult withTimeLimit = Run(
            TimeLimitFrame(1, 120),
            NewTurnFrame());

        True(withoutTimeLimit.Result.IsSuccess,
            withoutTimeLimit.Result.Error.ToString());
        True(withTimeLimit.Result.IsSuccess,
            withTimeLimit.Result.Error.ToString());
        Equal(
            GameplayMessageKindV1.NewTurn,
            withoutTimeLimit.Result.Message!.Kind);
        Equal(
            GameplayMessageKindV1.NewTurn,
            withTimeLimit.Result.Message!.Kind);
        Equal(withoutTimeLimit.AfterSnapshot, withTimeLimit.AfterSnapshot);
        Equal(withoutTimeLimit.VisibleEventCount, withTimeLimit.VisibleEventCount);
    }

    internal static void TestInvalidTimeLimitPlayerFailsClosed()
    {
        SessionRunResult result = Run(TimeLimitFrame(2, 120));

        False(result.Result.IsSuccess);
        Equal(GameplayErrorCode.InvalidParticipant, result.Result.Error);
        Equal(result.BeforeSnapshot, result.AfterSnapshot);
        Equal(0, result.VisibleEventCount);
    }

    internal static void TestShortTimeLimitFailsClosed()
    {
        SessionRunResult result = Run(
            WireFrameCodec.EncodeStoc(
                StocPacketType.TimeLimit,
                new byte[] { 0, 0, 0 }));

        False(result.Result.IsSuccess);
        Equal(GameplayErrorCode.MalformedOuterFrame, result.Result.Error);
        Equal(result.BeforeSnapshot, result.AfterSnapshot);
        Equal(0, result.VisibleEventCount);
    }

    internal static void TestOverlongTimeLimitFailsClosed()
    {
        SessionRunResult result = Run(
            WireFrameCodec.EncodeStoc(
                StocPacketType.TimeLimit,
                new byte[] { 0, 0, 0, 0, 0 }));

        False(result.Result.IsSuccess);
        Equal(GameplayErrorCode.MalformedOuterFrame, result.Result.Error);
        Equal(result.BeforeSnapshot, result.AfterSnapshot);
        Equal(0, result.VisibleEventCount);
    }

    private static SessionRunResult Run(params byte[][] frames)
    {
        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0));
        TestTransport transport = new(
            new[] { Join(new[] { startFrame }.Concat(frames).ToArray()) });
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
                session.Mirror.VisibleEvents.Count);
        }
        finally
        {
            session.DisposeAsync().GetAwaiter().GetResult();
            acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    private static byte[] TimeLimitFrame(byte player, ushort leftTime) =>
        WireFrameCodec.EncodeStoc(
            StocPacketType.TimeLimit,
            PacketPayloadCodec.EncodeStocTimeLimit(
                new StocTimeLimitPayload(player, leftTime)));

    private static byte[] NewTurnFrame() =>
        WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            new byte[] { 40, 0 });

    private readonly record struct SessionRunResult(
        GameplayMirrorPumpResult Result,
        string BeforeSnapshot,
        string AfterSnapshot,
        int VisibleEventCount);
}
