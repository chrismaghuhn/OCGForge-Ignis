namespace OCGForge.Ignis.Gameplay.Tests.Fixtures;

internal enum I6GLocatorParityErrorCodeV1 : byte
{
    None = 0,
    InvalidInput = 1,
    UnsupportedCandidate = 2,
    PublicReferenceMissing = 3
}

internal enum I6GLocatorParityClassificationV1 : byte
{
    ExactMatch = 0,
    OrdinalScopeDrift = 1,
    ZoneMappingDrift = 2,
    StaleFrame = 3,
    EntityMissing = 4,
    AmbiguousEntity = 5,
    Other = 6
}

internal readonly record struct I6GLocatorParityObservationV1(
    int CandidateIndex,
    FlatPromptChoiceKindV1 ChoiceKind,
    byte AbsolutePlayer,
    PublicSemanticZoneV1 PublicZone,
    bool I4PublicStateLocatorPresent,
    int I4PublicStateLocatorMatchCount,
    int I6C5ExactLocatorMatchCount,
    int I6C5SamePublicCardMatchCount,
    I6GLocatorParityClassificationV1 Classification);

internal readonly record struct I6GLocatorParityCharacterizationResultV1(
    bool IsSuccess,
    I6GLocatorParityErrorCodeV1 Error,
    I6GLocatorParityObservationV1 Observation);

internal static class I6GLocatorParityCharacterizationV1
{
    internal static I6GLocatorParityCharacterizationResultV1 CompareCandidate(
        int candidateIndex,
        FlatPublicCandidateDescriptorV1? candidate,
        PublicStateSnapshotV1? i4PublicState,
        IReadOnlyList<PerspectiveSafeEntityV1>? i6C5EntitySource)
    {
        if (candidate is null ||
            i4PublicState is null ||
            i6C5EntitySource is null)
        {
            return Failure(I6GLocatorParityErrorCodeV1.InvalidInput);
        }

        PublicSemanticLocatorV1? locator = GetCandidateLocator(candidate);
        if (locator is null ||
            !TryGetPublicZone(locator, out byte absolutePlayer, out PublicSemanticZoneV1 publicZone))
        {
            return Failure(I6GLocatorParityErrorCodeV1.UnsupportedCandidate);
        }

        PublicCardStateV1[] i4Matches = i4PublicState.Cards
            .Where(card => card.Locator == locator)
            .ToArray();
        if (i4Matches.Length != 1)
        {
            return Failure(I6GLocatorParityErrorCodeV1.PublicReferenceMissing);
        }

        PublicCardStateV1 i4Card = i4Matches[0];
        PerspectiveSafeEntityV1[] samePublicCard = i6C5EntitySource
            .Where(entity =>
                entity.IdentityKnown &&
                entity.Passcode.HasValue &&
                i4Card.CardCode.HasValue &&
                entity.Passcode.Value == i4Card.CardCode.Value &&
                entity.Controller == i4Card.AbsolutePlayer &&
                TryMapZone(entity.Zone, out PublicSemanticZoneV1 mappedZone) &&
                mappedZone == i4Card.Zone)
            .ToArray();
        int exactLocatorMatchCount = i6C5EntitySource.Count(entity =>
            string.Equals(
                entity.Locator,
                locator.Value,
                StringComparison.Ordinal));

        I6GLocatorParityClassificationV1 classification =
            Classify(
                locator.Value,
                exactLocatorMatchCount,
                samePublicCard);
        return new(
            true,
            I6GLocatorParityErrorCodeV1.None,
            new(
                candidateIndex,
                candidate.ChoiceKind,
                absolutePlayer,
                publicZone,
                true,
                i4Matches.Length,
                exactLocatorMatchCount,
                samePublicCard.Length,
                classification));
    }

    private static I6GLocatorParityClassificationV1 Classify(
        string i4Locator,
        int exactLocatorMatchCount,
        IReadOnlyList<PerspectiveSafeEntityV1> samePublicCard)
    {
        if (exactLocatorMatchCount == 1)
        {
            return I6GLocatorParityClassificationV1.ExactMatch;
        }

        if (exactLocatorMatchCount > 1)
        {
            return I6GLocatorParityClassificationV1.AmbiguousEntity;
        }

        if (samePublicCard.Count == 0)
        {
            return I6GLocatorParityClassificationV1.EntityMissing;
        }

        if (IsPublicOrdinalLocator(i4Locator) &&
            samePublicCard.Any(entity => !IsSameLocator(entity.Locator, i4Locator)))
        {
            return I6GLocatorParityClassificationV1.OrdinalScopeDrift;
        }

        return I6GLocatorParityClassificationV1.Other;
    }

