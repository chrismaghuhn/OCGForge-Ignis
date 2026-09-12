using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
        Equal(
            I6C6ExternalRuntimeReadinessResultV1.None,
            runtimeStart.Readiness);

        I6C6ClosureHarnessExecutionDiagnosticsV1 readinessFailure =
            I6C6ClosureHarnessExecutionDiagnosticsV1.ExternalRuntimeStart(
                I6C6ExternalRuntimeReadinessResultV1.ProcessExited);
        Equal(
            I6C6ExternalRuntimeReadinessResultV1.ProcessExited,
            readinessFailure.Readiness);
        I6C6ExternalRuntimeReadinessException startedReadinessFailure =
            new(
                I6C6ExternalRuntimeReadinessResultV1.DeadlineExpired,
                processStarted: true);
        True(startedReadinessFailure.ProcessStarted);

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

        I6C6ClosureHarnessExecutionDiagnosticsV1 cancelledReadiness =
            I6C6ClosureHarnessExecutionDiagnosticsV1.Cancelled(
                readiness:
                    I6C6ExternalRuntimeReadinessResultV1.Cancelled);
        Equal(
            I6C6ExternalRuntimeReadinessResultV1.Cancelled,
            cancelledReadiness.Readiness);

        I6C6ClosureHarnessExecutionDiagnosticsV1 unexpected =
            I6C6ClosureHarnessExecutionDiagnosticsV1.UnexpectedException();
        Equal(
            I6C6ClosureHarnessExecutionStageV1.UnexpectedException,
            unexpected.Stage);
        Equal(I2ErrorCode.None, unexpected.I2ErrorCode);
        Equal(
            I6C6ClosureHarnessExceptionSiteV1.None,
            unexpected.ExceptionSite);
        Null(unexpected.ExceptionType);

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

    internal static void TestUnexpectedExceptionDiagnosticsPreserveBoundary()
    {
        (I6C6ClosureHarnessExceptionSiteV1 Site, Exception Exception)[] cases =
        {
            (
                I6C6ClosureHarnessExceptionSiteV1.TransportConstruction,
                new InvalidOperationException()),
            (
                I6C6ClosureHarnessExceptionSiteV1.RunnerConstruction,
                new ArgumentException()),
            (
                I6C6ClosureHarnessExceptionSiteV1.ExternalRuntimeStartReadiness,
                new IOException()),
            (
                I6C6ClosureHarnessExceptionSiteV1.SessionStart,
                new InvalidDataException()),
            (
                I6C6ClosureHarnessExceptionSiteV1.DeckLoad,
                new FileNotFoundException()),
            (
                I6C6ClosureHarnessExceptionSiteV1.PreDuelDrive,
                new InvalidOperationException()),
            (
                I6C6ClosureHarnessExceptionSiteV1.GameplayCapture,
                new NotSupportedException()),
            (
                I6C6ClosureHarnessExceptionSiteV1.RunnerDisposal,
                new ObjectDisposedException("runner")),
            (
                I6C6ClosureHarnessExceptionSiteV1.CaptureTransportDisposal,
                new IOException()),
            (
                I6C6ClosureHarnessExceptionSiteV1.ExternalRuntimeOwnerDisposal,
                new ObjectDisposedException("owner"))
        };

        foreach ((I6C6ClosureHarnessExceptionSiteV1 site, Exception exception)
                     in cases)
        {
            I6C6ClosureHarnessExecutionDiagnosticsV1 diagnostics =
                I6C6ClosureHarnessExecutionDiagnosticsV1.UnexpectedException(
                    site,
                    exception);
            Equal(
                I6C6ClosureHarnessExecutionStageV1.UnexpectedException,
                diagnostics.Stage);
            Equal(site, diagnostics.ExceptionSite);
            Equal(exception.GetType().FullName, diagnostics.ExceptionType);

            I6C6ClosureHarnessExecutionResultV1 result =
                I6C6ClosureHarnessV1.UnexpectedExceptionResultForTest(
                    site,
                    exception,
                    processStarted: true);
            Equal(
                I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
                result.ErrorCode);
            True(result.ProcessStarted);
            False(result.GameplayCaptureSucceeded);
            Null(result.Capture);
            NotNull(result.Diagnostics);
            Equal(site, result.Diagnostics!.Value.ExceptionSite);
            Equal(
                exception.GetType().FullName,
                result.Diagnostics!.Value.ExceptionType);
        }

        I6C6ClosureHarnessExecutionDiagnosticsV1 deckLoad =
            I6C6ClosureHarnessExecutionDiagnosticsV1.DeckLoad(
                new FileNotFoundException());
        Equal(
            I6C6ClosureHarnessExecutionStageV1.DeckLoad,
            deckLoad.Stage);
        Equal(
            I6C6ClosureHarnessExceptionSiteV1.DeckLoad,
            deckLoad.ExceptionSite);
        Equal(
            typeof(FileNotFoundException).FullName,
            deckLoad.ExceptionType);
    }

    internal static void TestGameplayCaptureExceptionDiagnosticsPreserveSubsite()
    {
        Type diagnosticsType =
            typeof(I6C6ClosureHarnessExecutionDiagnosticsV1);
        BindingFlags flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        NotNull(diagnosticsType.GetProperty("GameplayCaptureSubsite", flags));

        I6C6ClosureHarnessGameplayCaptureSubsiteV1[] cases =
        {
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.HandoffClaim,
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.InitialGameplayPump,
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.MirrorConstruction,
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.MirrorSessionConstruction,
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.InitialFrameConstruction,
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.InitialFrameFailureDiagnostics,
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.SubsequentGameplayPump,
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.SubsequentFrameConstruction,
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.SubsequentFrameFailureDiagnostics,
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.CaptureFinalization
        };

        foreach (I6C6ClosureHarnessGameplayCaptureSubsiteV1 subsite in cases)
        {
            Exception exception = new ArgumentException();
            I6C6ClosureHarnessExecutionResultV1 result =
                I6C6ClosureHarnessV1.InvokeGameplayCapturePhaseForTest(
                    subsite,
                    () => throw exception);

            Equal(
                I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
                result.ErrorCode);
            False(result.GameplayCaptureSucceeded);
            Null(result.Capture);
            NotNull(result.Diagnostics);
            Equal(
                I6C6ClosureHarnessExecutionStageV1.UnexpectedException,
                result.Diagnostics!.Value.Stage);
            Equal(
                I6C6ClosureHarnessExceptionSiteV1.GameplayCapture,
                result.Diagnostics!.Value.ExceptionSite);
            Equal(
                exception.GetType().FullName,
                result.Diagnostics!.Value.ExceptionType);
            Equal(
                subsite,
                result.Diagnostics!.Value.GameplayCaptureSubsite);
            Null(diagnosticsType.GetProperty("ExceptionMessage", flags));
            Null(diagnosticsType.GetProperty("StackTrace", flags));
        }
    }

    internal static void TestUnexpectedExceptionDiagnosticsAreNonSemantic()
    {
        PerspectiveSafeFrameV1 frame = CreateFullFrame();
        I6C6OpponentRuntimeBindingResultV1 binding =
            CreateOpponentBinding();
        True(binding.IsSuccess);
        NotNull(binding.Binding);

        string before = Digest(frame, binding.Binding!);
        _ = I6C6ClosureHarnessV1.UnexpectedExceptionResultForTest(
            I6C6ClosureHarnessExceptionSiteV1.GameplayCapture,
            new InvalidOperationException("must not be hashed"),
            processStarted: true);
        string after = Digest(frame, binding.Binding!);

        Equal(before, after);
    }

    internal static void TestCleanupFailuresPreserveFirstExecutionFailure()
    {
        I6C6ClosureHarnessExecutionResultV1 sessionFailure =
            I6C6ClosureHarnessV1.UnexpectedExceptionResultForTest(
                I6C6ClosureHarnessExceptionSiteV1.SessionStart,
                new InvalidOperationException(),
                processStarted: true);
        I6C6ClosureHarnessExecutionResultV1 sessionWithCleanupFailure =
            I6C6ClosureHarnessV1.PreserveCleanupFailureForTest(
                sessionFailure,
                I6C6ClosureHarnessExceptionSiteV1.RunnerDisposal,
                new IOException());
        AssertPrimaryAndCleanup(
            sessionWithCleanupFailure,
            I6C6ClosureHarnessExecutionStageV1.UnexpectedException,
            I6C6ClosureHarnessExceptionSiteV1.SessionStart,
            typeof(InvalidOperationException),
            I6C6ClosureHarnessExceptionSiteV1.RunnerDisposal,
            typeof(IOException));

        I6C6ClosureHarnessExecutionResultV1 preDuelFailure = new(
            I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
            true,
            false,
            Diagnostics:
                I6C6ClosureHarnessExecutionDiagnosticsV1.PreDuelDrive(
                    I2ErrorCode.InvalidStateTransition,
                    I6C6ClosureHarnessPreDuelFailureStageV1.DuelStartRequest));
        I6C6ClosureHarnessExecutionResultV1 preDuelWithCleanupFailure =
            I6C6ClosureHarnessV1.PreserveCleanupFailureForTest(
                preDuelFailure,
                I6C6ClosureHarnessExceptionSiteV1.CaptureTransportDisposal,
                new IOException());
        AssertPrimaryAndCleanup(
            preDuelWithCleanupFailure,
            I6C6ClosureHarnessExecutionStageV1.PreDuelDrive,
            I6C6ClosureHarnessExceptionSiteV1.None,
            null,
            I6C6ClosureHarnessExceptionSiteV1.CaptureTransportDisposal,
            typeof(IOException));

        I6C6LiveGameplayCaptureResultV1 capture =
            CreateCaptureFailureForDiagnostics();
        I6C6ClosureHarnessExecutionResultV1 gameplayFailure = new(
            I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
            true,
            false,
            capture,
            I6C6ClosureHarnessExecutionDiagnosticsV1.GameplayCapture());
        I6C6ClosureHarnessExecutionResultV1 gameplayWithCleanupFailure =
            I6C6ClosureHarnessV1.PreserveCleanupFailureForTest(
                gameplayFailure,
                I6C6ClosureHarnessExceptionSiteV1.ExternalRuntimeOwnerDisposal,
                new ObjectDisposedException("owner"));
        AssertPrimaryAndCleanup(
            gameplayWithCleanupFailure,
            I6C6ClosureHarnessExecutionStageV1.GameplayCapture,
            I6C6ClosureHarnessExceptionSiteV1.None,
            null,
            I6C6ClosureHarnessExceptionSiteV1.ExternalRuntimeOwnerDisposal,
            typeof(ObjectDisposedException));
        True(ReferenceEquals(capture, gameplayWithCleanupFailure.Capture));

        I6C6ClosureHarnessExecutionResultV1 constructionFailure =
            I6C6ClosureHarnessV1.UnexpectedExceptionResultForTest(
                I6C6ClosureHarnessExceptionSiteV1.RunnerConstruction,
                new ArgumentException(),
                processStarted: false);
        I6C6ClosureHarnessExecutionResultV1 constructionWithCleanupFailure =
            I6C6ClosureHarnessV1.PreserveCleanupFailureForTest(
                constructionFailure,
                I6C6ClosureHarnessExceptionSiteV1.CaptureTransportDisposal,
                new IOException());
        AssertPrimaryAndCleanup(
            constructionWithCleanupFailure,
            I6C6ClosureHarnessExecutionStageV1.UnexpectedException,
            I6C6ClosureHarnessExceptionSiteV1.RunnerConstruction,
            typeof(ArgumentException),
            I6C6ClosureHarnessExceptionSiteV1.CaptureTransportDisposal,
            typeof(IOException));

        (I6C6ClosureHarnessExceptionSiteV1 Site, Type ExceptionType)[] cleanupOnly =
        {
            (
                I6C6ClosureHarnessExceptionSiteV1.RunnerDisposal,
                typeof(ObjectDisposedException)),
            (
                I6C6ClosureHarnessExceptionSiteV1.CaptureTransportDisposal,
                typeof(IOException)),
            (
                I6C6ClosureHarnessExceptionSiteV1.ExternalRuntimeOwnerDisposal,
                typeof(InvalidOperationException))
        };
        foreach ((I6C6ClosureHarnessExceptionSiteV1 site, Type exceptionType)
                     in cleanupOnly)
        {
            I6C6ClosureHarnessExecutionResultV1 success = new(
                I6C6ClosureHarnessErrorCodeV1.None,
                true,
                true,
                capture);
            Exception exception = (Exception)Activator.CreateInstance(
                exceptionType,
                exceptionType == typeof(ObjectDisposedException)
                    ? new object?[] { "resource" }
                    : Array.Empty<object>())!;
            I6C6ClosureHarnessExecutionResultV1 cleanupFailure =
                I6C6ClosureHarnessV1.PreserveCleanupFailureForTest(
                    success,
                    site,
                    exception);
            Equal(
                I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
                cleanupFailure.ErrorCode);
            False(cleanupFailure.GameplayCaptureSucceeded);
            True(ReferenceEquals(capture, cleanupFailure.Capture));
            NotNull(cleanupFailure.Diagnostics);
            Equal(
                I6C6ClosureHarnessExecutionStageV1.UnexpectedException,
                cleanupFailure.Diagnostics!.Value.Stage);
            Equal(site, cleanupFailure.Diagnostics!.Value.ExceptionSite);
            Equal(
                exceptionType.FullName,
                cleanupFailure.Diagnostics!.Value.ExceptionType);
            Null(cleanupFailure.CleanupDiagnostics);
        }
    }

    private static void AssertPrimaryAndCleanup(
        I6C6ClosureHarnessExecutionResultV1 result,
        I6C6ClosureHarnessExecutionStageV1 primaryStage,
        I6C6ClosureHarnessExceptionSiteV1 primarySite,
        Type? primaryExceptionType,
        I6C6ClosureHarnessExceptionSiteV1 cleanupSite,
        Type cleanupExceptionType)
    {
        NotNull(result.Diagnostics);
        Equal(primaryStage, result.Diagnostics!.Value.Stage);
        Equal(primarySite, result.Diagnostics!.Value.ExceptionSite);
        Equal(
            primaryExceptionType?.FullName,
            result.Diagnostics!.Value.ExceptionType);
        NotNull(result.CleanupDiagnostics);
        Equal(
            cleanupSite,
            result.CleanupDiagnostics!.Value.ExceptionSite);
        Equal(
            cleanupExceptionType.FullName,
            result.CleanupDiagnostics!.Value.ExceptionType);
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

    internal static void TestPreDuelDuelStartRequiresAllLobbyPredicates()
    {
        RunDuelStartGatingCase(
            "remote player is not ready",
            PreDuelLobbyFrames(),
            new[] { PreDuelPlayerChange(0x09) },
            rejectDuelStart: false,
            expectedHsStartAttempts: 0,
            expectedHsStartWrites: 0,
            expectEarlyHsStart: false,
            expectPumpAfterLocalReady: true,
            blockWhenEmpty: true);

        RunDuelStartGatingCase(
            "remote player becomes ready",
            PreDuelLobbyFrames(),
            new[]
            {
                PreDuelPlayerChange(0x09),
                PreDuelPlayerChange(0x19)
            },
            rejectDuelStart: false,
            expectedHsStartAttempts: 1,
            expectedHsStartWrites: 1,
            expectEarlyHsStart: false,
            expectPumpAfterLocalReady: false,
            expectSuccessfulHandoff: true);

        RunDuelStartGatingCase(
            "client is not host",
            PreDuelLobbyFrames(typeChange: 0x00),
            new[]
            {
                PreDuelPlayerChange(0x09),
                PreDuelPlayerChange(0x19)
            },
            rejectDuelStart: false,
            expectedHsStartAttempts: 0,
            expectedHsStartWrites: 0,
            expectEarlyHsStart: false,
            expectPumpAfterLocalReady: true,
            blockWhenEmpty: true);

        RunDuelStartGatingCase(
            "second slot is unoccupied",
            PreDuelLobbyFrames(includeOpponent: false),
            new[] { PreDuelPlayerChange(0x09) },
            rejectDuelStart: false,
            expectedHsStartAttempts: 0,
            expectedHsStartWrites: 0,
            expectEarlyHsStart: false,
            expectPumpAfterLocalReady: true,
            blockWhenEmpty: true);

        RunDuelStartGatingCase(
            "legal preflight with rejected I2 send",
            PreDuelLobbyFrames(),
            new[]
            {
                PreDuelPlayerChange(0x09),
                PreDuelPlayerChange(0x19)
            },
            rejectDuelStart: true,
            expectedHsStartAttempts: 1,
            expectedHsStartWrites: 0,
            expectEarlyHsStart: false,
            expectPumpAfterLocalReady: false,
            expectedError: I2ErrorCode.SendFailed,
            expectedFailureStage:
                I6C6ClosureHarnessPreDuelFailureStageV1.DuelStartRequest,
            expectSuccessfulHandoff: false);
    }

    private static void RunDuelStartGatingCase(
        string name,
        byte[] lobbyFrame,
        byte[][] readyFrames,
        bool rejectDuelStart,
        int expectedHsStartAttempts,
        int expectedHsStartWrites,
        bool expectEarlyHsStart,
        bool expectPumpAfterLocalReady,
        bool blockWhenEmpty = false,
        bool expectSuccessfulHandoff = false,
        I2ErrorCode? expectedError = null,
        I6C6ClosureHarnessPreDuelFailureStageV1 expectedFailureStage =
            I6C6ClosureHarnessPreDuelFailureStageV1.None)
    {
        using CancellationTokenSource cancellation = new();
        DuelStartGatingTransport transport = new(
            new[] { lobbyFrame },
            readyFrames,
            rejectDuelStart,
            blockWhenEmpty);
        I2SessionRunner runner = new(transport);
        Task<I6C6ClosureHarnessV1.I6C6PreDuelHandoffResultV1>? driveTask =
            null;
        try
        {
            I2Result started = runner.StartAsync(
                    PreDuelTestConnection(),
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            True(started.IsSuccess, name);

            if (blockWhenEmpty)
            {
                driveTask = Task.Run(
                    () => I6C6ClosureHarnessV1.DriveToGameplayAsync(
                            runner,
                            new PrevalidatedProtocolDeck(
                                new uint[] { 1 },
                                Array.Empty<uint>()),
                            1,
                            0,
                            cancellation.Token)
                        .GetAwaiter()
                        .GetResult());
                Task completed = Task.WhenAny(
                        transport.EmptyReadStarted,
                        driveTask)
                    .GetAwaiter()
                    .GetResult();
                True(
                    ReferenceEquals(completed, transport.EmptyReadStarted),
                    name);
                True(runner.State == I2SessionState.Ready, name);
                True(transport.HsStartAttemptCount == 0, name);
                transport.ReleaseEmptyRead();
            }

            I6C6ClosureHarnessV1.I6C6PreDuelHandoffResultV1 handoff =
                driveTask?.GetAwaiter().GetResult() ??
                I6C6ClosureHarnessV1.DriveToGameplayAsync(
                        runner,
                        new PrevalidatedProtocolDeck(
                            new uint[] { 1 },
                            Array.Empty<uint>()),
                        1,
                        0,
                        cancellation.Token)
                    .GetAwaiter()
                    .GetResult();

            True(
                expectedHsStartAttempts == transport.HsStartAttemptCount,
                name);
            True(
                expectedHsStartWrites == transport.HsStartWriteCount,
                name);
            True(expectEarlyHsStart == transport.EarlyHsStart, name);
            True(
                expectPumpAfterLocalReady ==
                    transport.PumpContinuedAfterLocalReady,
                name);

            if (expectedError is I2ErrorCode error)
            {
                Equal(error, handoff.ErrorCode);
                Equal(expectedFailureStage, handoff.FailureStage);
            }

            if (expectSuccessfulHandoff)
            {
                True(handoff.IsSuccess, name);
                Equal(I2SessionState.HandedOff, runner.State);
            }
        }
        finally
        {
            transport.ReleaseEmptyRead();
            runner.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    internal static void TestExternalRuntimeListenerReadinessBarrier()
    {
        IPEndPoint expectedListener = new(IPAddress.Loopback, 7911);
        Queue<IReadOnlyList<IPEndPoint>> laterSnapshots = new(
            new IReadOnlyList<IPEndPoint>[]
            {
                Array.Empty<IPEndPoint>(),
                new[] { expectedListener }
            });
        I6C6ExternalRuntimeReadinessResultV1 later =
            I6C6ExternalRuntimeProcessOwnerV1
                .WaitForListenerForTestAsync(
                    () => laterSnapshots.Count == 0
                        ? new[] { expectedListener }
                        : laterSnapshots.Dequeue(),
                    () => true,
                    7911,
                    TimeSpan.FromSeconds(1),
                    CancellationToken.None,
                    TimeSpan.Zero)
                .GetAwaiter()
                .GetResult();
        Equal(I6C6ExternalRuntimeReadinessResultV1.Ready, later);

        I6C6ExternalRuntimeReadinessResultV1 wrongPort =
            I6C6ExternalRuntimeProcessOwnerV1
                .WaitForListenerForTestAsync(
                    () => new[]
                    {
                        new IPEndPoint(IPAddress.Loopback, 7912)
                    },
                    () => true,
                    7911,
                    TimeSpan.Zero,
                    CancellationToken.None,
                    TimeSpan.Zero)
                .GetAwaiter()
                .GetResult();
        Equal(
            I6C6ExternalRuntimeReadinessResultV1.DeadlineExpired,
            wrongPort);

        I6C6ExternalRuntimeReadinessResultV1 wildcard =
            I6C6ExternalRuntimeProcessOwnerV1
                .WaitForListenerForTestAsync(
                    () => new[]
                    {
                        new IPEndPoint(IPAddress.Any, 7911)
                    },
                    () => true,
                    7911,
                    TimeSpan.Zero,
                    CancellationToken.None,
                    TimeSpan.Zero)
                .GetAwaiter()
                .GetResult();
        Equal(
            I6C6ExternalRuntimeReadinessResultV1.DeadlineExpired,
            wildcard);

        I6C6ExternalRuntimeReadinessResultV1 conflicting =
            I6C6ExternalRuntimeProcessOwnerV1
                .WaitForListenerForTestAsync(
                    () => new[]
                    {
                        expectedListener,
                        new IPEndPoint(IPAddress.Any, 7911)
                    },
                    () => true,
                    7911,
                    TimeSpan.Zero,
                    CancellationToken.None,
                    TimeSpan.Zero)
                .GetAwaiter()
                .GetResult();
        Equal(
            I6C6ExternalRuntimeReadinessResultV1.DeadlineExpired,
            conflicting);

        I6C6ExternalRuntimeReadinessResultV1 exited =
            I6C6ExternalRuntimeProcessOwnerV1
                .WaitForListenerForTestAsync(
                    () => Array.Empty<IPEndPoint>(),
                    () => false,
                    7911,
                    TimeSpan.FromSeconds(1),
                    CancellationToken.None,
                    TimeSpan.Zero)
                .GetAwaiter()
                .GetResult();
        Equal(
            I6C6ExternalRuntimeReadinessResultV1.ProcessExited,
            exited);

        I6C6ExternalRuntimeReadinessResultV1 ownedListener =
            I6C6ExternalRuntimeProcessOwnerV1
                .WaitForListenerForTestAsync(
                    () => new[] { expectedListener },
                    () => true,
                    7911,
                    TimeSpan.FromSeconds(1),
                    CancellationToken.None,
                    TimeSpan.Zero,
                    _ => I6C6ExternalRuntimeListenerOwnershipResultV1.Owned)
                .GetAwaiter()
                .GetResult();
        Equal(I6C6ExternalRuntimeReadinessResultV1.Ready, ownedListener);

        I6C6ExternalRuntimeReadinessResultV1 foreignListener =
            I6C6ExternalRuntimeProcessOwnerV1
                .WaitForListenerForTestAsync(
                    () => new[] { expectedListener },
                    () => true,
                    7911,
                    TimeSpan.FromSeconds(1),
                    CancellationToken.None,
                    TimeSpan.Zero,
                    _ => I6C6ExternalRuntimeListenerOwnershipResultV1.NotOwned)
                .GetAwaiter()
                .GetResult();
        Equal(
            I6C6ExternalRuntimeReadinessResultV1.ListenerOwnershipMismatch,
            foreignListener);

        I6C6ExternalRuntimeReadinessResultV1 unavailableOwnership =
            I6C6ExternalRuntimeProcessOwnerV1
                .WaitForListenerForTestAsync(
                    () => new[] { expectedListener },
                    () => true,
                    7911,
                    TimeSpan.FromSeconds(1),
                    CancellationToken.None,
                    TimeSpan.Zero,
                    _ => I6C6ExternalRuntimeListenerOwnershipResultV1.Unavailable)
                .GetAwaiter()
                .GetResult();
        Equal(
            I6C6ExternalRuntimeReadinessResultV1.ObservationFailed,
            unavailableOwnership);

        if (OperatingSystem.IsWindows())
        {
            TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                IPEndPoint ownedEndpoint =
                    (IPEndPoint)listener.LocalEndpoint;
                Equal(
                    I6C6ExternalRuntimeListenerOwnershipResultV1.Owned,
                    I6C6ExternalRuntimeProcessOwnerV1
                        .GetListenerOwnershipForTest(
                            ownedEndpoint,
                            Process.GetCurrentProcess().Id));
                Equal(
                    I6C6ExternalRuntimeListenerOwnershipResultV1.NotOwned,
                    I6C6ExternalRuntimeProcessOwnerV1
                        .GetListenerOwnershipForTest(ownedEndpoint, -1));
            }
            finally
            {
                listener.Stop();
            }
        }

        I6C6ExternalRuntimeReadinessResultV1 deadline =
            I6C6ExternalRuntimeProcessOwnerV1
                .WaitForListenerForTestAsync(
                    () => Array.Empty<IPEndPoint>(),
                    () => true,
                    7911,
                    TimeSpan.Zero,
                    CancellationToken.None,
                    TimeSpan.Zero)
                .GetAwaiter()
                .GetResult();
        Equal(
            I6C6ExternalRuntimeReadinessResultV1.DeadlineExpired,
            deadline);

        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();
        I6C6ExternalRuntimeReadinessResultV1 cancellation =
            I6C6ExternalRuntimeProcessOwnerV1
                .WaitForListenerForTestAsync(
                    () => Array.Empty<IPEndPoint>(),
                    () => true,
                    7911,
                    TimeSpan.FromSeconds(1),
                    cancelled.Token,
                    TimeSpan.Zero)
                .GetAwaiter()
                .GetResult();
        Equal(
            I6C6ExternalRuntimeReadinessResultV1.Cancelled,
            cancellation);

        I6C6ExternalRuntimeReadinessResultV1 observationFailure =
            I6C6ExternalRuntimeProcessOwnerV1
                .WaitForListenerForTestAsync(
                    () => throw new InvalidOperationException(),
                    () => true,
                    7911,
                    TimeSpan.FromSeconds(1),
                    CancellationToken.None,
                    TimeSpan.Zero)
                .GetAwaiter()
                .GetResult();
        Equal(
            I6C6ExternalRuntimeReadinessResultV1.ObservationFailed,
            observationFailure);

    }

    internal static void TestExternalRuntimeReadinessGatesI2Start()
    {
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource<bool> runtimeStartEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseRuntimeStart =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        CountingTransport transport = new();
        I2SessionRunner runner = new(transport);

        try
        {
            Task<I2Result> sequence =
                I6C6ExternalRuntimeStartupSequenceV1
                    .RunAsync(
                        async token =>
                        {
                            runtimeStartEntered.TrySetResult(true);
                            await releaseRuntimeStart.Task.WaitAsync(token)
                                .ConfigureAwait(false);
                            return new object();
                        },
                        (_, token) => runner.StartAsync(
                            PreDuelTestConnection(),
                            token),
                        cancellation.Token)
                    .AsTask();

            runtimeStartEntered.Task.GetAwaiter().GetResult();
            Equal(0, transport.ConnectCount);

            releaseRuntimeStart.TrySetResult(true);
            I2Result started = sequence.GetAwaiter().GetResult();
            True(started.IsSuccess);
            Equal(1, transport.ConnectCount);
        }
        finally
        {
            runner.DisposeAsync().GetAwaiter().GetResult();
        }

        CountingTransport failedTransport = new();
        I2SessionRunner failedRunner = new(failedTransport);
        try
        {
            try
            {
                I6C6ExternalRuntimeStartupSequenceV1
                    .RunAsync<object, I2Result>(
                        _ => throw new InvalidOperationException(
                            "runtime readiness failed"),
                        (_, token) => failedRunner.StartAsync(
                            PreDuelTestConnection(),
                            token),
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
                throw new InvalidOperationException(
                    "failed runtime startup was accepted");
            }
            catch (InvalidOperationException exception)
                when (exception.Message == "runtime readiness failed")
            {
            }

            Equal(0, failedTransport.ConnectCount);
        }
        finally
        {
            failedRunner.DisposeAsync().GetAwaiter().GetResult();
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

    private static byte[] PreDuelLobbyFrames(
        byte typeChange = 0x10,
        bool includeOpponent = true) =>
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
                    new StocTypeChangePayload(typeChange))),
            WireFrameCodec.EncodeStoc(
                StocPacketType.HsPlayerEnter,
                PacketPayloadCodec.EncodeStocHsPlayerEnter(
                    new StocHsPlayerEnterPayload("Ignis", 0))),
            includeOpponent
                ? WireFrameCodec.EncodeStoc(
                    StocPacketType.HsPlayerEnter,
                    PacketPayloadCodec.EncodeStocHsPlayerEnter(
                        new StocHsPlayerEnterPayload("Opponent", 1)))
                : Array.Empty<byte>());

    private static byte[] PreDuelPlayerChange(byte status) =>
        WireFrameCodec.EncodeStoc(
            StocPacketType.HsPlayerChange,
            PacketPayloadCodec.EncodeStocHsPlayerChange(
                new StocHsPlayerChangePayload(status)));

    private static byte[] PreDuelDuelStartFrame() =>
        WireFrameCodec.EncodeStoc(
            StocPacketType.DuelStart,
            Array.Empty<byte>());

    private static byte[] PreDuelSelectHandFrame() =>
        WireFrameCodec.EncodeStoc(
            StocPacketType.SelectHand,
            Array.Empty<byte>());

    private static byte[] PreDuelHandLossFrame() =>
        WireFrameCodec.EncodeStoc(
            StocPacketType.HandResult,
            PacketPayloadCodec.EncodeStocHandResult(
                new StocHandResultPayload(1, 2)));

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
            I6C6OpponentRuntimeParticipantLeaseV1.TryStartOwnedProcess(
                CounterScenario(),
                0);

        False(result.IsSuccess);
        Equal(
            I6C6ClosureHarnessErrorCodeV1.ScenarioInputProvenanceMismatch,
            result.ErrorCode);
        Null(result.Lease);
    }

    internal static void TestOpponentParticipantArgumentsAreTokenExact()
    {
        const string expectedDeck =
            @"C:\ProjectIgnis\WindBot\Decks\AI_CyberDragon.ydk";

        ProcessStartInfo canonical =
            I6C6OpponentRuntimeParticipantLeaseV1.CreateStartInfo(
                CounterScenario(),
                7911);
        Equal(3, canonical.ArgumentList.Count);
        Equal($"DeckFile={expectedDeck}", canonical.ArgumentList[0]);
        Equal("Port=7911", canonical.ArgumentList[1]);
        Equal("Version=0x000B0029", canonical.ArgumentList[2]);
        True(
            I6C6OpponentRuntimeParticipantLeaseV1.HasProcessInputForTest(
                canonical,
                expectedDeck));
        True(
            I6C6OpponentRuntimeParticipantLeaseV1.HasPortInputForTest(
                canonical,
                7911));
        True(
            I6C6OpponentRuntimeParticipantLeaseV1.HasVersionInputForTest(
                canonical));

        ProcessStartInfo validArgumentList = new()
        {
            FileName = "WindBot.exe"
        };
        validArgumentList.ArgumentList.Add($"DeckFile={expectedDeck}");
        validArgumentList.ArgumentList.Add("Port=7911");
        True(
            I6C6OpponentRuntimeParticipantLeaseV1.HasProcessInputForTest(
                validArgumentList,
                expectedDeck));
        True(
            I6C6OpponentRuntimeParticipantLeaseV1.HasPortInputForTest(
                validArgumentList,
                7911));

        ProcessStartInfo literalQuotedArgumentList = new()
        {
            FileName = "WindBot.exe"
        };
        literalQuotedArgumentList.ArgumentList.Add($"DeckFile=\"{expectedDeck}\"");
        literalQuotedArgumentList.ArgumentList.Add("Port=7911");
        False(
            I6C6OpponentRuntimeParticipantLeaseV1.HasProcessInputForTest(
                literalQuotedArgumentList,
                expectedDeck));

        ProcessStartInfo missingVersion = new()
        {
            FileName = "WindBot.exe"
        };
        missingVersion.ArgumentList.Add($"DeckFile={expectedDeck}");
        missingVersion.ArgumentList.Add("Port=7911");
        False(
            I6C6OpponentRuntimeParticipantLeaseV1.HasVersionInputForTest(
                missingVersion));

        ProcessStartInfo wrongVersion = new()
        {
            FileName = "WindBot.exe"
        };
        wrongVersion.ArgumentList.Add($"DeckFile={expectedDeck}");
        wrongVersion.ArgumentList.Add("Port=7911");
        wrongVersion.ArgumentList.Add("Version=0x000A0128");
        False(
            I6C6OpponentRuntimeParticipantLeaseV1.HasVersionInputForTest(
                wrongVersion));

        ProcessStartInfo literalQuotedVersion = new()
        {
            FileName = "WindBot.exe"
        };
        literalQuotedVersion.ArgumentList.Add($"DeckFile={expectedDeck}");
        literalQuotedVersion.ArgumentList.Add("Port=7911");
        literalQuotedVersion.ArgumentList.Add("Version=\"0x000B0029\"");
        False(
            I6C6OpponentRuntimeParticipantLeaseV1.HasVersionInputForTest(
                literalQuotedVersion));

        ProcessStartInfo duplicateVersion = new()
        {
            FileName = "WindBot.exe"
        };
        duplicateVersion.ArgumentList.Add($"DeckFile={expectedDeck}");
        duplicateVersion.ArgumentList.Add("Port=7911");
        duplicateVersion.ArgumentList.Add("Version=0x000B0029");
        duplicateVersion.ArgumentList.Add("Version=0x000B0029");
        False(
            I6C6OpponentRuntimeParticipantLeaseV1.HasVersionInputForTest(
                duplicateVersion));

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
            Arguments =
                $"DeckFile=\"{expectedDeck}\" Port=7911 " +
                "Version=0x000B0029"
        };
        True(
            I6C6OpponentRuntimeParticipantLeaseV1.HasProcessInputForTest(
                validRawArguments,
                expectedDeck));
        True(
            I6C6OpponentRuntimeParticipantLeaseV1.HasPortInputForTest(
                validRawArguments,
                7911));
        True(
            I6C6OpponentRuntimeParticipantLeaseV1.HasVersionInputForTest(
                validRawArguments));

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

    internal static void TestOpponentParticipantOwnsCanonicalLaunch()
    {
        const string expectedDeck =
            @"C:\ProjectIgnis\WindBot\Decks\AI_CyberDragon.ydk";
        Type leaseType =
            typeof(I6C6OpponentRuntimeParticipantLeaseV1);
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;

        MethodInfo? ownedStart = leaseType.GetMethod(
            "TryStartOwnedProcess",
            flags);
        NotNull(ownedStart);

        MethodInfo? legacyAdoption = leaseType.GetMethod(
            "TryCreateFromOwnedProcess",
            flags);
        Null(legacyAdoption);

        MethodInfo? validate = leaseType.GetMethod(
            "ValidateStartInfoForTest",
            flags);
        NotNull(validate);

        MethodInfo? factory = leaseType.GetMethods(flags)
            .SingleOrDefault(method =>
                method.Name == "CreateStartInfo" &&
                method.GetParameters().Length == 2);
        NotNull(factory);
        ProcessStartInfo canonical = (ProcessStartInfo)factory!.Invoke(
            null,
            new object[] { CounterScenario(), 7911 })!;

        bool IsValid(ProcessStartInfo value) =>
            (bool)validate!.Invoke(
                null,
                new object[] { value, expectedDeck, 7911 })!;

        True(IsValid(canonical));

        ProcessStartInfo wrongExecutable = CloneStartInfo(canonical);
        wrongExecutable.FileName =
            @"C:\ProjectIgnis\WindBot\Other.exe";
        False(IsValid(wrongExecutable));

        ProcessStartInfo wrongWorkingDirectory = CloneStartInfo(canonical);
        wrongWorkingDirectory.WorkingDirectory =
            @"C:\ProjectIgnis";
        False(IsValid(wrongWorkingDirectory));

        ProcessStartInfo quotedDeck = CloneStartInfo(canonical);
        quotedDeck.ArgumentList[0] =
            $"DeckFile=\"{expectedDeck}\"";
        False(IsValid(quotedDeck));

        ProcessStartInfo wrongDeck = CloneStartInfo(canonical);
        wrongDeck.ArgumentList[0] =
            $"DeckFile={expectedDeck}.bak";
        False(IsValid(wrongDeck));

        ProcessStartInfo wrongPort = CloneStartInfo(canonical);
        wrongPort.ArgumentList[1] = "Port=7912";
        False(IsValid(wrongPort));

        ProcessStartInfo missingVersion = CloneStartInfo(canonical);
        missingVersion.ArgumentList.RemoveAt(2);
        False(IsValid(missingVersion));

        ProcessStartInfo wrongVersion = CloneStartInfo(canonical);
        wrongVersion.ArgumentList[2] = "Version=0x000A0128";
        False(IsValid(wrongVersion));

        ProcessStartInfo duplicateVersion = CloneStartInfo(canonical);
        duplicateVersion.ArgumentList.Add("Version=0x000B0029");
        False(IsValid(duplicateVersion));
    }

    internal static void TestOpponentParticipantStartFailuresFailClosed()
    {
        Type leaseType =
            typeof(I6C6OpponentRuntimeParticipantLeaseV1);
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        MethodInfo? startForTest = leaseType.GetMethod(
            "TryStartProcessForTest",
            flags);
        NotNull(startForTest);

        ProcessStartInfo missingExecutable = new(
            Path.Combine(
                Path.GetTempPath(),
                "ocgforge-ignis-missing-windbot.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        I6C6ClosureHarnessErrorCodeV1 startFailure =
            (I6C6ClosureHarnessErrorCodeV1)startForTest!.Invoke(
                null,
                new object[] { missingExecutable })!;
        Equal(
            I6C6ClosureHarnessErrorCodeV1.RuntimeArtifactUnavailable,
            startFailure);

        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        MethodInfo? classifyExitedForTest = leaseType.GetMethod(
            "TryClassifyStartedProcessForTest",
            flags);
        NotNull(classifyExitedForTest);

        ProcessStartInfo exitsImmediately = new("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        exitsImmediately.ArgumentList.Add("/c");
        exitsImmediately.ArgumentList.Add("exit 0");
        using Process exited = Process.Start(exitsImmediately)!;
        True(exited.WaitForExit(5000));
        I6C6ClosureHarnessErrorCodeV1 immediateExit =
            (I6C6ClosureHarnessErrorCodeV1)classifyExitedForTest!.Invoke(
                null,
                new object[] { exited })!;
        Equal(
            I6C6ClosureHarnessErrorCodeV1.RuntimeArtifactUnavailable,
            immediateExit);
    }

    private static ProcessStartInfo CloneStartInfo(ProcessStartInfo source)
    {
        ProcessStartInfo clone = new(source.FileName)
        {
            WorkingDirectory = source.WorkingDirectory,
            UseShellExecute = source.UseShellExecute,
            CreateNoWindow = source.CreateNoWindow
        };
        foreach (string argument in source.ArgumentList)
        {
            clone.ArgumentList.Add(argument);
        }

        return clone;
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

    private sealed class CountingTransport : IByteTransport
    {
        internal int ConnectCount { get; private set; }

        public ValueTask ConnectAsync(
            string host,
            int port,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConnectCount++;
            return ValueTask.CompletedTask;
        }

        public ValueTask<int> ReadAsync(
            Memory<byte> destination,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(0);
        }

        public ValueTask WriteAsync(
            ReadOnlyMemory<byte> source,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }

        public ValueTask CloseAsync() => ValueTask.CompletedTask;

        public ValueTask DisposeAsync() => CloseAsync();
    }

    private sealed class DuelStartGatingTransport : IByteTransport
    {
        private readonly Queue<QueuedChunk> chunks = new();
        private readonly byte[][] readyFrames;
        private readonly bool rejectDuelStart;
        private readonly bool blockWhenEmpty;
        private readonly TaskCompletionSource<bool> emptyReadStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> releaseEmptyRead =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private QueuedChunk? currentChunk;
        private int currentOffset;
        private bool localReadyObserved;
        private bool remoteReadyObserved;
        private bool emptyReadSignaled;

        internal DuelStartGatingTransport(
            IEnumerable<byte[]> lobbyFrames,
            IEnumerable<byte[]> readyFrames,
            bool rejectDuelStart,
            bool blockWhenEmpty)
        {
            foreach (byte[] frame in lobbyFrames)
            {
                chunks.Enqueue(new(frame.ToArray(), false, false));
            }

            this.readyFrames = readyFrames
                .Select(frame => frame.ToArray())
                .ToArray();
            this.rejectDuelStart = rejectDuelStart;
            this.blockWhenEmpty = blockWhenEmpty;
        }

        internal int HsStartAttemptCount { get; private set; }

        internal int HsStartWriteCount { get; private set; }

        internal bool EarlyHsStart { get; private set; }

        internal bool PumpContinuedAfterLocalReady { get; private set; }

        internal Task EmptyReadStarted => emptyReadStarted.Task;

        internal void ReleaseEmptyRead() => releaseEmptyRead.TrySetResult(true);

        public ValueTask ConnectAsync(
            string host,
            int port,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }

        public async ValueTask<int> ReadAsync(
            Memory<byte> destination,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            while (currentChunk is null || currentOffset == currentChunk.Bytes.Length)
            {
                if (chunks.Count == 0)
                {
                    if (localReadyObserved && !PumpContinuedAfterLocalReady)
                    {
                        PumpContinuedAfterLocalReady = true;
                    }

                    if (blockWhenEmpty && !emptyReadSignaled)
                    {
                        emptyReadSignaled = true;
                        emptyReadStarted.TrySetResult(true);
                        await releaseEmptyRead.Task.WaitAsync(
                                cancellationToken)
                            .ConfigureAwait(false);
                    }

                    return 0;
                }

                currentChunk = chunks.Dequeue();
                currentOffset = 0;
                if (currentChunk.LocalReady)
                {
                    localReadyObserved = true;
                }

                if (currentChunk.RemoteReady)
                {
                    remoteReadyObserved = true;
                }
            }

            int count = Math.Min(
                destination.Length,
                currentChunk.Bytes.Length - currentOffset);
            currentChunk.Bytes.AsMemory(currentOffset, count).CopyTo(destination);
            currentOffset += count;
            return count;
        }

        public ValueTask WriteAsync(
            ReadOnlyMemory<byte> source,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FrameReadResult<CtosFrame> parsed = WireFrameCodec.TryReadCtos(
                source.Span);
            if (parsed.Status != FrameReadStatus.Success ||
                parsed.Frame is null)
            {
                throw new InvalidDataException("invalid scripted CTOS frame");
            }

            switch (parsed.Frame.Type)
            {
                case CtosPacketType.UpdateDeck:
                    chunks.Enqueue(
                        new(PreDuelWatchFrame(), false, false));
                    break;

                case CtosPacketType.HsReady:
                    for (int index = 0; index < readyFrames.Length; index++)
                    {
                        chunks.Enqueue(
                            new(
                                readyFrames[index],
                                LocalReady: index == 0,
                                RemoteReady: index == 1));
                    }

                    break;

                case CtosPacketType.HsStart:
                    HsStartAttemptCount++;
                    if (!remoteReadyObserved)
                    {
                        EarlyHsStart = true;
                        throw new InvalidOperationException(
                            "HsStart was attempted before remote ready.");
                    }

                    if (rejectDuelStart)
                    {
                        throw new InvalidOperationException(
                            "scripted duel start send failure");
                    }

                    HsStartWriteCount++;
                    chunks.Enqueue(
                        new(PreDuelDuelStartFrame(), false, false));
                    chunks.Enqueue(
                        new(PreDuelSelectHandFrame(), false, false));
                    break;

                case CtosPacketType.HandResult:
                    chunks.Enqueue(
                        new(PreDuelHandLossFrame(), false, false));
                    break;
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask CloseAsync()
        {
            ReleaseEmptyRead();
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync() => CloseAsync();

        private sealed record QueuedChunk(
            byte[] Bytes,
            bool LocalReady,
            bool RemoteReady);
    }
}
