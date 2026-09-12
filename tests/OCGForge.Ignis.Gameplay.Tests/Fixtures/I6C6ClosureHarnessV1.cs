using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;

namespace OCGForge.Ignis.Gameplay.Tests.Fixtures;

internal enum I6C6ClosureHarnessErrorCodeV1 : byte
{
    None = 0,
    RuntimeProvenanceMismatch = 1,
    ForbiddenRuntimeExecutable = 2,
    AssetRootInvalid = 3,
    ScenarioConfigurationInvalid = 4,
    RealRunNotAuthorized = 5,
    SyntheticEvidenceRejected = 6,
    IncompleteLiveEvidence = 7,
    RuntimeArtifactUnavailable = 8,
    ScenarioInputProvenanceMismatch = 9,
    UnsupportedPreDuelState = 10,
    ExecutionFailed = 11,
    RealExecutionRequiresInputs = 12,
    PropertyEvidenceMissing = 13,
    PropertyValueMismatch = 14,
    InvalidPropertyEvidenceInput = 15
}

internal enum I6C6ClosureScenarioKindV1 : byte
{
    Link = 1,
    Counter = 2
}

internal enum I6C6ClosureHarnessExecutionStageV1 : byte
{
    None = 0,
    ExternalRuntimeStart = 1,
    SessionStart = 2,
    DeckLoad = 3,
    PreDuelDrive = 4,
    GameplayCapture = 5,
    Cancelled = 6,
    UnexpectedException = 7
}

internal enum I6C6ClosureHarnessExceptionSiteV1 : byte
{
    None = 0,
    TransportConstruction = 1,
    RunnerConstruction = 2,
    ExternalRuntimeStartReadiness = 3,
    SessionStart = 4,
    DeckLoad = 5,
    PreDuelDrive = 6,
    GameplayCapture = 7,
    RunnerDisposal = 8,
    CaptureTransportDisposal = 9,
    ExternalRuntimeOwnerDisposal = 10
}

internal enum I6C6ClosureHarnessGameplayCaptureSubsiteV1 : byte
{
    None = 0,
    HandoffClaim = 1,
    InitialGameplayPump = 2,
    MirrorConstruction = 3,
    MirrorSessionConstruction = 4,
    InitialFrameConstruction = 5,
    InitialFrameFailureDiagnostics = 6,
    ObservationConstruction = 7,
    SubsequentGameplayPump = 8,
    SubsequentFrameConstruction = 9,
    SubsequentFrameFailureDiagnostics = 10,
    CaptureFinalization = 11
}

internal enum I6C6ClosureHarnessPreDuelFailureStageV1 : byte
{
    None = 0,
    PumpRead = 1,
    RpsSelection = 2,
    TurnPreferenceSelection = 3,
    DeckSubmission = 4,
    ReadyRequest = 5,
    DuelStartRequest = 6,
    RuntimeHandoff = 7,
    InvalidState = 8
}

internal readonly record struct I6C6ClosureHarnessExecutionDiagnosticsV1(
    I6C6ClosureHarnessExecutionStageV1 Stage,
    I2ErrorCode I2ErrorCode = I2ErrorCode.None,
    I6C6ClosureHarnessPreDuelFailureStageV1 PreDuelStage =
        I6C6ClosureHarnessPreDuelFailureStageV1.None,
    I6C6ExternalRuntimeReadinessResultV1 Readiness =
        I6C6ExternalRuntimeReadinessResultV1.None,
    I6C6ClosureHarnessExceptionSiteV1 ExceptionSite =
        I6C6ClosureHarnessExceptionSiteV1.None,
    string? ExceptionType = null,
    I6C6ClosureHarnessGameplayCaptureSubsiteV1 GameplayCaptureSubsite =
        I6C6ClosureHarnessGameplayCaptureSubsiteV1.None)
{
    internal static I6C6ClosureHarnessExecutionDiagnosticsV1 ExternalRuntimeStart(
        I6C6ExternalRuntimeReadinessResultV1 readiness =
            I6C6ExternalRuntimeReadinessResultV1.None) =>
        new(
            I6C6ClosureHarnessExecutionStageV1.ExternalRuntimeStart,
            Readiness: readiness);

    internal static I6C6ClosureHarnessExecutionDiagnosticsV1
        SessionStart(I2ErrorCode error) =>
        new(
            I6C6ClosureHarnessExecutionStageV1.SessionStart,
            error);

    internal static I6C6ClosureHarnessExecutionDiagnosticsV1
        DeckLoad() =>
        new(I6C6ClosureHarnessExecutionStageV1.DeckLoad);

    internal static I6C6ClosureHarnessExecutionDiagnosticsV1
        PreDuelDrive(
            I2ErrorCode error,
            I6C6ClosureHarnessPreDuelFailureStageV1 stage) =>
        new(
            I6C6ClosureHarnessExecutionStageV1.PreDuelDrive,
            error,
            stage);

    internal static I6C6ClosureHarnessExecutionDiagnosticsV1
        GameplayCapture() =>
        new(I6C6ClosureHarnessExecutionStageV1.GameplayCapture);

    internal static I6C6ClosureHarnessExecutionDiagnosticsV1
        Cancelled(
            I2ErrorCode error = I2ErrorCode.Cancelled,
            I6C6ClosureHarnessPreDuelFailureStageV1 stage =
                I6C6ClosureHarnessPreDuelFailureStageV1.None,
            I6C6ExternalRuntimeReadinessResultV1 readiness =
                I6C6ExternalRuntimeReadinessResultV1.None) =>
        new(
            I6C6ClosureHarnessExecutionStageV1.Cancelled,
            error,
            stage,
            readiness);

    internal static I6C6ClosureHarnessExecutionDiagnosticsV1
        UnexpectedException() =>
        new(I6C6ClosureHarnessExecutionStageV1.UnexpectedException);

    internal static I6C6ClosureHarnessExecutionDiagnosticsV1
        UnexpectedException(
            I6C6ClosureHarnessExceptionSiteV1 site,
            Exception exception,
            I6C6ClosureHarnessGameplayCaptureSubsiteV1 gameplayCaptureSubsite =
                I6C6ClosureHarnessGameplayCaptureSubsiteV1.None) =>
        new(
            I6C6ClosureHarnessExecutionStageV1.UnexpectedException,
            ExceptionSite: site,
            ExceptionType: exception.GetType().FullName ??
                exception.GetType().Name,
            GameplayCaptureSubsite: gameplayCaptureSubsite);

    internal static I6C6ClosureHarnessExecutionDiagnosticsV1
        DeckLoad(Exception exception) =>
        new(
            I6C6ClosureHarnessExecutionStageV1.DeckLoad,
            ExceptionSite: I6C6ClosureHarnessExceptionSiteV1.DeckLoad,
            ExceptionType: exception.GetType().FullName ??
                exception.GetType().Name);
}

internal sealed record I6C6ClosureScenarioConfigurationV1(
    string ScenarioId,
    string PrimaryDeckPath,
    string PrimaryDeckSha256,
    string OpponentDeckPath,
    string OpponentDeckSha256,
    string PatchedLocation);

internal sealed record I6C6ClosureHarnessConfigurationV1(
    string RuntimeExecutablePath,
    string RuntimeExecutableSha256,
    string AssetRoot,
    string DatabaseSha256,
    string CardscriptsCommit,
    string EdoproRuntimeHead,
    string A2PatchCommit,
    string A2PatchsetSha256,
    string StartupCompatPatchCommit,
    string StartupCompatPatchsetSha256,
    string LoopbackHostPatchParent,
    string LoopbackHostPatchCommit,
    string LoopbackHostPatchsetSha256,
    string ServerBootstrapPatchParent,
    string ServerBootstrapPatchCommit,
    string ServerBootstrapPatchsetSha256,
    string ServerBootstrapRuntimeExecutableSha256,
    string TimerGuardParent,
    string TimerGuardCommit,
    string TimerGuardPatchsetSha256,
    string TimerGuardRuntimeExecutableSha256,
    string RngParent,
    string RngImplementationCommit,
    string RngKatCommit,
    string RngCombinedPatchsetSha256,
    string EvidenceRngId,
    ulong EvidenceRngRoot,
    string FinalRuntimeExecutableSha256,
    TimeSpan ExternalRuntimeReadinessTimeout,
    string ServerOnlyBootstrapFixParent,
    string ServerOnlyBootstrapFixCommit,
    string ServerOnlyBootstrapFixPatchsetSha256,
    string ServerOnlyBootstrapFixRuntimeExecutableSha256,
    I6C6ClosureScenarioConfigurationV1 LinkScenario,
    I6C6ClosureScenarioConfigurationV1 CounterScenario);

internal readonly record struct I6C6ClosureHarnessValidationResultV1(
    bool IsSuccess,
    I6C6ClosureHarnessErrorCodeV1 ErrorCode,
    int ExternalRuntimeProcessOwnerCount,
    int TcpCaptureOwnerCount,
    bool ReusesIgnisDecoder,
    bool ReusesIgnisMirror,
    bool ReusesCurrentPath,
    bool AllowsSyntheticEvidenceInRealMode,
    bool AllowsSyntheticLinkEvidenceInRealMode,
    bool AllowsSyntheticCounterEvidenceInRealMode);

internal readonly record struct I6C6ClosureHarnessExecutionResultV1(
    I6C6ClosureHarnessErrorCodeV1 ErrorCode,
    bool ProcessStarted,
    bool GameplayCaptureSucceeded,
    I6C6LiveGameplayCaptureResultV1? Capture = null,
    I6C6ClosureHarnessExecutionDiagnosticsV1? Diagnostics = null,
    I6C6ClosureHarnessExecutionDiagnosticsV1? CleanupDiagnostics = null);

internal readonly record struct I6C6ClosureEvidenceValidationResultV1(
    bool IsSuccess,
    I6C6ClosureHarnessErrorCodeV1 ErrorCode,
    I6C6LinkPropertyEvidenceResultV1? LinkEvidence = null,
    I6C6CounterPropertyEvidenceResultV1? CounterEvidence = null);

internal readonly record struct I6C6ClosureBindingResultV1(
    bool IsSuccess,
    I6C6ClosureHarnessErrorCodeV1 ErrorCode,
    I6C6ClosureHarnessBindingV1? Binding);

internal sealed class I6C6ClosureHarnessBindingV1
{
    private I6C6ClosureHarnessBindingV1(
        I6C6ClosureHarnessConfigurationV1 configuration,
        I6C6ClosureScenarioConfigurationV1 scenario)
    {
        Configuration = configuration;
        Scenario = scenario;
    }

    internal I6C6ClosureHarnessConfigurationV1 Configuration { get; }

    internal I6C6ClosureScenarioConfigurationV1 Scenario { get; }

    internal static I6C6ClosureHarnessBindingV1 Create(
        I6C6ClosureHarnessConfigurationV1 configuration,
        I6C6ClosureScenarioConfigurationV1 scenario)
    {
        I6C6ClosureHarnessValidationResultV1 validation =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                configuration,
                requireLocalArtifacts: true);
        if (!validation.IsSuccess)
        {
            throw new InvalidDataException(
                $"I6C6 runtime binding is invalid: {validation.ErrorCode}");
        }

        return new(configuration, scenario);
    }
}

internal enum I6C6CaptureFailureStageV1 : byte
{
    None = 0,
    HandoffAcquire = 1,
    InitialPump = 2,
    MirrorCreate = 3,
    InitialFrame = 4,
    SubsequentPump = 5,
    SubsequentFrame = 6,
    ReadinessLimit = 7
}

internal enum I6C6GameplayMessageClassV1 : byte
{
    Unknown = 0,
    ExistingI4Prompt = 1,
    ExistingI5Prompt = 2,
    KnownNonPromptGameplayMessage = 3
}

internal enum I6C6PromptParseResultV1 : byte
{
    NotApplicable = 0,
    Fail = 1,
    Pass = 2
}

internal readonly record struct I6C6UnknownGameplayMessageClassificationV1(
    byte InnerMessageId,
    I6C6GameplayMessageClassV1 MessageClass,
    FlatPromptFamilyV1? PromptFamily,
    byte? PromptPlayer,
    I6C6PromptParseResultV1 PromptParseResult);

internal readonly record struct I6C6CaptureFailureDiagnosticsV1(
    I6C6CaptureFailureStageV1 Stage,
    ulong? FailureOrdinal,
    GameplayMessageKindV1? FailureMessageKind,
    PerspectiveSafeFrameSourceErrorCodeV1? FrameSourceErrorCode,
    PerspectiveSafeSourceSectionV1? FrameSourceErrorSection,
    string? MirrorFailureSite = null,
    I6C6MirrorFailureInputDiagnosticsV1? MirrorFailureInput = null,
    I6C6UnknownGameplayMessageClassificationV1?
        UnknownGameplayMessageClassification = null)
{
    internal static I6C6CaptureFailureDiagnosticsV1 FromFrameSourceFailure(
        I6C6CaptureFailureStageV1 stage,
        ulong failureOrdinal,
        GameplayMessageKindV1 messageKind,
        PerspectiveSafeFrameSourceResultV1 result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsSuccess || result.Error is null)
        {
            throw new ArgumentException(
                "A frame-source failure diagnostic requires a failed result.",
                nameof(result));
        }

        PerspectiveSafeFrameSourceErrorV1 error = result.Error.Value;
        return new(
            stage,
            failureOrdinal,
            messageKind,
            error.Code,
            error.Section);
    }
}

internal static class I6C6CapturedGameplayMessageTraceV1
{
    internal static GameplayMessageV1? TryFindMessageAtOrdinal(
        GameplayPerspectiveV1 expectedPerspective,
        ReadOnlyMemory<byte> pendingBytes,
        IReadOnlyList<byte[]> receivedChunks,
        ulong ordinal)
    {
        ArgumentNullException.ThrowIfNull(expectedPerspective);
        ArgumentNullException.ThrowIfNull(receivedChunks);

        int totalLength = pendingBytes.Length;
        foreach (byte[] chunk in receivedChunks)
        {
            ArgumentNullException.ThrowIfNull(chunk);
            totalLength = checked(totalLength + chunk.Length);
        }

        byte[] receivedBytes = new byte[totalLength - pendingBytes.Length];
        int writeOffset = 0;
        foreach (byte[] chunk in receivedChunks)
        {
            chunk.CopyTo(receivedBytes, writeOffset);
            writeOffset += chunk.Length;
        }

        GameplayMessageV1? receivedMessage = TryFindMessage(
            expectedPerspective,
            receivedBytes,
            ordinal,
            allowLeadingNonGameplayPackets: true);
        if (receivedMessage is not null)
        {
            return receivedMessage;
        }

        byte[] bytes = new byte[pendingBytes.Length + receivedBytes.Length];
        pendingBytes.Span.CopyTo(bytes);
        receivedBytes.CopyTo(bytes, pendingBytes.Length);
        return TryFindMessage(
            expectedPerspective,
            bytes,
            ordinal,
            allowLeadingNonGameplayPackets: false);
    }

    internal static I6C6UnknownGameplayMessageClassificationV1?
        TryClassifyMessageAtOrdinal(
            GameplayPerspectiveV1 expectedPerspective,
            ReadOnlyMemory<byte> pendingBytes,
            IReadOnlyList<byte[]> receivedChunks,
            ulong ordinal)
    {
        ArgumentNullException.ThrowIfNull(expectedPerspective);
        ArgumentNullException.ThrowIfNull(receivedChunks);

        int totalLength = pendingBytes.Length;
        foreach (byte[] chunk in receivedChunks)
        {
            ArgumentNullException.ThrowIfNull(chunk);
            totalLength = checked(totalLength + chunk.Length);
        }

        byte[] receivedBytes = new byte[totalLength - pendingBytes.Length];
        int writeOffset = 0;
        foreach (byte[] chunk in receivedChunks)
        {
            chunk.CopyTo(receivedBytes, writeOffset);
            writeOffset += chunk.Length;
        }

        I6C6UnknownGameplayMessageClassificationV1? classification =
            TryClassifyMessage(
                expectedPerspective,
                receivedBytes,
                ordinal,
                allowLeadingNonGameplayPackets: true);
        if (classification is not null)
        {
            return classification;
        }

        byte[] bytes = new byte[pendingBytes.Length + receivedBytes.Length];
        pendingBytes.Span.CopyTo(bytes);
        receivedBytes.CopyTo(bytes, pendingBytes.Length);
        return TryClassifyMessage(
            expectedPerspective,
            bytes,
            ordinal,
            allowLeadingNonGameplayPackets: false);
    }

    internal static byte[]? TryFindInnerMessageAtOrdinal(
        GameplayPerspectiveV1 expectedPerspective,
        ReadOnlyMemory<byte> pendingBytes,
        IReadOnlyList<byte[]> receivedChunks,
        ulong ordinal)
    {
        ArgumentNullException.ThrowIfNull(expectedPerspective);
        ArgumentNullException.ThrowIfNull(receivedChunks);

        int totalLength = pendingBytes.Length;
        foreach (byte[] chunk in receivedChunks)
        {
            ArgumentNullException.ThrowIfNull(chunk);
            totalLength = checked(totalLength + chunk.Length);
        }

        byte[] receivedBytes = new byte[totalLength - pendingBytes.Length];
        int writeOffset = 0;
        foreach (byte[] chunk in receivedChunks)
        {
            chunk.CopyTo(receivedBytes, writeOffset);
            writeOffset += chunk.Length;
        }

        byte[]? receivedMessage = TryFindInnerMessage(
            expectedPerspective,
            receivedBytes,
            ordinal,
            allowLeadingNonGameplayPackets: true);
        if (receivedMessage is not null)
        {
            return receivedMessage;
        }

        byte[] bytes = new byte[pendingBytes.Length + receivedBytes.Length];
        pendingBytes.Span.CopyTo(bytes);
        receivedBytes.CopyTo(bytes, pendingBytes.Length);
        return TryFindInnerMessage(
            expectedPerspective,
            bytes,
            ordinal,
            allowLeadingNonGameplayPackets: false);
    }

