using System.Security.Cryptography;

namespace OCGForge.Ignis.Model;

public enum OcgForgeTask7MaterializationErrorCodeV1 : byte
{
    UnknownSchema,
    SourceAssociationMismatch,
    ModelInputIdentityMismatch,
    CardVocabularyMismatch,
    RaggedReconstructionMismatch,
    InvalidOffset,
    OffsetOverflow,
    CandidateCountMismatch,
    RoutingSidecarMismatch,
    OptionalPresenceMismatch,
    ReferenceTypeMismatch,
    ChainStateMismatch,
    InvalidLimb,
    InvalidBoolean,
    InvalidPadding,
    PadOnRealRow,
    ForbiddenSource,
    CanonicalizationFailure,
    InternalFailure
}

public readonly record struct OcgForgeTask7MaterializationErrorV1(
    OcgForgeTask7MaterializationErrorCodeV1 Code,
    string FieldPath);

public sealed class OcgForgeTask7MaterializationSourceSampleV1
{
    internal OcgForgeTask7MaterializationSourceSampleV1(
        OcgForgeLogicalModelInputV1 logical,
        OcgForgeEncodedModelInputV1 encoded,
        OcgForgeCardVocabularyV1 vocabulary,
        string modelInputIdentity,
        string cardVocabularyIdentity)
    {
        Logical = logical ?? throw new ArgumentNullException(nameof(logical));
        Encoded = encoded ?? throw new ArgumentNullException(nameof(encoded));
        Vocabulary = vocabulary ?? throw new ArgumentNullException(nameof(vocabulary));
        ModelInputIdentity = modelInputIdentity ??
            throw new ArgumentNullException(nameof(modelInputIdentity));
        CardVocabularyIdentity = cardVocabularyIdentity ??
            throw new ArgumentNullException(nameof(cardVocabularyIdentity));
    }

    public OcgForgeLogicalModelInputV1 Logical { get; }

    public OcgForgeEncodedModelInputV1 Encoded { get; }

    public OcgForgeCardVocabularyV1 Vocabulary { get; }

    public string ModelInputIdentity { get; }

    public string CardVocabularyIdentity { get; }
}

public sealed class OcgForgeTask7MaterializationSourceSampleResultV1
{
    private OcgForgeTask7MaterializationSourceSampleResultV1(
        bool isSuccess,
        OcgForgeTask7MaterializationErrorV1? error,
        OcgForgeTask7MaterializationSourceSampleV1? value)
    {
        IsSuccess = isSuccess;
        Error = error;
        Value = value;
    }

    public bool IsSuccess { get; }

    public OcgForgeTask7MaterializationErrorV1? Error { get; }

    public OcgForgeTask7MaterializationSourceSampleV1? Value { get; }

    internal static OcgForgeTask7MaterializationSourceSampleResultV1 Success(
        OcgForgeTask7MaterializationSourceSampleV1 value) =>
        new(true, null, value);

    internal static OcgForgeTask7MaterializationSourceSampleResultV1 Failure(
        OcgForgeTask7MaterializationErrorCodeV1 code,
        string fieldPath) =>
        new(false, new(code, fieldPath), null);
}

public sealed class OcgForgeTask7MaterializationSourceBatchV1
{
    internal OcgForgeTask7MaterializationSourceBatchV1(
        OcgForgeRaggedModelBatchV1 ragged,
        IEnumerable<OcgForgeTask7MaterializationSourceSampleV1> samples)
    {
        Ragged = ragged ?? throw new ArgumentNullException(nameof(ragged));
        this.samples = (samples ?? throw new ArgumentNullException(nameof(samples)))
            .ToArray();
        Samples = Array.AsReadOnly(this.samples);
    }

    private readonly OcgForgeTask7MaterializationSourceSampleV1[] samples;

    public OcgForgeRaggedModelBatchV1 Ragged { get; }

    public IReadOnlyList<OcgForgeTask7MaterializationSourceSampleV1> Samples { get; }
}

public sealed class OcgForgeTask7MaterializationSourceBatchResultV1
{
    private OcgForgeTask7MaterializationSourceBatchResultV1(
        bool isSuccess,
        OcgForgeTask7MaterializationErrorV1? error,
        OcgForgeTask7MaterializationSourceBatchV1? value)
    {
        IsSuccess = isSuccess;
        Error = error;
        Value = value;
    }

    public bool IsSuccess { get; }

    public OcgForgeTask7MaterializationErrorV1? Error { get; }

    public OcgForgeTask7MaterializationSourceBatchV1? Value { get; }

    internal static OcgForgeTask7MaterializationSourceBatchResultV1 Success(
        OcgForgeTask7MaterializationSourceBatchV1 value) =>
        new(true, null, value);

    internal static OcgForgeTask7MaterializationSourceBatchResultV1 Failure(
        OcgForgeTask7MaterializationErrorCodeV1 code,
        string fieldPath) =>
        new(false, new(code, fieldPath), null);
}

public sealed class OcgForgeTask7MaterializedSampleV1
{
    internal OcgForgeTask7MaterializedSampleV1(
        string modelInputIdentity,
        string cardVocabularyIdentity,
        string publicObservationDigest,
        string? publicCandidateDomainDigest,
        uint candidateCount,
        IEnumerable<byte> canonicalBytes)
    {
        ModelInputIdentity = modelInputIdentity ??
            throw new ArgumentNullException(nameof(modelInputIdentity));
        CardVocabularyIdentity = cardVocabularyIdentity ??
            throw new ArgumentNullException(nameof(cardVocabularyIdentity));
        PublicObservationDigest = publicObservationDigest ??
            throw new ArgumentNullException(nameof(publicObservationDigest));
        PublicCandidateDomainDigest = publicCandidateDomainDigest;
        CandidateCount = candidateCount;
        this.canonicalBytes = (canonicalBytes ??
            throw new ArgumentNullException(nameof(canonicalBytes))).ToArray();
    }

    private readonly byte[] canonicalBytes;

    public string ModelInputIdentity { get; }

    public string CardVocabularyIdentity { get; }

    public string PublicObservationDigest { get; }

    public string? PublicCandidateDomainDigest { get; }

    public uint CandidateCount { get; }

    public byte[] CanonicalBytes => canonicalBytes.ToArray();
}

public sealed class OcgForgeTask7MaterializedBatchV1
{
    internal OcgForgeTask7MaterializedBatchV1(
        string schemaId,
        string configurationIdentity,
        IEnumerable<OcgForgeTask7MaterializedSampleV1> samples)
    {
        SchemaId = schemaId ?? throw new ArgumentNullException(nameof(schemaId));
        ConfigurationIdentity = configurationIdentity ??
            throw new ArgumentNullException(nameof(configurationIdentity));
        this.samples = (samples ?? throw new ArgumentNullException(nameof(samples)))
            .ToArray();
        Samples = Array.AsReadOnly(this.samples);
    }

    private readonly OcgForgeTask7MaterializedSampleV1[] samples;

    public string SchemaId { get; }

    public string ConfigurationIdentity { get; }

    public IReadOnlyList<OcgForgeTask7MaterializedSampleV1> Samples { get; }
}

public sealed class OcgForgeTask7MaterializationResultV1
{
    private OcgForgeTask7MaterializationResultV1(
        bool isSuccess,
        OcgForgeTask7MaterializationErrorV1? error,
        OcgForgeTask7MaterializedBatchV1? value)
    {
        IsSuccess = isSuccess;
        Error = error;
        Value = value;
    }

    public bool IsSuccess { get; }

    public OcgForgeTask7MaterializationErrorV1? Error { get; }

    public OcgForgeTask7MaterializedBatchV1? Value { get; }

    internal static OcgForgeTask7MaterializationResultV1 Success(
        OcgForgeTask7MaterializedBatchV1 value) =>
        new(true, null, value);

    internal static OcgForgeTask7MaterializationResultV1 Failure(
        OcgForgeTask7MaterializationErrorCodeV1 code,
        string fieldPath) =>
        new(false, new(code, fieldPath), null);
}

public static class OcgForgeTask7InputMaterializationV1
{
    public const string SchemaId =
        "ocgforge.phase6.task7.input_materialization.v1";

    public const string ConfigurationSchemaId =
        "ocgforge.phase6.task7.input_materialization_config.v1";

    public const string ConfigurationIdentityPrefix =
        "phase6_task7_input_materialization_config.v1.";

    private const string FrozenSourceCommit =
        "f929de0b4d4157327dba003067d2e21e42f7ad75";
    private const string FrozenP5AcceptanceHead =
        "3c99e86c487361fc4e0f5f12678b4867e59232b7";
    private const string FrozenBundleDomain =
        "ocgforge-ignis.i6.model-contract-bundle.v1";
    private const string FrozenConfigIdentity =
        ConfigurationIdentityPrefix +
        "20f394c888e959446fa263c3520f3dd3b1f48b3a23e58373da7153a691ab1e7a";

