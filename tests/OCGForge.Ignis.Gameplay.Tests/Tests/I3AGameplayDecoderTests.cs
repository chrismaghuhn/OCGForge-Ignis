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

internal static class I3AGameplayDecoderTests
{
    internal static void TestStartSelfIsPlayer0()
    {
        GameplayMessageDecoderV1 decoder = new();
        GameplayMessageDecodeResult result = decoder.Decode(
            new StocGameMessagePayload(CreateStartBytes(0x00)));

        True(result.IsSuccess);
        Equal(GameplayErrorCode.None, result.Error);
        NotNull(result.Message);
        NotNull(result.Perspective);
        Equal(GameplayPerspectiveKind.SelfIsPlayer0, result.Perspective!.Kind);
        Equal((byte)0x00, result.Message!.Start.PlayerType);
        Equal((byte)4, GameplayMessageV1.MessageId);
        Equal(8000u, result.Message.Start.LifePoints0);
        Equal(7000u, result.Message.Start.LifePoints1);
        Equal((ushort)40, result.Message.Start.DeckCount0);
        Equal((ushort)15, result.Message.Start.ExtraCount0);
        Equal((ushort)41, result.Message.Start.DeckCount1);
        Equal((ushort)16, result.Message.Start.ExtraCount1);
    }

    internal static void TestStartSelfIsPlayer1()
    {
        GameplayMessageDecoderV1 decoder = new();
        GameplayMessageDecodeResult result = decoder.Decode(
            new StocGameMessagePayload(CreateStartBytes(0x01)));

        True(result.IsSuccess);
        Equal(GameplayPerspectiveKind.SelfIsPlayer1, result.Perspective!.Kind);
        Equal(GameplayPerspectiveKind.SelfIsPlayer1, decoder.Perspective!.Kind);
    }

    internal static void TestStartLength()
    {
        byte[] valid = CreateStartBytes(0x00);
        for (int length = 0; length < valid.Length; length++)
        {
            GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(
                new StocGameMessagePayload(valid.AsSpan(0, length)));
            False(result.IsSuccess);
            Equal(GameplayErrorCode.MalformedGameMessage, result.Error);
        }

        byte[] trailing = new byte[valid.Length + 1];
        valid.CopyTo(trailing, 0);
        trailing[^1] = 0xaa;
        GameplayMessageDecodeResult withTrailing = new GameplayMessageDecoderV1().Decode(
            new StocGameMessagePayload(trailing));
        False(withTrailing.IsSuccess);
        Equal(GameplayErrorCode.MalformedGameMessage, withTrailing.Error);

        True(!Enum.GetNames<GameplayErrorCode>().Contains("NeedMoreData", StringComparer.Ordinal));
    }

    internal static void TestRoleRejection()
    {
        foreach (byte observer in new byte[] { 0x10, 0x11 })
        {
            GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(
                new StocGameMessagePayload(CreateStartBytes(observer)));
            False(result.IsSuccess);
            Equal(GameplayErrorCode.UnsupportedPerspective, result.Error);
        }

        foreach (byte invalid in new byte[] { 0x02, 0xff })
        {
            GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(
                new StocGameMessagePayload(CreateStartBytes(invalid)));
            False(result.IsSuccess);
            Equal(GameplayErrorCode.InvalidPerspectiveRole, result.Error);
        }
    }

    internal static void TestDuplicateAndConflict()
    {
        GameplayMessageDecoderV1 decoder = new();
        True(decoder.Decode(new StocGameMessagePayload(CreateStartBytes(0x00))).IsSuccess);

        GameplayMessageDecodeResult duplicate = decoder.Decode(
            new StocGameMessagePayload(CreateStartBytes(0x00)));
        False(duplicate.IsSuccess);
        Equal(GameplayErrorCode.DuplicatePerspective, duplicate.Error);

        GameplayMessageDecodeResult conflict = decoder.Decode(
            new StocGameMessagePayload(CreateStartBytes(0x01)));
        False(conflict.IsSuccess);
        Equal(GameplayErrorCode.ConflictingPerspective, conflict.Error);
        Equal(GameplayPerspectiveKind.SelfIsPlayer0, decoder.Perspective!.Kind);
    }