    private static GameplayMessageV1? TryFindMessage(
        GameplayPerspectiveV1 expectedPerspective,
        byte[] bytes,
        ulong ordinal,
        bool allowLeadingNonGameplayPackets)
    {
        GameplayMessageDecoderV1 decoder = new();
        ulong currentOrdinal = 0;
        int readOffset = 0;
        bool gameplayStarted = false;
        while (readOffset < bytes.Length)
        {
            FrameReadResult<ValidatedStocPacket> parsed =
                PacketPayloadValidator.TryReadValidatedStoc(
                    bytes.AsSpan(readOffset));
            if (parsed.Status != FrameReadStatus.Success ||
                parsed.Frame is null ||
                parsed.ConsumedBytes <= 0)
            {
                return null;
            }

            if (parsed.Frame.Type != StocPacketType.GameMsg)
            {
                if (gameplayStarted &&
                    parsed.Frame.Type == StocPacketType.TimeLimit)
                {
                    readOffset = checked(readOffset + parsed.ConsumedBytes);
                    continue;
                }

                if (!allowLeadingNonGameplayPackets || gameplayStarted)
                {
                    return null;
                }

                readOffset = checked(readOffset + parsed.ConsumedBytes);
                continue;
            }

            if (parsed.Frame.Payload is not StocGameMessagePayload gameMessage)
            {
                return null;
            }

            if (gameMessage.Bytes.Span.Length == 11 &&
                gameMessage.Bytes.Span[0] == 2)
            {
                if (!IsValidMsgHint(gameMessage.Bytes.Span))
                {
                    return null;
                }

                gameplayStarted = true;
                if (currentOrdinal == ulong.MaxValue)
                {
                    return null;
                }

                currentOrdinal++;
                readOffset = checked(readOffset + parsed.ConsumedBytes);
                continue;
            }

            GameplayMessageDecodeResult decoded = decoder.Decode(gameMessage);
            if (!decoded.IsSuccess ||
                decoded.Message is null ||
                (decoded.Perspective is not null &&
                 decoded.Perspective.PlayerType != expectedPerspective.PlayerType))
            {
                return null;
            }

            gameplayStarted = true;

            if (currentOrdinal == ordinal)
            {
                return decoded.Message;
            }

            if (currentOrdinal == ulong.MaxValue)
            {
                return null;
            }

            currentOrdinal++;
            readOffset = checked(readOffset + parsed.ConsumedBytes);
        }

        return null;
    }

    private static byte[]? TryFindInnerMessage(
        GameplayPerspectiveV1 expectedPerspective,
        byte[] bytes,
        ulong ordinal,
        bool allowLeadingNonGameplayPackets)
    {
        GameplayMessageDecoderV1 decoder = new();
        ulong currentOrdinal = 0;
        int readOffset = 0;
        bool gameplayStarted = false;
        while (readOffset < bytes.Length)
        {
            FrameReadResult<ValidatedStocPacket> parsed =
                PacketPayloadValidator.TryReadValidatedStoc(
                    bytes.AsSpan(readOffset));
            if (parsed.Status != FrameReadStatus.Success ||
                parsed.Frame is null ||
                parsed.ConsumedBytes <= 0)
            {
                return null;
            }

            if (parsed.Frame.Type != StocPacketType.GameMsg)
            {
                if (gameplayStarted &&
                    parsed.Frame.Type == StocPacketType.TimeLimit)
                {
                    readOffset = checked(readOffset + parsed.ConsumedBytes);
                    continue;
                }

                if (!allowLeadingNonGameplayPackets || gameplayStarted)
                {
                    return null;
                }

                readOffset = checked(readOffset + parsed.ConsumedBytes);
                continue;
            }

            if (parsed.Frame.Payload is not StocGameMessagePayload gameMessage ||
                gameMessage.Bytes.IsEmpty)
            {
                return null;
            }

            ReadOnlySpan<byte> innerBytes = gameMessage.Bytes.Span;
            if (currentOrdinal == ordinal)
            {
                return innerBytes.ToArray();
            }

            if (innerBytes[0] == 2)
            {
                if (!IsValidMsgHint(innerBytes))
                {
                    return null;
                }
            }
            else
            {
                GameplayMessageDecodeResult decoded = decoder.Decode(gameMessage);
                if (!decoded.IsSuccess ||
                    decoded.Message is null ||
                    (decoded.Perspective is not null &&
                     decoded.Perspective.PlayerType !=
                         expectedPerspective.PlayerType))
                {
                    return null;
                }
            }

            gameplayStarted = true;
            if (currentOrdinal == ulong.MaxValue)
            {
                return null;
            }

            currentOrdinal++;
            readOffset = checked(readOffset + parsed.ConsumedBytes);
        }

        return null;
    }

    private static bool IsValidMsgHint(ReadOnlySpan<byte> bytes) =>
        bytes.Length == 11 && bytes[2] <= 1;

    private static I6C6UnknownGameplayMessageClassificationV1?
        TryClassifyMessage(
            GameplayPerspectiveV1 expectedPerspective,
            byte[] bytes,
            ulong ordinal,
            bool allowLeadingNonGameplayPackets)
    {
        GameplayMessageDecoderV1 decoder = new();
        ulong currentOrdinal = 0;
        int readOffset = 0;
        bool gameplayStarted = false;
        while (readOffset < bytes.Length)
        {
            FrameReadResult<ValidatedStocPacket> parsed =
                PacketPayloadValidator.TryReadValidatedStoc(
                    bytes.AsSpan(readOffset));
            if (parsed.Status != FrameReadStatus.Success ||
                parsed.Frame is null ||
                parsed.ConsumedBytes <= 0)
            {
                return null;
            }

            if (parsed.Frame.Type != StocPacketType.GameMsg)
            {
                if (gameplayStarted &&
                    parsed.Frame.Type == StocPacketType.TimeLimit)
                {
                    readOffset = checked(readOffset + parsed.ConsumedBytes);
                    continue;
                }

                if (!allowLeadingNonGameplayPackets || gameplayStarted)
                {
                    return null;
                }

                readOffset = checked(readOffset + parsed.ConsumedBytes);
                continue;
            }

            if (parsed.Frame.Payload is not StocGameMessagePayload gameMessage)
            {
                return null;
            }

            ReadOnlySpan<byte> innerBytes = gameMessage.Bytes.Span;
            if (innerBytes.IsEmpty)
            {
                return null;
            }

            GameplayMessageDecodeResult decoded = decoder.Decode(gameMessage);
            if (currentOrdinal == ordinal)
            {
                if (decoded.IsSuccess && decoded.Message is not null)
                {
                    if (decoded.Perspective is not null &&
                        decoded.Perspective.PlayerType !=
                            expectedPerspective.PlayerType)
                    {
                        return null;
                    }

                    return new(
                        innerBytes[0],
                        I6C6GameplayMessageClassV1.KnownNonPromptGameplayMessage,
                        null,
                        null,
                        I6C6PromptParseResultV1.NotApplicable);
                }

                return ClassifyPrompt(innerBytes);
            }

            if (innerBytes[0] == 2)
            {
                if (!IsValidMsgHint(innerBytes))
                {
                    return null;
                }

                gameplayStarted = true;
                if (currentOrdinal == ulong.MaxValue)
                {
                    return null;
                }

                currentOrdinal++;
                readOffset = checked(readOffset + parsed.ConsumedBytes);
                continue;
            }

            if (!decoded.IsSuccess || decoded.Message is null ||
                (decoded.Perspective is not null &&
                 decoded.Perspective.PlayerType !=
                     expectedPerspective.PlayerType))
            {
                return null;
            }

            gameplayStarted = true;
            if (currentOrdinal == ulong.MaxValue)
            {
                return null;
            }

            currentOrdinal++;
            readOffset = checked(readOffset + parsed.ConsumedBytes);
        }

        return null;
    }

    private static I6C6UnknownGameplayMessageClassificationV1
        ClassifyPrompt(ReadOnlySpan<byte> bytes)
    {
        byte innerMessageId = bytes[0];
        if (FlatPromptProjectionV1.TryProject(
                bytes,
                out FlatPromptProjectionDraftV1? projected,
                out _) &&
            projected is not null)
        {
            return new(
                innerMessageId,
                I6C6GameplayMessageClassV1.ExistingI4Prompt,
                projected.Context.PromptFamily,
                projected.Context.ActingPlayer,
                I6C6PromptParseResultV1.Pass);
        }

        if (FlatPromptProjectionV1.TryParseWireDraft(
                bytes,
                out FlatPromptWireDraftV1? i4Draft,
                out _) &&
            i4Draft is not null)
        {
            return new(
                innerMessageId,
                I6C6GameplayMessageClassV1.ExistingI4Prompt,
                i4Draft.Family,
                TryGetActingPlayer(i4Draft, out byte i4Player)
                    ? i4Player
                    : null,
                I6C6PromptParseResultV1.Pass);
        }

        if (FlatPromptProjectionV1.TryParseI5WireDraft(
                bytes,
                out FlatPromptWireDraftV1? i5Draft,
                out _) &&
            i5Draft is not null)
        {
            return new(
                innerMessageId,
                I6C6GameplayMessageClassV1.ExistingI5Prompt,
                i5Draft.Family,
                TryGetActingPlayer(i5Draft, out byte i5Player)
                    ? i5Player
                    : null,
                I6C6PromptParseResultV1.Pass);
        }

        return new(
            innerMessageId,
            I6C6GameplayMessageClassV1.Unknown,
            null,
            null,
            I6C6PromptParseResultV1.Fail);
    }

    private static bool TryGetActingPlayer(
        FlatPromptWireDraftV1 draft,
        out byte actingPlayer)
    {
        switch (draft)
        {
            case FlatPromptSelectCardWireDraftV1 value:
                actingPlayer = value.ActingPlayer;
                return true;
            case FlatPromptSelectTributeWireDraftV1 value:
                actingPlayer = value.ActingPlayer;
                return true;
            case FlatPromptSelectUnselectWireDraftV1 value:
                actingPlayer = value.ActingPlayer;
                return true;
            case FlatPromptAnnounceNumberWireDraftV1 value:
                actingPlayer = value.ActingPlayer;
                return true;
            case FlatPromptPlaceWireDraftV1 value:
                actingPlayer = value.ActingPlayer;
                return true;
            case FlatPromptRaceWireDraftV1 value:
                actingPlayer = value.ActingPlayer;
                return true;
            case FlatPromptAttributeWireDraftV1 value:
                actingPlayer = value.ActingPlayer;
                return true;
            case FlatPromptSelectCounterWireDraftV1 value:
                actingPlayer = value.ActingPlayer;
                return true;
            case FlatPromptSortWireDraftV1 value:
                actingPlayer = value.ActingPlayer;
                return true;
            case FlatPromptEffectYnWireDraftV1 value:
                actingPlayer = value.ActingPlayer;
                return true;
            case FlatPromptChainWireDraftV1 value:
                actingPlayer = value.ActingPlayer;
                return true;
            case FlatPromptBattleWireDraftV1 value:
                actingPlayer = value.ActingPlayer;
                return true;
            case FlatPromptIdleWireDraftV1 value:
                actingPlayer = value.ActingPlayer;
                return true;
            default:
                actingPlayer = 0;
                return false;
        }
    }
}

internal enum I6GCorrelationFailureClassV1 : byte
{
    NormalizationFailure = 0,
    MirrorAddressNotUnique = 1,
    MainDeckUnsupported = 2,
    PileCardCodeUnproven = 3,
    PublicReferenceNotUnique = 4,
    PublicReferenceMissing = 5,
    OtherUnproven = 6
}

internal enum I6GMirrorCardCodeStateV1 : byte
{
    KnownProven = 0,
    UnknownRedacted = 1,
    Unproven = 2,
    NotApplicable = 3
}

internal readonly record struct I6GRealI4CorrelationFailureV1(
    FlatPromptSourceSectionV1 Section,
    int SectionLocalOrdinal,
    MirrorZoneV1? FailureZone,
    I6GCorrelationFailureClassV1 FailureClass,
    int MirrorAddressMatchCount,
    int? PublicReferenceMatchCount,
    I6GMirrorCardCodeStateV1 MirrorCardCodeState,
    bool SourceCardCodePresent,
    bool? PublicSafeCardCodeMatch);

internal readonly record struct I6GRealI4PromptReferenceDiagnosticsV1(
    FlatPromptFamilyV1 PromptFamily,
    byte ActingPlayer,
    int SummonCount,
    int SpecialSummonCount,
    int RepositionCount,
    int MsetCount,
    int SsetCount,
    int ActivateCount,
    I6GRealI4CorrelationFailureV1? FirstCorrelationFailure);

internal readonly record struct I6GRealI4PromptBoundaryEvidenceV1(
    bool IsSuccess,
    FlatPromptErrorCodeV1 Error,
    byte PromptId,
    FlatPromptFamilyV1? PromptFamily,
    byte? ActingPlayer,
    bool ActingPlayerMatchesPerspective,
    bool PublicStateProjectionPassed,
    bool PromptProjectionPassed,
    int LegalCandidateCount,
    IReadOnlyList<FlatPromptChoiceKindV1> ChoiceKinds,
    bool CompleteDomain,
    bool AllCandidatesResponseBound,
    bool ToBattlePhasePresent,
    bool ToEndPhasePresent,
    bool ShuffleHandPresent,
    I6GRealI4PromptReferenceDiagnosticsV1? ReferenceDiagnostics);

internal static class I6GRealI4PromptReferenceDiagnosticExtractorV1
{
    internal static I6GRealI4PromptReferenceDiagnosticsV1 Create(
        FlatPromptIdleWireDraftV1 prompt,
        MirrorSnapshotV1 capturedMirror,
        PublicStateSnapshotV1 acceptedSnapshot)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(capturedMirror);
        ArgumentNullException.ThrowIfNull(acceptedSnapshot);

        I6GRealI4CorrelationFailureV1? firstFailure = null;
        for (int ordinal = 0; ordinal < prompt.SummonEntries.Count; ordinal++)
        {
            FlatPromptIdleCardWireEntryV1 entry = prompt.SummonEntries[ordinal];
            if (!TryCorrelate(
                    FlatPromptSourceSectionV1.Summon,
                    ordinal,
                    entry.SourceCardCode,
                    new ModernLocInfoV1(
                        entry.Controller,
                        entry.Location,
                        entry.Sequence,
                        0),
                    capturedMirror,
                    acceptedSnapshot,
                    ref firstFailure))
            {
                return CreateResult(prompt, firstFailure);
            }
        }

        for (int ordinal = 0;
             ordinal < prompt.SpecialSummonEntries.Count;
             ordinal++)
        {
            FlatPromptIdleCardWireEntryV1 entry =
                prompt.SpecialSummonEntries[ordinal];
            if (!TryCorrelate(
                    FlatPromptSourceSectionV1.SpecialSummon,
                    ordinal,
                    entry.SourceCardCode,
                    new ModernLocInfoV1(
                        entry.Controller,
                        entry.Location,
                        entry.Sequence,
                        0),
                    capturedMirror,
                    acceptedSnapshot,
                    ref firstFailure))
            {
                return CreateResult(prompt, firstFailure);
            }
        }

        for (int ordinal = 0;
             ordinal < prompt.RepositionEntries.Count;
             ordinal++)
        {
            FlatPromptIdleRepositionWireEntryV1 entry =
                prompt.RepositionEntries[ordinal];
            if (!TryCorrelate(
                    FlatPromptSourceSectionV1.Reposition,
                    ordinal,
                    entry.SourceCardCode,
                    new ModernLocInfoV1(
                        entry.Controller,
                        entry.Location,
                        entry.Sequence,
                        0),
                    capturedMirror,
                    acceptedSnapshot,
                    ref firstFailure))
            {
                return CreateResult(prompt, firstFailure);
            }
        }

        for (int ordinal = 0; ordinal < prompt.MonsterSetEntries.Count; ordinal++)
        {
            FlatPromptIdleCardWireEntryV1 entry =
                prompt.MonsterSetEntries[ordinal];
            if (!TryCorrelate(
                    FlatPromptSourceSectionV1.Mset,
                    ordinal,
                    entry.SourceCardCode,
                    new ModernLocInfoV1(
                        entry.Controller,
                        entry.Location,
                        entry.Sequence,
                        0),
                    capturedMirror,
                    acceptedSnapshot,
                    ref firstFailure))
            {
                return CreateResult(prompt, firstFailure);
            }
        }

        for (int ordinal = 0;
             ordinal < prompt.SpellTrapSetEntries.Count;
             ordinal++)
        {
            FlatPromptIdleCardWireEntryV1 entry =
                prompt.SpellTrapSetEntries[ordinal];
            if (!TryCorrelate(
                    FlatPromptSourceSectionV1.Sset,
                    ordinal,
                    entry.SourceCardCode,
                    new ModernLocInfoV1(
                        entry.Controller,
                        entry.Location,
                        entry.Sequence,
                        0),
                    capturedMirror,
                    acceptedSnapshot,
                    ref firstFailure))
            {
                return CreateResult(prompt, firstFailure);
            }
        }

        for (int ordinal = 0; ordinal < prompt.ActivatableEntries.Count; ordinal++)
        {
            FlatPromptIdleActivatableWireEntryV1 entry =
                prompt.ActivatableEntries[ordinal];
            if (!TryCorrelate(
                    FlatPromptSourceSectionV1.Activate,
                    ordinal,
                    entry.SourceCardCode,
                    new ModernLocInfoV1(
                        entry.Controller,
                        entry.Location,
                        entry.Sequence,
                        0),
                    capturedMirror,
                    acceptedSnapshot,
                    ref firstFailure))
            {
                return CreateResult(prompt, firstFailure);
            }
        }

