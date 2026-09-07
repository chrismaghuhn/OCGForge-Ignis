using System.Globalization;
using OCGForge.Ignis.Gameplay;

namespace OCGForge.Ignis.Gameplay.Tests.Fixtures;

internal static class I6C6ComparisonFixtures
{
    private const string SupportedMessageFamilyEnvelopeId =
        "ocgforge-ignis.i6c6.supported-message-family-envelope.v1";

    internal static I6C6ComparisonResultV1 Compare(
        I6C6PublicSafeStateV1 native,
        I6C6PublicSafeStateV1 ignis,
        I6C6ScenarioPairingV1 pairing)
    {
        if (!TryValidatePairing(pairing, out I6C6ComparisonResultV1 pairingFailure))
        {
            return pairingFailure;
        }

        if (native.PlayerToAct.IsPresent || ignis.PlayerToAct.IsPresent)
        {
            return Failure(
                I6C6ComparisonErrorCodeV1.BlockedPendingI6D);
        }

        if (!TryValidateState(native, out I6C6ComparisonResultV1 nativeFailure))
        {
            return nativeFailure;
        }

        if (!TryValidateState(ignis, out I6C6ComparisonResultV1 ignisFailure))
        {
            return ignisFailure;
        }

        if (native.TurnPlayer != ignis.TurnPlayer)
        {
            return Failure(
                I6C6ComparisonErrorCodeV1.OptionalPresenceMismatch);
        }

        string nativeSignature = Normalize(native);
        string ignisSignature = Normalize(ignis);
        if (!string.Equals(nativeSignature, ignisSignature, StringComparison.Ordinal))
        {
            return Failure(
                I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
        }

        return Success();
    }

    internal static I6C6FullComparisonResultV1 CompareFrameToNative(
        PerspectiveSafeFrameV1 frame,
        I6C6NativePublicSafeStateRowV1 native)
    {
        if (frame is null || native is null)
        {
            return FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "input");
        }

        if (frame.Globals.PlayerToAct.HasValue ||
            native.Globals is null ||
            native.Globals.PlayerToAct.HasValue)
        {
            return FullFailure(
                I6C6FullComparisonErrorCodeV1.BlockedPendingI6D,
                "globals.player_to_act");
        }

        if (!TryNormalizeFrame(
                frame,
                out I6C6NormalizedPublicSafeStateV1? normalizedFrame,
                out I6C6FullComparisonResultV1 frameFailure))
        {
            return frameFailure;
        }

        if (!TryNormalizeNative(
                native,
                out I6C6NormalizedPublicSafeStateV1? normalizedNative,
                out I6C6FullComparisonResultV1 nativeFailure))
        {
            return nativeFailure;
        }

        return CompareNormalized(normalizedFrame!, normalizedNative!);
    }

    private static bool TryNormalizeFrame(
        PerspectiveSafeFrameV1 frame,
        out I6C6NormalizedPublicSafeStateV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;

        if (!TryNormalizeFrameGlobals(
                frame.Globals,
                out I6C6NormalizedGlobalsV1? globals,
                out failure) ||
            !TryNormalizeFrameZones(
                frame.Zones,
                out I6C6NormalizedZoneV1[]? zones,
                out failure) ||
            !TryNormalizeFrameEntities(
                frame.Entities,
                out I6C6NormalizedEntityV1[]? entities,
                out failure) ||
            !TryNormalizeFrameRelationships(
                frame.Relationships,
                out I6C6NormalizedRelationshipV1[]? relationships,
                out failure) ||
            !TryNormalizeFrameChain(
                frame.Chain,
                out I6C6NormalizedChainV1? chain,
                out failure) ||
            !TryNormalizeFrameEvents(
                frame.VisibleEvents,
                out I6C6NormalizedVisibleEventV1[]? events,
                out failure) ||
            !TryNormalizeFrameMatchContext(
                frame.MatchContext,
                out I6C6NormalizedMatchContextV1? matchContext,
                out failure))
        {
            return false;
        }

        normalized = new I6C6NormalizedPublicSafeStateV1(
            globals!,
            zones!,
            entities!,
            relationships!,
            chain!,
            events!,
            matchContext!);
        failure = default;
        return true;
    }

    private static bool TryNormalizeNative(
        I6C6NativePublicSafeStateRowV1 native,
        out I6C6NormalizedPublicSafeStateV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (native.Globals is null || native.Chain is null ||
            native.MatchContext is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "native_public_safe_state");
            return false;
        }

        if (!TryNormalizeNativeGlobals(
                native.Globals,
                out I6C6NormalizedGlobalsV1? globals,
                out failure) ||
            !TryNormalizeNativeZones(
                native.Zones,
                out I6C6NormalizedZoneV1[]? zones,
                out failure) ||
            !TryNormalizeNativeEntities(
                native.Entities,
                out I6C6NormalizedEntityV1[]? entities,
                out failure) ||
            !TryNormalizeNativeRelationships(
                native.Relationships,
                out I6C6NormalizedRelationshipV1[]? relationships,
                out failure) ||
            !TryNormalizeNativeChain(
                native.Chain,
                out I6C6NormalizedChainV1? chain,
                out failure) ||
            !TryNormalizeNativeEvents(
                native.VisibleEvents,
                out I6C6NormalizedVisibleEventV1[]? events,
                out failure) ||
            !TryNormalizeNativeMatchContext(
                native.MatchContext,
                out I6C6NormalizedMatchContextV1? matchContext,
                out failure))
        {
            return false;
        }

