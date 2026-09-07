using System.Collections.ObjectModel;

namespace OCGForge.Ignis.Gameplay;

/// <summary>
/// Atomic, source-side history for perspective-safe visible events. The
/// ledger stores typed event values and private event-time location facts only;
/// it is not an OCGForge codec and does not expose engine identity.
/// </summary>
internal sealed class PerspectiveSafeEventLedgerV1
{
    private const byte LocationDeck = 0x01;
    private const byte LocationHand = 0x02;
    private const byte LocationMonster = 0x04;
    private const byte LocationSpellTrap = 0x08;
    private const byte LocationGraveyard = 0x10;
    private const byte LocationBanished = 0x20;
    private const byte LocationExtra = 0x40;
    private const byte LocationOverlay = 0x80;
    private const uint PositionFaceUpMask = 0x05;
    private const uint ReasonDestroy = 0x01;

    private readonly LedgerEntry[] entries;
    private readonly ReadOnlyCollection<PerspectiveSafeVisibleEventV1> eventsView;

    private PerspectiveSafeEventLedgerV1(
        IEnumerable<LedgerEntry> entries,
        ulong nextEventIndex)
    {
        this.entries = entries.ToArray();
        eventsView = Array.AsReadOnly(
            this.entries.Select(entry => entry.Event).ToArray());
        NextEventIndex = nextEventIndex;
    }

    internal static PerspectiveSafeEventLedgerV1 Empty { get; } =
        new(Array.Empty<LedgerEntry>(), 0);

    internal static PerspectiveSafeEventLedgerV1 ForTesting(
        ulong nextEventIndex) =>
        new(Array.Empty<LedgerEntry>(), nextEventIndex);

    internal IReadOnlyList<PerspectiveSafeVisibleEventV1> Events => eventsView;

    internal ulong NextEventIndex { get; }

    internal IReadOnlyList<PerspectiveSafeEventSourceFactV1> SourceFacts =>
        entries.Select(entry => entry.SourceFact).ToArray();

    internal static bool TryAppend(
        PerspectiveSafeEventLedgerV1 previous,
        GameplayMessageV1 message,
        GameplayPerspectiveV1 perspective,
        out PerspectiveSafeEventLedgerV1 next,
        out GameplayErrorCode error)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(perspective);

        if (!TryBuildDrafts(message, perspective, out List<EventDraft> drafts, out error))
        {
            next = previous;
            return false;
        }

        if (drafts.Count == 0)
        {
            next = previous;
            error = GameplayErrorCode.None;
            return true;
        }

        ulong eventCount = (ulong)drafts.Count;
        if (previous.NextEventIndex > ulong.MaxValue - eventCount)
        {
            next = previous;
            error = GameplayErrorCode.ArithmeticFailure;
            return false;
        }

        List<LedgerEntry> staged = new(previous.entries.Length + drafts.Count);
        staged.AddRange(previous.entries);
        ulong nextEventIndex = previous.NextEventIndex;
        foreach (EventDraft draft in drafts)
        {
            PerspectiveSafeVisibleEventV1 value = new(
                nextEventIndex,
                draft.Kind,
                draft.Player,
                draft.EntityLocator,
                draft.PublicPasscode,
                draft.FromZone,
                draft.ToZone,
                draft.Count,
                draft.Amount,
                draft.CounterType,
                draft.Phase,
                draft.Winner,
                draft.WinReason,
                draft.EffectDescription,
                draft.Targets);
            staged.Add(new LedgerEntry(value, draft.SourceLocations));
            nextEventIndex++;
        }

