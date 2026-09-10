using System.Reflection;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I4PrivateOccurrenceSidecarTests
{
    internal static void TestKnownPileProjectionEmitsSidecar()
    {
        MirrorSnapshotV1 snapshot =
            CreateDuplicateOwnHandSnapshot(reverseInsertionOrder: false);
        PublicStateProjectionResultV1 publicOnlyProjection =
            PublicStateProjectionV1.TryProject(
                snapshot,
                new PublicStateProjectionContextV1(0));
        PublicStateProjectionResultV1 projection =
            CreateDuplicateOwnHandProjection(reverseInsertionOrder: false);
        True(publicOnlyProjection.IsSuccess, publicOnlyProjection.Error.ToString());
        True(projection.IsSuccess, projection.Error.ToString());
        Null(publicOnlyProjection.PrivateOccurrenceSidecar);
        BytesEqual(
            publicOnlyProjection.CanonicalBytes.Span,
            projection.CanonicalBytes.Span);
        Equal(
            publicOnlyProjection.PublicProjectionId,
            projection.PublicProjectionId);
        True(publicOnlyProjection.Snapshot!.Cards
            .Select(card => card.Locator.Value)
            .SequenceEqual(projection.Snapshot!.Cards
                .Select(card => card.Locator.Value)));
        NotNull(projection.Snapshot);
        NotNull(projection.PrivateOccurrenceSidecar);

        PrivateI4OccurrencePublicLocatorSidecarV1 sidecar =
            projection.PrivateOccurrenceSidecar!;
        Equal(0ul, sidecar.FrameInstanceOrdinal);
        Equal(projection.PublicProjectionId, sidecar.AcceptedPublicProjectionId);
        Equal(2, sidecar.Entries.Count);

        PrivateI4OccurrencePublicLocatorSidecarEntryV1[] entries =
            sidecar.Entries
                .OrderBy(entry => entry.SourceSequence)
                .ToArray();
        Equal(0u, entries[0].SourceSequence);
        Equal(1u, entries[1].SourceSequence);
        True(PublicSemanticLocatorV1.TryCreatePublicOrdinal(
            0,
            PublicSemanticZoneV1.Hand,
            0x11223344,
            0,
            out PublicSemanticLocatorV1? expectedFirstLocator));
        True(PublicSemanticLocatorV1.TryCreatePublicOrdinal(
            0,
            PublicSemanticZoneV1.Hand,
            0x11223344,
            1,
            out PublicSemanticLocatorV1? expectedSecondLocator));
        Equal(expectedFirstLocator, entries[0].AcceptedI4PublicLocator);
        Equal(expectedSecondLocator, entries[1].AcceptedI4PublicLocator);
        NotEqual(
            entries[0].AcceptedI4PublicLocator,
            entries[1].AcceptedI4PublicLocator);
        True(projection.Snapshot!.Cards.Any(card =>
            card.Locator == entries[0].AcceptedI4PublicLocator));
        True(projection.Snapshot.Cards.Any(card =>
            card.Locator == entries[1].AcceptedI4PublicLocator));

        True(sidecar.TryGet(
            0,
            MirrorZoneV1.Hand,
            0,
            isOverlay: false,
            overlayIndex: null,
            out PrivateI4OccurrencePublicLocatorSidecarEntryV1? first));
        True(sidecar.TryGet(
            0,
            MirrorZoneV1.Hand,
            1,
            isOverlay: false,
            overlayIndex: null,
            out PrivateI4OccurrencePublicLocatorSidecarEntryV1? second));
        Equal(entries[0].AcceptedI4PublicLocator, first!.AcceptedI4PublicLocator);
        Equal(entries[1].AcceptedI4PublicLocator, second!.AcceptedI4PublicLocator);
    }

    internal static void TestDuplicateSidecarPairingIsInsertionOrderIndependent()
    {
        PublicStateProjectionResultV1 forward =
            CreateDuplicateOwnHandProjection(reverseInsertionOrder: false);
        PublicStateProjectionResultV1 reverse =
            CreateDuplicateOwnHandProjection(reverseInsertionOrder: true);
        True(forward.IsSuccess, forward.Error.ToString());
        True(reverse.IsSuccess, reverse.Error.ToString());
        BytesEqual(forward.CanonicalBytes.Span, reverse.CanonicalBytes.Span);
        Equal(forward.PublicProjectionId, reverse.PublicProjectionId);
        NotNull(forward.PrivateOccurrenceSidecar);
        NotNull(reverse.PrivateOccurrenceSidecar);

        string[] forwardMapping = forward.PrivateOccurrenceSidecar!.Entries
            .OrderBy(entry => entry.SourceSequence)
            .Select(entry => entry.SourceSequence + ":" +
                entry.AcceptedI4PublicLocator.Value)
            .ToArray();
        string[] reverseMapping = reverse.PrivateOccurrenceSidecar!.Entries
            .OrderBy(entry => entry.SourceSequence)
            .Select(entry => entry.SourceSequence + ":" +
                entry.AcceptedI4PublicLocator.Value)
            .ToArray();
        True(forwardMapping.SequenceEqual(reverseMapping));

        string[] forwardPublicLocators = forward.Snapshot!.Cards
            .Select(card => card.Locator.Value)
            .ToArray();
        string[] reversePublicLocators = reverse.Snapshot!.Cards
            .Select(card => card.Locator.Value)
            .ToArray();
        True(forwardPublicLocators.SequenceEqual(reversePublicLocators));
    }

    internal static void TestNullPositionDuplicatePairingIsInsertionOrderIndependent()
    {
        PublicStateProjectionResultV1 forward =
            PublicStateProjectionV1.TryProject(
                CreateDuplicateOwnHandSnapshot(
                    reverseInsertionOrder: false,
                    unknownPositions: true),
                new PublicStateProjectionContextV1(0),
                frameInstanceOrdinal: 0);
        PublicStateProjectionResultV1 reverse =
            PublicStateProjectionV1.TryProject(
                CreateDuplicateOwnHandSnapshot(
                    reverseInsertionOrder: true,
                    unknownPositions: true),
                new PublicStateProjectionContextV1(0),
                frameInstanceOrdinal: 0);
        True(forward.IsSuccess, forward.Error.ToString());
        True(reverse.IsSuccess, reverse.Error.ToString());
        BytesEqual(forward.CanonicalBytes.Span, reverse.CanonicalBytes.Span);
        Equal(forward.PublicProjectionId, reverse.PublicProjectionId);
        NotNull(forward.PrivateOccurrenceSidecar);
        NotNull(reverse.PrivateOccurrenceSidecar);

        PrivateI4OccurrencePublicLocatorSidecarEntryV1[] forwardEntries =
            forward.PrivateOccurrenceSidecar!.Entries
                .OrderBy(entry => entry.SourceSequence)
                .ToArray();
        PrivateI4OccurrencePublicLocatorSidecarEntryV1[] reverseEntries =
            reverse.PrivateOccurrenceSidecar!.Entries
                .OrderBy(entry => entry.SourceSequence)
                .ToArray();
        Equal(2, forwardEntries.Length);
        Equal(2, reverseEntries.Length);
        Equal(0u, forwardEntries[0].SourceSequence);
        Equal(1u, forwardEntries[1].SourceSequence);
        True(forwardEntries.Select(entry =>
                entry.SourceSequence + ":" + entry.AcceptedI4PublicLocator.Value)
            .SequenceEqual(reverseEntries.Select(entry =>
                entry.SourceSequence + ":" + entry.AcceptedI4PublicLocator.Value)));
        True(PublicSemanticLocatorV1.TryCreatePublicOrdinal(
            0,
            PublicSemanticZoneV1.Hand,
            0x11223344,
            0,
            out PublicSemanticLocatorV1? expectedFirstLocator));
        True(PublicSemanticLocatorV1.TryCreatePublicOrdinal(
            0,
            PublicSemanticZoneV1.Hand,
            0x11223344,
            1,
            out PublicSemanticLocatorV1? expectedSecondLocator));
        Equal(expectedFirstLocator, forwardEntries[0].AcceptedI4PublicLocator);
        Equal(expectedSecondLocator, forwardEntries[1].AcceptedI4PublicLocator);
    }

    internal static void TestHiddenOpponentHandEmitsNoSidecarEntry()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                deckCount0: 0,
                extraCount0: 0,
                deckCount1: 1,
                extraCount1: 0);
        MirrorApplyResult draw = mirror.Apply(DecodeMessage(
            decoder,
            DrawMessage(1, (0x01020304u, 0x08u))));
        True(draw.IsSuccess, draw.Error.ToString());

        PublicStateProjectionResultV1 projection =
            PublicStateProjectionV1.TryProject(
                mirror.Snapshot,
                new PublicStateProjectionContextV1(0),
                frameInstanceOrdinal: 0);
        True(projection.IsSuccess, projection.Error.ToString());
        NotNull(projection.PrivateOccurrenceSidecar);
        Equal(0, projection.PrivateOccurrenceSidecar!.Entries.Count);
    }

    internal static void TestSidecarStoresNoPrivateIdentity()
    {
        Type[] forbiddenTypes =
        {
            typeof(MirrorEntityIdV1),
            typeof(ModernLocInfoV1)
        };
        FieldInfo[] fields = typeof(
            PrivateI4OccurrencePublicLocatorSidecarEntryV1).GetFields(
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public);
        False(fields.Any(field => ContainsType(field.FieldType, forbiddenTypes)));
        False(fields.Any(field => field.Name is
            "CardCode" or
            "PromptLocalCardCode" or
            "RawLocInfo" or
            "Pointer" or
            "ObjectHash"));
    }

    private static PublicStateProjectionResultV1
        CreateDuplicateOwnHandProjection(bool reverseInsertionOrder)
    {
        return PublicStateProjectionV1.TryProject(
            CreateDuplicateOwnHandSnapshot(reverseInsertionOrder),
            new PublicStateProjectionContextV1(0),
            frameInstanceOrdinal: 0);
    }

    private static MirrorSnapshotV1 CreateDuplicateOwnHandSnapshot(
        bool reverseInsertionOrder,
        bool unknownPositions = false)
    {
        const uint duplicateCardCode = 0x11223344;
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                deckCount0: 2,
                extraCount0: 0,
                deckCount1: 0,
                extraCount1: 0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        foreach (uint sequence in new[] { 0u, 1u })
        {
            MirrorApplyResult moved = mirror.Apply(DecodeMessage(
                decoder,
                MoveMessage(
                    duplicateCardCode,
                    empty,
                    new ModernLocInfoV1(0, 0x02, sequence, 0x08),
                    0)));
            True(moved.IsSuccess, moved.Error.ToString());
        }

        MirrorSnapshotV1 snapshot = mirror.Snapshot;
        IEnumerable<MirrorCardSnapshotV1> cards = snapshot.Cards;
        if (unknownPositions)
        {
            cards = cards.Select(card => new MirrorCardSnapshotV1(
                card.EntityId,
                card.Controller,
                card.Owner,
                card.Zone,
                card.Sequence,
                card.IsOverlay,
                card.OverlayIndex,
                MirrorValueV1.Unknown<uint>(),
                card.CardCode,
                card.QueryFields));
        }

        if (reverseInsertionOrder)
        {
            snapshot = new MirrorSnapshotV1(
                snapshot.Perspective,
                snapshot.Participants,
                cards.Reverse(),
                snapshot.TurnCount,
                snapshot.TurnPlayer,
                snapshot.Phase,
                snapshot.Terminal,
                snapshot.PendingChain,
                snapshot.Chains,
                snapshot.TargetRelations,
                snapshot.ChainTargetRelations,
                snapshot.EquipmentRelations,
                snapshot.OverlayRelations,
                snapshot.PendingChainSource);
        }
        else if (unknownPositions)
        {
            snapshot = new MirrorSnapshotV1(
                snapshot.Perspective,
                snapshot.Participants,
                cards,
                snapshot.TurnCount,
                snapshot.TurnPlayer,
                snapshot.Phase,
                snapshot.Terminal,
                snapshot.PendingChain,
                snapshot.Chains,
                snapshot.TargetRelations,
                snapshot.ChainTargetRelations,
                snapshot.EquipmentRelations,
                snapshot.OverlayRelations,
                snapshot.PendingChainSource);
        }

        return snapshot;
    }

    private static bool ContainsType(Type value, IReadOnlyList<Type> forbidden)
    {
        if (forbidden.Contains(value))
        {
            return true;
        }

        if (value.IsArray)
        {
            return ContainsType(value.GetElementType()!, forbidden);
        }

        return value.IsGenericType &&
            value.GetGenericArguments().Any(argument =>
                ContainsType(argument, forbidden));
    }
}
