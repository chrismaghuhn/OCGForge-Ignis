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

internal static class GameplayMessageFixtures
{
    internal static byte[] CreateStartBytes(
        byte playerType,
        ushort deckCount0 = 40,
        ushort extraCount0 = 15,
        ushort deckCount1 = 41,
        ushort extraCount1 = 16)
    {
        byte[] bytes = new byte[18];
        bytes[0] = 4;
        bytes[1] = playerType;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2, 4), 8000);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(6, 4), 7000);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(10, 2), deckCount0);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12, 2), extraCount0);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(14, 2), deckCount1);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(16, 2), extraCount1);
        return bytes;
    }

    internal static GameplayMessageV1 DecodeMessage(
        GameplayMessageDecoderV1 decoder,
        byte[] bytes)
    {
        GameplayMessageDecodeResult result = decoder.Decode(
            new StocGameMessagePayload(bytes));
        True(result.IsSuccess, result.Error.ToString());
        NotNull(result.Message);
        return result.Message!;
    }

    internal static byte[] U32(uint value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        return bytes;
    }

    internal static byte[] I32(int value)
    {
        byte[] bytes = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        return bytes;
    }

    internal static byte[] U64(ulong value)
    {
        byte[] bytes = new byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        return bytes;
    }

    internal static byte[] LocInfo(byte controller, byte location, uint sequence, uint position)
    {
        byte[] bytes = new byte[10];
        bytes[0] = controller;
        bytes[1] = location;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2, 4), sequence);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(6, 4), position);
        return bytes;
    }

    internal static byte[] UpdateCardMessage(
        byte player,
        byte location,
        byte sequence,
        ModernQueryV1 query) =>
        Join(
            new byte[] { 7, player, location, sequence },
            query.RawBytes.ToArray());

    internal static byte[] UpdateDataMessage(
        byte player,
        byte location,
        byte[] queryBody) =>
        Join(new byte[] { 6, player, location }, U32((uint)queryBody.Length), queryBody);

    internal static byte[] MoveMessage(
        uint cardCode,
        ModernLocInfoV1 previous,
        ModernLocInfoV1 current,
        uint reason) =>
        Join(
            new byte[] { 50 },
            U32(cardCode),
            LocInfo(previous.Controller, previous.Location, previous.Sequence, previous.Position),
            LocInfo(current.Controller, current.Location, current.Sequence, current.Position),
            U32(reason));

    internal static byte[] PosChangeMessage(
        byte controller,
        byte location,
        byte sequence,
        byte previousPosition,
        byte currentPosition) =>
        Join(
            new byte[] { 53 },
            U32(0),
            new byte[] { controller, location, sequence, previousPosition, currentPosition });

    internal static byte[] SetMessage(uint cardCode, ModernLocInfoV1 location) =>
        Join(
            new byte[] { 54 },
            U32(cardCode),
            LocInfo(location.Controller, location.Location, location.Sequence, location.Position));

    internal static byte[] SwapMessage(ModernLocInfoV1 first, ModernLocInfoV1 second) =>
        Join(
            new byte[] { 55 },
            U32(0),
            LocInfo(first.Controller, first.Location, first.Sequence, first.Position),
            U32(0),
            LocInfo(second.Controller, second.Location, second.Sequence, second.Position));

    internal static byte[] CardTargetMessage(
        ModernLocInfoV1 source,
        ModernLocInfoV1 target,
        bool cancel = false) =>
        Join(
            new byte[] { (byte)(cancel ? 97 : 96) },
            LocInfo(source.Controller, source.Location, source.Sequence, source.Position),
            LocInfo(target.Controller, target.Location, target.Sequence, target.Position));

    internal static byte[] EquipMessage(ModernLocInfoV1 card, ModernLocInfoV1 target) =>
        Join(
            new byte[] { 93 },
            LocInfo(card.Controller, card.Location, card.Sequence, card.Position),
            LocInfo(target.Controller, target.Location, target.Sequence, target.Position));

    internal static byte[] UnequipMessage(ModernLocInfoV1 card) =>
        Join(
            new byte[] { 95 },
            LocInfo(card.Controller, card.Location, card.Sequence, card.Position));

    internal static byte[] ChainingMessage(
        ModernLocInfoV1 card,
        uint chainSize,
        uint cardCode = 0x11223344) =>
        Join(
            new byte[] { 70 },
            U32(cardCode),
            LocInfo(card.Controller, card.Location, card.Sequence, card.Position),
            new byte[] { card.Controller, card.Location },
            U32(card.Sequence),
            U64(0x0102030405060708),
            U32(chainSize));

    internal static byte[] BecomeTargetMessage(params ModernLocInfoV1[] targets)
    {
        List<byte[]> parts = new() { new byte[] { 83 }, U32((uint)targets.Length) };
        parts.AddRange(
            targets.Select(target =>
                LocInfo(target.Controller, target.Location, target.Sequence, target.Position)));
        return Join(parts.ToArray());
    }

    internal static byte[] DrawMessage(
        byte player,
        params (uint Code, uint Position)[] cards)
    {
        List<byte[]> parts = new() { new byte[] { 90, player }, U32((uint)cards.Length) };
        parts.AddRange(cards.Select(card => Join(U32(card.Code), U32(card.Position))));
        return Join(parts.ToArray());
    }

    internal static byte[] Join(params byte[][] parts)
    {
        int length = 0;
        foreach (byte[] part in parts)
        {
            length = checked(length + part.Length);
        }

        byte[] result = new byte[length];
        int offset = 0;
        foreach (byte[] part in parts)
        {
            part.CopyTo(result, offset);
            offset += part.Length;
        }

        return result;
    }
}