    private static readonly string[] RequiredContractIds =
    {
        OcgForgeLogicalModelInputV1.SchemaId,
        OcgForgeEncodedModelInputV1.SchemaId,
        OcgForgeCardVocabularyV1.SchemaId,
        OcgForgeModelInputIdentityV1.SchemaId,
        OcgForgeModelBatchLayoutV1.SchemaId,
        SchemaId,
        ConfigurationSchemaId
    };

    private sealed record ReferenceComponent(
        string Name,
        string SourceType,
        string PresenceRule);

    private sealed record ReferenceDescriptor(
        string Name,
        IReadOnlyList<ReferenceComponent> Components);

    private sealed record ColumnDescriptor(
        string Name,
        string SourceType,
        byte LimbCount,
        string PresenceRule,
        string PaddingRule);

    private sealed record TableDescriptor(
        string Name,
        string Kind,
        string RowOrder,
        string? Parent,
        string? ParentOffset,
        string RowMaskRule,
        IReadOnlyList<ColumnDescriptor> Columns);

    private sealed record RuleDescriptor(string Id, string Value);

    private static readonly IReadOnlyList<ReferenceDescriptor> ReferenceDescriptors =
        new ReferenceDescriptor[]
        {
            new(
                "R",
                new ReferenceComponent[]
                {
                    new("public_locator_ordinal", "U32", "required"),
                    new("current_entity_ordinal", "P<U32>", "optional")
                }),
            new(
                "OR",
                new ReferenceComponent[]
                {
                    new("present", "Bool", "required"),
                    new("reference", "R", "composite_defined")
                }),
            new(
                "CR",
                new ReferenceComponent[]
                {
                    new("present", "Bool", "required"),
                    new("kind_code", "U8", "composite_defined"),
                    new("reference", "R", "composite_defined")
                }),
            new(
                "HR",
                new ReferenceComponent[]
                {
                    new("present", "Bool", "required"),
                    new("public_locator_ordinal", "U32", "composite_defined")
                })
        };

    private static ColumnDescriptor Column(
        string name,
        string sourceType,
        byte limbCount,
        string presenceRule,
        string paddingRule) =>
        new(name, sourceType, limbCount, presenceRule, paddingRule);

