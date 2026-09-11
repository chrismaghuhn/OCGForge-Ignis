using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;

namespace OCGForge.Ignis.Gameplay.Tests.Fixtures;

internal readonly record struct I6C6OpponentRuntimeBindingResultV1(
    bool IsSuccess,
    I6C6ClosureHarnessErrorCodeV1 ErrorCode,
    I6C6OpponentRuntimeBindingV1? Binding);

internal sealed class I6C6OpponentRuntimeBindingV1
{
    private I6C6OpponentRuntimeBindingV1(
        string scenarioId,
        string actualParticipantDeckPath,
        string opponentDeckSha256,
        string participantInputIdentity)
    {
        ScenarioId = scenarioId;
        ActualParticipantDeckPath = actualParticipantDeckPath;
        OpponentDeckSha256 = opponentDeckSha256;
        ParticipantInputIdentity = participantInputIdentity;
    }

    internal string ScenarioId { get; }

    internal string ActualParticipantDeckPath { get; }

    internal string OpponentDeckSha256 { get; }

    internal string ParticipantInputIdentity { get; }

    internal bool Matches(I6C6ClosureScenarioConfigurationV1 scenario) =>
        string.Equals(ScenarioId, scenario.ScenarioId, StringComparison.Ordinal) &&
        string.Equals(
            ActualParticipantDeckPath,
            scenario.OpponentDeckPath,
            StringComparison.OrdinalIgnoreCase) &&
        string.Equals(
            OpponentDeckSha256,
            scenario.OpponentDeckSha256,
            StringComparison.Ordinal) &&
        ParticipantInputIdentity.Length != 0;

    internal static I6C6OpponentRuntimeBindingResultV1
        TryCreateFromActualParticipant(
            I6C6ClosureScenarioConfigurationV1 scenario,
            string actualParticipantDeckPath,
            string participantInputIdentity)
    {
        if (scenario is null ||
            string.IsNullOrWhiteSpace(actualParticipantDeckPath) ||
            string.IsNullOrWhiteSpace(participantInputIdentity) ||
            !File.Exists(actualParticipantDeckPath) ||
            !string.Equals(
                actualParticipantDeckPath,
                scenario.OpponentDeckPath,
                StringComparison.OrdinalIgnoreCase))
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.ScenarioInputProvenanceMismatch,
                null);
        }

        string actualHash;
        try
        {
            using FileStream stream = File.OpenRead(actualParticipantDeckPath);
            actualHash = Convert.ToHexString(SHA256.HashData(stream))
                .ToLowerInvariant();
        }
        catch (IOException)
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.RuntimeArtifactUnavailable,
                null);
        }

        if (!string.Equals(
                actualHash,
                scenario.OpponentDeckSha256,
                StringComparison.Ordinal))
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.ScenarioInputProvenanceMismatch,
                null);
        }

        return new(
            true,
            I6C6ClosureHarnessErrorCodeV1.None,
            new I6C6OpponentRuntimeBindingV1(
                scenario.ScenarioId,
                actualParticipantDeckPath,
                actualHash,
                participantInputIdentity));
    }
}

internal readonly record struct I6C6OpponentRuntimeParticipantLeaseResultV1(
    bool IsSuccess,
    I6C6ClosureHarnessErrorCodeV1 ErrorCode,
    I6C6OpponentRuntimeParticipantLeaseV1? Lease);

internal sealed class I6C6OpponentRuntimeParticipantLeaseV1 : IAsyncDisposable
{
    private const string ExpectedWindBotExecutablePath =
        @"C:\ProjectIgnis\WindBot\WindBot.exe";
    private const string ExpectedWindBotWorkingDirectory =
        @"C:\ProjectIgnis\WindBot";
    private const string ExpectedWindBotVersionArgument =
        "Version=0x000B0029";

    private readonly Process process;

    private I6C6OpponentRuntimeParticipantLeaseV1(
        Process process,
        I6C6OpponentRuntimeBindingV1 binding,
        int connectionPort)
    {
        this.process = process;
        Binding = binding;
        ConnectionPort = connectionPort;
    }

    internal I6C6OpponentRuntimeBindingV1 Binding { get; }

    internal int ConnectionPort { get; }

