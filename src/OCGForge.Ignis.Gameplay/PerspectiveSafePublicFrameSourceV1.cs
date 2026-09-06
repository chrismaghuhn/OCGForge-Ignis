namespace OCGForge.Ignis.Gameplay;

/// <summary>
/// Validates the I6C1 source shape only. It does not extract runtime facts,
/// encode OCGForge bytes, or establish source provenance.
/// </summary>
public static class PerspectiveSafePublicFrameSourceV1
{
    /// <summary>
    /// Returns one immutable structurally valid container or one stable error.
    /// </summary>
    public static PerspectiveSafeFrameSourceResultV1 TryCreate(
        PerspectiveSafeFrameSourceInputV1? input)
    {
        if (input is null)
        {
            return Failure(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidInput,
                PerspectiveSafeSourceSectionV1.Input);
        }

        if (input.Globals is null)
        {
            return Failure(
                PerspectiveSafeFrameSourceErrorCodeV1.MissingGlobals,
                PerspectiveSafeSourceSectionV1.Globals);
        }

        if (input.Zones is null)
        {
            return Failure(
                PerspectiveSafeFrameSourceErrorCodeV1.MissingZones,
                PerspectiveSafeSourceSectionV1.Zones);
        }

        if (input.Entities is null)
        {
            return Failure(
                PerspectiveSafeFrameSourceErrorCodeV1.MissingEntities,
                PerspectiveSafeSourceSectionV1.Entities);
        }

        if (input.Relationships is null)
        {
            return Failure(
                PerspectiveSafeFrameSourceErrorCodeV1.MissingRelationships,
                PerspectiveSafeSourceSectionV1.Relationships);
        }

        if (input.Chain is null)
        {
            return Failure(
                PerspectiveSafeFrameSourceErrorCodeV1.MissingChain,
                PerspectiveSafeSourceSectionV1.Chain);
        }

        if (input.VisibleEvents is null)
        {
            return Failure(
                PerspectiveSafeFrameSourceErrorCodeV1.MissingVisibleEvents,
                PerspectiveSafeSourceSectionV1.VisibleEvents);
        }

        if (input.MatchContext is null)
        {
            return Failure(
                PerspectiveSafeFrameSourceErrorCodeV1.MissingMatchContext,
                PerspectiveSafeSourceSectionV1.MatchContext);
        }

        if (!ValidateGlobals(input.Globals, out PerspectiveSafeFrameSourceErrorV1 error) ||
            !ValidateZones(input.Zones, out error) ||
            !ValidateEntities(input.Entities, out error) ||
            !ValidateRelationships(input.Relationships, out error) ||
            !ValidateChain(input.Chain, out error) ||
            !ValidateEvents(input.VisibleEvents, out error) ||
            !ValidateMatchContext(input.MatchContext, out error))
        {
            return PerspectiveSafeFrameSourceResultV1.Failure(error);
        }

        if (input.Globals.LifePoints.Count != 2)
        {
            return Failure(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidLifePointCardinality,
                PerspectiveSafeSourceSectionV1.Globals);
        }

        if (input.Globals.ChainLength != input.Chain.Length)
        {
            return Failure(
                PerspectiveSafeFrameSourceErrorCodeV1.CrossSectionMismatch,
                PerspectiveSafeSourceSectionV1.Chain);
        }

        if (input.Globals.DuelFlags != input.MatchContext.DuelFlags)
        {
            return Failure(
                PerspectiveSafeFrameSourceErrorCodeV1.CrossSectionMismatch,
                PerspectiveSafeSourceSectionV1.MatchContext);
        }

        return PerspectiveSafeFrameSourceResultV1.Success(
            new PerspectiveSafeFrameV1(input));
    }

    /// <summary>
    /// Extracts the I6C2-owned, perspective-safe facts from a committed Mirror
    /// snapshot. The result is deliberately partial until later I6C slices
    /// close their dependencies.
    /// </summary>
    public static PerspectiveSafeI6C2SourceResultV1 TryCreateI6C2(
        PerspectiveStateMirrorV1? mirror)
    {
        if (mirror is null)
        {
            return PerspectiveSafeI6C2SourceResultV1.Failure(
                Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.MissingMirror,
                    PerspectiveSafeSourceSectionV1.Input));
        }

