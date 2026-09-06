using System.Buffers;
using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace OCGForge.Ignis.Model;

public enum I6BAuthorityClass : byte
{
    AuthoritativeSemantic = 0,
    AuthoritativeEncoding = 1,
    AuthoritativeIdentity = 2,
    PhysicalExecutionOnly = 3,
    HistoricalSmokeOnly = 4,
    DerivedEvidence = 5,
    NotRelevantToI6 = 6
}

public enum I6BErrorCode
{
    InvalidManifest,
    SourceIdentityMismatch,
    RegistryMismatch,
    ConfigurationIdentityMismatch,
    UnsupportedContractValue,
    CanonicalizationFailure,
    InternalFailure
}

public sealed class OcgForgeModelContractRegistryEntryV1
{
    public OcgForgeModelContractRegistryEntryV1(
        string contractId,
        string ownerRepositoryId,
        byte authorityClass,
        byte runtimeRequired,
        byte canonicalBytesRequired,
        byte identityRequired)
    {
        ContractId = contractId ?? throw new ArgumentNullException(nameof(contractId));
        OwnerRepositoryId = ownerRepositoryId ?? throw new ArgumentNullException(nameof(ownerRepositoryId));
        AuthorityClass = authorityClass;
        RuntimeRequired = runtimeRequired;
        CanonicalBytesRequired = canonicalBytesRequired;
        IdentityRequired = identityRequired;
    }

    public string ContractId { get; }

    public string OwnerRepositoryId { get; }

    public byte AuthorityClass { get; }

    public byte RuntimeRequired { get; }

    public byte CanonicalBytesRequired { get; }

    public byte IdentityRequired { get; }
}

public sealed class OcgForgeModelContractBundleV1Manifest
{
    public OcgForgeModelContractBundleV1Manifest(
        string identityDomain,
        string schemaId,
        string ocgForgeSourceCommit,
        string p5AcceptanceExecutionHead,
        IEnumerable<OcgForgeModelContractRegistryEntryV1> registry,
        string task7MaterializationConfigIdentity,
        string ignisRuntimeContractId,
        string compatibilityProfileToken)
    {
        IdentityDomain = identityDomain ?? throw new ArgumentNullException(nameof(identityDomain));
        SchemaId = schemaId ?? throw new ArgumentNullException(nameof(schemaId));
        OcgForgeSourceCommit = ocgForgeSourceCommit ?? throw new ArgumentNullException(nameof(ocgForgeSourceCommit));
        P5AcceptanceExecutionHead = p5AcceptanceExecutionHead ?? throw new ArgumentNullException(nameof(p5AcceptanceExecutionHead));
        Task7MaterializationConfigIdentity = task7MaterializationConfigIdentity ?? throw new ArgumentNullException(nameof(task7MaterializationConfigIdentity));
        IgnisRuntimeContractId = ignisRuntimeContractId ?? throw new ArgumentNullException(nameof(ignisRuntimeContractId));
        CompatibilityProfileToken = compatibilityProfileToken ?? throw new ArgumentNullException(nameof(compatibilityProfileToken));

        Registry = new ReadOnlyCollection<OcgForgeModelContractRegistryEntryV1>(
            (registry ?? throw new ArgumentNullException(nameof(registry))).ToArray());
    }

    public string IdentityDomain { get; }

    public string SchemaId { get; }

    public string OcgForgeSourceCommit { get; }

    public string P5AcceptanceExecutionHead { get; }

    public IReadOnlyList<OcgForgeModelContractRegistryEntryV1> Registry { get; }

    public string Task7MaterializationConfigIdentity { get; }

    public string IgnisRuntimeContractId { get; }

    public string CompatibilityProfileToken { get; }
}

public sealed class I6BValidationErrorV1
{
    internal I6BValidationErrorV1(I6BErrorCode code, string reason)
    {
        Code = code;
        Reason = reason;
    }

    public I6BErrorCode Code { get; }

    public string Reason { get; }
}

public sealed class OcgForgeModelContractBundlePreflightResultV1
{
    private OcgForgeModelContractBundlePreflightResultV1(
        OcgForgeModelContractBundleV1? bundle,
        I6BValidationErrorV1? error)
    {
        Bundle = bundle;
        Error = error;
    }

    public bool IsSuccess => Bundle is not null;

    public OcgForgeModelContractBundleV1? Bundle { get; }

    public I6BValidationErrorV1? Error { get; }

    internal static OcgForgeModelContractBundlePreflightResultV1 Accepted(
        OcgForgeModelContractBundleV1 bundle)
    {
        return new OcgForgeModelContractBundlePreflightResultV1(bundle, null);
    }

    internal static OcgForgeModelContractBundlePreflightResultV1 Rejected(
        I6BErrorCode code,
        string reason)
    {
        return new OcgForgeModelContractBundlePreflightResultV1(
            null,
            new I6BValidationErrorV1(code, reason));
    }
}