        normalized = new I6C6NormalizedPublicSafeStateV1(
            globals!,
            zones!,
            entities!,
            relationships!,
            chain!,
            events!,
            matchContext!);
        failure = default;
        return true;
    }

    private static bool TryNormalizeFrameGlobals(
        PerspectiveSafeGlobalsV1 globals,
        out I6C6NormalizedGlobalsV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (globals is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "globals");
            return false;
        }

        IReadOnlyList<uint> lifePoints = globals.LifePoints;
        if (lifePoints is null || lifePoints.Count != 2 ||
            !IsValidOptionalPlayer(globals.TurnPlayer) ||
            !IsValidOptionalPlayer(globals.Winner))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "globals");
            return false;
        }

        normalized = new I6C6NormalizedGlobalsV1(
            globals.DuelFlags,
            lifePoints.ToArray(),
            globals.PlayerToAct,
            globals.TurnPlayer,
            globals.TurnCount,
            globals.Phase,
            globals.ChainLength,
            globals.Winner,
            globals.WinReason,
            globals.Terminal);
        failure = default;
        return true;
    }

    private static bool TryNormalizeNativeGlobals(
        I6C6NativeGlobalsV1 globals,
        out I6C6NormalizedGlobalsV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (globals is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "globals");
            return false;
        }

        IReadOnlyList<uint> lifePoints = globals.LifePoints;
        if (lifePoints is null || lifePoints.Count != 2 ||
            !IsValidOptionalPlayer(globals.TurnPlayer) ||
            !IsValidOptionalPlayer(globals.Winner))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "globals");
            return false;
        }

        normalized = new I6C6NormalizedGlobalsV1(
            globals.DuelFlags,
            lifePoints.ToArray(),
            globals.PlayerToAct,
            globals.TurnPlayer,
            globals.TurnCount,
            globals.Phase,
            globals.ChainLength,
            globals.Winner,
            globals.WinReason,
            globals.Terminal);
        failure = default;
        return true;
    }

    private static bool TryNormalizeFrameZones(
        IReadOnlyList<PerspectiveSafeZoneV1> source,
        out I6C6NormalizedZoneV1[]? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "zones");
            return false;
        }

        List<I6C6NormalizedZoneV1> result = new(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            PerspectiveSafeZoneV1 zone = source[index];
            if (zone.Player > 1 ||
                !TryMapZone(zone.Kind, out byte kind))
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.UnknownEnum,
                    $"zones[{index}].kind");
                return false;
            }

            result.Add(new I6C6NormalizedZoneV1(
                zone.Player,
                kind,
                zone.TotalCount,
                zone.PublicIdentityCount,
                zone.HiddenCount,
                zone.PlayerObservableOrder));
        }

        result.Sort(CompareZones);
        normalized = result.ToArray();
        failure = default;
        return true;
    }

    private static bool TryNormalizeNativeZones(
        IReadOnlyList<I6C6NativeZoneV1> source,
        out I6C6NormalizedZoneV1[]? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "zones");
            return false;
        }

        List<I6C6NormalizedZoneV1> result = new(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            I6C6NativeZoneV1 zone = source[index];
            if (zone.Player > 1 || zone.Kind > 10)
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.UnknownEnum,
                    $"zones[{index}].kind");
                return false;
            }

            result.Add(new I6C6NormalizedZoneV1(
                zone.Player,
                zone.Kind,
                zone.TotalCount,
                zone.PublicIdentityCount,
                zone.HiddenCount,
                zone.PlayerObservableOrder));
        }

        result.Sort(CompareZones);
        normalized = result.ToArray();
        failure = default;
        return true;
    }

    private static bool TryNormalizeFrameEntities(
        IReadOnlyList<PerspectiveSafeEntityV1> source,
        out I6C6NormalizedEntityV1[]? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "entities");
            return false;
        }

        List<I6C6NormalizedEntityV1> result = new(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            PerspectiveSafeEntityV1 entity = source[index];
            if (entity is null)
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidInput,
                    $"entities[{index}]");
                return false;
            }

            if (!TryNormalizeFrameEntity(
                    entity,
                    index,
                    out I6C6NormalizedEntityV1? value,
                    out failure))
            {
                return false;
            }

            result.Add(value!);
        }

        result.Sort(CompareEntities);
        if (HasDuplicateEntityLocator(result))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.DuplicateLocator,
                "entities.locator");
            return false;
        }

        normalized = result.ToArray();
        failure = default;
        return true;
    }

    private static bool TryNormalizeNativeEntities(
        IReadOnlyList<I6C6NativeEntityV1> source,
        out I6C6NormalizedEntityV1[]? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "entities");
            return false;
        }

        List<I6C6NormalizedEntityV1> result = new(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            I6C6NativeEntityV1 entity = source[index];
            if (entity is null)
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidInput,
                    $"entities[{index}]");
                return false;
            }

            if (!TryNormalizeNativeEntity(
                    entity,
                    index,
                    out I6C6NormalizedEntityV1? value,
                    out failure))
            {
                return false;
            }

            result.Add(value!);
        }

        result.Sort(CompareEntities);
        if (HasDuplicateEntityLocator(result))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.DuplicateLocator,
                "entities.locator");
            return false;
        }

        normalized = result.ToArray();
        failure = default;
        return true;
    }

    private static bool TryNormalizeFrameEntity(
        PerspectiveSafeEntityV1 entity,
        int sourceIndex,
        out I6C6NormalizedEntityV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (!TryValidateLocator(entity.Locator))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidLocator,
                $"entities[{sourceIndex}].locator");
            return false;
        }

        if (entity.Owner is > 1 || entity.Controller is > 1)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.UnknownEnum,
                $"entities[{sourceIndex}].owner_or_controller");
            return false;
        }

        if (!TryMapZone(entity.Zone, out byte zone) ||
            !TryMapPosition(entity.Position, out byte position))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.UnknownEnum,
                $"entities[{sourceIndex}].zone_or_position");
            return false;
        }

        if (!entity.IdentityKnown &&
            (entity.Passcode.HasValue || entity.Printed is not null ||
             entity.Current is not null))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.HiddenIdentityData,
                $"entities[{sourceIndex}].identity");
            return false;
        }

        if (entity.IdentityKnown &&
            (!entity.Passcode.HasValue || entity.Passcode.Value == 0))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.HiddenIdentityData,
                $"entities[{sourceIndex}].identity");
            return false;
        }

        if (!TryNormalizeFrameProperties(
                entity.Printed,
                $"entities[{sourceIndex}].printed",
                out I6C6NormalizedCardPropertiesV1? printed,
                out failure) ||
            !TryNormalizeFrameProperties(
                entity.Current,
                $"entities[{sourceIndex}].current",
                out I6C6NormalizedCardPropertiesV1? current,
                out failure))
        {
            return false;
        }

        normalized = new I6C6NormalizedEntityV1(
            entity.Locator,
            entity.IdentityKnown,
            entity.Passcode,
            entity.Owner,
            entity.Controller,
            zone,
            entity.Sequence,
            entity.OverlaySequence,
            position,
            entity.FaceUp,
            entity.FaceDown,
            printed,
            current);
        failure = default;
        return true;
    }

    private static bool TryNormalizeNativeEntity(
        I6C6NativeEntityV1 entity,
        int sourceIndex,
        out I6C6NormalizedEntityV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (!TryValidateLocator(entity.Locator))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidLocator,
                $"entities[{sourceIndex}].locator");
            return false;
        }

        if (entity.Owner is > 1 || entity.Controller is > 1 ||
            entity.Zone > 10 || !IsValidPosition(entity.Position))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.UnknownEnum,
                $"entities[{sourceIndex}].zone_or_position");
            return false;
        }

        if (entity.FaceUp && entity.FaceDown)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                $"entities[{sourceIndex}].face_flags");
            return false;
        }

        if (!entity.IdentityKnown &&
            (entity.Passcode.HasValue || entity.Printed is not null ||
             entity.Current is not null))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.HiddenIdentityData,
                $"entities[{sourceIndex}].identity");
            return false;
        }

        if (entity.IdentityKnown &&
            (!entity.Passcode.HasValue || entity.Passcode.Value == 0))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.HiddenIdentityData,
                $"entities[{sourceIndex}].identity");
            return false;
        }

        if (!TryNormalizeNativeProperties(
                entity.Printed,
                $"entities[{sourceIndex}].printed",
                out I6C6NormalizedCardPropertiesV1? printed,
                out failure) ||
            !TryNormalizeNativeProperties(
                entity.Current,
                $"entities[{sourceIndex}].current",
                out I6C6NormalizedCardPropertiesV1? current,
                out failure))
        {
            return false;
        }

        normalized = new I6C6NormalizedEntityV1(
            entity.Locator,
            entity.IdentityKnown,
            entity.Passcode,
            entity.Owner,
            entity.Controller,
            entity.Zone,
            entity.Sequence,
            entity.OverlaySequence,
            entity.Position,
            entity.FaceUp,
            entity.FaceDown,
            printed,
            current);
        failure = default;
        return true;
    }

    private static bool TryNormalizeFrameProperties(
        PerspectiveSafeCardPropertiesV1? source,
        string path,
        out I6C6NormalizedCardPropertiesV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = default;
            return true;
        }

        if (source.LinkMarkers is null || source.Counters is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                path);
            return false;
        }

        if (!TryMapFrameLinkMarkers(
                source.LinkMarkers,
                path,
                out byte[]? markers,
                out failure) ||
            !TryNormalizeFrameCounters(
                source.Counters,
                path,
                out I6C6NormalizedCounterV1[]? counters,
                out failure))
        {
            return false;
        }

        normalized = new I6C6NormalizedCardPropertiesV1(
            source.Type,
            source.Attribute,
            source.Race,
            source.Attack,
            source.Defense,
            source.BaseAttack,
            source.BaseDefense,
            source.Level,
            source.Rank,
            source.LinkRating,
            markers!,
            source.LeftScale,
            source.RightScale,
            source.StatusFlags,
            counters!);
        failure = default;
        return true;
    }

    private static bool TryNormalizeNativeProperties(
        I6C6NativeCardPropertiesV1? source,
        string path,
        out I6C6NormalizedCardPropertiesV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = default;
            return true;
        }

        if (source.LinkMarkers is null || source.Counters is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                path);
            return false;
        }

        if (source.LinkMarkers.Any(marker => marker > 7))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.UnknownEnum,
                path + ".link_markers");
            return false;
        }

        byte[] markers = source.LinkMarkers.ToArray();
        Array.Sort(markers);
        I6C6NormalizedCounterV1[] counters = source.Counters
            .Select(counter => new I6C6NormalizedCounterV1(counter.Type, counter.Count))
            .ToArray();
        Array.Sort(counters, CompareCounters);
        normalized = new I6C6NormalizedCardPropertiesV1(
            source.Type,
            source.Attribute,
            source.Race,
            source.Attack,
            source.Defense,
            source.BaseAttack,
            source.BaseDefense,
            source.Level,
            source.Rank,
            source.LinkRating,
            markers,
            source.LeftScale,
            source.RightScale,
            source.StatusFlags,
            counters);
        failure = default;
        return true;
    }

    private static bool TryMapFrameLinkMarkers(
        IReadOnlyList<PerspectiveSafeLinkMarkerV1> source,
        string path,
        out byte[]? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                path + ".link_markers");
            return false;
        }

        List<byte> values = new(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            if (!TryMapLinkMarker(source[index], out byte value))
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.UnknownEnum,
                    path + ".link_markers");
                return false;
            }

            values.Add(value);
        }

        values.Sort();
        normalized = values.ToArray();
        failure = default;
        return true;
    }

    private static bool TryNormalizeFrameCounters(
        IReadOnlyList<PerspectiveSafeCounterV1> source,
        string path,
        out I6C6NormalizedCounterV1[]? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                path + ".counters");
            return false;
        }

        I6C6NormalizedCounterV1[] values = source
            .Select(counter => new I6C6NormalizedCounterV1(counter.Type, counter.Count))
            .ToArray();
        Array.Sort(values, CompareCounters);
        normalized = values;
        failure = default;
        return true;
    }

    private static bool TryNormalizeFrameRelationships(
        IReadOnlyList<PerspectiveSafeRelationshipV1> source,
        out I6C6NormalizedRelationshipV1[]? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "relationships");
            return false;
        }

        List<I6C6NormalizedRelationshipV1> values = new(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            PerspectiveSafeRelationshipV1 relationship = source[index];
            if (relationship is null ||
                !TryValidateLocator(relationship.Source) ||
                !TryValidateLocator(relationship.Target) ||
                !TryMapRelationshipKind(
                    relationship.Kind,
                    out byte kind))
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidInput,
                    $"relationships[{index}]");
                return false;
            }

            values.Add(new I6C6NormalizedRelationshipV1(
                kind,
                relationship.Source,
                relationship.Target));
        }

        values.Sort(CompareRelationships);
        normalized = values.ToArray();
        failure = default;
        return true;
    }

    private static bool TryNormalizeNativeRelationships(
        IReadOnlyList<I6C6NativeRelationshipV1> source,
        out I6C6NormalizedRelationshipV1[]? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "relationships");
            return false;
        }

        List<I6C6NormalizedRelationshipV1> values = new(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            I6C6NativeRelationshipV1 relationship = source[index];
            if (relationship is null ||
                !TryValidateLocator(relationship.Source) ||
                !TryValidateLocator(relationship.Target) ||
                relationship.Kind > 2)
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidInput,
                    $"relationships[{index}]");
                return false;
            }

            values.Add(new I6C6NormalizedRelationshipV1(
                relationship.Kind,
                relationship.Source,
                relationship.Target));
        }

        values.Sort(CompareRelationships);
        normalized = values.ToArray();
        failure = default;
        return true;
    }

    private static bool TryNormalizeFrameChain(
        PerspectiveSafeChainStateV1 source,
        out I6C6NormalizedChainV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null || source.Links is null ||
            source.Length != (uint)source.Links.Count)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidOrdering,
                "chain");
            return false;
        }

        List<I6C6NormalizedChainLinkV1> links = new(source.Links.Count);
        uint? previousIndex = null;
        for (int index = 0; index < source.Links.Count; index++)
        {
            PerspectiveSafeChainLinkV1 link = source.Links[index];
            if (link is null)
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidInput,
                    $"chain.links[{index}]");
                return false;
            }

            if (previousIndex.HasValue && link.Index <= previousIndex.Value)
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidOrdering,
                    $"chain.links[{index}].index");
                return false;
            }

            if (link.ActivatingPlayer is > 1 ||
                !TryValidateOptionalLocator(link.Source))
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidInput,
                    $"chain.links[{index}]");
                return false;
            }

            if (!TryMapOptionalZone(
                    link.ActivationZone,
                    out byte? activationZone))
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.UnknownEnum,
                    $"chain.links[{index}].activation_zone");
                return false;
            }

            if (!TryNormalizeLocators(
                    link.Targets,
                    $"chain.links[{index}].targets",
                    out string[]? targets,
                    out failure))
            {
                return false;
            }

            links.Add(new I6C6NormalizedChainLinkV1(
                link.Index,
                link.ActivatingPlayer,
                link.Source,
                activationZone,
                link.EffectDescription,
                targets!));
            previousIndex = link.Index;
        }

        normalized = new I6C6NormalizedChainV1(source.Length, links.ToArray());
        failure = default;
        return true;
    }

    private static bool TryNormalizeNativeChain(
        I6C6NativeChainV1 source,
        out I6C6NormalizedChainV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null || source.Links is null ||
            source.Length != (uint)source.Links.Count)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidOrdering,
                "chain");
            return false;
        }

        List<I6C6NormalizedChainLinkV1> links = new(source.Links.Count);
        uint? previousIndex = null;
        for (int index = 0; index < source.Links.Count; index++)
        {
            I6C6NativeChainLinkV1 link = source.Links[index];
            if (link is null)
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidInput,
                    $"chain.links[{index}]");
                return false;
            }

            if (previousIndex.HasValue && link.Index <= previousIndex.Value)
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidOrdering,
                    $"chain.links[{index}].index");
                return false;
            }

            if (link.ActivatingPlayer is > 1 ||
                !TryValidateOptionalLocator(link.Source))
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidInput,
                    $"chain.links[{index}]");
                return false;
            }

            if (!TryValidateOptionalNativeZone(
                    link.ActivationZone,
                    out byte? activationZone))
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.UnknownEnum,
                    $"chain.links[{index}].activation_zone");
                return false;
            }

            if (!TryNormalizeLocators(
                    link.Targets,
                    $"chain.links[{index}].targets",
                    out string[]? targets,
                    out failure))
            {
                return false;
            }

            links.Add(new I6C6NormalizedChainLinkV1(
                link.Index,
                link.ActivatingPlayer,
                link.Source,
                activationZone,
                link.EffectDescription,
                targets!));
            previousIndex = link.Index;
        }

        normalized = new I6C6NormalizedChainV1(source.Length, links.ToArray());
        failure = default;
        return true;
    }

    private static bool TryNormalizeFrameEvents(
        IReadOnlyList<PerspectiveSafeVisibleEventV1> source,
        out I6C6NormalizedVisibleEventV1[]? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "visible_events");
            return false;
        }

        List<I6C6NormalizedVisibleEventV1> values = new(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            PerspectiveSafeVisibleEventV1 visibleEvent = source[index];
            if (visibleEvent is null)
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidInput,
                    $"visible_events[{index}]");
                return false;
            }

            if (!TryMapVisibleEventKind(visibleEvent.Kind, out byte kind) ||
                visibleEvent.Player is > 1 ||
                !TryValidateOptionalLocator(visibleEvent.EntityLocator) ||
                !TryMapOptionalZone(visibleEvent.FromZone, out byte? fromZone) ||
                !TryMapOptionalZone(visibleEvent.ToZone, out byte? toZone) ||
                !TryValidateOptionalPlayer(visibleEvent.Winner))
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidInput,
                    $"visible_events[{index}]");
                return false;
            }

            if (!TryNormalizeLocators(
                    visibleEvent.Targets,
                    $"visible_events[{index}].targets",
                    out string[]? targets,
                    out failure))
            {
                return false;
            }

            values.Add(new I6C6NormalizedVisibleEventV1(
                visibleEvent.EventIndex,
                kind,
                visibleEvent.Player,
                visibleEvent.EntityLocator,
                visibleEvent.PublicPasscode,
                fromZone,
                toZone,
                visibleEvent.Count,
                visibleEvent.Amount,
                visibleEvent.CounterType,
                visibleEvent.Phase,
                visibleEvent.Winner,
                visibleEvent.WinReason,
                visibleEvent.EffectDescription,
                targets!));
        }

        values.Sort(CompareVisibleEvents);
        if (HasDuplicateEventIndex(values))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.DuplicateEventIndex,
                "visible_events.event_index");
            return false;
        }

        normalized = values.ToArray();
        failure = default;
        return true;
    }

    private static bool TryNormalizeNativeEvents(
        IReadOnlyList<I6C6NativeVisibleEventV1> source,
        out I6C6NormalizedVisibleEventV1[]? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "visible_events");
            return false;
        }

        List<I6C6NormalizedVisibleEventV1> values = new(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            I6C6NativeVisibleEventV1 visibleEvent = source[index];
            if (visibleEvent is null)
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidInput,
                    $"visible_events[{index}]");
                return false;
            }

            if (visibleEvent.Kind > 22 ||
                visibleEvent.Player is > 1 ||
                !TryValidateOptionalLocator(visibleEvent.EntityLocator) ||
                !TryValidateOptionalNativeZone(visibleEvent.FromZone, out byte? fromZone) ||
                !TryValidateOptionalNativeZone(visibleEvent.ToZone, out byte? toZone) ||
                !TryValidateOptionalPlayer(visibleEvent.Winner))
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidInput,
                    $"visible_events[{index}]");
                return false;
            }

            if (visibleEvent.Targets is null)
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidInput,
                    $"visible_events[{index}].targets");
                return false;
            }

            if (!TryNormalizeLocators(
                    visibleEvent.Targets,
                    $"visible_events[{index}].targets",
                    out string[]? targets,
                    out failure))
            {
                return false;
            }

            values.Add(new I6C6NormalizedVisibleEventV1(
                visibleEvent.EventIndex,
                visibleEvent.Kind,
                visibleEvent.Player,
                visibleEvent.EntityLocator,
                visibleEvent.PublicPasscode,
                fromZone,
                toZone,
                visibleEvent.Count,
                visibleEvent.Amount,
                visibleEvent.CounterType,
                visibleEvent.Phase,
                visibleEvent.Winner,
                visibleEvent.WinReason,
                visibleEvent.EffectDescription,
                targets!));
        }

        values.Sort(CompareVisibleEvents);
        if (HasDuplicateEventIndex(values))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.DuplicateEventIndex,
                "visible_events.event_index");
            return false;
        }

        normalized = values.ToArray();
        failure = default;
        return true;
    }

    private static bool TryNormalizeFrameMatchContext(
        PerspectiveSafeMatchContextV1 source,
        out I6C6NormalizedMatchContextV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "match_context");
            return false;
        }

        if (!TryNormalizeFrameDeck(
                source.OwnDeck,
                "match_context.own_deck",
                out I6C6NormalizedDeckV1? ownDeck,
                out failure) ||
            !TryNormalizeFrameDeck(
                source.OpponentDeck,
                "match_context.opponent_deck",
                out I6C6NormalizedDeckV1? opponentDeck,
                out failure))
        {
            return false;
        }

        if (source.PerspectivePlayer > 1 ||
            source.Knowledge.OwnDecklistKnown != source.OwnDeck.Known ||
            source.Knowledge.OpponentDecklistKnown != source.OpponentDeck.Known)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "match_context");
            return false;
        }

        normalized = new I6C6NormalizedMatchContextV1(
            source.PerspectivePlayer,
            source.DuelFlags,
            source.Knowledge.OwnDecklistKnown,
            source.Knowledge.OpponentDecklistKnown,
            ownDeck!,
            opponentDeck!);
        failure = default;
        return true;
    }

    private static bool TryNormalizeNativeMatchContext(
        I6C6NativeMatchContextV1 source,
        out I6C6NormalizedMatchContextV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "match_context");
            return false;
        }

        if (!TryNormalizeNativeDeck(
                source.OwnDecklistKnown,
                source.OwnMainDeck,
                source.OwnExtraDeck,
                "match_context.own_deck",
                out I6C6NormalizedDeckV1? ownDeck,
                out failure) ||
            !TryNormalizeNativeDeck(
                source.OpponentDecklistKnown,
                source.OpponentMainDeck,
                source.OpponentExtraDeck,
                "match_context.opponent_deck",
                out I6C6NormalizedDeckV1? opponentDeck,
                out failure))
        {
            return false;
        }

        if (source.PerspectivePlayer > 1)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                "match_context.perspective_player");
            return false;
        }

        normalized = new I6C6NormalizedMatchContextV1(
            source.PerspectivePlayer,
            source.DuelFlags,
            source.OwnDecklistKnown,
            source.OpponentDecklistKnown,
            ownDeck!,
            opponentDeck!);
        failure = default;
        return true;
    }

    private static bool TryNormalizeFrameDeck(
        PerspectiveSafeDeckV1 source,
        string path,
        out I6C6NormalizedDeckV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null || source.MainDeck is null || source.ExtraDeck is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                path);
            return false;
        }

        return TryCreateNormalizedDeck(
            source.Known,
            source.MainDeck,
            source.ExtraDeck,
            path,
            out normalized,
            out failure);
    }

    private static bool TryNormalizeNativeDeck(
        bool known,
        IReadOnlyList<uint> mainDeck,
        IReadOnlyList<uint> extraDeck,
        string path,
        out I6C6NormalizedDeckV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (mainDeck is null || extraDeck is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                path);
            return false;
        }

        return TryCreateNormalizedDeck(
            known,
            mainDeck,
            extraDeck,
            path,
            out normalized,
            out failure);
    }

    private static bool TryCreateNormalizedDeck(
        bool known,
        IReadOnlyList<uint> mainDeck,
        IReadOnlyList<uint> extraDeck,
        string path,
        out I6C6NormalizedDeckV1? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (!known && (mainDeck.Count != 0 || extraDeck.Count != 0) ||
            mainDeck.Any(value => value == 0) ||
            extraDeck.Any(value => value == 0))
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                path);
            return false;
        }

        uint[] main = mainDeck.ToArray();
        uint[] extra = extraDeck.ToArray();
        Array.Sort(main);
        Array.Sort(extra);
        normalized = new I6C6NormalizedDeckV1(known, main, extra);
        failure = default;
        return true;
    }

    private static bool TryNormalizeLocators(
        IReadOnlyList<string> source,
        string path,
        out string[]? normalized,
        out I6C6FullComparisonResultV1 failure)
    {
        normalized = null;
        if (source is null)
        {
            failure = FullFailure(
                I6C6FullComparisonErrorCodeV1.InvalidInput,
                path);
            return false;
        }

        List<string> values = new(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            string locator = source[index];
            if (!TryValidateLocator(locator))
            {
                failure = FullFailure(
                    I6C6FullComparisonErrorCodeV1.InvalidLocator,
                    path);
                return false;
            }

            values.Add(locator);
        }

        values.Sort(StringComparer.Ordinal);
        normalized = values.ToArray();
        failure = default;
        return true;
    }

    private static bool TryValidateLocator(string? value) =>
        PublicSemanticLocatorV1.TryParse(value, out _);

    private static bool TryValidateOptionalLocator(string? value) =>
        value is null || TryValidateLocator(value);

    private static bool TryValidateOptionalPlayer(byte? value) =>
        !value.HasValue || value.Value <= 1;

    private static bool TryMapZone(
        PerspectiveSafeSemanticZoneV1 value,
        out byte code)
    {
        switch (value)
        {
            case PerspectiveSafeSemanticZoneV1.Unknown:
                code = 0;
                return true;
            case PerspectiveSafeSemanticZoneV1.MainDeck:
                code = 1;
                return true;
            case PerspectiveSafeSemanticZoneV1.Hand:
                code = 2;
                return true;
            case PerspectiveSafeSemanticZoneV1.MonsterZone:
                code = 3;
                return true;
            case PerspectiveSafeSemanticZoneV1.SpellTrapZone:
                code = 4;
                return true;
            case PerspectiveSafeSemanticZoneV1.Graveyard:
                code = 5;
                return true;
            case PerspectiveSafeSemanticZoneV1.Banished:
                code = 6;
                return true;
            case PerspectiveSafeSemanticZoneV1.ExtraDeck:
                code = 7;
                return true;
            case PerspectiveSafeSemanticZoneV1.FieldZone:
                code = 8;
                return true;
            case PerspectiveSafeSemanticZoneV1.PendulumRelevant:
                code = 9;
                return true;
            case PerspectiveSafeSemanticZoneV1.Overlay:
                code = 10;
                return true;
            default:
                code = 0;
                return false;
        }
    }

    private static bool TryMapPosition(
        PerspectiveSafePositionV1 value,
        out byte code)
    {
        switch (value)
        {
            case PerspectiveSafePositionV1.Unknown:
                code = 0;
                return true;
            case PerspectiveSafePositionV1.FaceUpAttack:
                code = 1;
                return true;
            case PerspectiveSafePositionV1.FaceDownAttack:
                code = 2;
                return true;
            case PerspectiveSafePositionV1.FaceUpDefense:
                code = 4;
                return true;
            case PerspectiveSafePositionV1.FaceDownDefense:
                code = 8;
                return true;
            default:
                code = 0;
                return false;
        }
    }

    private static bool TryMapLinkMarker(
        PerspectiveSafeLinkMarkerV1 value,
        out byte code)
    {
        switch (value)
        {
            case PerspectiveSafeLinkMarkerV1.BottomLeft:
                code = 0;
                return true;
            case PerspectiveSafeLinkMarkerV1.Bottom:
                code = 1;
                return true;
            case PerspectiveSafeLinkMarkerV1.BottomRight:
                code = 2;
                return true;
            case PerspectiveSafeLinkMarkerV1.Left:
                code = 3;
                return true;
            case PerspectiveSafeLinkMarkerV1.Right:
                code = 4;
                return true;
            case PerspectiveSafeLinkMarkerV1.TopLeft:
                code = 5;
                return true;
            case PerspectiveSafeLinkMarkerV1.Top:
                code = 6;
                return true;
            case PerspectiveSafeLinkMarkerV1.TopRight:
                code = 7;
                return true;
            default:
                code = 0;
                return false;
        }
    }

    private static bool TryMapRelationshipKind(
        PerspectiveSafeRelationshipKindV1 value,
        out byte code)
    {
        switch (value)
        {
            case PerspectiveSafeRelationshipKindV1.XyzMaterial:
                code = 0;
                return true;
            case PerspectiveSafeRelationshipKindV1.Equip:
                code = 1;
                return true;
            case PerspectiveSafeRelationshipKindV1.Target:
                code = 2;
                return true;
            default:
                code = 0;
                return false;
        }
    }

    private static bool TryMapVisibleEventKind(
        PerspectiveSafeVisibleEventKindV1 value,
        out byte code)
    {
        code = (byte)value;
        return code <= 22;
    }

    private static bool TryMapOptionalZone(
        PerspectiveSafeSemanticZoneV1? value,
        out byte? code)
    {
        code = null;
        if (!value.HasValue)
        {
            return true;
        }

        if (!TryMapZone(value.Value, out byte mapped))
        {
            return false;
        }

        code = mapped;
        return true;
    }

    private static bool TryValidateOptionalNativeZone(
        byte? value,
        out byte? normalized)
    {
        normalized = value;
        return !value.HasValue || value.Value <= 10;
    }

    private static I6C6FullComparisonResultV1 CompareNormalized(
        I6C6NormalizedPublicSafeStateV1 left,
        I6C6NormalizedPublicSafeStateV1 right)
    {
        I6C6FullComparisonResultV1 result = CompareGlobals(left.Globals, right.Globals);
        if (!result.IsSuccess)
        {
            return result;
        }

        result = CompareZones(left.Zones, right.Zones);
        if (!result.IsSuccess)
        {
            return result;
        }

        result = CompareEntities(left.Entities, right.Entities);
        if (!result.IsSuccess)
        {
            return result;
        }

        result = CompareRelationships(left.Relationships, right.Relationships);
        if (!result.IsSuccess)
        {
            return result;
        }

        result = CompareChains(left.Chain, right.Chain);
        if (!result.IsSuccess)
        {
            return result;
        }

        result = CompareVisibleEvents(left.VisibleEvents, right.VisibleEvents);
        if (!result.IsSuccess)
        {
            return result;
        }

        return CompareMatchContexts(left.MatchContext, right.MatchContext);
    }

    private static I6C6FullComparisonResultV1 CompareGlobals(
        I6C6NormalizedGlobalsV1 left,
        I6C6NormalizedGlobalsV1 right)
    {
        if (left.DuelFlags != right.DuelFlags)
        {
            return FullFailure(
                I6C6FullComparisonErrorCodeV1.SemanticMismatch,
                "globals.duel_flags");
        }

        I6C6FullComparisonResultV1 result = CompareList(
            left.LifePoints,
            right.LifePoints,
            "globals.life_points",
            I6C6FullComparisonErrorCodeV1.CardinalityMismatch);
        if (!result.IsSuccess)
        {
            return result;
        }

        result = CompareOptional(left.TurnPlayer, right.TurnPlayer, "globals.turn_player");
        if (!result.IsSuccess)
        {
            return result;
        }

        result = CompareOptional(left.TurnCount, right.TurnCount, "globals.turn_count");
        if (!result.IsSuccess)
        {
            return result;
        }

        result = CompareOptional(left.Phase, right.Phase, "globals.phase");
        if (!result.IsSuccess)
        {
            return result;
        }

        if (left.ChainLength != right.ChainLength)
        {
            return FullFailure(
                I6C6FullComparisonErrorCodeV1.SemanticMismatch,
                "globals.chain_length");
        }

        result = CompareOptional(left.Winner, right.Winner, "globals.winner");
        if (!result.IsSuccess)
        {
            return result;
        }

        result = CompareOptional(left.WinReason, right.WinReason, "globals.win_reason");
        if (!result.IsSuccess)
        {
            return result;
        }

        return left.Terminal == right.Terminal
            ? FullSuccess()
            : FullFailure(
                I6C6FullComparisonErrorCodeV1.SemanticMismatch,
                "globals.terminal");
    }

    private static I6C6FullComparisonResultV1 CompareZones(
        IReadOnlyList<I6C6NormalizedZoneV1> left,
        IReadOnlyList<I6C6NormalizedZoneV1> right)
    {
        if (!TryCompareCount(left, right, "zones", out I6C6FullComparisonResultV1 result))
        {
            return result;
        }

        for (int index = 0; index < left.Count; index++)
        {
            I6C6NormalizedZoneV1 first = left[index];
            I6C6NormalizedZoneV1 second = right[index];
            result = CompareScalar(first.Player, second.Player, $"zones[{index}].player");
            if (!result.IsSuccess) return result;
            result = CompareScalar(first.Kind, second.Kind, $"zones[{index}].kind");
            if (!result.IsSuccess) return result;
            result = CompareScalar(first.TotalCount, second.TotalCount, $"zones[{index}].total_count");
            if (!result.IsSuccess) return result;
            result = CompareScalar(first.PublicIdentityCount, second.PublicIdentityCount,
                $"zones[{index}].public_identity_count");
            if (!result.IsSuccess) return result;
            result = CompareScalar(first.HiddenCount, second.HiddenCount, $"zones[{index}].hidden_count");
            if (!result.IsSuccess) return result;
            result = CompareScalar(first.PlayerObservableOrder, second.PlayerObservableOrder,
                $"zones[{index}].player_observable_order");
            if (!result.IsSuccess) return result;
        }

        return FullSuccess();
    }

    private static I6C6FullComparisonResultV1 CompareEntities(
        IReadOnlyList<I6C6NormalizedEntityV1> left,
        IReadOnlyList<I6C6NormalizedEntityV1> right)
    {
        if (!TryCompareCount(left, right, "entities", out I6C6FullComparisonResultV1 result))
        {
            return result;
        }

        for (int index = 0; index < left.Count; index++)
        {
            I6C6NormalizedEntityV1 first = left[index];
            I6C6NormalizedEntityV1 second = right[index];
            result = CompareScalar(first.Locator, second.Locator, $"entities[{index}].locator");
            if (!result.IsSuccess) return result;
            result = CompareScalar(first.IdentityKnown, second.IdentityKnown,
                $"entities[{index}].identity_known");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.Passcode, second.Passcode, $"entities[{index}].passcode");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.Owner, second.Owner, $"entities[{index}].owner");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.Controller, second.Controller, $"entities[{index}].controller");
            if (!result.IsSuccess) return result;
            result = CompareScalar(first.Zone, second.Zone, $"entities[{index}].zone");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.Sequence, second.Sequence, $"entities[{index}].sequence");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.OverlaySequence, second.OverlaySequence,
                $"entities[{index}].overlay_sequence");
            if (!result.IsSuccess) return result;
            result = CompareScalar(first.Position, second.Position, $"entities[{index}].position");
            if (!result.IsSuccess) return result;
            result = CompareScalar(first.FaceUp, second.FaceUp, $"entities[{index}].face_up");
            if (!result.IsSuccess) return result;
            result = CompareScalar(first.FaceDown, second.FaceDown, $"entities[{index}].face_down");
            if (!result.IsSuccess) return result;
            result = CompareProperties(first.Printed, second.Printed, $"entities[{index}].printed");
            if (!result.IsSuccess) return result;
            result = CompareProperties(first.Current, second.Current, $"entities[{index}].current");
            if (!result.IsSuccess) return result;
        }

        return FullSuccess();
    }

    private static I6C6FullComparisonResultV1 CompareProperties(
        I6C6NormalizedCardPropertiesV1? left,
        I6C6NormalizedCardPropertiesV1? right,
        string path)
    {
        if (left is null || right is null)
        {
            return left is null && right is null
                ? FullSuccess()
                : FullFailure(
                    I6C6FullComparisonErrorCodeV1.OptionalPresenceMismatch,
                    path);
        }

        I6C6FullComparisonResultV1 result = CompareOptional(left.Type, right.Type, path + ".type");
        if (!result.IsSuccess) return result;
        result = CompareOptional(left.Attribute, right.Attribute, path + ".attribute");
        if (!result.IsSuccess) return result;
        result = CompareOptional(left.Race, right.Race, path + ".race");
        if (!result.IsSuccess) return result;
        result = CompareOptional(left.Attack, right.Attack, path + ".attack");
        if (!result.IsSuccess) return result;
        result = CompareOptional(left.Defense, right.Defense, path + ".defense");
        if (!result.IsSuccess) return result;
        result = CompareOptional(left.BaseAttack, right.BaseAttack, path + ".base_attack");
        if (!result.IsSuccess) return result;
        result = CompareOptional(left.BaseDefense, right.BaseDefense, path + ".base_defense");
        if (!result.IsSuccess) return result;
        result = CompareOptional(left.Level, right.Level, path + ".level");
        if (!result.IsSuccess) return result;
        result = CompareOptional(left.Rank, right.Rank, path + ".rank");
        if (!result.IsSuccess) return result;
        result = CompareOptional(left.LinkRating, right.LinkRating, path + ".link_rating");
        if (!result.IsSuccess) return result;
        result = CompareList(left.LinkMarkers, right.LinkMarkers, path + ".link_markers",
            I6C6FullComparisonErrorCodeV1.CardinalityMismatch);
        if (!result.IsSuccess) return result;
        result = CompareOptional(left.LeftScale, right.LeftScale, path + ".left_scale");
        if (!result.IsSuccess) return result;
        result = CompareOptional(left.RightScale, right.RightScale, path + ".right_scale");
        if (!result.IsSuccess) return result;
        result = CompareOptional(left.StatusFlags, right.StatusFlags, path + ".status_flags");
        if (!result.IsSuccess) return result;
        return CompareCounters(left.Counters, right.Counters, path + ".counters");
    }

    private static I6C6FullComparisonResultV1 CompareRelationships(
        IReadOnlyList<I6C6NormalizedRelationshipV1> left,
        IReadOnlyList<I6C6NormalizedRelationshipV1> right)
    {
        if (!TryCompareCount(left, right, "relationships", out I6C6FullComparisonResultV1 result))
        {
            return result;
        }

        for (int index = 0; index < left.Count; index++)
        {
            result = CompareScalar(left[index].Kind, right[index].Kind,
                $"relationships[{index}].kind");
            if (!result.IsSuccess) return result;
            result = CompareScalar(left[index].Source, right[index].Source,
                $"relationships[{index}].source");
            if (!result.IsSuccess) return result;
            result = CompareScalar(left[index].Target, right[index].Target,
                $"relationships[{index}].target");
            if (!result.IsSuccess) return result;
        }

        return FullSuccess();
    }

    private static I6C6FullComparisonResultV1 CompareChains(
        I6C6NormalizedChainV1 left,
        I6C6NormalizedChainV1 right)
    {
        if (left.Length != right.Length)
        {
            return FullFailure(
                I6C6FullComparisonErrorCodeV1.SemanticMismatch,
                "chain.length");
        }

        if (!TryCompareCount(left.Links, right.Links, "chain.links",
                out I6C6FullComparisonResultV1 result))
        {
            return result;
        }

        for (int index = 0; index < left.Links.Count; index++)
        {
            I6C6NormalizedChainLinkV1 first = left.Links[index];
            I6C6NormalizedChainLinkV1 second = right.Links[index];
            result = CompareScalar(first.Index, second.Index, $"chain.links[{index}].index");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.ActivatingPlayer, second.ActivatingPlayer,
                $"chain.links[{index}].activating_player");
            if (!result.IsSuccess) return result;
            result = CompareOptionalString(first.Source, second.Source,
                $"chain.links[{index}].source");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.ActivationZone, second.ActivationZone,
                $"chain.links[{index}].activation_zone");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.EffectDescription, second.EffectDescription,
                $"chain.links[{index}].effect_description");
            if (!result.IsSuccess) return result;
            result = CompareList(first.Targets, second.Targets,
                $"chain.links[{index}].targets",
                I6C6FullComparisonErrorCodeV1.CardinalityMismatch);
            if (!result.IsSuccess) return result;
        }

        return FullSuccess();
    }

    private static I6C6FullComparisonResultV1 CompareVisibleEvents(
        IReadOnlyList<I6C6NormalizedVisibleEventV1> left,
        IReadOnlyList<I6C6NormalizedVisibleEventV1> right)
    {
        if (!TryCompareCount(left, right, "visible_events",
                out I6C6FullComparisonResultV1 result))
        {
            return result;
        }

        for (int index = 0; index < left.Count; index++)
        {
            I6C6NormalizedVisibleEventV1 first = left[index];
            I6C6NormalizedVisibleEventV1 second = right[index];
            result = CompareScalar(first.EventIndex, second.EventIndex,
                $"visible_events[{index}].event_index");
            if (!result.IsSuccess) return result;
            result = CompareScalar(first.Kind, second.Kind, $"visible_events[{index}].kind");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.Player, second.Player, $"visible_events[{index}].player");
            if (!result.IsSuccess) return result;
            result = CompareOptionalString(first.EntityLocator, second.EntityLocator,
                $"visible_events[{index}].entity");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.PublicPasscode, second.PublicPasscode,
                $"visible_events[{index}].public_passcode");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.FromZone, second.FromZone,
                $"visible_events[{index}].from_zone");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.ToZone, second.ToZone,
                $"visible_events[{index}].to_zone");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.Count, second.Count, $"visible_events[{index}].count");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.Amount, second.Amount, $"visible_events[{index}].amount");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.CounterType, second.CounterType,
                $"visible_events[{index}].counter_type");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.Phase, second.Phase, $"visible_events[{index}].phase");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.Winner, second.Winner, $"visible_events[{index}].winner");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.WinReason, second.WinReason,
                $"visible_events[{index}].win_reason");
            if (!result.IsSuccess) return result;
            result = CompareOptional(first.EffectDescription, second.EffectDescription,
                $"visible_events[{index}].effect_description");
            if (!result.IsSuccess) return result;
            result = CompareList(first.Targets, second.Targets,
                $"visible_events[{index}].targets",
                I6C6FullComparisonErrorCodeV1.CardinalityMismatch);
            if (!result.IsSuccess) return result;
        }

        return FullSuccess();
    }

    private static I6C6FullComparisonResultV1 CompareMatchContexts(
        I6C6NormalizedMatchContextV1 left,
        I6C6NormalizedMatchContextV1 right)
    {
        I6C6FullComparisonResultV1 result = CompareScalar(
            left.PerspectivePlayer,
            right.PerspectivePlayer,
            "match_context.perspective_player");
        if (!result.IsSuccess) return result;
        result = CompareScalar(left.DuelFlags, right.DuelFlags, "match_context.duel_flags");
        if (!result.IsSuccess) return result;
        result = CompareScalar(left.OwnDecklistKnown, right.OwnDecklistKnown,
            "match_context.own_decklist_known");
        if (!result.IsSuccess) return result;
        result = CompareScalar(left.OpponentDecklistKnown, right.OpponentDecklistKnown,
            "match_context.opponent_decklist_known");
        if (!result.IsSuccess) return result;
        result = CompareDecks(left.OwnDeck, right.OwnDeck, "match_context.own_deck");
        if (!result.IsSuccess) return result;
        return CompareDecks(left.OpponentDeck, right.OpponentDeck, "match_context.opponent_deck");
    }

    private static I6C6FullComparisonResultV1 CompareDecks(
        I6C6NormalizedDeckV1 left,
        I6C6NormalizedDeckV1 right,
        string path)
    {
        I6C6FullComparisonResultV1 result = CompareScalar(left.Known, right.Known, path + ".known");
        if (!result.IsSuccess) return result;
        result = CompareList(left.MainDeck, right.MainDeck, path + ".main_deck",
            I6C6FullComparisonErrorCodeV1.CardinalityMismatch);
        if (!result.IsSuccess) return result;
        return CompareList(left.ExtraDeck, right.ExtraDeck, path + ".extra_deck",
            I6C6FullComparisonErrorCodeV1.CardinalityMismatch);
    }

    private static I6C6FullComparisonResultV1 CompareCounters(
        IReadOnlyList<I6C6NormalizedCounterV1> left,
        IReadOnlyList<I6C6NormalizedCounterV1> right,
        string path)
    {
        if (!TryCompareCount(left, right, path, out I6C6FullComparisonResultV1 result))
        {
            return result;
        }

        for (int index = 0; index < left.Count; index++)
        {
            result = CompareScalar(left[index].Type, right[index].Type,
                $"{path}[{index}].type");
            if (!result.IsSuccess) return result;
            result = CompareScalar(left[index].Count, right[index].Count,
                $"{path}[{index}].count");
            if (!result.IsSuccess) return result;
        }

        return FullSuccess();
    }

    private static I6C6FullComparisonResultV1 CompareScalar<T>(
        T left,
        T right,
        string path)
    {
        return EqualityComparer<T>.Default.Equals(left, right)
            ? FullSuccess()
            : FullFailure(I6C6FullComparisonErrorCodeV1.SemanticMismatch, path);
    }

    private static I6C6FullComparisonResultV1 CompareOptional<T>(
        T? left,
        T? right,
        string path)
        where T : struct
    {
        if (left.HasValue != right.HasValue)
        {
            return FullFailure(
                I6C6FullComparisonErrorCodeV1.OptionalPresenceMismatch,
                path);
        }

        return !left.HasValue || EqualityComparer<T>.Default.Equals(left.Value, right!.Value)
            ? FullSuccess()
            : FullFailure(I6C6FullComparisonErrorCodeV1.SemanticMismatch, path);
    }

    private static I6C6FullComparisonResultV1 CompareOptionalString(
        string? left,
        string? right,
        string path)
    {
        if ((left is null) != (right is null))
        {
            return FullFailure(
                I6C6FullComparisonErrorCodeV1.OptionalPresenceMismatch,
                path);
        }

        return string.Equals(left, right, StringComparison.Ordinal)
            ? FullSuccess()
            : FullFailure(I6C6FullComparisonErrorCodeV1.SemanticMismatch, path);
    }

    private static I6C6FullComparisonResultV1 CompareList<T>(
        IReadOnlyList<T> left,
        IReadOnlyList<T> right,
        string path,
        I6C6FullComparisonErrorCodeV1 cardinalityError)
    {
        if (!TryCompareCount(left, right, path, out I6C6FullComparisonResultV1 result,
                cardinalityError))
        {
            return result;
        }

        for (int index = 0; index < left.Count; index++)
        {
            result = CompareScalar(left[index], right[index], $"{path}[{index}]");
            if (!result.IsSuccess) return result;
        }

        return FullSuccess();
    }

    private static bool TryCompareCount<T>(
        IReadOnlyList<T> left,
        IReadOnlyList<T> right,
        string path,
        out I6C6FullComparisonResultV1 result,
        I6C6FullComparisonErrorCodeV1 error = I6C6FullComparisonErrorCodeV1.CardinalityMismatch)
    {
        if (left.Count != right.Count)
        {
            result = FullFailure(error, path);
            return false;
        }

        result = default;
        return true;
    }

    private static int CompareZones(
        I6C6NormalizedZoneV1 left,
        I6C6NormalizedZoneV1 right)
    {
        int result = left.Player.CompareTo(right.Player);
        if (result != 0) return result;
        result = left.Kind.CompareTo(right.Kind);
        if (result != 0) return result;
        result = left.TotalCount.CompareTo(right.TotalCount);
        if (result != 0) return result;
        result = left.PublicIdentityCount.CompareTo(right.PublicIdentityCount);
        if (result != 0) return result;
        result = left.HiddenCount.CompareTo(right.HiddenCount);
        if (result != 0) return result;
        return left.PlayerObservableOrder.CompareTo(right.PlayerObservableOrder);
    }

    private static int CompareEntities(
        I6C6NormalizedEntityV1 left,
        I6C6NormalizedEntityV1 right) =>
        string.CompareOrdinal(left.Locator, right.Locator);

    private static int CompareRelationships(
        I6C6NormalizedRelationshipV1 left,
        I6C6NormalizedRelationshipV1 right)
    {
        int result = left.Kind.CompareTo(right.Kind);
        if (result != 0) return result;
        result = string.CompareOrdinal(left.Source, right.Source);
        return result != 0 ? result : string.CompareOrdinal(left.Target, right.Target);
    }

    private static int CompareCounters(
        I6C6NormalizedCounterV1 left,
        I6C6NormalizedCounterV1 right)
    {
        int result = left.Type.CompareTo(right.Type);
        return result != 0 ? result : left.Count.CompareTo(right.Count);
    }

    private static int CompareVisibleEvents(
        I6C6NormalizedVisibleEventV1 left,
        I6C6NormalizedVisibleEventV1 right) =>
        left.EventIndex.CompareTo(right.EventIndex);

    private static bool HasDuplicateEventIndex(
        IReadOnlyList<I6C6NormalizedVisibleEventV1> values)
    {
        for (int index = 1; index < values.Count; index++)
        {
            if (values[index - 1].EventIndex == values[index].EventIndex)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasDuplicateEntityLocator(
        IReadOnlyList<I6C6NormalizedEntityV1> values)
    {
        for (int index = 1; index < values.Count; index++)
        {
            if (string.Equals(
                    values[index - 1].Locator,
                    values[index].Locator,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static I6C6FullComparisonResultV1 FullSuccess() =>
        new(true, I6C6FullComparisonErrorCodeV1.None, null);

    private static I6C6FullComparisonResultV1 FullFailure(
        I6C6FullComparisonErrorCodeV1 errorCode,
        string path) =>
        new(false, errorCode, path);

    private static bool TryValidatePairing(
        I6C6ScenarioPairingV1 pairing,
        out I6C6ComparisonResultV1 failure)
    {
        if (pairing is null ||
            pairing.NativePerspectivePlayer > 1 ||
            pairing.IgnisPerspectivePlayer > 1 ||
            pairing.NativePerspectivePlayer != pairing.IgnisPerspectivePlayer)
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
            return false;
        }

        if (pairing.StartingPlayer > 1)
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
            return false;
        }

        if (pairing.SeedWords is null || pairing.SeedWords.Count != 4)
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
            return false;
        }

        if (string.IsNullOrEmpty(pairing.NativeSetupDescriptorId))
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
            return false;
        }

        if (!string.Equals(
                pairing.MessageFamilyEnvelopeId,
                SupportedMessageFamilyEnvelopeId,
                StringComparison.Ordinal) ||
            pairing.StateSnapshotSelector != 1)
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
            return false;
        }

        failure = default;
        return true;
    }

    private static bool TryValidateState(
        I6C6PublicSafeStateV1 state,
        out I6C6ComparisonResultV1 failure)
    {
        if (!IsValidOptionalPlayer(state.TurnPlayer))
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.OptionalPresenceMismatch);
            return false;
        }

        if (!IsValidSemanticZone(state.EnumSample.SemanticZone) ||
            !IsValidPosition(state.EnumSample.Position) ||
            !IsValidRelationshipKind(state.EnumSample.RelationshipKind) ||
            !IsValidVisibleEventKind(state.EnumSample.VisibleEventKind))
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.UnknownEnum);
            return false;
        }

        if (state.Entities is null)
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
            return false;
        }

        List<string> locators = new(state.Entities.Count);
        foreach (I6C6PublicEntityV1 entity in state.Entities)
        {
            if (!PublicSemanticLocatorV1.TryParse(entity.Locator, out _))
            {
                failure = Failure(
                    I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
                return false;
            }

            if (!entity.IdentityKnown &&
                (entity.PublicPasscode.HasValue ||
                 entity.HasPrintedProperties ||
                 entity.HasCurrentProperties))
            {
                failure = Failure(
                    I6C6ComparisonErrorCodeV1.HiddenIdentityData);
                return false;
            }

            if (entity.IdentityKnown &&
                (!entity.PublicPasscode.HasValue || entity.PublicPasscode.Value == 0))
            {
                failure = Failure(
                    I6C6ComparisonErrorCodeV1.HiddenIdentityData);
                return false;
            }

            locators.Add(entity.Locator);
        }

        locators.Sort(StringComparer.Ordinal);
        for (int index = 1; index < locators.Count; index++)
        {
            if (string.Equals(
                    locators[index - 1],
                    locators[index],
                    StringComparison.Ordinal))
            {
                failure = Failure(
                    I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
                return false;
            }
        }

        failure = default;
        return true;
    }

    private static string Normalize(I6C6PublicSafeStateV1 state)
    {
        IEnumerable<I6C6PublicEntityV1> entities = state.Entities
            .OrderBy(entity => entity.Locator, StringComparer.Ordinal);

        return string.Join(
            "|",
            "PTA=" + EncodeOptional(state.PlayerToAct),
            "TURN=" + EncodeOptional(state.TurnPlayer),
            "ENUM=" + string.Join(
                ",",
                state.EnumSample.SemanticZone.ToString(CultureInfo.InvariantCulture),
                state.EnumSample.Position.ToString(CultureInfo.InvariantCulture),
                state.EnumSample.RelationshipKind.ToString(CultureInfo.InvariantCulture),
                state.EnumSample.VisibleEventKind.ToString(CultureInfo.InvariantCulture)),
            "ENTITIES=" + string.Join(
                ";",
                entities.Select(EncodeEntity)));
    }

    private static string EncodeOptional(I6C6OptionalByteV1 value) =>
        value.IsPresent
            ? "PRESENT(" + value.Value.ToString(CultureInfo.InvariantCulture) + ")"
            : "ABSENT";

    private static string EncodeEntity(I6C6PublicEntityV1 entity) =>
        entity.IdentityKnown
            ? entity.Locator + ":KNOWN:" + entity.PublicPasscode!.Value
                .ToString(CultureInfo.InvariantCulture)
            : entity.Locator + ":UNKNOWN";

    private static bool IsValidOptionalPlayer(I6C6OptionalByteV1 value) =>
        !value.IsPresent
            ? value.Value == 0
            : value.Value <= 1;

    private static bool IsValidOptionalPlayer(byte? value) =>
        !value.HasValue || value.Value <= 1;

    private static bool IsValidSemanticZone(byte value) => value <= 10;

    private static bool IsValidPosition(byte value) =>
        value is 0 or 1 or 2 or 4 or 8;

    private static bool IsValidRelationshipKind(byte value) => value <= 2;

    private static bool IsValidVisibleEventKind(byte value) => value <= 22;

    private static I6C6ComparisonResultV1 Success() =>
        new(true, I6C6ComparisonErrorCodeV1.None);

    private static I6C6ComparisonResultV1 Failure(
        I6C6ComparisonErrorCodeV1 errorCode) =>
        new(false, errorCode);
}

