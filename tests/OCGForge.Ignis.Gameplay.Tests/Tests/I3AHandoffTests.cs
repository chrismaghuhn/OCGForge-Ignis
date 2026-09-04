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

internal static class I3AHandoffTests
{
    internal static void TestHandoffClaimExactlyOnce()
    {
        TestTransport transport = new(Array.Empty<byte[]>());
        GameplayHandoffOfferV1 handoff = CreateHandoff(transport, Array.Empty<byte>());

        GameplayHandoffAcquireResult first = GameplayHandoffConsumerV1.TryCreate(handoff);
        True(first.IsSuccess);
        NotNull(first.Consumer);

        GameplayHandoffAcquireResult second = GameplayHandoffConsumerV1.TryCreate(handoff);
        False(second.IsSuccess);
        Equal(GameplayErrorCode.HandoffAlreadyClaimed, second.Error);
        Null(second.Consumer);

        first.Consumer!.DisposeAsync().GetAwaiter().GetResult();
        Equal(1, transport.CloseCallCount);
    }

    internal static void TestPendingBytesFirst()
    {
        byte[] frame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0x00));
        TestTransport transport = new(new[] { new byte[] { 0xaa } });
        GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
            CreateHandoff(transport, frame));
        True(acquired.IsSuccess);

        GameplayPumpResult result = acquired.Consumer!.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(result.IsSuccess);
        Equal(GameplayPerspectiveKind.SelfIsPlayer0, result.Perspective!.Kind);
        Equal(0, transport.ReadCallCount);
        Equal(0, transport.CloseCallCount);

        GameplaySessionV1 session = result.Session!;
        acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        Equal(0, transport.CloseCallCount);
        session.CloseOwnedTransportAsync().GetAwaiter().GetResult();
        session.CloseOwnedTransportAsync().GetAwaiter().GetResult();
        Equal(1, transport.CloseCallCount);
    }

    internal static void TestPartialPendingFrame()
    {
        byte[] frame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0x00));
        const int pendingLength = 3;
        TestTransport transport = new(Split(
            frame.AsSpan(pendingLength).ToArray(),
            new[] { 1, 2, 3, 5 }));
        GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
            CreateHandoff(transport, frame.AsSpan(0, pendingLength)));

        GameplayPumpResult result = acquired.Consumer!.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(result.IsSuccess);
        Equal(GameplayPerspectiveKind.SelfIsPlayer0, result.Perspective!.Kind);
        True(transport.ReadCallCount > 0);
        acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        result.Session!.CloseOwnedTransportAsync().GetAwaiter().GetResult();
    }

    internal static void TestSessionPendingReadFirst()
    {
        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0x00));
        byte[] futureFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            new byte[] { 3 });
        byte[] combined = new byte[startFrame.Length + futureFrame.Length];
        startFrame.CopyTo(combined, 0);
        futureFrame.CopyTo(combined, startFrame.Length);

        TestTransport transport = new(new[] { combined });
        GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
            CreateHandoff(transport, Array.Empty<byte>()));
        GameplayPumpResult result = acquired.Consumer!.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(result.IsSuccess);

        byte[] destination = new byte[futureFrame.Length];
        int readsBeforeSession = transport.ReadCallCount;
        int pendingCount = result.Session!.ReadAsync(
                destination,
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        Equal(futureFrame.Length, pendingCount);
        BytesEqual(futureFrame, destination);
        Equal(readsBeforeSession, transport.ReadCallCount);

        transport.Enqueue(new byte[] { 0xa1 });
        int liveCount = result.Session.ReadAsync(
                destination.AsMemory(0, 1),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        Equal(1, liveCount);
        Equal(readsBeforeSession + 1, transport.ReadCallCount);

        acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        result.Session.CloseOwnedTransportAsync().GetAwaiter().GetResult();
    }

    internal static void TestPumpDisposeLifecycle()
    {
        byte[] frame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0x00));
        const int pendingLength = 3;
        LifecycleRaceTransport transport = new(frame.AsSpan(pendingLength).ToArray());
        GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
            CreateHandoff(transport, frame.AsSpan(0, pendingLength)));
        True(acquired.IsSuccess);

        Task<GameplayPumpResult> pumpTask = Task.Run(async () =>
            await acquired.Consumer!.PumpAsync(CancellationToken.None));
        True(transport.ReadStarted.Wait(TimeSpan.FromSeconds(5)));

        Task disposeTask = acquired.Consumer!.DisposeAsync().AsTask();
        transport.Release();

        GameplayPumpResult result = pumpTask.GetAwaiter().GetResult();
        disposeTask.GetAwaiter().GetResult();
        True(result.IsSuccess);
        Equal(0, transport.CloseCallCount);

        result.Session!.CloseOwnedTransportAsync().GetAwaiter().GetResult();
        Equal(1, transport.CloseCallCount);
    }

    internal static void TestChunkingDeterminism()
    {
        byte[] frame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0x01));
        string whole = RunChunking(new[] { frame }, out _);
        string oneByte = RunChunking(
            frame.Select(value => new[] { value }).ToArray(),
            out _);
        string irregular = RunChunking(
            Split(frame, new[] { 1, 2, 5, 3, 7 }),
            out _);

        Equal(whole, oneByte);
        Equal(whole, irregular);
    }

    internal static void TestPendingSuffixTransfer()
    {
        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0x00));
        byte[] futureFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            new byte[] { 3 });
        byte[] combined = new byte[startFrame.Length + futureFrame.Length];
        startFrame.CopyTo(combined, 0);
        futureFrame.CopyTo(combined, startFrame.Length);

        TestTransport transport = new(new[] { combined });
        GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
            CreateHandoff(transport, Array.Empty<byte>()));
        GameplayPumpResult result = acquired.Consumer!.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();

        True(result.IsSuccess);
        BytesEqual(futureFrame, result.Session!.PendingBytes.Span);
        acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        result.Session.CloseOwnedTransportAsync().GetAwaiter().GetResult();
    }

    internal static void TestFailureCloseExactlyOnce()
    {
        byte[] observerFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0x10));
        TestTransport transport = new(Array.Empty<byte[]>());
        GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
            CreateHandoff(transport, observerFrame));

        GameplayPumpResult result = acquired.Consumer!.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        False(result.IsSuccess);
        Equal(GameplayErrorCode.UnsupportedPerspective, result.Error);
        Equal(1, transport.CloseCallCount);
        int readsAfterFailure = transport.ReadCallCount;

        GameplayPumpResult repeated = acquired.Consumer.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        False(repeated.IsSuccess);
        Equal(readsAfterFailure, transport.ReadCallCount);
        acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        Equal(1, transport.CloseCallCount);
    }

    internal static void TestShortInnerMessage()
    {
        byte[] frame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            new byte[] { GameplayMessageV1.MessageId, 0x00 });
        TestTransport transport = new(Array.Empty<byte[]>());
        GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
            CreateHandoff(transport, frame));

        GameplayPumpResult result = acquired.Consumer!.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        False(result.IsSuccess);
        Equal(GameplayErrorCode.MalformedGameMessage, result.Error);
        Equal(1, transport.CloseCallCount);
    }

    internal static void TestMalformedOuterFrame()
    {
        TestTransport transport = new(Array.Empty<byte[]>());
        GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
            CreateHandoff(transport, new byte[] { 1, 0, 0xff }));

        GameplayPumpResult result = acquired.Consumer!.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        False(result.IsSuccess);
        Equal(GameplayErrorCode.MalformedOuterFrame, result.Error);
        Equal(1, transport.CloseCallCount);
    }
}
