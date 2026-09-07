using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using OCGForge.Ignis.Gameplay;

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

internal sealed record I6C6ScenarioManifestV1(
    string ScenarioContractId,
    string ScenarioId,
    byte PerspectivePlayer,
    byte StartingPlayer,
    ulong DuelFlags,
    IReadOnlyList<ulong> SeedWords,
    string Seat0DeckId,
    string Seat0DeckSha256,
    string Seat1DeckId,
    string Seat1DeckSha256,
    string OcgforgeSemanticCommit,
    string RulesBundleId,
    string OcgforgeCoreCommit,
    string OcgforgeCorePatchsetId,
    string OcgforgeCorePatchsetSha256,
    string OcgforgeCardscriptsCommit,
    string IgnisEdoproCommit,
    string IgnisEdoproCoreCommit,
    string IgnisCardscriptsCommit,
    string NativeSetupDescriptorId,
    string NativeRawTranscriptSha256,
    string IgnisRawReplayTranscriptSha256,
    string NativePublicEventTranscriptSha256,
    string IgnisPublicEventTranscriptSha256,
    string SupportedMessageFamilyEnvelopeId,
    byte ComparisonBoundaryKind,
    ulong ComparisonBoundaryPublicEventPrefixCount,
    byte ComparisonBoundaryStateSnapshotSelector);

internal sealed record I6C6ScenarioForensicProvenanceV1(
    string OcgforgeBabelCdbCommit,
    string IgnisBabelCdbCommit);

internal sealed record I6C6NativeScenarioEvidenceV1(
    string ScenarioContractId,
    string ScenarioId,
    byte PerspectivePlayer,
    byte StartingPlayer,
    ulong DuelFlags,
    IReadOnlyList<ulong> SeedWords,
    string Seat0DeckId,
    string Seat0DeckSha256,
    string Seat1DeckId,
    string Seat1DeckSha256,
    string OcgforgeSemanticCommit,
    string RulesBundleId,
    string OcgforgeCoreCommit,
    string OcgforgeCorePatchsetId,
    string OcgforgeCorePatchsetSha256,
    string OcgforgeCardscriptsCommit,
    string NativeSetupDescriptorId,
    string NativeRawTranscriptSha256,
    string NativePublicEventTranscriptSha256,
    ulong PublicEventPrefixCount);

internal sealed record I6C6IgnisReplayEvidenceV1(
    string ScenarioContractId,
    string ScenarioId,
    byte PerspectivePlayer,
    byte StartingPlayer,
    ulong DuelFlags,
    IReadOnlyList<ulong> SeedWords,
    string Seat0DeckId,
    string Seat0DeckSha256,
    string Seat1DeckId,
    string Seat1DeckSha256,
    string NativeSetupDescriptorId,
    string IgnisEdoproCommit,
    string IgnisEdoproCoreCommit,
    string IgnisCardscriptsCommit,
    string IgnisRawReplayTranscriptSha256,
    string IgnisPublicEventTranscriptSha256,
    ulong PublicEventPrefixCount);

internal static class I6C6ScenarioEvidenceV1
{
    internal const string OcgforgeSemanticCommit =
        "f929de0b4d4157327dba003067d2e21e42f7ad75";
    internal const string OcgforgeCoreCommit =
        "9a0c558c2d686542f7914a6d529fd7aa57746aed";
    internal const string RulesBundleId =
        "3adfe6b4cfe2c2805e50b389fc0eb4e70a3b0b6107436614d328fddc865e585f";
    internal const string OcgforgeCardscriptsCommit =
        "f337c87018ca723c1aded5143e616bb649555273";
    internal const string IgnisEdoproCommit =
        "30935e847165a9ef0e547fb51a43f36168fab7c7";
    internal const string IgnisEdoproCoreCommit =
        "46779fbe40e6a9bd8967f5dc6a03f4eaa6550d57";
    internal const string IgnisCardscriptsCommit =
        "00a828b79303d047d6905f528857cc287ad3a84e";
    internal const string OcgforgeBabelCdbCommit =
        "89ad6837b0766a52984d8c715a7d5d4f8447946b";
    internal const string IgnisBabelCdbCommit =
        "2142b4b45e7963fd944940f144951177e87eb15c";
    internal const string ManifestDomain =
        "OCGFORGE-IGNIS-I6C6-SAME-SCENARIO-REPLAY-V1\0";
    internal const string PublicEventTranscriptDomain =
        "OCGFORGE-IGNIS-I6C6-PUBLIC-EVENT-TRANSCRIPT-V1\0";
    internal const string SupportedMessageFamilyEnvelopeId =
        "ocgforge-ignis.i6c6.supported-message-family-envelope.v1";

