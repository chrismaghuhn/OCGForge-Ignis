namespace OCGForge.Ignis.Gameplay;

internal static class FlatPromptCardCorrelationV1
{
    internal static bool TryCorrelate(
        MirrorSnapshotV1 capturedMirror,
        PublicStateSnapshotV1 acceptedSnapshot,
        uint sourceCardCode,
        ModernLocInfoV1 sourceLocation,
        out FlatPromptCardCorrelationResultV1? result,
        out FlatPromptErrorCodeV1 error)
    {
        ArgumentNullException.ThrowIfNull(capturedMirror);
        ArgumentNullException.ThrowIfNull(acceptedSnapshot);
        result = null;
        error = FlatPromptErrorCodeV1.None;

        if (!MirrorAddressNormalizationV1.TryNormalize(
                sourceLocation,
                out MirrorAddressNormalizationV1 normalized,
                out GameplayErrorCode normalizationError))
        {
            error = MapNormalizationError(normalizationError);
            return false;
        }

        MirrorCardSnapshotV1[] resolvedCards = capturedMirror.Cards
            .Where(card =>
                card.Zone == normalized.Zone &&
                card.Sequence == normalized.Sequence &&
                card.IsOverlay == normalized.IsOverlay &&
                (!normalized.IsOverlay ||
                 card.OverlayIndex == normalized.OverlayIndex) &&
                TryGetAbsolutePlayer(
                    capturedMirror.Perspective,
                    card.Controller,
                    out byte absolutePlayer) &&
                absolutePlayer == normalized.Controller)
            .ToArray();
        if (resolvedCards.Length != 1)
        {
            error = FlatPromptErrorCodeV1.UnprovenPublicReference;
            return false;
        }

        MirrorCardSnapshotV1 resolvedCard = resolvedCards[0];
        if (normalized.IsOverlay)
        {
            return TryCorrelateOverlay(
                acceptedSnapshot,
                normalized.Controller,
                resolvedCard,
                sourceCardCode,
                out result,
                out error);
        }

        return normalized.Zone switch
        {
            MirrorZoneV1.Hand or MirrorZoneV1.ExtraDeck =>
                TryCorrelatePile(
                    acceptedSnapshot,
                    normalized.Controller,
                    normalized.Zone,
                    resolvedCard,
                    sourceCardCode,
                    out result,
                    out error),
            MirrorZoneV1.MainDeck => Fail(
                FlatPromptErrorCodeV1.UnprovenPublicReference,
                out result,
                out error),
            _ => TryCorrelateIndexed(
                acceptedSnapshot,
                normalized.Controller,
                normalized.Zone,
                resolvedCard,
                sourceCardCode,
                out result,
                out error)
        };
    }

    private static bool TryCorrelateIndexed(
        PublicStateSnapshotV1 acceptedSnapshot,
        byte absolutePlayer,
        MirrorZoneV1 mirrorZone,
        MirrorCardSnapshotV1 resolvedCard,
        uint sourceCardCode,
        out FlatPromptCardCorrelationResultV1? result,
        out FlatPromptErrorCodeV1 error)
    {
        result = null;
        error = FlatPromptErrorCodeV1.None;
        List<PublicCardStateV1> matches = new();
        foreach (PublicCardStateV1 acceptedCard in acceptedSnapshot.Cards)
        {
            if (acceptedCard.AbsolutePlayer != absolutePlayer ||
                !IsIndexedZoneCompatible(mirrorZone, acceptedCard.Zone) ||
                !PublicSemanticLocatorV1.TryCreateIndexed(
                    absolutePlayer,
                    acceptedCard.Zone,
                    resolvedCard.Sequence,
                    out PublicSemanticLocatorV1? expectedLocator) ||
                expectedLocator is null ||
                acceptedCard.Locator != expectedLocator)
            {
                continue;
            }

            matches.Add(acceptedCard);
        }

        return CompleteCorrelation(
            matches,
            sourceCardCode,
            out result,
            out error);
    }

    private static bool TryCorrelatePile(
        PublicStateSnapshotV1 acceptedSnapshot,
        byte absolutePlayer,
        MirrorZoneV1 mirrorZone,
        MirrorCardSnapshotV1 resolvedCard,
        uint sourceCardCode,
        out FlatPromptCardCorrelationResultV1? result,
        out FlatPromptErrorCodeV1 error)
    {
        result = null;
        error = FlatPromptErrorCodeV1.None;
        if (!IsKnownProvenCardCode(resolvedCard.CardCode))
        {
            return Fail(
                FlatPromptErrorCodeV1.UnprovenPublicReference,
                out result,
                out error);
        }

        PublicSemanticZoneV1 publicZone = mirrorZone == MirrorZoneV1.Hand
            ? PublicSemanticZoneV1.Hand
            : PublicSemanticZoneV1.ExtraDeck;
        List<PublicCardStateV1> matches = acceptedSnapshot.Cards
            .Where(card =>
                card.AbsolutePlayer == absolutePlayer &&
                card.Zone == publicZone &&
                card.CardCode.HasValue &&
                card.CardCode.Value == resolvedCard.CardCode.Value)
            .ToList();
        return CompleteCorrelation(
            matches,
            sourceCardCode,
            out result,
            out error);
    }