    internal bool IsLive => !process.HasExited;

    internal bool IsForConnection(ConnectionConfigurationV1 connection) =>
        connection.Port == ConnectionPort;

    internal static ProcessStartInfo CreateStartInfo(
        I6C6ClosureScenarioConfigurationV1 scenario,
        int connectionPort)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (connectionPort is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(connectionPort));
        }

        if (string.IsNullOrWhiteSpace(scenario.OpponentDeckPath) ||
            scenario.OpponentDeckPath.Contains('"', StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "The WindBot deck path is not a valid argument value.");
        }

        ProcessStartInfo startInfo = new(ExpectedWindBotExecutablePath)
        {
            WorkingDirectory = ExpectedWindBotWorkingDirectory,
            UseShellExecute = true,
            CreateNoWindow = false
        };
        startInfo.ArgumentList.Add($"DeckFile={scenario.OpponentDeckPath}");
        startInfo.ArgumentList.Add(
            $"Port={connectionPort.ToString(CultureInfo.InvariantCulture)}");
        startInfo.ArgumentList.Add(ExpectedWindBotVersionArgument);
        return startInfo;
    }

    internal static I6C6OpponentRuntimeParticipantLeaseResultV1
        TryCreateFromOwnedProcess(
            Process? process,
            I6C6ClosureScenarioConfigurationV1 scenario,
            string actualParticipantDeckPath,
            int connectionPort)
    {
        if (process is null || process.HasExited)
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.RuntimeArtifactUnavailable,
                null);
        }

        if (connectionPort is < 1 or > 65535)
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.ScenarioInputProvenanceMismatch,
                null);
        }

        string executablePath;
        try
        {
            executablePath = process.MainModule?.FileName ?? string.Empty;
        }
        catch (InvalidOperationException)
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.RuntimeArtifactUnavailable,
                null);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.RuntimeArtifactUnavailable,
                null);
        }

        if (!string.Equals(
                Path.GetFileName(executablePath),
                "WindBot.exe",
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                executablePath,
                ExpectedWindBotExecutablePath,
                StringComparison.OrdinalIgnoreCase) ||
            !HasProcessInput(process.StartInfo, actualParticipantDeckPath) ||
            !HasPortInput(process.StartInfo, connectionPort) ||
            !HasVersionInput(process.StartInfo))
        {
            return new(
                false,
                I6C6ClosureHarnessErrorCodeV1.ScenarioInputProvenanceMismatch,
                null);
        }

        I6C6OpponentRuntimeBindingResultV1 binding =
            I6C6OpponentRuntimeBindingV1.TryCreateFromActualParticipant(
                scenario,
                actualParticipantDeckPath,
                "windbot.deckfile.v1");
        if (!binding.IsSuccess || binding.Binding is null)
        {
            return new(false, binding.ErrorCode, null);
        }

        return new(
            true,
            I6C6ClosureHarnessErrorCodeV1.None,
            new I6C6OpponentRuntimeParticipantLeaseV1(
                process,
                binding.Binding,
                connectionPort));
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

    internal static bool HasProcessInputForTest(
        ProcessStartInfo startInfo,
        string deckPath) =>
        HasProcessInput(startInfo, deckPath);

    internal static bool HasPortInputForTest(
        ProcessStartInfo startInfo,
        int port) =>
        HasPortInput(startInfo, port);

    internal static bool HasVersionInputForTest(ProcessStartInfo startInfo) =>
        HasVersionInput(startInfo);

    private static bool HasProcessInput(
        ProcessStartInfo startInfo,
        string deckPath)
    {
        if (startInfo is null || string.IsNullOrWhiteSpace(deckPath))
        {
            return false;
        }

        string expectedDeckPath = NormalizeDeckPath(deckPath);
        return HasSingleExactAssignment(
            startInfo,
            "DeckFile",
            expectedDeckPath,
            StringComparison.OrdinalIgnoreCase,
            static value => NormalizeDeckPath(value));
    }

    private static bool HasPortInput(
        ProcessStartInfo startInfo,
        int port)
    {
        if (startInfo is null || port is < 1 or > 65535)
        {
            return false;
        }

        return HasSingleExactAssignment(
            startInfo,
            "Port",
            port.ToString(CultureInfo.InvariantCulture),
            StringComparison.Ordinal,
            static value => value);
    }

    private static bool HasVersionInput(ProcessStartInfo startInfo) =>
        HasSingleExactAssignment(
            startInfo,
            "Version",
            ExpectedWindBotVersionArgument["Version=".Length..],
            StringComparison.Ordinal,
            static value => value);

    private static bool HasSingleExactAssignment(
        ProcessStartInfo startInfo,
        string key,
        string expectedValue,
        StringComparison comparison,
        Func<string, string> normalize)
    {
        string prefix = key + "=";
        string[] assignments = GetEffectiveArgumentTokens(startInfo)
            .Where(argument =>
                argument.StartsWith(prefix, StringComparison.Ordinal))
            .ToArray();
        return assignments.Length == 1 &&
            TryReadExactAssignment(assignments[0], key, out string actualValue) &&
            string.Equals(
                normalize(actualValue),
                expectedValue,
                comparison);
    }

    private static IEnumerable<string> GetEffectiveArgumentTokens(
        ProcessStartInfo startInfo)
    {
        if (startInfo.ArgumentList.Count != 0)
        {
            if (!string.IsNullOrWhiteSpace(startInfo.Arguments))
            {
                yield break;
            }

            foreach (string argument in startInfo.ArgumentList)
            {
                yield return argument;
            }

            yield break;
        }

        foreach (string argument in TokenizeRawArguments(startInfo.Arguments))
        {
            yield return argument;
        }
    }

    private static bool TryReadExactAssignment(
        string argument,
        string key,
        out string value)
    {
        value = string.Empty;
        string prefix = key + "=";
        if (string.IsNullOrEmpty(argument) ||
            !argument.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        string rawValue = argument[prefix.Length..];
        if (rawValue.Length == 0)
        {
            return false;
        }

        if (rawValue.Contains('"', StringComparison.Ordinal))
        {
            return false;
        }

        if (rawValue.Length == 0)
        {
            return false;
        }

        value = rawValue;
        return true;
    }

    private static string NormalizeDeckPath(string path)
    {
        return path
            .Replace('/', '\\')
            .TrimEnd('\\');
    }

    private static IReadOnlyList<string> TokenizeRawArguments(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return Array.Empty<string>();
        }

        List<string> tokens = new();
        StringBuilder token = new();
        bool inQuotes = false;
        bool tokenStarted = false;

        foreach (char character in arguments)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
                tokenStarted = true;
                continue;
            }

            if (char.IsWhiteSpace(character) && !inQuotes)
            {
                if (tokenStarted)
                {
                    tokens.Add(token.ToString());
                    token.Clear();
                    tokenStarted = false;
                }

                continue;
            }

            token.Append(character);
            tokenStarted = true;
        }

        if (inQuotes)
        {
            return Array.Empty<string>();
        }

        if (tokenStarted)
        {
            tokens.Add(token.ToString());
        }

        return tokens;
    }
}