        return CreateResult(prompt, null);
    }

    private static I6GRealI4PromptReferenceDiagnosticsV1 CreateResult(
        FlatPromptIdleWireDraftV1 prompt,
        I6GRealI4CorrelationFailureV1? firstFailure) =>
        new(
            FlatPromptFamilyV1.MsgSelectIdleCmd,
            prompt.ActingPlayer,
            prompt.SummonEntries.Count,
            prompt.SpecialSummonEntries.Count,
            prompt.RepositionEntries.Count,
            prompt.MonsterSetEntries.Count,
            prompt.SpellTrapSetEntries.Count,
            prompt.ActivatableEntries.Count,
            firstFailure);

    private static bool TryCorrelate(
        FlatPromptSourceSectionV1 section,
        int sectionLocalOrdinal,
        uint sourceCardCode,
        ModernLocInfoV1 sourceLocation,
        MirrorSnapshotV1 capturedMirror,
        PublicStateSnapshotV1 acceptedSnapshot,
        ref I6GRealI4CorrelationFailureV1? firstFailure)
    {
        if (FlatPromptCardCorrelationV1.TryCorrelate(
                capturedMirror,
                acceptedSnapshot,
                sourceCardCode,
                sourceLocation,
                out _,
                out FlatPromptErrorCodeV1 correlationError))
        {
            return true;
        }

        firstFailure ??= DiagnoseFailure(
            section,
            sectionLocalOrdinal,
            sourceCardCode,
            sourceLocation,
            capturedMirror,
            acceptedSnapshot,
            correlationError);
        return false;
    }

    private static I6GRealI4CorrelationFailureV1 DiagnoseFailure(
        FlatPromptSourceSectionV1 section,
        int sectionLocalOrdinal,
        uint sourceCardCode,
        ModernLocInfoV1 sourceLocation,
        MirrorSnapshotV1 capturedMirror,
        PublicStateSnapshotV1 acceptedSnapshot,
        FlatPromptErrorCodeV1 correlationError)
    {
        bool sourceCardCodePresent = sourceCardCode != 0;
        if (!MirrorAddressNormalizationV1.TryNormalize(
                sourceLocation,
                out MirrorAddressNormalizationV1 normalized,
                out _))
        {
            return new(
                section,
                sectionLocalOrdinal,
                null,
                I6GCorrelationFailureClassV1.NormalizationFailure,
                0,
                null,
                I6GMirrorCardCodeStateV1.NotApplicable,
                sourceCardCodePresent,
                null);
        }

        MirrorCardSnapshotV1[] mirrorMatches = capturedMirror.Cards
            .Where(card =>
                card.Zone == normalized.Zone &&
                card.Sequence == normalized.Sequence &&
                card.IsOverlay == normalized.IsOverlay &&
                (!normalized.IsOverlay ||
                 card.OverlayIndex == normalized.OverlayIndex) &&
                PublicSemanticLocatorV1.TryGetAbsolutePlayer(
                    capturedMirror.Perspective,
                    card.Controller,
                    out byte absolutePlayer) &&
                absolutePlayer == normalized.Controller)
            .ToArray();
        if (mirrorMatches.Length != 1)
        {
            return new(
                section,
                sectionLocalOrdinal,
                normalized.Zone,
                I6GCorrelationFailureClassV1.MirrorAddressNotUnique,
                mirrorMatches.Length,
                null,
                I6GMirrorCardCodeStateV1.NotApplicable,
                sourceCardCodePresent,
                null);
        }

        MirrorCardSnapshotV1 resolvedCard = mirrorMatches[0];
        I6GMirrorCardCodeStateV1 cardCodeState =
            GetCardCodeState(resolvedCard.CardCode);
        if (normalized.Zone == MirrorZoneV1.MainDeck)
        {
            return new(
                section,
                sectionLocalOrdinal,
                normalized.Zone,
                I6GCorrelationFailureClassV1.MainDeckUnsupported,
                1,
                null,
                cardCodeState,
                sourceCardCodePresent,
                null);
        }

        if (normalized.Zone is MirrorZoneV1.Hand or MirrorZoneV1.ExtraDeck)
        {
            if (cardCodeState != I6GMirrorCardCodeStateV1.KnownProven)
            {
                return new(
                    section,
                    sectionLocalOrdinal,
                    normalized.Zone,
                    I6GCorrelationFailureClassV1.PileCardCodeUnproven,
                    1,
                    null,
                    cardCodeState,
                    sourceCardCodePresent,
                    null);
            }

            PublicSemanticZoneV1 publicZone = normalized.Zone == MirrorZoneV1.Hand
                ? PublicSemanticZoneV1.Hand
                : PublicSemanticZoneV1.ExtraDeck;
            PublicCardStateV1[] publicMatches = acceptedSnapshot.Cards
                .Where(card =>
                    card.AbsolutePlayer == normalized.Controller &&
                    card.Zone == publicZone &&
                    card.CardCode.HasValue &&
                    card.CardCode.Value == resolvedCard.CardCode.Value)
                .ToArray();
            return CreatePublicReferenceFailure(
                section,
                sectionLocalOrdinal,
                normalized.Zone,
                sourceCardCode,
                publicMatches,
                cardCodeState,
                sourceCardCodePresent);
        }

        if (normalized.IsOverlay)
        {
            if (!PublicSemanticLocatorV1.TryCreateOverlay(
                    normalized.Controller,
                    resolvedCard.Sequence,
                    resolvedCard.OverlayIndex,
                    out PublicSemanticLocatorV1? expectedLocator) ||
                expectedLocator is null)
            {
                return OtherFailure(
                    section,
                    sectionLocalOrdinal,
                    normalized.Zone,
                    1,
                    null,
                    cardCodeState,
                    sourceCardCodePresent);
            }

            PublicCardStateV1[] publicMatches = acceptedSnapshot.Cards
                .Where(card =>
                    card.AbsolutePlayer == normalized.Controller &&
                    card.Zone == PublicSemanticZoneV1.Overlay &&
                    card.Locator == expectedLocator)
                .ToArray();
            return CreatePublicReferenceFailure(
                section,
                sectionLocalOrdinal,
                normalized.Zone,
                sourceCardCode,
                publicMatches,
                cardCodeState,
                sourceCardCodePresent);
        }

        PublicCardStateV1[] indexedMatches = acceptedSnapshot.Cards
            .Where(card =>
                card.AbsolutePlayer == normalized.Controller &&
                IsIndexedZoneCompatible(normalized.Zone, card.Zone) &&
                PublicSemanticLocatorV1.TryCreateIndexed(
                    normalized.Controller,
                    card.Zone,
                    resolvedCard.Sequence,
                    out PublicSemanticLocatorV1? expectedLocator) &&
                expectedLocator is not null &&
                card.Locator == expectedLocator)
            .ToArray();
        if (correlationError != FlatPromptErrorCodeV1.UnprovenPublicReference)
        {
            return OtherFailure(
                section,
                sectionLocalOrdinal,
                normalized.Zone,
                1,
                indexedMatches,
                cardCodeState,
                sourceCardCodePresent);
        }

        return CreatePublicReferenceFailure(
            section,
            sectionLocalOrdinal,
            normalized.Zone,
            sourceCardCode,
            indexedMatches,
            cardCodeState,
            sourceCardCodePresent);
    }

    private static I6GRealI4CorrelationFailureV1 CreatePublicReferenceFailure(
        FlatPromptSourceSectionV1 section,
        int sectionLocalOrdinal,
        MirrorZoneV1 zone,
        uint sourceCardCode,
        IReadOnlyList<PublicCardStateV1> publicMatches,
        I6GMirrorCardCodeStateV1 cardCodeState,
        bool sourceCardCodePresent)
    {
        I6GCorrelationFailureClassV1 failureClass = publicMatches.Count switch
        {
            0 => I6GCorrelationFailureClassV1.PublicReferenceMissing,
            1 => I6GCorrelationFailureClassV1.OtherUnproven,
            _ => I6GCorrelationFailureClassV1.PublicReferenceNotUnique
        };
        bool? safeCardCodeMatch = publicMatches.Count == 1 &&
                                  sourceCardCodePresent
            ? publicMatches[0].CardCode is uint publicCardCode &&
              publicCardCode == sourceCardCode
            : null;
        return new(
            section,
            sectionLocalOrdinal,
            zone,
            failureClass,
            1,
            publicMatches.Count,
            cardCodeState,
            sourceCardCodePresent,
            safeCardCodeMatch);
    }

    private static I6GRealI4CorrelationFailureV1 OtherFailure(
        FlatPromptSourceSectionV1 section,
        int sectionLocalOrdinal,
        MirrorZoneV1 zone,
        int mirrorAddressMatchCount,
        IReadOnlyList<PublicCardStateV1>? publicMatches,
        I6GMirrorCardCodeStateV1 cardCodeState,
        bool sourceCardCodePresent) =>
        new(
            section,
            sectionLocalOrdinal,
            zone,
            I6GCorrelationFailureClassV1.OtherUnproven,
            mirrorAddressMatchCount,
            publicMatches?.Count,
            cardCodeState,
            sourceCardCodePresent,
            null);

    private static I6GMirrorCardCodeStateV1 GetCardCodeState(
        MirrorValueV1<uint> value)
    {
        if (!value.IsKnown)
        {
            return value.Provenance == MirrorProvenanceV1.UnknownRedacted
                ? I6GMirrorCardCodeStateV1.UnknownRedacted
                : I6GMirrorCardCodeStateV1.Unproven;
        }

        return value.Value != 0 &&
               value.Provenance is
                   MirrorProvenanceV1.PublicProtocolFact or
                   MirrorProvenanceV1.PerspectivePrivateFact or
                   MirrorProvenanceV1.DerivedFromProvenPublicFacts
            ? I6GMirrorCardCodeStateV1.KnownProven
            : I6GMirrorCardCodeStateV1.Unproven;
    }

    private static bool IsIndexedZoneCompatible(
        MirrorZoneV1 mirrorZone,
        PublicSemanticZoneV1 publicZone) =>
        (mirrorZone, publicZone) switch
        {
            (MirrorZoneV1.MonsterZone, PublicSemanticZoneV1.MonsterZone) => true,
            (MirrorZoneV1.Graveyard, PublicSemanticZoneV1.Graveyard) => true,
            (MirrorZoneV1.Banished, PublicSemanticZoneV1.Banished) => true,
            (MirrorZoneV1.SpellTrapZone,
                PublicSemanticZoneV1.SpellTrapZone) => true,
            (MirrorZoneV1.SpellTrapZone,
                PublicSemanticZoneV1.FieldZone) => true,
            (MirrorZoneV1.SpellTrapZone,
                PublicSemanticZoneV1.PendulumRelevantState) => true,
            _ => false
        };
}

internal static class I6GRealI4PromptBoundaryV1
{
    internal static I6GRealI4PromptBoundaryEvidenceV1 TryEvaluate(
        GameplayPerspectiveV1 expectedPerspective,
        ReadOnlyMemory<byte> pendingBytes,
        IReadOnlyList<byte[]> receivedChunks,
        ulong ordinal,
        PerspectiveStateMirrorV1 mirror,
        ulong duelFlags)
    {
        ArgumentNullException.ThrowIfNull(expectedPerspective);
        ArgumentNullException.ThrowIfNull(receivedChunks);
        ArgumentNullException.ThrowIfNull(mirror);

        byte[]? promptBytes = I6C6CapturedGameplayMessageTraceV1
            .TryFindInnerMessageAtOrdinal(
                expectedPerspective,
                pendingBytes,
                receivedChunks,
                ordinal);
        if (promptBytes is null || promptBytes.Length == 0)
        {
            return Failure(0, FlatPromptErrorCodeV1.MalformedPrompt);
        }

        byte promptId = promptBytes[0];
        if (promptId != (byte)FlatPromptFamilyV1.MsgSelectIdleCmd)
        {
            return Failure(
                promptId,
                FlatPromptErrorCodeV1.UnsupportedPromptLayout);
        }

        PublicStateProjectionResultV1 publicProjection =
            PublicStateProjectionV1.TryProject(
                mirror.Snapshot,
                new PublicStateProjectionContextV1(duelFlags));
        if (!publicProjection.IsSuccess || publicProjection.Snapshot is null)
        {
            return Failure(
                promptId,
                FlatPromptErrorCodeV1.UnprovenPublicReference);
        }

        if (!FlatPromptProjectionV1.TryParseWireDraft(
                promptBytes,
                out FlatPromptWireDraftV1? wireDraft,
                out FlatPromptErrorCodeV1 wireError) ||
            wireDraft is not FlatPromptIdleWireDraftV1 idleDraft)
        {
            return Failure(
                promptId,
                wireError,
                publicStateProjectionPassed: true);
        }

        I6GRealI4PromptReferenceDiagnosticsV1 referenceDiagnostics =
            I6GRealI4PromptReferenceDiagnosticExtractorV1.Create(
                idleDraft,
                mirror.Snapshot,
                publicProjection.Snapshot);

        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 prompt = session.TryAcceptPrompt(
            promptBytes,
            mirror,
            publicProjection);
        if (!prompt.IsSuccess ||
            prompt.Context is null ||
            prompt.Candidates is null)
        {
            return Failure(
                promptId,
                prompt.Error,
                publicStateProjectionPassed: true,
                referenceDiagnostics: referenceDiagnostics);
        }

        FlatPromptPublicContextV1 context = prompt.Context;
        FlatPromptChoiceKindV1[] choiceKinds = prompt.Candidates
            .Select(candidate => candidate.ChoiceKind)
            .ToArray();
        bool actingPlayerMatchesPerspective =
            context.ActingPlayer == expectedPerspective.PlayerType;
        if (!actingPlayerMatchesPerspective)
        {
            return new(
                false,
                FlatPromptErrorCodeV1.InvalidParticipant,
                promptId,
                context.PromptFamily,
                context.ActingPlayer,
                false,
                true,
                true,
                prompt.Candidates.Count,
                choiceKinds,
                true,
                false,
                choiceKinds.Contains(FlatPromptChoiceKindV1.ToBp),
                choiceKinds.Contains(FlatPromptChoiceKindV1.ToEp),
                choiceKinds.Contains(FlatPromptChoiceKindV1.ShuffleHand),
                referenceDiagnostics);
        }

        bool allCandidatesResponseBound = true;
        foreach (FlatPublicCandidateDescriptorV1 candidate in prompt.Candidates)
        {
            if (!session.TryCaptureSelection(
                    candidate.I4LocalCandidateKey,
                    out FlatPromptSelectionHandleV1? handle,
                    out _) ||
                !session.TryResolveSelection(
                    handle,
                    out _,
                    out _))
            {
                allCandidatesResponseBound = false;
                break;
            }
        }

        return new(
            allCandidatesResponseBound,
            allCandidatesResponseBound
                ? FlatPromptErrorCodeV1.None
                : FlatPromptErrorCodeV1.InvalidResponseBinding,
            promptId,
            context.PromptFamily,
            context.ActingPlayer,
            true,
            true,
            true,
            prompt.Candidates.Count,
            choiceKinds,
            true,
            allCandidatesResponseBound,
            choiceKinds.Contains(FlatPromptChoiceKindV1.ToBp),
            choiceKinds.Contains(FlatPromptChoiceKindV1.ToEp),
            choiceKinds.Contains(FlatPromptChoiceKindV1.ShuffleHand),
            referenceDiagnostics);
    }

    private static I6GRealI4PromptBoundaryEvidenceV1 Failure(
        byte promptId,
        FlatPromptErrorCodeV1 error,
        bool publicStateProjectionPassed = false,
        I6GRealI4PromptReferenceDiagnosticsV1? referenceDiagnostics = null) =>
        new(
            false,
            error,
            promptId,
            null,
            null,
            false,
            publicStateProjectionPassed,
            false,
            0,
            Array.Empty<FlatPromptChoiceKindV1>(),
            false,
            false,
            false,
            false,
            false,
            referenceDiagnostics);
}

internal readonly record struct I6C6MirrorFailureInputDiagnosticsV1(
    byte Player,
    MirrorZoneV1 Location,
    int QueryCount,
    int QueryIndex,
    bool QueryIsOnFieldSkipped,
    uint? PreZoneCount,
    int PreRepresentedEntityCount);

internal static class I6C6MirrorFailureSiteV1
{
    internal static I6C6MirrorFailureClassificationV1? TryClassify(
        GameplayErrorCode error,
        GameplayMessageV1 message,
        MirrorSnapshotV1 snapshot)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (error != GameplayErrorCode.UnknownMirrorReference ||
            message.Kind is not GameplayMessageKindV1.Draw and
                not GameplayMessageKindV1.UpdateData)
        {
            return null;
        }

        if (message.Kind == GameplayMessageKindV1.UpdateData)
        {
            return ClassifyUpdateDataFailure(message, snapshot);
        }

        if (message.Draw is null || message.Draw.Player > 1)
        {
            return null;
        }

        MirrorParticipantRoleV1 role =
            message.Draw.Player == snapshot.Perspective.PlayerType
                ? MirrorParticipantRoleV1.Self
                : MirrorParticipantRoleV1.Opponent;
        bool representedMainDeckEntity = snapshot
            .GetZone(role, MirrorZoneV1.MainDeck)
            .Cards
            .Count > 0;
        return representedMainDeckEntity
            ? new I6C6MirrorFailureClassificationV1(
                "ApplyDraw/hidden-main-deck-continuity-guard",
                null)
            : null;
    }

    private static I6C6MirrorFailureClassificationV1? ClassifyUpdateDataFailure(
        GameplayMessageV1 message,
        MirrorSnapshotV1 snapshot)
    {
        GameplayUpdateDataPayloadV1? payload = message.UpdateData;
        if (payload is null)
        {
            return null;
        }

        byte baseLocation = (byte)(payload.Location & 0x7f);
        if (baseLocation == 0x40)
        {
            MirrorParticipantRoleV1 role =
                payload.Player == snapshot.Perspective.PlayerType
                    ? MirrorParticipantRoleV1.Self
                    : MirrorParticipantRoleV1.Opponent;
            MirrorZoneSnapshotV1 extraDeck = snapshot.GetZone(
                role,
                MirrorZoneV1.ExtraDeck);
            uint? preZoneCount = extraDeck.Count.IsKnown
                ? extraDeck.Count.Value
                : null;

            if (payload.Queries.Any(query => query.IsOnFieldSkipped))
            {
                int index = payload.Queries
                    .Select((query, queryIndex) => (query, queryIndex))
                    .First(value => value.query.IsOnFieldSkipped)
                    .queryIndex;
                return new I6C6MirrorFailureClassificationV1(
                    "ApplyUpdateData/extra-bootstrap-skipped-query",
                    new(
                        payload.Player,
                        MirrorZoneV1.ExtraDeck,
                        payload.Queries.Count,
                        index,
                        true,
                        preZoneCount,
                        extraDeck.Cards.Count));
            }

            if (payload.Queries.Any(query => !query.Fields.Any(
                    field => field.Flag == QueryFlagV1.Position)))
            {
                int index = payload.Queries
                    .Select((query, queryIndex) => (query, queryIndex))
                    .First(value => !value.query.Fields.Any(
                        field => field.Flag == QueryFlagV1.Position))
                    .queryIndex;
                return new I6C6MirrorFailureClassificationV1(
                    "ApplyUpdateData/extra-bootstrap-missing-position",
                    new(
                        payload.Player,
                        MirrorZoneV1.ExtraDeck,
                        payload.Queries.Count,
                        index,
                        false,
                        preZoneCount,
                        extraDeck.Cards.Count));
            }

            if (payload.Player == snapshot.Perspective.PlayerType &&
                extraDeck.Cards.Any(
                    card => !card.CardCode.IsKnown ||
                            !card.Position.IsKnown))
            {
                int index = payload.Queries
                    .Select((query, queryIndex) => (query, queryIndex))
                    .FirstOrDefault(value =>
                        !value.query.Fields.Any(
                            field => field.Flag == QueryFlagV1.Position) ||
                        !value.query.Fields.Any(
                            field => field.Flag == QueryFlagV1.Code))
                    .queryIndex;
                return new I6C6MirrorFailureClassificationV1(
                    "ApplyUpdateData/extra-self-card-or-position-unproven",
                    new(
                        payload.Player,
                        MirrorZoneV1.ExtraDeck,
                        payload.Queries.Count,
                        index,
                        false,
                        preZoneCount,
                        extraDeck.Cards.Count));
            }

            return null;
        }

        if (!TryMapZone(baseLocation, out MirrorZoneV1 zone) ||
            payload.Player > 1)
        {
            return null;
        }

        MirrorParticipantRoleV1 participant =
            payload.Player == snapshot.Perspective.PlayerType
                ? MirrorParticipantRoleV1.Self
                : MirrorParticipantRoleV1.Opponent;
        MirrorZoneSnapshotV1 zoneSnapshot = snapshot.GetZone(participant, zone);
        int missingIndex = -1;
        for (int index = 0; index < payload.Queries.Count; index++)
        {
            if (!zoneSnapshot.Cards.Any(card =>
                    !card.IsOverlay && card.Sequence == (uint)index))
            {
                missingIndex = index;
                break;
            }
        }

        if (missingIndex < 0)
        {
            return null;
        }

        uint? preNonExtraZoneCount = zoneSnapshot.Count.IsKnown
            ? zoneSnapshot.Count.Value
            : null;
        return new I6C6MirrorFailureClassificationV1(
            "ApplyUpdateData/non-extra-entity-missing",
            new(
                payload.Player,
                zone,
                payload.Queries.Count,
                missingIndex,
                payload.Queries[missingIndex].IsOnFieldSkipped,
                preNonExtraZoneCount,
                zoneSnapshot.Cards.Count));
    }

    private static bool TryMapZone(byte location, out MirrorZoneV1 zone)
    {
        zone = (byte)(location & 0x7f) switch
        {
            0x01 => MirrorZoneV1.MainDeck,
            0x02 => MirrorZoneV1.Hand,
            0x04 => MirrorZoneV1.MonsterZone,
            0x08 => MirrorZoneV1.SpellTrapZone,
            0x10 => MirrorZoneV1.Graveyard,
            0x20 => MirrorZoneV1.Banished,
            0x40 => MirrorZoneV1.ExtraDeck,
            _ => default
        };
        return (location & 0x7f) is 0x01 or 0x02 or 0x04 or 0x08 or
            0x10 or 0x20 or 0x40;
    }
}