    internal static void TestUnsupportedMessages()
    {
        GameplayMessageDecoderV1 decoder = new();
        GameplayMessageDecodeResult dependent = decoder.Decode(
            new StocGameMessagePayload(new byte[] { 6, 0, 0 }));
        False(dependent.IsSuccess);
        Equal(GameplayErrorCode.PerspectiveNotEstablished, dependent.Error);
        Null(decoder.Perspective);

        GameplayMessageDecodeResult tooLate = decoder.Decode(
            new StocGameMessagePayload(CreateStartBytes(0x00)));
        False(tooLate.IsSuccess);
        Equal(GameplayErrorCode.PerspectiveEstablishmentTooLate, tooLate.Error);

        GameplayMessageDecodeResult unknown = new GameplayMessageDecoderV1().Decode(
            new StocGameMessagePayload(new byte[] { 0xff }));
        False(unknown.IsSuccess);
        Equal(GameplayErrorCode.UnknownMessageId, unknown.Error);

        GameplayMessageDecodeResult unsupported = new GameplayMessageDecoderV1().Decode(
            new StocGameMessagePayload(new byte[] { 3 }));
        False(unsupported.IsSuccess);
        Equal(GameplayErrorCode.UnsupportedMessage, unsupported.Error);
    }

    internal static void TestModernLocInfo()
    {
        byte[] bytes = new byte[10];
        bytes[0] = 1;
        bytes[1] = 0x80;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2, 4), 0x11223344);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(6, 4), 0x55667788);

        True(GameplayWirePrimitivesV1.TryDecodeModernLocInfo(
            bytes,
            out ModernLocInfoV1 value,
            out GameplayErrorCode error));
        Equal(GameplayErrorCode.None, error);
        Equal((byte)1, value.Controller);
        Equal((byte)0x80, value.Location);
        Equal(0x11223344u, value.Sequence);
        Equal(0x55667788u, value.Position);

        False(GameplayWirePrimitivesV1.TryDecodeModernLocInfo(
            bytes.AsSpan(0, 9),
            out _,
            out error));
        Equal(GameplayErrorCode.MalformedGameMessage, error);
    }

    internal static void TestPrivacyBoundary()
    {
        GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(
            new StocGameMessagePayload(CreateStartBytes(0x00)));
        True(result.IsSuccess);

        string[] forbidden =
        {
            "socket", "endpoint", "password", "credential", "pid", "thread",
            "timestamp", "wall", "pointer", "object", "runtime", "engine",
            "locator", "CoreHost", "hidden"
        };
        AssertDoesNotContainForbidden(result.ToString(), forbidden);
        AssertDoesNotContainForbidden(result.Message!.ToString(), forbidden);
        AssertDoesNotContainForbidden(result.Perspective!.ToString(), forbidden);
        False(typeof(MirrorEntityIdV1).IsPublic);
        Null(typeof(MirrorCardSnapshotV1).GetProperty(
            "EntityId",
            BindingFlags.Instance | BindingFlags.Public));
        Null(typeof(MirrorSnapshotV1).GetProperty(
            "PendingChain",
            BindingFlags.Instance | BindingFlags.Public));
        Null(typeof(MirrorSnapshotV1).GetProperty(
            "TargetRelations",
            BindingFlags.Instance | BindingFlags.Public));
    }

    internal static void TestValueOwnership()
    {
        byte[] bytes = CreateStartBytes(0x00);
        StocGameMessagePayload payload = new(bytes);
        bytes[1] = 0x01;
        GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(payload);
        True(result.IsSuccess);
        Equal((byte)0x00, result.Message!.Start.PlayerType);
    }
}
