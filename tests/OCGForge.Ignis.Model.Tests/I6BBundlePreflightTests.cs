using System.Security.Cryptography;
using OCGForge.Ignis.Model;

namespace OCGForge.Ignis.Model.Tests;

internal static class I6BBundlePreflightTests
{
    private const string ExpectedIdentityDomain = "ocgforge-ignis.i6.model-contract-bundle.v1";
    private const string ExpectedSourceCommit = "3edfcabf51dd914f96adc4df903b1ac2a9d20e5f";
    private const string ExpectedP5ExecutionHead = "3c99e86c487361fc4e0f5f12678b4867e59232b7";
    private const string ExpectedTask7ConfigIdentity =
        "phase6_task7_input_materialization_config.v1.20f394c888e959446fa263c3520f3dd3b1f48b3a23e58373da7153a691ab1e7a";
    private const string ExpectedRuntimeContractId = "ocgforge-ignis.runtime-bundle-identity.v1";
    private const string ExpectedCompatibilityProfile = "ocgforge-ignis.i6.compatibility-profile.v1";

    private static readonly string[] ExpectedContractIds =
    {
        "ocgforge.public_environment_observation.v1",
        "ocgforge.public_safe_state.v1",
        "ocgforge.public_action_identity.v1",
        "ocgforge.public_candidate_domain.v1",
        "ocgforge.public_semantic_decision_identity.v1",
        "ocgforge.episodic_environment.v2",
        "ocgforge.environment_identity.v2",
        "ocgforge.model_logical_input.v1",
        "ocgforge.model_encoded_input.v1",
        "ocgforge.model_card_vocabulary.v1",
        "ocgforge.model_input_identity.v1",
        "ocgforge.model_batch_layout.v1",
        "ocgforge.model_supervision_sample.v1",
        "ocgforge.phase6.task7.input_materialization.v1",
        "ocgforge.phase6.task7.input_materialization_config.v1"
    };

    private static readonly (byte Authority, byte Runtime, byte Canonical, byte Identity)[] ExpectedFlags =
    {
        (0, 1, 1, 1),
        (0, 1, 1, 0),
        (2, 1, 1, 1),
        (2, 1, 1, 1),
        (2, 1, 1, 1),
        (0, 1, 0, 1),
        (2, 1, 1, 1),
        (0, 1, 1, 0),
        (1, 1, 1, 0),
        (2, 1, 1, 1),
        (2, 1, 1, 1),
        (3, 0, 1, 0),
        (5, 0, 1, 0),
        (3, 0, 1, 0),
        (2, 0, 1, 1)
    };

    private const int ExpectedCanonicalLength = 1229;
    private const string ExpectedCanonicalSha256 =
        "60fbdf650ec68c6dd7541a0dc7229139be6f0897971d485fd5ddd0330fe2be89";
    private const string ExpectedBundleIdentity =
        "ocgforge-ignis.i6.model-contract-bundle.v1.60fbdf650ec68c6dd7541a0dc7229139be6f0897971d485fd5ddd0330fe2be89";

    private const string ExpectedCanonicalHex =
        "0000002a6f6367666f7267652d69676e69732e69362e6d6f64656c2d636f6e74726163742d62756e646c652e76310000002a6f6367666f7267652d69" +
        "676e69732e69362e6d6f64656c2d636f6e74726163742d62756e646c652e763100000028336564666361626635316464393134663936616463346466" +
        "393033623161633261396432306535660000002833633939653836633438373336316663346530663566313236373862343836376535393233326237" +
        "0000000f0000002a6f6367666f7267652e7075626c69635f656e7669726f6e6d656e745f6f62736572766174696f6e2e7631000000086f6367666f72" +
        "6765000101010000001d6f6367666f7267652e7075626c69635f736166655f73746174652e7631000000086f6367666f72676500010100000000226f" +
        "6367666f7267652e7075626c69635f616374696f6e5f6964656e746974792e7631000000086f6367666f72676502010101000000236f6367666f7267" +
        "652e7075626c69635f63616e6469646174655f646f6d61696e2e7631000000086f6367666f726765020101010000002d6f6367666f7267652e707562" +
        "6c69635f73656d616e7469635f6465636973696f6e5f6964656e746974792e7631000000086f6367666f72676502010101000000206f6367666f7267" +
        "652e657069736f6469635f656e7669726f6e6d656e742e7632000000086f6367666f72676500010001000000206f6367666f7267652e656e7669726f" +
        "6e6d656e745f6964656e746974792e7632000000086f6367666f726765020101010000001f6f6367666f7267652e6d6f64656c5f6c6f676963616c5f" +
        "696e7075742e7631000000086f6367666f726765000101000000001f6f6367666f7267652e6d6f64656c5f656e636f6465645f696e7075742e763100" +
        "0000086f6367666f72676501010100000000216f6367666f7267652e6d6f64656c5f636172645f766f636162756c6172792e7631000000086f636766" +
        "6f72676502010101000000206f6367666f7267652e6d6f64656c5f696e7075745f6964656e746974792e7631000000086f6367666f72676502010101" +
        "0000001e6f6367666f7267652e6d6f64656c5f62617463685f6c61796f75742e7631000000086f6367666f72676503000100000000246f6367666f72" +
        "67652e6d6f64656c5f7375706572766973696f6e5f73616d706c652e7631000000086f6367666f726765050001000000002e6f6367666f7267652e70" +
        "68617365362e7461736b372e696e7075745f6d6174657269616c697a6174696f6e2e7631000000086f6367666f72676503000100000000356f636766" +
        "6f7267652e7068617365362e7461736b372e696e7075745f6d6174657269616c697a6174696f6e5f636f6e6669672e7631000000086f6367666f7267" +
        "65020001010000006d7068617365365f7461736b375f696e7075745f6d6174657269616c697a6174696f6e5f636f6e6669672e76312e323066333934" +
        "633838386539353934343666613236336333353230663364643362316634386233613233653538333733646137313533613639316162316537610000" +
        "00296f6367666f7267652d69676e69732e72756e74696d652d62756e646c652d6964656e746974792e76310000002a6f6367666f7267652d69676e69" +
        "732e69362e636f6d7061746962696c6974792d70726f66696c652e7631";

