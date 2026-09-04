using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.ModernQueryFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I3CPublicStateProjectionTests
{
    internal static void TestCorePerspectiveState()
    {
        foreach (byte playerType in new byte[] { 0, 1 })
        {
            (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
                CreateMirror(playerType);
            ModernLocInfoV1 empty = new(0, 0, 0, 0);
            Apply(
                mirror,
                decoder,
                MoveMessage(
                    0x11223344,
                    empty,
                    new ModernLocInfoV1(playerType, 0x04, 0, 0x04),
                    0));
            Apply(mirror, decoder, new byte[] { 40, 1 });
            Apply(mirror, decoder, new byte[] { 41, 4, 0 });

            PublicStateProjectionResultV1 result = Project(mirror);
            PublicStateSnapshotV1 snapshot = result.Snapshot!;
            Equal(
                "ocgforge-ignis.public-state-projection.v1",
                snapshot.ContractId);
            Equal(playerType, snapshot.PerspectivePlayer);
            Equal(0ul, snapshot.DuelFlags);
            Equal(1ul, snapshot.TurnCount);
            Equal((byte)1, snapshot.TurnPlayer);
            Equal((ushort)4, snapshot.Phase);
            False(snapshot.Terminal);
            Equal(2, snapshot.Participants.Count);
            Equal((byte)0, snapshot.Participants[0].AbsolutePlayer);
            Equal((byte)1, snapshot.Participants[1].AbsolutePlayer);
            Equal(8000u, snapshot.Participants[0].LifePoints);
            Equal(7000u, snapshot.Participants[1].LifePoints);
            Equal(2u, snapshot.Participants[0].MainDeckCount);
            Equal(1u, snapshot.Participants[0].ExtraDeckCount);
            Equal(2u, snapshot.Participants[1].MainDeckCount);
            Equal(1u, snapshot.Participants[1].ExtraDeckCount);
            Equal(1, snapshot.Cards.Count);
            Equal(
                playerType == 0
                    ? "p0:MONSTER_ZONE:0"
                    : "p1:MONSTER_ZONE:0",
                snapshot.Cards[0].Locator.Value);
        }
    }

    internal static void TestKnowledgeProjection()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        Apply(
            mirror,
            decoder,
            MoveMessage(
                0x11223344,
                empty,
                new ModernLocInfoV1(0, 0x02, 0, 0x08),
                0));
        Apply(
            mirror,
            decoder,
            MoveMessage(
                0x55667788,
                empty,
                new ModernLocInfoV1(1, 0x04, 0, 0x08),
                0));
        Apply(
            mirror,
            decoder,
            MoveMessage(
                0x99aabbcc,
                empty,
                new ModernLocInfoV1(1, 0x04, 1, 0x04),
                0));

        PublicStateSnapshotV1 snapshot = Project(mirror).Snapshot!;
        PublicCardStateV1 ownHand = snapshot.Cards.Single(
            card => card.Zone == PublicSemanticZoneV1.Hand);
        PublicCardStateV1 hiddenOpponent = snapshot.Cards.Single(
            card => card.Locator.Value == "p1:MONSTER_ZONE:0");
        PublicCardStateV1 publicOpponent = snapshot.Cards.Single(
            card => card.Locator.Value == "p1:MONSTER_ZONE:1");
        Equal(0, ownHand.AbsolutePlayer);
        Equal(0x11223344u, ownHand.CardCode);
        Equal(0x99aabbccu, publicOpponent.CardCode);
        Null(hiddenOpponent.CardCode);
        Equal(0x08u, hiddenOpponent.Position);
        AssertDoesNotContainForbidden(
            Encoding.UTF8.GetString(Project(mirror).CanonicalBytes.Span),
            new[] { "1432778632", "55667788" });
    }

    internal static void TestHiddenPopulationProjection()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        Apply(
            mirror,
            decoder,
            MoveMessage(
                0x11112222,
                empty,
                new ModernLocInfoV1(1, 0x02, 0, 0x08),
                0));
        Apply(
            mirror,
            decoder,
            MoveMessage(
                0x33334444,
                empty,
                new ModernLocInfoV1(1, 0x40, 0, 0x08),
                0));

        PublicStateProjectionResultV1 result = Project(mirror);
        PublicStateSnapshotV1 snapshot = result.Snapshot!;
        PublicParticipantStateV1 opponent = snapshot.Participants[1];
        Equal(2u, opponent.MainDeckCount);
        Equal(1u, opponent.HandCount);
        Equal(2u, opponent.ExtraDeckCount);
        False(snapshot.Cards.Any(card => card.Zone == PublicSemanticZoneV1.Hand));
        False(snapshot.Cards.Any(card => card.Zone == PublicSemanticZoneV1.ExtraDeck));
        string canonical = Encoding.UTF8.GetString(result.CanonicalBytes.Span);
        AssertDoesNotContainForbidden(
            canonical,
            new[] { ":MAIN_DECK:", ":unknown", "286335522", "858997828" });
    }

    internal static void TestSemanticLocatorIndependence()
    {
        (PerspectiveStateMirrorV1 first, GameplayMessageDecoderV1 firstDecoder) =
            CreateMirror(0);
        (PerspectiveStateMirrorV1 second, GameplayMessageDecoderV1 secondDecoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        ModernLocInfoV1 firstSlot = new(0, 0x04, 0, 0x04);
        ModernLocInfoV1 secondSlot = new(0, 0x04, 1, 0x04);
        Apply(first, firstDecoder, MoveMessage(0xaaaabbbb, empty, firstSlot, 0));
        Apply(first, firstDecoder, MoveMessage(0xccccdddd, empty, secondSlot, 0));
        Apply(first, firstDecoder, MoveMessage(0x12345678, empty, new ModernLocInfoV1(0, 0x02, 0, 0x08), 0));
        Equal(1u, first.Snapshot.GetZone(
            MirrorParticipantRoleV1.Self,
            MirrorZoneV1.Hand).Count.Value);
        Apply(first, firstDecoder, MoveMessage(0x12345678, empty, new ModernLocInfoV1(0, 0x02, 1, 0x08), 0));
        Apply(
            first,
            firstDecoder,
            MoveMessage(
                0xdeadbeef,
                empty,
                new ModernLocInfoV1(0, 0x84, 0, 0),
                0));

        Apply(second, secondDecoder, MoveMessage(0xccccdddd, empty, secondSlot, 0));
        Apply(second, secondDecoder, MoveMessage(0xaaaabbbb, empty, firstSlot, 0));
        Apply(second, secondDecoder, MoveMessage(0x12345678, empty, new ModernLocInfoV1(0, 0x02, 0, 0x08), 0));
        Apply(second, secondDecoder, MoveMessage(0x12345678, empty, new ModernLocInfoV1(0, 0x02, 1, 0x08), 0));
        Apply(
            second,
            secondDecoder,
            MoveMessage(
                0xdeadbeef,
                empty,
                new ModernLocInfoV1(0, 0x84, 0, 0),
                0));

        PublicStateProjectionResultV1 firstResult = Project(first);
        PublicStateProjectionResultV1 secondResult = Project(second);
        BytesEqual(firstResult.CanonicalBytes.Span, secondResult.CanonicalBytes.Span);
        Equal(firstResult.Sha256, secondResult.Sha256);
        PublicStateSnapshotV1 snapshot = firstResult.Snapshot!;
        True(snapshot.Cards.Any(card => card.Locator.Value == "p0:OVERLAY:0:0"));
        True(snapshot.Cards.Any(card => card.Locator.Value == "p0:HAND:public:305419896:0"));
        True(snapshot.Cards.Any(card => card.Locator.Value == "p0:HAND:public:305419896:1"));
    }

    internal static void TestPairedHiddenWorlds()
    {
        PublicStateProjectionResultV1 worldA = ProjectHiddenOpponentWorld(0x01020304);
        PublicStateProjectionResultV1 worldB = ProjectHiddenOpponentWorld(0xa0b0c0d0);
        BytesEqual(worldA.CanonicalBytes.Span, worldB.CanonicalBytes.Span);
        Equal(worldA.Sha256, worldB.Sha256);
        AssertDoesNotContainForbidden(
            Encoding.UTF8.GetString(worldA.CanonicalBytes.Span),
            new[] { "16909060", "2695984544" });
        AssertDoesNotContainForbidden(
            worldA.Sha256,
            new[] { "16909060", "2695984544" });
        NotNull(worldA.Snapshot);
        NotNull(worldB.Snapshot);
    }

    internal static void TestCanonicalDeterminism()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
            (PerspectiveStateMirrorV1 mirror, _) = CreateMirror(
                0,
                deckCount0: 2,
                extraCount0: 1,
                deckCount1: 3,
                extraCount1: 2);
            PublicStateProjectionResultV1 result = Project(mirror, 0x800);
            string canonical = Encoding.UTF8.GetString(result.CanonicalBytes.Span);
            Equal(
                "{\"contract_id\":\"ocgforge-ignis.public-state-projection.v1\",\"perspective_player\":0,\"duel_flags\":2048,\"turn_count\":0,\"turn_player\":null,\"phase\":null,\"terminal\":false,\"participants\":[{\"absolute_player\":0,\"life_points\":8000,\"main_deck_count\":2,\"hand_count\":0,\"extra_deck_count\":1},{\"absolute_player\":1,\"life_points\":7000,\"main_deck_count\":3,\"hand_count\":0,\"extra_deck_count\":2}],\"cards\":[]}",
                canonical);
            True(result.CanonicalBytes.Span[0] == (byte)'{');
            False(result.CanonicalBytes.Span.Contains((byte)'\n'));
            False(result.CanonicalBytes.Span.Contains((byte)'\r'));
            False(result.CanonicalBytes.ToArray().Any(
                value => char.IsWhiteSpace((char)value)));
            Equal(
                Convert.ToHexString(
                    SHA256.HashData(result.CanonicalBytes.Span)).ToLowerInvariant(),
                result.Sha256);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    internal static void TestSzoneLayoutMapping()
    {
        (PerspectiveStateMirrorV1 fieldMirror, GameplayMessageDecoderV1 fieldDecoder) =
            CreateMirror(0);
        Apply(
            fieldMirror,
            fieldDecoder,
            MoveMessage(
                0x11111111,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x08, 5, 0x04),
                0));
        PublicStateSnapshotV1 fieldSnapshot = Project(fieldMirror).Snapshot!;
        PublicCardStateV1 fieldCard = fieldSnapshot.Cards.Single();
        Equal(PublicSemanticZoneV1.FieldZone, fieldCard.Zone);
        Equal("p0:FIELD_ZONE:5", fieldCard.Locator.Value);

        (PerspectiveStateMirrorV1 separateMirror,
            GameplayMessageDecoderV1 separateDecoder) = CreateMirror(0);
        Apply(
            separateMirror,
            separateDecoder,
            MoveMessage(
                0x22222222,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x08, 6, 0x04),
                0));
        PublicStateSnapshotV1 separateSnapshot = Project(
            separateMirror,
            0x800 | 0x1000).Snapshot!;
        PublicCardStateV1 separateCard = separateSnapshot.Cards.Single();
        Equal(
            PublicSemanticZoneV1.PendulumRelevantState,
            separateCard.Zone);
        Equal("p0:PENDULUM_RELEVANT_STATE:6", separateCard.Locator.Value);

        (PerspectiveStateMirrorV1 separateThreeColumnMirror,
            GameplayMessageDecoderV1 separateThreeColumnDecoder) = CreateMirror(0);
        Apply(
            separateThreeColumnMirror,
            separateThreeColumnDecoder,
            MoveMessage(
                0x2a2a2a2a,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x08, 1, 0x04),
                0));
        PublicCardStateV1 separateThreeColumnCard = Project(
            separateThreeColumnMirror,
            0x800 | 0x1000 | 0x400000).Snapshot!.Cards.Single();
        Equal(
            PublicSemanticZoneV1.SpellTrapZone,
            separateThreeColumnCard.Zone);
        Equal("p0:SPELL_TRAP_ZONE:1", separateThreeColumnCard.Locator.Value);

        (PerspectiveStateMirrorV1 sharedPzoneMirror,
            GameplayMessageDecoderV1 sharedPzoneDecoder) = CreateMirror(0);
        Apply(
            sharedPzoneMirror,
            sharedPzoneDecoder,
            MoveMessage(
                0x33333333,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x08, 0, 0x04),
                0));
        Apply(
            sharedPzoneMirror,
            sharedPzoneDecoder,
            UpdateCardMessage(
                0,
                0x08,
                0,
                DecodeQuery(
                    QueryRecord(QueryFlagV1.Type, U32(0x01000002)),
                    QueryRecord(QueryFlagV1.Position, U32(0x04)),
                    QueryEnd())));
        PublicCardStateV1 sharedPzoneCard = Project(
            sharedPzoneMirror,
            0x800).Snapshot!.Cards.Single();
        Equal(
            PublicSemanticZoneV1.PendulumRelevantState,
            sharedPzoneCard.Zone);
        Equal("p0:PENDULUM_RELEVANT_STATE:0", sharedPzoneCard.Locator.Value);

        (PerspectiveStateMirrorV1 threeColumnMirror,
            GameplayMessageDecoderV1 threeColumnDecoder) = CreateMirror(0);
        Apply(
            threeColumnMirror,
            threeColumnDecoder,
            MoveMessage(
                0x44444444,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x08, 1, 0x04),
                0));
        Apply(
            threeColumnMirror,
            threeColumnDecoder,
            UpdateCardMessage(
                0,
                0x08,
                1,
                DecodeQuery(
                    QueryRecord(QueryFlagV1.Type, U32(0x01000002)),
                    QueryRecord(QueryFlagV1.Position, U32(0x04)),
                    QueryEnd())));
        PublicCardStateV1 threeColumnCard = Project(
            threeColumnMirror,
            0x800 | 0x400000).Snapshot!.Cards.Single();
        Equal(
            PublicSemanticZoneV1.PendulumRelevantState,
            threeColumnCard.Zone);
        Equal("p0:PENDULUM_RELEVANT_STATE:1", threeColumnCard.Locator.Value);

        (PerspectiveStateMirrorV1 sharedMirror,
            GameplayMessageDecoderV1 sharedDecoder) = CreateMirror(0);
        Apply(
            sharedMirror,
            sharedDecoder,
            MoveMessage(
                0x55555555,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x08, 0, 0x04),
                0));
        PublicStateProjectionResultV1 shared = Project(sharedMirror, 0x800);
        False(shared.IsSuccess);
        Equal(PublicStateProjectionErrorV1.UnsupportedLayout, shared.Error);
        Null(shared.Snapshot);
        True(shared.CanonicalBytes.IsEmpty);
        Null(shared.Sha256);
    }

    internal static void TestPublicApiBoundary()
    {
        Assembly assembly = typeof(PublicStateSnapshotV1).Assembly;
        False(assembly.GetTypes().Any(
            type => type.Name == "PublicStateProjectionV1" && type.IsPublic));

        Type[] publicTypes =
        {
            typeof(PublicStateProjectionContextV1),
            typeof(PublicStateProjectionErrorV1),
            typeof(PublicStateProjectionResultV1),
            typeof(PublicStateSnapshotV1),
            typeof(PublicParticipantStateV1),
            typeof(PublicCardStateV1)
        };
        string[] forbidden =
        {
            "MirrorEntityIdV1",
            "MirrorSnapshotV1",
            "MirrorCardSnapshotV1",
            "MirrorQueryValueV1",
            "MirrorQueryFieldSnapshotV1",
            "MirrorRelationSnapshotV1",
            "ModernLocInfoV1",
            "GameplayMessageV1",
            "EntityId",
            "MirrorOrdinal",
            "ProtocolSequenceIdentity",
            "PacketOffset",
            "Socket",
            "PID",
            "Timestamp",
            "Thread"
        };
        foreach (Type type in publicTypes)
        {
            foreach (MemberInfo member in type.GetMembers(
                         BindingFlags.Public |
                         BindingFlags.Instance |
                         BindingFlags.Static))
            {
                AssertDoesNotContainForbidden(member.ToString(), forbidden);
            }

            foreach (PropertyInfo property in type.GetProperties(
                         BindingFlags.Public |
                         BindingFlags.Instance |
                         BindingFlags.Static))
            {
                AssertDoesNotContainForbidden(property.PropertyType.FullName, forbidden);
            }
        }
    }

    internal static void TestCanonicalByteStorageIsolation()
    {
        (PerspectiveStateMirrorV1 mirror, _) = CreateMirror(0);
        PublicStateProjectionResultV1 result = Project(mirror);
        True(result.IsSuccess, result.Error.ToString());
        byte[] before = result.CanonicalBytes.ToArray();
        string originalSha256 = result.Sha256!;

        ReadOnlyMemory<byte> exposed = result.CanonicalBytes;
        True(MemoryMarshal.TryGetArray(
            exposed,
            out ArraySegment<byte> segment));
        NotNull(segment.Array);
        byte[] exposedArray = segment.Array!;
        int exposedIndex = segment.Offset;
        exposedArray[exposedIndex] = exposedArray[exposedIndex] == (byte)'{'
            ? (byte)'}'
            : (byte)'{';

        BytesEqual(before, result.CanonicalBytes.Span);
        Equal(originalSha256, result.Sha256);
        Equal(
            originalSha256,
            Convert.ToHexString(
                SHA256.HashData(result.CanonicalBytes.Span)).ToLowerInvariant());
    }

    private static PublicStateProjectionResultV1 Project(
        PerspectiveStateMirrorV1 mirror,
        ulong duelFlags = 0) =>
        PublicStateProjectionV1.TryProject(
            mirror.Snapshot,
            new PublicStateProjectionContextV1(duelFlags));

    private static void Apply(
        PerspectiveStateMirrorV1 mirror,
        GameplayMessageDecoderV1 decoder,
        byte[] bytes)
    {
        MirrorApplyResult result = mirror.Apply(DecodeMessage(decoder, bytes));
        True(
            result.IsSuccess,
            result.Error + " for " + Convert.ToHexString(bytes));
    }

    private static PublicStateProjectionResultV1 ProjectHiddenOpponentWorld(
        uint hiddenCode)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        Apply(
            mirror,
            decoder,
            MoveMessage(
                0x01010101,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x04, 0, 0x04),
                0));
        Apply(
            mirror,
            decoder,
            MoveMessage(
                hiddenCode,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(1, 0x02, 0, 0x08),
                0));
        return Project(mirror);
    }
}
