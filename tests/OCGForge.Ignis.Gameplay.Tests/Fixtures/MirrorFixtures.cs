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

internal static class MirrorFixtures
{
    internal static (PerspectiveStateMirrorV1 Mirror, GameplayMessageDecoderV1 Decoder)
        CreateMirror(
            byte playerType,
            ushort deckCount0 = 2,
            ushort extraCount0 = 1,
            ushort deckCount1 = 2,
            ushort extraCount1 = 1)
    {
        GameplayMessageDecoderV1 decoder = new();
        GameplayMessageDecodeResult start = decoder.Decode(
            new StocGameMessagePayload(
                CreateStartBytes(
                    playerType,
                    deckCount0,
                    extraCount0,
                    deckCount1,
                    extraCount1)));
        True(start.IsSuccess, start.Error.ToString());
        MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
            start.Message!,
            start.Perspective!);
        True(created.IsSuccess, created.Error.ToString());
        return (created.Mirror!, decoder);
    }

    internal static string RunMirrorTranscript(byte[][] chunks)
    {
        TestTransport transport = new(chunks);
        GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
            CreateHandoff(transport, Array.Empty<byte>()));
        True(acquired.IsSuccess);
        GameplayPumpResult start = acquired.Consumer!.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(start.IsSuccess, start.Error.ToString());
        MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
            start.Message!,
            start.Perspective!);
        True(created.IsSuccess, created.Error.ToString());

        GameplayMirrorSessionV1 session = new(start.Session!, created.Mirror!);
        GameplayMirrorPumpResult move = session.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(move.IsSuccess, move.Error.ToString());
        GameplayMirrorPumpResult turn = session.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(turn.IsSuccess, turn.Error.ToString());
        GameplayMirrorPumpResult phase = session.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(phase.IsSuccess, phase.Error.ToString());
        string result = session.Mirror.Snapshot.ToDeterministicString();
        session.DisposeAsync().GetAwaiter().GetResult();
        acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        return result;
    }
}
