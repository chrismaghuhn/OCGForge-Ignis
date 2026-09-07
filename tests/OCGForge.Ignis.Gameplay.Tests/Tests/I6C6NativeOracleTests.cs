using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6C6NativeOracleTests
{
    // I6C6-1 deliberately references the future I6C6-2 comparison owner.
    // The missing type is the intended RED build failure.
    internal static void TestI6C6_1RedContract()
    {
        AssertPlayerToActBoundary();
        AssertOptionalPresenceIsSemantic();
        AssertEnumMappingAndUnknownRejection();
        AssertLocatorOrderingIsDeterministic();
        AssertHiddenIdentityDataIsRejected();
        AssertUnprovenPairingFailsClosed();
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
            I6C6ScenarioFixtures.StateWithKnownPlayerToAct();

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
                    "p0:HAND:public:01020304:1",
                    0x01020304),
                I6C6PublicEntityV1.Known(
                    "p0:HAND:public:01020304:0",
                    0x01020304));
        I6C6PublicSafeStateV1 secondConstruction =
            I6C6ScenarioFixtures.StateWithEntities(
                I6C6PublicEntityV1.Known(
                    "p0:HAND:public:01020304:0",
                    0x01020304),
                I6C6PublicEntityV1.Known(
                    "p0:HAND:public:01020304:1",
                    0x01020304));

        I6C6ComparisonResultV1 result =
            I6C6ComparisonFixtures.Compare(
                firstConstruction,
                secondConstruction,
                I6C6ScenarioFixtures.SupportedPairing());
        True(result.IsSuccess, result.ErrorCode.ToString());
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
}