internal enum I6C6FullComparisonErrorCodeV1 : byte
{
    None = 0,
    InvalidInput = 1,
    BlockedPendingI6D = 2,
    UnknownEnum = 3,
    InvalidLocator = 4,
    DuplicateLocator = 5,
    DuplicateEventIndex = 6,
    HiddenIdentityData = 7,
    OptionalPresenceMismatch = 8,
    CardinalityMismatch = 9,
    InvalidOrdering = 10,
    SemanticMismatch = 11,
    UnprovenScenarioPairing = 12
}

internal readonly record struct I6C6FullComparisonResultV1(
    bool IsSuccess,
    I6C6FullComparisonErrorCodeV1 ErrorCode,
    string? PublicSafeFieldPath);

internal sealed record I6C6NativePublicSafeStateRowV1(
    I6C6NativeGlobalsV1 Globals,
    IReadOnlyList<I6C6NativeZoneV1> Zones,
    IReadOnlyList<I6C6NativeEntityV1> Entities,
    IReadOnlyList<I6C6NativeRelationshipV1> Relationships,
    I6C6NativeChainV1 Chain,
    IReadOnlyList<I6C6NativeVisibleEventV1> VisibleEvents,
    I6C6NativeMatchContextV1 MatchContext);

internal sealed record I6C6NativeGlobalsV1(
    ulong DuelFlags,
    IReadOnlyList<uint> LifePoints,
    byte? PlayerToAct,
    byte? TurnPlayer,
    uint? TurnCount,
    uint? Phase,
    uint ChainLength,
    byte? Winner,
    byte? WinReason,
    bool Terminal);

