namespace OCGForge.Ignis.Gameplay.Tests.Fixtures;

internal enum I6DCrossLocatorMappingProofKindV1 : byte
{
    None = 0,
    ExactPublicToken = 1,
    UniqueSafeAttributeMatch = 2,
    AmbiguousSafeCurrentEntities = 3,
    MissingSafeCurrentEntity = 4,
    StaleFrame = 5,
    NoSafePublicIdentity = 6
}

internal enum I6DCrossLocatorFormV1 : byte
{
    None = 0,
    Indexed = 1,
    PublicOrdinal = 2,
    Overlay = 3,
    Invalid = 4
}

internal readonly record struct I6DCrossLocatorMappingObservationV1(
    FlatPromptChoiceKindV1 ChoiceKind,
    byte AbsolutePlayer,
    PublicSemanticZoneV1 PublicZone,
    I6DCrossLocatorFormV1 I4LocatorForm,
    I6DCrossLocatorFormV1 I6C5LocatorForm,
    bool I4PublicLocatorPresent,
    int I4PublicLocatorMatchCount,
    int I6C5ExactLocatorMatchCount,
    int I6C5SafeCurrentMatchCount,
    bool SafePublicIdentityAvailable,
    bool UniqueMatchIsMappingAuthority,
    bool UsesHiddenIdentity,
    bool UsesMirrorEntityIdentity,
    bool UsesRawProtocolAddress,
    I6DCrossLocatorMappingProofKindV1 ProofKind);

internal readonly record struct I6DCrossLocatorMappingResultV1(
    bool IsSuccess,
    I6DCrossLocatorMappingObservationV1 Observation);

internal enum I6DPrivateBindingFieldRoleV1 : byte
{
    Lifecycle = 0,
    CandidateLookup = 1,
    PrivateSourceOccurrence = 2,
    SafeTarget = 3
}

internal readonly record struct I6DPrivateBindingFieldSpecV1(
    string Name,
    string TypeName,
    I6DPrivateBindingFieldRoleV1 Role,
    bool EntersPublicIdentity,
    bool MayFeedPublicDescriptor);

internal readonly record struct I6DPrivateBindingCaseV1(
    string Name,
    bool RequiresPrivateBinding,
    int SourceOccurrenceCount,
    bool SourceOccurrencesAreDistinct,
    bool TargetLocatorsAreDistinct,
    bool ExactTokenPathUnchanged,
    bool PublicIdentityIndependentOfPrivateData);

internal readonly record struct I6DPrivateBindingPrivacyProjectionV1(
    uint PrivateSourceSequence,
    string AcceptedTargetLocator,
    string PublicDescriptor);

internal static class I6DPrivateSourceOccurrenceBindingDesignV1
{
    private static readonly IReadOnlyList<I6DPrivateBindingFieldSpecV1>
        fieldSpecs = Array.AsReadOnly(
            new[]
            {
                new I6DPrivateBindingFieldSpecV1(
                    "PromptInstanceOrdinal",
                    "ulong",
                    I6DPrivateBindingFieldRoleV1.Lifecycle,
                    false,
                    false),
                new I6DPrivateBindingFieldSpecV1(
                    "ContinuationStep",
                    "int",
                    I6DPrivateBindingFieldRoleV1.Lifecycle,
                    false,
                    false),
                new I6DPrivateBindingFieldSpecV1(
                    "FrameInstanceOrdinal",
                    "ulong",
                    I6DPrivateBindingFieldRoleV1.Lifecycle,
                    false,
                    false),
                new I6DPrivateBindingFieldSpecV1(
                    "AcceptedPublicProjectionId",
                    "string",
                    I6DPrivateBindingFieldRoleV1.Lifecycle,
                    false,
                    false),
                new I6DPrivateBindingFieldSpecV1(
                    "I4LocalCandidateKey",
                    "string",
                    I6DPrivateBindingFieldRoleV1.CandidateLookup,
                    false,
                    false),
                new I6DPrivateBindingFieldSpecV1(
                    "SourceSection",
                    "FlatPromptSourceSectionV1",
                    I6DPrivateBindingFieldRoleV1.CandidateLookup,
                    false,
                    false),
                new I6DPrivateBindingFieldSpecV1(
                    "SourceOrdinal",
                    "int",
                    I6DPrivateBindingFieldRoleV1.CandidateLookup,
                    false,
                    false),
                new I6DPrivateBindingFieldSpecV1(
                    "AbsoluteController",
                    "byte",
                    I6DPrivateBindingFieldRoleV1.PrivateSourceOccurrence,
                    false,
                    false),
                new I6DPrivateBindingFieldSpecV1(
                    "NormalizedZone",
                    "MirrorZoneV1",
                    I6DPrivateBindingFieldRoleV1.PrivateSourceOccurrence,
                    false,
                    false),
                new I6DPrivateBindingFieldSpecV1(
                    "SourceSequence",
                    "uint",
                    I6DPrivateBindingFieldRoleV1.PrivateSourceOccurrence,
                    false,
                    false),
                new I6DPrivateBindingFieldSpecV1(
                    "IsOverlay",
                    "bool",
                    I6DPrivateBindingFieldRoleV1.PrivateSourceOccurrence,
                    false,
                    false),
                new I6DPrivateBindingFieldSpecV1(
                    "OverlayIndex",
                    "uint?",
                    I6DPrivateBindingFieldRoleV1.PrivateSourceOccurrence,
                    false,
                    false),
                new I6DPrivateBindingFieldSpecV1(
                    "AcceptedI6C5TargetLocator",
                    "PublicSemanticLocatorV1",
                    I6DPrivateBindingFieldRoleV1.SafeTarget,
                    false,
                    true)
            });