internal readonly record struct I6C6MirrorFailureClassificationV1(
    string Site,
    I6C6MirrorFailureInputDiagnosticsV1? Input);

internal sealed class I6C6FrameReadinessDiagnosticsV1
{
    internal I6C6FrameReadinessDiagnosticsV1(
        PerspectiveSafeFrameSourceErrorV1? initialFrameError,
        int provisionalNotReadyCount,
        ulong? firstCompleteFrameOrdinal,
        GameplayMessageKindV1? firstCompleteFrameMessageKind,
        bool? actionRequiredBeforeFrameReady,
        int messagesAppliedBeforeReady,
        IReadOnlyList<GameplayMessageKindV1> appliedMessageKinds,
        bool? visibleEventHistoryPreserved)
    {
        InitialFrameError = initialFrameError;
        ProvisionalNotReadyCount = provisionalNotReadyCount;
        FirstCompleteFrameOrdinal = firstCompleteFrameOrdinal;
        FirstCompleteFrameMessageKind = firstCompleteFrameMessageKind;
        ActionRequiredBeforeFrameReady = actionRequiredBeforeFrameReady;
        MessagesAppliedBeforeReady = messagesAppliedBeforeReady;
        AppliedMessageKinds = appliedMessageKinds.ToArray();
        VisibleEventHistoryPreserved = visibleEventHistoryPreserved;
    }

    internal PerspectiveSafeFrameSourceErrorV1? InitialFrameError { get; }

    internal int ProvisionalNotReadyCount { get; }

    internal bool FirstCompleteFrame => FirstCompleteFrameOrdinal.HasValue;

    internal ulong? FirstCompleteFrameOrdinal { get; }

    internal GameplayMessageKindV1? FirstCompleteFrameMessageKind { get; }

    internal bool? ActionRequiredBeforeFrameReady { get; }

    internal int MessagesAppliedBeforeReady { get; }

    internal IReadOnlyList<GameplayMessageKindV1> AppliedMessageKinds { get; }

    internal bool? VisibleEventHistoryPreserved { get; }
}

internal sealed class I6C6FrameReadinessTraceV1
{
    private readonly List<GameplayMessageKindV1> appliedMessageKinds = new();

    internal PerspectiveSafeFrameSourceErrorV1? InitialFrameError { get; set; }

    internal int ProvisionalNotReadyCount { get; set; }

    internal ulong? FirstCompleteFrameOrdinal { get; set; }

    internal GameplayMessageKindV1? FirstCompleteFrameMessageKind { get; set; }

    internal bool? ActionRequiredBeforeFrameReady { get; set; } = false;

    internal int MessagesAppliedBeforeReady { get; set; }

    internal bool? VisibleEventHistoryPreserved { get; set; }

    internal IReadOnlyList<GameplayMessageKindV1> AppliedMessageKinds =>
        appliedMessageKinds;

    internal void RecordMessage(GameplayMessageV1 message) =>
        appliedMessageKinds.Add(message.Kind);

    internal void MarkReady(
        ulong ordinal,
        GameplayMessageKindV1 messageKind,
        PerspectiveSafeFrameV1 frame,
        PerspectiveStateMirrorV1 mirror)
    {
        FirstCompleteFrameOrdinal = ordinal;
        FirstCompleteFrameMessageKind = messageKind;
        MessagesAppliedBeforeReady = appliedMessageKinds.Count - 1;
        VisibleEventHistoryPreserved =
            frame.VisibleEvents.SequenceEqual(mirror.VisibleEvents);
    }

    internal I6C6FrameReadinessDiagnosticsV1 Snapshot() =>
        new(
            InitialFrameError,
            ProvisionalNotReadyCount,
            FirstCompleteFrameOrdinal,
            FirstCompleteFrameMessageKind,
            ActionRequiredBeforeFrameReady,
            MessagesAppliedBeforeReady,
            appliedMessageKinds,
            VisibleEventHistoryPreserved);
}

internal sealed class I6C6LiveGameplayCaptureResultV1
{
    private I6C6LiveGameplayCaptureResultV1(
        bool isSuccess,
        GameplayErrorCode errorCode,
        I6C6ClosureHarnessBindingV1 binding,
        I6C6OpponentRuntimeBindingV1 opponentRuntimeBinding,
        IReadOnlyList<byte[]> receivedTcpChunks,
        IReadOnlyList<I6C6LiveGameplayObservationV1> observations,
        I6C6CaptureFailureDiagnosticsV1? failureDiagnostics,
        I6C6FrameReadinessDiagnosticsV1 readinessDiagnostics,
        I6GRealI4PromptBoundaryEvidenceV1? i4PromptBoundaryEvidence)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        Binding = binding;
        OpponentRuntimeBinding = opponentRuntimeBinding;
        FailureDiagnostics = failureDiagnostics;
        I4PromptBoundaryEvidence = i4PromptBoundaryEvidence;
        ReadinessDiagnostics = readinessDiagnostics ??
            throw new ArgumentNullException(nameof(readinessDiagnostics));
        ReceivedTcpChunks = receivedTcpChunks
            .Select(chunk => chunk.ToArray())
            .ToArray();
        Observations = observations.ToArray();
    }

    internal bool IsSuccess { get; }

    internal GameplayErrorCode ErrorCode { get; }

    internal I6C6ClosureHarnessBindingV1 Binding { get; }

    internal I6C6OpponentRuntimeBindingV1 OpponentRuntimeBinding { get; }

    internal I6C6CaptureFailureDiagnosticsV1? FailureDiagnostics { get; }

    internal I6GRealI4PromptBoundaryEvidenceV1?
        I4PromptBoundaryEvidence { get; }

    internal I6C6CaptureFailureStageV1 FailureStage =>
        FailureDiagnostics?.Stage ?? I6C6CaptureFailureStageV1.None;

    internal ulong? FailureOrdinal => FailureDiagnostics?.FailureOrdinal;

    internal GameplayMessageKindV1? FailureMessageKind =>
        FailureDiagnostics?.FailureMessageKind;

    internal PerspectiveSafeFrameSourceErrorCodeV1? FrameSourceErrorCode =>
        FailureDiagnostics?.FrameSourceErrorCode;

    internal PerspectiveSafeSourceSectionV1? FrameSourceErrorSection =>
        FailureDiagnostics?.FrameSourceErrorSection;

    internal I6C6FrameReadinessDiagnosticsV1 ReadinessDiagnostics { get; }

    internal IReadOnlyList<byte[]> ReceivedTcpChunks { get; }

    internal IReadOnlyList<I6C6LiveGameplayObservationV1> Observations { get; }

    internal IReadOnlyList<GameplayMessageV1> Messages => Observations
        .Select(observation => observation.Message)
        .ToArray();

    internal PerspectiveSafeFrameV1? Frame => Observations.Count == 0
        ? null
        : Observations[^1].Frame;

    private static I6C6LiveGameplayCaptureResultV1 FromCapture(
        bool isSuccess,
        GameplayErrorCode errorCode,
        I6C6ClosureHarnessBindingV1 binding,
        I6C6OpponentRuntimeBindingV1 opponentRuntimeBinding,
        IReadOnlyList<byte[]> receivedTcpChunks,
        IReadOnlyList<I6C6LiveGameplayObservationV1> observations,
        I6C6CaptureFailureDiagnosticsV1? failureDiagnostics = null,
        I6C6FrameReadinessDiagnosticsV1? readinessDiagnostics = null,
        I6GRealI4PromptBoundaryEvidenceV1? i4PromptBoundaryEvidence = null) =>
        new(
            isSuccess,
            errorCode,
            binding,
            opponentRuntimeBinding,
            receivedTcpChunks,
            observations,
            failureDiagnostics,
            readinessDiagnostics ??
                new I6C6FrameReadinessDiagnosticsV1(
                    null,
                    0,
                    null,
                    null,
                    false,
                    0,
                    Array.Empty<GameplayMessageKindV1>(),
                    null),
            i4PromptBoundaryEvidence);

    internal static async ValueTask<I6C6LiveGameplayCaptureResultV1> CaptureAsync(
        I6C6ClosureHarnessBindingV1 binding,
        I6C6OpponentRuntimeBindingV1 opponentRuntimeBinding,
        GameplayHandoffOfferV1 handoff,
        I6C6TcpCaptureTransportV1 captureTransport,
        PerspectiveSafeMatchContextV1 matchContext,
        PerspectiveSafePrintedProviderV1 printedProvider,
        int maximumAdditionalMessages,
        CancellationToken cancellationToken,
        Action<I6C6ClosureHarnessGameplayCaptureSubsiteV1>
            setGameplayCaptureSubsite)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(opponentRuntimeBinding);
        ArgumentNullException.ThrowIfNull(handoff);
        ArgumentNullException.ThrowIfNull(captureTransport);
        ArgumentNullException.ThrowIfNull(matchContext);
        ArgumentNullException.ThrowIfNull(printedProvider);
        ArgumentNullException.ThrowIfNull(setGameplayCaptureSubsite);
        if (maximumAdditionalMessages < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumAdditionalMessages));
        }

        I6C6FrameReadinessTraceV1 readiness = new();
        setGameplayCaptureSubsite(
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.HandoffClaim);
        GameplayHandoffAcquireResult acquired =
            GameplayHandoffConsumerV1.TryCreate(handoff);
        if (!acquired.IsSuccess || acquired.Consumer is null)
        {
            return Failure(
                binding,
                opponentRuntimeBinding,
                acquired.Error,
                captureTransport,
                Array.Empty<I6C6LiveGameplayObservationV1>(),
                new(
                    I6C6CaptureFailureStageV1.HandoffAcquire,
                    null,
                    null,
                    null,
                    null),
                readiness.Snapshot());
        }

        await using GameplayHandoffConsumerV1 consumer = acquired.Consumer;
        setGameplayCaptureSubsite(
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.InitialGameplayPump);
        GameplayPumpResult first = await consumer.PumpAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!first.IsSuccess ||
            first.Message is null ||
            first.Perspective is null ||
            first.Session is null)
        {
            return Failure(
                binding,
                opponentRuntimeBinding,
                first.Error,
                captureTransport,
                Array.Empty<I6C6LiveGameplayObservationV1>(),
                new(
                    I6C6CaptureFailureStageV1.InitialPump,
                    0,
                    null,
                    null,
                    null),
                readiness.Snapshot());
        }

        setGameplayCaptureSubsite(
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.MirrorConstruction);
        MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
            first.Message,
            first.Perspective);
        if (!created.IsSuccess || created.Mirror is null)
        {
            return Failure(
                binding,
                opponentRuntimeBinding,
                created.Error,
                captureTransport,
                Array.Empty<I6C6LiveGameplayObservationV1>(),
                new(
                    I6C6CaptureFailureStageV1.MirrorCreate,
                    0,
                    first.Message?.Kind,
                    null,
                    null),
                readiness.Snapshot());
        }

        byte[] initialPendingBytes = first.Session.PendingBytes.ToArray();
        setGameplayCaptureSubsite(
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.MirrorSessionConstruction);
        await using GameplayMirrorSessionV1 session =
            new(
                first.Session,
                created.Mirror,
                matchContext,
                printedProvider);
        setGameplayCaptureSubsite(
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.InitialFrameConstruction);
        PerspectiveSafeFrameSourceResultV1 initialFrame =
            session.TryCreateI6C5Frame();
        ulong wireOrdinal = 0;

        setGameplayCaptureSubsite(
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.ObservationConstruction);
        readiness.RecordMessage(first.Message);
        List<I6C6LiveGameplayObservationV1> observations = new();
        bool frameReady = initialFrame.IsSuccess && initialFrame.Frame is not null;
        if (frameReady)
        {
            readiness.MarkReady(
                0,
                first.Message.Kind,
                initialFrame.Frame!,
                created.Mirror);
            observations.Add(new(0, first.Message, initialFrame.Frame!));
        }
        else
        {
            if (!IsProvisionalFrameReadinessFailure(initialFrame))
            {
                setGameplayCaptureSubsite(
                    I6C6ClosureHarnessGameplayCaptureSubsiteV1
                        .InitialFrameFailureDiagnostics);
                return Failure(
                    binding,
                    opponentRuntimeBinding,
                    GameplayErrorCode.InvalidState,
                    captureTransport,
                    observations,
                    I6C6CaptureFailureDiagnosticsV1.FromFrameSourceFailure(
                        I6C6CaptureFailureStageV1.InitialFrame,
                        0,
                        first.Message.Kind,
                        initialFrame),
                    readiness.Snapshot());
            }

            setGameplayCaptureSubsite(
                I6C6ClosureHarnessGameplayCaptureSubsiteV1
                    .ObservationConstruction);
            readiness.InitialFrameError = initialFrame.Error;
            readiness.ProvisionalNotReadyCount = 1;
            readiness.MessagesAppliedBeforeReady =
                readiness.AppliedMessageKinds.Count;
        }

        for (int index = 0; index < maximumAdditionalMessages; index++)
        {
            int presentationMessagesBefore =
                session.PresentationMessagesConsumed;
            setGameplayCaptureSubsite(
                I6C6ClosureHarnessGameplayCaptureSubsiteV1
                    .SubsequentGameplayPump);
            GameplayMirrorPumpResult next = await session.PumpAsync(
                    cancellationToken)
                .ConfigureAwait(false);
            int presentationMessagesConsumed =
                session.PresentationMessagesConsumed -
                presentationMessagesBefore;
            if (presentationMessagesConsumed < 0)
            {
                return Failure(
                    binding,
                    opponentRuntimeBinding,
                    GameplayErrorCode.InvalidState,
                    captureTransport,
                    observations,
                    new(
                        I6C6CaptureFailureStageV1.SubsequentPump,
                        null,
                        null,
                        null,
                        null),
                    readiness.Snapshot());
            }

            ulong currentWireOrdinal = checked(
                wireOrdinal + (ulong)presentationMessagesConsumed + 1);
            if (!next.IsSuccess || next.Message is null)
            {
                if (!frameReady &&
                    next.Error is GameplayErrorCode.UnsupportedMessage or
                        GameplayErrorCode.UnknownMessageId)
                {
                    readiness.ActionRequiredBeforeFrameReady = null;
                }

                ulong failureOrdinal = currentWireOrdinal;
                GameplayMessageV1? failedMessage =
                    I6C6CapturedGameplayMessageTraceV1.TryFindMessageAtOrdinal(
                        first.Perspective,
                        initialPendingBytes,
                        captureTransport.ReceivedChunks,
                        failureOrdinal);
                I6C6MirrorFailureClassificationV1? failureClassification =
                    failedMessage is null
                    ? null
                    : I6C6MirrorFailureSiteV1.TryClassify(
                        next.Error,
                        failedMessage,
                        next.Snapshot);
                I6C6UnknownGameplayMessageClassificationV1?
                    unknownMessageClassification = failedMessage is null
                    ? I6C6CapturedGameplayMessageTraceV1
                        .TryClassifyMessageAtOrdinal(
                            first.Perspective,
                            initialPendingBytes,
                            captureTransport.ReceivedChunks,
                            failureOrdinal)
                    : null;

                return Failure(
                    binding,
                    opponentRuntimeBinding,
                    next.Error,
                    captureTransport,
                    observations,
                    new(
                        I6C6CaptureFailureStageV1.SubsequentPump,
                        failureOrdinal,
                        failedMessage?.Kind,
                        null,
                        null,
                        failureClassification?.Site,
                        failureClassification?.Input,
                        unknownMessageClassification),
                    readiness.Snapshot(),
                    next.Error == GameplayErrorCode.UnknownMessageId
                        ? I6GRealI4PromptBoundaryV1.TryEvaluate(
                            first.Perspective,
                            initialPendingBytes,
                            captureTransport.ReceivedChunks,
                            failureOrdinal,
                            created.Mirror,
                            matchContext.DuelFlags)
                        : null);
            }

            setGameplayCaptureSubsite(
                I6C6ClosureHarnessGameplayCaptureSubsiteV1
                    .ObservationConstruction);
            readiness.RecordMessage(next.Message);
            wireOrdinal = currentWireOrdinal;
            ulong ordinal = currentWireOrdinal;
            setGameplayCaptureSubsite(
                I6C6ClosureHarnessGameplayCaptureSubsiteV1
                    .SubsequentFrameConstruction);
            PerspectiveSafeFrameSourceResultV1 frame =
                session.TryCreateI6C5Frame();

            if (!frameReady)
            {
                if (frame.IsSuccess && frame.Frame is not null)
                {
                    setGameplayCaptureSubsite(
                        I6C6ClosureHarnessGameplayCaptureSubsiteV1
                            .ObservationConstruction);
                    frameReady = true;
                    readiness.MarkReady(
                        ordinal,
                        next.Message.Kind,
                        frame.Frame,
                        created.Mirror);
                    observations.Add(new(ordinal, next.Message, frame.Frame));
                    continue;
                }

                if (IsProvisionalFrameReadinessFailure(frame))
                {
                    setGameplayCaptureSubsite(
                        I6C6ClosureHarnessGameplayCaptureSubsiteV1
                            .ObservationConstruction);
                    readiness.ProvisionalNotReadyCount++;
                    readiness.MessagesAppliedBeforeReady =
                        readiness.AppliedMessageKinds.Count;
                    continue;
                }

                setGameplayCaptureSubsite(
                    I6C6ClosureHarnessGameplayCaptureSubsiteV1
                        .SubsequentFrameFailureDiagnostics);
                return Failure(
                    binding,
                    opponentRuntimeBinding,
                    GameplayErrorCode.InvalidState,
                    captureTransport,
                    observations,
                    I6C6CaptureFailureDiagnosticsV1.FromFrameSourceFailure(
                        I6C6CaptureFailureStageV1.SubsequentFrame,
                        ordinal,
                        next.Message.Kind,
                        frame),
                    readiness.Snapshot());
            }

            if (!frame.IsSuccess || frame.Frame is null)
            {
                setGameplayCaptureSubsite(
                    I6C6ClosureHarnessGameplayCaptureSubsiteV1
                        .SubsequentFrameFailureDiagnostics);
                return Failure(
                    binding,
                    opponentRuntimeBinding,
                    GameplayErrorCode.InvalidState,
                    captureTransport,
                    observations,
                    I6C6CaptureFailureDiagnosticsV1.FromFrameSourceFailure(
                        I6C6CaptureFailureStageV1.SubsequentFrame,
                        ordinal,
                        next.Message.Kind,
                        frame),
                    readiness.Snapshot());
            }

            setGameplayCaptureSubsite(
                I6C6ClosureHarnessGameplayCaptureSubsiteV1
                    .ObservationConstruction);
            observations.Add(
                new(ordinal, next.Message, frame.Frame));
        }

        if (!frameReady)
        {
            return Failure(
                binding,
                opponentRuntimeBinding,
                GameplayErrorCode.InvalidState,
                captureTransport,
                observations,
                new(
                    I6C6CaptureFailureStageV1.ReadinessLimit,
                    null,
                    null,
                    null,
                    null),
                readiness.Snapshot());
        }

        setGameplayCaptureSubsite(
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.CaptureFinalization);
        return FromCapture(
            true,
            GameplayErrorCode.None,
            binding,
            opponentRuntimeBinding,
            captureTransport.ReceivedChunks,
            observations,
            readinessDiagnostics: readiness.Snapshot());
    }

    private static bool IsProvisionalFrameReadinessFailure(
        PerspectiveSafeFrameSourceResultV1 result) =>
        !result.IsSuccess &&
        result.Error is
        {
            Code: PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
            Section: PerspectiveSafeSourceSectionV1.Entities
        };

    private static I6C6LiveGameplayCaptureResultV1 Failure(
        I6C6ClosureHarnessBindingV1 binding,
        I6C6OpponentRuntimeBindingV1 opponentRuntimeBinding,
        GameplayErrorCode error,
        I6C6TcpCaptureTransportV1 captureTransport,
        IReadOnlyList<I6C6LiveGameplayObservationV1> observations,
        I6C6CaptureFailureDiagnosticsV1? failureDiagnostics = null,
        I6C6FrameReadinessDiagnosticsV1? readinessDiagnostics = null,
        I6GRealI4PromptBoundaryEvidenceV1? i4PromptBoundaryEvidence = null) =>
        FromCapture(
            false,
            error,
            binding,
            opponentRuntimeBinding,
            captureTransport.ReceivedChunks,
            observations,
            failureDiagnostics,
            readinessDiagnostics,
            i4PromptBoundaryEvidence);
}