internal sealed record I6C6NativeZoneV1(
    byte Player,
    byte Kind,
    uint TotalCount,
    uint PublicIdentityCount,
    uint HiddenCount,
    bool PlayerObservableOrder);

internal sealed record I6C6NativeCardPropertiesV1(
    uint? Type,
    uint? Attribute,
    ulong? Race,
    int? Attack,
    int? Defense,
    int? BaseAttack,
    int? BaseDefense,
    uint? Level,
    uint? Rank,
    uint? LinkRating,
    IReadOnlyList<byte> LinkMarkers,
    uint? LeftScale,
    uint? RightScale,
    uint? StatusFlags,
    IReadOnlyList<I6C6NativeCounterV1> Counters);

internal readonly record struct I6C6NativeCounterV1(uint Type, uint Count);

internal sealed record I6C6NativeEntityV1(
    string Locator,
    bool IdentityKnown,
    uint? Passcode,
    byte? Owner,
    byte? Controller,
    byte Zone,
    uint? Sequence,
    uint? OverlaySequence,
    byte Position,
    bool FaceUp,
    bool FaceDown,
    I6C6NativeCardPropertiesV1? Printed,
    I6C6NativeCardPropertiesV1? Current);

internal sealed record I6C6NativeRelationshipV1(
    byte Kind,
    string Source,
    string Target);

