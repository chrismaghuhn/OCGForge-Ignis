using System.Buffers.Binary;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6C6CurrentCounterTests
{
    internal static void TestCounterAddRemovePublishesCurrent()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreatePublicMonsterMirror(0);

        Equal(
            0,
            GetCurrentCounters(mirror, 0, 0).Count);

        ApplyCounter(mirror, decoder, 101, 7, 0, 0x04, 0, 3);
        Equal(
            3u,
            GetCurrentCounters(mirror, 0, 0).Single(counter => counter.Type == 7).Count);

        ApplyCounter(mirror, decoder, 101, 7, 0, 0x04, 0, 2);
        Equal(
            5u,
            GetCurrentCounters(mirror, 0, 0).Single(counter => counter.Type == 7).Count);

        ApplyCounter(mirror, decoder, 101, 9, 0, 0x04, 0, 4);
        Equal(
            2,
            GetCurrentCounters(mirror, 0, 0).Count);

        ApplyCounter(mirror, decoder, 102, 7, 0, 0x04, 0, 4);
        Equal(
            1u,
            GetCurrentCounters(mirror, 0, 0).Single(counter => counter.Type == 7).Count);

        ApplyCounter(mirror, decoder, 102, 7, 0, 0x04, 0, 1);
        False(GetCurrentCounters(mirror, 0, 0).Any(counter => counter.Type == 7));
        Equal(
            4u,
            GetCurrentCounters(mirror, 0, 0).Single(counter => counter.Type == 9).Count);
    }

    internal static void TestCounterResetAndControlSemantics()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreatePublicMonsterMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 selfMonster = new(0, 0x04, 0, 0x04);
        ModernLocInfoV1 opponentMonster = new(1, 0x04, 0, 0x04);
        ModernLocInfoV1 opponentGrave = new(1, 0x10, 0, 0x04);

        ApplyCounter(mirror, decoder, 101, 5, 0, 0x04, 0, 2);
        True(mirror.Apply(DecodeMessage(
            decoder,
            MoveMessage(0, selfMonster, opponentMonster, 0))).IsSuccess);
        Equal(
            2u,
            GetCurrentCounters(mirror, 1, 0).Single(counter => counter.Type == 5).Count);

        MirrorApplyResult graveMove = mirror.Apply(DecodeMessage(
            decoder,
            MoveMessage(0, opponentMonster, opponentGrave, 0)));
        True(graveMove.IsSuccess, "grave move: " + graveMove.Error);
        Equal(0, GetCurrentCounters(mirror, 1, 0, "GRAVEYARD").Count);

        MirrorApplyResult recreated = mirror.Apply(DecodeMessage(
            decoder,
            MoveMessage(0, empty, new ModernLocInfoV1(0, 0x04, 0, 0x04), 0)));
        True(recreated.IsSuccess, "recreate: " + recreated.Error);
        ApplyCounter(mirror, decoder, 101, 11, 0, 0x04, 0, 1);
        True(mirror.Apply(DecodeMessage(
            decoder,
            MoveMessage(
                0,
                new ModernLocInfoV1(0, 0x04, 0, 0x04),
                new ModernLocInfoV1(0, 0x08, 0, 0x04),
                0))).IsSuccess);
        MirrorCardSnapshotV1 changedZoneCard = mirror.Snapshot.Cards.Single(
            card => card.Zone == MirrorZoneV1.SpellTrapZone &&
                    card.Sequence == 0);
        False(changedZoneCard.QueryFields.Any(
            field => field.Flag == QueryFlagV1.Counters));
    }

    internal static void TestCounterTurnSetAndOverlayResets()
    {
        (PerspectiveStateMirrorV1 setMirror, GameplayMessageDecoderV1 setDecoder) =
            CreatePublicMonsterMirror(0);
        ApplyCounter(setMirror, setDecoder, 101, 12, 0, 0x04, 0, 1);
        True(setMirror.Apply(DecodeMessage(
            setDecoder,
            SetMessage(
                0,
                new ModernLocInfoV1(0, 0x04, 0, 0x08)))).IsSuccess);
        Equal(0, GetCurrentCounters(setMirror, 0, 0).Count);

        (PerspectiveStateMirrorV1 overlayMirror, GameplayMessageDecoderV1 overlayDecoder) =
            CreatePublicMonsterMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 parent = new(0, 0x04, 0, 0x04);
        ModernLocInfoV1 material = new(0, 0x04, 1, 0x04);
        True(overlayMirror.Apply(DecodeMessage(
            overlayDecoder,
            MoveMessage(0x2222, empty, material, 0))).IsSuccess);
        ApplyCounter(overlayMirror, overlayDecoder, 101, 13, 0, 0x04, 1, 2);
        True(overlayMirror.Apply(DecodeMessage(
            overlayDecoder,
            MoveMessage(
                0,
                material,
                new ModernLocInfoV1(0, 0x84, 0, 0),
                0))).IsSuccess);
        False(overlayMirror.Snapshot.Cards.Any(
            card => card.IsOverlay &&
                    card.QueryFields.Any(field => field.Flag == QueryFlagV1.Counters)));

        Equal(0, GetCurrentCounters(overlayMirror, 0, 0).Count);
    }

    internal static void TestCounterUnknownEntityFailsAtomically()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        string before = mirror.Snapshot.ToDeterministicString();

        MirrorApplyResult result = mirror.Apply(DecodeMessage(
            decoder,
            CounterMessage(101, 7, 0, 0x04, 0, 1)));

        False(result.IsSuccess);
        Equal(GameplayErrorCode.UnknownMirrorReference, result.Error);
        Equal(before, mirror.Snapshot.ToDeterministicString());
    }

    internal static void TestCounterHiddenEntityFailsClosed()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        True(mirror.Apply(DecodeMessage(
            decoder,
            MoveMessage(
                0,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(1, 0x04, 0, 0x08),
                0))).IsSuccess);
        string before = mirror.Snapshot.ToDeterministicString();

        MirrorApplyResult result = mirror.Apply(DecodeMessage(
            decoder,
            CounterMessage(101, 7, 1, 0x04, 0, 1)));

        False(result.IsSuccess);
        Equal(GameplayErrorCode.InvalidStateTransition, result.Error);
        Equal(before, mirror.Snapshot.ToDeterministicString());
    }

    internal static void TestCounterPerspectiveOneUsesAbsoluteAddress()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreatePublicMonsterMirror(1);
        ApplyCounter(mirror, decoder, 101, 21, 1, 0x04, 0, 3);

        Equal(
            3u,
            GetCurrentCounters(mirror, 1, 0).Single(counter => counter.Type == 21).Count);
        Equal(
            MirrorParticipantRoleV1.Self,
            mirror.Snapshot.Cards.Single().Controller);
    }

    private static (PerspectiveStateMirrorV1 Mirror, GameplayMessageDecoderV1 Decoder)
        CreatePublicMonsterMirror(byte perspective)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(perspective);
        True(mirror.Apply(DecodeMessage(
            decoder,
            MoveMessage(
                0x12345678,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(perspective, 0x04, 0, 0x04),
                0))).IsSuccess);
        return (mirror, decoder);
    }

    private static IReadOnlyList<PerspectiveSafeCounterV1> GetCurrentCounters(
        PerspectiveStateMirrorV1 mirror,
        byte absoluteController,
        byte sequence,
        string zone = "MONSTER_ZONE")
    {
        PerspectiveSafeI6C2SourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C2(mirror);
        True(result.IsSuccess, result.Error?.ToString() ?? "I6C2 source failed");
        NotNull(result.Source);
        string locator = $"p{absoluteController}:{zone}:{sequence}";
        PerspectiveSafeEntityV1? entity = result.Source!.Entities.SingleOrDefault(
            candidate => candidate.Locator == locator);
        True(entity is not null, "missing " + locator);
        NotNull(entity!.Current);
        return entity.Current!.Counters;
    }

    private static void ApplyCounter(
        PerspectiveStateMirrorV1 mirror,
        GameplayMessageDecoderV1 decoder,
        byte messageId,
        ushort counterType,
        byte controller,
        byte location,
        byte sequence,
        ushort count)
    {
        MirrorApplyResult result = mirror.Apply(DecodeMessage(
            decoder,
            CounterMessage(
                messageId,
                counterType,
                controller,
                location,
                sequence,
                count)));
        True(result.IsSuccess, result.Error.ToString());
    }

    private static byte[] CounterMessage(
        byte messageId,
        ushort counterType,
        byte controller,
        byte location,
        byte sequence,
        ushort count)
    {
        byte[] result = new byte[8];
        result[0] = messageId;
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(1, 2), counterType);
        result[3] = controller;
        result[4] = location;
        result[5] = sequence;
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(6, 2), count);
        return result;
    }
}