internal readonly record struct I6C6LiveGameplayObservationV1(
    ulong Ordinal,
    GameplayMessageV1 Message,
    PerspectiveSafeFrameV1 Frame);

internal sealed record I6C6NativeLinkPropertyReferenceV1(
    string EntityLocator,
    ulong BoundaryOrdinal,
    uint LinkRating,
    IReadOnlyList<PerspectiveSafeLinkMarkerV1> LinkMarkers);

internal readonly record struct I6C6LinkPropertyEvidenceResultV1(
    bool IsSuccess,
    I6C6ClosureHarnessErrorCodeV1 ErrorCode,
    bool OwnerPrivatePresent,
    bool OpponentHiddenAbsent,
    bool FaceUpPublicPresent,
    bool LinkRatingExactMatch,
    bool LinkMarkersExactMatch,
    uint? ObservedLinkRating,
    IReadOnlyList<PerspectiveSafeLinkMarkerV1> ObservedLinkMarkers);

internal readonly record struct I6C6CounterTransitionEvidenceV1(
    GameplayMessageKindV1 MessageKind,
    string EntityLocator,
    ushort CounterType,
    uint Before,
    uint Delta,
    uint ExpectedAfter,
    uint ActualAfter);

internal readonly record struct I6C6CounterPropertyEvidenceResultV1(
    bool IsSuccess,
    I6C6ClosureHarnessErrorCodeV1 ErrorCode,
    bool AddObserved,
    bool AddCurrentMatch,
    bool RemoveObserved,
    bool RemoveCurrentMatch,
    bool ResetLifecycleObserved,
    IReadOnlyList<I6C6CounterTransitionEvidenceV1> Transitions);

internal sealed record I6C6ClosureEvidenceRequirementsV1(
    I6C6NativeLinkPropertyReferenceV1? LinkReference,
    bool RequireCounterAdd,
    bool RequireCounterRemove,
    bool RequireCounterReset);

internal static class I6C6LinkPropertyEvidenceExtractorV1
{
    internal static I6C6LinkPropertyEvidenceResultV1 Extract(
        IReadOnlyList<I6C6LiveGameplayObservationV1> observations,
        I6C6NativeLinkPropertyReferenceV1 reference)
    {
        if (observations is null ||
            observations.Count == 0 ||
            reference is null ||
            string.IsNullOrEmpty(reference.EntityLocator) ||
            reference.LinkRating == 0 ||
            reference.LinkMarkers is null ||
            reference.LinkMarkers.Count == 0 ||
            !reference.EntityLocator.Contains(
                ":EXTRA_DECK:",
                StringComparison.Ordinal) ||
            !PublicSemanticLocatorV1.TryParse(
                reference.EntityLocator,
                out _))
        {
            return Failure(
                I6C6ClosureHarnessErrorCodeV1.InvalidPropertyEvidenceInput);
        }

        bool ownerPrivatePresent = false;
        bool opponentHiddenAbsent = false;
        bool faceUpPublicPresent = false;
        uint? observedRating = null;
        IReadOnlyList<PerspectiveSafeLinkMarkerV1> observedMarkers =
            Array.Empty<PerspectiveSafeLinkMarkerV1>();

        foreach (I6C6LiveGameplayObservationV1 observation in observations)
        {
            if (observation.Ordinal != reference.BoundaryOrdinal)
            {
                continue;
            }

            foreach (PerspectiveSafeEntityV1 entity in observation.Frame.Entities)
            {
                PerspectiveSafeCardPropertiesV1? current = entity.Current;
                bool hasLinkEvidence =
                    current?.LinkRating is not null &&
                    current.LinkMarkers.Count > 0;

                if (entity.Zone == PerspectiveSafeSemanticZoneV1.ExtraDeck &&
                    entity.IdentityKnown &&
                    string.Equals(
                        entity.Locator,
                        reference.EntityLocator,
                        StringComparison.Ordinal) &&
                    hasLinkEvidence)
                {
                    ownerPrivatePresent = true;
                    observedRating = current!.LinkRating;
                    observedMarkers = current.LinkMarkers.ToArray();
                }

                if (entity.Zone == PerspectiveSafeSemanticZoneV1.ExtraDeck &&
                    !entity.IdentityKnown &&
                    entity.Passcode is null &&
                    entity.Printed is null &&
                    entity.Current is null)
                {
                    opponentHiddenAbsent = true;
                }

                if (entity.IdentityKnown && entity.FaceUp && hasLinkEvidence)
                {
                    faceUpPublicPresent = true;
                }
            }
        }

        bool ratingExact = ownerPrivatePresent &&
            observedRating == reference.LinkRating;
        bool markersExact = ownerPrivatePresent &&
            observedMarkers.SequenceEqual(reference.LinkMarkers);
        if (!ownerPrivatePresent ||
            !opponentHiddenAbsent ||
            !faceUpPublicPresent)
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.PropertyEvidenceMissing,
                ownerPrivatePresent,
                opponentHiddenAbsent,
                faceUpPublicPresent,
                ratingExact,
                markersExact,
                observedRating,
                observedMarkers);
        }

        if (!ratingExact || !markersExact)
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.PropertyValueMismatch,
                ownerPrivatePresent,
                opponentHiddenAbsent,
                faceUpPublicPresent,
                ratingExact,
                markersExact,
                observedRating,
                observedMarkers);
        }

        return new(
            true,
            I6C6ClosureHarnessErrorCodeV1.None,
            ownerPrivatePresent,
            opponentHiddenAbsent,
            faceUpPublicPresent,
            true,
            true,
            observedRating,
            observedMarkers);
    }

    private static I6C6LinkPropertyEvidenceResultV1 Failure(
        I6C6ClosureHarnessErrorCodeV1 errorCode) =>
        new(
            false,
            errorCode,
            false,
            false,
            false,
            false,
            false,
            null,
            Array.Empty<PerspectiveSafeLinkMarkerV1>());
}

internal static class I6C6CounterPropertyEvidenceExtractorV1
{
    internal static I6C6CounterPropertyEvidenceResultV1 Extract(
        IReadOnlyList<I6C6LiveGameplayObservationV1> observations)
    {
        if (observations is null || observations.Count == 0)
        {
            return Failure(
                I6C6ClosureHarnessErrorCodeV1.InvalidPropertyEvidenceInput);
        }

        List<I6C6CounterTransitionEvidenceV1> transitions = new();
        bool addObserved = false;
        bool addCurrentMatch = true;
        bool removeObserved = false;
        bool removeCurrentMatch = true;
        bool resetLifecycleObserved = false;

        for (int index = 0; index < observations.Count; index++)
        {
            I6C6LiveGameplayObservationV1 observation = observations[index];
            if (IsLifecycleResetMessage(observation.Message.Kind) &&
                index > 0 &&
                HasAnyCounter(observations[index - 1].Frame) &&
                !HasAnyCounter(observation.Frame))
            {
                resetLifecycleObserved = true;
            }

            if (observation.Message.Kind is not
                (GameplayMessageKindV1.AddCounter or
                GameplayMessageKindV1.RemoveCounter))
            {
                continue;
            }

            GameplayCounterPayloadV1 payload = observation.Message.Counter;
            if (!TryGetCounterLocator(payload, out string locator))
            {
                return Failure(
                    I6C6ClosureHarnessErrorCodeV1.InvalidPropertyEvidenceInput);
            }

            PerspectiveSafeFrameV1? beforeFrame = index > 0
                ? observations[index - 1].Frame
                : null;
            if (!TryGetCounterCount(
                    beforeFrame,
                    locator,
                    payload.CounterType,
                    out uint before) ||
                !TryGetCounterCount(
                    observation.Frame,
                    locator,
                    payload.CounterType,
                    out uint actualAfter))
            {
                return Failure(
                    I6C6ClosureHarnessErrorCodeV1.PropertyEvidenceMissing);
            }

            uint expectedAfter;
            if (observation.Message.Kind == GameplayMessageKindV1.AddCounter)
            {
                addObserved = true;
                try
                {
                    expectedAfter = checked(before + payload.Count);
                }
                catch (OverflowException)
                {
                    return Failure(
                        I6C6ClosureHarnessErrorCodeV1.PropertyValueMismatch);
                }

                addCurrentMatch &= expectedAfter == actualAfter;
            }
            else
            {
                removeObserved = true;
                if (before == 0)
                {
                    return Failure(
                        I6C6ClosureHarnessErrorCodeV1.PropertyValueMismatch);
                }

                expectedAfter = payload.Count >= before
                    ? 0
                    : before - payload.Count;
                removeCurrentMatch &= expectedAfter == actualAfter;
            }

            transitions.Add(
                new(
                    observation.Message.Kind,
                    locator,
                    payload.CounterType,
                    before,
                    payload.Count,
                    expectedAfter,
                    actualAfter));
        }

        bool success = addObserved &&
            addCurrentMatch &&
            removeCurrentMatch;
        return new(
            success,
            success
                ? I6C6ClosureHarnessErrorCodeV1.None
                : I6C6ClosureHarnessErrorCodeV1.PropertyEvidenceMissing,
            addObserved,
            addCurrentMatch,
            removeObserved,
            removeCurrentMatch,
            resetLifecycleObserved,
            transitions.ToArray());
    }

    private static bool TryGetCounterLocator(
        GameplayCounterPayloadV1 payload,
        out string locator)
    {
        PublicSemanticZoneV1 zone = payload.Location switch
        {
            0x04 => PublicSemanticZoneV1.MonsterZone,
            0x08 => PublicSemanticZoneV1.SpellTrapZone,
            _ => default
        };
        if (payload.Location is not (0x04 or 0x08) ||
            !PublicSemanticLocatorV1.TryCreateIndexed(
                payload.Controller,
                zone,
                payload.Sequence,
                out PublicSemanticLocatorV1? value))
        {
            locator = string.Empty;
            return false;
        }

        locator = value!.Value;
        return true;
    }

    private static bool TryGetCounterCount(
        PerspectiveSafeFrameV1? frame,
        string locator,
        ushort counterType,
        out uint count)
    {
        count = 0;
        if (frame is null)
        {
            return true;
        }

        PerspectiveSafeEntityV1? entity = frame.Entities.SingleOrDefault(
            candidate => string.Equals(
                candidate.Locator,
                locator,
                StringComparison.Ordinal));
        if (entity is null || entity.Current is null)
        {
            return false;
        }

        PerspectiveSafeCounterV1[] counters = entity.Current.Counters
            .Where(counter => counter.Type == counterType)
            .ToArray();
        if (counters.Length > 1)
        {
            return false;
        }

        count = counters.Length == 0 ? 0 : counters[0].Count;
        return true;
    }

    private static bool HasAnyCounter(PerspectiveSafeFrameV1 frame) =>
        frame.Entities.Any(entity =>
            entity.Current is not null &&
            entity.Current.Counters.Count > 0);

    private static bool IsLifecycleResetMessage(
        GameplayMessageKindV1 kind) =>
        kind is GameplayMessageKindV1.Move or
            GameplayMessageKindV1.PosChange or
            GameplayMessageKindV1.Set or
            GameplayMessageKindV1.ShuffleDeck or
            GameplayMessageKindV1.ShuffleHand or
            GameplayMessageKindV1.ShuffleExtra or
            GameplayMessageKindV1.ShuffleSetCard or
            GameplayMessageKindV1.ReverseDeck or
            GameplayMessageKindV1.Swap or
            GameplayMessageKindV1.SwapGraveDeck;

    private static I6C6CounterPropertyEvidenceResultV1 Failure(
        I6C6ClosureHarnessErrorCodeV1 errorCode) =>
        new(
            false,
            errorCode,
            false,
            false,
            false,
            false,
            false,
            Array.Empty<I6C6CounterTransitionEvidenceV1>());
}

internal enum I6C6ExternalRuntimeReadinessResultV1 : byte
{
    None = 0,
    Ready = 1,
    ProcessExited = 2,
    DeadlineExpired = 3,
    Cancelled = 4,
    ObservationFailed = 5,
    PortOccupied = 6,
    ListenerOwnershipMismatch = 7
}

internal enum I6C6ExternalRuntimeListenerOwnershipResultV1 : byte
{
    Owned = 0,
    NotOwned = 1,
    Unavailable = 2
}

internal sealed class I6C6ExternalRuntimeReadinessException : Exception
{
    internal I6C6ExternalRuntimeReadinessException(
        I6C6ExternalRuntimeReadinessResultV1 result,
        bool processStarted = false)
    {
        Result = result;
        ProcessStarted = processStarted;
    }

    internal I6C6ExternalRuntimeReadinessResultV1 Result { get; }

    internal bool ProcessStarted { get; }
}

internal static class I6C6ExternalRuntimeStartupSequenceV1
{
    internal static async ValueTask<TResult> RunAsync<TRuntime, TResult>(
        Func<CancellationToken, ValueTask<TRuntime>> startRuntime,
        Func<TRuntime, CancellationToken, ValueTask<TResult>> startNext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startRuntime);
        ArgumentNullException.ThrowIfNull(startNext);
        TRuntime runtime = await startRuntime(cancellationToken)
            .ConfigureAwait(false);
        return await startNext(runtime, cancellationToken)
            .ConfigureAwait(false);
    }
}

internal sealed class I6C6ExternalRuntimeProcessOwnerV1 : IAsyncDisposable
{
    private static readonly TimeSpan ListenerReadinessPollInterval =
        TimeSpan.FromMilliseconds(25);

    private const uint ErrorInsufficientBuffer = 122;
    private const int AddressFamilyInterNetwork = 2;
    private const int AddressFamilyInterNetworkV6 = 23;

    private readonly Process process;

    private I6C6ExternalRuntimeProcessOwnerV1(Process process)
    {
        this.process = process;
    }

    internal static ProcessStartInfo CreateStartInfo(
        I6C6ClosureHarnessConfigurationV1 configuration,
        ConnectionConfigurationV1 connection)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(connection);
        I6C6ClosureHarnessValidationResultV1 validation =
            I6C6ClosureHarnessV1.ValidateConfiguration(configuration);
        if (!validation.IsSuccess)
        {
            throw new InvalidDataException(
                $"I6C6 runtime configuration is invalid: {validation.ErrorCode}");
        }

