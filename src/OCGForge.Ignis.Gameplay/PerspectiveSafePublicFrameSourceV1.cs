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

        return PerspectiveSafeFrameSourceResultV1.Success(
            new PerspectiveSafeFrameV1(input));
    }

    private static PerspectiveSafeFrameSourceResultV1 Failure(
        PerspectiveSafeFrameSourceErrorCodeV1 code,
        PerspectiveSafeSourceSectionV1 section) =>
        PerspectiveSafeFrameSourceResultV1.Failure(
            new PerspectiveSafeFrameSourceErrorV1(code, section));

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

            if (string.IsNullOrEmpty(entity.Locator))
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

            if (!IsPlayer(link.ActivatingPlayer) ||
                (link.Source is not null && !IsLocator(link.Source)) ||
                (link.ActivationZone.HasValue &&
                 !IsSemanticZone(link.ActivationZone.Value, allowUnknown: false)) ||
                !ValidateSortedLocators(
                    link.Targets,
                    PerspectiveSafeSourceSectionV1.Chain,
                    out error))
            {
                if (error.Code == 0)
                {
                    error = Error(
                        link.ActivatingPlayer is > 1
                            ? PerspectiveSafeFrameSourceErrorCodeV1.InvalidPlayer
                            : link.ActivationZone.HasValue
                                ? PerspectiveSafeFrameSourceErrorCodeV1.UnknownEnum
                                : PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator,
                        PerspectiveSafeSourceSectionV1.Chain);
                }

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

            if (!IsPlayer(visibleEvent.Player) ||
                !IsPlayer(visibleEvent.Winner) ||
                (visibleEvent.EntityLocator is not null &&
                 !IsLocator(visibleEvent.EntityLocator)) ||
                (visibleEvent.FromZone.HasValue &&
                 !IsSemanticZone(visibleEvent.FromZone.Value, allowUnknown: false)) ||
                (visibleEvent.ToZone.HasValue &&
                 !IsSemanticZone(visibleEvent.ToZone.Value, allowUnknown: false)) ||
                !ValidateSortedLocators(
                    visibleEvent.Targets,
                    PerspectiveSafeSourceSectionV1.VisibleEvents,
                    out error))
            {
                if (error.Code == 0)
                {
                    error = Error(
                        !IsPlayer(visibleEvent.Player) ||
                        !IsPlayer(visibleEvent.Winner)
                            ? PerspectiveSafeFrameSourceErrorCodeV1.InvalidPlayer
                            : visibleEvent.FromZone.HasValue ||
                              visibleEvent.ToZone.HasValue
                                ? PerspectiveSafeFrameSourceErrorCodeV1.UnknownEnum
                                : PerspectiveSafeFrameSourceErrorCodeV1.InvalidLocator,
                        PerspectiveSafeSourceSectionV1.VisibleEvents);
                }

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

    private static bool IsPlayer(byte? value) => !value.HasValue || value <= 1;

    private static bool IsLocator(string value) => !string.IsNullOrEmpty(value);

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