    private static readonly IReadOnlyList<string> primaryLookupKey =
        Array.AsReadOnly(
            new[]
            {
                "PromptInstanceOrdinal",
                "ContinuationStep",
                "I4LocalCandidateKey"
            });

    private static readonly IReadOnlyList<string> candidateCrossChecks =
        Array.AsReadOnly(
            new[]
            {
                "SourceSection",
                "SourceOrdinal"
            });

    private static readonly IReadOnlyList<string> invalidationRules =
        Array.AsReadOnly(
            new[]
            {
                "PromptInstanceOrdinalMismatch",
                "ContinuationStepMismatch",
                "FrameInstanceOrdinalMismatch",
                "AcceptedPublicProjectionIdMismatch",
                "CandidateKeyMismatch",
                "SourceSectionOrOrdinalMismatch",
                "MissingBinding",
                "AmbiguousBinding",
                "BindingKeyCollision",
                "SourceOccurrenceCollision",
                "TargetLocatorMissingOrNonUnique"
            });

    private static readonly IReadOnlyList<string> forbiddenCarrierData =
        Array.AsReadOnly(
            new[]
            {
                "PromptLocalCardCode",
                "CardCode",
                "MirrorEntityIdV1",
                "ModernLocInfoV1",
                "RawLocInfo",
                "Pointer",
                "ObjectHash",
                "CollectionOrder"
            });

    private static readonly IReadOnlyList<I6DPrivateBindingCaseV1> cases =
        Array.AsReadOnly(
            new[]
            {
                new I6DPrivateBindingCaseV1(
                    "OwnHandUnique",
                    true,
                    1,
                    true,
                    true,
                    false,
                    true),
                new I6DPrivateBindingCaseV1(
                    "OwnHandDuplicate",
                    true,
                    2,
                    true,
                    true,
                    false,
                    true),
                new I6DPrivateBindingCaseV1(
                    "OpponentPublicHand",
                    false,
                    1,
                    true,
                    true,
                    true,
                    true),
                new I6DPrivateBindingCaseV1(
                    "CrossPileSameCode",
                    true,
                    1,
                    true,
                    true,
                    false,
                    true)
            });

    internal const string CarrierName = "PrivateCrossLocatorBindingV1";

    internal const string SemanticOwner =
        "I6D OcgForgePublicCandidateBridgeV1";

    internal const string AcquisitionSeam =
        "Gameplay/I4 correlation before CompleteCorrelation";

    internal const string ConsumptionSeam =
        "accepted decision boundary -> OcgForgePublicCandidateBridgeV1.TryCreate";

    internal const string BoundaryConstructionSeam =
        "internal TryAccept(frame, completeProjection, completeBindings, out boundary, out error)";

    internal const string BindingSetName =
        "PrivateCrossLocatorBindingSetV1";

    internal const string CarrierVisibility =
        "internal immutable Gameplay-to-Model handoff";

    internal const bool FrameInstanceOrdinalIsSessionOwned = true;

    internal const bool FrameInstanceOrdinalIsCallerSupplied = false;

    internal const bool PrivateBindingIsPublicType = false;

    internal const bool PrivateBindingInPublicDescriptor = false;

    internal const bool PrivateBindingInPublicActionKey = false;

    internal const bool PrivateBindingInDomainDigest = false;

    internal const bool PrivateBindingInModelInput = false;

    internal const bool PrivateBindingInObservation = false;

    internal const bool AcceptedTargetMayFeedPublicDescriptor = true;

    internal const bool ExactTokenPathMayOmitPrivateBinding = true;

