using System.Buffers;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;

namespace OCGForge.Ignis.Gameplay;

public enum PublicStateProjectionErrorV1 : byte
{
    None = 0,
    InvalidSnapshot = 1,
    UnsupportedLayout = 2,
    UnprovenKnowledge = 3,
    UnprovenLocator = 4,
    CanonicalizationFailure = 5
}

public sealed class PublicStateProjectionContextV1
{
    public PublicStateProjectionContextV1(ulong duelFlags)
    {
        DuelFlags = duelFlags;
    }

    public ulong DuelFlags { get; }
}

public sealed class PublicParticipantStateV1
{
    internal PublicParticipantStateV1(
        byte absolutePlayer,
        uint lifePoints,
        uint mainDeckCount,
        uint handCount,
        uint extraDeckCount)
    {
        AbsolutePlayer = absolutePlayer;
        LifePoints = lifePoints;
        MainDeckCount = mainDeckCount;
        HandCount = handCount;
        ExtraDeckCount = extraDeckCount;
    }

    public byte AbsolutePlayer { get; }

    public uint LifePoints { get; }

    public uint MainDeckCount { get; }

    public uint HandCount { get; }

    public uint ExtraDeckCount { get; }
}

public sealed class PublicCardStateV1
{
    internal PublicCardStateV1(
        PublicSemanticLocatorV1 locator,
        byte absolutePlayer,
        PublicSemanticZoneV1 zone,
        uint? cardCode,
        uint? position)
    {
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));
        AbsolutePlayer = absolutePlayer;
        Zone = zone;
        CardCode = cardCode;
        Position = position;
    }

    public PublicSemanticLocatorV1 Locator { get; }

    public byte AbsolutePlayer { get; }

    public PublicSemanticZoneV1 Zone { get; }

    public uint? CardCode { get; }

    public uint? Position { get; }
}

public sealed class PublicStateSnapshotV1
{
    private const string ContractIdValue =
        "ocgforge-ignis.public-state-projection.v1";

    private readonly PublicParticipantStateV1[] participants;
    private readonly PublicCardStateV1[] cards;
    private readonly ReadOnlyCollection<PublicParticipantStateV1> participantsView;
    private readonly ReadOnlyCollection<PublicCardStateV1> cardsView;

    internal PublicStateSnapshotV1(
        byte perspectivePlayer,
        ulong duelFlags,
        ulong turnCount,
        byte? turnPlayer,
        ushort? phase,
        bool terminal,
        IEnumerable<PublicParticipantStateV1> participants,
        IEnumerable<PublicCardStateV1> cards)
    {
        ContractId = ContractIdValue;
        PerspectivePlayer = perspectivePlayer;
        DuelFlags = duelFlags;
        TurnCount = turnCount;
        TurnPlayer = turnPlayer;
        Phase = phase;
        Terminal = terminal;
        this.participants = participants.ToArray();
        this.cards = cards.ToArray();
        participantsView = Array.AsReadOnly(this.participants);
        cardsView = Array.AsReadOnly(this.cards);
    }

    public string ContractId { get; }

    public byte PerspectivePlayer { get; }

    public ulong DuelFlags { get; }

    public ulong TurnCount { get; }

    public byte? TurnPlayer { get; }

    public ushort? Phase { get; }

    public bool Terminal { get; }

    public IReadOnlyList<PublicParticipantStateV1> Participants =>
        participantsView;

    public IReadOnlyList<PublicCardStateV1> Cards => cardsView;
}

public sealed class PublicStateProjectionResultV1
{
    private const string PublicProjectionIdPrefix =
        "ocgforge-ignis.public-state-projection.v1.";

    private readonly byte[] canonicalBytes;

    private PublicStateProjectionResultV1(
        bool isSuccess,
        PublicStateProjectionErrorV1 error,
        PublicStateSnapshotV1? snapshot,
        byte[] canonicalBytes,
        string? sha256)
    {
        IsSuccess = isSuccess;
        Error = error;
        Snapshot = snapshot;
        this.canonicalBytes = canonicalBytes.ToArray();
        Sha256 = sha256;
        PublicProjectionId = sha256 is null
            ? null
            : PublicProjectionIdPrefix + sha256;
    }

