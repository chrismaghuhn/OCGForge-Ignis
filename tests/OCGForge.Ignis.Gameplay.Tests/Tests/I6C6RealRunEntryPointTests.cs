using System.Diagnostics;
using System.Reflection;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.ModernQueryFixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6C6RealRunEntryPointTests
{
    internal static void TestCaptureFailureDiagnosticsExposeSourceError()
    {
        Type resultType = typeof(I6C6LiveGameplayCaptureResultV1);
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

        NotNull(resultType.GetProperty("FailureStage", flags));
        NotNull(resultType.GetProperty("FailureOrdinal", flags));
        NotNull(resultType.GetProperty("FailureMessageKind", flags));
        NotNull(resultType.GetProperty("FrameSourceErrorCode", flags));
        NotNull(resultType.GetProperty("FrameSourceErrorSection", flags));
    }

    internal static void TestCaptureFailureDiagnosticsRetainFrameSourceError()
    {
        PerspectiveSafeFrameSourceResultV1 sourceFailure =
            PerspectiveSafeFrameSourceResultV1.Failure(
                new PerspectiveSafeFrameSourceErrorV1(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                    PerspectiveSafeSourceSectionV1.Entities));

        I6C6CaptureFailureDiagnosticsV1 diagnostics =
            I6C6CaptureFailureDiagnosticsV1.FromFrameSourceFailure(
                I6C6CaptureFailureStageV1.SubsequentFrame,
                17,
                GameplayMessageKindV1.UpdateData,
                sourceFailure);

        Equal(I6C6CaptureFailureStageV1.SubsequentFrame, diagnostics.Stage);
        Equal((ulong?)17, diagnostics.FailureOrdinal);
        Equal(
            GameplayMessageKindV1.UpdateData,
            diagnostics.FailureMessageKind);
        Equal(
            PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
            diagnostics.FrameSourceErrorCode);
        Equal(
            PerspectiveSafeSourceSectionV1.Entities,
            diagnostics.FrameSourceErrorSection);
    }

    internal static void TestFrameReadinessDiagnosticsExposeBoundary()
    {
        Type resultType = typeof(I6C6LiveGameplayCaptureResultV1);
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        PropertyInfo? readinessProperty = resultType.GetProperty(
            "ReadinessDiagnostics",
            flags);
        NotNull(readinessProperty);

        Type readinessType = readinessProperty!.PropertyType;
        foreach (string propertyName in new[]
                 {
                     "InitialFrameError",
                     "ProvisionalNotReadyCount",
                     "FirstCompleteFrame",
                     "FirstCompleteFrameOrdinal",
                     "FirstCompleteFrameMessageKind",
                     "ActionRequiredBeforeFrameReady",
                     "MessagesAppliedBeforeReady",
                     "AppliedMessageKinds",
                     "VisibleEventHistoryPreserved"
                 })
        {
            NotNull(readinessType.GetProperty(propertyName, flags));
        }
    }

    internal static void TestCapturedFailureMessageDiagnosticsHaveReassemblySeam()
    {
        Type? traceType = typeof(I6C6LiveGameplayCaptureResultV1)
            .Assembly
            .GetType(
                "OCGForge.Ignis.Gameplay.Tests.Fixtures.I6C6CapturedGameplayMessageTraceV1");
        NotNull(traceType);
        NotNull(
            traceType!.GetMethod(
                "TryFindMessageAtOrdinal",
                BindingFlags.Static | BindingFlags.NonPublic));
    }

    internal static void TestCapturedFailureMessageDiagnosticsDecodeKind()
    {
        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0));
        byte[] drawFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            DrawMessage(0, (0x10203040u, 0x05u)));

        GameplayMessageV1? failedMessage =
            I6C6CapturedGameplayMessageTraceV1.TryFindMessageAtOrdinal(
                GameplayPerspectiveV1.SelfIsPlayer0,
                startFrame,
                new[] { drawFrame },
                1);

        NotNull(failedMessage);
        Equal(GameplayMessageKindV1.Draw, failedMessage!.Kind);
    }

    internal static void TestMirrorFailureSiteClassifiesMissingUpdateEntity()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            MirrorFixtures.CreateMirror(0, deckCount0: 1);
        GameplayMessageV1 message = DecodeMessage(
            decoder,
            UpdateDataMessage(
                0,
                0x01,
                Join(
                    QueryRecord(QueryFlagV1.Position, U32(0x04)),
                    QueryEnd())));

        I6C6MirrorFailureClassificationV1? classification =
            I6C6MirrorFailureSiteV1.TryClassify(
                GameplayErrorCode.UnknownMirrorReference,
                message,
                mirror.Snapshot);
        NotNull(classification);
        Equal(
            "ApplyUpdateData/non-extra-entity-missing",
            classification!.Value.Site);
        NotNull(classification.Value.Input);
        Equal((byte)0, classification.Value.Input!.Value.Player);
        Equal(MirrorZoneV1.MainDeck, classification.Value.Input.Value.Location);
        Equal(1, classification.Value.Input.Value.QueryCount);
        Equal(0, classification.Value.Input.Value.QueryIndex);
        False(classification.Value.Input.Value.QueryIsOnFieldSkipped);
        Equal((uint?)1, classification.Value.Input.Value.PreZoneCount);
        Equal(0, classification.Value.Input.Value.PreRepresentedEntityCount);
    }

    internal static void TestMirrorFailureInputDiagnosticsExposeFields()
    {
        Type diagnosticsType = typeof(I6C6CaptureFailureDiagnosticsV1);
        PropertyInfo? inputProperty = diagnosticsType.GetProperty(
            "MirrorFailureInput",
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);
        NotNull(inputProperty);

        Type inputType = inputProperty!.PropertyType;
        if (Nullable.GetUnderlyingType(inputType) is Type underlying)
        {
            inputType = underlying;
        }

        foreach (string propertyName in new[]
                 {
                     "Player",
                     "Location",
                     "QueryCount",
                     "QueryIndex",
                     "QueryIsOnFieldSkipped",
                     "PreZoneCount",
                     "PreRepresentedEntityCount"
                 })
        {
            NotNull(
                inputType.GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic));
        }
    }

    internal static void TestUnknownGameplayMessageClassificationHasParserSeam()
    {
        Type? traceType = typeof(I6C6LiveGameplayCaptureResultV1)
            .Assembly
            .GetType(
                "OCGForge.Ignis.Gameplay.Tests.Fixtures.I6C6CapturedGameplayMessageTraceV1");
        NotNull(traceType);
        NotNull(
            traceType!.GetMethod(
                "TryClassifyMessageAtOrdinal",
                BindingFlags.Static | BindingFlags.NonPublic));
    }

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

    internal static void TestOpponentParticipantArgumentsAreTokenExact()
    {
        const string expectedDeck =
            @"C:\ProjectIgnis\WindBot\Decks\AI_CyberDragon.ydk";

        ProcessStartInfo validArgumentList = new()
        {
            FileName = "WindBot.exe"
        };
        validArgumentList.ArgumentList.Add($"DeckFile=\"{expectedDeck}\"");
        validArgumentList.ArgumentList.Add("Port=7911");
        True(
            I6C6OpponentRuntimeParticipantLeaseV1.HasProcessInputForTest(
                validArgumentList,
                expectedDeck));
        True(
            I6C6OpponentRuntimeParticipantLeaseV1.HasPortInputForTest(
                validArgumentList,
                7911));

        ProcessStartInfo invalidArgumentList = new()
        {
            FileName = "WindBot.exe"
        };
        invalidArgumentList.ArgumentList.Add($"DeckFile=\"{expectedDeck}.bak\"");
        invalidArgumentList.ArgumentList.Add("Port=79110");
        False(
            I6C6OpponentRuntimeParticipantLeaseV1.HasProcessInputForTest(
                invalidArgumentList,
                expectedDeck));
        False(
            I6C6OpponentRuntimeParticipantLeaseV1.HasPortInputForTest(
                invalidArgumentList,
                7911));

        ProcessStartInfo validRawArguments = new()
        {
            FileName = "WindBot.exe",
            Arguments = $"DeckFile=\"{expectedDeck}\" Port=7911"
        };
        True(
            I6C6OpponentRuntimeParticipantLeaseV1.HasProcessInputForTest(
                validRawArguments,
                expectedDeck));
        True(
            I6C6OpponentRuntimeParticipantLeaseV1.HasPortInputForTest(
                validRawArguments,
                7911));

        ProcessStartInfo invalidRawArguments = new()
        {
            FileName = "WindBot.exe",
            Arguments = $"DeckFile=\"{expectedDeck}.bak\" Port=79110"
        };
        False(
            I6C6OpponentRuntimeParticipantLeaseV1.HasProcessInputForTest(
                invalidRawArguments,
                expectedDeck));
        False(
            I6C6OpponentRuntimeParticipantLeaseV1.HasPortInputForTest(
                invalidRawArguments,
                7911));
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