    private static bool TryCorrelateOverlay(
        PublicStateSnapshotV1 acceptedSnapshot,
        byte absolutePlayer,
        MirrorCardSnapshotV1 resolvedCard,
        uint sourceCardCode,
        out FlatPromptCardCorrelationResultV1? result,
        out FlatPromptErrorCodeV1 error)
    {
        result = null;
        error = FlatPromptErrorCodeV1.None;
        if (!PublicSemanticLocatorV1.TryCreateOverlay(
                absolutePlayer,
                resolvedCard.Sequence,
                resolvedCard.OverlayIndex,
                out PublicSemanticLocatorV1? expectedLocator) ||
            expectedLocator is null)
        {
            return Fail(
                FlatPromptErrorCodeV1.UnprovenPublicReference,
                out result,
                out error);
        }

        List<PublicCardStateV1> matches = acceptedSnapshot.Cards
            .Where(card =>
                card.AbsolutePlayer == absolutePlayer &&
                card.Zone == PublicSemanticZoneV1.Overlay &&
                card.Locator == expectedLocator)
            .ToList();
        return CompleteCorrelation(
            matches,
            sourceCardCode,
            out result,
            out error);
    }

    private static bool CompleteCorrelation(
        List<PublicCardStateV1> matches,
        uint sourceCardCode,
        out FlatPromptCardCorrelationResultV1? result,
        out FlatPromptErrorCodeV1 error)
    {
        result = null;
        error = FlatPromptErrorCodeV1.None;
        if (matches.Count != 1)
        {
            error = FlatPromptErrorCodeV1.UnprovenPublicReference;
            return false;
        }

        PublicCardStateV1 acceptedCard = matches[0];
        uint? safeCardCode = sourceCardCode != 0 &&
                             acceptedCard.CardCode.HasValue &&
                             acceptedCard.CardCode.Value == sourceCardCode
            ? acceptedCard.CardCode.Value
            : null;
        result = new FlatPromptCardCorrelationResultV1(
            acceptedCard.Locator,
            safeCardCode);
        return true;
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

    private static bool IsKnownProvenCardCode(MirrorValueV1<uint> value) =>
        value.IsKnown &&
        value.Value != 0 &&
        value.Provenance is
            MirrorProvenanceV1.PublicProtocolFact or
            MirrorProvenanceV1.PerspectivePrivateFact or
            MirrorProvenanceV1.DerivedFromProvenPublicFacts;

    private static bool TryGetAbsolutePlayer(
        GameplayPerspectiveV1? perspective,
        MirrorParticipantRoleV1 role,
        out byte absolutePlayer) =>
        PublicSemanticLocatorV1.TryGetAbsolutePlayer(
            perspective,
            role,
            out absolutePlayer);

    private static FlatPromptErrorCodeV1 MapNormalizationError(
        GameplayErrorCode error) =>
        error switch
        {
            GameplayErrorCode.InvalidParticipant =>
                FlatPromptErrorCodeV1.InvalidParticipant,
            GameplayErrorCode.InvalidLocation or
                GameplayErrorCode.StateCapacityExceeded =>
                FlatPromptErrorCodeV1.InvalidLocation,
            _ => FlatPromptErrorCodeV1.UnprovenPublicReference
        };

    private static bool Fail(
        FlatPromptErrorCodeV1 failure,
        out FlatPromptCardCorrelationResultV1? result,
        out FlatPromptErrorCodeV1 error)
    {
        result = null;
        error = failure;
        return false;
    }
}

internal sealed class FlatPromptCardCorrelationResultV1
{
    internal FlatPromptCardCorrelationResultV1(
        PublicSemanticLocatorV1 acceptedLocator,
        uint? safeCardCode)
    {
        AcceptedLocator = acceptedLocator ??
            throw new ArgumentNullException(nameof(acceptedLocator));
        SafeCardCode = safeCardCode;
    }

    internal PublicSemanticLocatorV1 AcceptedLocator { get; }

    internal uint? SafeCardCode { get; }
}
