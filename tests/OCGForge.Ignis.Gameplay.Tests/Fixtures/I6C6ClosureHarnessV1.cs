using System.IO;
using System.Diagnostics;
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
    IncompleteLiveEvidence = 7
}

internal enum I6C6ClosureEvidenceOriginV1 : byte
{
    LiveTcp = 1,
    Synthetic = 2
}

internal sealed record I6C6ClosureScenarioConfigurationV1(
    string ScenarioId,
    string PrimaryDeckPath,
    string OpponentDeckPath,
    string PatchedLocation);

internal sealed record I6C6ClosureHarnessConfigurationV1(
    string RuntimeExecutablePath,
    string AssetRoot,
    string EdoproRuntimeHead,
    string A2PatchCommit,
    string StartupCompatPatchCommit,
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
    bool DuelExecuted);

internal readonly record struct I6C6ClosureEvidenceCaptureV1(
    I6C6ClosureEvidenceOriginV1 Origin,
    bool HasReceivedTcpBytes,
    bool HasDecodedGameplayMessage,
    bool MirrorTransitionApplied,
    bool CurrentFrameProduced,
    bool LinkEvidenceFromLiveTcp,
    bool CounterEvidenceFromLiveTcp);

internal readonly record struct I6C6ClosureEvidenceValidationResultV1(
    bool IsSuccess,
    I6C6ClosureHarnessErrorCodeV1 ErrorCode);

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
        return startInfo;
    }

    internal static I6C6ExternalRuntimeProcessOwnerV1 Start(
        I6C6ClosureHarnessConfigurationV1 configuration)
    {
        ProcessStartInfo startInfo = CreateStartInfo(configuration);
        Process started = Process.Start(startInfo) ??
            throw new InvalidOperationException(
                "The external EDOPro process could not be started.");
        return new(started);
    }

    internal bool HasExited => process.HasExited;

    public ValueTask DisposeAsync()
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

        process.Dispose();
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
            throw new InvalidDataException("The TCP transport returned an invalid read count.");
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

internal sealed record I6C6LiveGameplayCaptureResultV1(
    bool IsSuccess,
    GameplayErrorCode ErrorCode,
    IReadOnlyList<byte[]> ReceivedTcpChunks,
    IReadOnlyList<GameplayMessageV1> Messages,
    PerspectiveSafeFrameV1? Frame);