internal sealed record I6C6RealRunRequestV1(
    I6C6ClosureHarnessConfigurationV1 Configuration,
    I6C6ClosureScenarioKindV1 ScenarioKind,
    ConnectionConfigurationV1 Connection,
    PerspectiveSafeMatchContextV1 MatchContext,
    PerspectiveSafePrintedProviderV1 PrintedProvider,
    int MaximumAdditionalMessages,
    byte RpsChoice,
    byte TurnPreference,
    I6C6ClosureEvidenceRequirementsV1 Requirements,
    I6C6OpponentRuntimeParticipantLeaseV1 OpponentRuntimeParticipant);

internal readonly record struct I6C6RealRunEntryPointResultV1(
    bool IsSuccess,
    string Status,
    bool ChildRuntimeStarted,
    bool LiveEvidenceProduced,
    string? SafeEvidenceSha256 = null,
    I6C6ClosureHarnessExecutionResultV1? Execution = null,
    I6C6ClosureEvidenceValidationResultV1? Evidence = null);

internal static class I6C6RealRunEntryPointV1
{
    internal const string InputsUnavailableStatus =
        "STATUS=BLOCKED_I6C6_RUNTIME_INPUTS_UNAVAILABLE";

    internal static I6C6RealRunEntryPointResultV1 TryPrepare(
        I6C6RealRunRequestV1? request)
    {
        if (request is null ||
            request.Configuration is null ||
            request.Connection is null ||
            request.MatchContext is null ||
            request.PrintedProvider is null ||
            request.Requirements is null ||
            request.OpponentRuntimeParticipant is null)
        {
            return Blocked();
        }

        I6C6ClosureHarnessValidationResultV1 validation =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                request.Configuration,
                requireLocalArtifacts: true);
        I6C6ClosureScenarioConfigurationV1? scenario = request.ScenarioKind switch
        {
            I6C6ClosureScenarioKindV1.Link => request.Configuration.LinkScenario,
            I6C6ClosureScenarioKindV1.Counter => request.Configuration.CounterScenario,
            _ => null
        };
        if (!validation.IsSuccess ||
            scenario is null ||
            !request.OpponentRuntimeParticipant.IsLive ||
            !request.OpponentRuntimeParticipant.IsForConnection(
                request.Connection) ||
            !request.OpponentRuntimeParticipant.Binding.Matches(scenario) ||
            request.MatchContext.PerspectivePlayer > 1 ||
            !IsLoopback(request.Connection.Host) ||
            request.MaximumAdditionalMessages <= 0 ||
            !HasRequestedEvidence(request))
        {
            return Blocked();
        }

