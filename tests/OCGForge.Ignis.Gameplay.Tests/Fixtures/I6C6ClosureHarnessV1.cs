using System.Diagnostics;
using System.Globalization;
using System.IO;
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
    I6C6LiveGameplayCaptureResultV1? Capture = null);

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
        I6C6FrameReadinessDiagnosticsV1 readinessDiagnostics)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        Binding = binding;
        OpponentRuntimeBinding = opponentRuntimeBinding;
        FailureDiagnostics = failureDiagnostics;
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
        I6C6FrameReadinessDiagnosticsV1? readinessDiagnostics = null) =>
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
                    null));

    internal static async ValueTask<I6C6LiveGameplayCaptureResultV1> CaptureAsync(
        I6C6ClosureHarnessBindingV1 binding,
        I6C6OpponentRuntimeBindingV1 opponentRuntimeBinding,
        GameplayHandoffOfferV1 handoff,
        I6C6TcpCaptureTransportV1 captureTransport,
        PerspectiveSafeMatchContextV1 matchContext,
        PerspectiveSafePrintedProviderV1 printedProvider,
        int maximumAdditionalMessages,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(opponentRuntimeBinding);
        ArgumentNullException.ThrowIfNull(handoff);
        ArgumentNullException.ThrowIfNull(captureTransport);
        ArgumentNullException.ThrowIfNull(matchContext);
        ArgumentNullException.ThrowIfNull(printedProvider);
        if (maximumAdditionalMessages < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumAdditionalMessages));
        }

        I6C6FrameReadinessTraceV1 readiness = new();
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
        await using GameplayMirrorSessionV1 session =
            new(
                first.Session,
                created.Mirror,
                matchContext,
                printedProvider);
        PerspectiveSafeFrameSourceResultV1 initialFrame =
            session.TryCreateI6C5Frame();
        ulong wireOrdinal = 0;

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

            readiness.InitialFrameError = initialFrame.Error;
            readiness.ProvisionalNotReadyCount = 1;
            readiness.MessagesAppliedBeforeReady =
                readiness.AppliedMessageKinds.Count;
        }

        for (int index = 0; index < maximumAdditionalMessages; index++)
        {
            int presentationMessagesBefore =
                session.PresentationMessagesConsumed;
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
                    readiness.Snapshot());
            }

            readiness.RecordMessage(next.Message);
            wireOrdinal = currentWireOrdinal;
            ulong ordinal = currentWireOrdinal;
            PerspectiveSafeFrameSourceResultV1 frame =
                session.TryCreateI6C5Frame();

            if (!frameReady)
            {
                if (frame.IsSuccess && frame.Frame is not null)
                {
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
                    readiness.ProvisionalNotReadyCount++;
                    readiness.MessagesAppliedBeforeReady =
                        readiness.AppliedMessageKinds.Count;
                    continue;
                }

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
        I6C6FrameReadinessDiagnosticsV1? readinessDiagnostics = null) =>
        FromCapture(
            false,
            error,
            binding,
            opponentRuntimeBinding,
            captureTransport.ReceivedChunks,
            observations,
            failureDiagnostics,
            readinessDiagnostics);
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

internal sealed class I6C6ExternalRuntimeProcessOwnerV1 : IAsyncDisposable
{
    private readonly Process process;

    private I6C6ExternalRuntimeProcessOwnerV1(Process process)
    {
        this.process = process;
    }

    internal static ProcessStartInfo CreateStartInfo(
        I6C6ClosureHarnessConfigurationV1 configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
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
        return startInfo;
    }

    internal static I6C6ExternalRuntimeProcessOwnerV1 Start(
        I6C6ClosureHarnessBindingV1 binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ProcessStartInfo startInfo = CreateStartInfo(binding.Configuration);
        Process started = Process.Start(startInfo) ??
            throw new InvalidOperationException(
                "The external EDOPro process could not be started.");
        return new(started);
    }

    internal bool HasExited => process.HasExited;

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

    public ValueTask WriteAsync(
        ReadOnlyMemory<byte> source,
        CancellationToken cancellationToken) =>
        inner.WriteAsync(source, cancellationToken);

    public ValueTask CloseAsync() => inner.CloseAsync();

    public ValueTask DisposeAsync() => inner.DisposeAsync();
}