internal static class I6C6LiveGameplayCaptureV1
{
    internal static async ValueTask<I6C6LiveGameplayCaptureResultV1> CaptureAsync(
        GameplayHandoffOfferV1 handoff,
        I6C6TcpCaptureTransportV1 captureTransport,
        PerspectiveSafeMatchContextV1 matchContext,
        PerspectiveSafePrintedProviderV1 printedProvider,
        int maximumAdditionalMessages,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handoff);
        ArgumentNullException.ThrowIfNull(captureTransport);
        ArgumentNullException.ThrowIfNull(matchContext);
        ArgumentNullException.ThrowIfNull(printedProvider);
        if (maximumAdditionalMessages < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumAdditionalMessages));
        }

        GameplayHandoffAcquireResult acquired =
            GameplayHandoffConsumerV1.TryCreate(handoff);
        if (!acquired.IsSuccess || acquired.Consumer is null)
        {
            return Failure(
                acquired.Error,
                captureTransport,
                Array.Empty<GameplayMessageV1>());
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
                first.Error,
                captureTransport,
                Array.Empty<GameplayMessageV1>());
        }

        MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
            first.Message,
            first.Perspective);
        if (!created.IsSuccess || created.Mirror is null)
        {
            return Failure(
                created.Error,
                captureTransport,
                new[] { first.Message });
        }

        await using GameplayMirrorSessionV1 session =
            new(
                first.Session,
                created.Mirror,
                matchContext,
                printedProvider);
        List<GameplayMessageV1> messages = new() { first.Message };
        for (int index = 0; index < maximumAdditionalMessages; index++)
        {
            GameplayMirrorPumpResult next = await session.PumpAsync(
                    cancellationToken)
                .ConfigureAwait(false);
            if (!next.IsSuccess || next.Message is null)
            {
                return Failure(next.Error, captureTransport, messages);
            }

            messages.Add(next.Message);
        }

        PerspectiveSafeFrameSourceResultV1 frame =
            session.TryCreateI6C5Frame();
        if (!frame.IsSuccess || frame.Frame is null)
        {
            return Failure(
                GameplayErrorCode.InvalidState,
                captureTransport,
                messages);
        }

        return new(
            true,
            GameplayErrorCode.None,
            captureTransport.ReceivedChunks,
            messages.ToArray(),
            frame.Frame);
    }

    private static I6C6LiveGameplayCaptureResultV1 Failure(
        GameplayErrorCode error,
        I6C6TcpCaptureTransportV1 captureTransport,
        IReadOnlyList<GameplayMessageV1> messages) =>
        new(
            false,
            error,
            captureTransport.ReceivedChunks,
            messages.ToArray(),
            null);
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
    private const string ExpectedStartupCompatPatchCommit =
        ExpectedRuntimeHead;

    internal static I6C6ClosureHarnessValidationResultV1 ValidateConfiguration(
        I6C6ClosureHarnessConfigurationV1? configuration)
    {
        if (configuration is null)
        {
            return Failure(I6C6ClosureHarnessErrorCodeV1.ScenarioConfigurationInvalid);
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
                configuration.StartupCompatPatchCommit,
                ExpectedStartupCompatPatchCommit,
                StringComparison.Ordinal))
        {
            return Failure(
                I6C6ClosureHarnessErrorCodeV1.RuntimeProvenanceMismatch);
        }

        if (string.IsNullOrWhiteSpace(configuration.AssetRoot) ||
            !Path.IsPathFullyQualified(configuration.AssetRoot))
        {
            return Failure(I6C6ClosureHarnessErrorCodeV1.AssetRootInvalid);
        }

        string executableName = Path.GetFileName(
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
                "EXTRA") ||
            !IsScenario(
                configuration.CounterScenario,
                "projectignis.windbot.ai-blackwing.v1",
                "NONE"))
        {
            return Failure(
                I6C6ClosureHarnessErrorCodeV1.ScenarioConfigurationInvalid);
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

    internal static I6C6ClosureHarnessExecutionResultV1 TryExecute(
        I6C6ClosureHarnessConfigurationV1 configuration,
        bool realRunAuthorized)
    {
        I6C6ClosureHarnessValidationResultV1 validation =
            ValidateConfiguration(configuration);
        if (!validation.IsSuccess)
        {
            return new(validation.ErrorCode, false, false);
        }

        if (!realRunAuthorized)
        {
            return new(
                I6C6ClosureHarnessErrorCodeV1.RealRunNotAuthorized,
                false,
                false);
        }

        throw new InvalidOperationException(
            "Real I6C6-3 execution is intentionally not part of this harness slice.");
    }

    internal static I6C6ClosureEvidenceValidationResultV1 ValidateEvidence(
        I6C6ClosureEvidenceCaptureV1 capture)
    {
        if (capture.Origin != I6C6ClosureEvidenceOriginV1.LiveTcp)
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.SyntheticEvidenceRejected);
        }

        if (!capture.HasReceivedTcpBytes ||
            !capture.HasDecodedGameplayMessage ||
            !capture.MirrorTransitionApplied ||
            !capture.CurrentFrameProduced ||
            !capture.LinkEvidenceFromLiveTcp ||
            !capture.CounterEvidenceFromLiveTcp)
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.IncompleteLiveEvidence);
        }

        return new(true, I6C6ClosureHarnessErrorCodeV1.None);
    }

    private static bool IsScenario(
        I6C6ClosureScenarioConfigurationV1? scenario,
        string expectedId,
        string expectedPatchedLocation) =>
        scenario is not null &&
        string.Equals(scenario.ScenarioId, expectedId, StringComparison.Ordinal) &&
        string.Equals(
            scenario.PatchedLocation,
            expectedPatchedLocation,
            StringComparison.Ordinal) &&
        IsAbsoluteNonEmptyPath(scenario.PrimaryDeckPath) &&
        IsAbsoluteNonEmptyPath(scenario.OpponentDeckPath);

    private static bool IsAbsoluteNonEmptyPath(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Path.IsPathFullyQualified(value);

    private static bool HasPipelineType(Type value) =>
        Array.IndexOf(ExistingIgnisPipelineTypes, value) >= 0;

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
}