    internal const bool NonEqualLocatorRequiresPrivateBinding = true;

    internal const bool CompleteBindingSetIsAcceptedAtomically = true;

    internal const bool BindingSetCompleteForRequiredCandidates = true;

    internal const bool BindingSetHasNoExtraEntries = true;

    internal const bool BindingSetIsDetachedCallerInput = false;

    internal const bool RejectionRulesAreRequirementsOnly = true;

    internal const bool MirrorEntityIdIsStored = false;

    internal const bool RawProtocolAddressIsStored = false;

    internal const bool HandSequenceIsPublicSubstitute = false;

    internal const bool PromptCardCodeIsPublicSubstitute = false;

    internal static IReadOnlyList<I6DPrivateBindingFieldSpecV1> Fields =>
        fieldSpecs;

    internal static IReadOnlyList<string> PrimaryLookupKey =>
        primaryLookupKey;

    internal static IReadOnlyList<string> CandidateCrossChecks =>
        candidateCrossChecks;

    internal static IReadOnlyList<string> InvalidationRules =>
        invalidationRules;

    internal static IReadOnlyList<string> ForbiddenCarrierData =>
        forbiddenCarrierData;

    internal static IReadOnlyList<I6DPrivateBindingCaseV1> Cases => cases;
}

internal static class I6DCrossLocatorMappingAuthorityCharacterizationV1
{
    internal static I6DCrossLocatorMappingResultV1 Characterize(
        FlatPublicCandidateDescriptorV1? candidate,
        PublicStateSnapshotV1? i4PublicState,
        IReadOnlyList<PerspectiveSafeEntityV1>? i6C5Entities,
        bool frameIsCurrent = true)
    {
        if (candidate is null ||
            i4PublicState is null ||
            i6C5Entities is null)
        {
            return Failure();
        }

        if (!TryGetCandidateLocator(
                candidate,
                out PublicSemanticLocatorV1? locator) ||
            locator is null ||
            !TryGetLocatorParts(
                locator,
                out byte absolutePlayer,
                out PublicSemanticZoneV1 publicZone,
                out I6DCrossLocatorFormV1 i4LocatorForm))
        {
            return Failure();
        }

        PublicCardStateV1[] i4Matches = i4PublicState.Cards
            .Where(card => card.Locator == locator)
            .ToArray();
        if (i4Matches.Length != 1)
        {
            return Failure();
        }

        PublicCardStateV1 i4Card = i4Matches[0];
        int exactLocatorMatchCount = i6C5Entities.Count(entity =>
            string.Equals(
                entity.Locator,
                locator.Value,
                StringComparison.Ordinal));
        PerspectiveSafeEntityV1[] safeMatches = i6C5Entities
            .Where(entity =>
                entity.IdentityKnown &&
                entity.Passcode.HasValue &&
                i4Card.CardCode.HasValue &&
                entity.Passcode.Value == i4Card.CardCode.Value &&
                entity.Controller == i4Card.AbsolutePlayer &&
                TryMapZone(entity.Zone, out PublicSemanticZoneV1 mappedZone) &&
                mappedZone == i4Card.Zone)
            .ToArray();

        I6DCrossLocatorFormV1 i6C5LocatorForm =
            safeMatches.Length == 0
                ? I6DCrossLocatorFormV1.None
                : GetLocatorForm(safeMatches[0].Locator);
        I6DCrossLocatorMappingProofKindV1 proofKind =
            !frameIsCurrent
                ? I6DCrossLocatorMappingProofKindV1.StaleFrame
                : exactLocatorMatchCount == 1
                ? I6DCrossLocatorMappingProofKindV1.ExactPublicToken
                : !i4Card.CardCode.HasValue
                ? I6DCrossLocatorMappingProofKindV1.NoSafePublicIdentity
                : safeMatches.Length == 1
                ? I6DCrossLocatorMappingProofKindV1.UniqueSafeAttributeMatch
                : safeMatches.Length > 1
                ? I6DCrossLocatorMappingProofKindV1.AmbiguousSafeCurrentEntities
                : I6DCrossLocatorMappingProofKindV1.MissingSafeCurrentEntity;

        return new(
            true,
            new(
                candidate.ChoiceKind,
                absolutePlayer,
                publicZone,
                i4LocatorForm,
                i6C5LocatorForm,
                true,
                i4Matches.Length,
                exactLocatorMatchCount,
                safeMatches.Length,
                i4Card.CardCode.HasValue,
                false,
                false,
                false,
                false,
                proofKind));
    }