    public static void TestCanonicalKat()
    {
        OcgForgeModelContractBundleV1Manifest manifest = CreateManifest();
        OcgForgeModelContractBundlePreflightResultV1 result =
            OcgForgeModelContractBundleV1.TryAccept(manifest);

        Require(result.IsSuccess, "the frozen manifest must be accepted");
        Require(result.Bundle is not null, "accepted result must contain a bundle");

        OcgForgeModelContractBundleV1 bundle = result.Bundle!;
        byte[] expectedBytes = Convert.FromHexString(ExpectedCanonicalHex);
        byte[] actualBytes = bundle.CanonicalBytes.ToArray();

        Require(actualBytes.SequenceEqual(expectedBytes), "canonical bytes differ from the independent KAT");
        Require(actualBytes.Length == ExpectedCanonicalLength, "canonical byte length differs from the independent KAT");
        Require(Convert.ToHexString(SHA256.HashData(actualBytes)).ToLowerInvariant() == ExpectedCanonicalSha256,
            "canonical SHA-256 differs from the independent KAT");
        Require(bundle.Identity == ExpectedBundleIdentity, "bundle identity differs from the independent KAT");
    }

    public static void TestRegistryOrderAndFlags()
    {
        OcgForgeModelContractBundlePreflightResultV1 result =
            OcgForgeModelContractBundleV1.TryAccept(CreateManifest());
        Require(result.IsSuccess && result.Bundle is not null, "the frozen manifest must be accepted");

        IReadOnlyList<OcgForgeModelContractRegistryEntryV1> registry = result.Bundle!.Registry;
        Require(registry.Count == 15, "the registry must contain exactly fifteen entries");
        for (int index = 0; index < registry.Count; index++)
        {
            OcgForgeModelContractRegistryEntryV1 entry = registry[index];
            (byte authority, byte runtime, byte canonical, byte identity) = ExpectedFlags[index];
            Require(entry.ContractId == ExpectedContractIds[index], $"registry order differs at index {index}");
            Require(entry.OwnerRepositoryId == "ocgforge", $"registry owner differs at index {index}");
            Require(entry.AuthorityClass == authority, $"authority differs at index {index}");
            Require(entry.RuntimeRequired == runtime, $"runtime flag differs at index {index}");
            Require(entry.CanonicalBytesRequired == canonical, $"canonical flag differs at index {index}");
            Require(entry.IdentityRequired == identity, $"identity flag differs at index {index}");
        }
    }