        ProcessStartInfo startInfo = new(configuration.RuntimeExecutablePath)
        {
            WorkingDirectory = Path.GetDirectoryName(
                configuration.RuntimeExecutablePath) ?? string.Empty,
            UseShellExecute = true,
            CreateNoWindow = false
        };
        startInfo.ArgumentList.Add("-C");
        startInfo.ArgumentList.Add(configuration.AssetRoot);
        startInfo.ArgumentList.Add("-r");
        startInfo.ArgumentList.Add("-m");
        startInfo.ArgumentList.Add("-i6c6-server-only");
        startInfo.ArgumentList.Add("-i6c6-server-port");
        startInfo.ArgumentList.Add(
            connection.Port.ToString(CultureInfo.InvariantCulture));
        return startInfo;
    }

    internal static async ValueTask<I6C6ExternalRuntimeProcessOwnerV1>
        StartAsync(
            I6C6ClosureHarnessBindingV1 binding,
            ConnectionConfigurationV1 connection,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(connection);
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetLoopbackAddress(connection.Host, out IPAddress expectedAddress))
        {
            throw new I6C6ExternalRuntimeReadinessException(
                I6C6ExternalRuntimeReadinessResultV1.ObservationFailed);
        }

        IReadOnlyList<IPEndPoint> initialListeners;
        try
        {
            initialListeners = GetActiveListeners();
        }
        catch (Exception)
        {
            throw new I6C6ExternalRuntimeReadinessException(
                I6C6ExternalRuntimeReadinessResultV1.ObservationFailed);
        }

        if (HasListenerOnPort(initialListeners, connection.Port))
        {
            throw new I6C6ExternalRuntimeReadinessException(
                I6C6ExternalRuntimeReadinessResultV1.PortOccupied);
        }

        ProcessStartInfo startInfo = CreateStartInfo(
            binding.Configuration,
            connection);
        Process started = Process.Start(startInfo) ??
            throw new InvalidOperationException(
                "The external EDOPro process could not be started.");
        I6C6ExternalRuntimeProcessOwnerV1 owner = new(started);

        I6C6ExternalRuntimeReadinessResultV1 readiness =
            await WaitForListenerAsync(
                    GetActiveListeners,
                    () => !owner.HasExited,
                    connection.Port,
                    binding.Configuration.ExternalRuntimeReadinessTimeout,
                    cancellationToken,
                    ListenerReadinessPollInterval,
                    expectedAddress,
                    endpoint => GetListenerOwnership(
                        endpoint,
                        owner.process.Id))
                .ConfigureAwait(false);
        if (readiness != I6C6ExternalRuntimeReadinessResultV1.Ready)
        {
            await owner.DisposeAsync().ConfigureAwait(false);
            throw new I6C6ExternalRuntimeReadinessException(
                readiness,
                processStarted: true);
        }

        return owner;
    }

    internal static ValueTask<I6C6ExternalRuntimeReadinessResultV1>
        WaitForListenerForTestAsync(
            Func<IReadOnlyList<IPEndPoint>> listenerSnapshot,
            Func<bool> processIsAlive,
            int expectedPort,
            TimeSpan timeout,
            CancellationToken cancellationToken,
            TimeSpan pollInterval,
            Func<IPEndPoint, I6C6ExternalRuntimeListenerOwnershipResultV1>?
                listenerOwnership = null) =>
        WaitForListenerAsync(
            listenerSnapshot,
            processIsAlive,
            expectedPort,
            timeout,
            cancellationToken,
            pollInterval,
            IPAddress.Loopback,
            listenerOwnership);

    internal static I6C6ExternalRuntimeListenerOwnershipResultV1
        GetListenerOwnershipForTest(IPEndPoint endpoint, int processId) =>
        GetListenerOwnership(endpoint, processId);

    internal bool HasExited => process.HasExited;

    private static async ValueTask<I6C6ExternalRuntimeReadinessResultV1>
        WaitForListenerAsync(
            Func<IReadOnlyList<IPEndPoint>> listenerSnapshot,
            Func<bool> processIsAlive,
            int expectedPort,
            TimeSpan timeout,
            CancellationToken cancellationToken,
            TimeSpan pollInterval,
            IPAddress expectedAddress,
            Func<IPEndPoint, I6C6ExternalRuntimeListenerOwnershipResultV1>?
                listenerOwnership)
    {
        if (listenerSnapshot is null ||
            processIsAlive is null ||
            expectedAddress is null ||
            expectedPort is < 1 or > 65535 ||
            timeout < TimeSpan.Zero ||
            pollInterval < TimeSpan.Zero)
        {
            return I6C6ExternalRuntimeReadinessResultV1.ObservationFailed;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return I6C6ExternalRuntimeReadinessResultV1.Cancelled;
            }

            bool isAlive;
            IReadOnlyList<IPEndPoint> listeners;
            try
            {
                isAlive = processIsAlive();
                listeners = listenerSnapshot();
            }
            catch (Exception)
            {
                return I6C6ExternalRuntimeReadinessResultV1
                    .ObservationFailed;
            }

            if (!isAlive)
            {
                return I6C6ExternalRuntimeReadinessResultV1.ProcessExited;
            }

            IPEndPoint? exactListener = FindExactLoopbackListener(
                listeners,
                expectedAddress,
                expectedPort);
            if (exactListener is not null)
            {
                if (listenerOwnership is null)
                {
                    return I6C6ExternalRuntimeReadinessResultV1.Ready;
                }

                return listenerOwnership(exactListener) switch
                {
                    I6C6ExternalRuntimeListenerOwnershipResultV1.Owned =>
                        I6C6ExternalRuntimeReadinessResultV1.Ready,
                    I6C6ExternalRuntimeListenerOwnershipResultV1.NotOwned =>
                        I6C6ExternalRuntimeReadinessResultV1
                            .ListenerOwnershipMismatch,
                    _ => I6C6ExternalRuntimeReadinessResultV1
                        .ObservationFailed
                };
            }

            TimeSpan elapsed = stopwatch.Elapsed;
            if (elapsed >= timeout)
            {
                return I6C6ExternalRuntimeReadinessResultV1.DeadlineExpired;
            }

            TimeSpan remaining = timeout - elapsed;
            TimeSpan delay = remaining < pollInterval
                ? remaining
                : pollInterval;
            try
            {
                // This only schedules another passive OS listener observation;
                // it never opens or retries a readiness connection.
                await Task.Delay(delay, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return I6C6ExternalRuntimeReadinessResultV1.Cancelled;
            }
        }
    }

    private static IReadOnlyList<IPEndPoint> GetActiveListeners() =>
        IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();

    private static bool TryGetLoopbackAddress(
        string host,
        out IPAddress address)
    {
        switch (host)
        {
            case "127.0.0.1":
                address = IPAddress.Loopback;
                return true;
            case "::1":
                address = IPAddress.IPv6Loopback;
                return true;
            default:
                address = IPAddress.None;
                return false;
        }
    }

    private static bool HasListenerOnPort(
        IReadOnlyList<IPEndPoint> listeners,
        int port) =>
        listeners.Any(listener => listener.Port == port);

    private static IPEndPoint? FindExactLoopbackListener(
        IReadOnlyList<IPEndPoint> listeners,
        IPAddress expectedAddress,
        int expectedPort)
    {
        if (listeners is null || expectedAddress is null)
        {
            return null;
        }

        int matchingPortCount = 0;
        IPEndPoint? matchingListener = null;
        foreach (IPEndPoint listener in listeners)
        {
            if (listener.Port != expectedPort)
            {
                continue;
            }

            matchingPortCount++;
            if (!expectedAddress.Equals(listener.Address))
            {
                return null;
            }

            matchingListener = listener;
        }

        return matchingPortCount == 1 ? matchingListener : null;
    }

    private static I6C6ExternalRuntimeListenerOwnershipResultV1
        GetListenerOwnership(IPEndPoint endpoint, int processId)
    {
        if (!OperatingSystem.IsWindows())
        {
            return I6C6ExternalRuntimeListenerOwnershipResultV1.Unavailable;
        }

        int addressFamily = endpoint.AddressFamily switch
        {
            AddressFamily.InterNetwork => AddressFamilyInterNetwork,
            AddressFamily.InterNetworkV6 => AddressFamilyInterNetworkV6,
            _ => 0
        };
        if (addressFamily == 0)
        {
            return I6C6ExternalRuntimeListenerOwnershipResultV1.Unavailable;
        }

        try
        {
            int bufferSize = 0;
            uint result = GetExtendedTcpTable(
                IntPtr.Zero,
                ref bufferSize,
                sort: false,
                addressFamily,
                TcpTableClass.OwnerPidListener,
                reserved: 0);
            if (result != ErrorInsufficientBuffer ||
                bufferSize <= sizeof(int))
            {
                return I6C6ExternalRuntimeListenerOwnershipResultV1
                    .Unavailable;
            }

            IntPtr table = Marshal.AllocHGlobal(bufferSize);
            try
            {
                result = GetExtendedTcpTable(
                    table,
                    ref bufferSize,
                    sort: false,
                    addressFamily,
                    TcpTableClass.OwnerPidListener,
                    reserved: 0);
                if (result != 0)
                {
                    return I6C6ExternalRuntimeListenerOwnershipResultV1
                        .Unavailable;
                }

                int rowCount = Marshal.ReadInt32(table);
                int rowSize = addressFamily == AddressFamilyInterNetwork
                    ? 24
                    : 56;
                if (rowCount < 0 ||
                    rowCount > (bufferSize - sizeof(int)) / rowSize)
                {
                    return I6C6ExternalRuntimeListenerOwnershipResultV1
                        .Unavailable;
                }

                IntPtr row = IntPtr.Add(table, sizeof(int));
                for (int index = 0; index < rowCount; index++)
                {
                    if (MatchesListenerRow(
                            row,
                            endpoint,
                            addressFamily,
                            out int owningProcessId) &&
                        owningProcessId == processId)
                    {
                        return I6C6ExternalRuntimeListenerOwnershipResultV1
                            .Owned;
                    }

                    row = IntPtr.Add(row, rowSize);
                }

                return I6C6ExternalRuntimeListenerOwnershipResultV1
                    .NotOwned;
            }
            finally
            {
                Marshal.FreeHGlobal(table);
            }
        }
        catch (Exception)
        {
            return I6C6ExternalRuntimeListenerOwnershipResultV1.Unavailable;
        }
    }

    private static bool MatchesListenerRow(
        IntPtr row,
        IPEndPoint endpoint,
        int addressFamily,
        out int owningProcessId)
    {
        if (addressFamily == AddressFamilyInterNetwork)
        {
            uint localAddress = unchecked((uint)Marshal.ReadInt32(row, 4));
            uint localPort = unchecked((uint)Marshal.ReadInt32(row, 8));
            owningProcessId = Marshal.ReadInt32(row, 20);
            return endpoint.Port == DecodeNetworkPort(localPort) &&
                endpoint.Address.Equals(
                    new IPAddress(BitConverter.GetBytes(localAddress)));
        }

        byte[] localAddressBytes = new byte[16];
        Marshal.Copy(row, localAddressBytes, 0, localAddressBytes.Length);
        uint localScopeId = unchecked((uint)Marshal.ReadInt32(row, 16));
        uint localPortV6 = unchecked((uint)Marshal.ReadInt32(row, 20));
        owningProcessId = Marshal.ReadInt32(row, 52);
        return endpoint.Port == DecodeNetworkPort(localPortV6) &&
            endpoint.Address.Equals(
                new IPAddress(localAddressBytes, localScopeId));
    }

    private static int DecodeNetworkPort(uint networkPort) =>
        BinaryPrimitives.ReverseEndianness((ushort)(networkPort & 0xffff));

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr tcpTable,
        ref int size,
        [MarshalAs(UnmanagedType.Bool)] bool sort,
        int addressFamily,
        TcpTableClass tableClass,
        uint reserved);

    private enum TcpTableClass : int
    {
        OwnerPidListener = 3
    }

    public ValueTask DisposeAsync()
    {
        try
        {
            if (!process.HasExited)
            {
                process.CloseMainWindow();
                process.WaitForExit(2000);
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
        }
        finally
        {
            process.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}

internal sealed class I6C6TcpCaptureTransportV1 : IByteTransport
{
    private readonly IByteTransport inner;
    private readonly List<byte[]> receivedChunks = new();

    internal I6C6TcpCaptureTransportV1(IByteTransport inner)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    internal IReadOnlyList<byte[]> ReceivedChunks => receivedChunks
        .Select(chunk => chunk.ToArray())
        .ToArray();

    internal int CtosResponseWriteCount { get; private set; }

    public ValueTask ConnectAsync(
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken) =>
        inner.ConnectAsync(host, port, timeout, cancellationToken);

    public async ValueTask<int> ReadAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken)
    {
        int count = await inner.ReadAsync(destination, cancellationToken)
            .ConfigureAwait(false);
        if (count < 0 || count > destination.Length)
        {
            throw new InvalidDataException(
                "The TCP transport returned an invalid read count.");
        }

        if (count > 0)
        {
            receivedChunks.Add(destination[..count].ToArray());
        }

        return count;
    }

    public async ValueTask WriteAsync(
        ReadOnlyMemory<byte> source,
        CancellationToken cancellationToken)
    {
        await inner.WriteAsync(source, cancellationToken)
            .ConfigureAwait(false);
        FrameReadResult<ValidatedCtosPacket> parsed =
            PacketPayloadValidator.TryReadValidatedCtos(source.Span);
        if (parsed.Status == FrameReadStatus.Success &&
            parsed.Frame is not null &&
            parsed.Frame.Type == CtosPacketType.Response)
        {
            CtosResponseWriteCount = checked(CtosResponseWriteCount + 1);
        }
    }

    public ValueTask CloseAsync() => inner.CloseAsync();

    public ValueTask DisposeAsync() => inner.DisposeAsync();
}

internal static class I6C6ClosureHarnessV1
{
    private sealed class ExecutionScope
    {
        internal I6C6ClosureHarnessExceptionSiteV1 ExceptionSite =
            I6C6ClosureHarnessExceptionSiteV1.None;

        internal I6C6ClosureHarnessGameplayCaptureSubsiteV1
            GameplayCaptureSubsite =
                I6C6ClosureHarnessGameplayCaptureSubsiteV1.None;

        internal I6C6ExternalRuntimeProcessOwnerV1? ProcessOwner;
    }

    private static readonly Type[] ExistingIgnisPipelineTypes =
    {
        typeof(I2SessionRunner),
        typeof(GameplayMessageDecoderV1),
        typeof(GameplayMirrorSessionV1),
        typeof(PerspectiveStateMirrorV1),
        typeof(PerspectiveSafePublicFrameSourceV1)
    };

    private const string ExpectedRuntimeHead =
        "d72872347c34e7a7f37ba12b7e8fb20cdac78e0d";
    private const string ExpectedA2PatchCommit =
        "edfb77d7baf68b986209d327a871021296c2954b";
    private const string ExpectedA2PatchsetSha256 =
        "fd97edae44cb07a0b43f477f14863c40177eb4ac9a804d7c425450f07d5894d7";
    private const string ExpectedStartupCompatPatchCommit =
        ExpectedRuntimeHead;
    private const string ExpectedStartupCompatPatchsetSha256 =
        "83bf958fd115b6f85e4dee744dfc4685d5612d1c9d795480adc01831e7e33b49";
    private const string ExpectedServerBootstrapRuntimeExecutableSha256 =
        "ebb959d087ed0dd26a2b891f4db42ff5afc79f73e3aeadce06dac10fb49b504e";
    private const string ExpectedTimerGuardParent =
        ExpectedServerBootstrapPatchCommit;
    private const string ExpectedTimerGuardCommit =
        "2f728fd80e82eb952baeb7ffe9968c7086dca9d0";
    private const string ExpectedTimerGuardPatchsetSha256 =
        "35613a11761aa76a770d9400186895c38f1aa9bf041739e4b2109bcd47f2a9a1";
    private const string ExpectedTimerGuardRuntimeExecutableSha256 =
        "7f0433b62660e9c1dae3054ca07a446c07a7d112d005e988d575deba64930f66";
    private const string ExpectedRngParent = ExpectedTimerGuardCommit;
    private const string ExpectedRngImplementationCommit =
        "c5cd78d282c220b557df5cafe30cd6a6aed80c26";
    private const string ExpectedRngKatCommit =
        "4e1f93683d4ccec76558d06a5483f4090e30113c";
    private const string ExpectedRngCombinedPatchsetSha256 =
        "d5f4fdde30cd1fb427d92d07304806f95974f138a2bc94928f65e6885541d1a8";
    private const string ExpectedEvidenceRngId =
        "ocgforge-ignis.i6c6.evidence-rng.v1";
    private const ulong ExpectedEvidenceRngRoot = 0x2e43fb46490a681dUL;
    private const string ExpectedFinalRuntimeExecutableSha256 =
        "d57d9d705fb01ef2ba9d73eb89b29b6c438bf9d9c91e20daa8bca2fbf19babed";
    private static readonly TimeSpan MaximumExternalRuntimeReadinessTimeout =
        TimeSpan.FromMinutes(1);
    private const string ExpectedLoopbackHostPatchParent =
        ExpectedRuntimeHead;
    private const string ExpectedLoopbackHostPatchCommit =
        "68a660565650aa2988cf4ad55831bd9ef861931d";
    private const string ExpectedLoopbackHostPatchsetSha256 =
        "aa87d5467290b4ae7a95774e1e8ba2288a5587501991e0a7fcfc20271e102997";
    private const string ExpectedServerBootstrapPatchParent =
        ExpectedLoopbackHostPatchCommit;
    private const string ExpectedServerBootstrapPatchCommit =
        "690d031c9b882a36ffd1ea167af80d0ef9cf791b";
    private const string ExpectedServerBootstrapPatchsetSha256 =
        "7500ad5f75fe31910427343d65672b871e8a7c092f75f121ef57831ce4b0c31f";
    private const string ExpectedServerOnlyBootstrapFixParent =
        "4e1f93683d4ccec76558d06a5483f4090e30113c";
    private const string ExpectedServerOnlyBootstrapFixCommit =
        "1dcb983b2b7a2ead807eda8aa98d26066ce0baa3";
    private const string ExpectedServerOnlyBootstrapFixPatchsetSha256 =
        "5019259b4db733e4a38f285b45d0a4dea8b11a245047edee76fcb6234fa4bf79";
    private const string ExpectedServerOnlyBootstrapFixRuntimeExecutableSha256 =
        ExpectedFinalRuntimeExecutableSha256;
    private const string ExpectedDatabaseSha256 =
        "c49a077285e1d999f32056cb65303b75e311e859b4486c48f41772a193069225";
    private const string ExpectedCardscriptsCommit =
        "00a828b79303d047d6905f528857cc287ad3a84e";
    private const string ExpectedLinkPrimaryDeckSha256 =
        "5807306a04e08b452938aa06e6692738ffc8c3346cde3045202c6d380ddd4b10";
    private const string ExpectedLinkOpponentDeckSha256 =
        "0051f350303eed589fed1bba0cf58e345644c91cb5825415357a5ac297ee09b2";
    private const string ExpectedCounterPrimaryDeckSha256 =
        ExpectedLinkOpponentDeckSha256;
    private const string ExpectedCounterOpponentDeckSha256 =
        "ed30c491ad4323ed4729c2de68d7298714e01d71ac5321aaabcb7401a91fbda1";

    internal static I6C6ClosureHarnessValidationResultV1 ValidateConfiguration(
        I6C6ClosureHarnessConfigurationV1? configuration,
        bool requireLocalArtifacts = false)
    {
        if (configuration is null)
        {
            return Failure(
                I6C6ClosureHarnessErrorCodeV1.ScenarioConfigurationInvalid);
        }

        if (configuration.ExternalRuntimeReadinessTimeout <= TimeSpan.Zero ||
            configuration.ExternalRuntimeReadinessTimeout >
                MaximumExternalRuntimeReadinessTimeout)
        {
            return Failure(
                I6C6ClosureHarnessErrorCodeV1.ScenarioConfigurationInvalid);
        }

        if (!string.Equals(
                configuration.EdoproRuntimeHead,
                ExpectedRuntimeHead,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.A2PatchCommit,
                ExpectedA2PatchCommit,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.A2PatchsetSha256,
                ExpectedA2PatchsetSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.StartupCompatPatchCommit,
                ExpectedStartupCompatPatchCommit,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.StartupCompatPatchsetSha256,
                ExpectedStartupCompatPatchsetSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.LoopbackHostPatchParent,
                ExpectedLoopbackHostPatchParent,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.LoopbackHostPatchCommit,
                ExpectedLoopbackHostPatchCommit,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.LoopbackHostPatchsetSha256,
                ExpectedLoopbackHostPatchsetSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.ServerBootstrapPatchParent,
                ExpectedServerBootstrapPatchParent,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.ServerBootstrapPatchCommit,
                ExpectedServerBootstrapPatchCommit,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.ServerBootstrapPatchsetSha256,
                ExpectedServerBootstrapPatchsetSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.ServerBootstrapRuntimeExecutableSha256,
                ExpectedServerBootstrapRuntimeExecutableSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.ServerOnlyBootstrapFixParent,
                ExpectedServerOnlyBootstrapFixParent,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.ServerOnlyBootstrapFixCommit,
                ExpectedServerOnlyBootstrapFixCommit,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.ServerOnlyBootstrapFixPatchsetSha256,
                ExpectedServerOnlyBootstrapFixPatchsetSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.ServerOnlyBootstrapFixRuntimeExecutableSha256,
                ExpectedServerOnlyBootstrapFixRuntimeExecutableSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.TimerGuardParent,
                ExpectedTimerGuardParent,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.TimerGuardCommit,
                ExpectedTimerGuardCommit,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.TimerGuardPatchsetSha256,
                ExpectedTimerGuardPatchsetSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.TimerGuardRuntimeExecutableSha256,
                ExpectedTimerGuardRuntimeExecutableSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.RngParent,
                ExpectedRngParent,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.RngImplementationCommit,
                ExpectedRngImplementationCommit,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.RngKatCommit,
                ExpectedRngKatCommit,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.RngCombinedPatchsetSha256,
                ExpectedRngCombinedPatchsetSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.EvidenceRngId,
                ExpectedEvidenceRngId,
                StringComparison.Ordinal) ||
            configuration.EvidenceRngRoot != ExpectedEvidenceRngRoot ||
            !string.Equals(
                configuration.RuntimeExecutableSha256,
                ExpectedFinalRuntimeExecutableSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.FinalRuntimeExecutableSha256,
                ExpectedFinalRuntimeExecutableSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.DatabaseSha256,
                ExpectedDatabaseSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.CardscriptsCommit,
                ExpectedCardscriptsCommit,
                StringComparison.Ordinal))
        {
            return Failure(
                I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch);
        }

        if (!string.Equals(
                configuration.AssetRoot,
                @"C:\ProjectIgnis",
                StringComparison.OrdinalIgnoreCase) ||
            !IsAbsoluteNonEmptyPath(configuration.RuntimeExecutablePath))
        {
            return Failure(I6C6ClosureHarnessErrorCodeV1.AssetRootInvalid);
        }

        string executableName = GetPathFileName(
            configuration.RuntimeExecutablePath);
        if (!string.Equals(
                executableName,
                "ygoprodll.exe",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                executableName,
                "EDOPro.exe",
                StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                I6C6ClosureHarnessErrorCodeV1.ForbiddenRuntimeExecutable);
        }

        if (!IsScenario(
                configuration.LinkScenario,
                "projectignis.tactical-try.cyber-dragon.v1",
                ExpectedLinkPrimaryDeckSha256,
                ExpectedLinkOpponentDeckSha256,
                "EXTRA") ||
            !IsScenario(
                configuration.CounterScenario,
                "projectignis.windbot.ai-blackwing.v1",
                ExpectedCounterPrimaryDeckSha256,
                ExpectedCounterOpponentDeckSha256,
                "NONE"))
        {
            return Failure(
                I6C6ClosureHarnessErrorCodeV1.ScenarioConfigurationInvalid);
        }

        if (requireLocalArtifacts)
        {
            I6C6ClosureHarnessErrorCodeV1 localError =
                ValidateLocalArtifacts(configuration);
            if (localError != I6C6ClosureHarnessErrorCodeV1.None)
            {
                return Failure(localError);
            }
        }

        return new(
            true,
            I6C6ClosureHarnessErrorCodeV1.None,
            ExternalRuntimeProcessOwnerCount: 1,
            TcpCaptureOwnerCount: 1,
            ReusesIgnisDecoder:
                HasPipelineType(typeof(I2SessionRunner)) &&
                HasPipelineType(typeof(GameplayMessageDecoderV1)),
            ReusesIgnisMirror:
                HasPipelineType(typeof(GameplayMirrorSessionV1)) &&
                HasPipelineType(typeof(PerspectiveStateMirrorV1)),
            ReusesCurrentPath:
                HasPipelineType(typeof(PerspectiveSafePublicFrameSourceV1)),
            AllowsSyntheticEvidenceInRealMode: false,
            AllowsSyntheticLinkEvidenceInRealMode: false,
            AllowsSyntheticCounterEvidenceInRealMode: false);
    }

    internal static I6C6ClosureBindingResultV1 TryBind(
        I6C6ClosureHarnessConfigurationV1 configuration,
        I6C6ClosureScenarioKindV1 scenarioKind,
        bool requireLocalArtifacts)
    {
        I6C6ClosureHarnessValidationResultV1 validation =
            ValidateConfiguration(configuration, requireLocalArtifacts);
        if (!validation.IsSuccess)
        {
            return new(false, validation.ErrorCode, null);
        }

        I6C6ClosureScenarioConfigurationV1 scenario = scenarioKind switch
        {
            I6C6ClosureScenarioKindV1.Link => configuration.LinkScenario,
            I6C6ClosureScenarioKindV1.Counter => configuration.CounterScenario,
            _ => throw new ArgumentOutOfRangeException(nameof(scenarioKind))
        };
        return new(
            true,
            I6C6ClosureHarnessErrorCodeV1.None,
            I6C6ClosureHarnessBindingV1.Create(configuration, scenario));
    }

    internal static I6C6ClosureHarnessExecutionResultV1 TryBeginRealExecution(
        I6C6ClosureHarnessConfigurationV1 configuration,
        bool realRunAuthorized)
    {
        I6C6ClosureHarnessValidationResultV1 validation =
            ValidateConfiguration(configuration);
        if (!validation.IsSuccess)
        {
            return new(validation.ErrorCode, false, false);
        }

        return realRunAuthorized
            ? new(
                I6C6ClosureHarnessErrorCodeV1.RealExecutionRequiresInputs,
                false,
                false)
            : new(
                I6C6ClosureHarnessErrorCodeV1.RealRunNotAuthorized,
                false,
                false);
    }

    internal static I6C6ClosureHarnessExecutionDiagnosticsV1
        ClassifyPreDuelFailure(
            I2ErrorCode errorCode,
            I6C6ClosureHarnessPreDuelFailureStageV1 failureStage,
            bool cancellationRequested) =>
        cancellationRequested || errorCode == I2ErrorCode.Cancelled
            ? I6C6ClosureHarnessExecutionDiagnosticsV1.Cancelled(
                errorCode,
                failureStage)
            : I6C6ClosureHarnessExecutionDiagnosticsV1.PreDuelDrive(
                errorCode,
                failureStage);

    internal static I6C6ClosureHarnessExecutionResultV1
        UnexpectedExceptionResultForTest(
            I6C6ClosureHarnessExceptionSiteV1 site,
            Exception exception,
            bool processStarted) =>
        UnexpectedExceptionResult(site, exception, processStarted);

    internal static I6C6ClosureHarnessExecutionResultV1
        InvokeGameplayCapturePhaseForTest(
            I6C6ClosureHarnessGameplayCaptureSubsiteV1 subsite,
            Action operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ExecutionScope scope = new()
        {
            ExceptionSite = I6C6ClosureHarnessExceptionSiteV1.GameplayCapture,
            GameplayCaptureSubsite = subsite
        };
        try
        {
            operation();
            return new(
                I6C6ClosureHarnessErrorCodeV1.None,
                true,
                true);
        }
        catch (Exception exception)
        {
            return UnexpectedExceptionResult(
                scope.ExceptionSite,
                exception,
                true,
                scope.GameplayCaptureSubsite);
        }
    }

    internal static I6C6ClosureHarnessExecutionResultV1
        PreserveCleanupFailureForTest(
            I6C6ClosureHarnessExecutionResultV1 result,
            I6C6ClosureHarnessExceptionSiteV1 cleanupSite,
            Exception cleanupException) =>
        PreserveCleanupFailure(
            result,
            I6C6ClosureHarnessExecutionDiagnosticsV1.UnexpectedException(
                cleanupSite,
                cleanupException));

    private static I6C6ClosureHarnessExecutionResultV1
        UnexpectedExceptionResult(
            I6C6ClosureHarnessExceptionSiteV1 site,
            Exception exception,
            bool processStarted,
            I6C6ClosureHarnessGameplayCaptureSubsiteV1 gameplayCaptureSubsite =
                I6C6ClosureHarnessGameplayCaptureSubsiteV1.None) =>
        new(
            I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
            processStarted,
            false,
            Diagnostics:
                I6C6ClosureHarnessExecutionDiagnosticsV1.UnexpectedException(
                    site,
                    exception,
                    gameplayCaptureSubsite));

    private static I6C6ClosureHarnessExecutionResultV1
        PreserveCleanupFailure(
            I6C6ClosureHarnessExecutionResultV1 result,
            I6C6ClosureHarnessExecutionDiagnosticsV1 cleanupDiagnostics)
    {
        if (result.ErrorCode != I6C6ClosureHarnessErrorCodeV1.None ||
            !result.GameplayCaptureSucceeded ||
            result.Diagnostics is not null)
        {
            return result with
            {
                CleanupDiagnostics = result.CleanupDiagnostics ??
                    cleanupDiagnostics
            };
        }

        return new(
            I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
            result.ProcessStarted,
            false,
            result.Capture,
            cleanupDiagnostics);
    }

    internal static async ValueTask<I6C6ClosureHarnessExecutionResultV1>
        ExecuteAsync(
            I6C6ClosureHarnessConfigurationV1 configuration,
            I6C6ClosureScenarioKindV1 scenarioKind,
            ConnectionConfigurationV1 connection,
            PerspectiveSafeMatchContextV1 matchContext,
            PerspectiveSafePrintedProviderV1 printedProvider,
            int maximumAdditionalMessages,
            byte rpsChoice,
            byte turnPreference,
            bool realRunAuthorized,
            I6C6OpponentRuntimeParticipantLeaseV1 opponentRuntimeParticipant,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(matchContext);
        ArgumentNullException.ThrowIfNull(printedProvider);
        ArgumentNullException.ThrowIfNull(opponentRuntimeParticipant);
        if (!realRunAuthorized)
        {
            return new(
                I6C6ClosureHarnessErrorCodeV1.RealRunNotAuthorized,
                false,
                false);
        }

        I6C6ClosureBindingResultV1 bindingResult = TryBind(
            configuration,
            scenarioKind,
            requireLocalArtifacts: true);
        if (!bindingResult.IsSuccess || bindingResult.Binding is null)
        {
            return new(bindingResult.ErrorCode, false, false);
        }

        I6C6ClosureHarnessBindingV1 binding = bindingResult.Binding;
        if (!opponentRuntimeParticipant.IsLive ||
            !opponentRuntimeParticipant.IsForConnection(connection) ||
            !opponentRuntimeParticipant.Binding.Matches(binding.Scenario))
        {
            return new(
                I6C6ClosureHarnessErrorCodeV1.ScenarioInputProvenanceMismatch,
                false,
                false);
        }

        ExecutionScope scope = new();
        I6C6ClosureHarnessExecutionResultV1 result =
            await ExecuteWithResourcesAsync(
                    binding,
                    opponentRuntimeParticipant,
                    connection,
                    matchContext,
                    printedProvider,
                    maximumAdditionalMessages,
                    rpsChoice,
                    turnPreference,
                    cancellationToken,
                    scope)
                .ConfigureAwait(false);

        try
        {
            scope.ExceptionSite =
                I6C6ClosureHarnessExceptionSiteV1
                    .ExternalRuntimeOwnerDisposal;
            if (scope.ProcessOwner is not null)
            {
                await scope.ProcessOwner.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            return PreserveCleanupFailure(
                result,
                I6C6ClosureHarnessExecutionDiagnosticsV1.UnexpectedException(
                    I6C6ClosureHarnessExceptionSiteV1
                        .ExternalRuntimeOwnerDisposal,
                    exception));
        }

        return result;
    }

    private static async ValueTask<I6C6ClosureHarnessExecutionResultV1>
        ExecuteWithResourcesAsync(
            I6C6ClosureHarnessBindingV1 binding,
            I6C6OpponentRuntimeParticipantLeaseV1 opponentRuntimeParticipant,
            ConnectionConfigurationV1 connection,
            PerspectiveSafeMatchContextV1 matchContext,
            PerspectiveSafePrintedProviderV1 printedProvider,
            int maximumAdditionalMessages,
            byte rpsChoice,
            byte turnPreference,
            CancellationToken cancellationToken,
            ExecutionScope scope)
    {
        I6C6TcpCaptureTransportV1 captureTransport;
        try
        {
            scope.ExceptionSite =
                I6C6ClosureHarnessExceptionSiteV1.TransportConstruction;
            captureTransport = new(new TcpClientTransport());
        }
        catch (Exception exception)
        {
            return UnexpectedExceptionResult(
                I6C6ClosureHarnessExceptionSiteV1.TransportConstruction,
                exception,
                false);
        }

        I2SessionRunner runner;
        try
        {
            scope.ExceptionSite =
                I6C6ClosureHarnessExceptionSiteV1.RunnerConstruction;
            runner = new(captureTransport);
        }
        catch (Exception exception)
        {
            I6C6ClosureHarnessExecutionResultV1 constructionFailure =
                UnexpectedExceptionResult(
                    I6C6ClosureHarnessExceptionSiteV1.RunnerConstruction,
                    exception,
                    false);
            try
            {
                scope.ExceptionSite =
                    I6C6ClosureHarnessExceptionSiteV1
                        .CaptureTransportDisposal;
                await captureTransport.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception disposalException)
            {
                return PreserveCleanupFailure(
                    constructionFailure,
                    I6C6ClosureHarnessExecutionDiagnosticsV1.UnexpectedException(
                    I6C6ClosureHarnessExceptionSiteV1
                        .CaptureTransportDisposal,
                        disposalException));
            }

            return constructionFailure;
        }

        I6C6ClosureHarnessExecutionResultV1 result;
        try
        {
            result = await ExecuteCoreAsync(
                    binding,
                    opponentRuntimeParticipant,
                    connection,
                    matchContext,
                    printedProvider,
                    maximumAdditionalMessages,
                    rpsChoice,
                    turnPreference,
                    cancellationToken,
                    scope,
                    runner,
                    captureTransport)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            result = new(
                I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
                scope.ProcessOwner is not null,
                false,
                Diagnostics:
                    I6C6ClosureHarnessExecutionDiagnosticsV1.Cancelled());
        }
        catch (Exception exception)
        {
            result = UnexpectedExceptionResult(
                scope.ExceptionSite,
                exception,
                scope.ProcessOwner is not null,
                scope.GameplayCaptureSubsite);
        }

        Exception? disposalExceptionToReport = null;
        I6C6ClosureHarnessExceptionSiteV1 disposalSite =
            I6C6ClosureHarnessExceptionSiteV1.None;
        try
        {
            scope.ExceptionSite =
                I6C6ClosureHarnessExceptionSiteV1.RunnerDisposal;
            await runner.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            disposalExceptionToReport = exception;
            disposalSite =
                I6C6ClosureHarnessExceptionSiteV1.RunnerDisposal;
        }

        try
        {
            scope.ExceptionSite =
                I6C6ClosureHarnessExceptionSiteV1
                    .CaptureTransportDisposal;
            await captureTransport.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            if (disposalExceptionToReport is null)
            {
                disposalExceptionToReport = exception;
                disposalSite =
                    I6C6ClosureHarnessExceptionSiteV1
                        .CaptureTransportDisposal;
            }
        }

        return disposalExceptionToReport is null
            ? result
            : PreserveCleanupFailure(
                result,
                I6C6ClosureHarnessExecutionDiagnosticsV1.UnexpectedException(
                    disposalSite,
                    disposalExceptionToReport));
    }

    private static async ValueTask<I6C6ClosureHarnessExecutionResultV1>
        ExecuteCoreAsync(
            I6C6ClosureHarnessBindingV1 binding,
            I6C6OpponentRuntimeParticipantLeaseV1 opponentRuntimeParticipant,
            ConnectionConfigurationV1 connection,
            PerspectiveSafeMatchContextV1 matchContext,
            PerspectiveSafePrintedProviderV1 printedProvider,
            int maximumAdditionalMessages,
            byte rpsChoice,
            byte turnPreference,
            CancellationToken cancellationToken,
            ExecutionScope scope,
            I2SessionRunner runner,
            I6C6TcpCaptureTransportV1 captureTransport)
    {
        I2Result started;
        scope.ExceptionSite =
            I6C6ClosureHarnessExceptionSiteV1.ExternalRuntimeStartReadiness;
        try
        {
            started =
                await I6C6ExternalRuntimeStartupSequenceV1
                    .RunAsync(
                        token =>
                            I6C6ExternalRuntimeProcessOwnerV1.StartAsync(
                                binding,
                                connection,
                                token),
                        (owner, token) =>
                        {
                            scope.ProcessOwner = owner;
                            scope.ExceptionSite =
                                I6C6ClosureHarnessExceptionSiteV1.SessionStart;
                            return runner.StartAsync(connection, token);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return new(
                I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
                false,
                false,
                Diagnostics:
                    I6C6ClosureHarnessExecutionDiagnosticsV1.Cancelled());
        }
        catch (I6C6ExternalRuntimeReadinessException readinessException)
        {
            if (readinessException.Result ==
                I6C6ExternalRuntimeReadinessResultV1.Cancelled)
            {
                return new(
                    I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
                    readinessException.ProcessStarted,
                    false,
                    Diagnostics:
                        I6C6ClosureHarnessExecutionDiagnosticsV1.Cancelled(
                            readiness:
                                I6C6ExternalRuntimeReadinessResultV1.Cancelled));
            }

            return new(
                I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
                readinessException.ProcessStarted,
                false,
                Diagnostics:
                    I6C6ClosureHarnessExecutionDiagnosticsV1.ExternalRuntimeStart(
                        readinessException.Result));
        }
        catch (Exception exception)
        {
            return UnexpectedExceptionResult(
                scope.ExceptionSite,
                exception,
                scope.ProcessOwner is not null);
        }

        if (!started.IsSuccess)
        {
            I6C6ClosureHarnessExecutionDiagnosticsV1 diagnostics =
                cancellationToken.IsCancellationRequested ||
                started.Error == I2ErrorCode.Cancelled
                    ? I6C6ClosureHarnessExecutionDiagnosticsV1.Cancelled(
                        started.Error)
                    : I6C6ClosureHarnessExecutionDiagnosticsV1.SessionStart(
                        started.Error);
            return new(
                I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
                true,
                false,
                Diagnostics: diagnostics);
        }

        PrevalidatedProtocolDeck deck;
        scope.ExceptionSite = I6C6ClosureHarnessExceptionSiteV1.DeckLoad;
        try
        {
            deck = LoadDeck(binding.Scenario.PrimaryDeckPath);
        }
        catch (Exception exception)
        {
            return new(
                I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
                true,
                false,
                Diagnostics:
                    I6C6ClosureHarnessExecutionDiagnosticsV1.DeckLoad(
                        exception));
        }

        scope.ExceptionSite = I6C6ClosureHarnessExceptionSiteV1.PreDuelDrive;
        I6C6PreDuelHandoffResultV1 handoff =
            await DriveToGameplayAsync(
                    runner,
                    deck,
                    rpsChoice,
                    turnPreference,
                    cancellationToken)
                .ConfigureAwait(false);
        if (!handoff.IsSuccess || handoff.Offer is null)
        {
            return new(
                I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
                true,
                false,
                Diagnostics: ClassifyPreDuelFailure(
                    handoff.ErrorCode,
                    handoff.FailureStage,
                    cancellationToken.IsCancellationRequested));
        }

        scope.ExceptionSite = I6C6ClosureHarnessExceptionSiteV1.GameplayCapture;
        scope.GameplayCaptureSubsite =
            I6C6ClosureHarnessGameplayCaptureSubsiteV1.None;
        I6C6LiveGameplayCaptureResultV1 capture =
            await I6C6LiveGameplayCaptureResultV1.CaptureAsync(
                    binding,
                    opponentRuntimeParticipant.Binding,
                    handoff.Offer,
                    captureTransport,
                    matchContext,
                    printedProvider,
                    maximumAdditionalMessages,
                    cancellationToken,
                    subsite => scope.GameplayCaptureSubsite = subsite)
                .ConfigureAwait(false);
        if (!opponentRuntimeParticipant.IsLive)
        {
            I6C6ClosureHarnessExecutionDiagnosticsV1 diagnostics =
                cancellationToken.IsCancellationRequested
                    ? I6C6ClosureHarnessExecutionDiagnosticsV1.Cancelled()
                    : I6C6ClosureHarnessExecutionDiagnosticsV1.GameplayCapture();
            return new(
                I6C6ClosureHarnessErrorCodeV1.ScenarioInputProvenanceMismatch,
                true,
                false,
                capture,
                diagnostics);
        }

        I6C6ClosureHarnessExecutionDiagnosticsV1? captureDiagnostics =
            capture.IsSuccess
                ? null
                : cancellationToken.IsCancellationRequested ||
                  capture.ErrorCode == GameplayErrorCode.Cancelled
                    ? I6C6ClosureHarnessExecutionDiagnosticsV1.Cancelled()
                    : I6C6ClosureHarnessExecutionDiagnosticsV1.GameplayCapture();
        return new(
            capture.IsSuccess
                ? I6C6ClosureHarnessErrorCodeV1.None
                : I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
            true,
            capture.IsSuccess,
            capture,
            captureDiagnostics);
    }

    internal static I6C6ClosureEvidenceValidationResultV1 ValidateEvidence(
        I6C6LiveGameplayCaptureResultV1? capture,
        I6C6ClosureEvidenceRequirementsV1? requirements)
    {
        if (requirements is null ||
            requirements.LinkReference is null &&
            !requirements.RequireCounterAdd &&
            !requirements.RequireCounterRemove &&
            !requirements.RequireCounterReset)
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.InvalidPropertyEvidenceInput);
        }

        if (capture is null || !capture.IsSuccess)
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.IncompleteLiveEvidence);
        }

        if (requirements.LinkReference is not null &&
            !string.Equals(
                capture.Binding.Scenario.PatchedLocation,
                "EXTRA",
                StringComparison.Ordinal))
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.PropertyEvidenceMissing);
        }

        if (capture.ReceivedTcpChunks.Count == 0 ||
            capture.ReceivedTcpChunks.Any(chunk => chunk.Length == 0) ||
            capture.Observations.Count == 0 ||
            capture.Frame is null)
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.IncompleteLiveEvidence);
        }

        I6C6LinkPropertyEvidenceResultV1? linkEvidence = null;
        if (requirements.LinkReference is not null)
        {
            linkEvidence = I6C6LinkPropertyEvidenceExtractorV1.Extract(
                capture.Observations,
                requirements.LinkReference);
            if (!linkEvidence.Value.IsSuccess)
            {
                return new(
                    false,
                    linkEvidence.Value.ErrorCode,
                    linkEvidence,
                    null);
            }
        }

        I6C6CounterPropertyEvidenceResultV1? counterEvidence = null;
        if (requirements.RequireCounterAdd ||
            requirements.RequireCounterRemove ||
            requirements.RequireCounterReset)
        {
            counterEvidence = I6C6CounterPropertyEvidenceExtractorV1.Extract(
                capture.Observations);
            if (!counterEvidence.Value.IsSuccess ||
                requirements.RequireCounterAdd &&
                !counterEvidence.Value.AddObserved ||
                requirements.RequireCounterRemove &&
                (!counterEvidence.Value.RemoveObserved ||
                !counterEvidence.Value.RemoveCurrentMatch) ||
                requirements.RequireCounterReset &&
                !counterEvidence.Value.ResetLifecycleObserved)
            {
                return new(
                    false,
                    counterEvidence.Value.ErrorCode ==
                        I6C6ClosureHarnessErrorCodeV1.None
                        ? I6C6ClosureHarnessErrorCodeV1.PropertyEvidenceMissing
                        : counterEvidence.Value.ErrorCode,
                    linkEvidence,
                    counterEvidence);
            }
        }

        return new(
            true,
            I6C6ClosureHarnessErrorCodeV1.None,
            linkEvidence,
            counterEvidence);
    }

    internal static PrevalidatedProtocolDeck LoadDeck(string path)
    {
        if (!IsAbsoluteNonEmptyPath(path) || !File.Exists(path))
        {
            throw new FileNotFoundException(
                "The configured scenario deck does not exist.",
                path);
        }

        List<uint> main = new();
        List<uint> extra = new();
        List<uint> side = new();
        List<uint> active = main;
        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.Equals("#main", StringComparison.OrdinalIgnoreCase))
            {
                active = main;
                continue;
            }

            if (line.Equals("#extra", StringComparison.OrdinalIgnoreCase))
            {
                active = extra;
                continue;
            }

            if (line.Equals("!side", StringComparison.OrdinalIgnoreCase))
            {
                active = side;
                continue;
            }

            if (line[0] == '#' || line[0] == '!')
            {
                continue;
            }

            if (!uint.TryParse(
                    line,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out uint code) ||
                code == 0)
            {
                throw new InvalidDataException(
                    "The configured scenario deck contains an invalid card code.");
            }

            active.Add(code);
        }

        if (main.Count == 0)
        {
            throw new InvalidDataException(
                "The configured scenario deck contains no Main Deck cards.");
        }

        return new PrevalidatedProtocolDeck(main.Concat(extra), side);
    }

    internal static async ValueTask<I6C6PreDuelHandoffResultV1>
        DriveToGameplayAsync(
            I2SessionRunner runner,
            PrevalidatedProtocolDeck deck,
            byte rpsChoice,
            byte turnPreference,
            CancellationToken cancellationToken)
    {
        for (int step = 0; step < 256; step++)
        {
            I2PumpResult pumped = await runner.PumpReadAsync(cancellationToken)
                .ConfigureAwait(false);
            if (!pumped.IsSuccess)
            {
                return new(
                    false,
                    pumped.Error,
                    I6C6ClosureHarnessPreDuelFailureStageV1.PumpRead,
                    null);
            }

            if (pumped.RuntimeHandoff is not null)
            {
                return new(
                    true,
                    I2ErrorCode.None,
                    I6C6ClosureHarnessPreDuelFailureStageV1.None,
                    pumped.RuntimeHandoff);
            }

            if (pumped.ChoiceRequest is not null)
            {
                PreDuelChoiceRequest request = pumped.ChoiceRequest;
                byte choice = request.Kind == PreDuelChoiceKind.Rps
                    ? rpsChoice
                    : turnPreference;
                if (!request.LegalValues.Contains(choice))
                {
                    I6C6ClosureHarnessPreDuelFailureStageV1 choiceStage =
                        request.Kind == PreDuelChoiceKind.Rps
                            ? I6C6ClosureHarnessPreDuelFailureStageV1.RpsSelection
                            : I6C6ClosureHarnessPreDuelFailureStageV1
                                .TurnPreferenceSelection;
                    return new(
                        false,
                        I2ErrorCode.InvalidChoice,
                        choiceStage,
                        null);
                }

                I2Result submitted = await runner.SubmitChoiceAsync(
                        request.Token,
                        choice,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!submitted.IsSuccess)
                {
                    I6C6ClosureHarnessPreDuelFailureStageV1 choiceStage =
                        request.Kind == PreDuelChoiceKind.Rps
                            ? I6C6ClosureHarnessPreDuelFailureStageV1.RpsSelection
                            : I6C6ClosureHarnessPreDuelFailureStageV1
                                .TurnPreferenceSelection;
                    return new(false, submitted.Error, choiceStage, null);
                }

                continue;
            }

            switch (runner.State)
            {
                case I2SessionState.LobbyJoined:
                    I2Result deckSubmission = await runner.SubmitDeckAsync(
                            deck,
                            cancellationToken)
                        .ConfigureAwait(false);
                    if (!deckSubmission.IsSuccess)
                    {
                        return new(
                            false,
                            deckSubmission.Error,
                            I6C6ClosureHarnessPreDuelFailureStageV1
                                .DeckSubmission,
                            null);
                    }

                    break;

                case I2SessionState.DeckSubmitted:
                    I2Result readyRequest = await runner.RequestReadyAsync(
                            cancellationToken)
                        .ConfigureAwait(false);
                    if (!readyRequest.IsSuccess)
                    {
                        return new(
                            false,
                            readyRequest.Error,
                            I6C6ClosureHarnessPreDuelFailureStageV1.ReadyRequest,
                            null);
                    }

                    break;

                case I2SessionState.Ready:
                    if (!CanRequestDuelStart(runner))
                    {
                        break;
                    }

                    I2Result duelStartRequest = await
                        runner.RequestDuelStartAsync(cancellationToken)
                            .ConfigureAwait(false);
                    if (!duelStartRequest.IsSuccess)
                    {
                        return new(
                            false,
                            duelStartRequest.Error,
                            I6C6ClosureHarnessPreDuelFailureStageV1
                                .DuelStartRequest,
                            null);
                    }

                    break;

                case I2SessionState.DeckSubmitted or
                    I2SessionState.ReadyRequested or
                    I2SessionState.Starting or
                    I2SessionState.DuelStarted or
                    I2SessionState.WaitingForHandResult or
                    I2SessionState.WaitingForTpRequest or
                    I2SessionState.WaitingForTpChoice:
                    break;

                default:
                    return new(
                        false,
                        I2ErrorCode.InvalidStateTransition,
                        I6C6ClosureHarnessPreDuelFailureStageV1.InvalidState,
                        null);
            }
        }

        return new(
            false,
            I2ErrorCode.InvalidStateTransition,
            I6C6ClosureHarnessPreDuelFailureStageV1.RuntimeHandoff,
            null);
    }

    private static bool CanRequestDuelStart(I2SessionRunner runner)
    {
        if (runner.State != I2SessionState.Ready ||
            runner.PendingChoice is not null ||
            !runner.Lobby.IsHost ||
            runner.Lobby.PreDuelLobbyPosition is not (0 or 1))
        {
            return false;
        }

        IReadOnlyList<LobbyPlayerSnapshot> players =
            runner.Lobby.SnapshotPlayers();
        return IsOccupiedAndReady(
                players,
                ClientContractV1.FirstDuelistPosition) &&
            IsOccupiedAndReady(
                players,
                ClientContractV1.SecondDuelistPosition);
    }

    private static bool IsOccupiedAndReady(
        IReadOnlyList<LobbyPlayerSnapshot> players,
        byte position)
    {
        LobbyPlayerSnapshot[] matches = players
            .Where(player => player.Position == position)
            .ToArray();
        return matches.Length == 1 &&
            matches[0].IsOccupied &&
            matches[0].IsReady;
    }

    private static I6C6ClosureHarnessErrorCodeV1 ValidateLocalArtifacts(
        I6C6ClosureHarnessConfigurationV1 configuration)
    {
        if (!File.Exists(configuration.RuntimeExecutablePath))
        {
            return I6C6ClosureHarnessErrorCodeV1.RuntimeArtifactUnavailable;
        }

        string actualRuntimeHash =
            HashFile(configuration.RuntimeExecutablePath);
        if (!string.Equals(
                actualRuntimeHash,
                configuration.RuntimeExecutableSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                actualRuntimeHash,
                configuration.FinalRuntimeExecutableSha256,
                StringComparison.Ordinal))
        {
            return I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch;
        }

        string databasePath = Path.Combine(
            configuration.AssetRoot,
            "expansions",
            "cards.cdb");
        if (!File.Exists(databasePath) ||
            !string.Equals(
                HashFile(databasePath),
                configuration.DatabaseSha256,
                StringComparison.Ordinal))
        {
            return I6C6ClosureHarnessErrorCodeV1.ScenarioInputProvenanceMismatch;
        }

        foreach (I6C6ClosureScenarioConfigurationV1 scenario in new[]
                 {
                     configuration.LinkScenario,
                     configuration.CounterScenario
                 })
        {
            if (!File.Exists(scenario.PrimaryDeckPath) ||
                !File.Exists(scenario.OpponentDeckPath))
            {
                return I6C6ClosureHarnessErrorCodeV1.RuntimeArtifactUnavailable;
            }

            if (!string.Equals(
                    HashFile(scenario.PrimaryDeckPath),
                    scenario.PrimaryDeckSha256,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    HashFile(scenario.OpponentDeckPath),
                    scenario.OpponentDeckSha256,
                    StringComparison.Ordinal))
            {
                return I6C6ClosureHarnessErrorCodeV1.ScenarioInputProvenanceMismatch;
            }
        }

        return I6C6ClosureHarnessErrorCodeV1.None;
    }

    private static bool IsScenario(
        I6C6ClosureScenarioConfigurationV1? scenario,
        string expectedId,
        string expectedPrimaryDeckSha256,
        string expectedOpponentDeckSha256,
        string expectedPatchedLocation) =>
        scenario is not null &&
        string.Equals(scenario.ScenarioId, expectedId, StringComparison.Ordinal) &&
        string.Equals(
            scenario.PrimaryDeckSha256,
            expectedPrimaryDeckSha256,
            StringComparison.Ordinal) &&
        string.Equals(
            scenario.OpponentDeckSha256,
            expectedOpponentDeckSha256,
            StringComparison.Ordinal) &&
        string.Equals(
            scenario.PatchedLocation,
            expectedPatchedLocation,
            StringComparison.Ordinal) &&
        IsAbsoluteNonEmptyPath(scenario.PrimaryDeckPath) &&
        IsAbsoluteNonEmptyPath(scenario.OpponentDeckPath);

    private static bool IsAbsoluteNonEmptyPath(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (Path.IsPathFullyQualified(value) || IsWindowsAbsolutePath(value));

    private static bool IsWindowsAbsolutePath(string value) =>
        value.Length >= 3 &&
        char.IsLetter(value[0]) &&
        value[1] == ':' &&
        (value[2] == '\\' || value[2] == '/');

    private static string GetPathFileName(string value) =>
        Path.GetFileName(value.Replace('\\', '/'));

    private static bool HasPipelineType(Type value) =>
        Array.IndexOf(ExistingIgnisPipelineTypes, value) >= 0;

    private static string HashFile(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static I6C6ClosureHarnessValidationResultV1 Failure(
        I6C6ClosureHarnessErrorCodeV1 errorCode) =>
        new(
            false,
            errorCode,
            ExternalRuntimeProcessOwnerCount: 0,
            TcpCaptureOwnerCount: 0,
            ReusesIgnisDecoder: false,
            ReusesIgnisMirror: false,
            ReusesCurrentPath: false,
            AllowsSyntheticEvidenceInRealMode: false,
            AllowsSyntheticLinkEvidenceInRealMode: false,
            AllowsSyntheticCounterEvidenceInRealMode: false);

    internal readonly record struct I6C6PreDuelHandoffResultV1(
        bool IsSuccess,
        I2ErrorCode ErrorCode,
        I6C6ClosureHarnessPreDuelFailureStageV1 FailureStage,
        GameplayHandoffOfferV1? Offer);
}