    public bool IsSuccess { get; }

    public PublicStateProjectionErrorV1 Error { get; }

    public PublicStateSnapshotV1? Snapshot { get; }

    public ReadOnlyMemory<byte> CanonicalBytes =>
        new ReadOnlyMemory<byte>(canonicalBytes.ToArray());

    public string? Sha256 { get; }

    public string? PublicProjectionId { get; }

    internal static PublicStateProjectionResultV1 Success(
        PublicStateSnapshotV1 snapshot,
        byte[] canonicalBytes,
        string sha256) =>
        new(
            true,
            PublicStateProjectionErrorV1.None,
            snapshot,
            canonicalBytes,
            sha256);

    internal static PublicStateProjectionResultV1 Failure(
        PublicStateProjectionErrorV1 error) =>
        new(
            false,
            error,
            null,
            Array.Empty<byte>(),
            null);
}

internal static class PublicStateProjectionV1
{
    private const ulong DuelPzone = 0x800;
    private const ulong DuelSeparatePzone = 0x1000;
    private const ulong DuelThreeColumnsField = 0x400000;
    private const uint TypeMonster = 0x01;
    private const uint TypeSpell = 0x02;
    private const uint TypePendulum = 0x01000000;

    internal static PublicStateProjectionResultV1 TryProject(
        MirrorSnapshotV1? mirror,
        PublicStateProjectionContextV1? context)
    {
        if (mirror is null || context is null)
        {
            return PublicStateProjectionResultV1.Failure(
                PublicStateProjectionErrorV1.InvalidSnapshot);
        }

        if (!TryCreateParticipants(
                mirror,
                out PublicParticipantStateV1[] participants,
                out byte? turnPlayer,
                out PublicStateProjectionErrorV1 participantError))
        {
            return PublicStateProjectionResultV1.Failure(participantError);
        }

        if (!TryCreateCards(
                mirror,
                context.DuelFlags,
                out PublicCardStateV1[] cards,
                out PublicStateProjectionErrorV1 cardError))
        {
            return PublicStateProjectionResultV1.Failure(cardError);
        }

        ushort? phase = null;
        if (mirror.Phase.IsKnown)
        {
            if (!IsKnownValue(mirror.Phase.Provenance))
            {
                return PublicStateProjectionResultV1.Failure(
                    PublicStateProjectionErrorV1.UnprovenKnowledge);
            }

            phase = mirror.Phase.Value;
        }
        else if (mirror.Phase.Provenance != MirrorProvenanceV1.UnknownRedacted)
        {
            return PublicStateProjectionResultV1.Failure(
                PublicStateProjectionErrorV1.InvalidSnapshot);
        }

        if (mirror.Terminal.Winner is MirrorParticipantRoleV1 winner &&
            !TryGetAbsolutePlayer(mirror.Perspective, winner, out _))
        {
            return PublicStateProjectionResultV1.Failure(
                PublicStateProjectionErrorV1.InvalidSnapshot);
        }

        PublicStateSnapshotV1 snapshot = new(
            mirror.Perspective.PlayerType,
            context.DuelFlags,
            mirror.TurnCount,
            turnPlayer,
            phase,
            mirror.Terminal.IsTerminal,
            participants,
            cards);

        if (!TryEncode(snapshot, out byte[] canonicalBytes))
        {
            return PublicStateProjectionResultV1.Failure(
                PublicStateProjectionErrorV1.CanonicalizationFailure);
        }

        string sha256 = Convert.ToHexString(
            SHA256.HashData(canonicalBytes)).ToLowerInvariant();
        return PublicStateProjectionResultV1.Success(
            snapshot,
            canonicalBytes,
            sha256);
    }

