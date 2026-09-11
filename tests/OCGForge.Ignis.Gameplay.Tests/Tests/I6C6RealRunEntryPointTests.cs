using System.Diagnostics;
using System.Reflection;
using OCGForge.Ignis.Client;
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

    internal static void TestExecutionFailureDiagnostics()
    {
        PropertyInfo? diagnosticsProperty =
            typeof(I6C6ClosureHarnessExecutionResultV1).GetProperty(
                "Diagnostics",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
        NotNull(diagnosticsProperty);

        I6C6ClosureHarnessExecutionDiagnosticsV1 runtimeStart =
            I6C6ClosureHarnessExecutionDiagnosticsV1.ExternalRuntimeStart();
        Equal(
            I6C6ClosureHarnessExecutionStageV1.ExternalRuntimeStart,
            runtimeStart.Stage);
        Equal(I2ErrorCode.None, runtimeStart.I2ErrorCode);

        I6C6ClosureHarnessExecutionDiagnosticsV1 deckLoad =
            I6C6ClosureHarnessExecutionDiagnosticsV1.DeckLoad();
        Equal(
            I6C6ClosureHarnessExecutionStageV1.DeckLoad,
            deckLoad.Stage);

        I6C6ClosureHarnessExecutionDiagnosticsV1 sessionStart =
            I6C6ClosureHarnessExecutionDiagnosticsV1.SessionStart(
                I2ErrorCode.ConnectionTimeout);
        Equal(
            I6C6ClosureHarnessExecutionStageV1.SessionStart,
            sessionStart.Stage);
        Equal(I2ErrorCode.ConnectionTimeout, sessionStart.I2ErrorCode);
        Equal(
            I6C6ClosureHarnessPreDuelFailureStageV1.None,
            sessionStart.PreDuelStage);

        I6C6ClosureHarnessExecutionDiagnosticsV1 preDuel =
            I6C6ClosureHarnessExecutionDiagnosticsV1.PreDuelDrive(
                I2ErrorCode.DeckRejected,
                I6C6ClosureHarnessPreDuelFailureStageV1.DeckSubmission);
        Equal(
            I6C6ClosureHarnessExecutionStageV1.PreDuelDrive,
            preDuel.Stage);
        Equal(I2ErrorCode.DeckRejected, preDuel.I2ErrorCode);
        Equal(
            I6C6ClosureHarnessPreDuelFailureStageV1.DeckSubmission,
            preDuel.PreDuelStage);

        I6C6ClosureHarnessExecutionDiagnosticsV1 cancelled =
            I6C6ClosureHarnessExecutionDiagnosticsV1.Cancelled();
        Equal(
            I6C6ClosureHarnessExecutionStageV1.Cancelled,
            cancelled.Stage);
        Equal(I2ErrorCode.Cancelled, cancelled.I2ErrorCode);

        I6C6ClosureHarnessExecutionDiagnosticsV1 cancelledPreDuel =
            I6C6ClosureHarnessExecutionDiagnosticsV1.Cancelled(
                I2ErrorCode.Cancelled,
                I6C6ClosureHarnessPreDuelFailureStageV1.PumpRead);
        Equal(
            I6C6ClosureHarnessExecutionStageV1.Cancelled,
            cancelledPreDuel.Stage);
        Equal(
            I6C6ClosureHarnessPreDuelFailureStageV1.PumpRead,
            cancelledPreDuel.PreDuelStage);

        I6C6ClosureHarnessExecutionDiagnosticsV1 unexpected =
            I6C6ClosureHarnessExecutionDiagnosticsV1.UnexpectedException();
        Equal(
            I6C6ClosureHarnessExecutionStageV1.UnexpectedException,
            unexpected.Stage);
        Equal(I2ErrorCode.None, unexpected.I2ErrorCode);

        I6C6ClosureHarnessExecutionDiagnosticsV1 capture =
            I6C6ClosureHarnessExecutionDiagnosticsV1.GameplayCapture();
        I6C6LiveGameplayCaptureResultV1 captureObject =
            CreateCaptureFailureForDiagnostics();
        I6C6ClosureHarnessExecutionResultV1 captureFailure = new(
            I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
            true,
            false,
            captureObject,
            capture);
        True(ReferenceEquals(captureObject, captureFailure.Capture));
        Equal(
            I6C6ClosureHarnessExecutionStageV1.GameplayCapture,
            captureFailure.Diagnostics!.Value.Stage);
        Equal(
            I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
            captureFailure.ErrorCode);

        I6C6ClosureHarnessExecutionResultV1 success = new(
            I6C6ClosureHarnessErrorCodeV1.None,
            true,
            true);
        Equal(I6C6ClosureHarnessErrorCodeV1.None, success.ErrorCode);
        Null(success.Diagnostics);
    }

    private static I6C6LiveGameplayCaptureResultV1
        CreateCaptureFailureForDiagnostics()
    {
        ConstructorInfo? constructor =
            typeof(I6C6LiveGameplayCaptureResultV1).GetConstructors(
                BindingFlags.Instance | BindingFlags.NonPublic)
            .SingleOrDefault();
        NotNull(constructor);
        return (I6C6LiveGameplayCaptureResultV1)constructor!.Invoke(
            new object?[]
            {
                false,
                GameplayErrorCode.InvalidState,
                null,
                null,
                Array.Empty<byte[]>(),
                Array.Empty<I6C6LiveGameplayObservationV1>(),
                null,
                new I6C6FrameReadinessDiagnosticsV1(
                    null,
                    0,
                    null,
                    null,
                    false,
                    0,
                    Array.Empty<GameplayMessageKindV1>(),
                    null),
                null
            });
    }

    internal static void TestPreDuelI2ErrorPropagation()
    {
        PreDuelFailureCase[] cases =
        {
            new(
                "DeckSubmission/SendFailed",
                CtosPacketType.UpdateDeck,
                I6C6ClosureHarnessPreDuelFailureStageV1.DeckSubmission,
                I2ErrorCode.SendFailed,
                false,
                new[] { PreDuelLobbyFrames() }),
            new(
                "ReadyRequest/SendFailed",
                CtosPacketType.HsReady,
                I6C6ClosureHarnessPreDuelFailureStageV1.ReadyRequest,
                I2ErrorCode.SendFailed,
                false,
                new[] { PreDuelLobbyFrames() }),
            new(
                "ReadyRequest/Cancelled",
                CtosPacketType.HsReady,
                I6C6ClosureHarnessPreDuelFailureStageV1.ReadyRequest,
                I2ErrorCode.Cancelled,
                true,
                new[] { PreDuelLobbyFrames() }),
            new(
                "DuelStartRequest/SendFailed",
                CtosPacketType.HsStart,
                I6C6ClosureHarnessPreDuelFailureStageV1.DuelStartRequest,
                I2ErrorCode.SendFailed,
                false,
                new[] { PreDuelLobbyFrames() }),
            new(
                "DuelStartRequest/Cancelled",
                CtosPacketType.HsStart,
                I6C6ClosureHarnessPreDuelFailureStageV1.DuelStartRequest,
                I2ErrorCode.Cancelled,
                true,
                new[] { PreDuelLobbyFrames() })
        };

        foreach (PreDuelFailureCase testCase in cases)
        {
            using CancellationTokenSource cancellation = new();
            ScriptedPreDuelTransport transport = new(
                testCase.Frames,
                testCase.FailurePacketType,
                testCase.CancelOnFailure,
                cancellation);
            I2SessionRunner runner = new(transport);
            try
            {
                I2Result started = runner.StartAsync(
                        PreDuelTestConnection(),
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
                True(started.IsSuccess, testCase.Name);

                I6C6ClosureHarnessV1.I6C6PreDuelHandoffResultV1 handoff =
                    I6C6ClosureHarnessV1.DriveToGameplayAsync(
                            runner,
                            new PrevalidatedProtocolDeck(
                                new uint[] { 1 },
                                Array.Empty<uint>()),
                            0,
                            0,
                            testCase.CancelOnFailure
                                ? cancellation.Token
                                : CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();

                if (handoff.IsSuccess || handoff.ErrorCode != testCase.ErrorCode)
                {
                    throw new InvalidOperationException(
                        $"{testCase.Name}: expected {testCase.ErrorCode}, " +
                        $"got {handoff.ErrorCode} in {runner.State}");
                }
                Equal(testCase.ErrorCode, handoff.ErrorCode);
                Equal(testCase.FailureStage, handoff.FailureStage);

                I6C6ClosureHarnessExecutionDiagnosticsV1 diagnostics =
                    I6C6ClosureHarnessV1.ClassifyPreDuelFailure(
                        handoff.ErrorCode,
                        handoff.FailureStage,
                        testCase.CancelOnFailure &&
                        cancellation.IsCancellationRequested);
                Equal(
                    testCase.ErrorCode == I2ErrorCode.Cancelled
                        ? I6C6ClosureHarnessExecutionStageV1.Cancelled
                        : I6C6ClosureHarnessExecutionStageV1.PreDuelDrive,
                    diagnostics.Stage);
                Equal(testCase.ErrorCode, diagnostics.I2ErrorCode);
                Equal(testCase.FailureStage, diagnostics.PreDuelStage);
            }
            finally
            {
                runner.DisposeAsync().GetAwaiter().GetResult();
            }
        }
    }

    private static ConnectionConfigurationV1 PreDuelTestConnection() =>
        new(
            "127.0.0.1",
            7911,
            "Ignis",
            0,
            RoomPasswordV1.Create(string.Empty),
            TimeSpan.FromSeconds(1));

    private static byte[] PreDuelLobbyFrames() =>
        Join(
            WireFrameCodec.EncodeStoc(
                StocPacketType.JoinGame,
                PacketPayloadCodec.EncodeStocJoinGame(
                    new HostInfoPayload(
                        0,
                        5,
                        0,
                        0,
                        0,
                        0,
                        8000,
                        5,
                        1,
                        0,
                        0,
                        ClientContractV1.ExpectedServerHandshake,
                        new ProtocolClientVersion(41, 0, 11, 0),
                        1,
                        1,
                        1,
                        0,
                        0,
                        0,
                        new DeckSizeLimits(40, 60),
                        new DeckSizeLimits(0, 15),
                        new DeckSizeLimits(0, 15)))),
            WireFrameCodec.EncodeStoc(
                StocPacketType.TypeChange,
                PacketPayloadCodec.EncodeStocTypeChange(
                    new StocTypeChangePayload(0x10))),
            WireFrameCodec.EncodeStoc(
                StocPacketType.HsPlayerEnter,
                PacketPayloadCodec.EncodeStocHsPlayerEnter(
                    new StocHsPlayerEnterPayload("Ignis", 0))),
            WireFrameCodec.EncodeStoc(
                StocPacketType.HsPlayerEnter,
                PacketPayloadCodec.EncodeStocHsPlayerEnter(
                    new StocHsPlayerEnterPayload("Opponent", 1))));

    private static byte[] PreDuelReadyFrames() =>
        Join(
            WireFrameCodec.EncodeStoc(
                StocPacketType.HsPlayerChange,
                PacketPayloadCodec.EncodeStocHsPlayerChange(
                    new StocHsPlayerChangePayload(0x09))),
            WireFrameCodec.EncodeStoc(
                StocPacketType.HsPlayerChange,
                PacketPayloadCodec.EncodeStocHsPlayerChange(
                    new StocHsPlayerChangePayload(0x19))));

    private static byte[] PreDuelWatchFrame() =>
        WireFrameCodec.EncodeStoc(
            StocPacketType.HsWatchChange,
            PacketPayloadCodec.EncodeStocHsWatchChange(
                new StocHsWatchChangePayload(0)));

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

    private readonly record struct PreDuelFailureCase(
        string Name,
        CtosPacketType FailurePacketType,
        I6C6ClosureHarnessPreDuelFailureStageV1 FailureStage,
        I2ErrorCode ErrorCode,
        bool CancelOnFailure,
        byte[][] Frames);

    private sealed class ScriptedPreDuelTransport : IByteTransport
    {
        private readonly Queue<byte[]> chunks;
        private readonly CtosPacketType failurePacketType;
        private readonly bool cancelOnFailure;
        private readonly CancellationTokenSource cancellation;
        private byte[]? currentChunk;
        private int currentOffset;
        private bool closed;

        internal ScriptedPreDuelTransport(
            IEnumerable<byte[]> frames,
            CtosPacketType failurePacketType,
            bool cancelOnFailure,
            CancellationTokenSource cancellation)
        {
            chunks = new Queue<byte[]>(
                frames.Select(frame => frame.ToArray()));
            this.failurePacketType = failurePacketType;
            this.cancelOnFailure = cancelOnFailure;
            this.cancellation = cancellation ??
                throw new ArgumentNullException(nameof(cancellation));
        }

        public ValueTask ConnectAsync(
            string host,
            int port,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }

        public ValueTask<int> ReadAsync(
            Memory<byte> destination,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            while (currentChunk is null || currentOffset == currentChunk.Length)
            {
                if (chunks.Count == 0)
                {
                    return ValueTask.FromResult(0);
                }

                currentChunk = chunks.Dequeue();
                currentOffset = 0;
            }

            int count = Math.Min(
                destination.Length,
                currentChunk.Length - currentOffset);
            currentChunk.AsMemory(currentOffset, count).CopyTo(destination);
            currentOffset += count;
            return ValueTask.FromResult(count);
        }

        public ValueTask WriteAsync(
            ReadOnlyMemory<byte> source,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FrameReadResult<CtosFrame> parsed = WireFrameCodec.TryReadCtos(
                source.Span);

            if (parsed.Status == FrameReadStatus.Success &&
                parsed.Frame is not null &&
                parsed.Frame.Type == failurePacketType)
            {
                if (cancelOnFailure)
                {
                    cancellation.Cancel();
                    throw new OperationCanceledException(cancellation.Token);
                }

                throw new InvalidOperationException(
                    "scripted pre-duel write failure");
            }

            if (parsed.Status == FrameReadStatus.Success &&
                parsed.Frame is not null)
            {
                if (parsed.Frame.Type == CtosPacketType.UpdateDeck)
                {
                    chunks.Enqueue(PreDuelWatchFrame());
                }
                else if (parsed.Frame.Type == CtosPacketType.HsReady)
                {
                    chunks.Enqueue(PreDuelReadyFrames());
                }
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask CloseAsync()
        {
            if (!closed)
            {
                closed = true;
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync() => CloseAsync();
    }
}