internal sealed record I6C6NativeChainV1(
    uint Length,
    IReadOnlyList<I6C6NativeChainLinkV1> Links);

internal sealed record I6C6NativeChainLinkV1(
    uint Index,
    byte? ActivatingPlayer,
    string? Source,
    byte? ActivationZone,
    ulong? EffectDescription,
    IReadOnlyList<string> Targets);

internal sealed record I6C6NativeVisibleEventV1(
    ulong EventIndex,
    byte Kind,
    byte? Player = null,
    string? EntityLocator = null,
    uint? PublicPasscode = null,
    byte? FromZone = null,
    byte? ToZone = null,
    uint? Count = null,
    int? Amount = null,
    uint? CounterType = null,
    uint? Phase = null,
    byte? Winner = null,
    byte? WinReason = null,
    ulong? EffectDescription = null,
    IReadOnlyList<string>? Targets = null);

internal sealed record I6C6NativeMatchContextV1(
    byte PerspectivePlayer,
    ulong DuelFlags,
    bool OwnDecklistKnown,
    bool OpponentDecklistKnown,
    IReadOnlyList<uint> OwnMainDeck,
    IReadOnlyList<uint> OwnExtraDeck,
    IReadOnlyList<uint> OpponentMainDeck,
    IReadOnlyList<uint> OpponentExtraDeck);

