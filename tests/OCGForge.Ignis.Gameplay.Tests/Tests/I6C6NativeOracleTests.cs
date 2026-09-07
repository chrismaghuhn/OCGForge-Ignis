using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6C6NativeOracleTests
{
    internal static void TestI6C6_1RedContract()
    {
        AssertPlayerToActBoundary();
        AssertOptionalPresenceIsSemantic();
        AssertEnumMappingAndUnknownRejection();
        AssertLocatorOrderingIsDeterministic();
        AssertHiddenIdentityDataIsRejected();
        AssertUnprovenPairingFailsClosed();
        AssertFullFrameAndNativeRowBoundary();
        AssertFullBoundaryDiagnosticsArePublicSafe();
    }

    private static void AssertPlayerToActBoundary()
    {
        I6C6ComparisonResultV1 eligible =
            I6C6ComparisonFixtures.Compare(
                I6C6ScenarioFixtures.StateWithoutPlayerToAct(),
                I6C6ScenarioFixtures.StateWithoutPlayerToAct(),
                I6C6ScenarioFixtures.SupportedPairing());
        True(eligible.IsSuccess, eligible.ErrorCode.ToString());

        I6C6ComparisonResultV1 blocked =
            I6C6ComparisonFixtures.Compare(
                I6C6ScenarioFixtures.StateWithKnownPlayerToAct(),
                I6C6ScenarioFixtures.StateWithoutPlayerToAct(),
                I6C6ScenarioFixtures.SupportedPairing());
        Equal(I6C6ComparisonErrorCodeV1.BlockedPendingI6D, blocked.ErrorCode);
    }

    private static void AssertOptionalPresenceIsSemantic()
    {
        I6C6PublicSafeStateV1 absent =
            I6C6ScenarioFixtures.StateWithoutPlayerToAct();
        I6C6PublicSafeStateV1 presentZero =
            I6C6ScenarioFixtures.StateWithPresentZeroTurnPlayer();

        I6C6ComparisonResultV1 result =
            I6C6ComparisonFixtures.Compare(
                absent,
                presentZero,
                I6C6ScenarioFixtures.SupportedPairing());
        Equal(I6C6ComparisonErrorCodeV1.OptionalPresenceMismatch,
            result.ErrorCode);
    }

    private static void AssertEnumMappingAndUnknownRejection()
    {
        I6C6PublicSafeStateV1 expected =
            I6C6ScenarioFixtures.PublicState(I6C6OptionalByteV1.Absent);
        I6C6ComparisonResultV1 known =
            I6C6ComparisonFixtures.Compare(
                expected,
                I6C6ScenarioFixtures.PublicState(I6C6OptionalByteV1.Absent),
                I6C6ScenarioFixtures.SupportedPairing());
        True(known.IsSuccess, known.ErrorCode.ToString());

        I6C6ComparisonResultV1 unknown =
            I6C6ComparisonFixtures.Compare(
                expected,
                I6C6ScenarioFixtures.StateWithUnknownEnum(),
                I6C6ScenarioFixtures.SupportedPairing());
        Equal(I6C6ComparisonErrorCodeV1.UnknownEnum, unknown.ErrorCode);
    }

    private static void AssertLocatorOrderingIsDeterministic()
    {
        I6C6PublicSafeStateV1 firstConstruction =
            I6C6ScenarioFixtures.StateWithEntities(
                I6C6PublicEntityV1.Known(
                    "p0:HAND:public:12345678:1",
                    12345678),
                I6C6PublicEntityV1.Known(
                    "p0:HAND:public:12345678:0",
                    12345678));
        I6C6PublicSafeStateV1 secondConstruction =
            I6C6ScenarioFixtures.StateWithEntities(
                I6C6PublicEntityV1.Known(
                    "p0:HAND:public:12345678:0",
                    12345678),
                I6C6PublicEntityV1.Known(
                    "p0:HAND:public:12345678:1",
                    12345678));

        I6C6ComparisonResultV1 result =
            I6C6ComparisonFixtures.Compare(
                firstConstruction,
                secondConstruction,
                I6C6ScenarioFixtures.SupportedPairing());
        True(result.IsSuccess, result.ErrorCode.ToString());

        I6C6ComparisonResultV1 reverseResult =
            I6C6ComparisonFixtures.Compare(
                secondConstruction,
                firstConstruction,
                I6C6ScenarioFixtures.SupportedPairing());
        True(reverseResult.IsSuccess, reverseResult.ErrorCode.ToString());
    }

    private static void AssertHiddenIdentityDataIsRejected()
    {
        I6C6ComparisonResultV1 redacted =
            I6C6ComparisonFixtures.Compare(
                I6C6ScenarioFixtures.StateWithHiddenUnknownEntity(),
                I6C6ScenarioFixtures.StateWithHiddenUnknownEntity(),
                I6C6ScenarioFixtures.SupportedPairing());
        True(redacted.IsSuccess, redacted.ErrorCode.ToString());

        I6C6ComparisonResultV1 forbidden =
            I6C6ComparisonFixtures.Compare(
                I6C6ScenarioFixtures.StateWithForbiddenHiddenIdentityData(),
                I6C6ScenarioFixtures.StateWithForbiddenHiddenIdentityData(),
                I6C6ScenarioFixtures.SupportedPairing());
        Equal(I6C6ComparisonErrorCodeV1.HiddenIdentityData,
            forbidden.ErrorCode);
    }

    private static void AssertUnprovenPairingFailsClosed()
    {
        I6C6ComparisonResultV1 result =
            I6C6ComparisonFixtures.Compare(
                I6C6ScenarioFixtures.StateWithoutPlayerToAct(),
                I6C6ScenarioFixtures.StateWithoutPlayerToAct(),
                I6C6ScenarioFixtures.MissingSetupDescriptorPairing());
        Equal(I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing,
            result.ErrorCode);
    }

    private static void AssertFullFrameAndNativeRowBoundary()
    {
        PerspectiveSafeFrameV1 frame = CreateFullFrame();
        I6C6NativePublicSafeStateRowV1 native = CreateEquivalentNativeRow();

        I6C6FullComparisonResultV1 result =
            I6C6ComparisonFixtures.CompareFrameToNative(frame, native);
        True(result.IsSuccess, result.ErrorCode.ToString());
    }

    private static void AssertFullBoundaryDiagnosticsArePublicSafe()
    {
        PerspectiveSafeFrameV1 frame = CreateFullFrame();
        I6C6NativePublicSafeStateRowV1 native = CreateEquivalentNativeRow();
        I6C6NativePublicSafeStateRowV1 changed = native with
        {
            Globals = native.Globals with
            {
                LifePoints = new uint[] { 7999, 7000 }
            }
        };

        I6C6FullComparisonResultV1 mismatch =
            I6C6ComparisonFixtures.CompareFrameToNative(frame, changed);
        Equal(I6C6FullComparisonErrorCodeV1.SemanticMismatch,
            mismatch.ErrorCode);
        Equal("globals.life_points[0]", mismatch.PublicSafeFieldPath);
        AssertDoesNotContainForbidden(
            mismatch.PublicSafeFieldPath,
            new[] { "7999", "8000", "12345678" });
    }

    private static PerspectiveSafeFrameV1 CreateFullFrame()
    {
        PerspectiveSafeCardPropertiesV1 properties =
            new(
                type: 1,
                attribute: 2,
                race: 4,
                attack: 1000,
                defense: 1200,
                baseAttack: 1000,
                baseDefense: 1200,
                level: 4,
                linkMarkers: new[]
                {
                    PerspectiveSafeLinkMarkerV1.Bottom,
                    PerspectiveSafeLinkMarkerV1.Top
                },
                leftScale: 1,
                rightScale: 2,
                statusFlags: 3,
                counters: new[]
                {
                    new PerspectiveSafeCounterV1(1, 0),
                    new PerspectiveSafeCounterV1(2, 1)
                });
        const string knownLocator = "p0:HAND:public:12345678:0";
        const string hiddenLocator = "p1:SPELL_TRAP_ZONE:0";

        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreate(
                new PerspectiveSafeFrameSourceInputV1(
                    new PerspectiveSafeGlobalsV1(
                        duelFlags: 0x1234,
                        lifePoints: new uint[] { 8000, 7000 },
                        turnPlayer: 0,
                        turnCount: 3,
                        phase: 4,
                        chainLength: 1),
                    new[]
                    {
                        new PerspectiveSafeZoneV1(
                            0,
                            PerspectiveSafeSemanticZoneV1.MainDeck,
                            2,
                            0,
                            2,
                            false),
                        new PerspectiveSafeZoneV1(
                            0,
                            PerspectiveSafeSemanticZoneV1.Hand,
                            1,
                            1,
                            0,
                            true)
                    },
                    new[]
                    {
                        new PerspectiveSafeEntityV1(
                            knownLocator,
                            identityKnown: true,
                            passcode: 12345678,
                            owner: 0,
                            controller: 0,
                            zone: PerspectiveSafeSemanticZoneV1.Hand,
                            sequence: 0,
                            overlaySequence: null,
                            position: PerspectiveSafePositionV1.Unknown,
                            faceUp: false,
                            faceDown: false,
                            printed: properties,
                            current: properties),
                        new PerspectiveSafeEntityV1(
                            hiddenLocator,
                            identityKnown: false,
                            passcode: null,
                            owner: null,
                            controller: 1,
                            zone: PerspectiveSafeSemanticZoneV1.SpellTrapZone,
                            sequence: 0,
                            overlaySequence: null,
                            position: PerspectiveSafePositionV1.FaceDownDefense,
                            faceUp: false,
                            faceDown: true)
                    },
                    new[]
                    {
                        new PerspectiveSafeRelationshipV1(
                            PerspectiveSafeRelationshipKindV1.Target,
                            knownLocator,
                            hiddenLocator)
                    },
                    new PerspectiveSafeChainStateV1(
                        1,
                        new[]
                        {
                            new PerspectiveSafeChainLinkV1(
                                index: 0,
                                activatingPlayer: 0,
                                source: knownLocator,
                                activationZone: PerspectiveSafeSemanticZoneV1.Hand,
                                effectDescription: 42,
                                targets: new[] { knownLocator, hiddenLocator })
                        }),
                    new[]
                    {
                        new PerspectiveSafeVisibleEventV1(
                            0,
                            PerspectiveSafeVisibleEventKindV1.TurnStarted,
                            player: 0,
                            phase: 1),
                        new PerspectiveSafeVisibleEventV1(
                            1,
                            PerspectiveSafeVisibleEventKindV1.CardMoved,
                            entityLocator: knownLocator,
                            fromZone: PerspectiveSafeSemanticZoneV1.Hand,
                            toZone: PerspectiveSafeSemanticZoneV1.MonsterZone)
                    },
                    new PerspectiveSafeMatchContextV1(
                        perspectivePlayer: 0,
                        duelFlags: 0x1234,
                        knowledge: new PerspectiveSafeKnowledgeV1(true, false),
                        ownDeck: new PerspectiveSafeDeckV1(
                            known: true,
                            mainDeck: new uint[] { 1, 2 },
                            extraDeck: new uint[] { 3 }),
                        opponentDeck: new PerspectiveSafeDeckV1(known: false))));
        True(result.IsSuccess, result.Error?.ToString() ?? "frame rejected");
        NotNull(result.Frame);
        return result.Frame!;
    }

    private static I6C6NativePublicSafeStateRowV1 CreateEquivalentNativeRow() =>
        new(
            new I6C6NativeGlobalsV1(
                DuelFlags: 0x1234,
                LifePoints: new uint[] { 8000, 7000 },
                PlayerToAct: null,
                TurnPlayer: 0,
                TurnCount: 3,
                Phase: 4,
                ChainLength: 1,
                Winner: null,
                WinReason: null,
                Terminal: false),
            new I6C6NativeZoneV1[]
            {
                new(0, 1, 2, 0, 2, false),
                new(0, 2, 1, 1, 0, true)
            },
            new I6C6NativeEntityV1[]
            {
                new(
                    "p0:HAND:public:12345678:0",
                    true,
                    12345678,
                    0,
                    0,
                    2,
                    0,
                    null,
                    0,
                    false,
                    false,
                    CreateEquivalentNativeProperties(),
                    CreateEquivalentNativeProperties()),
                new(
                    "p1:SPELL_TRAP_ZONE:0",
                    false,
                    null,
                    null,
                    1,
                    4,
                    0,
                    null,
                    8,
                    false,
                    true,
                    null,
                    null)
            },
            new[]
            {
                new I6C6NativeRelationshipV1(
                    2,
                    "p0:HAND:public:12345678:0",
                    "p1:SPELL_TRAP_ZONE:0")
            },
            new I6C6NativeChainV1(
                1,
                new[]
                {
                    new I6C6NativeChainLinkV1(
                        0,
                        0,
                        "p0:HAND:public:12345678:0",
                        2,
                        42,
                        new[]
                        {
                            "p0:HAND:public:12345678:0",
                            "p1:SPELL_TRAP_ZONE:0"
                        })
                }),
            new[]
            {
                new I6C6NativeVisibleEventV1(
                    0,
                    1,
                    0,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    1,
                    Targets: Array.Empty<string>()),
                new I6C6NativeVisibleEventV1(
                    1,
                    3,
                    null,
                    "p0:HAND:public:12345678:0",
                    null,
                    2,
                    3)
            },
            new I6C6NativeMatchContextV1(
                0,
                0x1234,
                true,
                false,
                new uint[] { 1, 2 },
                new uint[] { 3 },
                Array.Empty<uint>(),
                Array.Empty<uint>()));

    private static I6C6NativeCardPropertiesV1 CreateEquivalentNativeProperties() =>
        new(
            1,
            2,
            4,
            1000,
            1200,
            1000,
            1200,
            4,
            null,
            null,
            new byte[] { 1, 6 },
            1,
            2,
            3,
            new[]
            {
                new I6C6NativeCounterV1(1, 0),
                new I6C6NativeCounterV1(2, 1)
            });
}