        if (!string.Equals(
                request.PrintedProvider.Manifest.OcgForgeSemanticCommit,
                "f929de0b4d4157327dba003067d2e21e42f7ad75",
                StringComparison.Ordinal) ||
            !string.Equals(
                request.PrintedProvider.Manifest.RulesBundleId,
                "3adfe6b4cfe2c2805e50b389fc0eb4e70a3b0b6107436614d328fddc865e585f",
                StringComparison.Ordinal))
        {
            return Blocked();
        }

        return new(
            true,
            "STATUS=I6C6_RUNTIME_INPUTS_READY",
            false,
            false);
    }

    internal static async ValueTask<I6C6RealRunEntryPointResultV1> ExecuteAsync(
        I6C6RealRunRequestV1? request,
        CancellationToken cancellationToken)
    {
        I6C6RealRunEntryPointResultV1 prepared = TryPrepare(request);
        if (!prepared.IsSuccess || request is null)
        {
            return prepared;
        }

        I6C6ClosureHarnessExecutionResultV1 execution =
            await I6C6ClosureHarnessV1.ExecuteAsync(
                    request.Configuration,
                    request.ScenarioKind,
                    request.Connection,
                    request.MatchContext,
                    request.PrintedProvider,
                    request.MaximumAdditionalMessages,
                    request.RpsChoice,
                    request.TurnPreference,
                    realRunAuthorized: true,
                    request.OpponentRuntimeParticipant,
                    cancellationToken)
                .ConfigureAwait(false);
        if (!execution.GameplayCaptureSucceeded || execution.Capture is null)
        {
            return new(
                false,
                "STATUS=I6C6_RUNTIME_EXECUTION_FAILED",
                execution.ProcessStarted,
                false,
                Execution: execution);
        }

        I6C6ClosureEvidenceValidationResultV1 evidence =
            I6C6ClosureHarnessV1.ValidateEvidence(
                execution.Capture,
                request.Requirements);
        if (!evidence.IsSuccess)
        {
            return new(
                false,
                "STATUS=I6C6_RUNTIME_EVIDENCE_INVALID",
                execution.ProcessStarted,
                true,
                Execution: execution,
                Evidence: evidence);
        }

        return new(
            true,
            "STATUS=I6C6_RUNTIME_EVIDENCE_PASS",
            execution.ProcessStarted,
            true,
            CanonicalSafeEvidenceSha256(execution.Capture, evidence),
            execution,
            evidence);
    }

    private static bool HasRequestedEvidence(I6C6RealRunRequestV1 request)
    {
        bool requested = request.Requirements.LinkReference is not null ||
            request.Requirements.RequireCounterAdd ||
            request.Requirements.RequireCounterRemove ||
            request.Requirements.RequireCounterReset;
        if (!requested)
        {
            return false;
        }

        return request.ScenarioKind switch
        {
            I6C6ClosureScenarioKindV1.Link =>
                request.Requirements.LinkReference is not null &&
                request.Requirements.RequireCounterAdd == false &&
                request.Requirements.RequireCounterRemove == false &&
                request.Requirements.RequireCounterReset == false,
            I6C6ClosureScenarioKindV1.Counter =>
                request.Requirements.LinkReference is null &&
                (request.Requirements.RequireCounterAdd ||
                 request.Requirements.RequireCounterRemove ||
                 request.Requirements.RequireCounterReset),
            _ => false
        };
    }

    private static bool IsLoopback(string host) =>
        host is "127.0.0.1" or "::1";

    private static I6C6RealRunEntryPointResultV1 Blocked() => new(
        false,
        InputsUnavailableStatus,
        false,
        false);

    private static string CanonicalSafeEvidenceSha256(
        I6C6LiveGameplayCaptureResultV1 capture,
        I6C6ClosureEvidenceValidationResultV1 evidence) =>
        CanonicalSafeEvidenceSha256Core(
            capture.Binding.Scenario.ScenarioId,
            capture.OpponentRuntimeBinding,
            capture.Observations,
            evidence);

    internal static string CanonicalSafeEvidenceSha256ForTest(
        string scenarioId,
        I6C6OpponentRuntimeBindingV1 opponentRuntimeBinding,
        int diagnosticReceivedTcpChunkCount,
        IReadOnlyList<I6C6LiveGameplayObservationV1> observations,
        I6C6ClosureEvidenceValidationResultV1 evidence)
    {
        _ = diagnosticReceivedTcpChunkCount;
        return CanonicalSafeEvidenceSha256Core(
            scenarioId,
            opponentRuntimeBinding,
            observations,
            evidence);
    }

    private static string CanonicalSafeEvidenceSha256Core(
        string scenarioId,
        I6C6OpponentRuntimeBindingV1 opponentRuntimeBinding,
        IReadOnlyList<I6C6LiveGameplayObservationV1> observations,
        I6C6ClosureEvidenceValidationResultV1 evidence)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);
        WriteAscii(writer, "OCGFORGE-IGNIS-I6C6-SAFE-RUNTIME-EVIDENCE-V1\0");
        writer.Write(scenarioId);
        writer.Write(opponentRuntimeBinding.OpponentDeckSha256);
        writer.Write(opponentRuntimeBinding.ParticipantInputIdentity);
        if (observations is null || observations.Count == 0)
        {
            throw new InvalidDataException("safe evidence needs observations");
        }

        writer.Write(observations[0].Frame.MatchContext.PerspectivePlayer);
        writer.Write(observations.Count);

        foreach (I6C6LiveGameplayObservationV1 observation in observations)
        {
            writer.Write(observation.Ordinal);
            writer.Write((byte)observation.Message.Kind);
            WriteSafeFrame(writer, observation.Frame);
        }

        if (evidence.LinkEvidence is { } link)
        {
            writer.Write((byte)1);
            writer.Write(link.OwnerPrivatePresent);
            writer.Write(link.OpponentHiddenAbsent);
            writer.Write(link.FaceUpPublicPresent);
            writer.Write(link.LinkRatingExactMatch);
            writer.Write(link.LinkMarkersExactMatch);
            WriteOptionalUInt32(writer, link.ObservedLinkRating);
            writer.Write(link.ObservedLinkMarkers.Count);
            foreach (PerspectiveSafeLinkMarkerV1 marker in link.ObservedLinkMarkers)
            {
                writer.Write((byte)marker);
            }
        }
        else
        {
            writer.Write((byte)0);
        }

        if (evidence.CounterEvidence is { } counter)
        {
            writer.Write((byte)1);
            writer.Write(counter.AddObserved);
            writer.Write(counter.AddCurrentMatch);
            writer.Write(counter.RemoveObserved);
            writer.Write(counter.RemoveCurrentMatch);
            writer.Write(counter.ResetLifecycleObserved);
            writer.Write(counter.Transitions.Count);
            foreach (I6C6CounterTransitionEvidenceV1 transition in
                     counter.Transitions)
            {
                writer.Write((byte)transition.MessageKind);
                writer.Write(transition.EntityLocator);
                writer.Write(transition.CounterType);
                writer.Write(transition.Before);
                writer.Write(transition.Delta);
                writer.Write(transition.ExpectedAfter);
                writer.Write(transition.ActualAfter);
            }
        }
        else
        {
            writer.Write((byte)0);
        }

        writer.Flush();
        return Convert.ToHexString(SHA256.HashData(stream.ToArray()))
            .ToLowerInvariant();
    }

    private static void WriteSafeFrame(
        BinaryWriter writer,
        PerspectiveSafeFrameV1 frame)
    {
        PerspectiveSafeGlobalsV1 globals = frame.Globals;
        writer.Write(globals.DuelFlags);
        writer.Write(globals.LifePoints.Count);
        foreach (uint lifePoints in globals.LifePoints)
        {
            writer.Write(lifePoints);
        }

        WriteOptionalByte(writer, globals.PlayerToAct);
        WriteOptionalByte(writer, globals.TurnPlayer);
        WriteOptionalUInt32(writer, globals.TurnCount);
        WriteOptionalUInt32(writer, globals.Phase);
        writer.Write(globals.ChainLength);
        WriteOptionalByte(writer, globals.Winner);
        WriteOptionalByte(writer, globals.WinReason);
        writer.Write(globals.Terminal);

        writer.Write(frame.Zones.Count);
        foreach (PerspectiveSafeZoneV1 zone in frame.Zones)
        {
            writer.Write(zone.Player);
            writer.Write((byte)zone.Kind);
            writer.Write(zone.TotalCount);
            writer.Write(zone.PublicIdentityCount);
            writer.Write(zone.HiddenCount);
            writer.Write(zone.PlayerObservableOrder);
        }

        writer.Write(frame.Entities.Count);
        foreach (PerspectiveSafeEntityV1 entity in frame.Entities)
        {
            writer.Write(entity.Locator);
            writer.Write(entity.IdentityKnown);
            if (!entity.IdentityKnown &&
                (entity.Passcode.HasValue ||
                 entity.Printed is not null ||
                 entity.Current is not null))
            {
                throw new InvalidDataException(
                    "hidden entity identity crossed the safe evidence boundary");
            }

            WriteOptionalUInt32(writer, entity.Passcode);
            WriteOptionalByte(writer, entity.Owner);
            WriteOptionalByte(writer, entity.Controller);
            writer.Write((byte)entity.Zone);
            WriteOptionalUInt32(writer, entity.Sequence);
            WriteOptionalUInt32(writer, entity.OverlaySequence);
            writer.Write((byte)entity.Position);
            writer.Write(entity.FaceUp);
            writer.Write(entity.FaceDown);
            WriteCurrentProperties(writer, entity.Printed);
            WriteCurrentProperties(writer, entity.Current);
        }

        writer.Write(frame.Relationships.Count);
        foreach (PerspectiveSafeRelationshipV1 relationship in frame.Relationships)
        {
            writer.Write((byte)relationship.Kind);
            writer.Write(relationship.Source);
            writer.Write(relationship.Target);
        }

        writer.Write(frame.Chain.Length);
        writer.Write(frame.Chain.Links.Count);
        foreach (PerspectiveSafeChainLinkV1 link in frame.Chain.Links)
        {
            writer.Write(link.Index);
            WriteOptionalByte(writer, link.ActivatingPlayer);
            WriteOptionalString(writer, link.Source);
            WriteOptionalByte(writer, link.ActivationZone is { }
                ? (byte)link.ActivationZone.Value
                : null);
            WriteOptionalUInt64(writer, link.EffectDescription);
            writer.Write(link.Targets.Count);
            foreach (string target in link.Targets)
            {
                writer.Write(target);
            }
        }

        writer.Write(frame.VisibleEvents.Count);
        foreach (PerspectiveSafeVisibleEventV1 visibleEvent in frame.VisibleEvents)
        {
            writer.Write(visibleEvent.EventIndex);
            writer.Write((byte)visibleEvent.Kind);
            WriteOptionalByte(writer, visibleEvent.Player);
            WriteOptionalString(writer, visibleEvent.EntityLocator);
            WriteOptionalUInt32(writer, visibleEvent.PublicPasscode);
            WriteOptionalByte(writer, visibleEvent.FromZone is { }
                ? (byte)visibleEvent.FromZone.Value
                : null);
            WriteOptionalByte(writer, visibleEvent.ToZone is { }
                ? (byte)visibleEvent.ToZone.Value
                : null);
            WriteOptionalUInt32(writer, visibleEvent.Count);
            WriteOptionalInt32(writer, visibleEvent.Amount);
            WriteOptionalUInt32(writer, visibleEvent.CounterType);
            WriteOptionalUInt32(writer, visibleEvent.Phase);
            WriteOptionalByte(writer, visibleEvent.Winner);
            WriteOptionalByte(writer, visibleEvent.WinReason);
            WriteOptionalUInt64(writer, visibleEvent.EffectDescription);
            writer.Write(visibleEvent.Targets.Count);
            foreach (string target in visibleEvent.Targets)
            {
                writer.Write(target);
            }
        }

        PerspectiveSafeMatchContextV1 context = frame.MatchContext;
        writer.Write(context.PerspectivePlayer);
        writer.Write(context.DuelFlags);
        writer.Write(context.Knowledge.OwnDecklistKnown);
        writer.Write(context.Knowledge.OpponentDecklistKnown);
        WriteDeck(writer, context.OwnDeck);
        WriteDeck(writer, context.OpponentDeck);
    }

    private static void WriteDeck(
        BinaryWriter writer,
        PerspectiveSafeDeckV1 deck)
    {
        writer.Write(deck.Known);
        writer.Write(deck.MainDeck.Count);
        foreach (uint passcode in deck.MainDeck)
        {
            writer.Write(passcode);
        }

        writer.Write(deck.ExtraDeck.Count);
        foreach (uint passcode in deck.ExtraDeck)
        {
            writer.Write(passcode);
        }
    }

    private static void WriteCurrentProperties(
        BinaryWriter writer,
        PerspectiveSafeCardPropertiesV1? properties)
    {
        writer.Write(properties is not null);
        if (properties is null)
        {
            return;
        }

        WriteOptionalUInt32(writer, properties.Type);
        WriteOptionalUInt32(writer, properties.Attribute);
        WriteOptionalUInt64(writer, properties.Race);
        WriteOptionalInt32(writer, properties.Attack);
        WriteOptionalInt32(writer, properties.Defense);
        WriteOptionalInt32(writer, properties.BaseAttack);
        WriteOptionalInt32(writer, properties.BaseDefense);
        WriteOptionalUInt32(writer, properties.Level);
        WriteOptionalUInt32(writer, properties.Rank);
        WriteOptionalUInt32(writer, properties.LinkRating);
        writer.Write(properties.LinkMarkers.Count);
        foreach (PerspectiveSafeLinkMarkerV1 marker in properties.LinkMarkers)
        {
            writer.Write((byte)marker);
        }

        WriteOptionalUInt32(writer, properties.LeftScale);
        WriteOptionalUInt32(writer, properties.RightScale);
        WriteOptionalUInt32(writer, properties.StatusFlags);
        writer.Write(properties.Counters.Count);
        foreach (PerspectiveSafeCounterV1 counter in properties.Counters)
        {
            writer.Write(counter.Type);
            writer.Write(counter.Count);
        }
    }

    private static void WriteAscii(BinaryWriter writer, string value) =>
        writer.Write(Encoding.ASCII.GetBytes(value));

    private static void WriteOptionalByte(BinaryWriter writer, byte? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
        {
            writer.Write(value.Value);
        }
    }

    private static void WriteOptionalUInt32(BinaryWriter writer, uint? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
        {
            writer.Write(value.Value);
        }
    }

    private static void WriteOptionalUInt64(BinaryWriter writer, ulong? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
        {
            writer.Write(value.Value);
        }
    }

    private static void WriteOptionalInt32(BinaryWriter writer, int? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
        {
            writer.Write(value.Value);
        }
    }

    private static void WriteOptionalString(BinaryWriter writer, string? value)
    {
        writer.Write(value is not null);
        if (value is not null)
        {
            writer.Write(value);
        }
    }
}