    internal static byte[] CanonicalScenarioManifestBytes(
        I6C6ScenarioManifestV1 manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ValidateManifest(manifest);

        using MemoryStream stream = new();
        WriteRawAscii(stream, ManifestDomain);
        WriteString(stream, manifest.ScenarioContractId);
        WriteString(stream, manifest.ScenarioId);
        stream.WriteByte(manifest.PerspectivePlayer);
        stream.WriteByte(manifest.StartingPlayer);
        WriteUInt64BigEndian(stream, manifest.DuelFlags);
        WriteUInt32BigEndian(stream, checked((uint)manifest.SeedWords.Count));
        foreach (ulong word in manifest.SeedWords)
        {
            WriteUInt64BigEndian(stream, word);
        }

        WriteString(stream, manifest.Seat0DeckId);
        WriteString(stream, manifest.Seat0DeckSha256);
        WriteString(stream, manifest.Seat1DeckId);
        WriteString(stream, manifest.Seat1DeckSha256);
        WriteString(stream, manifest.OcgforgeSemanticCommit);
        WriteString(stream, manifest.RulesBundleId);
        WriteString(stream, manifest.OcgforgeCoreCommit);
        WriteString(stream, manifest.OcgforgeCorePatchsetId);
        WriteString(stream, manifest.OcgforgeCorePatchsetSha256);
        WriteString(stream, manifest.OcgforgeCardscriptsCommit);
        WriteString(stream, manifest.IgnisEdoproCommit);
        WriteString(stream, manifest.IgnisEdoproCoreCommit);
        WriteString(stream, manifest.IgnisCardscriptsCommit);
        WriteString(stream, manifest.NativeSetupDescriptorId);
        WriteString(stream, manifest.NativeRawTranscriptSha256);
        WriteString(stream, manifest.IgnisRawReplayTranscriptSha256);
        WriteString(stream, manifest.NativePublicEventTranscriptSha256);
        WriteString(stream, manifest.IgnisPublicEventTranscriptSha256);
        WriteString(stream, manifest.SupportedMessageFamilyEnvelopeId);
        stream.WriteByte(manifest.ComparisonBoundaryKind);
        WriteUInt64BigEndian(
            stream,
            manifest.ComparisonBoundaryPublicEventPrefixCount);
        stream.WriteByte(manifest.ComparisonBoundaryStateSnapshotSelector);
        return stream.ToArray();
    }

    internal static byte[] CanonicalPublicEventTranscriptBytes(
        IReadOnlyList<PerspectiveSafeVisibleEventV1> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        using MemoryStream stream = new();
        WriteRawAscii(stream, PublicEventTranscriptDomain);
        WriteUInt32BigEndian(stream, checked((uint)events.Count));

        ulong? previousEventIndex = null;
        foreach (PerspectiveSafeVisibleEventV1 value in events)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (previousEventIndex.HasValue &&
                value.EventIndex <= previousEventIndex.Value)
            {
                throw new InvalidDataException(
                    "public event indices must be strictly increasing");
            }

            WriteUInt64BigEndian(stream, value.EventIndex);
            stream.WriteByte(VisibleEventKindCode(value.Kind));
            WriteOptionalByte(stream, value.Player);
            WriteOptionalLocator(stream, value.EntityLocator);
            WriteOptionalUInt32(stream, value.PublicPasscode);
            WriteOptionalZone(stream, value.FromZone);
            WriteOptionalZone(stream, value.ToZone);
            WriteOptionalUInt32(stream, value.Count);
            WriteOptionalInt32(stream, value.Amount);
            WriteOptionalUInt32(stream, value.CounterType);
            WriteOptionalUInt32(stream, value.Phase);
            WriteOptionalByte(stream, value.Winner);
            WriteOptionalByte(stream, value.WinReason);
            WriteOptionalUInt64(stream, value.EffectDescription);

            if (value.Targets is null)
            {
                throw new InvalidDataException(
                    "public event targets must be present");
            }

            string[] targets = value.Targets.ToArray();
            foreach (string target in targets)
            {
                if (!PublicSemanticLocatorV1.TryParse(target, out _))
                {
                    throw new InvalidDataException(
                        "public event target locator is not canonical");
                }
            }

            Array.Sort(targets, StringComparer.Ordinal);
            WriteUInt32BigEndian(stream, checked((uint)targets.Length));
            foreach (string target in targets)
            {
                WriteString(stream, target);
            }

            previousEventIndex = value.EventIndex;
        }

