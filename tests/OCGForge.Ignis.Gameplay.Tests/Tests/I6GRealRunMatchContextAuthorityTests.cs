using System.Reflection;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6GRealRunMatchContextAuthorityTests
{
    internal static void TestCounterContextBindsClosedOpponentKnowledge()
    {
        I6C6ClosureScenarioConfigurationV1 scenario = CounterScenario();
        True(File.Exists(scenario.OpponentDeckPath));

        I6GRealRunMatchContextBindingResultV1 result =
            I6GRealRunMatchContextAuthorityV1.TryCreateCounter(
                scenario,
                new I6GRealRunMatchContextConfigurationV1(
                    scenario.ScenarioId,
                    GameplayPerspectiveV1.SelfIsPlayer0,
                    190464UL,
                    OwnDecklistKnown: true,
                    OpponentDecklistKnown: false));

        True(result.IsSuccess, result.ErrorCode.ToString());
        NotNull(result.Context);
        PerspectiveSafeMatchContextV1 context = result.Context!;

        Equal((byte)0, context.PerspectivePlayer);
        Equal(190464UL, context.DuelFlags);
        True(context.OwnDeck.Known);
        True(context.OwnDeck.MainDeck.Count > 0);
        True(context.OwnDeck.MainDeck.SequenceEqual(
            context.OwnDeck.MainDeck.OrderBy(value => value)));
        True(context.OwnDeck.ExtraDeck.SequenceEqual(
            context.OwnDeck.ExtraDeck.OrderBy(value => value)));
        var loadedPrimaryDeck = I6C6ClosureHarnessV1.LoadDeck(
            scenario.PrimaryDeckPath);
        uint[] expectedPrimaryCodes = loadedPrimaryDeck.MainAndExtraCards
            .OrderBy(value => value)
            .ToArray();
        uint[] actualPrimaryCodes = context.OwnDeck.MainDeck
            .Concat(context.OwnDeck.ExtraDeck)
            .OrderBy(value => value)
            .ToArray();
        True(expectedPrimaryCodes.SequenceEqual(actualPrimaryCodes));

        False(context.Knowledge.OpponentDecklistKnown);
        False(context.OpponentDeck.Known);
        Equal(0, context.OpponentDeck.MainDeck.Count);
        Equal(0, context.OpponentDeck.ExtraDeck.Count);
        Equal(
            context.OwnDeck.Known,
            context.Knowledge.OwnDecklistKnown);
        Equal(
            context.OpponentDeck.Known,
            context.Knowledge.OpponentDecklistKnown);
    }

    internal static void TestPerspectiveUsesGameplayPerspectivePlayerType()
    {
        I6C6ClosureScenarioConfigurationV1 scenario = CounterScenario();
        I6GRealRunMatchContextBindingResultV1 result =
            I6GRealRunMatchContextAuthorityV1.TryCreate(
                scenario,
                new I6GRealRunMatchContextConfigurationV1(
                    scenario.ScenarioId,
                    GameplayPerspectiveV1.SelfIsPlayer1,
                    190464UL,
                    OwnDecklistKnown: false,
                    OpponentDecklistKnown: false));

        True(result.IsSuccess, result.ErrorCode.ToString());
        NotNull(result.Context);
        Equal((byte)1, result.Context!.PerspectivePlayer);
    }

    internal static void TestMissingContextInputsFailClosed()
    {
        I6C6ClosureScenarioConfigurationV1 scenario = CounterScenario();

        I6GRealRunMatchContextBindingResultV1 missingConfiguration =
            I6GRealRunMatchContextAuthorityV1.TryCreateCounter(
                scenario,
                null);
        False(missingConfiguration.IsSuccess);
        Equal(
            I6GRealRunMatchContextErrorCodeV1.MissingConfiguration,
            missingConfiguration.ErrorCode);
        Null(missingConfiguration.Context);

        I6GRealRunMatchContextBindingResultV1 missingPerspective =
            I6GRealRunMatchContextAuthorityV1.TryCreateCounter(
                scenario,
                new I6GRealRunMatchContextConfigurationV1(
                    scenario.ScenarioId,
                    null,
                    190464UL,
                    OwnDecklistKnown: false,
                    OpponentDecklistKnown: false));
        False(missingPerspective.IsSuccess);
        Equal(
            I6GRealRunMatchContextErrorCodeV1.MissingPerspective,
            missingPerspective.ErrorCode);

        I6GRealRunMatchContextBindingResultV1 missingDuelFlags =
            I6GRealRunMatchContextAuthorityV1.TryCreateCounter(
                scenario,
                new I6GRealRunMatchContextConfigurationV1(
                    scenario.ScenarioId,
                    GameplayPerspectiveV1.SelfIsPlayer0,
                    null,
                    OwnDecklistKnown: false,
                    OpponentDecklistKnown: false));
        False(missingDuelFlags.IsSuccess);
        Equal(
            I6GRealRunMatchContextErrorCodeV1.MissingDuelFlags,
            missingDuelFlags.ErrorCode);

        I6GRealRunMatchContextBindingResultV1 wrongScenario =
            I6GRealRunMatchContextAuthorityV1.TryCreateCounter(
                scenario,
                new I6GRealRunMatchContextConfigurationV1(
                    "wrong-scenario",
                    GameplayPerspectiveV1.SelfIsPlayer0,
                    190464UL,
                    OwnDecklistKnown: false,
                    OpponentDecklistKnown: false));
        False(wrongScenario.IsSuccess);
        Equal(
            I6GRealRunMatchContextErrorCodeV1.ScenarioMismatch,
            wrongScenario.ErrorCode);
    }

    internal static void TestCounterCannotUpgradeOpponentKnowledge()
    {
        I6GRealRunMatchContextBindingResultV1 result =
            I6GRealRunMatchContextAuthorityV1.TryCreateCounter(
                CounterScenario(),
                new I6GRealRunMatchContextConfigurationV1(
                    "projectignis.windbot.ai-blackwing.v1",
                    GameplayPerspectiveV1.SelfIsPlayer0,
                    190464UL,
                    OwnDecklistKnown: true,
                    OpponentDecklistKnown: true));

        False(result.IsSuccess);
        Equal(
            I6GRealRunMatchContextErrorCodeV1.CounterOpponentKnowledgeForbidden,
            result.ErrorCode);
        Null(result.Context);
    }

    internal static void TestKnownDeckValidationRemainsFailClosed()
    {
        PerspectiveSafeMatchContextV1 unsorted = new(
            perspectivePlayer: 0,
            duelFlags: 190464UL,
            knowledge: new PerspectiveSafeKnowledgeV1(true, false),
            ownDeck: new PerspectiveSafeDeckV1(
                known: true,
                mainDeck: new uint[] { 2, 1 }),
            opponentDeck: new PerspectiveSafeDeckV1(known: false));
        False(
            PerspectiveSafeMatchContextValidationV1.TryValidate(
                unsorted,
                expectedPerspectivePlayer: 0,
                out _));

        PerspectiveSafeMatchContextV1 zeroPasscode = new(
            perspectivePlayer: 0,
            duelFlags: 190464UL,
            knowledge: new PerspectiveSafeKnowledgeV1(true, false),
            ownDeck: new PerspectiveSafeDeckV1(
                known: true,
                mainDeck: new uint[] { 0 }),
            opponentDeck: new PerspectiveSafeDeckV1(known: false));
        False(
            PerspectiveSafeMatchContextValidationV1.TryValidate(
                zeroPasscode,
                expectedPerspectivePlayer: 0,
                out _));
    }

    internal static void TestRealRunRequestCarriesConfigurationNotContext()
    {
        PropertyInfo? configuration = typeof(I6C6RealRunRequestV1)
            .GetProperty(nameof(I6C6RealRunRequestV1.MatchContextConfiguration));
        NotNull(configuration);
        Null(typeof(I6C6RealRunRequestV1).GetProperty("MatchContext"));
    }

    private static I6C6ClosureScenarioConfigurationV1 CounterScenario() =>
        new(
            "projectignis.windbot.ai-blackwing.v1",
            @"C:\ProjectIgnis\WindBot\Decks\AI_Blackwing.ydk",
            "0051f350303eed589fed1bba0cf58e345644c91cb5825415357a5ac297ee09b2",
            @"C:\ProjectIgnis\WindBot\Decks\AI_CyberDragon.ydk",
            "ed30c491ad4323ed4729c2de68d7298714e01d71ac5321aaabcb7401a91fbda1",
            "NONE");
}