internal static class I6C6ClosureHarnessV1
{
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
    private const string ExpectedRuntimeExecutableSha256 =
        "ebb959d087ed0dd26a2b891f4db42ff5afc79f73e3aeadce06dac10fb49b504e";
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
                ExpectedRuntimeExecutableSha256,
                StringComparison.Ordinal) ||
            !string.Equals(
                configuration.RuntimeExecutableSha256,
                ExpectedRuntimeExecutableSha256,
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

        I6C6ExternalRuntimeProcessOwnerV1? processOwner = null;
        try
        {
            processOwner = I6C6ExternalRuntimeProcessOwnerV1.Start(binding);
            await using I6C6TcpCaptureTransportV1 captureTransport =
                new(new TcpClientTransport());
            await using I2SessionRunner runner = new(captureTransport);

            I2Result started = await runner.StartAsync(
                    connection,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!started.IsSuccess)
            {
                return new(
                    I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
                    true,
                    false);
            }

            PrevalidatedProtocolDeck deck = LoadDeck(
                binding.Scenario.PrimaryDeckPath);
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
                    false);
            }

            I6C6LiveGameplayCaptureResultV1 capture =
                await I6C6LiveGameplayCaptureResultV1.CaptureAsync(
                        binding,
                        opponentRuntimeParticipant.Binding,
                        handoff.Offer,
                        captureTransport,
                        matchContext,
                        printedProvider,
                        maximumAdditionalMessages,
                        cancellationToken)
                    .ConfigureAwait(false);
            if (!opponentRuntimeParticipant.IsLive)
            {
                return new(
                    I6C6ClosureHarnessErrorCodeV1.ScenarioInputProvenanceMismatch,
                    true,
                    false,
                    capture);
            }

            return new(
                capture.IsSuccess
                    ? I6C6ClosureHarnessErrorCodeV1.None
                    : I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
                true,
                capture.IsSuccess,
                capture);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return new(
                I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
                processOwner is not null,
                false);
        }
        catch
        {
            return new(
                I6C6ClosureHarnessErrorCodeV1.ExecutionFailed,
                processOwner is not null,
                false);
        }
        finally
        {
            if (processOwner is not null)
            {
                await processOwner.DisposeAsync().ConfigureAwait(false);
            }
        }
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

    private static async ValueTask<I6C6PreDuelHandoffResultV1>
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
                return new(false, pumped.Error, null);
            }

            if (pumped.RuntimeHandoff is not null)
            {
                return new(true, I2ErrorCode.None, pumped.RuntimeHandoff);
            }

            if (pumped.ChoiceRequest is not null)
            {
                PreDuelChoiceRequest request = pumped.ChoiceRequest;
                byte choice = request.Kind == PreDuelChoiceKind.Rps
                    ? rpsChoice
                    : turnPreference;
                if (!request.LegalValues.Contains(choice))
                {
                    return new(
                        false,
                        I2ErrorCode.InvalidChoice,
                        null);
                }

                I2Result submitted = await runner.SubmitChoiceAsync(
                        request.Token,
                        choice,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!submitted.IsSuccess)
                {
                    return new(false, submitted.Error, null);
                }

                continue;
            }

            switch (runner.State)
            {
                case I2SessionState.LobbyJoined:
                    if (!(await runner.SubmitDeckAsync(
                                deck,
                                cancellationToken)
                            .ConfigureAwait(false)).IsSuccess)
                    {
                        return new(false, I2ErrorCode.DeckRejected, null);
                    }

                    break;

                case I2SessionState.DeckSubmitted:
                    if (!(await runner.RequestReadyAsync(cancellationToken)
                            .ConfigureAwait(false)).IsSuccess)
                    {
                        return new(false, I2ErrorCode.InvalidStateTransition, null);
                    }

                    break;

                case I2SessionState.Ready:
                    if (!(await runner.RequestDuelStartAsync(cancellationToken)
                            .ConfigureAwait(false)).IsSuccess)
                    {
                        return new(false, I2ErrorCode.InvalidStateTransition, null);
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
                        null);
            }
        }

        return new(
            false,
            I2ErrorCode.InvalidStateTransition,
            null);
    }

    private static I6C6ClosureHarnessErrorCodeV1 ValidateLocalArtifacts(
        I6C6ClosureHarnessConfigurationV1 configuration)
    {
        if (!File.Exists(configuration.RuntimeExecutablePath))
        {
            return I6C6ClosureHarnessErrorCodeV1.RuntimeArtifactUnavailable;
        }

        if (!string.Equals(
                HashFile(configuration.RuntimeExecutablePath),
                configuration.RuntimeExecutableSha256,
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

    private readonly record struct I6C6PreDuelHandoffResultV1(
        bool IsSuccess,
        I2ErrorCode ErrorCode,
        GameplayHandoffOfferV1? Offer);
}
