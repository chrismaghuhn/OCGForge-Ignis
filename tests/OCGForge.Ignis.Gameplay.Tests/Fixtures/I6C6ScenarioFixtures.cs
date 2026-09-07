namespace OCGForge.Ignis.Gameplay.Tests.Fixtures;

// I6C6-1 owns only the synthetic, public-safe contract inputs. The comparison
// implementation is intentionally owned by I6C6-2.
internal enum I6C6ComparisonErrorCodeV1
{
    None = 0,
    BlockedPendingI6D = 1,
    OptionalPresenceMismatch = 2,
    UnknownEnum = 3,
    HiddenIdentityData = 4,
    UnprovenScenarioPairing = 5
}

internal readonly record struct I6C6ComparisonResultV1(
    bool IsSuccess,
    I6C6ComparisonErrorCodeV1 ErrorCode);

internal readonly record struct I6C6OptionalByteV1(
    bool IsPresent,
    byte Value)
{
    internal static I6C6OptionalByteV1 Absent => new(false, 0);

    internal static I6C6OptionalByteV1 Present(byte value) => new(true, value);
}

internal readonly record struct I6C6PublicEnumSampleV1(
    byte SemanticZone,
    byte Position,
    byte RelationshipKind,
    byte VisibleEventKind);

internal sealed record I6C6PublicEntityV1(
    string Locator,
    bool IdentityKnown,
    uint? PublicPasscode,
    bool HasPrintedProperties,
    bool HasCurrentProperties)
{
    internal static I6C6PublicEntityV1 Unknown(string locator) =>
        new(locator, false, null, false, false);

    internal static I6C6PublicEntityV1 Known(string locator, uint passcode) =>
        new(locator, true, passcode, false, false);

    internal static I6C6PublicEntityV1 HiddenWithIdentityDerivedData(
        string locator) =>
        new(locator, false, null, true, true);
}

internal sealed record I6C6PublicSafeStateV1(
    I6C6OptionalByteV1 PlayerToAct,
    I6C6OptionalByteV1 TurnPlayer,
    I6C6PublicEnumSampleV1 EnumSample,
    IReadOnlyList<I6C6PublicEntityV1> Entities);

internal sealed record I6C6ScenarioPairingV1(
    byte NativePerspectivePlayer,
    byte IgnisPerspectivePlayer,
    byte StartingPlayer,
    IReadOnlyList<ulong> SeedWords,
    string? NativeSetupDescriptorId,
    string MessageFamilyEnvelopeId,
    ulong PublicEventPrefixCount,
    byte StateSnapshotSelector);

internal static class I6C6NativePublicSafeCodesV1
{
    // These are the explicit ocgforge.public_safe_state.v1 wire codes, not
    // incidental values from any .NET enum declaration.
    internal const byte SemanticZoneHand = 2;
    internal const byte PositionFaceUpAttack = 1;
    internal const byte RelationshipEquip = 1;
    internal const byte VisibleEventCardMoved = 3;
    internal const byte Unknown = 0xFF;
}

internal static class I6C6ScenarioFixtures
{
    private const uint SyntheticKnownPasscode = 12345678;

    internal static I6C6PublicSafeStateV1 PublicState(
        I6C6OptionalByteV1 playerToAct,
        I6C6OptionalByteV1? turnPlayer = null,
        I6C6PublicEnumSampleV1? enumSample = null,
        IReadOnlyList<I6C6PublicEntityV1>? entities = null) =>
        new(
            playerToAct,
            turnPlayer ?? I6C6OptionalByteV1.Absent,
            enumSample ?? KnownEnumSample(),
            entities ?? new[]
            {
                I6C6PublicEntityV1.Known(
                    "p0:HAND:public:12345678:0",
                    SyntheticKnownPasscode)
            });

    internal static I6C6PublicEnumSampleV1 KnownEnumSample() =>
        new(
            I6C6NativePublicSafeCodesV1.SemanticZoneHand,
            I6C6NativePublicSafeCodesV1.PositionFaceUpAttack,
            I6C6NativePublicSafeCodesV1.RelationshipEquip,
            I6C6NativePublicSafeCodesV1.VisibleEventCardMoved);

    internal static I6C6ScenarioPairingV1 SupportedPairing() =>
        new(
            NativePerspectivePlayer: 0,
            IgnisPerspectivePlayer: 0,
            StartingPlayer: 0,
            SeedWords: new ulong[] { 1, 2, 3, 4 },
            NativeSetupDescriptorId: "synthetic-i6c6-setup-v1",
            MessageFamilyEnvelopeId:
                "ocgforge-ignis.i6c6.supported-message-family-envelope.v1",
            PublicEventPrefixCount: 0,
            StateSnapshotSelector: 1);

    internal static I6C6ScenarioPairingV1 MissingSetupDescriptorPairing() =>
        SupportedPairing() with { NativeSetupDescriptorId = null };

    internal static I6C6PublicSafeStateV1 StateWithKnownPlayerToAct() =>
        PublicState(
            I6C6OptionalByteV1.Present(0),
            turnPlayer: I6C6OptionalByteV1.Absent);

    internal static I6C6PublicSafeStateV1 StateWithoutPlayerToAct() =>
        PublicState(
            I6C6OptionalByteV1.Absent,
            turnPlayer: I6C6OptionalByteV1.Absent);

    internal static I6C6PublicSafeStateV1 StateWithPresentZeroTurnPlayer() =>
        PublicState(
            I6C6OptionalByteV1.Absent,
            turnPlayer: I6C6OptionalByteV1.Present(0));

    internal static I6C6PublicSafeStateV1 StateWithUnknownEnum() =>
        PublicState(
            I6C6OptionalByteV1.Absent,
            enumSample: new I6C6PublicEnumSampleV1(
                I6C6NativePublicSafeCodesV1.Unknown,
                I6C6NativePublicSafeCodesV1.PositionFaceUpAttack,
                I6C6NativePublicSafeCodesV1.RelationshipEquip,
                I6C6NativePublicSafeCodesV1.VisibleEventCardMoved));

    internal static I6C6PublicSafeStateV1 StateWithHiddenUnknownEntity() =>
        PublicState(
            I6C6OptionalByteV1.Absent,
            entities: new[]
            {
                I6C6PublicEntityV1.Unknown("p1:SPELL_TRAP_ZONE:0")
            });

    internal static I6C6PublicSafeStateV1 StateWithForbiddenHiddenIdentityData() =>
        PublicState(
            I6C6OptionalByteV1.Absent,
            entities: new[]
            {
                I6C6PublicEntityV1.HiddenWithIdentityDerivedData(
                    "p1:SPELL_TRAP_ZONE:0")
            });

    internal static I6C6PublicSafeStateV1 StateWithEntities(
        params I6C6PublicEntityV1[] entities) =>
        PublicState(I6C6OptionalByteV1.Absent, entities: entities);
}