public sealed class OcgForgeModelContractBundleV1
{
    private const string FrozenIdentityDomain = "ocgforge-ignis.i6.model-contract-bundle.v1";
    private const string FrozenOcgForgeSourceCommit = "3edfcabf51dd914f96adc4df903b1ac2a9d20e5f";
    private const string FrozenP5AcceptanceExecutionHead = "3c99e86c487361fc4e0f5f12678b4867e59232b7";
    private const string FrozenTask7MaterializationConfigIdentity =
        "phase6_task7_input_materialization_config.v1.20f394c888e959446fa263c3520f3dd3b1f48b3a23e58373da7153a691ab1e7a";
    private const string FrozenIgnisRuntimeContractId = "ocgforge-ignis.runtime-bundle-identity.v1";
    private const string FrozenCompatibilityProfileToken = "ocgforge-ignis.i6.compatibility-profile.v1";

    private const string OwnerRepositoryId = "ocgforge";
    private const int RegistryEntryCount = 15;
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    private static readonly RegistryExpectation[] RegistryExpectations =
    {
        new("ocgforge.public_environment_observation.v1", 0, 1, 1, 1),
        new("ocgforge.public_safe_state.v1", 0, 1, 1, 0),
        new("ocgforge.public_action_identity.v1", 2, 1, 1, 1),
        new("ocgforge.public_candidate_domain.v1", 2, 1, 1, 1),
        new("ocgforge.public_semantic_decision_identity.v1", 2, 1, 1, 1),
        new("ocgforge.episodic_environment.v2", 0, 1, 0, 1),
        new("ocgforge.environment_identity.v2", 2, 1, 1, 1),
        new("ocgforge.model_logical_input.v1", 0, 1, 1, 0),
        new("ocgforge.model_encoded_input.v1", 1, 1, 1, 0),
        new("ocgforge.model_card_vocabulary.v1", 2, 1, 1, 1),
        new("ocgforge.model_input_identity.v1", 2, 1, 1, 1),
        new("ocgforge.model_batch_layout.v1", 3, 0, 1, 0),
        new("ocgforge.model_supervision_sample.v1", 5, 0, 1, 0),
        new("ocgforge.phase6.task7.input_materialization.v1", 3, 0, 1, 0),
        new("ocgforge.phase6.task7.input_materialization_config.v1", 2, 0, 1, 1)
    };

    private readonly byte[] canonicalBytes;
    private readonly IReadOnlyList<OcgForgeModelContractRegistryEntryV1> registry;

    private OcgForgeModelContractBundleV1(
        OcgForgeModelContractBundleV1Manifest manifest,
        byte[] canonicalBytes,
        string identity)
    {
        IdentityDomain = manifest.IdentityDomain;
        SchemaId = manifest.SchemaId;
        OcgForgeSourceCommit = manifest.OcgForgeSourceCommit;
        P5AcceptanceExecutionHead = manifest.P5AcceptanceExecutionHead;
        registry = new ReadOnlyCollection<OcgForgeModelContractRegistryEntryV1>(manifest.Registry.ToArray());
        Task7MaterializationConfigIdentity = manifest.Task7MaterializationConfigIdentity;
        IgnisRuntimeContractId = manifest.IgnisRuntimeContractId;
        CompatibilityProfileToken = manifest.CompatibilityProfileToken;
        this.canonicalBytes = canonicalBytes.ToArray();
        Identity = identity;
    }

    public string IdentityDomain { get; }

    public string SchemaId { get; }

    public string OcgForgeSourceCommit { get; }

    public string P5AcceptanceExecutionHead { get; }

    public IReadOnlyList<OcgForgeModelContractRegistryEntryV1> Registry => registry;

    public string Task7MaterializationConfigIdentity { get; }

    public string IgnisRuntimeContractId { get; }

    public string CompatibilityProfileToken { get; }

    public string Identity { get; }

    public byte[] CanonicalBytes => canonicalBytes.ToArray();