    private static readonly IReadOnlyList<TableDescriptor> TableDescriptors =
        new TableDescriptor[]
        {
            new(
                "sample_header",
                "singleton",
                "sample_order",
                null,
                null,
                "singleton_all_true",
                new ColumnDescriptor[]
                {
                    Column("perspective_player", "U8", 1, "required", "zero"),
                    Column("decision_index", "U64", 4, "required", "zero"),
                    Column("public_observation_context_kind_code", "P<U16>", 1, "optional", "zero"),
                    Column("public_observation_context_player", "P<U8>", 1, "optional", "zero"),
                    Column("public_locator_count", "U32", 2, "required", "zero"),
                    Column("candidate_count", "U32", 2, "required", "zero")
                }),
            new(
                "globals",
                "singleton",
                "sample_order",
                null,
                null,
                "singleton_all_true",
                new ColumnDescriptor[]
                {
                    Column("duel_flags", "U64", 4, "required", "zero"),
                    Column("player_to_act", "P<U8>", 1, "optional", "zero"),
                    Column("turn_player", "P<U8>", 1, "optional", "zero"),
                    Column("turn_count", "P<U32>", 2, "optional", "zero"),
                    Column("phase", "P<U32>", 2, "optional", "zero"),
                    Column("chain_length", "U32", 2, "required", "zero"),
                    Column("winner", "P<U8>", 1, "optional", "zero"),
                    Column("win_reason", "P<U8>", 1, "optional", "zero"),
                    Column("terminal", "Bool", 0, "required", "false")
                }),
            new(
                "chain_state",
                "singleton",
                "sample_order",
                null,
                null,
                "singleton_all_true",
                new ColumnDescriptor[]
                {
                    Column("length", "U32", 2, "required", "zero")
                }),
            new(
                "match_context",
                "singleton",
                "sample_order",
                null,
                null,
                "singleton_all_true",
                new ColumnDescriptor[]
                {
                    Column("perspective_player", "U8", 1, "required", "zero"),
                    Column("duel_flags", "U64", 4, "required", "zero"),
                    Column("own_decklist_known", "Bool", 0, "required", "false"),
                    Column("opponent_decklist_known", "Bool", 0, "required", "false"),
                    Column("own_deck_known", "Bool", 0, "required", "false"),
                    Column("opponent_deck_known", "Bool", 0, "required", "false")
                }),
            new(
                "life_points",
                "ragged",
                "life_point_source_order",
                null,
                null,
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("value", "U32", 2, "required", "zero")
                }),
            new(
                "decision_context_references",
                "ragged",
                "public_observation_context_reference_order",
                null,
                null,
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("public_locator_ordinal", "U32", 2, "required", "zero")
                }),
            new(
                "zones",
                "ragged",
                "public_safe_state_zone_order",
                null,
                null,
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("player", "U8", 1, "required", "zero"),
                    Column("kind_code", "U8", 1, "required", "zero"),
                    Column("total_count", "U32", 2, "required", "zero"),
                    Column("public_identity_count", "U32", 2, "required", "zero"),
                    Column("hidden_count", "U32", 2, "required", "zero"),
                    Column("player_observable_order", "Bool", 0, "required", "false")
                }),
            new(
                "entities",
                "ragged",
                "canonical_locator_order",
                null,
                null,
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("public_locator_ordinal", "U32", 2, "required", "zero"),
                    Column("identity_known", "Bool", 0, "required", "false"),
                    Column("card_vocabulary_id", "U32", 2, "required", "pad_id_zero"),
                    Column("owner", "P<U8>", 1, "optional", "zero"),
                    Column("controller", "P<U8>", 1, "optional", "zero"),
                    Column("zone_code", "U8", 1, "required", "zero"),
                    Column("sequence", "P<U32>", 2, "optional", "zero"),
                    Column("overlay_sequence", "P<U32>", 2, "optional", "zero"),
                    Column("position_code", "U8", 1, "required", "zero"),
                    Column("face_up", "Bool", 0, "required", "false"),
                    Column("face_down", "Bool", 0, "required", "false")
                }),
            new(
                "entity_properties",
                "child",
                "entity_property_role_order",
                "entities",
                "entity_property_offsets",
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("property_role", "U8", 1, "required", "zero"),
                    Column("property_present", "Bool", 0, "required", "false"),
                    Column("type", "P<U32>", 2, "optional", "zero"),
                    Column("attribute", "P<U32>", 2, "optional", "zero"),
                    Column("race", "P<U64>", 4, "optional", "zero"),
                    Column("attack", "P<I32>", 2, "optional", "zero"),
                    Column("defense", "P<I32>", 2, "optional", "zero"),
                    Column("base_attack", "P<I32>", 2, "optional", "zero"),
                    Column("base_defense", "P<I32>", 2, "optional", "zero"),
                    Column("level", "P<U32>", 2, "optional", "zero"),
                    Column("rank", "P<U32>", 2, "optional", "zero"),
                    Column("link_rating", "P<U32>", 2, "optional", "zero"),
                    Column("left_scale", "P<U32>", 2, "optional", "zero"),
                    Column("right_scale", "P<U32>", 2, "optional", "zero"),
                    Column("status_flags", "P<U32>", 2, "optional", "zero")
                }),
            new(
                "property_link_markers",
                "child",
                "property_link_marker_source_order",
                "entity_properties",
                "property_link_marker_offsets",
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("link_marker_code", "U8", 1, "required", "zero")
                }),
            new(
                "property_counters",
                "child",
                "property_counter_source_order",
                "entity_properties",
                "property_counter_offsets",
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("type", "U32", 2, "required", "zero"),
                    Column("count", "U32", 2, "required", "zero")
                }),
            new(
                "relationships",
                "ragged",
                "relationship_source_order",
                null,
                null,
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("kind_code", "U8", 1, "required", "zero"),
                    Column("source", "R", 0, "composite_defined", "not_applicable"),
                    Column("target", "R", 0, "composite_defined", "not_applicable")
                }),
            new(
                "chain_links",
                "ragged",
                "chain_link_source_order",
                null,
                null,
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("index", "U32", 2, "required", "zero"),
                    Column("activating_player", "P<U8>", 1, "optional", "zero"),
                    Column("source", "OR", 0, "composite_defined", "not_applicable"),
                    Column("activation_zone_code", "P<U8>", 1, "optional", "zero"),
                    Column("effect_description", "P<U64>", 4, "optional", "zero")
                }),
            new(
                "chain_targets",
                "child",
                "chain_target_source_order",
                "chain_links",
                "chain_target_offsets",
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("target", "R", 0, "composite_defined", "not_applicable")
                }),
            new(
                "visible_events",
                "ragged",
                "visible_event_source_order",
                null,
                null,
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("event_index", "U64", 4, "required", "zero"),
                    Column("kind_code", "U8", 1, "required", "zero"),
                    Column("player", "P<U8>", 1, "optional", "zero"),
                    Column("entity", "HR", 0, "composite_defined", "not_applicable"),
                    Column("public_card_vocabulary_id", "P<U32>", 2, "optional", "zero"),
                    Column("from_zone_code", "P<U8>", 1, "optional", "zero"),
                    Column("to_zone_code", "P<U8>", 1, "optional", "zero"),
                    Column("count", "P<U32>", 2, "optional", "zero"),
                    Column("amount", "P<I32>", 2, "optional", "zero"),
                    Column("counter_type", "P<U32>", 2, "optional", "zero"),
                    Column("phase", "P<U32>", 2, "optional", "zero"),
                    Column("winner", "P<U8>", 1, "optional", "zero"),
                    Column("win_reason", "P<U8>", 1, "optional", "zero"),
                    Column("effect_description", "P<U64>", 4, "optional", "zero")
                }),
            new(
                "visible_event_targets",
                "child",
                "visible_event_target_source_order",
                "visible_events",
                "visible_event_target_offsets",
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("public_locator_ordinal", "U32", 2, "required", "zero")
                }),
            new(
                "own_main_deck_ids",
                "ragged",
                "deck_public_safe_order",
                null,
                null,
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("card_vocabulary_id", "U32", 2, "required", "pad_id_zero")
                }),
            new(
                "opponent_main_deck_ids",
                "ragged",
                "deck_public_safe_order",
                null,
                null,
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("card_vocabulary_id", "U32", 2, "required", "pad_id_zero")
                }),
            new(
                "own_extra_deck_ids",
                "ragged",
                "deck_public_safe_order",
                null,
                null,
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("card_vocabulary_id", "U32", 2, "required", "pad_id_zero")
                }),
            new(
                "opponent_extra_deck_ids",
                "ragged",
                "deck_public_safe_order",
                null,
                null,
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("card_vocabulary_id", "U32", 2, "required", "pad_id_zero")
                }),
            new(
                "public_locator_control_sidecar",
                "control_sidecar",
                "public_locator_token_order",
                null,
                null,
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("public_locator_token", "String", 0, "required", "not_applicable")
                }),
            new(
                "candidates",
                "candidate",
                "candidate_source_order",
                null,
                null,
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("action_kind_code", "U16", 1, "required", "zero"),
                    Column("choice_present", "Bool", 0, "required", "false"),
                    Column("choice_kind_code", "U8", 1, "required", "zero"),
                    Column("choice_value", "U64", 4, "required", "zero"),
                    Column("choice_response_index", "P<U32>", 2, "optional", "zero"),
                    Column("source_reference", "CR", 0, "composite_defined", "not_applicable"),
                    Column("target_reference", "CR", 0, "composite_defined", "not_applicable"),
                    Column("phase", "P<U32>", 2, "optional", "zero"),
                    Column("position", "P<U8>", 1, "optional", "zero"),
                    Column("source_index", "P<U32>", 2, "optional", "zero"),
                    Column("amount", "P<I32>", 2, "optional", "zero"),
                    Column("continuation_operation_code", "U8", 1, "required", "zero"),
                    Column("submits_engine_response", "Bool", 0, "required", "false")
                }),
            new(
                "routing_key_control_sidecar",
                "control_sidecar",
                "candidate_source_order",
                null,
                null,
                "real_rows_true",
                new ColumnDescriptor[]
                {
                    Column("public_action_key", "String", 0, "required", "not_applicable")
                })
        };

    private static readonly IReadOnlyList<RuleDescriptor> RuleDescriptors =
        new RuleDescriptor[]
        {
            new("candidate_cardinality", "N_TO_N"),
            new("candidate_order", "SOURCE_ORDER"),
            new("candidate_split", "FORBIDDEN"),
            new("routing_key_learned_feature", "NO"),
            new("raw_locator_learned_feature", "NO"),
            new("padding_semantic", "NO"),
            new("ragged_authority", "RAGGED_FIRST"),
            new("padded_equivalence", "EXACT_UNPAD"),
            new("globals_chain_length_source", "DISTINCT"),
            new("chain_state_length_source", "DISTINCT")
        };

    public static string ConfigurationIdentity =>
        ConfigurationIdentityPrefix +
        Convert.ToHexString(SHA256.HashData(CanonicalConfigurationBytes))
            .ToLowerInvariant();

    public static byte[] CanonicalConfigurationBytes
    {
        get
        {
            OcgForgeI6ECanonicalV1.Writer writer = new();
            writer.String(ConfigurationSchemaId);
            writer.String(SchemaId);
            WriteVector(writer, new[]
            {
                OcgForgeLogicalModelInputV1.SchemaId,
                OcgForgeEncodedModelInputV1.SchemaId,
                OcgForgeCardVocabularyV1.SchemaId,
                OcgForgeModelInputIdentityV1.SchemaId,
                OcgForgeModelBatchLayoutV1.SchemaId
            }, static (output, value) => output.String(value));
            writer.String("u16_most_significant_first");
            writer.String("torch.int64");
            writer.String("torch.bool");
            WriteVector(writer, ReferenceDescriptors, WriteReferenceDescriptor);
            WriteVector(writer, TableDescriptors, WriteTableDescriptor);
            WriteVector(writer, RuleDescriptors, WriteRuleDescriptor);
            return writer.ToArray();
        }
    }

    public static ushort[] U8Limbs(byte value) => new[] { (ushort)value };

    public static ushort[] U16Limbs(ushort value) => new[] { value };

    public static ushort[] U32Limbs(uint value) => new[]
    {
        checked((ushort)(value >> 16)),
        unchecked((ushort)value)
    };

    public static ushort[] U64Limbs(ulong value) => new[]
    {
        unchecked((ushort)(value >> 48)),
        unchecked((ushort)(value >> 32)),
        unchecked((ushort)(value >> 16)),
        unchecked((ushort)value)
    };

    public static ushort[] I32Limbs(int value) => U32Limbs(unchecked((uint)value));

    public static OcgForgeTask7MaterializationSourceSampleResultV1 TryCreateSourceSample(
        OcgForgeModelContractBundleV1? bundle,
        OcgForgeLogicalModelInputV1? logical,
        OcgForgeEncodedModelInputV1? encoded,
        OcgForgeCardVocabularyV1? vocabulary)
    {
        if (!IsExactBundle(bundle))
        {
            return OcgForgeTask7MaterializationSourceSampleResultV1.Failure(
                OcgForgeTask7MaterializationErrorCodeV1.UnknownSchema,
                "bundle");
        }

        if (logical is null || encoded is null || vocabulary is null)
        {
            return OcgForgeTask7MaterializationSourceSampleResultV1.Failure(
                OcgForgeTask7MaterializationErrorCodeV1.SourceAssociationMismatch,
                "source");
        }

        try
        {
            string vocabularyIdentity = vocabulary.Identity;
            if (encoded.CardVocabularyIdentity != vocabularyIdentity)
            {
                return OcgForgeTask7MaterializationSourceSampleResultV1.Failure(
                    OcgForgeTask7MaterializationErrorCodeV1.CardVocabularyMismatch,
                    "card_vocabulary_identity");
            }

            if (!VocabularyIdsMatch(encoded, vocabulary))
            {
                return OcgForgeTask7MaterializationSourceSampleResultV1.Failure(
                    OcgForgeTask7MaterializationErrorCodeV1.CardVocabularyMismatch,
                    "card_vocabulary_mapping");
            }

            OcgForgeModelInputIdentityResultV1 identity =
                OcgForgeModelInputIdentityV1.TryCreate(logical, encoded, vocabulary);
            if (!identity.IsSuccess || identity.Identity is null)
            {
                return OcgForgeTask7MaterializationSourceSampleResultV1.Failure(
                    OcgForgeTask7MaterializationErrorCodeV1.ModelInputIdentityMismatch,
                    identity.FieldPath ?? "model_input_identity");
            }

            _ = encoded.CanonicalBytes;
            return OcgForgeTask7MaterializationSourceSampleResultV1.Success(
                new OcgForgeTask7MaterializationSourceSampleV1(
                    logical,
                    encoded,
                    vocabulary,
                    identity.Identity,
                    vocabularyIdentity));
        }
        catch (ArgumentException)
        {
            return OcgForgeTask7MaterializationSourceSampleResultV1.Failure(
                OcgForgeTask7MaterializationErrorCodeV1.CanonicalizationFailure,
                "source");
        }
        catch (OverflowException)
        {
            return OcgForgeTask7MaterializationSourceSampleResultV1.Failure(
                OcgForgeTask7MaterializationErrorCodeV1.CanonicalizationFailure,
                "source");
        }
    }

    public static OcgForgeTask7MaterializationSourceBatchResultV1 TryCreateSourceBatch(
        OcgForgeModelContractBundleV1? bundle,
        IEnumerable<OcgForgeTask7MaterializationSourceSampleV1>? sourceSamples)
    {
        if (!IsExactBundle(bundle))
        {
            return OcgForgeTask7MaterializationSourceBatchResultV1.Failure(
                OcgForgeTask7MaterializationErrorCodeV1.UnknownSchema,
                "bundle");
        }

        if (sourceSamples is null)
        {
            return OcgForgeTask7MaterializationSourceBatchResultV1.Failure(
                OcgForgeTask7MaterializationErrorCodeV1.SourceAssociationMismatch,
                "samples");
        }

        try
        {
            OcgForgeTask7MaterializationSourceSampleV1[] sources =
                sourceSamples.ToArray();
            if (sources.Length == 0 || sources.Any(source => source is null))
            {
                return OcgForgeTask7MaterializationSourceBatchResultV1.Failure(
                    OcgForgeTask7MaterializationErrorCodeV1.SourceAssociationMismatch,
                    "samples");
            }

            foreach (OcgForgeTask7MaterializationSourceSampleV1 source in sources)
            {
                OcgForgeTask7MaterializationSourceSampleResultV1 validated =
                    TryCreateSourceSample(
                        bundle,
                        source.Logical,
                        source.Encoded,
                        source.Vocabulary);
                if (!validated.IsSuccess || validated.Value is null ||
                    validated.Value.ModelInputIdentity != source.ModelInputIdentity ||
                    validated.Value.CardVocabularyIdentity != source.CardVocabularyIdentity)
                {
                    OcgForgeTask7MaterializationErrorCodeV1 code =
                        validated.Error?.Code ??
                        (validated.Value is not null &&
                         validated.Value.CardVocabularyIdentity != source.CardVocabularyIdentity
                            ? OcgForgeTask7MaterializationErrorCodeV1.CardVocabularyMismatch
                            : OcgForgeTask7MaterializationErrorCodeV1.ModelInputIdentityMismatch);
                    return OcgForgeTask7MaterializationSourceBatchResultV1.Failure(
                        code,
                        validated.Error?.FieldPath ?? "samples");
                }
            }

            OcgForgeRaggedModelBatchResultV1 ragged =
                OcgForgeModelBatchLayoutV1.TryCreate(
                    sources.Select(source => source.Encoded));
            if (!ragged.IsSuccess || ragged.Value is null)
            {
                return OcgForgeTask7MaterializationSourceBatchResultV1.Failure(
                    MapLayoutError(ragged.Error?.Code),
                    "ragged");
            }

            return OcgForgeTask7MaterializationSourceBatchResultV1.Success(
                new OcgForgeTask7MaterializationSourceBatchV1(
                    ragged.Value,
                    sources));
        }
        catch (ArgumentException)
        {
            return OcgForgeTask7MaterializationSourceBatchResultV1.Failure(
                OcgForgeTask7MaterializationErrorCodeV1.SourceAssociationMismatch,
                "samples");
        }
        catch (OverflowException)
        {
            return OcgForgeTask7MaterializationSourceBatchResultV1.Failure(
                OcgForgeTask7MaterializationErrorCodeV1.OffsetOverflow,
                "samples");
        }
    }

    public static OcgForgeTask7MaterializationResultV1 TryMaterialize(
        OcgForgeModelContractBundleV1? bundle,
        IEnumerable<OcgForgeTask7MaterializationSourceSampleV1>? sourceSamples)
    {
        OcgForgeTask7MaterializationSourceBatchResultV1 batch =
            TryCreateSourceBatch(bundle, sourceSamples);
        if (!batch.IsSuccess || batch.Value is null)
        {
            return Failure(
                batch.Error?.Code ??
                    OcgForgeTask7MaterializationErrorCodeV1.SourceAssociationMismatch,
                batch.Error?.FieldPath ?? "samples");
        }

        return MaterializeSourceBatch(bundle, batch.Value);
    }

    public static OcgForgeTask7MaterializationResultV1 TryMaterialize(
        OcgForgeModelContractBundleV1? bundle,
        OcgForgeTask7MaterializationSourceBatchV1? sourceBatch) =>
        MaterializeSourceBatch(bundle, sourceBatch);

    private static OcgForgeTask7MaterializationResultV1 MaterializeSourceBatch(
        OcgForgeModelContractBundleV1? bundle,
        OcgForgeTask7MaterializationSourceBatchV1? sourceBatch)
    {
        if (!IsExactBundle(bundle))
        {
            return Failure(
                OcgForgeTask7MaterializationErrorCodeV1.UnknownSchema,
                "bundle");
        }

        if (sourceBatch is null)
        {
            return Failure(
                OcgForgeTask7MaterializationErrorCodeV1.SourceAssociationMismatch,
                "samples");
        }

        OcgForgeTask7MaterializationSourceSampleV1[] sources;
        try
        {
            sources = sourceBatch.Samples.ToArray();
        }
        catch
        {
            return Failure(
                OcgForgeTask7MaterializationErrorCodeV1.SourceAssociationMismatch,
                "samples");
        }

        if (sources.Length == 0)
        {
            return Failure(
                OcgForgeTask7MaterializationErrorCodeV1.SourceAssociationMismatch,
                "samples");
        }

        try
        {
            foreach (OcgForgeTask7MaterializationSourceSampleV1 source in sources)
            {
                if (source is null)
                {
                    return Failure(
                        OcgForgeTask7MaterializationErrorCodeV1.SourceAssociationMismatch,
                        "samples");
                }
            }

            if (sourceBatch.Ragged is null ||
                sourceBatch.Ragged.BatchSize != (uint)sources.Length ||
                sourceBatch.Ragged.Samples.Count != sources.Length)
            {
                return Failure(
                    OcgForgeTask7MaterializationErrorCodeV1.SourceAssociationMismatch,
                    "ragged");
            }

            OcgForgeRaggedModelBatchV1 ragged = sourceBatch.Ragged;
            List<OcgForgeTask7MaterializedSampleV1> materialized =
                new(sources.Length);
            for (int index = 0; index < sources.Length; index++)
            {
                OcgForgeTask7MaterializationSourceSampleV1 source = sources[index];
                if (source.Logical is null || source.Encoded is null ||
                    source.Vocabulary is null)
                {
                    return Failure(
                        OcgForgeTask7MaterializationErrorCodeV1.SourceAssociationMismatch,
                        $"samples[{index}]");
                }

                string vocabularyIdentity = source.Vocabulary.Identity;
                if (source.CardVocabularyIdentity != vocabularyIdentity ||
                    source.Encoded.CardVocabularyIdentity != vocabularyIdentity)
                {
                    return Failure(
                        OcgForgeTask7MaterializationErrorCodeV1.CardVocabularyMismatch,
                        $"samples[{index}].card_vocabulary_identity");
                }

                if (!VocabularyIdsMatch(source.Encoded, source.Vocabulary))
                {
                    return Failure(
                        OcgForgeTask7MaterializationErrorCodeV1.CardVocabularyMismatch,
                        $"samples[{index}].card_vocabulary_mapping");
                }

                OcgForgeModelInputIdentityResultV1 identity =
                    OcgForgeModelInputIdentityV1.TryCreate(
                        source.Logical,
                        source.Encoded,
                        source.Vocabulary);
                if (!identity.IsSuccess || identity.Identity is null ||
                    identity.Identity != source.ModelInputIdentity)
                {
                    return Failure(
                        OcgForgeTask7MaterializationErrorCodeV1.ModelInputIdentityMismatch,
                        $"samples[{index}].model_input_identity");
                }

                OcgForgeRaggedModelSampleResultV1 reconstructed =
                    OcgForgeModelBatchLayoutV1.TryReconstruct(ragged, index);
                if (!reconstructed.IsSuccess || reconstructed.Value is null ||
                    !reconstructed.Value.CanonicalBytes.SequenceEqual(
                        source.Encoded.CanonicalBytes))
                {
                    return Failure(
                        OcgForgeTask7MaterializationErrorCodeV1.RaggedReconstructionMismatch,
                        $"samples[{index}].ragged");
                }

                if (source.Encoded.CandidateFeatures.Count == 0 ||
                    source.Encoded.CandidateFeatures.Count !=
                        source.Encoded.RoutingKeys.Count)
                {
                    return Failure(
                        OcgForgeTask7MaterializationErrorCodeV1.CandidateCountMismatch,
                        $"samples[{index}].candidates");
                }

                byte[] canonicalBytes = CanonicalSampleBytes(
                    source.Encoded,
                    identity.Identity,
                    vocabularyIdentity,
                    ConfigurationIdentity);
                materialized.Add(new OcgForgeTask7MaterializedSampleV1(
                    identity.Identity,
                    vocabularyIdentity,
                    source.Encoded.PublicObservationDigest,
                    source.Encoded.PublicCandidateDomainDigest,
                    checked((uint)source.Encoded.CandidateFeatures.Count),
                    canonicalBytes));
            }

            return OcgForgeTask7MaterializationResultV1.Success(
                new OcgForgeTask7MaterializedBatchV1(
                    SchemaId,
                    ConfigurationIdentity,
                    materialized));
        }
        catch (OcgForgeTask7Failure failure)
        {
            return Failure(failure.Code, failure.FieldPath);
        }
        catch (ArgumentException)
        {
            return Failure(
                OcgForgeTask7MaterializationErrorCodeV1.CanonicalizationFailure,
                "materialization");
        }
        catch (OverflowException)
        {
            return Failure(
                OcgForgeTask7MaterializationErrorCodeV1.OffsetOverflow,
                "materialization");
        }
        catch
        {
            return Failure(
                OcgForgeTask7MaterializationErrorCodeV1.InternalFailure,
                "materialization");
        }
    }

    public static string ErrorCodeName(
        OcgForgeTask7MaterializationErrorCodeV1 code) =>
        code switch
        {
            OcgForgeTask7MaterializationErrorCodeV1.UnknownSchema => "unknown_schema",
            OcgForgeTask7MaterializationErrorCodeV1.SourceAssociationMismatch => "source_association_mismatch",
            OcgForgeTask7MaterializationErrorCodeV1.ModelInputIdentityMismatch => "model_input_identity_mismatch",
            OcgForgeTask7MaterializationErrorCodeV1.CardVocabularyMismatch => "card_vocabulary_mismatch",
            OcgForgeTask7MaterializationErrorCodeV1.RaggedReconstructionMismatch => "ragged_reconstruction_mismatch",
            OcgForgeTask7MaterializationErrorCodeV1.InvalidOffset => "invalid_offset",
            OcgForgeTask7MaterializationErrorCodeV1.OffsetOverflow => "offset_overflow",
            OcgForgeTask7MaterializationErrorCodeV1.CandidateCountMismatch => "candidate_count_mismatch",
            OcgForgeTask7MaterializationErrorCodeV1.RoutingSidecarMismatch => "routing_sidecar_mismatch",
            OcgForgeTask7MaterializationErrorCodeV1.OptionalPresenceMismatch => "optional_presence_mismatch",
            OcgForgeTask7MaterializationErrorCodeV1.ReferenceTypeMismatch => "reference_type_mismatch",
            OcgForgeTask7MaterializationErrorCodeV1.ChainStateMismatch => "chain_state_mismatch",
            OcgForgeTask7MaterializationErrorCodeV1.InvalidLimb => "invalid_limb",
            OcgForgeTask7MaterializationErrorCodeV1.InvalidBoolean => "invalid_boolean",
            OcgForgeTask7MaterializationErrorCodeV1.InvalidPadding => "invalid_padding",
            OcgForgeTask7MaterializationErrorCodeV1.PadOnRealRow => "pad_on_real_row",
            OcgForgeTask7MaterializationErrorCodeV1.ForbiddenSource => "forbidden_source",
            OcgForgeTask7MaterializationErrorCodeV1.CanonicalizationFailure => "canonicalization_failure",
            _ => "internal_failure"
        };

    private static bool IsExactBundle(OcgForgeModelContractBundleV1? bundle)
    {
        if (bundle is null ||
            bundle.IdentityDomain != FrozenBundleDomain ||
            bundle.SchemaId != FrozenBundleDomain ||
            bundle.OcgForgeSourceCommit != FrozenSourceCommit ||
            bundle.P5AcceptanceExecutionHead != FrozenP5AcceptanceHead ||
            bundle.Task7MaterializationConfigIdentity != FrozenConfigIdentity ||
            ConfigurationIdentity != FrozenConfigIdentity)
        {
            return false;
        }

        return RequiredContractIds.All(required =>
            bundle.Registry.Any(entry =>
                entry is not null &&
                entry.ContractId == required &&
                entry.OwnerRepositoryId == "ocgforge"));
    }

    private static OcgForgeTask7MaterializationResultV1 Failure(
        OcgForgeTask7MaterializationErrorCodeV1 code,
        string fieldPath) =>
        OcgForgeTask7MaterializationResultV1.Failure(code, fieldPath);

    private static OcgForgeTask7MaterializationErrorCodeV1 MapLayoutError(
        OcgForgeModelBatchLayoutErrorCodeV1? code) =>
        code switch
        {
            OcgForgeModelBatchLayoutErrorCodeV1.EmptyBatch =>
                OcgForgeTask7MaterializationErrorCodeV1.SourceAssociationMismatch,
            OcgForgeModelBatchLayoutErrorCodeV1.InvalidEncodedSample =>
                OcgForgeTask7MaterializationErrorCodeV1.CanonicalizationFailure,
            OcgForgeModelBatchLayoutErrorCodeV1.InvalidRaggedLayout =>
                OcgForgeTask7MaterializationErrorCodeV1.InvalidOffset,
            OcgForgeModelBatchLayoutErrorCodeV1.CandidateCountMismatch =>
                OcgForgeTask7MaterializationErrorCodeV1.CandidateCountMismatch,
            OcgForgeModelBatchLayoutErrorCodeV1.OptionalPresenceMismatch =>
                OcgForgeTask7MaterializationErrorCodeV1.OptionalPresenceMismatch,
            OcgForgeModelBatchLayoutErrorCodeV1.CountOverflow =>
                OcgForgeTask7MaterializationErrorCodeV1.OffsetOverflow,
            _ => OcgForgeTask7MaterializationErrorCodeV1.InternalFailure
        };

    private static bool VocabularyIdsMatch(
        OcgForgeEncodedModelInputV1 encoded,
        OcgForgeCardVocabularyV1 vocabulary)
    {
        bool Known(uint id) =>
            id >= 2 && (ulong)(id - 2) < (ulong)vocabulary.AscendingPasscodes.Count;

        bool DeckKnown(OcgForgeEncodedDeckV1 deck) =>
            deck.MainDeck.All(Known) && deck.ExtraDeck.All(Known);

        return encoded.Entities.All(entity =>
                !entity.IdentityKnown || Known(entity.CardVocabularyId)) &&
            DeckKnown(encoded.MatchContext.OwnDeck) &&
            DeckKnown(encoded.MatchContext.OpponentDeck) &&
            encoded.VisibleEvents.All(@event =>
                !@event.PublicCardVocabularyId.HasValue ||
                Known(@event.PublicCardVocabularyId.Value));
    }

    private static void WriteVector<T>(
        OcgForgeI6ECanonicalV1.Writer writer,
        IReadOnlyList<T> values,
        Action<OcgForgeI6ECanonicalV1.Writer, T> write)
    {
        writer.Count(values.Count);
        for (int index = 0; index < values.Count; index++)
        {
            write(writer, values[index]);
        }
    }

    private static void WriteReferenceDescriptor(
        OcgForgeI6ECanonicalV1.Writer writer,
        ReferenceDescriptor descriptor)
    {
        writer.String(descriptor.Name);
        WriteVector(writer, descriptor.Components, static (output, component) =>
        {
            output.String(component.Name);
            output.String(component.SourceType);
            output.String(component.PresenceRule);
        });
    }

    private static void WriteTableDescriptor(
        OcgForgeI6ECanonicalV1.Writer writer,
        TableDescriptor descriptor)
    {
        writer.String(descriptor.Name);
        writer.String(descriptor.Kind);
        writer.String(descriptor.RowOrder);
        writer.OptionalString(descriptor.Parent);
        writer.OptionalString(descriptor.ParentOffset);
        writer.String(descriptor.RowMaskRule);
        WriteVector(writer, descriptor.Columns, static (output, column) =>
        {
            output.String(column.Name);
            output.String(column.SourceType);
            output.U8(column.LimbCount);
            output.String(column.PresenceRule);
            output.String(column.PaddingRule);
        });
    }

    private static void WriteRuleDescriptor(
        OcgForgeI6ECanonicalV1.Writer writer,
        RuleDescriptor descriptor)
    {
        writer.String(descriptor.Id);
        writer.String(descriptor.Value);
    }

    private static byte[] CanonicalSampleBytes(
        OcgForgeEncodedModelInputV1 encoded,
        string modelInputIdentity,
        string cardVocabularyIdentity,
        string configurationIdentity)
    {
        OcgForgeI6ECanonicalV1.Writer writer = new();
        writer.String(SchemaId);
        writer.String(configurationIdentity);
        writer.String(modelInputIdentity);
        WriteVector(writer, new[]
        {
            OcgForgeLogicalModelInputV1.SchemaId,
            OcgForgeEncodedModelInputV1.SchemaId,
            OcgForgeCardVocabularyV1.SchemaId,
            OcgForgeModelInputIdentityV1.SchemaId,
            OcgForgeModelBatchLayoutV1.SchemaId
        }, static (output, value) => output.String(value));
        writer.String(cardVocabularyIdentity);
        writer.String(encoded.PublicObservationDigest);
        writer.Bool(encoded.PublicCandidateDomainDigest is not null);
        if (encoded.PublicCandidateDomainDigest is not null)
        {
            writer.String(encoded.PublicCandidateDomainDigest);
        }

        foreach (TableDescriptor table in TableDescriptors)
        {
            WriteTable(writer, table, encoded);
        }

        return writer.ToArray();
    }

    private static void WriteTable(
        OcgForgeI6ECanonicalV1.Writer writer,
        TableDescriptor table,
        OcgForgeEncodedModelInputV1 encoded)
    {
        int rows = TableRowCount(table.Name, encoded);
        writer.String(table.Name);
        writer.U64(checked((ulong)rows));
        if (table.Kind != "singleton")
        {
            WriteU64Vector(writer, new ulong[] { 0, checked((ulong)rows) });
        }

        if (table.ParentOffset is not null)
        {
            WriteU64Vector(writer, ChildOffsets(table.Name, encoded));
        }

        writer.Count(table.Columns.Count);
        foreach (ColumnDescriptor column in table.Columns)
        {
            writer.String(column.Name);
        }

        foreach (ColumnDescriptor column in table.Columns)
        {
            for (int row = 0; row < rows; row++)
            {
                WriteEncodedColumn(writer, table.Name, column.Name, encoded, row);
            }
        }

        WriteBoolVector(writer, rows, true);
    }

    private static void WriteU64Vector(
        OcgForgeI6ECanonicalV1.Writer writer,
        IReadOnlyList<ulong> values)
    {
        writer.Count(values.Count);
        for (int index = 0; index < values.Count; index++)
        {
            writer.U64(values[index]);
        }
    }

    private static void WriteBoolVector(
        OcgForgeI6ECanonicalV1.Writer writer,
        int count,
        bool value)
    {
        writer.Count(count);
        for (int index = 0; index < count; index++)
        {
            writer.Bool(value);
        }
    }

    private static int TableRowCount(
        string tableName,
        OcgForgeEncodedModelInputV1 encoded) =>
        tableName switch
        {
            "sample_header" or "globals" or "chain_state" or "match_context" => 1,
            "life_points" => encoded.Globals.LifePoints.Count,
            "decision_context_references" =>
                encoded.ObservationContextReferenceOrdinals.Count,
            "zones" => encoded.Zones.Count,
            "entities" => encoded.Entities.Count,
            "entity_properties" => checked(encoded.Entities.Count * 2),
            "property_link_markers" => PropertyRowCount(encoded, true),
            "property_counters" => PropertyRowCount(encoded, false),
            "relationships" => encoded.Relationships.Count,
            "chain_links" => encoded.Chain.Links.Count,
            "chain_targets" => encoded.Chain.Links.Sum(link => link.Targets.Count),
            "visible_events" => encoded.VisibleEvents.Count,
            "visible_event_targets" =>
                encoded.VisibleEvents.Sum(@event => @event.TargetPublicLocatorOrdinals.Count),
            "own_main_deck_ids" => encoded.MatchContext.OwnDeck.MainDeck.Count,
            "opponent_main_deck_ids" => encoded.MatchContext.OpponentDeck.MainDeck.Count,
            "own_extra_deck_ids" => encoded.MatchContext.OwnDeck.ExtraDeck.Count,
            "opponent_extra_deck_ids" => encoded.MatchContext.OpponentDeck.ExtraDeck.Count,
            "public_locator_control_sidecar" => encoded.PublicLocatorTable.Count,
            "candidates" => encoded.CandidateFeatures.Count,
            "routing_key_control_sidecar" => encoded.RoutingKeys.Count,
            _ => throw new ArgumentException("unknown Task7 table")
        };

    private static int PropertyRowCount(
        OcgForgeEncodedModelInputV1 encoded,
        bool linkMarkers)
    {
        int count = 0;
        foreach (OcgForgeEncodedEntityV1 entity in encoded.Entities)
        {
            count = checked(count + (linkMarkers
                ? entity.Printed?.LinkMarkerCodes.Count ?? 0
                : entity.Printed?.Counters.Count ?? 0));
            count = checked(count + (linkMarkers
                ? entity.Current?.LinkMarkerCodes.Count ?? 0
                : entity.Current?.Counters.Count ?? 0));
        }

        return count;
    }

    private static List<ulong> ChildOffsets(
        string tableName,
        OcgForgeEncodedModelInputV1 encoded) =>
        tableName switch
        {
            "entity_properties" => EntityPropertyOffsets(encoded),
            "property_link_markers" => PropertyOffsets(encoded, true),
            "property_counters" => PropertyOffsets(encoded, false),
            "chain_targets" => ChainTargetOffsets(encoded),
            "visible_event_targets" => VisibleEventTargetOffsets(encoded),
            _ => throw new ArgumentException("unknown Task7 child table")
        };

    private static List<ulong> EntityPropertyOffsets(
        OcgForgeEncodedModelInputV1 encoded)
    {
        List<ulong> offsets = new(encoded.Entities.Count + 1) { 0 };
        for (int index = 0; index < encoded.Entities.Count; index++)
        {
            offsets.Add(checked(offsets[^1] + 2UL));
        }

        return offsets;
    }

    private static List<ulong> PropertyOffsets(
        OcgForgeEncodedModelInputV1 encoded,
        bool linkMarkers)
    {
        List<ulong> offsets = new(checked(encoded.Entities.Count * 2 + 1)) { 0 };
        foreach (OcgForgeEncodedEntityV1 entity in encoded.Entities)
        {
            offsets.Add(checked(offsets[^1] + (ulong)(linkMarkers
                ? entity.Printed?.LinkMarkerCodes.Count ?? 0
                : entity.Printed?.Counters.Count ?? 0)));
            offsets.Add(checked(offsets[^1] + (ulong)(linkMarkers
                ? entity.Current?.LinkMarkerCodes.Count ?? 0
                : entity.Current?.Counters.Count ?? 0)));
        }

        return offsets;
    }

    private static List<ulong> ChainTargetOffsets(
        OcgForgeEncodedModelInputV1 encoded)
    {
        List<ulong> offsets = new(encoded.Chain.Links.Count + 1) { 0 };
        foreach (OcgForgeEncodedChainLinkV1 link in encoded.Chain.Links)
        {
            offsets.Add(checked(offsets[^1] + (ulong)link.Targets.Count));
        }

        return offsets;
    }

    private static List<ulong> VisibleEventTargetOffsets(
        OcgForgeEncodedModelInputV1 encoded)
    {
        List<ulong> offsets = new(encoded.VisibleEvents.Count + 1) { 0 };
        foreach (OcgForgeEncodedVisibleEventV1 @event in encoded.VisibleEvents)
        {
            offsets.Add(checked(offsets[^1] +
                (ulong)@event.TargetPublicLocatorOrdinals.Count));
        }

        return offsets;
    }

    private static (OcgForgeEncodedCardPropertiesV1? Property, byte Role)
        PropertyAt(OcgForgeEncodedModelInputV1 encoded, int row)
    {
        if (row < 0 || row >= checked(encoded.Entities.Count * 2))
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        OcgForgeEncodedEntityV1 entity = encoded.Entities[row / 2];
        return row % 2 == 0
            ? (entity.Printed, (byte)1)
            : (entity.Current, (byte)2);
    }

    private static byte PropertyLinkMarkerAt(
        OcgForgeEncodedModelInputV1 encoded,
        int row)
    {
        foreach (OcgForgeEncodedEntityV1 entity in encoded.Entities)
        {
            foreach (OcgForgeEncodedCardPropertiesV1? property in
                     new[] { entity.Printed, entity.Current })
            {
                if (property is null)
                {
                    continue;
                }

                if (row < property.LinkMarkerCodes.Count)
                {
                    return property.LinkMarkerCodes[row];
                }

                row -= property.LinkMarkerCodes.Count;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(row));
    }

    private static OcgForgeEncodedCounterV1 CounterAt(
        OcgForgeEncodedModelInputV1 encoded,
        int row)
    {
        foreach (OcgForgeEncodedEntityV1 entity in encoded.Entities)
        {
            foreach (OcgForgeEncodedCardPropertiesV1? property in
                     new[] { entity.Printed, entity.Current })
            {
                if (property is null)
                {
                    continue;
                }

                if (row < property.Counters.Count)
                {
                    return property.Counters[row];
                }

                row -= property.Counters.Count;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(row));
    }

    private static OcgForgeEncodedCurrentReferenceV1 ChainTargetAt(
        OcgForgeEncodedModelInputV1 encoded,
        int row)
    {
        foreach (OcgForgeEncodedChainLinkV1 link in encoded.Chain.Links)
        {
            if (row < link.Targets.Count)
            {
                return link.Targets[row];
            }

            row -= link.Targets.Count;
        }

        throw new ArgumentOutOfRangeException(nameof(row));
    }

    private static uint VisibleEventTargetAt(
        OcgForgeEncodedModelInputV1 encoded,
        int row)
    {
        foreach (OcgForgeEncodedVisibleEventV1 @event in encoded.VisibleEvents)
        {
            if (row < @event.TargetPublicLocatorOrdinals.Count)
            {
                return @event.TargetPublicLocatorOrdinals[row];
            }

            row -= @event.TargetPublicLocatorOrdinals.Count;
        }

        throw new ArgumentOutOfRangeException(nameof(row));
    }

    private static void WritePropertyColumn(
        OcgForgeI6ECanonicalV1.Writer writer,
        OcgForgeEncodedCardPropertiesV1? property,
        string columnName)
    {
        if (columnName == "property_present")
        {
            writer.Bool(property is not null);
            return;
        }

        if (property is null)
        {
            switch (columnName)
            {
                case "type":
                case "attribute":
                case "race":
                case "attack":
                case "defense":
                case "base_attack":
                case "base_defense":
                case "level":
                case "rank":
                case "link_rating":
                case "left_scale":
                case "right_scale":
                case "status_flags":
                    writer.Bool(false);
                    return;
                default:
                    throw new ArgumentException("unknown Task7 property column");
            }
        }

        switch (columnName)
        {
            case "type":
                writer.OptionalU32(property!.Type);
                break;
            case "attribute":
                writer.OptionalU32(property!.Attribute);
                break;
            case "race":
                writer.OptionalU64(property!.Race);
                break;
            case "attack":
                writer.OptionalI32(property!.Attack);
                break;
            case "defense":
                writer.OptionalI32(property!.Defense);
                break;
            case "base_attack":
                writer.OptionalI32(property!.BaseAttack);
                break;
            case "base_defense":
                writer.OptionalI32(property!.BaseDefense);
                break;
            case "level":
                writer.OptionalU32(property!.Level);
                break;
            case "rank":
                writer.OptionalU32(property!.Rank);
                break;
            case "link_rating":
                writer.OptionalU32(property!.LinkRating);
                break;
            case "left_scale":
                writer.OptionalU32(property!.LeftScale);
                break;
            case "right_scale":
                writer.OptionalU32(property!.RightScale);
                break;
            case "status_flags":
                writer.OptionalU32(property!.StatusFlags);
                break;
            default:
                throw new ArgumentException("unknown Task7 property column");
        }
    }

    private static void WriteEncodedColumn(
        OcgForgeI6ECanonicalV1.Writer writer,
        string tableName,
        string columnName,
        OcgForgeEncodedModelInputV1 encoded,
        int row)
    {
        switch (tableName)
        {
            case "sample_header":
                switch (columnName)
                {
                    case "perspective_player":
                        WriteU8Limb(writer, encoded.PerspectivePlayer);
                        return;
                    case "decision_index":
                        writer.U64(encoded.DecisionIndex);
                        return;
                    case "public_observation_context_kind_code":
                        writer.OptionalU16(encoded.PublicObservationContextKindCode);
                        return;
                    case "public_observation_context_player":
                        WriteOptionalU8(writer, encoded.PublicObservationContextPlayer);
                        return;
                    case "public_locator_count":
                        writer.U32(checked((uint)encoded.PublicLocatorTable.Count));
                        return;
                    case "candidate_count":
                        writer.U32(checked((uint)encoded.CandidateFeatures.Count));
                        return;
                    default:
                        throw new ArgumentException("unknown Task7 sample-header column");
                }

            case "globals":
                switch (columnName)
                {
                    case "duel_flags":
                        writer.U64(encoded.Globals.DuelFlags);
                        return;
                    case "player_to_act":
                        WriteOptionalU8(writer, encoded.Globals.PlayerToAct);
                        return;
                    case "turn_player":
                        WriteOptionalU8(writer, encoded.Globals.TurnPlayer);
                        return;
                    case "turn_count":
                        writer.OptionalU32(encoded.Globals.TurnCount);
                        return;
                    case "phase":
                        writer.OptionalU32(encoded.Globals.Phase);
                        return;
                    case "chain_length":
                        writer.U32(encoded.Globals.ChainLength);
                        return;
                    case "winner":
                        WriteOptionalU8(writer, encoded.Globals.Winner);
                        return;
                    case "win_reason":
                        WriteOptionalU8(writer, encoded.Globals.WinReason);
                        return;
                    case "terminal":
                        writer.Bool(encoded.Globals.Terminal);
                        return;
                    default:
                        throw new ArgumentException("unknown Task7 globals column");
                }

            case "chain_state":
                if (columnName != "length")
                {
                    throw new ArgumentException("unknown Task7 chain-state column");
                }

                writer.U32(encoded.Chain.Length);
                return;

            case "match_context":
                switch (columnName)
                {
                    case "perspective_player":
                        WriteU8Limb(writer, encoded.MatchContext.PerspectivePlayer);
                        return;
                    case "duel_flags":
                        writer.U64(encoded.MatchContext.DuelFlags);
                        return;
                    case "own_decklist_known":
                        writer.Bool(encoded.MatchContext.OwnDecklistKnown);
                        return;
                    case "opponent_decklist_known":
                        writer.Bool(encoded.MatchContext.OpponentDecklistKnown);
                        return;
                    case "own_deck_known":
                        writer.Bool(encoded.MatchContext.OwnDeck.Known);
                        return;
                    case "opponent_deck_known":
                        writer.Bool(encoded.MatchContext.OpponentDeck.Known);
                        return;
                    default:
                        throw new ArgumentException("unknown Task7 match-context column");
                }

            case "life_points":
                if (columnName != "value")
                {
                    throw new ArgumentException("unknown Task7 life-point column");
                }

                writer.U32(encoded.Globals.LifePoints[row]);
                return;

            case "decision_context_references":
                if (columnName != "public_locator_ordinal")
                {
                    throw new ArgumentException("unknown Task7 context-reference column");
                }

                writer.U32(encoded.ObservationContextReferenceOrdinals[row]);
                return;

            case "zones":
                WriteZoneColumn(writer, columnName, encoded.Zones[row]);
                return;

            case "entities":
                WriteEntityColumn(writer, columnName, encoded.Entities[row]);
                return;

            case "entity_properties":
                (OcgForgeEncodedCardPropertiesV1? property, byte role) =
                    PropertyAt(encoded, row);
                if (columnName == "property_role")
                {
                    WriteU8Limb(writer, role);
                }
                else
                {
                    WritePropertyColumn(writer, property, columnName);
                }

                return;

            case "property_link_markers":
                if (columnName != "link_marker_code")
                {
                    throw new ArgumentException("unknown Task7 link-marker column");
                }

                WriteU8Limb(writer, PropertyLinkMarkerAt(encoded, row));
                return;

            case "property_counters":
                OcgForgeEncodedCounterV1 counter = CounterAt(encoded, row);
                switch (columnName)
                {
                    case "type":
                        writer.U32(counter.Type);
                        return;
                    case "count":
                        writer.U32(counter.Count);
                        return;
                    default:
                        throw new ArgumentException("unknown Task7 counter column");
                }

            case "relationships":
                OcgForgeEncodedRelationshipV1 relationship = encoded.Relationships[row];
                switch (columnName)
                {
                    case "kind_code":
                        WriteU8Limb(writer, relationship.KindCode);
                        return;
                    case "source":
                        WriteCurrentReference(writer, relationship.Source);
                        return;
                    case "target":
                        WriteCurrentReference(writer, relationship.Target);
                        return;
                    default:
                        throw new ArgumentException("unknown Task7 relationship column");
                }

            case "chain_links":
                WriteChainLinkColumn(writer, columnName, encoded.Chain.Links[row]);
                return;

            case "chain_targets":
                if (columnName != "target")
                {
                    throw new ArgumentException("unknown Task7 chain-target column");
                }

                WriteCurrentReference(writer, ChainTargetAt(encoded, row));
                return;

            case "visible_events":
                WriteVisibleEventColumn(writer, columnName, encoded.VisibleEvents[row]);
                return;

            case "visible_event_targets":
                if (columnName != "public_locator_ordinal")
                {
                    throw new ArgumentException("unknown Task7 visible-event-target column");
                }

                writer.U32(VisibleEventTargetAt(encoded, row));
                return;

            case "own_main_deck_ids":
                if (columnName != "card_vocabulary_id")
                {
                    throw new ArgumentException("unknown Task7 own-main-deck column");
                }

                writer.U32(encoded.MatchContext.OwnDeck.MainDeck[row]);
                return;

            case "opponent_main_deck_ids":
                if (columnName != "card_vocabulary_id")
                {
                    throw new ArgumentException("unknown Task7 opponent-main-deck column");
                }

                writer.U32(encoded.MatchContext.OpponentDeck.MainDeck[row]);
                return;

            case "own_extra_deck_ids":
                if (columnName != "card_vocabulary_id")
                {
                    throw new ArgumentException("unknown Task7 own-extra-deck column");
                }

                writer.U32(encoded.MatchContext.OwnDeck.ExtraDeck[row]);
                return;

            case "opponent_extra_deck_ids":
                if (columnName != "card_vocabulary_id")
                {
                    throw new ArgumentException("unknown Task7 opponent-extra-deck column");
                }

                writer.U32(encoded.MatchContext.OpponentDeck.ExtraDeck[row]);
                return;

            case "public_locator_control_sidecar":
                if (columnName != "public_locator_token")
                {
                    throw new ArgumentException("unknown Task7 locator-sidecar column");
                }

                writer.String(encoded.PublicLocatorTable[row]);
                return;

            case "candidates":
                WriteCandidateColumn(writer, columnName, encoded.CandidateFeatures[row]);
                return;

            case "routing_key_control_sidecar":
                if (columnName != "public_action_key")
                {
                    throw new ArgumentException("unknown Task7 routing-sidecar column");
                }

                writer.String(encoded.RoutingKeys[row]);
                return;

            default:
                throw new ArgumentException("unknown Task7 table/column pair");
        }
    }

    private static void WriteZoneColumn(
        OcgForgeI6ECanonicalV1.Writer writer,
        string columnName,
        OcgForgeEncodedZoneV1 zone)
    {
        switch (columnName)
        {
            case "player":
                WriteU8Limb(writer, zone.Player);
                break;
            case "kind_code":
                WriteU8Limb(writer, zone.KindCode);
                break;
            case "total_count":
                writer.U32(zone.TotalCount);
                break;
            case "public_identity_count":
                writer.U32(zone.PublicIdentityCount);
                break;
            case "hidden_count":
                writer.U32(zone.HiddenCount);
                break;
            case "player_observable_order":
                writer.Bool(zone.PlayerObservableOrder);
                break;
            default:
                throw new ArgumentException("unknown Task7 zone column");
        }
    }

    private static void WriteEntityColumn(
        OcgForgeI6ECanonicalV1.Writer writer,
        string columnName,
        OcgForgeEncodedEntityV1 entity)
    {
        switch (columnName)
        {
            case "public_locator_ordinal":
                writer.U32(entity.PublicLocatorOrdinal);
                break;
            case "identity_known":
                writer.Bool(entity.IdentityKnown);
                break;
            case "card_vocabulary_id":
                writer.U32(entity.CardVocabularyId);
                break;
            case "owner":
                WriteOptionalU8(writer, entity.Owner);
                break;
            case "controller":
                WriteOptionalU8(writer, entity.Controller);
                break;
            case "zone_code":
                WriteU8Limb(writer, entity.ZoneCode);
                break;
            case "sequence":
                writer.OptionalU32(entity.Sequence);
                break;
            case "overlay_sequence":
                writer.OptionalU32(entity.OverlaySequence);
                break;
            case "position_code":
                WriteU8Limb(writer, entity.PositionCode);
                break;
            case "face_up":
                writer.Bool(entity.FaceUp);
                break;
            case "face_down":
                writer.Bool(entity.FaceDown);
                break;
            default:
                throw new ArgumentException("unknown Task7 entity column");
        }
    }

    private static void WriteChainLinkColumn(
        OcgForgeI6ECanonicalV1.Writer writer,
        string columnName,
        OcgForgeEncodedChainLinkV1 link)
    {
        switch (columnName)
        {
            case "index":
                writer.U32(link.Index);
                break;
            case "activating_player":
                WriteOptionalU8(writer, link.ActivatingPlayer);
                break;
            case "source":
                WriteOptionalCurrentReference(writer, link.Source);
                break;
            case "activation_zone_code":
                WriteOptionalU8(writer, link.ActivationZoneCode);
                break;
            case "effect_description":
                writer.OptionalU64(link.EffectDescription);
                break;
            default:
                throw new ArgumentException("unknown Task7 chain-link column");
        }
    }

    private static void WriteVisibleEventColumn(
        OcgForgeI6ECanonicalV1.Writer writer,
        string columnName,
        OcgForgeEncodedVisibleEventV1 @event)
    {
        switch (columnName)
        {
            case "event_index":
                writer.U64(@event.EventIndex);
                break;
            case "kind_code":
                WriteU8Limb(writer, @event.KindCode);
                break;
            case "player":
                WriteOptionalU8(writer, @event.Player);
                break;
            case "entity":
                WriteHistoricalReference(writer, @event.PublicLocatorOrdinal);
                break;
            case "public_card_vocabulary_id":
                writer.OptionalU32(@event.PublicCardVocabularyId);
                break;
            case "from_zone_code":
                WriteOptionalU8(writer, @event.FromZoneCode);
                break;
            case "to_zone_code":
                WriteOptionalU8(writer, @event.ToZoneCode);
                break;
            case "count":
                writer.OptionalU32(@event.Count);
                break;
            case "amount":
                writer.OptionalI32(@event.Amount);
                break;
            case "counter_type":
                writer.OptionalU32(@event.CounterType);
                break;
            case "phase":
                writer.OptionalU32(@event.Phase);
                break;
            case "winner":
                WriteOptionalU8(writer, @event.Winner);
                break;
            case "win_reason":
                WriteOptionalU8(writer, @event.WinReason);
                break;
            case "effect_description":
                writer.OptionalU64(@event.EffectDescription);
                break;
            default:
                throw new ArgumentException("unknown Task7 visible-event column");
        }
    }

    private static void WriteCandidateColumn(
        OcgForgeI6ECanonicalV1.Writer writer,
        string columnName,
        OcgForgeEncodedCandidateV1 candidate)
    {
        switch (columnName)
        {
            case "action_kind_code":
                writer.U16(candidate.ActionKindCode);
                break;
            case "choice_present":
                writer.Bool(candidate.Choice.HasValue);
                break;
            case "choice_kind_code":
                WriteU8Limb(writer, candidate.Choice?.KindCode ?? 0);
                break;
            case "choice_value":
                writer.U64(candidate.Choice?.Value ?? 0);
                break;
            case "choice_response_index":
                writer.OptionalU32(candidate.Choice?.ResponseIndex);
                break;
            case "source_reference":
                WriteOptionalCardReference(writer, candidate.SourceReference);
                break;
            case "target_reference":
                WriteOptionalCardReference(writer, candidate.TargetReference);
                break;
            case "phase":
                writer.OptionalU32(candidate.Phase);
                break;
            case "position":
                WriteOptionalU8(writer, candidate.Position);
                break;
            case "source_index":
                writer.OptionalU32(candidate.SourceIndex);
                break;
            case "amount":
                writer.OptionalI32(candidate.Amount);
                break;
            case "continuation_operation_code":
                WriteU8Limb(writer, candidate.ContinuationOperationCode);
                break;
            case "submits_engine_response":
                writer.Bool(candidate.SubmitsEngineResponse);
                break;
            default:
                throw new ArgumentException("unknown Task7 candidate column");
        }
    }

    private static void WriteCurrentReference(
        OcgForgeI6ECanonicalV1.Writer writer,
        OcgForgeEncodedCurrentReferenceV1 reference)
    {
        writer.U32(reference.PublicLocatorOrdinal);
        writer.OptionalU32(reference.CurrentEntityOrdinal);
    }

    private static void WriteOptionalCurrentReference(
        OcgForgeI6ECanonicalV1.Writer writer,
        OcgForgeEncodedCurrentReferenceV1? reference)
    {
        writer.Bool(reference.HasValue);
        if (reference.HasValue)
        {
            WriteCurrentReference(writer, reference.Value);
        }
    }

    private static void WriteOptionalCardReference(
        OcgForgeI6ECanonicalV1.Writer writer,
        OcgForgeEncodedCardReferenceV1? reference)
    {
        writer.Bool(reference.HasValue);
        if (reference.HasValue)
        {
            WriteU8Limb(writer, reference.Value.KindCode);
            WriteCurrentReference(writer, reference.Value.Reference);
        }
    }

    private static void WriteHistoricalReference(
        OcgForgeI6ECanonicalV1.Writer writer,
        uint? publicLocatorOrdinal)
    {
        writer.Bool(publicLocatorOrdinal.HasValue);
        if (publicLocatorOrdinal.HasValue)
        {
            writer.U32(publicLocatorOrdinal.Value);
        }
    }

    private static void WriteU8Limb(
        OcgForgeI6ECanonicalV1.Writer writer,
        byte value) =>
        writer.U16(value);

    private static void WriteOptionalU8(
        OcgForgeI6ECanonicalV1.Writer writer,
        byte? value)
    {
        writer.Bool(value.HasValue);
        if (value.HasValue)
        {
            WriteU8Limb(writer, value.Value);
        }
    }

    private sealed class OcgForgeTask7Failure : Exception
    {
        internal OcgForgeTask7Failure(
            OcgForgeTask7MaterializationErrorCodeV1 code,
            string fieldPath)
        {
            Code = code;
            FieldPath = fieldPath;
        }

        internal OcgForgeTask7MaterializationErrorCodeV1 Code { get; }

        internal string FieldPath { get; }
    }
}