        return TryCreateI6C2(mirror.Snapshot);
    }

    private static PerspectiveSafeI6C2SourceResultV1 TryCreateI6C2(
        MirrorSnapshotV1 snapshot)
    {
        if (!TryCreateI6C2Globals(
                snapshot,
                out PerspectiveSafeI6C2GlobalsV1? globals,
                out PerspectiveSafeFrameSourceErrorV1 error))
        {
            return PerspectiveSafeI6C2SourceResultV1.Failure(error);
        }

        if (!TryCreateI6C2Zones(
                snapshot,
                out PerspectiveSafeZoneV1[] zones,
                out error))
        {
            return PerspectiveSafeI6C2SourceResultV1.Failure(error);
        }

        if (!TryCreateI6C2Entities(
                snapshot,
                out PerspectiveSafeEntityV1[] entities,
                out bool entityIdentityBlocked,
                out bool entityLocatorBlocked,
                out bool entityOwnerBlocked,
                out error))
        {
            return PerspectiveSafeI6C2SourceResultV1.Failure(error);
        }

        PerspectiveSafeI6C2ConstituentStatusV1[] statuses =
            CreateI6C2Statuses(
                entityIdentityBlocked,
                entityLocatorBlocked,
                entityOwnerBlocked);
        PerspectiveSafeI6C2StateSourceV1 source =
            new(globals!, zones, entities, statuses);
        return PerspectiveSafeI6C2SourceResultV1.Success(source);
    }

    /// <summary>
    /// Extracts the I6C3 current relation, overlay, and chain facts from one
    /// committed Mirror snapshot. The returned source remains partial when a
    /// later I6 slice owns a required configuration seam.
    /// </summary>
    public static PerspectiveSafeI6C3SourceResultV1 TryCreateI6C3(
        PerspectiveStateMirrorV1? mirror)
    {
        if (mirror is null)
        {
            return PerspectiveSafeI6C3SourceResultV1.Failure(
                Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.MissingMirror,
                    PerspectiveSafeSourceSectionV1.Input));
        }

        MirrorSnapshotV1 snapshot = mirror.Snapshot;
        PerspectiveSafeI6C2SourceResultV1 baseResult = TryCreateI6C2(snapshot);
        if (!baseResult.IsSuccess)
        {
            return PerspectiveSafeI6C3SourceResultV1.Failure(
                baseResult.Error!.Value);
        }

        PerspectiveSafeFrameSourceErrorV1 error;
        if (!TryCreateI6C3Entities(
                snapshot,
                baseResult.Source!,
                out PerspectiveSafeZoneV1[] zones,
                out PerspectiveSafeEntityV1[] entities,
                out Dictionary<MirrorEntityIdV1, string> locatorById,
                out Dictionary<MirrorEntityIdV1, MirrorCardSnapshotV1> cardsById,
                out error,
                out bool overlayProof))
        {
            return PerspectiveSafeI6C3SourceResultV1.Failure(error);
        }

        if (!TryCreateI6C3Relationships(
                snapshot,
                locatorById,
                cardsById,
                out PerspectiveSafeRelationshipV1[] relationships,
                out bool targetPending,
                out bool equipPending,
                out bool relationshipEndpointPending,
                out error))
        {
            return PerspectiveSafeI6C3SourceResultV1.Failure(error);
        }

        if (!TryCreateI6C3Chain(
                snapshot,
                locatorById,
                cardsById,
                entities,
                relationships,
                out PerspectiveSafeChainStateV1 chain,
                out bool chainPending,
                out bool activationZonePending,
                out error))
        {
            return PerspectiveSafeI6C3SourceResultV1.Failure(error);
        }

        PerspectiveSafeI6C3ConstituentStatusV1[] statuses =
            CreateI6C3Statuses(
                overlayProof,
                targetPending,
                equipPending,
                relationshipEndpointPending,
                chainPending,
                activationZonePending);
        PerspectiveSafeI6C3StateSourceV1 source =
            new(
                baseResult.Source!,
                zones,
                entities,
                relationships,
                chain,
                statuses);
        return PerspectiveSafeI6C3SourceResultV1.Success(source);
    }

    private static bool TryCreateI6C3Entities(
        MirrorSnapshotV1 snapshot,
        PerspectiveSafeI6C2StateSourceV1 baseSource,
        out PerspectiveSafeZoneV1[] zones,
        out PerspectiveSafeEntityV1[] entities,
        out Dictionary<MirrorEntityIdV1, string> locatorById,
        out Dictionary<MirrorEntityIdV1, MirrorCardSnapshotV1> cardsById,
        out PerspectiveSafeFrameSourceErrorV1 error,
        out bool overlayProof)
    {
        zones = Array.Empty<PerspectiveSafeZoneV1>();
        entities = Array.Empty<PerspectiveSafeEntityV1>();
        locatorById = new();
        cardsById = new();
        overlayProof = false;
        if (!TryGetAbsolutePerspective(snapshot, out byte perspectivePlayer))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                PerspectiveSafeSourceSectionV1.Entities);
            return false;
        }

        Dictionary<string, PerspectiveSafeEntityV1> baseEntities =
            baseSource.Entities.ToDictionary(
                entity => entity.Locator,
                StringComparer.Ordinal);
        List<I6C3OrdinalCandidate> ordinalCandidates = new();
        foreach (MirrorCardSnapshotV1 card in snapshot.Cards)
        {
            if (!cardsById.TryAdd(card.EntityId, card))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            if (card.IsOverlay ||
                !TryMapOrdinaryZone(card.Zone, out PerspectiveSafeSemanticZoneV1 semanticZone))
            {
                continue;
            }

            if (!TryGetAbsolutePlayer(
                    snapshot,
                    card.Controller,
                    out byte absoluteController,
                    out error) ||
                !TryReadCardCode(
                    card.CardCode,
                    out uint? cardCode,
                    out error) ||
                !TryReadOwner(
                    snapshot,
                    card.Owner,
                    out byte? owner,
                    out error))
            {
                return false;
            }

            bool ownerIsPerspective =
                (owner ?? absoluteController) == perspectivePlayer;
            bool publicIdentity = card.CardCode.IsKnown &&
                card.CardCode.Provenance is
                    MirrorProvenanceV1.PublicProtocolFact or
                    MirrorProvenanceV1.DerivedFromProvenPublicFacts;
            bool identityVisible = ownerIsPerspective || publicIdentity;
            bool pile = semanticZone is
                PerspectiveSafeSemanticZoneV1.Hand or
                PerspectiveSafeSemanticZoneV1.ExtraDeck;
            if (pile && !identityVisible)
            {
                continue;
            }

            if (pile && !cardCode.HasValue)
            {
                continue;
            }

            bool sequenceVisible = semanticZone switch
            {
                PerspectiveSafeSemanticZoneV1.Hand => ownerIsPerspective,
                PerspectiveSafeSemanticZoneV1.ExtraDeck => false,
                _ => true
            };
            if (sequenceVisible)
            {
                if (!TryMapPublicLocatorZone(
                        semanticZone,
                        out PublicSemanticZoneV1 locatorZone) ||
                    !PublicSemanticLocatorV1.TryCreateIndexed(
                        absoluteController,
                        locatorZone,
                        card.Sequence,
                        out PublicSemanticLocatorV1? locator))
                {
                    error = Error(
                        PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                        PerspectiveSafeSourceSectionV1.Entities);
                    return false;
                }

                if (baseEntities.ContainsKey(locator!.Value))
                {
                    locatorById[card.EntityId] = locator.Value;
                }
            }
            else if (cardCode.HasValue)
            {
                ordinalCandidates.Add(new(
                    card,
                    absoluteController,
                    semanticZone,
                    cardCode.Value,
                    card.Sequence));
            }
        }

        ordinalCandidates.Sort(static (left, right) =>
        {
            int result = left.Controller.CompareTo(right.Controller);
            if (result != 0)
            {
                return result;
            }

            result = OrdinalPileRank(left.Zone).CompareTo(OrdinalPileRank(right.Zone));
            return result != 0
                ? result
                : left.SourceSequence.CompareTo(right.SourceSequence);
        });
        Dictionary<(byte Controller, uint CardCode), uint> ordinalCounters = new();
        foreach (I6C3OrdinalCandidate candidate in ordinalCandidates)
        {
            (byte Controller, uint CardCode) key =
                (candidate.Controller, candidate.CardCode);
            if (!ordinalCounters.TryGetValue(key, out uint ordinal))
            {
                ordinal = 0;
            }

            if (!PublicSemanticLocatorV1.TryCreatePublicOrdinal(
                    candidate.Controller,
                    ToPublicOrdinalZone(candidate.Zone),
                    candidate.CardCode,
                    ordinal,
                    out PublicSemanticLocatorV1? locator))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            if (baseEntities.ContainsKey(locator!.Value))
            {
                locatorById[candidate.Card.EntityId] = locator.Value;
            }

            if (ordinal == uint.MaxValue)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            ordinalCounters[key] = ordinal + 1;
        }

        List<PerspectiveSafeEntityV1> entityValues =
            baseSource.Entities.ToList();
        Dictionary<MirrorEntityIdV1, List<MirrorRelationSnapshotV1>>
            overlayRelationsByMaterial = new();
        foreach (MirrorRelationSnapshotV1 relation in snapshot.OverlayRelations)
        {
            if (!cardsById.TryGetValue(relation.Source, out MirrorCardSnapshotV1? parent) ||
                !cardsById.TryGetValue(relation.Target, out MirrorCardSnapshotV1? material) ||
                material is null ||
                !material.IsOverlay ||
                parent is null ||
                parent.IsOverlay ||
                parent.Zone != MirrorZoneV1.MonsterZone ||
                material.Zone != MirrorZoneV1.MonsterZone ||
                parent.Controller != material.Controller ||
                parent.Sequence != material.Sequence)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                    PerspectiveSafeSourceSectionV1.Relationships);
                return false;
            }

            if (!overlayRelationsByMaterial.TryGetValue(
                    relation.Target,
                    out List<MirrorRelationSnapshotV1>? relations))
            {
                relations = new();
                overlayRelationsByMaterial.Add(relation.Target, relations);
            }

            relations.Add(relation);
        }

        uint[] overlayTotals = new uint[2];
        uint[] overlayPublic = new uint[2];
        HashSet<string> publicLocators =
            new(entityValues.Select(entity => entity.Locator), StringComparer.Ordinal);
        foreach (MirrorCardSnapshotV1 material in cardsById.Values
                     .Where(card => card.IsOverlay)
                     .OrderBy(card => card.Controller)
                     .ThenBy(card => card.Sequence)
                     .ThenBy(card => card.OverlayIndex))
        {
            if (!overlayRelationsByMaterial.TryGetValue(
                    material.EntityId,
                    out List<MirrorRelationSnapshotV1>? relations) ||
                relations.Count != 1)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                    PerspectiveSafeSourceSectionV1.Relationships);
                return false;
            }

            MirrorRelationSnapshotV1 relation = relations[0];
            MirrorCardSnapshotV1 parent = cardsById[relation.Source];
            if (!TryGetAbsolutePlayer(
                    snapshot,
                    material.Controller,
                    out byte absoluteController,
                    out error) ||
                !locatorById.TryGetValue(parent.EntityId, out string? parentLocator) ||
                !PublicSemanticLocatorV1.TryCreateOverlay(
                    absoluteController,
                    parent.Sequence,
                    material.OverlayIndex,
                    out PublicSemanticLocatorV1? locator) ||
                !publicLocators.Add(locator!.Value))
            {
                if (error.Code == 0)
                {
                    error = Error(
                        PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                        PerspectiveSafeSourceSectionV1.Entities);
                }

                return false;
            }

            if (!baseEntities.TryGetValue(parentLocator!, out PerspectiveSafeEntityV1? parentEntity))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            if (!TryReadCardCode(
                    material.CardCode,
                    out uint? materialCode,
                    out error) ||
                !TryReadPosition(
                    material.Position,
                    out _,
                    out PerspectiveSafePositionV1 position,
                    out error) ||
                !TryReadOwner(
                    snapshot,
                    material.Owner,
                    out byte? owner,
                    out error))
            {
                return false;
            }

            bool identityVisible = parentEntity.IdentityKnown && materialCode.HasValue;
            PerspectiveSafeCardPropertiesV1? current = null;
            if (identityVisible &&
                !TryCreateCurrentProperties(material, out current, out error))
            {
                return false;
            }

            entityValues.Add(new PerspectiveSafeEntityV1(
                locator.Value,
                identityVisible,
                identityVisible ? materialCode : null,
                owner,
                absoluteController,
                PerspectiveSafeSemanticZoneV1.Overlay,
                parent.Sequence,
                material.OverlayIndex,
                position,
                position is
                    PerspectiveSafePositionV1.FaceUpAttack or
                    PerspectiveSafePositionV1.FaceUpDefense,
                position is
                    PerspectiveSafePositionV1.FaceDownAttack or
                    PerspectiveSafePositionV1.FaceDownDefense,
                printed: null,
                current));
            locatorById[material.EntityId] = locator.Value;
            overlayTotals[absoluteController] = checked(overlayTotals[absoluteController] + 1);
            if (identityVisible)
            {
                overlayPublic[absoluteController] =
                    checked(overlayPublic[absoluteController] + 1);
            }
        }

        if (snapshot.OverlayRelations.Count !=
            cardsById.Values.Count(card => card.IsOverlay))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                PerspectiveSafeSourceSectionV1.Relationships);
            return false;
        }

        entityValues.Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.Locator, right.Locator));
        if (entityValues.Select(entity => entity.Locator).Distinct(StringComparer.Ordinal).Count() !=
            entityValues.Count)
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                PerspectiveSafeSourceSectionV1.Entities);
            return false;
        }

        List<PerspectiveSafeZoneV1> zoneValues = baseSource.Zones.ToList();
        for (byte player = 0; player < 2; player++)
        {
            zoneValues.Add(new(
                player,
                PerspectiveSafeSemanticZoneV1.Overlay,
                overlayTotals[player],
                overlayPublic[player],
                overlayTotals[player] - overlayPublic[player],
                false));
        }

        zoneValues.Sort(CompareZones);
        zones = zoneValues.ToArray();
        entities = entityValues.ToArray();
        overlayProof = true;
        error = default;
        return true;
    }

    private static bool TryCreateI6C3Relationships(
        MirrorSnapshotV1 snapshot,
        Dictionary<MirrorEntityIdV1, string> locatorById,
        Dictionary<MirrorEntityIdV1, MirrorCardSnapshotV1> cardsById,
        out PerspectiveSafeRelationshipV1[] relationships,
        out bool targetPending,
        out bool equipPending,
        out bool endpointPending,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        List<PerspectiveSafeRelationshipV1> values = new();
        HashSet<(PerspectiveSafeRelationshipKindV1 Kind, string Source, string Target)>
            seen = new();
        targetPending = false;
        equipPending = false;
        endpointPending = false;

        foreach (MirrorRelationSnapshotV1 relation in snapshot.OverlayRelations)
        {
            if (!locatorById.TryGetValue(relation.Source, out string? parentLocator) ||
                !locatorById.TryGetValue(relation.Target, out string? materialLocator))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                    PerspectiveSafeSourceSectionV1.Relationships);
                relationships = Array.Empty<PerspectiveSafeRelationshipV1>();
                return false;
            }

            if (!seen.Add((
                    PerspectiveSafeRelationshipKindV1.XyzMaterial,
                    materialLocator,
                    parentLocator)))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                    PerspectiveSafeSourceSectionV1.Relationships);
                relationships = Array.Empty<PerspectiveSafeRelationshipV1>();
                return false;
            }

            values.Add(new(
                PerspectiveSafeRelationshipKindV1.XyzMaterial,
                materialLocator,
                parentLocator));
        }

        foreach (MirrorRelationSnapshotV1 relation in snapshot.EquipmentRelations)
        {
            I6C3EndpointResolution sourceResolution = TryResolveI6C3Endpoint(
                snapshot,
                relation.Source,
                locatorById,
                cardsById,
                out string? source,
                out PerspectiveSafeFrameSourceErrorV1 sourceError);
            I6C3EndpointResolution targetResolution = TryResolveI6C3Endpoint(
                snapshot,
                relation.Target,
                locatorById,
                cardsById,
                out string? target,
                out PerspectiveSafeFrameSourceErrorV1 targetError);
            if (sourceResolution == I6C3EndpointResolution.Unproven ||
                targetResolution == I6C3EndpointResolution.Unproven)
            {
                error = sourceResolution == I6C3EndpointResolution.Unproven
                    ? sourceError
                    : targetError;
                relationships = Array.Empty<PerspectiveSafeRelationshipV1>();
                return false;
            }

            if (sourceResolution == I6C3EndpointResolution.PendingI6C5 ||
                targetResolution == I6C3EndpointResolution.PendingI6C5)
            {
                equipPending = true;
                endpointPending = true;
                continue;
            }

            if (sourceResolution == I6C3EndpointResolution.Hidden ||
                targetResolution == I6C3EndpointResolution.Hidden)
            {
                continue;
            }

            if (!seen.Add((PerspectiveSafeRelationshipKindV1.Equip, source!, target!)))
            {
                continue;
            }

            values.Add(new(
                PerspectiveSafeRelationshipKindV1.Equip,
                source,
                target));
        }

        foreach (MirrorRelationSnapshotV1 relation in snapshot.TargetRelations.Concat(
                     snapshot.ChainTargetRelations))
        {
            I6C3EndpointResolution sourceResolution = TryResolveI6C3Endpoint(
                snapshot,
                relation.Source,
                locatorById,
                cardsById,
                out string? source,
                out PerspectiveSafeFrameSourceErrorV1 sourceError);
            I6C3EndpointResolution targetResolution = TryResolveI6C3Endpoint(
                snapshot,
                relation.Target,
                locatorById,
                cardsById,
                out string? target,
                out PerspectiveSafeFrameSourceErrorV1 targetError);
            if (sourceResolution == I6C3EndpointResolution.Unproven ||
                targetResolution == I6C3EndpointResolution.Unproven)
            {
                error = sourceResolution == I6C3EndpointResolution.Unproven
                    ? sourceError
                    : targetError;
                relationships = Array.Empty<PerspectiveSafeRelationshipV1>();
                return false;
            }

            if (sourceResolution == I6C3EndpointResolution.PendingI6C5 ||
                targetResolution == I6C3EndpointResolution.PendingI6C5)
            {
                targetPending = true;
                endpointPending = true;
                continue;
            }

            if (sourceResolution == I6C3EndpointResolution.Hidden ||
                targetResolution == I6C3EndpointResolution.Hidden)
            {
                continue;
            }

            if (!seen.Add((PerspectiveSafeRelationshipKindV1.Target, source!, target!)))
            {
                continue;
            }

            values.Add(new(
                PerspectiveSafeRelationshipKindV1.Target,
                source,
                target));
        }

        values.Sort(CompareRelationships);
        relationships = values.ToArray();
        error = default;
        return true;
    }

    private static bool TryCreateI6C3Chain(
        MirrorSnapshotV1 snapshot,
        Dictionary<MirrorEntityIdV1, string> locatorById,
        Dictionary<MirrorEntityIdV1, MirrorCardSnapshotV1> cardsById,
        IReadOnlyList<PerspectiveSafeEntityV1> entities,
        IReadOnlyList<PerspectiveSafeRelationshipV1> relationships,
        out PerspectiveSafeChainStateV1 chain,
        out bool chainPending,
        out bool activationZonePending,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        chain = new PerspectiveSafeChainStateV1(0, Array.Empty<PerspectiveSafeChainLinkV1>());
        chainPending = false;
        activationZonePending = false;
        List<MirrorChainSnapshotV1> sourceChains = snapshot.Chains.ToList();
        if (snapshot.PendingChainSource is not null)
        {
            sourceChains.Add(snapshot.PendingChainSource);
        }

        Dictionary<string, PerspectiveSafeEntityV1> entitiesByLocator =
            entities.ToDictionary(entity => entity.Locator, StringComparer.Ordinal);
        List<PerspectiveSafeChainLinkV1> links = new(sourceChains.Count);
        for (int index = 0; index < sourceChains.Count; index++)
        {
            MirrorChainSnapshotV1 sourceChain = sourceChains[index];
            uint expectedSize = checked((uint)index + 1);
            if (sourceChain.ChainSize != expectedSize ||
                sourceChain.TriggeringController > 1 ||
                !cardsById.TryGetValue(sourceChain.Card, out MirrorCardSnapshotV1? sourceCard))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                    PerspectiveSafeSourceSectionV1.Chain);
                return false;
            }

            if (!TryGetAbsolutePlayer(
                    snapshot,
                    sourceCard.Controller,
                    out byte absoluteController,
                    out error))
            {
                return false;
            }

            if (!TryMapChainActivationZone(
                    sourceCard,
                    out PerspectiveSafeSemanticZoneV1? activationZone,
                    out bool activationPending,
                    out error))
            {
                return false;
            }

            activationZonePending |= activationPending;
            if (activationPending)
            {
                chainPending = true;
            }

            I6C3EndpointResolution sourceResolution = TryResolveI6C3Endpoint(
                snapshot,
                sourceChain.Card,
                locatorById,
                cardsById,
                out string? sourceLocator,
                out error);
            if (sourceResolution == I6C3EndpointResolution.Unproven)
            {
                return false;
            }

            if (sourceResolution == I6C3EndpointResolution.PendingI6C5)
            {
                sourceLocator = null;
                chainPending = true;
            }
            else if (sourceResolution == I6C3EndpointResolution.Hidden)
            {
                sourceLocator = null;
            }

            PerspectiveSafeEntityV1? sourceEntity = sourceLocator is null
                ? null
                : entitiesByLocator[sourceLocator];
            bool sourcePublic = sourceEntity?.IdentityKnown == true;
            if (!sourcePublic)
            {
                sourceLocator = null;
            }

            ulong? effectDescription = sourcePublic
                ? sourceChain.Description
                : null;
            List<string> targets = new();
            if (sourcePublic)
            {
                foreach (MirrorEntityIdV1 targetId in sourceChain.Targets)
                {
                    I6C3EndpointResolution targetResolution = TryResolveI6C3Endpoint(
                        snapshot,
                        targetId,
                        locatorById,
                        cardsById,
                        out string? targetLocator,
                        out error);
                    if (targetResolution == I6C3EndpointResolution.Unproven)
                    {
                        return false;
                    }

                    if (targetResolution == I6C3EndpointResolution.PendingI6C5)
                    {
                        chainPending = true;
                        continue;
                    }

                    if (targetResolution == I6C3EndpointResolution.Hidden)
                    {
                        continue;
                    }

                    if (!relationships.Any(relation =>
                            relation.Kind == PerspectiveSafeRelationshipKindV1.Target &&
                            relation.Source == sourceLocator &&
                            relation.Target == targetLocator))
                    {
                        error = Error(
                            PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                            PerspectiveSafeSourceSectionV1.Chain);
                        return false;
                    }
                }

                targets.AddRange(
                    relationships
                        .Where(relation =>
                            relation.Kind == PerspectiveSafeRelationshipKindV1.Target &&
                            relation.Source == sourceLocator)
                        .Select(relation => relation.Target)
                        .Distinct(StringComparer.Ordinal));
                targets.Sort(StringComparer.Ordinal);
            }

            links.Add(new PerspectiveSafeChainLinkV1(
                (uint)index,
                sourceChain.TriggeringController,
                sourceLocator,
                activationPending ? null : activationZone,
                effectDescription,
                targets));
        }

        chain = new PerspectiveSafeChainStateV1(
            (uint)links.Count,
            links);
        error = default;
        return true;
    }

    private static I6C3EndpointResolution TryResolveI6C3Endpoint(
        MirrorSnapshotV1 snapshot,
        MirrorEntityIdV1 id,
        Dictionary<MirrorEntityIdV1, string> locatorById,
        Dictionary<MirrorEntityIdV1, MirrorCardSnapshotV1> cardsById,
        out string? locator,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        locator = null;
        if (locatorById.TryGetValue(id, out string? resolved))
        {
            locator = resolved;
            error = default;
            return I6C3EndpointResolution.Resolved;
        }

        if (!cardsById.TryGetValue(id, out MirrorCardSnapshotV1? card))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                PerspectiveSafeSourceSectionV1.Relationships);
            return I6C3EndpointResolution.Unproven;
        }

        if (card.IsOverlay || card.Zone == MirrorZoneV1.SpellTrapZone)
        {
            error = default;
            return card.Zone == MirrorZoneV1.SpellTrapZone
                ? I6C3EndpointResolution.PendingI6C5
                : I6C3EndpointResolution.Unproven;
        }

        if (card.Zone == MirrorZoneV1.MainDeck)
        {
            error = default;
            return I6C3EndpointResolution.Hidden;
        }

        if (card.Zone is MirrorZoneV1.Hand or MirrorZoneV1.ExtraDeck)
        {
            if (!TryGetAbsolutePlayer(
                    snapshot,
                    card.Controller,
                    out byte absoluteController,
                    out error) ||
                !TryReadOwner(
                    snapshot,
                    card.Owner,
                    out byte? owner,
                    out error) ||
                !TryReadCardCode(
                    card.CardCode,
                    out uint? cardCode,
                    out error))
            {
                return I6C3EndpointResolution.Unproven;
            }

            bool visible =
                (owner ?? absoluteController) == snapshot.Perspective.PlayerType ||
                (cardCode.HasValue && card.CardCode.Provenance is
                    MirrorProvenanceV1.PublicProtocolFact or
                    MirrorProvenanceV1.DerivedFromProvenPublicFacts);
            if (visible)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                    PerspectiveSafeSourceSectionV1.Relationships);
                return I6C3EndpointResolution.Unproven;
            }

            error = default;
            return I6C3EndpointResolution.Hidden;
        }

        error = Error(
            PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
            PerspectiveSafeSourceSectionV1.Relationships);
        return I6C3EndpointResolution.Unproven;
    }

    private static bool TryMapChainActivationZone(
        MirrorCardSnapshotV1 source,
        out PerspectiveSafeSemanticZoneV1? zone,
        out bool pendingI6C5,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        pendingI6C5 = false;
        if (source.IsOverlay)
        {
            zone = PerspectiveSafeSemanticZoneV1.Overlay;
            error = default;
            return true;
        }

        if (TryMapOrdinaryZone(source.Zone, out PerspectiveSafeSemanticZoneV1 ordinary))
        {
            zone = ordinary;
            error = default;
            return true;
        }

        if (source.Zone == MirrorZoneV1.SpellTrapZone)
        {
            zone = null;
            pendingI6C5 = true;
            error = default;
            return true;
        }

        zone = null;
        error = Error(
            PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
            PerspectiveSafeSourceSectionV1.Chain);
        return false;
    }

    private static PerspectiveSafeI6C3ConstituentStatusV1[] CreateI6C3Statuses(
        bool overlayProof,
        bool targetPending,
        bool equipPending,
        bool endpointPending,
        bool chainPending,
        bool activationZonePending) =>
        new[]
        {
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.OverlayZone,
                overlayProof
                    ? PerspectiveSafeI6C3SourceStatusV1.Proven
                    : PerspectiveSafeI6C3SourceStatusV1.Blocked),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.OverlayEntities,
                overlayProof
                    ? PerspectiveSafeI6C3SourceStatusV1.Proven
                    : PerspectiveSafeI6C3SourceStatusV1.Blocked),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.OverlayLocators,
                overlayProof
                    ? PerspectiveSafeI6C3SourceStatusV1.Proven
                    : PerspectiveSafeI6C3SourceStatusV1.Blocked),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.OverlayIdentity,
                overlayProof
                    ? PerspectiveSafeI6C3SourceStatusV1.Proven
                    : PerspectiveSafeI6C3SourceStatusV1.Blocked),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.XyzMaterialRelationships,
                PerspectiveSafeI6C3SourceStatusV1.Proven),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.EquipRelationships,
                equipPending
                    ? PerspectiveSafeI6C3SourceStatusV1.BlockedPendingI6C5
                    : PerspectiveSafeI6C3SourceStatusV1.Proven),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.TargetRelationships,
                targetPending
                    ? PerspectiveSafeI6C3SourceStatusV1.BlockedPendingI6C5
                    : PerspectiveSafeI6C3SourceStatusV1.Proven),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.RelationshipEndpoints,
                endpointPending
                    ? PerspectiveSafeI6C3SourceStatusV1.BlockedPendingI6C5
                    : PerspectiveSafeI6C3SourceStatusV1.Proven),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.RelationshipOrdering,
                PerspectiveSafeI6C3SourceStatusV1.Proven),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.ChainTriggerMetadata,
                PerspectiveSafeI6C3SourceStatusV1.Proven),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.ChainLength,
                PerspectiveSafeI6C3SourceStatusV1.Proven),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.ChainIndexMapping,
                PerspectiveSafeI6C3SourceStatusV1.Proven),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.ChainLinkOrder,
                PerspectiveSafeI6C3SourceStatusV1.Proven),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.ChainActivatingPlayer,
                PerspectiveSafeI6C3SourceStatusV1.Proven),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.ChainSourceLocator,
                chainPending
                    ? PerspectiveSafeI6C3SourceStatusV1.BlockedPendingI6C5
                    : PerspectiveSafeI6C3SourceStatusV1.Proven),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.ChainActivationZone,
                activationZonePending
                    ? PerspectiveSafeI6C3SourceStatusV1.BlockedPendingI6C5
                    : PerspectiveSafeI6C3SourceStatusV1.Proven),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.ChainEffectDescription,
                chainPending
                    ? PerspectiveSafeI6C3SourceStatusV1.BlockedPendingI6C5
                    : PerspectiveSafeI6C3SourceStatusV1.Proven),
            new PerspectiveSafeI6C3ConstituentStatusV1(
                PerspectiveSafeI6C3ConstituentV1.ChainTargets,
                chainPending
                    ? PerspectiveSafeI6C3SourceStatusV1.BlockedPendingI6C5
                    : PerspectiveSafeI6C3SourceStatusV1.Proven)
        };

    private static PerspectiveSafeFrameSourceResultV1 Failure(
        PerspectiveSafeFrameSourceErrorCodeV1 code,
        PerspectiveSafeSourceSectionV1 section) =>
        PerspectiveSafeFrameSourceResultV1.Failure(
            new PerspectiveSafeFrameSourceErrorV1(code, section));

    private static bool TryCreateI6C2Globals(
        MirrorSnapshotV1 snapshot,
        out PerspectiveSafeI6C2GlobalsV1? globals,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        globals = null;
        error = default;
        if (!TryGetAbsolutePerspective(snapshot, out byte perspectivePlayer))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                PerspectiveSafeSourceSectionV1.Globals);
            return false;
        }

        uint[] lifePoints = new uint[2];
        for (byte absolutePlayer = 0; absolutePlayer < 2; absolutePlayer++)
        {
            MirrorParticipantRoleV1 role = absolutePlayer == perspectivePlayer
                ? MirrorParticipantRoleV1.Self
                : MirrorParticipantRoleV1.Opponent;
            if (!TryGetParticipant(snapshot, role, out MirrorParticipantSnapshotV1? participant) ||
                !TryReadKnownMirrorValue(
                    participant!.LifePoints,
                    PerspectiveSafeSourceSectionV1.Globals,
                    out lifePoints[absolutePlayer],
                    out error))
            {
                if (error.Code == 0)
                {
                    error = Error(
                        PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                        PerspectiveSafeSourceSectionV1.Globals);
                }

                return false;
            }
        }

        if (!TryMapOptionalRole(
                snapshot,
                snapshot.TurnPlayer,
                out byte? turnPlayer,
                out error))
        {
            return false;
        }

        uint? turnCount = null;
        if (snapshot.TurnCount != 0)
        {
            if (snapshot.TurnCount > uint.MaxValue)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                    PerspectiveSafeSourceSectionV1.Globals);
                return false;
            }

            turnCount = (uint)snapshot.TurnCount;
        }

        uint? phase = null;
        if (snapshot.Phase.IsKnown)
        {
            if (!IsKnownValue(snapshot.Phase.Provenance))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                    PerspectiveSafeSourceSectionV1.Globals);
                return false;
            }

            phase = snapshot.Phase.Value;
        }
        else if (snapshot.Phase.Provenance != MirrorProvenanceV1.UnknownRedacted)
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                PerspectiveSafeSourceSectionV1.Globals);
            return false;
        }

        byte? winner = null;
        if (snapshot.Terminal.Winner is MirrorParticipantRoleV1 winnerRole)
        {
            if (!PublicSemanticLocatorV1.TryGetAbsolutePlayer(
                    snapshot.Perspective,
                    winnerRole,
                    out byte absoluteWinner))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                    PerspectiveSafeSourceSectionV1.Globals);
                return false;
            }

            winner = absoluteWinner;
        }

        if (!snapshot.Terminal.IsTerminal && winner.HasValue)
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                PerspectiveSafeSourceSectionV1.Globals);
            return false;
        }

        globals = new PerspectiveSafeI6C2GlobalsV1(
            lifePoints,
            turnPlayer,
            turnCount,
            phase,
            snapshot.Terminal.IsTerminal,
            winner,
            snapshot.Terminal.IsTerminal ? snapshot.Terminal.WinType : null);
        error = default;
        return true;
    }

    private static bool TryCreateI6C2Zones(
        MirrorSnapshotV1 snapshot,
        out PerspectiveSafeZoneV1[] zones,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        zones = Array.Empty<PerspectiveSafeZoneV1>();
        if (!TryGetAbsolutePerspective(snapshot, out byte perspectivePlayer))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                PerspectiveSafeSourceSectionV1.Zones);
            return false;
        }

        List<PerspectiveSafeZoneV1> values = new(12);
        for (byte absolutePlayer = 0; absolutePlayer < 2; absolutePlayer++)
        {
            MirrorParticipantRoleV1 role = absolutePlayer == perspectivePlayer
                ? MirrorParticipantRoleV1.Self
                : MirrorParticipantRoleV1.Opponent;
            if (!TryGetParticipant(snapshot, role, out MirrorParticipantSnapshotV1? participant))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                    PerspectiveSafeSourceSectionV1.Zones);
                return false;
            }

            if (!TryGetMirrorZone(
                    participant!,
                    MirrorZoneV1.MainDeck,
                    out MirrorZoneSnapshotV1? mainDeck) ||
                !TryGetMirrorZone(
                    participant!,
                    MirrorZoneV1.Hand,
                    out MirrorZoneSnapshotV1? hand) ||
                !TryGetMirrorZone(
                    participant!,
                    MirrorZoneV1.MonsterZone,
                    out MirrorZoneSnapshotV1? monster) ||
                !TryGetMirrorZone(
                    participant!,
                    MirrorZoneV1.Graveyard,
                    out MirrorZoneSnapshotV1? graveyard) ||
                !TryGetMirrorZone(
                    participant!,
                    MirrorZoneV1.Banished,
                    out MirrorZoneSnapshotV1? banished) ||
                !TryGetMirrorZone(
                    participant!,
                    MirrorZoneV1.ExtraDeck,
                    out MirrorZoneSnapshotV1? extraDeck))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                    PerspectiveSafeSourceSectionV1.Zones);
                return false;
            }

            if (!TryReadKnownMirrorValue(
                    mainDeck!.Count,
                    PerspectiveSafeSourceSectionV1.Zones,
                    out uint mainCount,
                    out error) ||
                !TryReadKnownMirrorValue(
                    hand!.Count,
                    PerspectiveSafeSourceSectionV1.Zones,
                    out uint handCount,
                    out error) ||
                !TryReadKnownMirrorValue(
                    monster!.Count,
                    PerspectiveSafeSourceSectionV1.Zones,
                    out uint monsterCount,
                    out error) ||
                !TryReadKnownMirrorValue(
                    graveyard!.Count,
                    PerspectiveSafeSourceSectionV1.Zones,
                    out uint graveCount,
                    out error) ||
                !TryReadKnownMirrorValue(
                    banished!.Count,
                    PerspectiveSafeSourceSectionV1.Zones,
                    out uint banishedCount,
                    out error) ||
                !TryReadKnownMirrorValue(
                    extraDeck!.Count,
                    PerspectiveSafeSourceSectionV1.Zones,
                    out uint extraCount,
                    out error))
            {
                return false;
            }

            values.Add(new(
                absolutePlayer,
                PerspectiveSafeSemanticZoneV1.MainDeck,
                mainCount,
                0,
                mainCount,
                false));
            values.Add(new(
                absolutePlayer,
                PerspectiveSafeSemanticZoneV1.Hand,
                handCount,
                absolutePlayer == perspectivePlayer ? handCount : 0,
                absolutePlayer == perspectivePlayer ? 0 : handCount,
                absolutePlayer == perspectivePlayer));

            if (!TryCountFieldCards(
                    monster!,
                    out uint monsterFaceUp,
                    out uint monsterRepresented,
                    out error) ||
                monsterRepresented != monsterCount)
            {
                if (monsterRepresented != monsterCount && error.Code == 0)
                {
                    error = Error(
                        PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                        PerspectiveSafeSourceSectionV1.Zones);
                }

                return false;
            }

            values.Add(new(
                absolutePlayer,
                PerspectiveSafeSemanticZoneV1.MonsterZone,
                monsterCount,
                absolutePlayer == perspectivePlayer ? monsterCount : monsterFaceUp,
                absolutePlayer == perspectivePlayer
                    ? 0
                    : monsterCount - monsterFaceUp,
                true));

            if (!TryCountFieldCards(
                    graveyard!,
                    out _,
                    out uint graveRepresented,
                    out error) ||
                graveRepresented != graveCount ||
                !TryCountFieldCards(
                    banished!,
                    out _,
                    out uint banishedRepresented,
                    out error) ||
                banishedRepresented != banishedCount)
            {
                if (error.Code == 0)
                {
                    error = Error(
                        PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                        PerspectiveSafeSourceSectionV1.Zones);
                }

                return false;
            }

            values.Add(new(
                absolutePlayer,
                PerspectiveSafeSemanticZoneV1.Graveyard,
                graveCount,
                graveCount,
                0,
                true));
            values.Add(new(
                absolutePlayer,
                PerspectiveSafeSemanticZoneV1.Banished,
                banishedCount,
                banishedCount,
                0,
                true));

            if (!TryCountFieldCards(
                    extraDeck!,
                    out uint extraFaceUp,
                    out _,
                    out error))
            {
                return false;
            }

            uint extraPublic = absolutePlayer == perspectivePlayer
                ? extraCount
                : extraFaceUp;
            values.Add(new(
                absolutePlayer,
                PerspectiveSafeSemanticZoneV1.ExtraDeck,
                extraCount,
                extraPublic,
                extraCount - extraPublic,
                false));
        }

        zones = values.ToArray();
        error = default;
        return true;
    }

    private static bool TryCreateI6C2Entities(
        MirrorSnapshotV1 snapshot,
        out PerspectiveSafeEntityV1[] entities,
        out bool entityIdentityBlocked,
        out bool entityLocatorBlocked,
        out bool entityOwnerBlocked,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        entities = Array.Empty<PerspectiveSafeEntityV1>();
        entityIdentityBlocked = false;
        entityLocatorBlocked = false;
        entityOwnerBlocked = false;
        if (!TryGetAbsolutePerspective(snapshot, out byte perspectivePlayer))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                PerspectiveSafeSourceSectionV1.Entities);
            return false;
        }

        List<I6C2EntityCandidate> candidates = new();
        foreach (MirrorCardSnapshotV1 card in snapshot.Cards)
        {
            if (!TryGetAbsolutePlayer(
                    snapshot,
                    card.Controller,
                    out byte absoluteController,
                    out error))
            {
                return false;
            }

            if (card.IsOverlay)
            {
                continue;
            }

            if (!TryMapOrdinaryZone(
                    card.Zone,
                    out PerspectiveSafeSemanticZoneV1 semanticZone))
            {
                continue;
            }

            if (!TryReadCardCode(card.CardCode, out uint? cardCode, out error) ||
                !TryReadPosition(
                    card.Position,
                    out _,
                    out PerspectiveSafePositionV1 position,
                    out error) ||
                !TryReadOwner(
                    snapshot,
                    card.Owner,
                    out byte? owner,
                    out error))
            {
                return false;
            }

            // OCGForge uses query.owner.value_or(player) only for its
            // visibility predicate. This fallback never populates the public
            // owner field below.
            bool ownerIsPerspective =
                (owner ?? absoluteController) == perspectivePlayer;
            bool publicIdentity = card.CardCode.IsKnown &&
                card.CardCode.Provenance is
                    MirrorProvenanceV1.PublicProtocolFact or
                    MirrorProvenanceV1.DerivedFromProvenPublicFacts;
            bool identityVisible = ownerIsPerspective || publicIdentity;
            if (card.CardCode.IsKnown &&
                card.CardCode.Provenance == MirrorProvenanceV1.PerspectivePrivateFact &&
                !ownerIsPerspective)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            bool pile = semanticZone is
                PerspectiveSafeSemanticZoneV1.Hand or
                PerspectiveSafeSemanticZoneV1.ExtraDeck;
            if (pile && !identityVisible)
            {
                continue;
            }

            if (pile && !cardCode.HasValue)
            {
                entityIdentityBlocked = true;
                entityLocatorBlocked = true;
                continue;
            }

            bool sequenceVisible = semanticZone switch
            {
                PerspectiveSafeSemanticZoneV1.Hand => ownerIsPerspective,
                PerspectiveSafeSemanticZoneV1.ExtraDeck => false,
                _ => true
            };
            PerspectiveSafeCardPropertiesV1? current = null;
            if (identityVisible && cardCode.HasValue)
            {
                if (!TryCreateCurrentProperties(
                        card,
                        out current,
                        out error))
                {
                    return false;
                }
            }

            I6C2EntityCandidate candidate = new(
                absoluteController,
                semanticZone,
                card.Sequence,
                cardCode,
                owner,
                sequenceVisible,
                position,
                current);
            if (!sequenceVisible)
            {
                candidate.NeedsPublicOrdinal = true;
            }
            else if (!TryMapPublicLocatorZone(
                         semanticZone,
                         out PublicSemanticZoneV1 locatorZone) ||
                     !PublicSemanticLocatorV1.TryCreateIndexed(
                         absoluteController,
                         locatorZone,
                         card.Sequence,
                         out PublicSemanticLocatorV1? locator))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }
            else
            {
                candidate.Locator = locator!.Value;
            }

            entityOwnerBlocked |= !owner.HasValue;
            candidates.Add(candidate);

        }

        if (!TryMarkMissingPerspectivePileEntities(
                snapshot,
                candidates,
                perspectivePlayer,
                ref entityIdentityBlocked,
                ref entityLocatorBlocked,
                out error))
        {
            return false;
        }

        List<I6C2EntityCandidate> ordinalCandidates = candidates
            .Where(candidate => candidate.NeedsPublicOrdinal)
            .OrderBy(candidate => candidate.AbsoluteController)
            .ThenBy(candidate => candidate.SemanticZone ==
                PerspectiveSafeSemanticZoneV1.Hand ? 0 : 1)
            .ThenBy(candidate => candidate.SourceSequence)
            .ToList();
        Dictionary<(byte Controller, uint CardCode), uint> ordinalCounters = new();
        foreach (I6C2EntityCandidate candidate in ordinalCandidates)
        {
            (byte Controller, uint CardCode) key =
                (candidate.AbsoluteController, candidate.CardCode!.Value);
            if (!ordinalCounters.TryGetValue(key, out uint ordinal))
            {
                ordinal = 0;
            }

            if (!TryMapPublicLocatorZone(
                    candidate.SemanticZone,
                    out PublicSemanticZoneV1 locatorZone) ||
                !PublicSemanticLocatorV1.TryCreatePublicOrdinal(
                    candidate.AbsoluteController,
                    locatorZone,
                    candidate.CardCode!.Value,
                    ordinal,
                    out PublicSemanticLocatorV1? locator))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            candidate.Locator = locator!.Value;
            if (ordinal == uint.MaxValue)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            ordinalCounters[key] = ordinal + 1;
        }

        candidates.Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.Locator, right.Locator));
        HashSet<string> locatorValues = new(StringComparer.Ordinal);
        List<PerspectiveSafeEntityV1> result = new(candidates.Count);
        foreach (I6C2EntityCandidate candidate in candidates)
        {
            if (!locatorValues.Add(candidate.Locator))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            result.Add(new PerspectiveSafeEntityV1(
                candidate.Locator,
                candidate.CardCode.HasValue,
                candidate.CardCode,
                candidate.Owner,
                candidate.AbsoluteController,
                candidate.SemanticZone,
                candidate.SequenceVisible ? candidate.SourceSequence : null,
                null,
                candidate.Position,
                candidate.Position is
                    PerspectiveSafePositionV1.FaceUpAttack or
                    PerspectiveSafePositionV1.FaceUpDefense,
                candidate.Position is
                    PerspectiveSafePositionV1.FaceDownAttack or
                    PerspectiveSafePositionV1.FaceDownDefense,
                printed: null,
                current: candidate.Current));
        }

        entities = result.ToArray();
        error = default;
        return true;
    }

    private static bool TryMarkMissingPerspectivePileEntities(
        MirrorSnapshotV1 snapshot,
        IReadOnlyList<I6C2EntityCandidate> candidates,
        byte perspectivePlayer,
        ref bool entityIdentityBlocked,
        ref bool entityLocatorBlocked,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        error = default;
        for (byte absolutePlayer = 0; absolutePlayer < 2; absolutePlayer++)
        {
            if (absolutePlayer != perspectivePlayer)
            {
                continue;
            }

            MirrorParticipantRoleV1 role = absolutePlayer == perspectivePlayer
                ? MirrorParticipantRoleV1.Self
                : MirrorParticipantRoleV1.Opponent;
            if (!TryGetParticipant(snapshot, role, out MirrorParticipantSnapshotV1? participant) ||
                !TryGetMirrorZone(
                    participant!,
                    MirrorZoneV1.Hand,
                    out MirrorZoneSnapshotV1? hand) ||
                !TryGetMirrorZone(
                    participant!,
                    MirrorZoneV1.ExtraDeck,
                    out MirrorZoneSnapshotV1? extraDeck) ||
                !TryReadKnownMirrorValue(
                    hand!.Count,
                    PerspectiveSafeSourceSectionV1.Entities,
                    out uint handCount,
                    out error) ||
                !TryReadKnownMirrorValue(
                    extraDeck!.Count,
                    PerspectiveSafeSourceSectionV1.Entities,
                    out uint extraCount,
                    out error))
            {
                if (error.Code == 0)
                {
                    error = Error(
                        PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                        PerspectiveSafeSourceSectionV1.Entities);
                }

                return false;
            }

            int ownHandEntities = candidates.Count(candidate =>
                candidate.AbsoluteController == absolutePlayer &&
                candidate.SemanticZone == PerspectiveSafeSemanticZoneV1.Hand);
            int ownExtraEntities = candidates.Count(candidate =>
                candidate.AbsoluteController == absolutePlayer &&
                candidate.SemanticZone == PerspectiveSafeSemanticZoneV1.ExtraDeck);
            if (ownHandEntities != handCount || ownExtraEntities != extraCount)
            {
                entityIdentityBlocked = true;
                entityLocatorBlocked = true;
            }
        }

        error = default;
        return true;
    }

    private static bool TryCreateCurrentProperties(
        MirrorCardSnapshotV1 card,
        out PerspectiveSafeCardPropertiesV1? properties,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        uint? type = null;
        uint? attribute = null;
        ulong? race = null;
        int? attack = null;
        int? defense = null;
        int? baseAttack = null;
        int? baseDefense = null;
        uint? level = null;
        uint? rank = null;
        uint? linkRating = null;
        uint? leftScale = null;
        uint? rightScale = null;
        uint? statusFlags = null;
        List<PerspectiveSafeLinkMarkerV1> linkMarkers = new();
        List<PerspectiveSafeCounterV1> counters = new();

        foreach (MirrorQueryFieldSnapshotV1 field in card.QueryFields)
        {
            if (!IsCurrentPropertyFlag(field.Flag))
            {
                continue;
            }

            if (!TryUseQueryValue(
                    field.Value,
                    out bool available,
                    out error))
            {
                properties = null;
                return false;
            }

            if (!available)
            {
                continue;
            }

            switch (field.Flag)
            {
                case QueryFlagV1.Type:
                    if (!TryGetUInt32QueryValue(field.Value, out type))
                    {
                        properties = null;
                        error = QueryValueError();
                        return false;
                    }

                    break;
                case QueryFlagV1.Attribute:
                    if (!TryGetUInt32QueryValue(field.Value, out attribute))
                    {
                        properties = null;
                        error = QueryValueError();
                        return false;
                    }

                    break;
                case QueryFlagV1.Race:
                    if (!TryGetUInt64QueryValue(field.Value, out race))
                    {
                        properties = null;
                        error = QueryValueError();
                        return false;
                    }

                    break;
                case QueryFlagV1.Attack:
                    if (!TryGetInt32QueryValue(field.Value, out attack))
                    {
                        properties = null;
                        error = QueryValueError();
                        return false;
                    }

                    break;
                case QueryFlagV1.Defense:
                    if (!TryGetInt32QueryValue(field.Value, out defense))
                    {
                        properties = null;
                        error = QueryValueError();
                        return false;
                    }

                    break;
                case QueryFlagV1.BaseAttack:
                    if (!TryGetInt32QueryValue(field.Value, out baseAttack))
                    {
                        properties = null;
                        error = QueryValueError();
                        return false;
                    }

                    break;
                case QueryFlagV1.BaseDefense:
                    if (!TryGetInt32QueryValue(field.Value, out baseDefense))
                    {
                        properties = null;
                        error = QueryValueError();
                        return false;
                    }

                    break;
                case QueryFlagV1.Level:
                    if (!TryGetUInt32QueryValue(field.Value, out level))
                    {
                        properties = null;
                        error = QueryValueError();
                        return false;
                    }

                    break;
                case QueryFlagV1.Rank:
                    if (!TryGetUInt32QueryValue(field.Value, out rank))
                    {
                        properties = null;
                        error = QueryValueError();
                        return false;
                    }

                    break;
                case QueryFlagV1.Link:
                    if (field.Value.Kind != MirrorQueryValueKindV1.UInt32Pair)
                    {
                        properties = null;
                        error = QueryValueError();
                        return false;
                    }

                    linkRating = field.Value.UInt32Value;
                    AddLinkMarkers(field.Value.LinkMarker, linkMarkers);
                    break;
                case QueryFlagV1.LScale:
                    if (!TryGetUInt32QueryValue(field.Value, out leftScale))
                    {
                        properties = null;
                        error = QueryValueError();
                        return false;
                    }

                    break;
                case QueryFlagV1.RScale:
                    if (!TryGetUInt32QueryValue(field.Value, out rightScale))
                    {
                        properties = null;
                        error = QueryValueError();
                        return false;
                    }

                    break;
                case QueryFlagV1.Status:
                    if (!TryGetUInt32QueryValue(field.Value, out statusFlags))
                    {
                        properties = null;
                        error = QueryValueError();
                        return false;
                    }

                    break;
                case QueryFlagV1.Counters:
                    if (field.Value.Kind != MirrorQueryValueKindV1.PackedUInt32Vector)
                    {
                        properties = null;
                        error = QueryValueError();
                        return false;
                    }

                    foreach (uint packed in field.Value.UInt32Values)
                    {
                        counters.Add(new(
                            packed & 0xffff,
                            packed >> 16));
                    }

                    break;
            }
        }

        bool link = type.HasValue && (type.Value & TypeLink) != 0;
        bool xyz = type.HasValue && (type.Value & TypeXyz) != 0;
        if (link)
        {
            defense = null;
            baseDefense = null;
        }

        if (xyz)
        {
            level = null;
        }
        else
        {
            rank = null;
        }

        counters.Sort(static (left, right) =>
        {
            int result = left.Type.CompareTo(right.Type);
            return result != 0 ? result : left.Count.CompareTo(right.Count);
        });
        properties = new PerspectiveSafeCardPropertiesV1(
            type,
            attribute,
            race,
            attack,
            defense,
            baseAttack,
            baseDefense,
            level,
            rank,
            linkRating,
            linkMarkers,
            leftScale,
            rightScale,
            statusFlags,
            counters);
        error = default;
        return true;
    }

    private static bool ValidateGlobals(
        PerspectiveSafeGlobalsV1 globals,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        if (!IsPlayer(globals.PlayerToAct) ||
            !IsPlayer(globals.TurnPlayer) ||
            !IsPlayer(globals.Winner))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidPlayer,
                PerspectiveSafeSourceSectionV1.Globals);
            return false;
        }

        error = default;
        return true;
    }

    private static bool ValidateZones(
        IReadOnlyList<PerspectiveSafeZoneV1> zones,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        for (int index = 0; index < zones.Count; index++)
        {
            PerspectiveSafeZoneV1 zone = zones[index];
            if (zone.Player > 1)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidPlayer,
                    PerspectiveSafeSourceSectionV1.Zones);
                return false;
            }

            if (!IsSemanticZone(zone.Kind, allowUnknown: false))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnknownEnum,
                    PerspectiveSafeSourceSectionV1.Zones);
                return false;
            }

            if (index > 0 && CompareZones(zones[index - 1], zone) > 0)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidOrdering,
                    PerspectiveSafeSourceSectionV1.Zones);
                return false;
            }
        }

        error = default;
        return true;
    }

    private static bool ValidateEntities(
        IReadOnlyList<PerspectiveSafeEntityV1> entities,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        HashSet<string> locators = new(StringComparer.Ordinal);
        for (int index = 0; index < entities.Count; index++)
        {
            PerspectiveSafeEntityV1 entity = entities[index];
            if (entity is null)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidInput,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            if (!IsLocator(entity.Locator))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            if (!locators.Add(entity.Locator))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.DuplicateLocator,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            if (entity.Owner is > 1 || entity.Controller is > 1)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidPlayer,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            if (!IsSemanticZone(entity.Zone, allowUnknown: false) ||
                !IsDefined(entity.Position))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnknownEnum,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            if (entity.FaceUp && entity.FaceDown)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.ContradictoryEntityState,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            if (!entity.IdentityKnown &&
                (entity.Passcode.HasValue ||
                 entity.Printed is not null ||
                 entity.Current is not null))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.ContradictoryEntityState,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            if (!ValidateProperties(entity.Printed, out error) ||
                !ValidateProperties(entity.Current, out error))
            {
                return false;
            }

            if (index > 0 &&
                string.CompareOrdinal(entities[index - 1].Locator, entity.Locator) > 0)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidOrdering,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }
        }

        error = default;
        return true;
    }

    private static bool ValidateProperties(
        PerspectiveSafeCardPropertiesV1? properties,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        if (properties is null)
        {
            error = default;
            return true;
        }

        for (int index = 0; index < properties.LinkMarkers.Count; index++)
        {
            PerspectiveSafeLinkMarkerV1 marker = properties.LinkMarkers[index];
            if (!IsDefined(marker))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnknownEnum,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            if (index > 0 &&
                properties.LinkMarkers[index - 1] > marker)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidOrdering,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }
        }

        for (int index = 1; index < properties.Counters.Count; index++)
        {
            PerspectiveSafeCounterV1 previous = properties.Counters[index - 1];
            PerspectiveSafeCounterV1 current = properties.Counters[index];
            if (CompareCounters(previous, current) > 0)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidOrdering,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }
        }

        error = default;
        return true;
    }

    private static bool ValidateRelationships(
        IReadOnlyList<PerspectiveSafeRelationshipV1> relationships,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        for (int index = 0; index < relationships.Count; index++)
        {
            PerspectiveSafeRelationshipV1 relationship = relationships[index];
            if (relationship is null)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidInput,
                    PerspectiveSafeSourceSectionV1.Relationships);
                return false;
            }

            if (!IsDefined(relationship.Kind))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnknownEnum,
                    PerspectiveSafeSourceSectionV1.Relationships);
                return false;
            }

            if (!IsLocator(relationship.Source) ||
                !IsLocator(relationship.Target))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator,
                    PerspectiveSafeSourceSectionV1.Relationships);
                return false;
            }

            if (index > 0 &&
                CompareRelationships(relationships[index - 1], relationship) > 0)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidOrdering,
                    PerspectiveSafeSourceSectionV1.Relationships);
                return false;
            }
        }

        error = default;
        return true;
    }

    private static bool ValidateChain(
        PerspectiveSafeChainStateV1 chain,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        error = default;
        if (chain.Length != (uint)chain.Links.Count)
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.ChainLengthMismatch,
                PerspectiveSafeSourceSectionV1.Chain);
            return false;
        }

        foreach (PerspectiveSafeChainLinkV1 link in chain.Links)
        {
            if (link is null)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidInput,
                    PerspectiveSafeSourceSectionV1.Chain);
                return false;
            }

            if (!IsPlayer(link.ActivatingPlayer))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidPlayer,
                    PerspectiveSafeSourceSectionV1.Chain);
                return false;
            }

            if (link.Source is not null && !IsLocator(link.Source))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator,
                    PerspectiveSafeSourceSectionV1.Chain);
                return false;
            }

            if (link.ActivationZone.HasValue &&
                !IsSemanticZone(link.ActivationZone.Value, allowUnknown: false))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnknownEnum,
                    PerspectiveSafeSourceSectionV1.Chain);
                return false;
            }

            if (!ValidateSortedLocators(
                    link.Targets,
                    PerspectiveSafeSourceSectionV1.Chain,
                    out error))
            {
                return false;
            }
        }

        error = default;
        return true;
    }

    private static bool ValidateEvents(
        IReadOnlyList<PerspectiveSafeVisibleEventV1> events,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        error = default;
        bool havePrevious = false;
        ulong previousIndex = 0;
        foreach (PerspectiveSafeVisibleEventV1 visibleEvent in events)
        {
            if (visibleEvent is null)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidInput,
                    PerspectiveSafeSourceSectionV1.VisibleEvents);
                return false;
            }

            if (!IsDefined(visibleEvent.Kind) ||
                visibleEvent.Kind == PerspectiveSafeVisibleEventKindV1.Unknown)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnknownEnum,
                    PerspectiveSafeSourceSectionV1.VisibleEvents);
                return false;
            }

            if (!IsPlayer(visibleEvent.Player))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidPlayer,
                    PerspectiveSafeSourceSectionV1.VisibleEvents);
                return false;
            }

            if (!IsPlayer(visibleEvent.Winner))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidPlayer,
                    PerspectiveSafeSourceSectionV1.VisibleEvents);
                return false;
            }

            if (visibleEvent.EntityLocator is not null &&
                !IsLocator(visibleEvent.EntityLocator))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator,
                    PerspectiveSafeSourceSectionV1.VisibleEvents);
                return false;
            }

            if (visibleEvent.FromZone.HasValue &&
                !IsSemanticZone(visibleEvent.FromZone.Value, allowUnknown: false))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnknownEnum,
                    PerspectiveSafeSourceSectionV1.VisibleEvents);
                return false;
            }

            if (visibleEvent.ToZone.HasValue &&
                !IsSemanticZone(visibleEvent.ToZone.Value, allowUnknown: false))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnknownEnum,
                    PerspectiveSafeSourceSectionV1.VisibleEvents);
                return false;
            }

            if (!ValidateSortedLocators(
                    visibleEvent.Targets,
                    PerspectiveSafeSourceSectionV1.VisibleEvents,
                    out error))
            {
                return false;
            }

            if (havePrevious)
            {
                if (visibleEvent.EventIndex == previousIndex)
                {
                    error = Error(
                        PerspectiveSafeFrameSourceErrorCodeV1.DuplicateEventIndex,
                        PerspectiveSafeSourceSectionV1.VisibleEvents);
                    return false;
                }

                if (visibleEvent.EventIndex < previousIndex)
                {
                    error = Error(
                        PerspectiveSafeFrameSourceErrorCodeV1.EventIndexNotIncreasing,
                        PerspectiveSafeSourceSectionV1.VisibleEvents);
                    return false;
                }
            }

            previousIndex = visibleEvent.EventIndex;
            havePrevious = true;
        }

        error = default;
        return true;
    }

    private static bool ValidateMatchContext(
        PerspectiveSafeMatchContextV1 context,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        if (context.PerspectivePlayer > 1)
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidPlayer,
                PerspectiveSafeSourceSectionV1.MatchContext);
            return false;
        }

        if (!ValidateDeck(context.OwnDeck, out error) ||
            !ValidateDeck(context.OpponentDeck, out error))
        {
            return false;
        }

        error = default;
        return true;
    }

    private static bool ValidateDeck(
        PerspectiveSafeDeckV1 deck,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        if (!deck.Known && (deck.MainDeck.Count != 0 || deck.ExtraDeck.Count != 0))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidDeckState,
                PerspectiveSafeSourceSectionV1.MatchContext);
            return false;
        }

        if (!IsSorted(deck.MainDeck) || !IsSorted(deck.ExtraDeck))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidOrdering,
                PerspectiveSafeSourceSectionV1.MatchContext);
            return false;
        }

        error = default;
        return true;
    }

    private static bool ValidateSortedLocators(
        IReadOnlyList<string> locators,
        PerspectiveSafeSourceSectionV1 section,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        for (int index = 0; index < locators.Count; index++)
        {
            if (!IsLocator(locators[index]))
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator,
                    section);
                return false;
            }

            if (index > 0 &&
                string.CompareOrdinal(locators[index - 1], locators[index]) > 0)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidOrdering,
                    section);
                return false;
            }
        }

        error = default;
        return true;
    }

    private static bool IsSorted(IReadOnlyList<uint> values)
    {
        for (int index = 1; index < values.Count; index++)
        {
            if (values[index - 1] > values[index])
            {
                return false;
            }
        }

        return true;
    }

    private const uint PositionFaceUpMask = 0x05;
    private const uint TypeXyz = 0x00800000;
    private const uint TypeLink = 0x04000000;
    private const uint LinkMarkerBottomLeft = 0x01;
    private const uint LinkMarkerBottom = 0x02;
    private const uint LinkMarkerBottomRight = 0x04;
    private const uint LinkMarkerLeft = 0x08;
    private const uint LinkMarkerRight = 0x10;
    private const uint LinkMarkerTopLeft = 0x20;
    private const uint LinkMarkerTop = 0x40;
    private const uint LinkMarkerTopRight = 0x80;

    private static PerspectiveSafeI6C2ConstituentStatusV1[] CreateI6C2Statuses(
        bool entityIdentityBlocked,
        bool entityLocatorBlocked,
        bool entityOwnerBlocked) =>
        new[]
        {
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.LifePoints,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.TurnPlayer,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.TurnCount,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.Phase,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.Terminal,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.Winner,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.WinReason,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.DuelFlags,
                PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C5),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.PlayerToAct,
                PerspectiveSafeI6C2SourceStatusV1.OutsideI6CPendingI6D),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.ChainLength,
                PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C3),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.Relationships,
                PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C3),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.Chain,
                PerspectiveSafeI6C2SourceStatusV1.Blocked),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.VisibleEvents,
                PerspectiveSafeI6C2SourceStatusV1.Blocked),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.EventIndex,
                PerspectiveSafeI6C2SourceStatusV1.Blocked),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.MatchContext,
                PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C5),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.MainDeckZone,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.HandZone,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.MonsterZone,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.SpellTrapLayout,
                PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C5),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.GraveyardZone,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.BanishedZone,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.ExtraDeckZone,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.OverlayZone,
                PerspectiveSafeI6C2SourceStatusV1.BlockedPendingI6C3),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.EntityLocator,
                entityLocatorBlocked
                    ? PerspectiveSafeI6C2SourceStatusV1.Blocked
                    : PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.EntityIdentity,
                entityIdentityBlocked
                    ? PerspectiveSafeI6C2SourceStatusV1.Blocked
                    : PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.EntityOwner,
                entityOwnerBlocked
                    ? PerspectiveSafeI6C2SourceStatusV1.Blocked
                    : PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.EntityController,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.EntitySequence,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.EntityPosition,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.EntityCurrentProperties,
                PerspectiveSafeI6C2SourceStatusV1.Proven),
            new PerspectiveSafeI6C2ConstituentStatusV1(
                PerspectiveSafeI6C2ConstituentV1.EntityPrintedProperties,
                PerspectiveSafeI6C2SourceStatusV1.Blocked)
        };

    private static bool TryGetAbsolutePerspective(
        MirrorSnapshotV1 snapshot,
        out byte perspectivePlayer)
    {
        perspectivePlayer = 0;
        if (snapshot.Perspective is null ||
            snapshot.Perspective.PlayerType > 1 ||
            (snapshot.Perspective.Kind != GameplayPerspectiveKind.SelfIsPlayer0 &&
             snapshot.Perspective.Kind != GameplayPerspectiveKind.SelfIsPlayer1) ||
            snapshot.Perspective.PlayerType != (byte)snapshot.Perspective.Kind)
        {
            return false;
        }

        perspectivePlayer = snapshot.Perspective.PlayerType;
        return true;
    }

    private static bool TryGetAbsolutePlayer(
        MirrorSnapshotV1 snapshot,
        MirrorParticipantRoleV1 role,
        out byte absolutePlayer,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        absolutePlayer = 0;
        if (!PublicSemanticLocatorV1.TryGetAbsolutePlayer(
                snapshot.Perspective,
                role,
                out absolutePlayer))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                PerspectiveSafeSourceSectionV1.Entities);
            return false;
        }

        error = default;
        return true;
    }

    private static bool TryGetParticipant(
        MirrorSnapshotV1 snapshot,
        MirrorParticipantRoleV1 role,
        out MirrorParticipantSnapshotV1? participant)
    {
        participant = null;
        if (role is not MirrorParticipantRoleV1.Self and
            not MirrorParticipantRoleV1.Opponent)
        {
            return false;
        }

        int found = 0;
        foreach (MirrorParticipantSnapshotV1 candidate in snapshot.Participants)
        {
            if (candidate.Role != role)
            {
                continue;
            }

            participant = candidate;
            found++;
        }

        return found == 1 && snapshot.Participants.Count == 2;
    }

    private static bool TryGetMirrorZone(
        MirrorParticipantSnapshotV1 participant,
        MirrorZoneV1 requested,
        out MirrorZoneSnapshotV1? zone)
    {
        zone = null;
        int found = 0;
        foreach (MirrorZoneSnapshotV1 candidate in participant.Zones)
        {
            if (candidate.Zone != requested)
            {
                continue;
            }

            zone = candidate;
            found++;
        }

        return found == 1;
    }

    private static bool TryReadKnownMirrorValue<T>(
        MirrorValueV1<T> value,
        PerspectiveSafeSourceSectionV1 section,
        out T result,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        result = default!;
        if (!value.IsKnown || !IsKnownValue(value.Provenance))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                section);
            return false;
        }

        result = value.Value;
        error = default;
        return true;
    }

    private static bool TryMapOptionalRole(
        MirrorSnapshotV1 snapshot,
        MirrorValueV1<MirrorParticipantRoleV1> value,
        out byte? absolutePlayer,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        absolutePlayer = null;
        if (!value.IsKnown)
        {
            if (value.Provenance != MirrorProvenanceV1.UnknownRedacted)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                    PerspectiveSafeSourceSectionV1.Globals);
                return false;
            }

            error = default;
            return true;
        }

        if (!IsKnownValue(value.Provenance) ||
            !PublicSemanticLocatorV1.TryGetAbsolutePlayer(
                snapshot.Perspective,
                value.Value,
                out byte mapped))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                PerspectiveSafeSourceSectionV1.Globals);
            return false;
        }

        absolutePlayer = mapped;
        error = default;
        return true;
    }

    private static bool TryReadCardCode(
        MirrorValueV1<uint> value,
        out uint? cardCode,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        cardCode = null;
        if (!value.IsKnown)
        {
            if (value.Provenance != MirrorProvenanceV1.UnknownRedacted)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            error = default;
            return true;
        }

        if (!IsKnownValue(value.Provenance) || value.Value == 0)
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                PerspectiveSafeSourceSectionV1.Entities);
            return false;
        }

        cardCode = value.Value;
        error = default;
        return true;
    }

    private static bool TryReadPosition(
        MirrorValueV1<uint> value,
        out uint? rawPosition,
        out PerspectiveSafePositionV1 position,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        rawPosition = null;
        position = PerspectiveSafePositionV1.Unknown;
        if (!value.IsKnown)
        {
            if (value.Provenance != MirrorProvenanceV1.UnknownRedacted)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            error = default;
            return true;
        }

        if (!IsKnownValue(value.Provenance))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                PerspectiveSafeSourceSectionV1.Entities);
            return false;
        }

        rawPosition = value.Value;
        position = value.Value switch
        {
            0x01 => PerspectiveSafePositionV1.FaceUpAttack,
            0x02 => PerspectiveSafePositionV1.FaceDownAttack,
            0x04 => PerspectiveSafePositionV1.FaceUpDefense,
            0x08 => PerspectiveSafePositionV1.FaceDownDefense,
            _ => PerspectiveSafePositionV1.Unknown
        };
        error = default;
        return true;
    }

    private static bool TryReadOwner(
        MirrorSnapshotV1 snapshot,
        MirrorValueV1<MirrorParticipantRoleV1> value,
        out byte? owner,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        owner = null;
        if (!value.IsKnown)
        {
            if (value.Provenance != MirrorProvenanceV1.UnknownRedacted)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                    PerspectiveSafeSourceSectionV1.Entities);
                return false;
            }

            error = default;
            return true;
        }

        if (!IsKnownValue(value.Provenance) ||
            !PublicSemanticLocatorV1.TryGetAbsolutePlayer(
                snapshot.Perspective,
                value.Value,
                out byte mapped))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                PerspectiveSafeSourceSectionV1.Entities);
            return false;
        }

        owner = mapped;
        error = default;
        return true;
    }

    private static bool TryMapOrdinaryZone(
        MirrorZoneV1 zone,
        out PerspectiveSafeSemanticZoneV1 semanticZone) {
        semanticZone = zone switch
        {
            MirrorZoneV1.Hand => PerspectiveSafeSemanticZoneV1.Hand,
            MirrorZoneV1.MonsterZone => PerspectiveSafeSemanticZoneV1.MonsterZone,
            MirrorZoneV1.Graveyard => PerspectiveSafeSemanticZoneV1.Graveyard,
            MirrorZoneV1.Banished => PerspectiveSafeSemanticZoneV1.Banished,
            MirrorZoneV1.ExtraDeck => PerspectiveSafeSemanticZoneV1.ExtraDeck,
            _ => PerspectiveSafeSemanticZoneV1.Unknown
        };
        return semanticZone != PerspectiveSafeSemanticZoneV1.Unknown;
    }

    private static bool TryMapPublicLocatorZone(
        PerspectiveSafeSemanticZoneV1 semanticZone,
        out PublicSemanticZoneV1 locatorZone)
    {
        locatorZone = semanticZone switch
        {
            PerspectiveSafeSemanticZoneV1.Hand => PublicSemanticZoneV1.Hand,
            PerspectiveSafeSemanticZoneV1.MonsterZone => PublicSemanticZoneV1.MonsterZone,
            PerspectiveSafeSemanticZoneV1.Graveyard => PublicSemanticZoneV1.Graveyard,
            PerspectiveSafeSemanticZoneV1.Banished => PublicSemanticZoneV1.Banished,
            PerspectiveSafeSemanticZoneV1.ExtraDeck => PublicSemanticZoneV1.ExtraDeck,
            _ => default
        };
        return semanticZone is
            PerspectiveSafeSemanticZoneV1.Hand or
            PerspectiveSafeSemanticZoneV1.MonsterZone or
            PerspectiveSafeSemanticZoneV1.Graveyard or
            PerspectiveSafeSemanticZoneV1.Banished or
            PerspectiveSafeSemanticZoneV1.ExtraDeck;
    }

    private static bool TryCountFieldCards(
        MirrorZoneSnapshotV1 zone,
        out uint faceUp,
        out uint represented,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        faceUp = 0;
        represented = 0;
        foreach (MirrorCardSnapshotV1 card in zone.Cards)
        {
            if (card.IsOverlay)
            {
                continue;
            }

            if (represented == uint.MaxValue)
            {
                error = Error(
                    PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
                    PerspectiveSafeSourceSectionV1.Zones);
                return false;
            }

            represented++;
            if (!TryReadPosition(
                    card.Position,
                    out uint? rawPosition,
                    out _,
                    out error))
            {
                return false;
            }

            if (rawPosition is uint raw && (raw & PositionFaceUpMask) != 0)
            {
                faceUp++;
            }
        }

        error = default;
        return true;
    }

    private static bool TryUseQueryValue(
        MirrorQueryValueV1 value,
        out bool available,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        available = false;
        if (!value.IsKnown)
        {
            if (value.Provenance != MirrorProvenanceV1.UnknownRedacted)
            {
                error = QueryValueError();
                return false;
            }

            error = default;
            return true;
        }

        if (!IsKnownValue(value.Provenance))
        {
            error = QueryValueError();
            return false;
        }

        available = true;
        error = default;
        return true;
    }

    private static bool TryGetUInt32QueryValue(
        MirrorQueryValueV1 value,
        out uint? result)
    {
        result = value.Kind == MirrorQueryValueKindV1.UInt32
            ? value.UInt32Value
            : null;
        return result.HasValue;
    }

    private static bool TryGetInt32QueryValue(
        MirrorQueryValueV1 value,
        out int? result)
    {
        result = value.Kind == MirrorQueryValueKindV1.Int32
            ? value.Int32Value
            : null;
        return result.HasValue;
    }

    private static bool TryGetUInt64QueryValue(
        MirrorQueryValueV1 value,
        out ulong? result)
    {
        result = value.Kind == MirrorQueryValueKindV1.UInt64
            ? value.UInt64Value
            : null;
        return result.HasValue;
    }

    private static bool IsCurrentPropertyFlag(QueryFlagV1 flag) =>
        flag is QueryFlagV1.Type or
            QueryFlagV1.Attribute or
            QueryFlagV1.Race or
            QueryFlagV1.Attack or
            QueryFlagV1.Defense or
            QueryFlagV1.BaseAttack or
            QueryFlagV1.BaseDefense or
            QueryFlagV1.Level or
            QueryFlagV1.Rank or
            QueryFlagV1.Counters or
            QueryFlagV1.Status or
            QueryFlagV1.LScale or
            QueryFlagV1.RScale or
            QueryFlagV1.Link;

    private static void AddLinkMarkers(
        uint markerBits,
        List<PerspectiveSafeLinkMarkerV1> markers)
    {
        if ((markerBits & LinkMarkerBottomLeft) != 0)
        {
            markers.Add(PerspectiveSafeLinkMarkerV1.BottomLeft);
        }

        if ((markerBits & LinkMarkerBottom) != 0)
        {
            markers.Add(PerspectiveSafeLinkMarkerV1.Bottom);
        }

        if ((markerBits & LinkMarkerBottomRight) != 0)
        {
            markers.Add(PerspectiveSafeLinkMarkerV1.BottomRight);
        }

        if ((markerBits & LinkMarkerLeft) != 0)
        {
            markers.Add(PerspectiveSafeLinkMarkerV1.Left);
        }

        if ((markerBits & LinkMarkerRight) != 0)
        {
            markers.Add(PerspectiveSafeLinkMarkerV1.Right);
        }

        if ((markerBits & LinkMarkerTopLeft) != 0)
        {
            markers.Add(PerspectiveSafeLinkMarkerV1.TopLeft);
        }

        if ((markerBits & LinkMarkerTop) != 0)
        {
            markers.Add(PerspectiveSafeLinkMarkerV1.Top);
        }

        if ((markerBits & LinkMarkerTopRight) != 0)
        {
            markers.Add(PerspectiveSafeLinkMarkerV1.TopRight);
        }
    }

    private static PerspectiveSafeFrameSourceErrorV1 QueryValueError() =>
        Error(
            PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
            PerspectiveSafeSourceSectionV1.Entities);

    private enum I6C3EndpointResolution : byte
    {
        Resolved = 0,
        Hidden = 1,
        PendingI6C5 = 2,
        Unproven = 3
    }

    private readonly record struct I6C3OrdinalCandidate(
        MirrorCardSnapshotV1 Card,
        byte Controller,
        PerspectiveSafeSemanticZoneV1 Zone,
        uint CardCode,
        uint SourceSequence);

    private static int OrdinalPileRank(PerspectiveSafeSemanticZoneV1 zone) =>
        zone == PerspectiveSafeSemanticZoneV1.Hand ? 0 : 1;

    private static PublicSemanticZoneV1 ToPublicOrdinalZone(
        PerspectiveSafeSemanticZoneV1 zone) =>
        zone == PerspectiveSafeSemanticZoneV1.Hand
            ? PublicSemanticZoneV1.Hand
            : PublicSemanticZoneV1.ExtraDeck;

    private static bool IsKnownValue(MirrorProvenanceV1 provenance) =>
        provenance is MirrorProvenanceV1.PublicProtocolFact or
            MirrorProvenanceV1.PerspectivePrivateFact or
            MirrorProvenanceV1.DerivedFromProvenPublicFacts;

    private sealed class I6C2EntityCandidate
    {
        internal I6C2EntityCandidate(
            byte absoluteController,
            PerspectiveSafeSemanticZoneV1 semanticZone,
            uint sourceSequence,
            uint? cardCode,
            byte? owner,
            bool sequenceVisible,
            PerspectiveSafePositionV1 position,
            PerspectiveSafeCardPropertiesV1? current)
        {
            AbsoluteController = absoluteController;
            SemanticZone = semanticZone;
            SourceSequence = sourceSequence;
            CardCode = cardCode;
            Owner = owner;
            SequenceVisible = sequenceVisible;
            Position = position;
            Current = current;
        }

        internal byte AbsoluteController { get; }

        internal PerspectiveSafeSemanticZoneV1 SemanticZone { get; }

        internal uint SourceSequence { get; }

        internal uint? CardCode { get; }

        internal byte? Owner { get; }

        internal bool SequenceVisible { get; }

        internal PerspectiveSafePositionV1 Position { get; }

        internal PerspectiveSafeCardPropertiesV1? Current { get; }

        internal bool NeedsPublicOrdinal { get; set; }

        internal string Locator { get; set; } = string.Empty;
    }

    private static bool IsPlayer(byte? value) => !value.HasValue || value <= 1;

    private static bool IsLocator(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character < '\u0020' || character == '\u007f')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSemanticZone(
        PerspectiveSafeSemanticZoneV1 value,
        bool allowUnknown) =>
        IsDefined(value) &&
        (allowUnknown || value != PerspectiveSafeSemanticZoneV1.Unknown);

    private static bool IsDefined<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        Enum.IsDefined(value);

    private static int CompareZones(
        PerspectiveSafeZoneV1 left,
        PerspectiveSafeZoneV1 right)
    {
        int result = left.Player.CompareTo(right.Player);
        if (result != 0)
        {
            return result;
        }

        result = ((byte)left.Kind).CompareTo((byte)right.Kind);
        if (result != 0)
        {
            return result;
        }

        result = left.TotalCount.CompareTo(right.TotalCount);
        if (result != 0)
        {
            return result;
        }

        result = left.PublicIdentityCount.CompareTo(right.PublicIdentityCount);
        if (result != 0)
        {
            return result;
        }

        result = left.HiddenCount.CompareTo(right.HiddenCount);
        if (result != 0)
        {
            return result;
        }

        return left.PlayerObservableOrder.CompareTo(right.PlayerObservableOrder);
    }

    private static int CompareRelationships(
        PerspectiveSafeRelationshipV1 left,
        PerspectiveSafeRelationshipV1 right)
    {
        int result = ((byte)left.Kind).CompareTo((byte)right.Kind);
        if (result != 0)
        {
            return result;
        }

        result = string.CompareOrdinal(left.Source, right.Source);
        return result != 0
            ? result
            : string.CompareOrdinal(left.Target, right.Target);
    }

    private static int CompareCounters(
        PerspectiveSafeCounterV1 left,
        PerspectiveSafeCounterV1 right)
    {
        int result = left.Type.CompareTo(right.Type);
        return result != 0 ? result : left.Count.CompareTo(right.Count);
    }

    private static PerspectiveSafeFrameSourceErrorV1 Error(
        PerspectiveSafeFrameSourceErrorCodeV1 code,
        PerspectiveSafeSourceSectionV1 section) =>
        new(code, section);
}