    private static bool IsSameLocator(string actual, string expected) =>
        string.Equals(actual, expected, StringComparison.Ordinal);

    private static bool IsPublicOrdinalLocator(string locator)
    {
        string[] parts = locator.Split(':');
        return parts.Length == 5 &&
            string.Equals(parts[2], "public", StringComparison.Ordinal);
    }

    private static PublicSemanticLocatorV1? GetCandidateLocator(
        FlatPublicCandidateDescriptorV1 candidate) =>
        candidate switch
        {
            FlatIdleCardActionPublicCandidateBaseV1 idle =>
                idle.PublicSemanticCardLocator,
            FlatIdleActivatablePublicCandidateBaseV1 activatable =>
                activatable.PublicSemanticCardLocator,
            _ => null
        };

    private static bool TryGetPublicZone(
        PublicSemanticLocatorV1 locator,
        out byte absolutePlayer,
        out PublicSemanticZoneV1 publicZone)
    {
        absolutePlayer = 0;
        publicZone = default;
        string[] parts = locator.Value.Split(':');
        if (parts.Length < 2 ||
            parts[0] is not ("p0" or "p1"))
        {
            return false;
        }

        absolutePlayer = parts[0] == "p0" ? (byte)0 : (byte)1;
        return parts[1] switch
        {
            "HAND" => SetZone(PublicSemanticZoneV1.Hand, out publicZone),
            "EXTRA_DECK" =>
                SetZone(PublicSemanticZoneV1.ExtraDeck, out publicZone),
            "MONSTER_ZONE" =>
                SetZone(PublicSemanticZoneV1.MonsterZone, out publicZone),
            "SPELL_TRAP_ZONE" =>
                SetZone(PublicSemanticZoneV1.SpellTrapZone, out publicZone),
            "FIELD_ZONE" =>
                SetZone(PublicSemanticZoneV1.FieldZone, out publicZone),
            "PENDULUM_RELEVANT_STATE" =>
                SetZone(PublicSemanticZoneV1.PendulumRelevantState, out publicZone),
            "GRAVEYARD" =>
                SetZone(PublicSemanticZoneV1.Graveyard, out publicZone),
            "BANISHED" =>
                SetZone(PublicSemanticZoneV1.Banished, out publicZone),
            _ => false
        };
    }

    private static bool TryMapZone(
        PerspectiveSafeSemanticZoneV1 source,
        out PublicSemanticZoneV1 mapped) =>
        source switch
        {
            PerspectiveSafeSemanticZoneV1.Hand =>
                SetZone(PublicSemanticZoneV1.Hand, out mapped),
            PerspectiveSafeSemanticZoneV1.ExtraDeck =>
                SetZone(PublicSemanticZoneV1.ExtraDeck, out mapped),
            PerspectiveSafeSemanticZoneV1.MonsterZone =>
                SetZone(PublicSemanticZoneV1.MonsterZone, out mapped),
            PerspectiveSafeSemanticZoneV1.SpellTrapZone =>
                SetZone(PublicSemanticZoneV1.SpellTrapZone, out mapped),
            PerspectiveSafeSemanticZoneV1.FieldZone =>
                SetZone(PublicSemanticZoneV1.FieldZone, out mapped),
            PerspectiveSafeSemanticZoneV1.PendulumRelevant =>
                SetZone(PublicSemanticZoneV1.PendulumRelevantState, out mapped),
            PerspectiveSafeSemanticZoneV1.Graveyard =>
                SetZone(PublicSemanticZoneV1.Graveyard, out mapped),
            PerspectiveSafeSemanticZoneV1.Banished =>
                SetZone(PublicSemanticZoneV1.Banished, out mapped),
            _ => SetZone(default, out mapped, false)
        };

    private static bool SetZone(
        PublicSemanticZoneV1 value,
        out PublicSemanticZoneV1 zone)
    {
        zone = value;
        return true;
    }

    private static bool SetZone(
        PublicSemanticZoneV1 value,
        out PublicSemanticZoneV1 zone,
        bool result = true)
    {
        zone = value;
        return result;
    }

    private static I6GLocatorParityCharacterizationResultV1 Failure(
        I6GLocatorParityErrorCodeV1 error) =>
        new(false, error, default);
}