    public static OcgForgeModelContractBundlePreflightResultV1 TryAccept(
        OcgForgeModelContractBundleV1Manifest? manifest)
    {
        if (manifest is null)
        {
            return OcgForgeModelContractBundlePreflightResultV1.Rejected(
                I6BErrorCode.InvalidManifest,
                "manifest_is_null");
        }

        if (manifest.IdentityDomain != FrozenIdentityDomain || manifest.SchemaId != FrozenIdentityDomain)
        {
            return OcgForgeModelContractBundlePreflightResultV1.Rejected(
                I6BErrorCode.UnsupportedContractValue,
                "bundle_schema_mismatch");
        }

        if (manifest.OcgForgeSourceCommit != FrozenOcgForgeSourceCommit ||
            manifest.P5AcceptanceExecutionHead != FrozenP5AcceptanceExecutionHead)
        {
            return OcgForgeModelContractBundlePreflightResultV1.Rejected(
                I6BErrorCode.SourceIdentityMismatch,
                "source_identity_mismatch");
        }

        if (manifest.Task7MaterializationConfigIdentity != FrozenTask7MaterializationConfigIdentity ||
            manifest.IgnisRuntimeContractId != FrozenIgnisRuntimeContractId ||
            manifest.CompatibilityProfileToken != FrozenCompatibilityProfileToken)
        {
            return OcgForgeModelContractBundlePreflightResultV1.Rejected(
                I6BErrorCode.ConfigurationIdentityMismatch,
                "configuration_identity_mismatch");
        }

        if (manifest.Registry.Count != RegistryEntryCount)
        {
            return OcgForgeModelContractBundlePreflightResultV1.Rejected(
                I6BErrorCode.RegistryMismatch,
                "registry_count_mismatch");
        }

        for (int index = 0; index < RegistryExpectations.Length; index++)
        {
            OcgForgeModelContractRegistryEntryV1? entry = manifest.Registry[index];
            RegistryExpectation expected = RegistryExpectations[index];
            if (entry is null ||
                entry.ContractId != expected.ContractId ||
                entry.OwnerRepositoryId != OwnerRepositoryId ||
                entry.AuthorityClass != expected.AuthorityClass ||
                entry.RuntimeRequired != expected.RuntimeRequired ||
                entry.CanonicalBytesRequired != expected.CanonicalBytesRequired ||
                entry.IdentityRequired != expected.IdentityRequired)
            {
                return OcgForgeModelContractBundlePreflightResultV1.Rejected(
                    I6BErrorCode.RegistryMismatch,
                    "registry_entry_mismatch");
            }
        }

        try
        {
            byte[] canonicalBytes = Canonicalize(manifest);
            byte[] digest = SHA256.HashData(canonicalBytes);
            string digestHex = Convert.ToHexString(digest).ToLowerInvariant();
            string identity = $"{FrozenIdentityDomain}.{digestHex}";
            OcgForgeModelContractBundleV1 bundle =
                new(manifest, canonicalBytes, identity);
            return OcgForgeModelContractBundlePreflightResultV1.Accepted(bundle);
        }
        catch (EncoderFallbackException)
        {
            return OcgForgeModelContractBundlePreflightResultV1.Rejected(
                I6BErrorCode.CanonicalizationFailure,
                "utf8_encoding_failure");
        }
        catch (OverflowException)
        {
            return OcgForgeModelContractBundlePreflightResultV1.Rejected(
                I6BErrorCode.CanonicalizationFailure,
                "canonical_length_overflow");
        }
    }

    private static byte[] Canonicalize(OcgForgeModelContractBundleV1Manifest manifest)
    {
        ArrayBufferWriter<byte> writer = new(2048);
        WriteString(writer, manifest.IdentityDomain);
        WriteString(writer, manifest.SchemaId);
        WriteString(writer, manifest.OcgForgeSourceCommit);
        WriteString(writer, manifest.P5AcceptanceExecutionHead);
        WriteUInt32(writer, RegistryEntryCount);

        foreach (OcgForgeModelContractRegistryEntryV1 entry in manifest.Registry)
        {
            WriteString(writer, entry.ContractId);
            WriteString(writer, entry.OwnerRepositoryId);
            WriteByte(writer, entry.AuthorityClass);
            WriteByte(writer, entry.RuntimeRequired);
            WriteByte(writer, entry.CanonicalBytesRequired);
            WriteByte(writer, entry.IdentityRequired);
        }

        WriteString(writer, manifest.Task7MaterializationConfigIdentity);
        WriteString(writer, manifest.IgnisRuntimeContractId);
        WriteString(writer, manifest.CompatibilityProfileToken);
        return writer.WrittenSpan.ToArray();
    }

    private static void WriteString(ArrayBufferWriter<byte> writer, string value)
    {
        byte[] bytes = StrictUtf8.GetBytes(value);
        if ((ulong)bytes.Length > uint.MaxValue)
        {
            throw new OverflowException();
        }

        WriteUInt32(writer, (uint)bytes.Length);
        Span<byte> destination = writer.GetSpan(bytes.Length);
        bytes.CopyTo(destination);
        writer.Advance(bytes.Length);
    }

    private static void WriteUInt32(ArrayBufferWriter<byte> writer, uint value)
    {
        Span<byte> destination = writer.GetSpan(sizeof(uint));
        BinaryPrimitives.WriteUInt32BigEndian(destination, value);
        writer.Advance(sizeof(uint));
    }

    private static void WriteByte(ArrayBufferWriter<byte> writer, byte value)
    {
        Span<byte> destination = writer.GetSpan(sizeof(byte));
        destination[0] = value;
        writer.Advance(sizeof(byte));
    }

    private sealed record RegistryExpectation(
        string ContractId,
        byte AuthorityClass,
        byte RuntimeRequired,
        byte CanonicalBytesRequired,
        byte IdentityRequired);
}