    public static void TestManifestMismatchMatrix()
    {
        AssertRejected(
            new OcgForgeModelContractBundleV1Manifest(
                "wrong-domain",
                ExpectedIdentityDomain,
                ExpectedSourceCommit,
                ExpectedP5ExecutionHead,
                Entries(),
                ExpectedTask7ConfigIdentity,
                ExpectedRuntimeContractId,
                ExpectedCompatibilityProfile),
            I6BErrorCode.UnsupportedContractValue,
            "wrong identity domain");

        AssertRejected(
            new OcgForgeModelContractBundleV1Manifest(
                ExpectedIdentityDomain,
                "wrong-schema",
                ExpectedSourceCommit,
                ExpectedP5ExecutionHead,
                Entries(),
                ExpectedTask7ConfigIdentity,
                ExpectedRuntimeContractId,
                ExpectedCompatibilityProfile),
            I6BErrorCode.UnsupportedContractValue,
            "wrong schema id");

        AssertRejected(
            CreateManifest(sourceCommit: "main"),
            I6BErrorCode.SourceIdentityMismatch,
            "floating source reference");
        AssertRejected(
            CreateManifest(sourceCommit: "2edfcabf51dd914f96adc4df903b1ac2a9d20e5f"),
            I6BErrorCode.SourceIdentityMismatch,
            "wrong source commit");
        AssertRejected(
            CreateManifest(p5Head: "3c99e86c487361fc4e0f5f12678b4867e59232b8"),
            I6BErrorCode.SourceIdentityMismatch,
            "wrong P5 execution head");

        List<OcgForgeModelContractRegistryEntryV1> swapped = Entries();
        (swapped[2], swapped[3]) = (swapped[3], swapped[2]);
        AssertRejected(CreateManifest(registry: swapped), I6BErrorCode.RegistryMismatch, "reordered registry");

        List<OcgForgeModelContractRegistryEntryV1> duplicated = Entries();
        duplicated[9] = duplicated[8];
        AssertRejected(CreateManifest(registry: duplicated), I6BErrorCode.RegistryMismatch, "duplicated registry entry");

        List<OcgForgeModelContractRegistryEntryV1> missing = Entries();
        missing.RemoveAt(missing.Count - 1);
        AssertRejected(CreateManifest(registry: missing), I6BErrorCode.RegistryMismatch, "missing registry entry");

        List<OcgForgeModelContractRegistryEntryV1> unknown = Entries();
        unknown[0] = new OcgForgeModelContractRegistryEntryV1("ocgforge.unknown.v1", "ocgforge", 0, 1, 1, 1);
        AssertRejected(CreateManifest(registry: unknown), I6BErrorCode.RegistryMismatch, "unknown registry entry");

        List<OcgForgeModelContractRegistryEntryV1> wrongOwner = Entries();
        wrongOwner[0] = new OcgForgeModelContractRegistryEntryV1(ExpectedContractIds[0], "OCGForge", 0, 1, 1, 1);
        AssertRejected(CreateManifest(registry: wrongOwner), I6BErrorCode.RegistryMismatch, "wrong owner");

        List<OcgForgeModelContractRegistryEntryV1> wrongAuthority = Entries();
        wrongAuthority[0] = new OcgForgeModelContractRegistryEntryV1(ExpectedContractIds[0], "ocgforge", 1, 1, 1, 1);
        AssertRejected(CreateManifest(registry: wrongAuthority), I6BErrorCode.RegistryMismatch, "wrong authority");

        List<OcgForgeModelContractRegistryEntryV1> wrongRuntimeFlag = Entries();
        wrongRuntimeFlag[0] = new OcgForgeModelContractRegistryEntryV1(ExpectedContractIds[0], "ocgforge", 0, 2, 1, 1);
        AssertRejected(CreateManifest(registry: wrongRuntimeFlag), I6BErrorCode.RegistryMismatch, "wrong runtime flag");

        List<OcgForgeModelContractRegistryEntryV1> wrongCanonicalFlag = Entries();
        wrongCanonicalFlag[0] = new OcgForgeModelContractRegistryEntryV1(ExpectedContractIds[0], "ocgforge", 0, 1, 0, 1);
        AssertRejected(CreateManifest(registry: wrongCanonicalFlag), I6BErrorCode.RegistryMismatch, "wrong canonical flag");

        List<OcgForgeModelContractRegistryEntryV1> wrongIdentityFlag = Entries();
        wrongIdentityFlag[0] = new OcgForgeModelContractRegistryEntryV1(ExpectedContractIds[0], "ocgforge", 0, 1, 1, 0);
        AssertRejected(CreateManifest(registry: wrongIdentityFlag), I6BErrorCode.RegistryMismatch, "wrong identity flag");

        AssertRejected(
            CreateManifest(task7Config: ExpectedTask7ConfigIdentity[..^1] + "b"),
            I6BErrorCode.ConfigurationIdentityMismatch,
            "wrong Task7 configuration identity");
        AssertRejected(
            CreateManifest(runtimeContractId: "ocgforge-ignis.runtime-bundle-identity.v2"),
            I6BErrorCode.ConfigurationIdentityMismatch,
            "wrong Ignis runtime contract id");
        AssertRejected(
            CreateManifest(profile: "ocgforge-ignis.i6.compatibility-profile.v2"),
            I6BErrorCode.ConfigurationIdentityMismatch,
            "wrong compatibility profile");

        OcgForgeModelContractBundlePreflightResultV1 nullResult =
            OcgForgeModelContractBundleV1.TryAccept(null);
        Require(!nullResult.IsSuccess && nullResult.Bundle is null && nullResult.Error is not null,
            "null manifest must be rejected atomically");
        Require(nullResult.Error!.Code == I6BErrorCode.InvalidManifest, "null manifest error code differs");
    }

