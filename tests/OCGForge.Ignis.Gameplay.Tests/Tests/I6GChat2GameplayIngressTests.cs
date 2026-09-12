using System.Text;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using static OCGForge.Ignis.Gameplay.Tests.TransportFixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6GChat2GameplayIngressTests
{
    internal static void TestValidChat2IsConsumedBeforeGameplay()
    {
        SessionRunResult baseline = Run(GameMessage(NewTurn()));
        SessionRunResult withChat2 = Run(
            Chat2Frame(),
            GameMessage(NewTurn()));

        True(baseline.Result.IsSuccess, baseline.Result.Error.ToString());
        True(withChat2.Result.IsSuccess, withChat2.Result.Error.ToString());
        Equal(
            GameplayMessageKindV1.NewTurn,
            withChat2.Result.Message!.Kind);
        Equal(1, withChat2.PresentationMessagesConsumed);
        Equal((ulong)1, withChat2.CurrentFrameOrdinal!.Value);
        True(withChat2.HasCurrentAuthority);
        Equal(baseline.AfterSnapshot, withChat2.AfterSnapshot);
        Equal(baseline.VisibleEventCount, withChat2.VisibleEventCount);
    }

    internal static void TestRepeatedChat2MarkersArePresentationOnly()
    {
        SessionRunResult baseline = Run(GameMessage(NewTurn()));
        SessionRunResult repeated = Run(
            Chat2Frame(),
            Chat2Frame(),
            Chat2Frame(),
            GameMessage(NewTurn()));

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

    internal static void TestChat2BetweenGameplayMessagesIsStateNeutral()
    {
        TwoPumpResult baseline = RunTwo(
            GameMessage(NewTurn()),
            GameMessage(NewTurn()));
        TwoPumpResult withChat2 = RunTwo(
            GameMessage(NewTurn()),
            Chat2Frame(),
            GameMessage(NewTurn()));

        True(baseline.FirstResult.IsSuccess,
            baseline.FirstResult.Error.ToString());
        True(baseline.SecondResult.IsSuccess,
            baseline.SecondResult.Error.ToString());
        True(withChat2.FirstResult.IsSuccess,
            withChat2.FirstResult.Error.ToString());
        True(withChat2.SecondResult.IsSuccess,
            withChat2.SecondResult.Error.ToString());
        Equal(
            GameplayMessageKindV1.NewTurn,
            withChat2.FirstResult.Message!.Kind);
        Equal(
            GameplayMessageKindV1.NewTurn,
            withChat2.SecondResult.Message!.Kind);
        Equal(1, withChat2.PresentationMessagesConsumed);
        Equal((ulong)1, withChat2.FirstFrameOrdinal!.Value);
        Equal((ulong)2, withChat2.SecondFrameOrdinal!.Value);
        Equal(baseline.AfterSecondSnapshot, withChat2.AfterSecondSnapshot);
        Equal(
            baseline.VisibleEventCount,
            withChat2.VisibleEventCount);
    }

    internal static void TestMalformedChat2FailsClosed()
    {
        SessionRunResult result = Run(Chat2Frame(Array.Empty<byte>()));

        False(result.Result.IsSuccess);
        Equal(GameplayErrorCode.MalformedOuterFrame, result.Result.Error);
        Equal(0, result.PresentationMessagesConsumed);
        Equal(result.BeforeSnapshot, result.AfterSnapshot);
        Equal(0, result.VisibleEventCount);
        False(result.HasCurrentAuthority);
    }

    internal static void TestChat2AdvancesCaptureWireOrdinal()
    {
        byte[] startFrame = GameMessage(CreateStartBytes(0));
        byte[] firstGameplayFrame = GameMessage(NewTurn());
        byte[] chat2Frame = Chat2Frame();
        byte[] secondGameplayFrame = GameMessage(NewTurn());

        GameplayMessageV1? message =
            I6C6CapturedGameplayMessageTraceV1.TryFindMessageAtOrdinal(
                GameplayPerspectiveV1.SelfIsPlayer0,
                startFrame,
                new[]
                {
                    Join(
                        firstGameplayFrame,
                        chat2Frame,
                        secondGameplayFrame)
                },
                3);

        NotNull(message);
        Equal(GameplayMessageKindV1.NewTurn, message!.Kind);
    }

    private static SessionRunResult Run(params byte[][] frames)
    {
        byte[] startFrame = GameMessage(CreateStartBytes(0));
        TestTransport transport = new(
            new[] { Join(new[] { startFrame }.Concat(frames).ToArray()) });
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

    private static TwoPumpResult RunTwo(params byte[][] frames)
    {
        byte[] startFrame = GameMessage(CreateStartBytes(0));
        TestTransport transport = new(
            new[] { Join(new[] { startFrame }.Concat(frames).ToArray()) });
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
            GameplayMirrorPumpResult first = session.PumpAsync(
                CancellationToken.None).GetAwaiter().GetResult();
            bool hasFirstAuthority = session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? firstAuthority);
            string afterFirstSnapshot =
                session.Mirror.Snapshot.ToDeterministicString();
            GameplayMirrorPumpResult second = session.PumpAsync(
                CancellationToken.None).GetAwaiter().GetResult();
            bool hasSecondAuthority = session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? secondAuthority);
            return new(
                first,
                second,
                afterFirstSnapshot,
                session.Mirror.Snapshot.ToDeterministicString(),
                session.PresentationMessagesConsumed,
                hasFirstAuthority ? firstAuthority!.FrameInstanceOrdinal : null,
                hasSecondAuthority
                    ? secondAuthority!.FrameInstanceOrdinal
                    : null,
                session.Mirror.VisibleEvents.Count);
        }
        finally
        {
            session.DisposeAsync().GetAwaiter().GetResult();
            acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    private static byte[] GameMessage(byte[] bytes) =>
        WireFrameCodec.EncodeStoc(StocPacketType.GameMsg, bytes);

    private static byte[] Chat2Frame(byte[]? payload = null) =>
        WireFrameCodec.EncodeStoc(
            StocPacketType.Chat2,
            payload ?? ValidChat2Payload());

    private static byte[] ValidChat2Payload() =>
        Join(
            new byte[] { (byte)StocChat2Type.System, 0 },
            FixedUtf16String.Encode("probe", 20),
            Encoding.Unicode.GetBytes("ignored\0"));

    private static byte[] NewTurn() => new byte[] { 40, 0 };

    private readonly record struct SessionRunResult(
        GameplayMirrorPumpResult Result,
        string BeforeSnapshot,
        string AfterSnapshot,
        int PresentationMessagesConsumed,
        bool HasCurrentAuthority,
        ulong? CurrentFrameOrdinal,
        int VisibleEventCount);

    private readonly record struct TwoPumpResult(
        GameplayMirrorPumpResult FirstResult,
        GameplayMirrorPumpResult SecondResult,
        string AfterFirstSnapshot,
        string AfterSecondSnapshot,
        int PresentationMessagesConsumed,
        ulong? FirstFrameOrdinal,
        ulong? SecondFrameOrdinal,
        int VisibleEventCount);
}