    private static bool TryCreateParticipants(
        MirrorSnapshotV1 mirror,
        out PublicParticipantStateV1[] participants,
        out byte? turnPlayer,
        out PublicStateProjectionErrorV1 error)
    {
        participants = Array.Empty<PublicParticipantStateV1>();
        turnPlayer = null;
        error = PublicStateProjectionErrorV1.None;
        if (mirror.Perspective is null ||
            (mirror.Perspective.Kind != GameplayPerspectiveKind.SelfIsPlayer0 &&
             mirror.Perspective.Kind != GameplayPerspectiveKind.SelfIsPlayer1) ||
            mirror.Perspective.PlayerType !=
                (byte)mirror.Perspective.Kind ||
            mirror.Participants.Count != 2)
        {
            error = PublicStateProjectionErrorV1.InvalidSnapshot;
            return false;
        }

        MirrorParticipantSnapshotV1?[] byRole = new MirrorParticipantSnapshotV1?[2];
        foreach (MirrorParticipantSnapshotV1 participant in mirror.Participants)
        {
            if (!IsMirrorRole(participant.Role) ||
                byRole[(int)participant.Role] is not null)
            {
                error = PublicStateProjectionErrorV1.InvalidSnapshot;
                return false;
            }

            byRole[(int)participant.Role] = participant;
        }

        if (byRole.Any(value => value is null))
        {
            error = PublicStateProjectionErrorV1.InvalidSnapshot;
            return false;
        }

        participants = new PublicParticipantStateV1[2];
        foreach (MirrorParticipantRoleV1 role in new[]
                 {
                     MirrorParticipantRoleV1.Self,
                     MirrorParticipantRoleV1.Opponent
                 })
        {
            if (!TryGetAbsolutePlayer(mirror.Perspective, role, out byte absolutePlayer))
            {
                error = PublicStateProjectionErrorV1.InvalidSnapshot;
                return false;
            }

            MirrorParticipantSnapshotV1 participant = byRole[(int)role]!;
            if (!TryGetZone(
                    participant,
                    MirrorZoneV1.MainDeck,
                    out MirrorZoneSnapshotV1 mainDeck) ||
                !TryGetZone(
                    participant,
                    MirrorZoneV1.Hand,
                    out MirrorZoneSnapshotV1 hand) ||
                !TryGetZone(
                    participant,
                    MirrorZoneV1.ExtraDeck,
                    out MirrorZoneSnapshotV1 extraDeck) ||
                !TryReadKnown(
                    participant.LifePoints,
                    out uint lifePoints) ||
                !TryReadKnown(mainDeck.Count, out uint mainDeckCount) ||
                !TryReadKnown(hand.Count, out uint handCount) ||
                !TryReadKnown(extraDeck.Count, out uint extraDeckCount))
            {
                error = PublicStateProjectionErrorV1.UnprovenKnowledge;
                return false;
            }

            participants[absolutePlayer] = new PublicParticipantStateV1(
                absolutePlayer,
                lifePoints,
                mainDeckCount,
                handCount,
                extraDeckCount);
        }

        if (mirror.TurnPlayer.IsKnown)
        {
            if (!IsKnownValue(mirror.TurnPlayer.Provenance) ||
                !TryGetAbsolutePlayer(
                    mirror.Perspective,
                    mirror.TurnPlayer.Value,
                    out byte absoluteTurnPlayer))
            {
                error = PublicStateProjectionErrorV1.UnprovenKnowledge;
                return false;
            }

            turnPlayer = absoluteTurnPlayer;
        }
        else if (mirror.TurnPlayer.Provenance != MirrorProvenanceV1.UnknownRedacted)
        {
            error = PublicStateProjectionErrorV1.InvalidSnapshot;
            return false;
        }

        return true;
    }