internal sealed record I6C6NormalizedPublicSafeStateV1(
    I6C6NormalizedGlobalsV1 Globals,
    IReadOnlyList<I6C6NormalizedZoneV1> Zones,
    IReadOnlyList<I6C6NormalizedEntityV1> Entities,
    IReadOnlyList<I6C6NormalizedRelationshipV1> Relationships,
    I6C6NormalizedChainV1 Chain,
    IReadOnlyList<I6C6NormalizedVisibleEventV1> VisibleEvents,
    I6C6NormalizedMatchContextV1 MatchContext);

internal sealed record I6C6NormalizedGlobalsV1(
    ulong DuelFlags,
    IReadOnlyList<uint> LifePoints,
    byte? PlayerToAct,
    byte? TurnPlayer,
    uint? TurnCount,
    uint? Phase,
    uint ChainLength,
    byte? Winner,
    byte? WinReason,
    bool Terminal);

internal readonly record struct I6C6NormalizedZoneV1(
    byte Player,
    byte Kind,
    uint TotalCount,
    uint PublicIdentityCount,
    uint HiddenCount,
    bool PlayerObservableOrder);

internal sealed record I6C6NormalizedCardPropertiesV1(
    uint? Type,
    uint? Attribute,
    ulong? Race,
    int? Attack,
    int? Defense,
    int? BaseAttack,
    int? BaseDefense,
    uint? Level,
    uint? Rank,
    uint? LinkRating,
    IReadOnlyList<byte> LinkMarkers,
    uint? LeftScale,
    uint? RightScale,
    uint? StatusFlags,
    IReadOnlyList<I6C6NormalizedCounterV1> Counters);