        next = new PerspectiveSafeEventLedgerV1(staged, nextEventIndex);
        error = GameplayErrorCode.None;
        return true;
    }

    private static bool TryBuildDrafts(
        GameplayMessageV1 message,
        GameplayPerspectiveV1 perspective,
        out List<EventDraft> drafts,
        out GameplayErrorCode error)
    {
        drafts = new List<EventDraft>();
        error = GameplayErrorCode.None;

        switch (message.Kind)
        {
            case GameplayMessageKindV1.Win:
                drafts.Add(new EventDraft(
                    PerspectiveSafeVisibleEventKindV1.Win,
                    winner: message.Win.Player,
                    winReason: message.Win.WinType));
                return true;

            case GameplayMessageKindV1.NewTurn:
                drafts.Add(new EventDraft(
                    PerspectiveSafeVisibleEventKindV1.TurnStarted,
                    player: message.NewTurn.Player));
                return true;

            case GameplayMessageKindV1.NewPhase:
                drafts.Add(new EventDraft(
                    PerspectiveSafeVisibleEventKindV1.PhaseChanged,
                    phase: message.NewPhase.Phase));
                return true;

            case GameplayMessageKindV1.Move:
                return TryBuildMoveDrafts(message.Move, perspective, drafts, out error);

            case GameplayMessageKindV1.PosChange:
                return TryBuildPositionDraft(message.PositionChange, perspective, drafts, out error);

            case GameplayMessageKindV1.Set:
                return TryBuildLocationDraft(
                    PerspectiveSafeVisibleEventKindV1.Set,
                    message.Set.CardCode,
                    message.Set.Location,
                    perspective,
                    drafts,
                    out error);

            case GameplayMessageKindV1.Summoning:
            case GameplayMessageKindV1.SpecialSummoning:
            case GameplayMessageKindV1.FlipSummoning:
                return TryBuildLocationDraft(
                    PerspectiveSafeVisibleEventKindV1.Summoned,
                    message.Summoning.CardCode,
                    message.Summoning.Location,
                    perspective,
                    drafts,
                    out error);

            case GameplayMessageKindV1.Summoned:
            case GameplayMessageKindV1.SpecialSummoned:
            case GameplayMessageKindV1.FlipSummoned:
                drafts.Add(new EventDraft(PerspectiveSafeVisibleEventKindV1.Summoned));
                return true;

            case GameplayMessageKindV1.Draw:
                return TryBuildDrawDrafts(message.Draw!, perspective, drafts, out error);

            case GameplayMessageKindV1.ConfirmCards:
            case GameplayMessageKindV1.ConfirmDeckTop:
            case GameplayMessageKindV1.ConfirmExtraTop:
                return TryBuildConfirmDrafts(message.Confirm!, perspective, drafts, out error);

            case GameplayMessageKindV1.ShuffleDeck:
            case GameplayMessageKindV1.ShuffleHand:
            case GameplayMessageKindV1.ShuffleExtra:
            case GameplayMessageKindV1.ShuffleSetCard:
            case GameplayMessageKindV1.ReverseDeck:
                return TryBuildShuffleDrafts(message.Shuffle!, drafts, out error);

            case GameplayMessageKindV1.Damage:
            case GameplayMessageKindV1.Recover:
            case GameplayMessageKindV1.LpUpdate:
                if (message.LifePoints.Amount > int.MaxValue)
                {
                    error = GameplayErrorCode.ArithmeticFailure;
                    return false;
                }

                int amount = (int)message.LifePoints.Amount;
                if (message.Kind == GameplayMessageKindV1.Damage)
                {
                    amount = -amount;
                }

                drafts.Add(new EventDraft(
                    PerspectiveSafeVisibleEventKindV1.LifePointsChanged,
                    player: message.LifePoints.Player,
                    amount: amount));
                return true;

            case GameplayMessageKindV1.Chaining:
                return TryBuildChainingDraft(message.Chaining, perspective, drafts, out error);

            case GameplayMessageKindV1.Chained:
            case GameplayMessageKindV1.ChainSolving:
            case GameplayMessageKindV1.ChainSolved:
                drafts.Add(new EventDraft(
                    message.Kind == GameplayMessageKindV1.Chained
                        ? PerspectiveSafeVisibleEventKindV1.ChainActivated
                        : PerspectiveSafeVisibleEventKindV1.ChainResolved,
                    count: message.ChainSize.ChainSize));
                return true;

            case GameplayMessageKindV1.ChainEnd:
                drafts.Add(new EventDraft(PerspectiveSafeVisibleEventKindV1.ChainEnded));
                return true;

            case GameplayMessageKindV1.BecomeTarget:
                return TryBuildBecomeTargetDraft(message.BecomeTarget!, perspective, drafts, out error);

            case GameplayMessageKindV1.Equip:
                return TryBuildRelationshipDraft(
                    PerspectiveSafeVisibleEventKindV1.Equipped,
                    message.Equip.Card,
                    message.Equip.Target,
                    perspective,
                    drafts,
                    out error);

            case GameplayMessageKindV1.CardTarget:
            case GameplayMessageKindV1.CancelTarget:
                return TryBuildRelationshipDraft(
                    PerspectiveSafeVisibleEventKindV1.Targeted,
                    message.CardTarget.Source,
                    message.CardTarget.Target,
                    perspective,
                    drafts,
                    out error);

            case GameplayMessageKindV1.AddCounter:
            case GameplayMessageKindV1.RemoveCounter:
                return TryBuildCounterDraft(message.Counter, perspective, drafts, out error);

            case GameplayMessageKindV1.Unequip:
                // MSG_UNEQUIP remains an I3 compatibility-only message for
                // the pinned runtime. It is deliberately not an I6C4 event.
                return true;

            case GameplayMessageKindV1.Start:
            case GameplayMessageKindV1.UpdateData:
            case GameplayMessageKindV1.UpdateCard:
            case GameplayMessageKindV1.Swap:
            case GameplayMessageKindV1.ChainNegated:
            case GameplayMessageKindV1.ChainDisabled:
            case GameplayMessageKindV1.PayLpCost:
                return true;

            default:
                error = GameplayErrorCode.UnsupportedMessage;
                return false;
        }
    }

    private static bool TryBuildMoveDrafts(
        GameplayMovePayloadV1 payload,
        GameplayPerspectiveV1 perspective,
        List<EventDraft> drafts,
        out GameplayErrorCode error)
    {
        error = GameplayErrorCode.None;
        if (!TryResolveLocation(
                payload.Previous,
                perspective,
                explicitReveal: false,
                out PerspectiveSafeSemanticZoneV1? fromZone,
                out _,
                out bool fromEmpty))
        {
            error = GameplayErrorCode.InvalidLocation;
            return false;
        }

        if (!TryResolveLocation(
                payload.Current,
                perspective,
                explicitReveal: false,
                out PerspectiveSafeSemanticZoneV1? toZone,
                out string? entity,
                out bool toEmpty))
        {
            error = GameplayErrorCode.InvalidLocation;
            return false;
        }

        PerspectiveSafeVisibleEventKindV1 kind =
            !toEmpty && toZone == PerspectiveSafeSemanticZoneV1.Banished
                ? PerspectiveSafeVisibleEventKindV1.CardBanished
                : !toEmpty &&
                  toZone == PerspectiveSafeSemanticZoneV1.Graveyard &&
                  (payload.Reason & ReasonDestroy) != 0
                    ? PerspectiveSafeVisibleEventKindV1.CardDestroyed
                    : !fromEmpty &&
                      (fromZone == PerspectiveSafeSemanticZoneV1.Banished ||
                       fromZone == PerspectiveSafeSemanticZoneV1.Graveyard)
                        ? PerspectiveSafeVisibleEventKindV1.CardReturned
                        : PerspectiveSafeVisibleEventKindV1.CardMoved;

        uint? publicPasscode =
            IsEventIdentityVisible(payload.Previous, payload.CardCode) ||
            IsEventIdentityVisible(payload.Current, payload.CardCode)
                ? payload.CardCode
                : null;
        drafts.Add(new EventDraft(
            kind,
            player: payload.Current.Controller,
            entityLocator: entity,
            publicPasscode: publicPasscode,
            fromZone: fromZone,
            toZone: toZone,
            sourceLocations: new[] { payload.Previous, payload.Current }));
        return true;
    }

    private static bool TryBuildPositionDraft(
        GameplayPositionChangePayloadV1 payload,
        GameplayPerspectiveV1 perspective,
        List<EventDraft> drafts,
        out GameplayErrorCode error)
    {
        error = GameplayErrorCode.None;
        ModernLocInfoV1 location = new(
            payload.Controller,
            payload.Location,
            payload.Sequence,
            payload.CurrentPosition);
        if (!TryResolveLocation(
                location,
                perspective,
                explicitReveal: false,
                out PerspectiveSafeSemanticZoneV1? zone,
                out string? entity,
                out _))
        {
            error = GameplayErrorCode.InvalidLocation;
            return false;
        }

        uint? publicPasscode =
            IsEventIdentityVisible(location, payload.CardCode) ||
            (payload.PreviousPosition & PositionFaceUpMask) != 0 ||
            (payload.CurrentPosition & PositionFaceUpMask) != 0
                ? payload.CardCode
                : null;
        drafts.Add(new EventDraft(
            PerspectiveSafeVisibleEventKindV1.PositionChanged,
            player: payload.Controller,
            entityLocator: entity,
            publicPasscode: publicPasscode,
            toZone: zone,
            sourceLocations: new[] { location }));
        return true;
    }

    private static bool TryBuildLocationDraft(
        PerspectiveSafeVisibleEventKindV1 kind,
        uint cardCode,
        ModernLocInfoV1 location,
        GameplayPerspectiveV1 perspective,
        List<EventDraft> drafts,
        out GameplayErrorCode error)
    {
        error = GameplayErrorCode.None;
        if (!TryResolveLocation(
                location,
                perspective,
                explicitReveal: false,
                out PerspectiveSafeSemanticZoneV1? zone,
                out string? entity,
                out _))
        {
            error = GameplayErrorCode.InvalidLocation;
            return false;
        }

        drafts.Add(new EventDraft(
            kind,
            player: location.Controller,
            entityLocator: entity,
            publicPasscode: IsEventIdentityVisible(location, cardCode) ? cardCode : null,
            toZone: zone,
            sourceLocations: new[] { location }));
        return true;
    }

    private static bool TryBuildDrawDrafts(
        GameplayDrawPayloadV1 payload,
        GameplayPerspectiveV1 perspective,
        List<EventDraft> drafts,
        out GameplayErrorCode error)
    {
        error = GameplayErrorCode.None;
        drafts.Add(new EventDraft(
            PerspectiveSafeVisibleEventKindV1.Draw,
            player: payload.Player,
            count: (uint)payload.Cards.Count));
        foreach (GameplayDrawCardRecordV1 card in payload.Cards)
        {
            if (payload.Player != perspective.PlayerType &&
                (card.Position & PositionFaceUpMask) == 0)
            {
                continue;
            }

            drafts.Add(new EventDraft(
                PerspectiveSafeVisibleEventKindV1.CardRevealed,
                player: payload.Player,
                publicPasscode: card.CardCode));
        }

        return true;
    }

    private static bool TryBuildConfirmDrafts(
        GameplayConfirmPayloadV1 payload,
        GameplayPerspectiveV1 perspective,
        List<EventDraft> drafts,
        out GameplayErrorCode error)
    {
        error = GameplayErrorCode.None;
        foreach (GameplayConfirmCardRecordV1 card in payload.Cards)
        {
            if (!TryResolveLocation(
                    card.Location,
                    perspective,
                    explicitReveal: payload.Recipient == perspective.PlayerType,
                    out PerspectiveSafeSemanticZoneV1? zone,
                    out string? entity,
                    out _))
            {
                error = GameplayErrorCode.InvalidLocation;
                return false;
            }

            bool visibleToPerspective = payload.Recipient == perspective.PlayerType;
            drafts.Add(new EventDraft(
                PerspectiveSafeVisibleEventKindV1.CardRevealed,
                player: card.Location.Controller,
                entityLocator: visibleToPerspective ? entity : null,
                publicPasscode: visibleToPerspective ? card.CardCode : null,
                toZone: zone,
                sourceLocations: new[] { card.Location }));
        }

        return true;
    }

    private static bool TryBuildShuffleDrafts(
        GameplayShufflePayloadV1 payload,
        List<EventDraft> drafts,
        out GameplayErrorCode error)
    {
        error = GameplayErrorCode.None;
        if (payload.Kind == GameplayShuffleKindV1.SetCard)
        {
            if (payload.Player is null ||
                payload.Previous.Count == 0 ||
                payload.Previous.Count != payload.Current.Count)
            {
                error = GameplayErrorCode.InvalidStateTransition;
                return false;
            }

            drafts.Add(new EventDraft(
                PerspectiveSafeVisibleEventKindV1.Shuffle,
                player: payload.Player,
                sourceLocations: payload.Previous.Concat(payload.Current)));
            drafts.Add(new EventDraft(
                PerspectiveSafeVisibleEventKindV1.RandomizationBoundary,
                player: payload.Player,
                count: 0,
                sourceLocations: payload.Previous.Concat(payload.Current)));
            return true;
        }

        drafts.Add(new EventDraft(
            PerspectiveSafeVisibleEventKindV1.Shuffle,
            player: payload.Player,
            sourceLocations: payload.Previous));
        drafts.Add(new EventDraft(
            PerspectiveSafeVisibleEventKindV1.RandomizationBoundary,
            player: payload.Player,
            count: 0,
            sourceLocations: payload.Previous));
        return true;
    }

    private static bool TryBuildChainingDraft(
        GameplayChainingPayloadV1 payload,
        GameplayPerspectiveV1 perspective,
        List<EventDraft> drafts,
        out GameplayErrorCode error)
    {
        error = GameplayErrorCode.None;
        if (!TryResolveLocation(
                payload.Location,
                perspective,
                explicitReveal: false,
                out PerspectiveSafeSemanticZoneV1? zone,
                out string? entity,
                out _))
        {
            error = GameplayErrorCode.InvalidLocation;
            return false;
        }

        bool identityVisible = IsEventIdentityVisible(
            payload.Location,
            payload.CardCode);
        drafts.Add(new EventDraft(
            PerspectiveSafeVisibleEventKindV1.ChainActivated,
            player: payload.TriggeringController,
            entityLocator: entity,
            publicPasscode: identityVisible ? payload.CardCode : null,
            toZone: zone,
            count: payload.ChainSize,
            effectDescription: identityVisible ? payload.Description : null,
            sourceLocations: new[] { payload.Location }));
        return true;
    }

    private static bool TryBuildBecomeTargetDraft(
        GameplayBecomeTargetPayloadV1 payload,
        GameplayPerspectiveV1 perspective,
        List<EventDraft> drafts,
        out GameplayErrorCode error)
    {
        error = GameplayErrorCode.None;
        List<string> targets = new();
        foreach (ModernLocInfoV1 target in payload.Targets)
        {
            if (!TryResolveLocation(
                    target,
                    perspective,
                    explicitReveal: false,
                    out _,
                    out string? locator,
                    out _))
            {
                error = GameplayErrorCode.InvalidLocation;
                return false;
            }

            if (locator is not null)
            {
                targets.Add(locator);
            }
        }

        drafts.Add(new EventDraft(
            PerspectiveSafeVisibleEventKindV1.Targeted,
            targets: targets,
            sourceLocations: payload.Targets));
        return true;
    }

    private static bool TryBuildRelationshipDraft(
        PerspectiveSafeVisibleEventKindV1 kind,
        ModernLocInfoV1 source,
        ModernLocInfoV1 target,
        GameplayPerspectiveV1 perspective,
        List<EventDraft> drafts,
        out GameplayErrorCode error)
    {
        error = GameplayErrorCode.None;
        if (!TryResolveLocation(
                source,
                perspective,
                explicitReveal: false,
                out _,
                out string? sourceLocator,
                out _) ||
            !TryResolveLocation(
                target,
                perspective,
                explicitReveal: false,
                out _,
                out string? targetLocator,
                out _))
        {
            error = GameplayErrorCode.InvalidLocation;
            return false;
        }

        List<string> targets = new();
        if (targetLocator is not null)
        {
            targets.Add(targetLocator);
        }

        drafts.Add(new EventDraft(
            kind,
            player: source.Controller,
            entityLocator: sourceLocator,
            targets: targets,
            sourceLocations: new[] { source, target }));
        return true;
    }

    private static bool TryBuildCounterDraft(
        GameplayCounterPayloadV1 payload,
        GameplayPerspectiveV1 perspective,
        List<EventDraft> drafts,
        out GameplayErrorCode error)
    {
        error = GameplayErrorCode.None;
        ModernLocInfoV1 location = new(
            payload.Controller,
            payload.Location,
            payload.Sequence,
            PositionFaceUpMask);
        if (!TryResolveLocation(
                location,
                perspective,
                explicitReveal: false,
                out _,
                out string? entity,
                out _))
        {
            error = GameplayErrorCode.InvalidLocation;
            return false;
        }

        drafts.Add(new EventDraft(
            PerspectiveSafeVisibleEventKindV1.CounterChanged,
            player: payload.Controller,
            entityLocator: entity,
            amount: payload.Count,
            counterType: payload.CounterType,
            sourceLocations: new[] { location }));
        return true;
    }

    private static bool TryResolveLocation(
        ModernLocInfoV1 location,
        GameplayPerspectiveV1 perspective,
        bool explicitReveal,
        out PerspectiveSafeSemanticZoneV1? zone,
        out string? locator,
        out bool empty)
    {
        zone = null;
        locator = null;
        empty = location.Location == 0;
        if (empty)
        {
            return true;
        }

        string zoneToken;
        if (location.Location == LocationOverlay ||
            location.Location == (LocationOverlay | LocationMonster))
        {
            zone = PerspectiveSafeSemanticZoneV1.Overlay;
            zoneToken = "OVERLAY";
        }
        else
        {
            switch (location.Location)
            {
                case LocationDeck:
                    zone = PerspectiveSafeSemanticZoneV1.MainDeck;
                    return true;
                case LocationHand:
                    zone = PerspectiveSafeSemanticZoneV1.Hand;
                    zoneToken = "HAND";
                    break;
                case LocationMonster:
                    zone = PerspectiveSafeSemanticZoneV1.MonsterZone;
                    zoneToken = "MONSTER_ZONE";
                    break;
                case LocationSpellTrap:
                    if (location.Sequence == 5)
                    {
                        zone = PerspectiveSafeSemanticZoneV1.FieldZone;
                        zoneToken = "FIELD_ZONE";
                        break;
                    }

                    // SZONE field/pzone meaning belongs to I6C5. Retain the
                    // typed event-time fact in the internal ledger, but do
                    // not expose a guessed public zone or locator.
                    return true;
                case LocationGraveyard:
                    zone = PerspectiveSafeSemanticZoneV1.Graveyard;
                    zoneToken = "GRAVEYARD";
                    break;
                case LocationBanished:
                    zone = PerspectiveSafeSemanticZoneV1.Banished;
                    zoneToken = "BANISHED";
                    break;
                case LocationExtra:
                    zone = PerspectiveSafeSemanticZoneV1.ExtraDeck;
                    zoneToken = "EXTRA_DECK";
                    break;
                default:
                    return false;
            }
        }

        if (zone == PerspectiveSafeSemanticZoneV1.MainDeck ||
            (zone == PerspectiveSafeSemanticZoneV1.Hand &&
             location.Controller != perspective.PlayerType &&
             !explicitReveal) ||
            (zone == PerspectiveSafeSemanticZoneV1.ExtraDeck &&
             location.Controller != perspective.PlayerType &&
             !explicitReveal &&
             (location.Position & PositionFaceUpMask) == 0))
        {
            return true;
        }

        locator = "p" + location.Controller + ":" + zoneToken + ":" + location.Sequence;
        return true;
    }

    private static bool IsEventIdentityVisible(
        ModernLocInfoV1 location,
        uint cardCode) =>
        cardCode != 0 &&
        location.Controller <= 1 &&
        (location.Position & PositionFaceUpMask) != 0;

    private sealed class LedgerEntry
    {
        internal LedgerEntry(
            PerspectiveSafeVisibleEventV1 @event,
            IEnumerable<ModernLocInfoV1> sourceLocations)
        {
            Event = @event;
            SourceFact = new PerspectiveSafeEventSourceFactV1(sourceLocations);
        }

        internal PerspectiveSafeVisibleEventV1 Event { get; }

        internal PerspectiveSafeEventSourceFactV1 SourceFact { get; }
    }

    private sealed class EventDraft
    {
        internal EventDraft(
            PerspectiveSafeVisibleEventKindV1 kind,
            byte? player = null,
            string? entityLocator = null,
            uint? publicPasscode = null,
            PerspectiveSafeSemanticZoneV1? fromZone = null,
            PerspectiveSafeSemanticZoneV1? toZone = null,
            uint? count = null,
            int? amount = null,
            uint? counterType = null,
            uint? phase = null,
            byte? winner = null,
            byte? winReason = null,
            ulong? effectDescription = null,
            IEnumerable<string>? targets = null,
            IEnumerable<ModernLocInfoV1>? sourceLocations = null)
        {
            Kind = kind;
            Player = player;
            EntityLocator = entityLocator;
            PublicPasscode = publicPasscode;
            FromZone = fromZone;
            ToZone = toZone;
            Count = count;
            Amount = amount;
            CounterType = counterType;
            Phase = phase;
            Winner = winner;
            WinReason = winReason;
            EffectDescription = effectDescription;
            Targets = targets?.ToArray() ?? Array.Empty<string>();
            SourceLocations = sourceLocations?.ToArray() ?? Array.Empty<ModernLocInfoV1>();
        }

        internal PerspectiveSafeVisibleEventKindV1 Kind { get; }

        internal byte? Player { get; }

        internal string? EntityLocator { get; }

        internal uint? PublicPasscode { get; }

        internal PerspectiveSafeSemanticZoneV1? FromZone { get; }

        internal PerspectiveSafeSemanticZoneV1? ToZone { get; }

        internal uint? Count { get; }

        internal int? Amount { get; }

        internal uint? CounterType { get; }

        internal uint? Phase { get; }

        internal byte? Winner { get; }

        internal byte? WinReason { get; }

        internal ulong? EffectDescription { get; }

        internal IReadOnlyList<string> Targets { get; }

        internal IReadOnlyList<ModernLocInfoV1> SourceLocations { get; }
    }
}

internal sealed class PerspectiveSafeEventSourceFactV1
{
    private readonly ModernLocInfoV1[] sourceLocations;
    private readonly ReadOnlyCollection<ModernLocInfoV1> sourceLocationsView;

    internal PerspectiveSafeEventSourceFactV1(
        IEnumerable<ModernLocInfoV1> sourceLocations)
    {
        this.sourceLocations = sourceLocations.ToArray();
        sourceLocationsView = Array.AsReadOnly(this.sourceLocations);
    }

    internal IReadOnlyList<ModernLocInfoV1> SourceLocations => sourceLocationsView;
}