    public static void TestFailureAtomicityAndImmutability()
    {
        List<OcgForgeModelContractRegistryEntryV1> callerEntries = Entries();
        OcgForgeModelContractBundleV1Manifest manifest = CreateManifest(registry: callerEntries);
        callerEntries[0] = new OcgForgeModelContractRegistryEntryV1("ocgforge.changed.v1", "ocgforge", 0, 1, 1, 1);

        OcgForgeModelContractBundlePreflightResultV1 result =
            OcgForgeModelContractBundleV1.TryAccept(manifest);
        Require(result.IsSuccess && result.Bundle is not null, "manifest must snapshot caller-owned registry input");

        OcgForgeModelContractBundleV1 bundle = result.Bundle!;
        byte[] firstBytes = bundle.CanonicalBytes;
        firstBytes[0] ^= 0xff;
        Require(bundle.CanonicalBytes.SequenceEqual(Convert.FromHexString(ExpectedCanonicalHex)),
            "caller mutation must not alter canonical bytes");

        IList<OcgForgeModelContractRegistryEntryV1> registryView =
            (IList<OcgForgeModelContractRegistryEntryV1>)bundle.Registry;
        bool mutationRejected = false;
        try
        {
            registryView[0] = callerEntries[0];
        }
        catch (NotSupportedException)
        {
            mutationRejected = true;
        }

        Require(mutationRejected, "registry view must be read-only");
        Require(bundle.Registry[0].ContractId == ExpectedContractIds[0],
            "rejected registry mutation must not alter the bundle");

        AssertRejected(
            CreateManifest(registry: Entries().Take(14).ToArray()),
            I6BErrorCode.RegistryMismatch,
            "failed manifest must not create a partial bundle");
    }

    public static void EmitDeterministicEvidence()
    {
        OcgForgeModelContractBundlePreflightResultV1 result =
            OcgForgeModelContractBundleV1.TryAccept(CreateManifest());
        Require(result.IsSuccess && result.Bundle is not null, "deterministic evidence requires an accepted manifest");

        byte[] bytes = result.Bundle!.CanonicalBytes;
        string sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        Console.WriteLine($"I6B_BUNDLE_IDENTITY={result.Bundle.Identity}");
        Console.WriteLine($"I6B_CANONICAL_SHA256={sha256}");
        Console.WriteLine($"I6B_CANONICAL_LENGTH={bytes.Length}");
    }

    internal static OcgForgeModelContractBundleV1Manifest CreateManifest(
        IEnumerable<OcgForgeModelContractRegistryEntryV1>? registry = null,
        string? identityDomain = null,
        string? schemaId = null,
        string? sourceCommit = null,
        string? p5Head = null,
        string? task7Config = null,
        string? runtimeContractId = null,
        string? profile = null)
    {
        return new OcgForgeModelContractBundleV1Manifest(
            identityDomain ?? ExpectedIdentityDomain,
            schemaId ?? ExpectedIdentityDomain,
            sourceCommit ?? ExpectedSourceCommit,
            p5Head ?? ExpectedP5ExecutionHead,
            registry ?? Entries(),
            task7Config ?? ExpectedTask7ConfigIdentity,
            runtimeContractId ?? ExpectedRuntimeContractId,
            profile ?? ExpectedCompatibilityProfile);
    }

    private static List<OcgForgeModelContractRegistryEntryV1> Entries()
    {
        return ExpectedContractIds
            .Select((contractId, index) =>
            {
                (byte authority, byte runtime, byte canonical, byte identity) = ExpectedFlags[index];
                return new OcgForgeModelContractRegistryEntryV1(
                    contractId,
                    "ocgforge",
                    authority,
                    runtime,
                    canonical,
                    identity);
            })
            .ToList();
    }

    private static void AssertRejected(
        OcgForgeModelContractBundleV1Manifest manifest,
        I6BErrorCode expectedCode,
        string caseName)
    {
        OcgForgeModelContractBundlePreflightResultV1 result =
            OcgForgeModelContractBundleV1.TryAccept(manifest);
        Require(!result.IsSuccess, $"{caseName} must be rejected");
        Require(result.Bundle is null, $"{caseName} must not expose an accepted bundle");
        Require(result.Error is not null, $"{caseName} must expose a structured error");
        Require(result.Error!.Code == expectedCode, $"{caseName} returned the wrong error code");
        Require(!result.Error.Reason.Contains("\\", StringComparison.Ordinal) &&
                !result.Error.Reason.Contains("/", StringComparison.Ordinal),
            $"{caseName} error must not expose a path");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
