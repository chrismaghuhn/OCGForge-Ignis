namespace OCGForge.Ignis.Gameplay;

internal static class PerspectiveSafeMatchContextValidationV1
{
    internal static bool TryValidate(
        PerspectiveSafeMatchContextV1 context,
        byte expectedPerspectivePlayer,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        if (context.PerspectivePlayer > 1 ||
            context.PerspectivePlayer != expectedPerspectivePlayer)
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidPlayer,
                PerspectiveSafeSourceSectionV1.MatchContext);
            return false;
        }

        if (context.Knowledge.OwnDecklistKnown != context.OwnDeck.Known ||
            context.Knowledge.OpponentDecklistKnown != context.OpponentDeck.Known)
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidDeckState,
                PerspectiveSafeSourceSectionV1.MatchContext);
            return false;
        }

        const ulong duelPzone = 0x800;
        const ulong duelSeparatePzone = 0x1000;
        if ((context.DuelFlags & duelSeparatePzone) != 0 &&
            (context.DuelFlags & duelPzone) == 0)
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                PerspectiveSafeSourceSectionV1.MatchContext);
            return false;
        }

        if (!TryValidateDeck(context.OwnDeck, out error) ||
            !TryValidateDeck(context.OpponentDeck, out error))
        {
            return false;
        }

        error = default;
        return true;
    }

    private static bool TryValidateDeck(
        PerspectiveSafeDeckV1 deck,
        out PerspectiveSafeFrameSourceErrorV1 error)
    {
        if (!deck.Known &&
            (deck.MainDeck.Count != 0 || deck.ExtraDeck.Count != 0))
        {
            error = Error(
                PerspectiveSafeFrameSourceErrorCodeV1.InvalidDeckState,
                PerspectiveSafeSourceSectionV1.MatchContext);
            return false;
        }

        if (deck.MainDeck.Any(value => value == 0) ||
            deck.ExtraDeck.Any(value => value == 0))
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

    private static PerspectiveSafeFrameSourceErrorV1 Error(
        PerspectiveSafeFrameSourceErrorCodeV1 code,
        PerspectiveSafeSourceSectionV1 section) =>
        new(code, section);
}