    private static bool TryGetCandidateLocator(
        FlatPublicCandidateDescriptorV1 candidate,
        out PublicSemanticLocatorV1? locator)
    {
        locator = candidate switch
        {
            FlatIdleCardActionPublicCandidateBaseV1 card =>
                card.PublicSemanticCardLocator,
            FlatIdleActivatablePublicCandidateBaseV1 activatable =>
                activatable.PublicSemanticCardLocator,
            _ => null
        };
        return locator is not null;
    }

    private static bool TryGetLocatorParts(
        PublicSemanticLocatorV1 locator,
        out byte absolutePlayer,
        out PublicSemanticZoneV1 publicZone,
        out I6DCrossLocatorFormV1 form)
    {
        absolutePlayer = 0;
        publicZone = default;
        form = I6DCrossLocatorFormV1.Invalid;
        string[] parts = locator.Value.Split(':');
        if (parts.Length < 2 || parts[0] is not ("p0" or "p1"))
        {
            return false;
        }

        absolutePlayer = parts[0] == "p0" ? (byte)0 : (byte)1;
        if (parts.Length == 3)
        {
            form = I6DCrossLocatorFormV1.Indexed;
        }
        else if (parts.Length == 5 &&
                 string.Equals(parts[2], "public", StringComparison.Ordinal))
        {
            form = I6DCrossLocatorFormV1.PublicOrdinal;
        }
        else if (parts[1] == "OVERLAY" && parts.Length == 4)
        {
            form = I6DCrossLocatorFormV1.Overlay;
        }
        else
        {
            return false;
        }

        return TryMapPublicZone(parts[1], out publicZone);
    }

    private static I6DCrossLocatorFormV1 GetLocatorForm(string locator)
    {
        if (!PublicSemanticLocatorV1.TryParse(locator, out PublicSemanticLocatorV1? parsed) ||
            parsed is null ||
            !TryGetLocatorParts(
                parsed,
                out _,
                out _,
                out I6DCrossLocatorFormV1 form))
        {
            return I6DCrossLocatorFormV1.Invalid;
        }

        return form;
    }

    private static bool TryMapPublicZone(
        string token,
        out PublicSemanticZoneV1 zone)
    {
        zone = token switch
        {
            "HAND" => PublicSemanticZoneV1.Hand,
            "MONSTER_ZONE" => PublicSemanticZoneV1.MonsterZone,
            "SPELL_TRAP_ZONE" => PublicSemanticZoneV1.SpellTrapZone,
            "FIELD_ZONE" => PublicSemanticZoneV1.FieldZone,
            "PENDULUM_RELEVANT_STATE" => PublicSemanticZoneV1.PendulumRelevantState,
            "GRAVEYARD" => PublicSemanticZoneV1.Graveyard,
            "BANISHED" => PublicSemanticZoneV1.Banished,
            "EXTRA_DECK" => PublicSemanticZoneV1.ExtraDeck,
            "OVERLAY" => PublicSemanticZoneV1.Overlay,
            _ => default
        };
        return token is
            "HAND" or
            "MONSTER_ZONE" or
            "SPELL_TRAP_ZONE" or
            "FIELD_ZONE" or
            "PENDULUM_RELEVANT_STATE" or
            "GRAVEYARD" or
            "BANISHED" or
            "EXTRA_DECK" or
            "OVERLAY";
    }

    private static bool TryMapZone(
        PerspectiveSafeSemanticZoneV1 source,
        out PublicSemanticZoneV1 zone)
    {
        zone = source switch
        {
            PerspectiveSafeSemanticZoneV1.Hand => PublicSemanticZoneV1.Hand,
            PerspectiveSafeSemanticZoneV1.MonsterZone => PublicSemanticZoneV1.MonsterZone,
            PerspectiveSafeSemanticZoneV1.SpellTrapZone => PublicSemanticZoneV1.SpellTrapZone,
            PerspectiveSafeSemanticZoneV1.FieldZone => PublicSemanticZoneV1.FieldZone,
            PerspectiveSafeSemanticZoneV1.PendulumRelevant => PublicSemanticZoneV1.PendulumRelevantState,
            PerspectiveSafeSemanticZoneV1.Graveyard => PublicSemanticZoneV1.Graveyard,
            PerspectiveSafeSemanticZoneV1.Banished => PublicSemanticZoneV1.Banished,
            PerspectiveSafeSemanticZoneV1.ExtraDeck => PublicSemanticZoneV1.ExtraDeck,
            PerspectiveSafeSemanticZoneV1.Overlay => PublicSemanticZoneV1.Overlay,
            _ => default
        };
        return source != PerspectiveSafeSemanticZoneV1.Unknown;
    }

    private static I6DCrossLocatorMappingResultV1 Failure() =>
        new(false, default);
}
