using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using static OCGForge.Ignis.Gameplay.Tests.TransportFixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I3DPublicProjectionPrivacyTests
{
    private const string PublicProjectionIdPrefix =
        "ocgforge-ignis.public-state-projection.v1.";

    private const string FirstGoldenJson =
        "{\"contract_id\":\"ocgforge-ignis.public-state-projection.v1\",\"perspective_player\":0,\"duel_flags\":2048,\"turn_count\":0,\"turn_player\":null,\"phase\":null,\"terminal\":false,\"participants\":[{\"absolute_player\":0,\"life_points\":8000,\"main_deck_count\":2,\"hand_count\":0,\"extra_deck_count\":1},{\"absolute_player\":1,\"life_points\":7000,\"main_deck_count\":3,\"hand_count\":0,\"extra_deck_count\":2}],\"cards\":[]}";

    private const string FirstGoldenSha256 =
        "5a12f8fea489c7dcdd74a88292a9f1b95be3ff201a795d3d9727d1e40fdf8268";

    private const string SecondGoldenJson =
        "{\"contract_id\":\"ocgforge-ignis.public-state-projection.v1\",\"perspective_player\":0,\"duel_flags\":0,\"turn_count\":0,\"turn_player\":null,\"phase\":null,\"terminal\":false,\"participants\":[{\"absolute_player\":0,\"life_points\":8000,\"main_deck_count\":2,\"hand_count\":0,\"extra_deck_count\":1},{\"absolute_player\":1,\"life_points\":7000,\"main_deck_count\":3,\"hand_count\":0,\"extra_deck_count\":2}],\"cards\":[{\"locator\":\"p0:MONSTER_ZONE:0\",\"absolute_player\":0,\"zone\":\"MONSTER_ZONE\",\"card_code\":305419896,\"position\":4},{\"locator\":\"p0:OVERLAY:0:0\",\"absolute_player\":0,\"zone\":\"OVERLAY\",\"card_code\":3735928559,\"position\":null},{\"locator\":\"p1:MONSTER_ZONE:1\",\"absolute_player\":1,\"zone\":\"MONSTER_ZONE\",\"card_code\":null,\"position\":8}]}";

    private const string SecondGoldenSha256 =
        "74f30ea1814edd755856496df4ce8e76594e4d51e47a1c5595aad24b450d91cb";

    internal static void TestFirstGolden()
    {
        (PerspectiveStateMirrorV1 mirror, _) = CreateMirror(
            0,
            deckCount0: 2,
            extraCount0: 1,
            deckCount1: 3,
            extraCount1: 2);
        PublicStateProjectionResultV1 result = Project(mirror, 0x800);
        AssertSuccess(result);
        byte[] bytes = result.CanonicalBytes.ToArray();
        Equal(386, bytes.Length);
        Equal(FirstGoldenJson, Encoding.UTF8.GetString(bytes));
        Equal(FirstGoldenSha256, result.Sha256);
        Equal(
            PublicProjectionIdPrefix + FirstGoldenSha256,
            result.PublicProjectionId);
    }

    internal static void TestSecondGolden()
    {
        PublicStateProjectionResultV1 result =
            Project(CreateSecondGoldenMirror().Mirror);
        AssertSuccess(result);
        byte[] bytes = result.CanonicalBytes.ToArray();
        Equal(700, bytes.Length);
        Equal(SecondGoldenJson, Encoding.UTF8.GetString(bytes));
        Equal(SecondGoldenSha256, result.Sha256);
        Equal(
            PublicProjectionIdPrefix + SecondGoldenSha256,
            result.PublicProjectionId);
    }

    internal static void TestFailureHasNoIdentity()
    {
        PublicStateProjectionResultV1 invalid =
            PublicStateProjectionV1.TryProject(
                null,
                new PublicStateProjectionContextV1(0));
        AssertFailure(invalid, PublicStateProjectionErrorV1.InvalidSnapshot);

        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        Apply(
            mirror,
            decoder,
            MoveMessage(
                0x11223344,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x08, 0, 0x04),
                0));
        PublicStateProjectionResultV1 unsupported = Project(mirror, 0x800);
        AssertFailure(
            unsupported,
            PublicStateProjectionErrorV1.UnsupportedLayout);
    }

    internal static void TestValueOwnership()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        PublicStateProjectionResultV1 result = Project(mirror);
        AssertSuccess(result);
        byte[] beforeBytes = result.CanonicalBytes.ToArray();
        string beforeSha256 = result.Sha256!;
        string beforePublicProjectionId = result.PublicProjectionId!;

        ReadOnlyMemory<byte> returned = result.CanonicalBytes;
        True(MemoryMarshal.TryGetArray(
            returned,
            out ArraySegment<byte> segment));
        NotNull(segment.Array);
        byte[] callerArray = segment.Array!;
        int callerIndex = segment.Offset;
        callerArray[callerIndex] = callerArray[callerIndex] == (byte)'{'
            ? (byte)'}'
            : (byte)'{';

        BytesEqual(beforeBytes, result.CanonicalBytes.Span);
        Equal(beforeSha256, result.Sha256);
        Equal(beforePublicProjectionId, result.PublicProjectionId);
        Equal(
            beforeSha256,
            Convert.ToHexString(
                SHA256.HashData(result.CanonicalBytes.Span)).ToLowerInvariant());
        Equal(
            PublicProjectionIdPrefix + beforeSha256,
            result.PublicProjectionId);

        Equal(0, result.Snapshot!.Cards.Count);
        Apply(
            mirror,
            decoder,
            MoveMessage(
                0x55667788,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x04, 0, 0x04),
                0));
        Equal(0, result.Snapshot.Cards.Count);
        BytesEqual(beforeBytes, result.CanonicalBytes.Span);
        Equal(beforeSha256, result.Sha256);
        Equal(beforePublicProjectionId, result.PublicProjectionId);
    }

    internal static void TestNoExternalBindingSeam()
    {
        Type resultType = typeof(PublicStateProjectionResultV1);
        PropertyInfo? identityProperty = resultType.GetProperty(
            "PublicProjectionId",
            BindingFlags.Public | BindingFlags.Instance);
        NotNull(identityProperty);
        False(identityProperty!.CanWrite);
        Equal(typeof(string), identityProperty.PropertyType);
        Equal(
            0,
            resultType.GetConstructors(
                    BindingFlags.Public | BindingFlags.Instance)
                .Length);
        Equal(
            0,
            resultType.GetFields(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Length);

        foreach (MethodInfo method in resultType.GetMethods(
                     BindingFlags.Public |
                     BindingFlags.Instance |
                     BindingFlags.Static |
                     BindingFlags.DeclaredOnly))
        {
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                False(
                    IsExternalBindingInput(parameter.ParameterType),
                    method.Name);
            }
        }

        Type projectorType = resultType.Assembly.GetType(
            "OCGForge.Ignis.Gameplay.PublicStateProjectionV1",
            throwOnError: true)!;
        False(projectorType.IsPublic);
        False(resultType.Assembly.GetExportedTypes().Any(
            type => type.Name.Contains("Identity", StringComparison.Ordinal) &&
                    type.Name != nameof(PublicStateProjectionResultV1)));
    }

    internal static void TestPairedWorldA()
    {
        PublicStateProjectionResultV1 worldA = ProjectOpponentHand(
            0x11223344);
        PublicStateProjectionResultV1 worldB = ProjectOpponentHand(
            0x55667788);
        AssertEqualProjection(worldA, worldB);
        AssertAbsent(worldA, 0x11223344, 0x55667788);
        False(worldA.Snapshot!.Cards.Any(
            card => card.Zone == PublicSemanticZoneV1.Hand));
    }

    internal static void TestPairedWorldB()
    {
        PublicStateProjectionResultV1 worldA = ProjectOpponentMainDeck(
            0x10203040,
            0x50607080);
        PublicStateProjectionResultV1 worldB = ProjectOpponentMainDeck(
            0x50607080,
            0x10203040);
        AssertEqualProjection(worldA, worldB);
        AssertAbsent(worldA, 0x10203040, 0x50607080);
        False(worldA.Snapshot!.Cards.Any(
            card => card.Locator.Value.Contains(
                "MAIN_DECK",
                StringComparison.Ordinal)));
    }

    internal static void TestPairedWorldC()
    {
        const uint firstCode = 0x12345678;
        const uint secondCode = 0x87654321;
        PublicStateProjectionResultV1 first =
            ProjectAfterRevealToHidden(firstCode);
        PublicStateProjectionResultV1 second =
            ProjectAfterRevealToHidden(secondCode);
        AssertEqualProjection(first, second);
        AssertAbsent(first, firstCode, secondCode);
        PublicCardStateV1 hidden = first.Snapshot!.Cards.Single();
        Null(hidden.CardCode);
        Equal("p1:MONSTER_ZONE:0", hidden.Locator.Value);
    }

    internal static void TestPairedWorldD()
    {
        const uint duplicateCode = 0x12345678;
        PublicStateProjectionResultV1 first =
            ProjectDuplicatePublicCards(false, duplicateCode);
        PublicStateProjectionResultV1 second =
            ProjectDuplicatePublicCards(true, duplicateCode);
        AssertEqualProjection(first, second);
        PublicCardStateV1[] cards = first.Snapshot!.Cards.ToArray();
        Equal(2, cards.Length);
        NotEqual(cards[0].Locator.Value, cards[1].Locator.Value);
        Equal(duplicateCode, cards[0].CardCode);
        Equal(duplicateCode, cards[1].CardCode);
    }

    internal static void TestPairedWorldE()
    {
        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0));
        byte[] moveFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            MoveMessage(
                0x11223344,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x02, 0, 0x08),
                0));
        byte[] turnFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            new byte[] { 40, 0 });
        byte[] phaseFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            new byte[] { 41, 4, 0 });
        byte[] transcript = Join(startFrame, moveFrame, turnFrame, phaseFrame);

        PublicStateProjectionResultV1 whole = RunChunkedProjection(
            new[] { transcript });
        PublicStateProjectionResultV1 byteWise = RunChunkedProjection(
            transcript.Select(value => new[] { value }).ToArray());
        AssertEqualProjection(whole, byteWise);
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

    private static void AssertSuccess(PublicStateProjectionResultV1 result)
    {
        True(result.IsSuccess, result.Error.ToString());
        NotNull(result.Snapshot);
        NotNull(result.Sha256);
        NotNull(result.PublicProjectionId);
    }

    private static void AssertFailure(
        PublicStateProjectionResultV1 result,
        PublicStateProjectionErrorV1 expectedError)
    {
        False(result.IsSuccess);
        Equal(expectedError, result.Error);
        Null(result.Snapshot);
        True(result.CanonicalBytes.IsEmpty);
        Null(result.Sha256);
        Null(result.PublicProjectionId);
    }

    private static void AssertEqualProjection(
        PublicStateProjectionResultV1 first,
        PublicStateProjectionResultV1 second)
    {
        AssertSuccess(first);
        AssertSuccess(second);
        BytesEqual(first.CanonicalBytes.Span, second.CanonicalBytes.Span);
        Equal(first.Sha256, second.Sha256);
        Equal(first.PublicProjectionId, second.PublicProjectionId);

        PublicStateSnapshotV1 firstSnapshot = first.Snapshot!;
        PublicStateSnapshotV1 secondSnapshot = second.Snapshot!;
        Equal(firstSnapshot.ContractId, secondSnapshot.ContractId);
        Equal(firstSnapshot.PerspectivePlayer, secondSnapshot.PerspectivePlayer);
        Equal(firstSnapshot.DuelFlags, secondSnapshot.DuelFlags);
        Equal(firstSnapshot.TurnCount, secondSnapshot.TurnCount);
        Equal(firstSnapshot.TurnPlayer, secondSnapshot.TurnPlayer);
        Equal(firstSnapshot.Phase, secondSnapshot.Phase);
        Equal(firstSnapshot.Terminal, secondSnapshot.Terminal);
        Equal(firstSnapshot.Participants.Count, secondSnapshot.Participants.Count);
        for (int index = 0; index < firstSnapshot.Participants.Count; index++)
        {
            PublicParticipantStateV1 firstParticipant =
                firstSnapshot.Participants[index];
            PublicParticipantStateV1 secondParticipant =
                secondSnapshot.Participants[index];
            Equal(firstParticipant.AbsolutePlayer, secondParticipant.AbsolutePlayer);
            Equal(firstParticipant.LifePoints, secondParticipant.LifePoints);
            Equal(firstParticipant.MainDeckCount, secondParticipant.MainDeckCount);
            Equal(firstParticipant.HandCount, secondParticipant.HandCount);
            Equal(firstParticipant.ExtraDeckCount, secondParticipant.ExtraDeckCount);
        }

        Equal(firstSnapshot.Cards.Count, secondSnapshot.Cards.Count);
        for (int index = 0; index < firstSnapshot.Cards.Count; index++)
        {
            PublicCardStateV1 firstCard = firstSnapshot.Cards[index];
            PublicCardStateV1 secondCard = secondSnapshot.Cards[index];
            Equal(firstCard.Locator.Value, secondCard.Locator.Value);
            Equal(firstCard.AbsolutePlayer, secondCard.AbsolutePlayer);
            Equal(firstCard.Zone, secondCard.Zone);
            Equal(firstCard.CardCode, secondCard.CardCode);
            Equal(firstCard.Position, secondCard.Position);
        }
    }

    private static void AssertAbsent(
        PublicStateProjectionResultV1 result,
        params uint[] forbiddenCodes)
    {
        string canonical = Encoding.UTF8.GetString(result.CanonicalBytes.Span);
        foreach (uint forbiddenCode in forbiddenCodes)
        {
            False(canonical.Contains(
                forbiddenCode.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal));
        }
    }

    private static bool IsExternalBindingInput(Type type) =>
        type == typeof(string) ||
        type == typeof(byte[]) ||
        type == typeof(ReadOnlyMemory<byte>) ||
        type == typeof(PublicStateSnapshotV1) ||
        type == typeof(MirrorSnapshotV1);

    private static (PerspectiveStateMirrorV1 Mirror, GameplayMessageDecoderV1 Decoder)
        CreateSecondGoldenMirror()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                deckCount0: 2,
                extraCount0: 1,
                deckCount1: 3,
                extraCount1: 2);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        Apply(
            mirror,
            decoder,
            MoveMessage(
                0x12345678,
                empty,
                new ModernLocInfoV1(0, 0x04, 0, 0x04),
                0));
        Apply(
            mirror,
            decoder,
            MoveMessage(
                0x55667788,
                empty,
                new ModernLocInfoV1(1, 0x04, 1, 0x08),
                0));
        Apply(
            mirror,
            decoder,
            MoveMessage(
                0xdeadbeef,
                empty,
                new ModernLocInfoV1(0, 0x84, 0, 0),
                0));
        return (mirror, decoder);
    }

    private static PublicStateProjectionResultV1 ProjectOpponentHand(
        uint hiddenCode)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
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

    private static PublicStateProjectionResultV1 ProjectOpponentMainDeck(
        uint firstCode,
        uint secondCode)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0, deckCount1: 3);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        Apply(
            mirror,
            decoder,
            MoveMessage(
                firstCode,
                empty,
                new ModernLocInfoV1(1, 0x01, 0, 0x08),
                0));
        Apply(
            mirror,
            decoder,
            MoveMessage(
                secondCode,
                empty,
                new ModernLocInfoV1(1, 0x01, 1, 0x08),
                0));
        return Project(mirror);
    }

    private static PublicStateProjectionResultV1 ProjectAfterRevealToHidden(
        uint publicCode)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        Apply(
            mirror,
            decoder,
            MoveMessage(
                publicCode,
                empty,
                new ModernLocInfoV1(1, 0x04, 0, 0x04),
                0));
        PublicStateProjectionResultV1 before = Project(mirror);
        AssertSuccess(before);
        Equal(publicCode, before.Snapshot!.Cards.Single().CardCode);
        Apply(
            mirror,
            decoder,
            PosChangeMessage(1, 0x04, 0, 0x04, 0x08));
        return Project(mirror);
    }

    private static PublicStateProjectionResultV1 ProjectDuplicatePublicCards(
        bool reverse,
        uint duplicateCode)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        uint firstSequence = reverse ? 1u : 0u;
        uint secondSequence = reverse ? 0u : 1u;
        Apply(
            mirror,
            decoder,
            MoveMessage(
                duplicateCode,
                empty,
                new ModernLocInfoV1(0, 0x04, firstSequence, 0x04),
                0));
        Apply(
            mirror,
            decoder,
            MoveMessage(
                duplicateCode,
                empty,
                new ModernLocInfoV1(0, 0x04, secondSequence, 0x04),
                0));
        return Project(mirror);
    }

    private static PublicStateProjectionResultV1 RunChunkedProjection(
        byte[][] chunks)
    {
        TestTransport transport = new(chunks);
        GameplayHandoffAcquireResult acquired =
            GameplayHandoffConsumerV1.TryCreate(
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
        try
        {
            GameplayMirrorPumpResult move = session.PumpAsync(
                CancellationToken.None).GetAwaiter().GetResult();
            True(move.IsSuccess, move.Error.ToString());
            GameplayMirrorPumpResult turn = session.PumpAsync(
                CancellationToken.None).GetAwaiter().GetResult();
            True(turn.IsSuccess, turn.Error.ToString());
            GameplayMirrorPumpResult phase = session.PumpAsync(
                CancellationToken.None).GetAwaiter().GetResult();
            True(phase.IsSuccess, phase.Error.ToString());
            return Project(session.Mirror);
        }
        finally
        {
            session.DisposeAsync().GetAwaiter().GetResult();
            acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }
}