    private static bool TryCreateCards(
        MirrorSnapshotV1 mirror,
        ulong duelFlags,
        out PublicCardStateV1[] cards,
        out PublicStateProjectionErrorV1 error)
    {
        cards = Array.Empty<PublicCardStateV1>();
        error = PublicStateProjectionErrorV1.None;
        List<PublicCardStateV1> indexedCards = new();
        Dictionary<PublicCardGroup, List<KnownPileCard>> pileGroups = new();
        HashSet<string> locatorValues = new(StringComparer.Ordinal);

        foreach (MirrorCardSnapshotV1 mirrorCard in mirror.Cards)
        {
            if (!TryGetAbsolutePlayer(
                    mirror.Perspective,
                    mirrorCard.Controller,
                    out byte absolutePlayer))
            {
                error = PublicStateProjectionErrorV1.InvalidSnapshot;
                return false;
            }

            if (!TryReadCardCode(
                    mirrorCard.CardCode,
                    out uint? cardCode,
                    out error) ||
                !TryReadPosition(
                    mirrorCard.Position,
                    out uint? position,
                    out error))
            {
                return false;
            }

            if (mirrorCard.Zone == MirrorZoneV1.MainDeck)
            {
                continue;
            }

            if (mirrorCard.IsOverlay)
            {
                if (mirrorCard.Zone != MirrorZoneV1.MonsterZone ||
                    !PublicSemanticLocatorV1.TryCreateOverlay(
                        absolutePlayer,
                        mirrorCard.Sequence,
                        mirrorCard.OverlayIndex,
                        out PublicSemanticLocatorV1? overlayLocator))
                {
                    error = PublicStateProjectionErrorV1.UnprovenLocator;
                    return false;
                }

                if (!AddCard(
                        indexedCards,
                        locatorValues,
                        new PublicCardStateV1(
                            overlayLocator!,
                            absolutePlayer,
                            PublicSemanticZoneV1.Overlay,
                            cardCode,
                            null),
                        out error))
                {
                    return false;
                }

                continue;
            }

            PublicSemanticZoneV1 semanticZone;
            switch (mirrorCard.Zone)
            {
                case MirrorZoneV1.Hand:
                    semanticZone = PublicSemanticZoneV1.Hand;
                    break;
                case MirrorZoneV1.ExtraDeck:
                    semanticZone = PublicSemanticZoneV1.ExtraDeck;
                    break;
                case MirrorZoneV1.MonsterZone:
                    semanticZone = PublicSemanticZoneV1.MonsterZone;
                    break;
                case MirrorZoneV1.SpellTrapZone:
                    if (!TryClassifySpellTrap(
                            mirrorCard,
                            duelFlags,
                            out semanticZone,
                            out error))
                    {
                        return false;
                    }

                    break;
                case MirrorZoneV1.Graveyard:
                    semanticZone = PublicSemanticZoneV1.Graveyard;
                    break;
                case MirrorZoneV1.Banished:
                    semanticZone = PublicSemanticZoneV1.Banished;
                    break;
                default:
                    error = PublicStateProjectionErrorV1.InvalidSnapshot;
                    return false;
            }

            if (semanticZone is PublicSemanticZoneV1.Hand or
                PublicSemanticZoneV1.ExtraDeck)
            {
                if (cardCode.HasValue)
                {
                    PublicCardGroup group = new(
                        absolutePlayer,
                        semanticZone,
                        cardCode.Value);
                    if (!pileGroups.TryGetValue(
                            group,
                            out List<KnownPileCard>? groupedCards))
                    {
                        groupedCards = new List<KnownPileCard>();
                        pileGroups.Add(group, groupedCards);
                    }

                    groupedCards.Add(new KnownPileCard(position));
                }

                continue;
            }

            if (!PublicSemanticLocatorV1.TryCreateIndexed(
                    absolutePlayer,
                    semanticZone,
                    mirrorCard.Sequence,
                    out PublicSemanticLocatorV1? indexedLocator))
            {
                error = PublicStateProjectionErrorV1.UnprovenLocator;
                return false;
            }

            if (!AddCard(
                    indexedCards,
                    locatorValues,
                    new PublicCardStateV1(
                        indexedLocator!,
                        absolutePlayer,
                        semanticZone,
                        cardCode,
                        position),
                    out error))
            {
                return false;
            }
        }

        foreach ((PublicCardGroup group, List<KnownPileCard> groupedCards)
                 in pileGroups)
        {
            groupedCards.Sort(KnownPileCard.Compare);
            for (uint ordinal = 0; ordinal < groupedCards.Count; ordinal++)
            {
                if (!PublicSemanticLocatorV1.TryCreatePublicOrdinal(
                        group.AbsolutePlayer,
                        group.Zone,
                        group.CardCode,
                        ordinal,
                        out PublicSemanticLocatorV1? locator))
                {
                    error = PublicStateProjectionErrorV1.UnprovenLocator;
                    return false;
                }

                KnownPileCard pileCard = groupedCards[(int)ordinal];
                if (!AddCard(
                        indexedCards,
                        locatorValues,
                        new PublicCardStateV1(
                            locator!,
                            group.AbsolutePlayer,
                            group.Zone,
                            group.CardCode,
                            pileCard.Position),
                        out error))
                {
                    return false;
                }
            }
        }

        indexedCards.Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(
                left.Locator.Value,
                right.Locator.Value));
        cards = indexedCards.ToArray();
        return true;
    }

    private static bool TryClassifySpellTrap(
        MirrorCardSnapshotV1 card,
        ulong duelFlags,
        out PublicSemanticZoneV1 semanticZone,
        out PublicStateProjectionErrorV1 error)
    {
        semanticZone = default;
        error = PublicStateProjectionErrorV1.None;
        bool hasPzone = (duelFlags & DuelPzone) != 0;
        bool hasSeparatePzone = (duelFlags & DuelSeparatePzone) != 0;
        bool hasThreeColumns = (duelFlags & DuelThreeColumnsField) != 0;
        uint sequence = card.Sequence;

        if (hasSeparatePzone && !hasPzone)
        {
            error = PublicStateProjectionErrorV1.UnsupportedLayout;
            return false;
        }

        if (sequence == 5)
        {
            semanticZone = PublicSemanticZoneV1.FieldZone;
            return true;
        }

        if (hasSeparatePzone)
        {
            if (sequence is 6 or 7)
            {
                semanticZone = PublicSemanticZoneV1.PendulumRelevantState;
                return true;
            }

            if (hasThreeColumns
                ? sequence is >= 1 and <= 3
                : sequence <= 4)
            {
                semanticZone = PublicSemanticZoneV1.SpellTrapZone;
                return true;
            }

            error = PublicStateProjectionErrorV1.UnsupportedLayout;
            return false;
        }

        uint firstSpellTrapSequence = hasThreeColumns ? 1u : 0u;
        uint lastSpellTrapSequence = hasThreeColumns ? 3u : 4u;
        if (sequence < firstSpellTrapSequence ||
            sequence > lastSpellTrapSequence)
        {
            error = PublicStateProjectionErrorV1.UnsupportedLayout;
            return false;
        }

        bool isSharedPzoneSlot = hasPzone &&
            (hasThreeColumns
                ? sequence is 1 or 3
                : sequence is 0 or 4);
        if (!isSharedPzoneSlot)
        {
            semanticZone = PublicSemanticZoneV1.SpellTrapZone;
            return true;
        }

        if (!TryProveSharedPzoneCard(
                card,
                out bool isPzone,
                out error))
        {
            return false;
        }

        semanticZone = isPzone
            ? PublicSemanticZoneV1.PendulumRelevantState
            : PublicSemanticZoneV1.SpellTrapZone;
        return true;
    }

    private static bool TryProveSharedPzoneCard(
        MirrorCardSnapshotV1 card,
        out bool isPzone,
        out PublicStateProjectionErrorV1 error)
    {
        isPzone = false;
        error = PublicStateProjectionErrorV1.None;
        MirrorQueryFieldSnapshotV1? typeField = null;
        foreach (MirrorQueryFieldSnapshotV1 field in card.QueryFields)
        {
            if (field.Flag != QueryFlagV1.Type)
            {
                continue;
            }

            if (typeField is not null)
            {
                error = PublicStateProjectionErrorV1.InvalidSnapshot;
                return false;
            }

            typeField = field;
        }

        if (typeField is null)
        {
            error = PublicStateProjectionErrorV1.UnsupportedLayout;
            return false;
        }

        MirrorQueryValueV1 value = typeField.Value;
        if (!value.IsKnown ||
            value.Kind != MirrorQueryValueKindV1.UInt32 ||
            !IsKnownValue(value.Provenance))
        {
            error = PublicStateProjectionErrorV1.UnprovenKnowledge;
            return false;
        }

        uint pzoneType = TypePendulum | TypeSpell;
        isPzone = (value.UInt32Value & (TypePendulum | TypeSpell | TypeMonster)) ==
                  pzoneType;
        return true;
    }

    private static bool AddCard(
        List<PublicCardStateV1> cards,
        HashSet<string> locatorValues,
        PublicCardStateV1 card,
        out PublicStateProjectionErrorV1 error)
    {
        error = PublicStateProjectionErrorV1.None;
        if (!locatorValues.Add(card.Locator.Value))
        {
            error = PublicStateProjectionErrorV1.UnprovenLocator;
            return false;
        }

        cards.Add(card);
        return true;
    }

    private static bool TryGetZone(
        MirrorParticipantSnapshotV1 participant,
        MirrorZoneV1 requested,
        out MirrorZoneSnapshotV1 zone)
    {
        zone = null!;
        if (participant.Zones.Count != Enum.GetValues<MirrorZoneV1>().Length)
        {
            return false;
        }

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

    private static bool TryReadKnown<T>(
        MirrorValueV1<T> value,
        out T result)
    {
        result = default!;
        if (!value.IsKnown || !IsKnownValue(value.Provenance))
        {
            return false;
        }

        result = value.Value;
        return true;
    }

    private static bool TryReadCardCode(
        MirrorValueV1<uint> value,
        out uint? cardCode,
        out PublicStateProjectionErrorV1 error)
    {
        cardCode = null;
        error = PublicStateProjectionErrorV1.None;
        if (!value.IsKnown)
        {
            if (value.Provenance != MirrorProvenanceV1.UnknownRedacted)
            {
                error = PublicStateProjectionErrorV1.InvalidSnapshot;
                return false;
            }

            return true;
        }

        if (!IsKnownValue(value.Provenance) || value.Value == 0)
        {
            error = PublicStateProjectionErrorV1.UnprovenKnowledge;
            return false;
        }

        cardCode = value.Value;
        return true;
    }

    private static bool TryReadPosition(
        MirrorValueV1<uint> value,
        out uint? position,
        out PublicStateProjectionErrorV1 error)
    {
        position = null;
        error = PublicStateProjectionErrorV1.None;
        if (!value.IsKnown)
        {
            if (value.Provenance != MirrorProvenanceV1.UnknownRedacted)
            {
                error = PublicStateProjectionErrorV1.InvalidSnapshot;
                return false;
            }

            return true;
        }

        if (!IsKnownValue(value.Provenance))
        {
            error = PublicStateProjectionErrorV1.UnprovenKnowledge;
            return false;
        }

        position = value.Value;
        return true;
    }

    private static bool TryGetAbsolutePlayer(
        GameplayPerspectiveV1? perspective,
        MirrorParticipantRoleV1 role,
        out byte absolutePlayer) =>
        PublicSemanticLocatorV1.TryGetAbsolutePlayer(
            perspective,
            role,
            out absolutePlayer);

    private static bool IsMirrorRole(MirrorParticipantRoleV1 role) =>
        role is MirrorParticipantRoleV1.Self or MirrorParticipantRoleV1.Opponent;

    private static bool IsKnownValue(MirrorProvenanceV1 provenance) =>
        provenance is MirrorProvenanceV1.PublicProtocolFact or
            MirrorProvenanceV1.PerspectivePrivateFact or
            MirrorProvenanceV1.DerivedFromProvenPublicFacts;

    private static bool TryEncode(
        PublicStateSnapshotV1 snapshot,
        out byte[] canonicalBytes)
    {
        canonicalBytes = Array.Empty<byte>();
        try
        {
            ArrayBufferWriter<byte> buffer = new();
            using (Utf8JsonWriter writer = new(
                       buffer,
                       new JsonWriterOptions { Indented = false }))
            {
                writer.WriteStartObject();
                writer.WriteString("contract_id", snapshot.ContractId);
                writer.WriteNumber("perspective_player", snapshot.PerspectivePlayer);
                writer.WriteNumber("duel_flags", snapshot.DuelFlags);
                writer.WriteNumber("turn_count", snapshot.TurnCount);
                WriteNullableNumber(writer, "turn_player", snapshot.TurnPlayer);
                WriteNullableNumber(writer, "phase", snapshot.Phase);
                writer.WriteBoolean("terminal", snapshot.Terminal);

                writer.WriteStartArray("participants");
                foreach (PublicParticipantStateV1 participant in snapshot.Participants)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("absolute_player", participant.AbsolutePlayer);
                    writer.WriteNumber("life_points", participant.LifePoints);
                    writer.WriteNumber("main_deck_count", participant.MainDeckCount);
                    writer.WriteNumber("hand_count", participant.HandCount);
                    writer.WriteNumber("extra_deck_count", participant.ExtraDeckCount);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteStartArray("cards");
                foreach (PublicCardStateV1 card in snapshot.Cards)
                {
                    writer.WriteStartObject();
                    writer.WriteString("locator", card.Locator.Value);
                    writer.WriteNumber("absolute_player", card.AbsolutePlayer);
                    writer.WriteString("zone", ZoneToken(card.Zone));
                    WriteNullableNumber(writer, "card_code", card.CardCode);
                    WriteNullableNumber(writer, "position", card.Position);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.Flush();
            }

            canonicalBytes = buffer.WrittenSpan.ToArray();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static void WriteNullableNumber(
        Utf8JsonWriter writer,
        string propertyName,
        byte? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
        else
        {
            writer.WriteNull(propertyName);
        }
    }

    private static void WriteNullableNumber(
        Utf8JsonWriter writer,
        string propertyName,
        ushort? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
        else
        {
            writer.WriteNull(propertyName);
        }
    }

    private static void WriteNullableNumber(
        Utf8JsonWriter writer,
        string propertyName,
        uint? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumber(propertyName, value.Value);
        }
        else
        {
            writer.WriteNull(propertyName);
        }
    }

    private static string ZoneToken(PublicSemanticZoneV1 zone) =>
        zone switch
        {
            PublicSemanticZoneV1.Hand => "HAND",
            PublicSemanticZoneV1.MonsterZone => "MONSTER_ZONE",
            PublicSemanticZoneV1.SpellTrapZone => "SPELL_TRAP_ZONE",
            PublicSemanticZoneV1.FieldZone => "FIELD_ZONE",
            PublicSemanticZoneV1.PendulumRelevantState =>
                "PENDULUM_RELEVANT_STATE",
            PublicSemanticZoneV1.Graveyard => "GRAVEYARD",
            PublicSemanticZoneV1.Banished => "BANISHED",
            PublicSemanticZoneV1.ExtraDeck => "EXTRA_DECK",
            PublicSemanticZoneV1.Overlay => "OVERLAY",
            _ => throw new InvalidOperationException("Unknown public zone.")
        };

    private readonly record struct PublicCardGroup(
        byte AbsolutePlayer,
        PublicSemanticZoneV1 Zone,
        uint CardCode);

    private sealed class KnownPileCard
    {
        internal KnownPileCard(uint? position)
        {
            Position = position;
        }

        internal uint? Position { get; }

        internal static int Compare(KnownPileCard left, KnownPileCard right)
        {
            if (!left.Position.HasValue)
            {
                return right.Position.HasValue ? -1 : 0;
            }

            if (!right.Position.HasValue)
            {
                return 1;
            }

            return left.Position.Value.CompareTo(right.Position.Value);
        }
    }
}
