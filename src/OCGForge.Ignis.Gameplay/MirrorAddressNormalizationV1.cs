namespace OCGForge.Ignis.Gameplay;

internal readonly record struct MirrorAddressNormalizationV1(
    byte Controller,
    MirrorZoneV1 Zone,
    uint Sequence,
    bool IsOverlay,
    uint OverlayIndex)
{
    private const byte LocationDeck = 0x01;
    private const byte LocationHand = 0x02;
    private const byte LocationMonster = 0x04;
    private const byte LocationSpellTrap = 0x08;
    private const byte LocationGraveyard = 0x10;
    private const byte LocationBanished = 0x20;
    private const byte LocationExtra = 0x40;
    private const byte LocationOverlay = 0x80;
    private const uint MaximumMonsterSequence = 6;
    private const uint MaximumSpellTrapSequence = 7;

    internal static bool TryNormalize(
        ModernLocInfoV1 value,
        out MirrorAddressNormalizationV1 normalized,
        out GameplayErrorCode error)
    {
        normalized = default;
        error = GameplayErrorCode.None;
        if (value.Controller > 1)
        {
            error = GameplayErrorCode.InvalidParticipant;
            return false;
        }

        MirrorZoneV1 zone = ToZone(value.Location, out bool valid);
        if (!valid)
        {
            error = GameplayErrorCode.InvalidLocation;
            return false;
        }

        bool isOverlay = (value.Location & LocationOverlay) != 0;
        if (isOverlay && zone != MirrorZoneV1.MonsterZone)
        {
            error = GameplayErrorCode.InvalidLocation;
            return false;
        }

        if (!isOverlay && !IsValidFieldSequence(zone, value.Sequence))
        {
            error = GameplayErrorCode.StateCapacityExceeded;
            return false;
        }

        normalized = new MirrorAddressNormalizationV1(
            value.Controller,
            zone,
            value.Sequence,
            isOverlay,
            isOverlay ? value.Position : 0);
        return true;
    }

    private static bool IsValidFieldSequence(
        MirrorZoneV1 zone,
        uint sequence) =>
        zone switch
        {
            MirrorZoneV1.MonsterZone => sequence <= MaximumMonsterSequence,
            MirrorZoneV1.SpellTrapZone => sequence <= MaximumSpellTrapSequence,
            _ => true
        };

    private static MirrorZoneV1 ToZone(byte location, out bool valid)
    {
        byte baseLocation = (byte)(location & 0x7f);
        (MirrorZoneV1 zone, bool isValid) = baseLocation switch
        {
            LocationDeck => (MirrorZoneV1.MainDeck, true),
            LocationExtra => (MirrorZoneV1.ExtraDeck, true),
            LocationHand => (MirrorZoneV1.Hand, true),
            LocationMonster => (MirrorZoneV1.MonsterZone, true),
            LocationSpellTrap => (MirrorZoneV1.SpellTrapZone, true),
            LocationGraveyard => (MirrorZoneV1.Graveyard, true),
            LocationBanished => (MirrorZoneV1.Banished, true),
            _ => (default, false)
        };
        valid = isValid;
        return zone;
    }
}
