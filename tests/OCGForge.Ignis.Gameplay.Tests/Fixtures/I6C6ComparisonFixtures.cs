using System.Globalization;
using OCGForge.Ignis.Gameplay;

namespace OCGForge.Ignis.Gameplay.Tests.Fixtures;

internal static class I6C6ComparisonFixtures
{
    private const string SupportedMessageFamilyEnvelopeId =
        "ocgforge-ignis.i6c6.supported-message-family-envelope.v1";

    internal static I6C6ComparisonResultV1 Compare(
        I6C6PublicSafeStateV1 native,
        I6C6PublicSafeStateV1 ignis,
        I6C6ScenarioPairingV1 pairing)
    {
        if (!TryValidatePairing(pairing, out I6C6ComparisonResultV1 pairingFailure))
        {
            return pairingFailure;
        }

        if (native.PlayerToAct.IsPresent || ignis.PlayerToAct.IsPresent)
        {
            return Failure(
                I6C6ComparisonErrorCodeV1.BlockedPendingI6D);
        }

        if (!TryValidateState(native, out I6C6ComparisonResultV1 nativeFailure))
        {
            return nativeFailure;
        }

        if (!TryValidateState(ignis, out I6C6ComparisonResultV1 ignisFailure))
        {
            return ignisFailure;
        }

        if (native.TurnPlayer != ignis.TurnPlayer)
        {
            return Failure(
                I6C6ComparisonErrorCodeV1.OptionalPresenceMismatch);
        }

        string nativeSignature = Normalize(native);
        string ignisSignature = Normalize(ignis);
        if (!string.Equals(nativeSignature, ignisSignature, StringComparison.Ordinal))
        {
            return Failure(
                I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
        }

        return Success();
    }

    private static bool TryValidatePairing(
        I6C6ScenarioPairingV1 pairing,
        out I6C6ComparisonResultV1 failure)
    {
        if (pairing is null ||
            pairing.NativePerspectivePlayer > 1 ||
            pairing.IgnisPerspectivePlayer > 1 ||
            pairing.NativePerspectivePlayer != pairing.IgnisPerspectivePlayer)
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
            return false;
        }

        if (pairing.StartingPlayer > 1)
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
            return false;
        }

        if (pairing.SeedWords is null || pairing.SeedWords.Count != 4)
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
            return false;
        }

        if (string.IsNullOrEmpty(pairing.NativeSetupDescriptorId))
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
            return false;
        }

        if (!string.Equals(
                pairing.MessageFamilyEnvelopeId,
                SupportedMessageFamilyEnvelopeId,
                StringComparison.Ordinal) ||
            pairing.StateSnapshotSelector != 1)
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
            return false;
        }

        failure = default;
        return true;
    }

    private static bool TryValidateState(
        I6C6PublicSafeStateV1 state,
        out I6C6ComparisonResultV1 failure)
    {
        if (!IsValidOptionalPlayer(state.TurnPlayer))
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.OptionalPresenceMismatch);
            return false;
        }

        if (!IsValidSemanticZone(state.EnumSample.SemanticZone) ||
            !IsValidPosition(state.EnumSample.Position) ||
            !IsValidRelationshipKind(state.EnumSample.RelationshipKind) ||
            !IsValidVisibleEventKind(state.EnumSample.VisibleEventKind))
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.UnknownEnum);
            return false;
        }

        if (state.Entities is null)
        {
            failure = Failure(
                I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
            return false;
        }

        List<string> locators = new(state.Entities.Count);
        foreach (I6C6PublicEntityV1 entity in state.Entities)
        {
            if (!PublicSemanticLocatorV1.TryParse(entity.Locator, out _))
            {
                failure = Failure(
                    I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
                return false;
            }

            if (!entity.IdentityKnown &&
                (entity.PublicPasscode.HasValue ||
                 entity.HasPrintedProperties ||
                 entity.HasCurrentProperties))
            {
                failure = Failure(
                    I6C6ComparisonErrorCodeV1.HiddenIdentityData);
                return false;
            }

            if (entity.IdentityKnown &&
                (!entity.PublicPasscode.HasValue || entity.PublicPasscode.Value == 0))
            {
                failure = Failure(
                    I6C6ComparisonErrorCodeV1.HiddenIdentityData);
                return false;
            }

            locators.Add(entity.Locator);
        }

        locators.Sort(StringComparer.Ordinal);
        for (int index = 1; index < locators.Count; index++)
        {
            if (string.Equals(
                    locators[index - 1],
                    locators[index],
                    StringComparison.Ordinal))
            {
                failure = Failure(
                    I6C6ComparisonErrorCodeV1.UnprovenScenarioPairing);
                return false;
            }
        }

        failure = default;
        return true;
    }

    private static string Normalize(I6C6PublicSafeStateV1 state)
    {
        IEnumerable<I6C6PublicEntityV1> entities = state.Entities
            .OrderBy(entity => entity.Locator, StringComparer.Ordinal);

        return string.Join(
            "|",
            "PTA=" + EncodeOptional(state.PlayerToAct),
            "TURN=" + EncodeOptional(state.TurnPlayer),
            "ENUM=" + string.Join(
                ",",
                state.EnumSample.SemanticZone.ToString(CultureInfo.InvariantCulture),
                state.EnumSample.Position.ToString(CultureInfo.InvariantCulture),
                state.EnumSample.RelationshipKind.ToString(CultureInfo.InvariantCulture),
                state.EnumSample.VisibleEventKind.ToString(CultureInfo.InvariantCulture)),
            "ENTITIES=" + string.Join(
                ";",
                entities.Select(EncodeEntity)));
    }

    private static string EncodeOptional(I6C6OptionalByteV1 value) =>
        value.IsPresent
            ? "PRESENT(" + value.Value.ToString(CultureInfo.InvariantCulture) + ")"
            : "ABSENT";

    private static string EncodeEntity(I6C6PublicEntityV1 entity) =>
        entity.IdentityKnown
            ? entity.Locator + ":KNOWN:" + entity.PublicPasscode!.Value
                .ToString(CultureInfo.InvariantCulture)
            : entity.Locator + ":UNKNOWN";

    private static bool IsValidOptionalPlayer(I6C6OptionalByteV1 value) =>
        !value.IsPresent
            ? value.Value == 0
            : value.Value <= 1;

    private static bool IsValidSemanticZone(byte value) => value <= 10;

    private static bool IsValidPosition(byte value) =>
        value is 0 or 1 or 2 or 4 or 8;

    private static bool IsValidRelationshipKind(byte value) => value <= 2;

    private static bool IsValidVisibleEventKind(byte value) => value <= 22;

    private static I6C6ComparisonResultV1 Success() =>
        new(true, I6C6ComparisonErrorCodeV1.None);

    private static I6C6ComparisonResultV1 Failure(
        I6C6ComparisonErrorCodeV1 errorCode) =>
        new(false, errorCode);
}