internal readonly record struct I6C6NormalizedCounterV1(uint Type, uint Count);

internal sealed record I6C6NormalizedEntityV1(
    string Locator,
    bool IdentityKnown,
    uint? Passcode,
    byte? Owner,
    byte? Controller,
    byte Zone,
    uint? Sequence,
    uint? OverlaySequence,
    byte Position,
    bool FaceUp,
    bool FaceDown,
    I6C6NormalizedCardPropertiesV1? Printed,
    I6C6NormalizedCardPropertiesV1? Current);

internal readonly record struct I6C6NormalizedRelationshipV1(
    byte Kind,
    string Source,
    string Target);

internal sealed record I6C6NormalizedChainV1(
    uint Length,
    IReadOnlyList<I6C6NormalizedChainLinkV1> Links);

internal sealed record I6C6NormalizedChainLinkV1(
    uint Index,
    byte? ActivatingPlayer,
    string? Source,
    byte? ActivationZone,
    ulong? EffectDescription,
    IReadOnlyList<string> Targets);

internal sealed record I6C6NormalizedVisibleEventV1(
    ulong EventIndex,
    byte Kind,
    byte? Player,
    string? EntityLocator,
    uint? PublicPasscode,
    byte? FromZone,
    byte? ToZone,
    uint? Count,
    int? Amount,
    uint? CounterType,
    uint? Phase,
    byte? Winner,
    byte? WinReason,
    ulong? EffectDescription,
    IReadOnlyList<string> Targets);

internal sealed record I6C6NormalizedMatchContextV1(
    byte PerspectivePlayer,
    ulong DuelFlags,
    bool OwnDecklistKnown,
    bool OpponentDecklistKnown,
    I6C6NormalizedDeckV1 OwnDeck,
    I6C6NormalizedDeckV1 OpponentDeck);

internal sealed record I6C6NormalizedDeckV1(
    bool Known,
    IReadOnlyList<uint> MainDeck,
    IReadOnlyList<uint> ExtraDeck);
