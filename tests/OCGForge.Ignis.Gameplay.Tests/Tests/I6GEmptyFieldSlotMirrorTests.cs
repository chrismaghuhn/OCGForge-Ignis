using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.ModernQueryFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6GEmptyFieldSlotMirrorTests
{
    internal static void TestMissingMonsterSkippedFieldSlotIsNoOp()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        string before = mirror.Snapshot.ToDeterministicString();
        int eventCountBefore = mirror.VisibleEvents.Count;
        ulong nextEventIndexBefore = mirror.NextEventIndex;

        MirrorApplyResult result = mirror.Apply(DecodeMessage(
            decoder,
            UpdateDataMessage(0, 0x04, SkippedQuery())));

        True(result.IsSuccess, result.Error.ToString());
        Equal(before, mirror.Snapshot.ToDeterministicString());
        Equal(eventCountBefore, mirror.VisibleEvents.Count);
        Equal(nextEventIndexBefore, mirror.NextEventIndex);
    }

    internal static void TestMissingSpellTrapSkippedFieldSlotIsNoOp()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        string before = mirror.Snapshot.ToDeterministicString();
        int eventCountBefore = mirror.VisibleEvents.Count;
        ulong nextEventIndexBefore = mirror.NextEventIndex;

        MirrorApplyResult result = mirror.Apply(DecodeMessage(
            decoder,
            UpdateDataMessage(0, 0x08, SkippedQuery())));

        True(result.IsSuccess, result.Error.ToString());
        Equal(before, mirror.Snapshot.ToDeterministicString());
        Equal(eventCountBefore, mirror.VisibleEvents.Count);
        Equal(nextEventIndexBefore, mirror.NextEventIndex);
    }

    internal static void TestExistingMonsterSkippedFieldSlotConflicts()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateFieldEntity(0x04);
        string before = mirror.Snapshot.ToDeterministicString();

        MirrorApplyResult result = mirror.Apply(DecodeMessage(
            decoder,
            UpdateDataMessage(0, 0x04, SkippedQuery())));

        False(result.IsSuccess);
        Equal(GameplayErrorCode.ConflictingSlotOccupancy, result.Error);
        Equal(before, mirror.Snapshot.ToDeterministicString());
    }

    internal static void TestExistingSpellTrapSkippedFieldSlotConflicts()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateFieldEntity(0x08);
        string before = mirror.Snapshot.ToDeterministicString();

        MirrorApplyResult result = mirror.Apply(DecodeMessage(
            decoder,
            UpdateDataMessage(0, 0x08, SkippedQuery())));

        False(result.IsSuccess);
        Equal(GameplayErrorCode.ConflictingSlotOccupancy, result.Error);
        Equal(before, mirror.Snapshot.ToDeterministicString());
    }

    internal static void TestMissingMonsterNonSkippedFieldSlotFailsClosed()
    {
        AssertMissingNonSkippedFieldSlot(0x04);
    }

    internal static void TestMissingSpellTrapNonSkippedFieldSlotFailsClosed()
    {
        AssertMissingNonSkippedFieldSlot(0x08);
    }

    internal static void TestMissingHandSkippedFieldSlotFailsClosed()
    {
        AssertMissingPileSkippedFieldSlot(0x02);
    }

    internal static void TestMissingGraveyardSkippedFieldSlotFailsClosed()
    {
        AssertMissingPileSkippedFieldSlot(0x10);
    }

    internal static void TestMissingBanishedSkippedFieldSlotFailsClosed()
    {
        AssertMissingPileSkippedFieldSlot(0x20);
    }

    internal static void TestMissingMainDeckSkippedFieldSlotFailsClosed()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0, deckCount0: 1);
        MirrorApplyResult result = mirror.Apply(DecodeMessage(
            decoder,
            UpdateDataMessage(0, 0x01, SkippedQuery())));

        False(result.IsSuccess);
        Equal(GameplayErrorCode.UnknownMirrorReference, result.Error);
    }

    private static void AssertMissingNonSkippedFieldSlot(byte location)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        string before = mirror.Snapshot.ToDeterministicString();

        MirrorApplyResult result = mirror.Apply(DecodeMessage(
            decoder,
            UpdateDataMessage(0, location, QueryEnd())));

        False(result.IsSuccess);
        Equal(GameplayErrorCode.UnknownMirrorReference, result.Error);
        Equal(before, mirror.Snapshot.ToDeterministicString());
    }

    private static void AssertMissingPileSkippedFieldSlot(byte location)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        string before = mirror.Snapshot.ToDeterministicString();

        MirrorApplyResult result = mirror.Apply(DecodeMessage(
            decoder,
            UpdateDataMessage(0, location, SkippedQuery())));

        False(result.IsSuccess);
        Equal(GameplayErrorCode.UnknownMirrorReference, result.Error);
        Equal(before, mirror.Snapshot.ToDeterministicString());
    }

    private static (PerspectiveStateMirrorV1 Mirror, GameplayMessageDecoderV1 Decoder)
        CreateFieldEntity(byte location)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        True(mirror.Apply(DecodeMessage(
            decoder,
            MoveMessage(
                0x12345678,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, location, 0, 0x04),
                0))).IsSuccess);
        return (mirror, decoder);
    }

    private static byte[] SkippedQuery() => new byte[] { 0, 0 };
}
