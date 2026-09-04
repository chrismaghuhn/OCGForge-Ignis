using System.Globalization;

namespace OCGForge.Ignis.Gameplay;

public enum PublicSemanticZoneV1 : byte
{
    Hand = 0,
    MonsterZone = 1,
    SpellTrapZone = 2,
    FieldZone = 3,
    PendulumRelevantState = 4,
    Graveyard = 5,
    Banished = 6,
    ExtraDeck = 7,
    Overlay = 8
}

public sealed class PublicSemanticLocatorV1 :
    IEquatable<PublicSemanticLocatorV1>,
    IComparable<PublicSemanticLocatorV1>
{
    private const uint FnvOffsetBasis = 2166136261;
    private const uint FnvPrime = 16777619;

    private readonly string value;

    private PublicSemanticLocatorV1(string canonicalValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(canonicalValue);
        value = canonicalValue;
    }

    public string Value => value;

    public static bool TryCreateIndexed(
        byte absolutePlayer,
        PublicSemanticZoneV1 zone,
        uint sequence,
        out PublicSemanticLocatorV1? locator)
    {
        locator = null;
        if (!IsAbsolutePlayer(absolutePlayer) ||
            !TryGetIndexedZoneToken(zone, out string zoneToken))
        {
            return false;
        }

        locator = new PublicSemanticLocatorV1(
            BuildIndexed(absolutePlayer, zoneToken, sequence));
        return true;
    }

    public static bool TryCreatePublicOrdinal(
        byte absolutePlayer,
        PublicSemanticZoneV1 zone,
        uint cardCode,
        uint ordinal,
        out PublicSemanticLocatorV1? locator)
    {
        locator = null;
        if (!IsAbsolutePlayer(absolutePlayer) ||
            cardCode == 0 ||
            !TryGetPublicOrdinalZoneToken(zone, out string zoneToken))
        {
            return false;
        }

        locator = new PublicSemanticLocatorV1(
            BuildPublicOrdinal(absolutePlayer, zoneToken, cardCode, ordinal));
        return true;
    }

    public static bool TryCreateOverlay(
        byte absolutePlayer,
        uint parentSequence,
        uint overlaySequence,
        out PublicSemanticLocatorV1? locator)
    {
        locator = null;
        if (!IsAbsolutePlayer(absolutePlayer))
        {
            return false;
        }

        locator = new PublicSemanticLocatorV1(
            BuildOverlay(absolutePlayer, parentSequence, overlaySequence));
        return true;
    }

    public static bool TryParse(
        string? text,
        out PublicSemanticLocatorV1? locator)
    {
        locator = null;
        if (string.IsNullOrEmpty(text) || ContainsForbiddenCharacter(text))
        {
            return false;
        }

        string[] parts = text.Split(':', StringSplitOptions.None);
        if (parts.Length == 3 &&
            TryParsePlayer(parts[0], out byte indexedPlayer) &&
            TryGetIndexedZoneToken(parts[1], out string indexedZone) &&
            TryParseUnsigned(parts[2], out uint indexedSequence))
        {
            string canonical = BuildIndexed(
                indexedPlayer,
                indexedZone,
                indexedSequence);
            return TryAcceptCanonical(text, canonical, out locator);
        }

        if (parts.Length == 5 &&
            TryParsePlayer(parts[0], out byte publicPlayer) &&
            TryGetPublicOrdinalZoneToken(parts[1], out string publicZone) &&
            string.Equals(parts[2], "public", StringComparison.Ordinal) &&
            TryParseUnsigned(parts[3], out uint cardCode) &&
            cardCode > 0 &&
            TryParseUnsigned(parts[4], out uint ordinal))
        {
            string canonical = BuildPublicOrdinal(
                publicPlayer,
                publicZone,
                cardCode,
                ordinal);
            return TryAcceptCanonical(text, canonical, out locator);
        }

        if (parts.Length == 4 &&
            TryParsePlayer(parts[0], out byte overlayPlayer) &&
            string.Equals(parts[1], "OVERLAY", StringComparison.Ordinal) &&
            TryParseUnsigned(parts[2], out uint parentSequence) &&
            TryParseUnsigned(parts[3], out uint overlaySequence))
        {
            string canonical = BuildOverlay(
                overlayPlayer,
                parentSequence,
                overlaySequence);
            return TryAcceptCanonical(text, canonical, out locator);
        }

        return false;
    }

    public static bool TryGetAbsolutePlayer(
        GameplayPerspectiveV1? perspective,
        MirrorParticipantRoleV1 role,
        out byte absolutePlayer)
    {
        absolutePlayer = 0;
        if (perspective is null)
        {
            return false;
        }

        switch (perspective.Kind)
        {
            case GameplayPerspectiveKind.SelfIsPlayer0:
                switch (role)
                {
                    case MirrorParticipantRoleV1.Self:
                        absolutePlayer = 0;
                        return true;
                    case MirrorParticipantRoleV1.Opponent:
                        absolutePlayer = 1;
                        return true;
                    default:
                        return false;
                }
            case GameplayPerspectiveKind.SelfIsPlayer1:
                switch (role)
                {
                    case MirrorParticipantRoleV1.Self:
                        absolutePlayer = 1;
                        return true;
                    case MirrorParticipantRoleV1.Opponent:
                        absolutePlayer = 0;
                        return true;
                    default:
                        return false;
                }
            default:
                return false;
        }
    }

    public bool Equals(PublicSemanticLocatorV1? other) =>
        other is not null &&
        string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is PublicSemanticLocatorV1 other && Equals(other);

    public override int GetHashCode()
    {
        uint hash = FnvOffsetBasis;
        foreach (char character in Value)
        {
            hash ^= character;
            hash = unchecked(hash * FnvPrime);
        }

        return unchecked((int)hash);
    }

    public int CompareTo(PublicSemanticLocatorV1? other) =>
        other is null ? 1 : string.CompareOrdinal(Value, other.Value);

    public override string ToString() => Value;

    public static bool operator ==(
        PublicSemanticLocatorV1? left,
        PublicSemanticLocatorV1? right) =>
        left is null
            ? right is null
            : right is not null && left.Equals(right);

    public static bool operator !=(
        PublicSemanticLocatorV1? left,
        PublicSemanticLocatorV1? right) =>
        !(left == right);

    public static bool operator <(
        PublicSemanticLocatorV1? left,
        PublicSemanticLocatorV1? right) =>
        CompareNullable(left, right) < 0;

    public static bool operator <=(
        PublicSemanticLocatorV1? left,
        PublicSemanticLocatorV1? right) =>
        CompareNullable(left, right) <= 0;

    public static bool operator >(
        PublicSemanticLocatorV1? left,
        PublicSemanticLocatorV1? right) =>
        CompareNullable(left, right) > 0;

    public static bool operator >=(
        PublicSemanticLocatorV1? left,
        PublicSemanticLocatorV1? right) =>
        CompareNullable(left, right) >= 0;

    private static int CompareNullable(
        PublicSemanticLocatorV1? left,
        PublicSemanticLocatorV1? right)
    {
        if (left is null)
        {
            return right is null ? 0 : -1;
        }

        return right is null ? 1 : left.CompareTo(right);
    }

    private static bool ContainsForbiddenCharacter(string text)
    {
        foreach (char character in text)
        {
            if (character == '\0' ||
                character == '\r' ||
                character == '\n' ||
                char.IsWhiteSpace(character))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryAcceptCanonical(
        string input,
        string canonical,
        out PublicSemanticLocatorV1? locator)
    {
        locator = null;
        if (!string.Equals(input, canonical, StringComparison.Ordinal))
        {
            return false;
        }

        locator = new PublicSemanticLocatorV1(canonical);
        return true;
    }

    private static bool TryParsePlayer(
        string token,
        out byte absolutePlayer)
    {
        absolutePlayer = 0;
        if (token.Length != 2 || token[0] != 'p')
        {
            return false;
        }

        switch (token[1])
        {
            case '0':
                absolutePlayer = 0;
                return true;
            case '1':
                absolutePlayer = 1;
                return true;
            default:
                return false;
        }
    }

    private static bool TryParseUnsigned(
        string token,
        out uint value)
    {
        value = 0;
        if (token.Length == 0 ||
            (token.Length > 1 && token[0] == '0'))
        {
            return false;
        }

        foreach (char character in token)
        {
            if (character < '0' || character > '9')
            {
                return false;
            }

            uint digit = (uint)(character - '0');
            if (value > (uint.MaxValue - digit) / 10)
            {
                return false;
            }

            value = value * 10 + digit;
        }

        return true;
    }

    private static bool IsAbsolutePlayer(byte absolutePlayer) =>
        absolutePlayer <= 1;

    private static bool TryGetIndexedZoneToken(
        PublicSemanticZoneV1 zone,
        out string token)
    {
        token = zone switch
        {
            PublicSemanticZoneV1.Hand => "HAND",
            PublicSemanticZoneV1.MonsterZone => "MONSTER_ZONE",
            PublicSemanticZoneV1.SpellTrapZone => "SPELL_TRAP_ZONE",
            PublicSemanticZoneV1.FieldZone => "FIELD_ZONE",
            PublicSemanticZoneV1.PendulumRelevantState =>
                "PENDULUM_RELEVANT_STATE",
            PublicSemanticZoneV1.Graveyard => "GRAVEYARD",
            PublicSemanticZoneV1.Banished => "BANISHED",
            _ => string.Empty
        };
        return token.Length != 0;
    }

    private static bool TryGetPublicOrdinalZoneToken(
        PublicSemanticZoneV1 zone,
        out string token)
    {
        token = zone switch
        {
            PublicSemanticZoneV1.Hand => "HAND",
            PublicSemanticZoneV1.ExtraDeck => "EXTRA_DECK",
            _ => string.Empty
        };
        return token.Length != 0;
    }

    private static bool TryGetIndexedZoneToken(
        string token,
        out string canonicalToken)
    {
        canonicalToken = token switch
        {
            "HAND" => "HAND",
            "MONSTER_ZONE" => "MONSTER_ZONE",
            "SPELL_TRAP_ZONE" => "SPELL_TRAP_ZONE",
            "FIELD_ZONE" => "FIELD_ZONE",
            "PENDULUM_RELEVANT_STATE" => "PENDULUM_RELEVANT_STATE",
            "GRAVEYARD" => "GRAVEYARD",
            "BANISHED" => "BANISHED",
            _ => string.Empty
        };
        return canonicalToken.Length != 0;
    }

    private static bool TryGetPublicOrdinalZoneToken(
        string token,
        out string canonicalToken)
    {
        canonicalToken = token switch
        {
            "HAND" => "HAND",
            "EXTRA_DECK" => "EXTRA_DECK",
            _ => string.Empty
        };
        return canonicalToken.Length != 0;
    }

    private static string BuildIndexed(
        byte absolutePlayer,
        string zone,
        uint sequence) =>
        "p" + absolutePlayer.ToString(CultureInfo.InvariantCulture) +
        ":" + zone + ":" +
        sequence.ToString(CultureInfo.InvariantCulture);

    private static string BuildPublicOrdinal(
        byte absolutePlayer,
        string zone,
        uint cardCode,
        uint ordinal) =>
        "p" + absolutePlayer.ToString(CultureInfo.InvariantCulture) +
        ":" + zone + ":public:" +
        cardCode.ToString(CultureInfo.InvariantCulture) + ":" +
        ordinal.ToString(CultureInfo.InvariantCulture);

    private static string BuildOverlay(
        byte absolutePlayer,
        uint parentSequence,
        uint overlaySequence) =>
        "p" + absolutePlayer.ToString(CultureInfo.InvariantCulture) +
        ":OVERLAY:" +
        parentSequence.ToString(CultureInfo.InvariantCulture) + ":" +
        overlaySequence.ToString(CultureInfo.InvariantCulture);
}
