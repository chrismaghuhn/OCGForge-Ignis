using System.Security.Cryptography;
using System.Text;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;

namespace OCGForge.Ignis.Gameplay.Tests.Fixtures;

internal sealed record I6C6RealRunRequestV1(
    I6C6ClosureHarnessConfigurationV1 Configuration,
    I6C6ClosureScenarioKindV1 ScenarioKind,
    ConnectionConfigurationV1 Connection,
    PerspectiveSafeMatchContextV1 MatchContext,
    PerspectiveSafePrintedProviderV1 PrintedProvider,
    int MaximumAdditionalMessages,
    byte RpsChoice,
    byte TurnPreference,
    I6C6ClosureEvidenceRequirementsV1 Requirements);

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
            request.Requirements is null)
        {
            return Blocked();
        }

        I6C6ClosureHarnessValidationResultV1 validation =
            I6C6ClosureHarnessV1.ValidateConfiguration(
                request.Configuration,
                requireLocalArtifacts: true);
        if (!validation.IsSuccess ||
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
        I6C6ClosureEvidenceValidationResultV1 evidence)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);
        WriteAscii(writer, "OCGFORGE-IGNIS-I6C6-SAFE-RUNTIME-EVIDENCE-V1\0");
        writer.Write(capture.Binding.Scenario.ScenarioId);
        writer.Write(capture.Frame!.MatchContext.PerspectivePlayer);
        writer.Write(capture.ReceivedTcpChunks.Count);
        writer.Write(capture.Observations.Count);

        foreach (I6C6LiveGameplayObservationV1 observation in capture.Observations)
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
        writer.Write(globals.LifePoints.Count);
        foreach (uint lifePoints in globals.LifePoints)
        {
            writer.Write(lifePoints);
        }

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
            WriteOptionalByte(writer, entity.Owner);
            WriteOptionalByte(writer, entity.Controller);
            writer.Write((byte)entity.Zone);
            WriteOptionalUInt32(writer, entity.Sequence);
            WriteOptionalUInt32(writer, entity.OverlaySequence);
            writer.Write((byte)entity.Position);
            writer.Write(entity.FaceUp);
            writer.Write(entity.FaceDown);
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
