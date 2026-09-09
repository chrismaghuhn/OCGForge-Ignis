using System.Reflection;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6C6RealRunEntryPointTests
{
    internal static void TestMissingInputsFailClosed()
    {
        I6C6RealRunEntryPointResultV1 result =
            I6C6RealRunEntryPointV1.TryPrepare(null);

        False(result.IsSuccess);
        Equal(
            "STATUS=BLOCKED_I6C6_RUNTIME_INPUTS_UNAVAILABLE",
            result.Status);
        False(result.ChildRuntimeStarted);
        False(result.LiveEvidenceProduced);
    }

    internal static void TestSafeEvidenceDigestExcludesTcpChunking()
    {
        PerspectiveSafeFrameV1 frame = CreateFullFrame();
        I6C6LiveGameplayObservationV1[] observations =
        {
            new(
                0,
                GameplayMessageV1.FromSummoned(
                    8,
                    GameplayMessageKindV1.Summoned),
                frame)
        };
        I6C6OpponentRuntimeBindingResultV1 binding =
            CreateOpponentBinding();

        string oneChunk = I6C6RealRunEntryPointV1
            .CanonicalSafeEvidenceSha256ForTest(
                "projectignis.windbot.ai-blackwing.v1",
                binding.Binding!,
                diagnosticReceivedTcpChunkCount: 1,
                observations,
                new(true, I6C6ClosureHarnessErrorCodeV1.None));
        string threeChunks = I6C6RealRunEntryPointV1
            .CanonicalSafeEvidenceSha256ForTest(
                "projectignis.windbot.ai-blackwing.v1",
                binding.Binding!,
                diagnosticReceivedTcpChunkCount: 3,
                observations,
                new(true, I6C6ClosureHarnessErrorCodeV1.None));

        Equal(oneChunk, threeChunks);
    }

    internal static void TestSafeEvidenceDigestCoversPublicFrameFields()
    {
        PerspectiveSafeFrameV1 baseline = CreateFullFrame();
        I6C6OpponentRuntimeBindingResultV1 binding =
            CreateOpponentBinding();
        string baselineDigest = Digest(baseline, binding.Binding!);

        PerspectiveSafeGlobalsV1 changedGlobals = new(
            duelFlags: 0x5678,
            lifePoints: baseline.Globals.LifePoints,
            playerToAct: 0,
            turnPlayer: baseline.Globals.TurnPlayer,
            turnCount: baseline.Globals.TurnCount,
            phase: baseline.Globals.Phase,
            chainLength: baseline.Globals.ChainLength,
            winner: baseline.Globals.Winner,
            winReason: baseline.Globals.WinReason,
            terminal: baseline.Globals.Terminal);
        PerspectiveSafeMatchContextV1 changedContext = new(
            perspectivePlayer: baseline.MatchContext.PerspectivePlayer,
            duelFlags: 0x5678,
            knowledge: new(false, false),
            ownDeck: new(false),
            opponentDeck: new(false));
        PerspectiveSafeFrameV1 changedGlobalsFrame = RebuildFrame(
            baseline,
            changedGlobals,
            matchContext: changedContext);
        False(baselineDigest == Digest(changedGlobalsFrame, binding.Binding!));

        PerspectiveSafeEntityV1 known = baseline.Entities[0];
        PerspectiveSafeEntityV1 changedEntity = new(
            known.Locator,
            known.IdentityKnown,
            known.Passcode!.Value + 1,
            known.Owner,
            known.Controller,
            known.Zone,
            known.Sequence,
            known.OverlaySequence,
            known.Position,
            known.FaceUp,
            known.FaceDown,
            new PerspectiveSafeCardPropertiesV1(
                type: 99,
                attribute: known.Printed!.Attribute,
                race: known.Printed.Race,
                attack: known.Printed.Attack,
                defense: known.Printed.Defense),
            known.Current);
        PerspectiveSafeFrameV1 changedEntityFrame = RebuildFrame(
            baseline,
            baseline.Globals,
            new[] { changedEntity }.Concat(baseline.Entities.Skip(1)));
        False(baselineDigest == Digest(changedEntityFrame, binding.Binding!));

        PerspectiveSafeVisibleEventV1 visibleEvent = baseline.VisibleEvents[1];
        PerspectiveSafeVisibleEventV1 changedEvent = new(
            visibleEvent.EventIndex,
            visibleEvent.Kind,
            visibleEvent.Player,
            visibleEvent.EntityLocator,
            publicPasscode: 87654321,
            visibleEvent.FromZone,
            visibleEvent.ToZone,
            visibleEvent.Count,
            visibleEvent.Amount,
            visibleEvent.CounterType,
            visibleEvent.Phase,
            visibleEvent.Winner,
            visibleEvent.WinReason,
            visibleEvent.EffectDescription,
            visibleEvent.Targets);
        PerspectiveSafeFrameV1 changedEventFrame = RebuildFrame(
            baseline,
            baseline.Globals,
            baseline.Entities,
            visibleEvents: new[] { baseline.VisibleEvents[0], changedEvent });
        False(baselineDigest == Digest(changedEventFrame, binding.Binding!));
    }

    internal static void TestOpponentRuntimeInputHashBinding()
    {
        I6C6ClosureScenarioConfigurationV1 scenario =
            new(
                "projectignis.windbot.ai-blackwing.v1",
                @"C:\ProjectIgnis\WindBot\Decks\AI_Blackwing.ydk",
                "0051f350303eed589fed1bba0cf58e345644c91cb5825415357a5ac297ee09b2",
                @"C:\ProjectIgnis\WindBot\Decks\AI_CyberDragon.ydk",
                "ed30c491ad4323ed4729c2de68d7298714e01d71ac5321aaabcb7401a91fbda1",
                "NONE");

        I6C6OpponentRuntimeBindingResultV1 valid =
            I6C6OpponentRuntimeBindingV1.TryCreateFromActualParticipant(
                scenario,
                scenario.OpponentDeckPath,
                "projectignis.windbot.ai-blackwing.v1.opponent");
        True(valid.IsSuccess, valid.ErrorCode.ToString());
        NotNull(valid.Binding);

        I6C6OpponentRuntimeBindingResultV1 wrong =
            I6C6OpponentRuntimeBindingV1.TryCreateFromActualParticipant(
                scenario,
                scenario.PrimaryDeckPath,
                "projectignis.windbot.ai-blackwing.v1.opponent");
        False(wrong.IsSuccess);
        Equal(
            I6C6ClosureHarnessErrorCodeV1.ScenarioInputProvenanceMismatch,
            wrong.ErrorCode);
    }

    internal static void TestOpponentParticipantLeaseRequiresOwnedProcess()
    {
        I6C6OpponentRuntimeParticipantLeaseResultV1 result =
            I6C6OpponentRuntimeParticipantLeaseV1.TryCreateFromOwnedProcess(
                null,
                CounterScenario(),
                CounterScenario().OpponentDeckPath,
                7911);

        False(result.IsSuccess);
        Equal(
            I6C6ClosureHarnessErrorCodeV1.RuntimeArtifactUnavailable,
            result.ErrorCode);
        Null(result.Lease);
    }

    private static I6C6ClosureScenarioConfigurationV1 CounterScenario() =>
        new(
            "projectignis.windbot.ai-blackwing.v1",
            @"C:\ProjectIgnis\WindBot\Decks\AI_Blackwing.ydk",
            "0051f350303eed589fed1bba0cf58e345644c91cb5825415357a5ac297ee09b2",
            @"C:\ProjectIgnis\WindBot\Decks\AI_CyberDragon.ydk",
            "ed30c491ad4323ed4729c2de68d7298714e01d71ac5321aaabcb7401a91fbda1",
            "NONE");

    private static I6C6OpponentRuntimeBindingResultV1 CreateOpponentBinding() =>
        I6C6OpponentRuntimeBindingV1.TryCreateFromActualParticipant(
            CounterScenario(),
            CounterScenario().OpponentDeckPath,
            "projectignis.windbot.ai-blackwing.v1.opponent");

    private static string Digest(
        PerspectiveSafeFrameV1 frame,
        I6C6OpponentRuntimeBindingV1 binding) =>
        I6C6RealRunEntryPointV1.CanonicalSafeEvidenceSha256ForTest(
            CounterScenario().ScenarioId,
            binding,
            diagnosticReceivedTcpChunkCount: 1,
            new[]
            {
                new I6C6LiveGameplayObservationV1(
                    0,
                    GameplayMessageV1.FromSummoned(
                        8,
                        GameplayMessageKindV1.Summoned),
                    frame)
            },
            new(true, I6C6ClosureHarnessErrorCodeV1.None));

    private static PerspectiveSafeFrameV1 CreateFullFrame()
    {
        MethodInfo method = typeof(I6C6NativeOracleTests).GetMethod(
                "CreateFullFrame",
                BindingFlags.NonPublic | BindingFlags.Static) ??
            throw new InvalidOperationException("full frame factory missing");
        return (PerspectiveSafeFrameV1)method.Invoke(
            null,
            new object[] { false, false })!;
    }

    private static PerspectiveSafeFrameV1 RebuildFrame(
        PerspectiveSafeFrameV1 source,
        PerspectiveSafeGlobalsV1 globals,
        IEnumerable<PerspectiveSafeEntityV1>? entities = null,
        PerspectiveSafeMatchContextV1? matchContext = null,
        IEnumerable<PerspectiveSafeVisibleEventV1>? visibleEvents = null)
    {
        PerspectiveSafeFrameSourceResultV1 result =
            PerspectiveSafePublicFrameSourceV1.TryCreate(
                new PerspectiveSafeFrameSourceInputV1(
                    globals,
                    source.Zones,
                    entities ?? source.Entities,
                    source.Relationships,
                    source.Chain,
                    visibleEvents ?? source.VisibleEvents,
                    matchContext ?? source.MatchContext));
        True(result.IsSuccess, result.Error?.ToString() ?? "frame rebuild failed");
        return result.Frame!;
    }
}