        return stream.ToArray();
    }

    internal static string CanonicalPublicEventTranscriptSha256(
        IReadOnlyList<PerspectiveSafeVisibleEventV1> events) =>
        Sha256Hex(CanonicalPublicEventTranscriptBytes(events));

    internal static string Sha256Hex(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    internal static I6C6ScenarioManifestV1 BindScenarioManifest(
        I6C6NativeScenarioEvidenceV1 native,
        I6C6IgnisReplayEvidenceV1 ignis,
        I6C6ScenarioForensicProvenanceV1 forensic)
    {
        ArgumentNullException.ThrowIfNull(native);
        ArgumentNullException.ThrowIfNull(ignis);
        ArgumentNullException.ThrowIfNull(forensic);

        if (!string.Equals(native.ScenarioContractId, ignis.ScenarioContractId,
                StringComparison.Ordinal) ||
            !string.Equals(native.ScenarioId, ignis.ScenarioId,
                StringComparison.Ordinal) ||
            native.PerspectivePlayer != ignis.PerspectivePlayer ||
            native.StartingPlayer != ignis.StartingPlayer ||
            native.DuelFlags != ignis.DuelFlags ||
            native.SeedWords is null || ignis.SeedWords is null ||
            native.SeedWords.Count != 4 || ignis.SeedWords.Count != 4 ||
            !native.SeedWords.SequenceEqual(ignis.SeedWords) ||
            !string.Equals(native.Seat0DeckId, ignis.Seat0DeckId,
                StringComparison.Ordinal) ||
            !string.Equals(native.Seat0DeckSha256, ignis.Seat0DeckSha256,
                StringComparison.Ordinal) ||
            !string.Equals(native.Seat1DeckId, ignis.Seat1DeckId,
                StringComparison.Ordinal) ||
            !string.Equals(native.Seat1DeckSha256, ignis.Seat1DeckSha256,
                StringComparison.Ordinal) ||
            !string.Equals(native.NativeSetupDescriptorId,
                ignis.NativeSetupDescriptorId,
                StringComparison.Ordinal) ||
            native.PublicEventPrefixCount != ignis.PublicEventPrefixCount ||
            !string.Equals(
                native.NativePublicEventTranscriptSha256,
                ignis.IgnisPublicEventTranscriptSha256,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException("native and Ignis scenario evidence differ");
        }

        if (native.OcgforgeSemanticCommit != OcgforgeSemanticCommit ||
            native.RulesBundleId != RulesBundleId ||
            native.OcgforgeCoreCommit != OcgforgeCoreCommit ||
            native.OcgforgeCardscriptsCommit != OcgforgeCardscriptsCommit ||
            ignis.IgnisEdoproCommit != IgnisEdoproCommit ||
            ignis.IgnisEdoproCoreCommit != IgnisEdoproCoreCommit ||
            ignis.IgnisCardscriptsCommit != IgnisCardscriptsCommit ||
            forensic.OcgforgeBabelCdbCommit != OcgforgeBabelCdbCommit ||
            forensic.IgnisBabelCdbCommit != IgnisBabelCdbCommit)
        {
            throw new InvalidDataException("scenario runtime provenance differs");
        }

        return new I6C6ScenarioManifestV1(
            native.ScenarioContractId,
            native.ScenarioId,
            native.PerspectivePlayer,
            native.StartingPlayer,
            native.DuelFlags,
            native.SeedWords.ToArray(),
            native.Seat0DeckId,
            native.Seat0DeckSha256,
            native.Seat1DeckId,
            native.Seat1DeckSha256,
            native.OcgforgeSemanticCommit,
            native.RulesBundleId,
            native.OcgforgeCoreCommit,
            native.OcgforgeCorePatchsetId,
            native.OcgforgeCorePatchsetSha256,
            native.OcgforgeCardscriptsCommit,
            ignis.IgnisEdoproCommit,
            ignis.IgnisEdoproCoreCommit,
            ignis.IgnisCardscriptsCommit,
            native.NativeSetupDescriptorId,
            native.NativeRawTranscriptSha256,
            ignis.IgnisRawReplayTranscriptSha256,
            native.NativePublicEventTranscriptSha256,
            ignis.IgnisPublicEventTranscriptSha256,
            SupportedMessageFamilyEnvelopeId,
            1,
            native.PublicEventPrefixCount,
            1);
    }

    private static void ValidateManifest(I6C6ScenarioManifestV1 value)
    {
        if (value.PerspectivePlayer > 1 || value.StartingPlayer > 1 ||
            value.SeedWords is null || value.SeedWords.Count != 4 ||
            value.ComparisonBoundaryKind != 1 ||
            value.ComparisonBoundaryStateSnapshotSelector != 1 ||
            value.SupportedMessageFamilyEnvelopeId !=
                SupportedMessageFamilyEnvelopeId)
        {
            throw new InvalidDataException("scenario manifest shape is invalid");
        }

        string[] tokens =
        {
            value.ScenarioContractId,
            value.ScenarioId,
            value.Seat0DeckId,
            value.Seat1DeckId,
            value.OcgforgeCorePatchsetId,
            value.NativeSetupDescriptorId
        };
        foreach (string token in tokens)
        {
            if (string.IsNullOrEmpty(token) ||
                token.Any(character =>
                    !(character is >= 'a' and <= 'z' or
                        >= '0' and <= '9' or '.' or '_' or '-')))
            {
                throw new InvalidDataException("scenario manifest token is invalid");
            }
        }

        ValidateSha256(value.Seat0DeckSha256);
        ValidateSha256(value.Seat1DeckSha256);
        ValidateSha256(value.RulesBundleId);
        ValidateSha256(value.OcgforgeCorePatchsetSha256);
        ValidateSha256(value.NativeRawTranscriptSha256);
        ValidateSha256(value.IgnisRawReplayTranscriptSha256);
        ValidateSha256(value.NativePublicEventTranscriptSha256);
        ValidateSha256(value.IgnisPublicEventTranscriptSha256);
        ValidateGitCommit(value.OcgforgeSemanticCommit);
        ValidateGitCommit(value.OcgforgeCoreCommit);
        ValidateGitCommit(value.OcgforgeCardscriptsCommit);
        ValidateGitCommit(value.IgnisEdoproCommit);
        ValidateGitCommit(value.IgnisEdoproCoreCommit);
        ValidateGitCommit(value.IgnisCardscriptsCommit);
    }

    private static void ValidateSha256(string value)
    {
        if (value is null || value.Length != 64 ||
            value.Any(character =>
                !(character is >= '0' and <= '9' or >= 'a' and <= 'f')))
        {
            throw new InvalidDataException("scenario manifest SHA-256 is invalid");
        }
    }

    private static void ValidateGitCommit(string value)
    {
        if (value is null || value.Length != 40 ||
            value.Any(character =>
                !(character is >= '0' and <= '9' or >= 'a' and <= 'f')))
        {
            throw new InvalidDataException("scenario manifest commit is invalid");
        }
    }

    private static void WriteRawAscii(Stream stream, string value) =>
        stream.Write(Encoding.ASCII.GetBytes(value));

    private static void WriteString(Stream stream, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        WriteUInt32BigEndian(stream, checked((uint)bytes.Length));
        stream.Write(bytes);
    }

    private static void WriteOptionalByte(Stream stream, byte? value)
    {
        stream.WriteByte(value.HasValue ? (byte)1 : (byte)0);
        if (value.HasValue)
        {
            stream.WriteByte(value.Value);
        }
    }

    private static void WriteOptionalUInt32(Stream stream, uint? value)
    {
        stream.WriteByte(value.HasValue ? (byte)1 : (byte)0);
        if (value.HasValue)
        {
            WriteUInt32BigEndian(stream, value.Value);
        }
    }

    private static void WriteOptionalUInt64(Stream stream, ulong? value)
    {
        stream.WriteByte(value.HasValue ? (byte)1 : (byte)0);
        if (value.HasValue)
        {
            WriteUInt64BigEndian(stream, value.Value);
        }
    }

    private static void WriteOptionalInt32(Stream stream, int? value)
    {
        stream.WriteByte(value.HasValue ? (byte)1 : (byte)0);
        if (value.HasValue)
        {
            WriteUInt32BigEndian(stream, unchecked((uint)value.Value));
        }
    }

    private static void WriteOptionalLocator(Stream stream, string? value)
    {
        stream.WriteByte(value is null ? (byte)0 : (byte)1);
        if (value is not null)
        {
            if (!PublicSemanticLocatorV1.TryParse(value, out _))
            {
                throw new InvalidDataException("public event locator is not canonical");
            }

            WriteString(stream, value);
        }
    }

    private static void WriteOptionalZone(
        Stream stream,
        PerspectiveSafeSemanticZoneV1? value)
    {
        stream.WriteByte(value.HasValue ? (byte)1 : (byte)0);
        if (value.HasValue)
        {
            stream.WriteByte(ZoneCode(value.Value));
        }
    }

    private static void WriteUInt32BigEndian(Stream stream, uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteUInt64BigEndian(Stream stream, ulong value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static byte ZoneCode(PerspectiveSafeSemanticZoneV1 value) => value switch
    {
        PerspectiveSafeSemanticZoneV1.Unknown => 0,
        PerspectiveSafeSemanticZoneV1.MainDeck => 1,
        PerspectiveSafeSemanticZoneV1.Hand => 2,
        PerspectiveSafeSemanticZoneV1.MonsterZone => 3,
        PerspectiveSafeSemanticZoneV1.SpellTrapZone => 4,
        PerspectiveSafeSemanticZoneV1.Graveyard => 5,
        PerspectiveSafeSemanticZoneV1.Banished => 6,
        PerspectiveSafeSemanticZoneV1.ExtraDeck => 7,
        PerspectiveSafeSemanticZoneV1.FieldZone => 8,
        PerspectiveSafeSemanticZoneV1.PendulumRelevant => 9,
        PerspectiveSafeSemanticZoneV1.Overlay => 10,
        _ => throw new InvalidDataException("unknown public zone")
    };

    private static byte VisibleEventKindCode(
        PerspectiveSafeVisibleEventKindV1 value) => value switch
    {
        PerspectiveSafeVisibleEventKindV1.Unknown => 0,
        PerspectiveSafeVisibleEventKindV1.TurnStarted => 1,
        PerspectiveSafeVisibleEventKindV1.PhaseChanged => 2,
        PerspectiveSafeVisibleEventKindV1.CardMoved => 3,
        PerspectiveSafeVisibleEventKindV1.CardRevealed => 4,
        PerspectiveSafeVisibleEventKindV1.Summoned => 5,
        PerspectiveSafeVisibleEventKindV1.Set => 6,
        PerspectiveSafeVisibleEventKindV1.Draw => 7,
        PerspectiveSafeVisibleEventKindV1.Shuffle => 8,
        PerspectiveSafeVisibleEventKindV1.RandomizationBoundary => 9,
        PerspectiveSafeVisibleEventKindV1.LifePointsChanged => 10,
        PerspectiveSafeVisibleEventKindV1.ChainActivated => 11,
        PerspectiveSafeVisibleEventKindV1.ChainResolved => 12,
        PerspectiveSafeVisibleEventKindV1.ChainEnded => 13,
        PerspectiveSafeVisibleEventKindV1.CardDestroyed => 14,
        PerspectiveSafeVisibleEventKindV1.CardBanished => 15,
        PerspectiveSafeVisibleEventKindV1.CardReturned => 16,
        PerspectiveSafeVisibleEventKindV1.PositionChanged => 17,
        PerspectiveSafeVisibleEventKindV1.CounterChanged => 18,
        PerspectiveSafeVisibleEventKindV1.Equipped => 19,
        PerspectiveSafeVisibleEventKindV1.Unequipped => 20,
        PerspectiveSafeVisibleEventKindV1.Targeted => 21,
        PerspectiveSafeVisibleEventKindV1.Win => 22,
        _ => throw new InvalidDataException("unknown public event kind")
    };
}
