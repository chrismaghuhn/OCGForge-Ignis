using System.Reflection;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6DCrossLocatorMappingAuthorityCharacterizationTests
{
    internal static void TestCrossLocatorAuthorityCases()
    {
        I6DCrossLocatorMappingObservationV1 ownUnique =
            Analyze(OwnHandUnique(), 0);
        Equal(
            I6DCrossLocatorMappingProofKindV1.UniqueSafeAttributeMatch,
            ownUnique.ProofKind);
        Equal(I6DCrossLocatorFormV1.PublicOrdinal, ownUnique.I4LocatorForm);
        Equal(I6DCrossLocatorFormV1.Indexed, ownUnique.I6C5LocatorForm);
        Equal(1, ownUnique.I6C5SafeCurrentMatchCount);
        False(ownUnique.UniqueMatchIsMappingAuthority);

        I6DCrossLocatorMappingObservationV1[] ownDuplicate =
            OwnHandDuplicate();
        True(ownDuplicate.All(value =>
            value.ProofKind ==
                I6DCrossLocatorMappingProofKindV1.AmbiguousSafeCurrentEntities));
        True(ownDuplicate.All(value =>
            value.I4LocatorForm == I6DCrossLocatorFormV1.PublicOrdinal &&
            value.I6C5LocatorForm == I6DCrossLocatorFormV1.Indexed &&
            value.I6C5SafeCurrentMatchCount == 2));

        I6DCrossLocatorMappingObservationV1 opponentHand =
            Analyze(OpponentPublicHand(), 0);
        Equal(
            I6DCrossLocatorMappingProofKindV1.ExactPublicToken,
            opponentHand.ProofKind);
        Equal(I6DCrossLocatorFormV1.PublicOrdinal, opponentHand.I4LocatorForm);
        Equal(I6DCrossLocatorFormV1.PublicOrdinal, opponentHand.I6C5LocatorForm);

        I6DCrossLocatorMappingObservationV1 extraDeck =
            Analyze(ExtraDeck(), 0);
        Equal(
            I6DCrossLocatorMappingProofKindV1.ExactPublicToken,
            extraDeck.ProofKind);
        Equal(I6DCrossLocatorFormV1.PublicOrdinal, extraDeck.I4LocatorForm);
        Equal(I6DCrossLocatorFormV1.PublicOrdinal, extraDeck.I6C5LocatorForm);

        I6DCrossLocatorMappingObservationV1 crossPile =
            Analyze(CrossPile(), 0);
        Equal(
            I6DCrossLocatorMappingProofKindV1.UniqueSafeAttributeMatch,
            crossPile.ProofKind);
        Equal(0, crossPile.I6C5ExactLocatorMatchCount);
        Equal(1, crossPile.I6C5SafeCurrentMatchCount);
        Equal(I6DCrossLocatorFormV1.PublicOrdinal, crossPile.I4LocatorForm);
        Equal(I6DCrossLocatorFormV1.PublicOrdinal, crossPile.I6C5LocatorForm);

        I6DCrossLocatorCaseV1 hiddenCase = HiddenOpponentHand();
        False(hiddenCase.PublicState.Cards.Any(card =>
            card.AbsolutePlayer == 1 &&
            card.Zone == PublicSemanticZoneV1.Hand));
        False(hiddenCase.I6C5Entities.Any(entity =>
            entity.Controller == 1 &&
            entity.Zone == PerspectiveSafeSemanticZoneV1.Hand &&
            entity.IdentityKnown &&
            entity.Passcode.HasValue));

        I6DCrossLocatorMappingObservationV1 missing =
            Analyze(OwnHandUnique(), 0, Array.Empty<PerspectiveSafeEntityV1>());
        Equal(
            I6DCrossLocatorMappingProofKindV1.MissingSafeCurrentEntity,
            missing.ProofKind);

        I6DCrossLocatorMappingObservationV1 stale =
            Analyze(OwnHandUnique(), 0, frameIsCurrent: false);
        Equal(
            I6DCrossLocatorMappingProofKindV1.StaleFrame,
            stale.ProofKind);

        True(ownUnique.SafePublicIdentityAvailable);
        True(!ownUnique.UsesHiddenIdentity);
        True(!ownUnique.UsesMirrorEntityIdentity);
        True(!ownUnique.UsesRawProtocolAddress);

        I6DCrossLocatorMappingObservationV1 repeat =
            Analyze(OwnHandUnique(), 0);
        Equal(ownUnique, repeat);
    }

    internal static void TestExistingPromptBindingHasNoPrivateOccurrenceSeam()
    {
        Type[] bindingTypes =
        {
            typeof(CurrentFlatPromptBindingV1),
            typeof(FlatPromptCardCorrelationResultV1)
        };
        Type[] forbiddenOccurrenceTypes =
        {
            typeof(MirrorEntityIdV1),
            typeof(MirrorCardSnapshotV1),
            typeof(ModernLocInfoV1),
            typeof(MirrorAddressNormalizationV1)
        };

        foreach (Type type in bindingTypes)
        {
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic);
            False(fields.Any(field =>
                ContainsType(field.FieldType, forbiddenOccurrenceTypes)));

            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            False(properties.Any(property =>
                property.Name is
                    "SourceOccurrence" or
                    "SourceLocation" or
                    "MirrorEntity" or
                    "MirrorEntityId" or
                    "RawLocInfo"));
        }
    }

    internal static void TestPrivateSourceOccurrenceBindingDesignContract()
    {
        Equal(
            "PrivateCrossLocatorBindingV1",
            I6DPrivateSourceOccurrenceBindingDesignV1.CarrierName);
        Equal(
            "I6D OcgForgePublicCandidateBridgeV1",
            I6DPrivateSourceOccurrenceBindingDesignV1.SemanticOwner);
        Equal(
            "Gameplay/I4 correlation before CompleteCorrelation",
            I6DPrivateSourceOccurrenceBindingDesignV1.AcquisitionSeam);
        Equal(
            "accepted decision boundary -> OcgForgePublicCandidateBridgeV1.TryCreate",
            I6DPrivateSourceOccurrenceBindingDesignV1.ConsumptionSeam);
        Equal(
            "internal TryAccept(frame, completeProjection, completeBindings, out boundary, out error)",
            I6DPrivateSourceOccurrenceBindingDesignV1
                .BoundaryConstructionSeam);
        Equal(
            "PrivateCrossLocatorBindingSetV1",
            I6DPrivateSourceOccurrenceBindingDesignV1.BindingSetName);
        Equal(
            "internal immutable Gameplay-to-Model handoff",
            I6DPrivateSourceOccurrenceBindingDesignV1.CarrierVisibility);

        string[] expectedFields =
        {
            "PromptInstanceOrdinal",
            "ContinuationStep",
            "FrameInstanceOrdinal",
            "AcceptedPublicProjectionId",
            "I4LocalCandidateKey",
            "SourceSection",
            "SourceOrdinal",
            "AbsoluteController",
            "NormalizedZone",
            "SourceSequence",
            "IsOverlay",
            "OverlayIndex",
            "AcceptedI6C5TargetLocator"
        };
        True(I6DPrivateSourceOccurrenceBindingDesignV1.Fields
            .Select(field => field.Name)
            .SequenceEqual(expectedFields));

        I6DPrivateBindingFieldSpecV1[] fields =
            I6DPrivateSourceOccurrenceBindingDesignV1.Fields.ToArray();
        True(fields.Take(4).All(field =>
            field.Role == I6DPrivateBindingFieldRoleV1.Lifecycle));
        True(fields.Skip(4).Take(3).All(field =>
            field.Role == I6DPrivateBindingFieldRoleV1.CandidateLookup));
        True(fields.Skip(7).Take(5).All(field =>
            field.Role == I6DPrivateBindingFieldRoleV1.PrivateSourceOccurrence));
        Equal(
            I6DPrivateBindingFieldRoleV1.SafeTarget,
            fields[^1].Role);
        Equal("uint?", fields[11].TypeName);
        Equal("PublicSemanticLocatorV1", fields[^1].TypeName);
        True(fields[^1].MayFeedPublicDescriptor);
        False(fields.Any(field => field.EntersPublicIdentity));
        True(I6DPrivateSourceOccurrenceBindingDesignV1
            .AcceptedTargetMayFeedPublicDescriptor);
        True(I6DPrivateSourceOccurrenceBindingDesignV1
            .ExactTokenPathMayOmitPrivateBinding);
        True(I6DPrivateSourceOccurrenceBindingDesignV1
            .NonEqualLocatorRequiresPrivateBinding);
        False(I6DPrivateSourceOccurrenceBindingDesignV1
            .PrivateBindingInPublicDescriptor);
        False(I6DPrivateSourceOccurrenceBindingDesignV1
            .PrivateBindingInPublicActionKey);
        False(I6DPrivateSourceOccurrenceBindingDesignV1
            .PrivateBindingInDomainDigest);
        False(I6DPrivateSourceOccurrenceBindingDesignV1
            .PrivateBindingInModelInput);
        False(I6DPrivateSourceOccurrenceBindingDesignV1
            .PrivateBindingInObservation);

        string[] expectedLookupKey =
        {
            "PromptInstanceOrdinal",
            "ContinuationStep",
            "I4LocalCandidateKey"
        };
        True(I6DPrivateSourceOccurrenceBindingDesignV1.PrimaryLookupKey
            .SequenceEqual(expectedLookupKey));
        True(I6DPrivateSourceOccurrenceBindingDesignV1.CandidateCrossChecks
            .SequenceEqual(new[] { "SourceSection", "SourceOrdinal" }));

        string[] expectedInvalidations =
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
        };
        True(I6DPrivateSourceOccurrenceBindingDesignV1.InvalidationRules
            .SequenceEqual(expectedInvalidations));

        True(I6DPrivateSourceOccurrenceBindingDesignV1
            .FrameInstanceOrdinalIsSessionOwned);
        False(I6DPrivateSourceOccurrenceBindingDesignV1
            .FrameInstanceOrdinalIsCallerSupplied);
        False(I6DPrivateSourceOccurrenceBindingDesignV1
            .PrivateBindingIsPublicType);
        True(I6DPrivateSourceOccurrenceBindingDesignV1
            .CompleteBindingSetIsAcceptedAtomically);
        True(I6DPrivateSourceOccurrenceBindingDesignV1
            .BindingSetCompleteForRequiredCandidates);
        True(I6DPrivateSourceOccurrenceBindingDesignV1
            .BindingSetHasNoExtraEntries);
        False(I6DPrivateSourceOccurrenceBindingDesignV1
            .BindingSetIsDetachedCallerInput);
        True(I6DPrivateSourceOccurrenceBindingDesignV1
            .RejectionRulesAreRequirementsOnly);
        False(I6DPrivateSourceOccurrenceBindingDesignV1
            .MirrorEntityIdIsStored);
        False(I6DPrivateSourceOccurrenceBindingDesignV1
            .RawProtocolAddressIsStored);
        False(I6DPrivateSourceOccurrenceBindingDesignV1
            .HandSequenceIsPublicSubstitute);
        False(I6DPrivateSourceOccurrenceBindingDesignV1
            .PromptCardCodeIsPublicSubstitute);

        string[] forbiddenCarrierData =
        {
            "PromptLocalCardCode",
            "CardCode",
            "MirrorEntityIdV1",
            "ModernLocInfoV1",
            "RawLocInfo",
            "Pointer",
            "ObjectHash",
            "CollectionOrder"
        };
        True(I6DPrivateSourceOccurrenceBindingDesignV1.ForbiddenCarrierData
            .SequenceEqual(forbiddenCarrierData));

        Equal(4, I6DPrivateSourceOccurrenceBindingDesignV1.Cases.Count);
        True(I6DPrivateSourceOccurrenceBindingDesignV1.Cases.All(
            value => value.PublicIdentityIndependentOfPrivateData));

        I6DPrivateBindingCaseV1 unique =
            I6DPrivateSourceOccurrenceBindingDesignV1.Cases.Single(
                value => value.Name == "OwnHandUnique");
        True(unique.RequiresPrivateBinding);
        Equal(1, unique.SourceOccurrenceCount);
        True(unique.SourceOccurrencesAreDistinct);
        False(unique.ExactTokenPathUnchanged);

        I6DPrivateBindingCaseV1 duplicate =
            I6DPrivateSourceOccurrenceBindingDesignV1.Cases.Single(
                value => value.Name == "OwnHandDuplicate");
        True(duplicate.RequiresPrivateBinding);
        Equal(2, duplicate.SourceOccurrenceCount);
        True(duplicate.SourceOccurrencesAreDistinct);
        True(duplicate.TargetLocatorsAreDistinct);
        True(duplicate.PublicIdentityIndependentOfPrivateData);

        I6DPrivateBindingCaseV1 opponentHand =
            I6DPrivateSourceOccurrenceBindingDesignV1.Cases.Single(
                value => value.Name == "OpponentPublicHand");
        False(opponentHand.RequiresPrivateBinding);
        True(opponentHand.ExactTokenPathUnchanged);

        I6DPrivateBindingCaseV1 crossPile =
            I6DPrivateSourceOccurrenceBindingDesignV1.Cases.Single(
                value => value.Name == "CrossPileSameCode");
        True(crossPile.RequiresPrivateBinding);
        True(crossPile.TargetLocatorsAreDistinct);
        False(crossPile.ExactTokenPathUnchanged);

        I6DPrivateBindingPrivacyProjectionV1 privacyA = new(
            3,
            "p0:HAND:3",
            "choice=SUMMON;target=p0:HAND:3");
        I6DPrivateBindingPrivacyProjectionV1 privacyB = new(
            8,
            "p0:HAND:3",
            "choice=SUMMON;target=p0:HAND:3");
        NotEqual(
            privacyA.PrivateSourceSequence,
            privacyB.PrivateSourceSequence);
        Equal(privacyA.AcceptedTargetLocator, privacyB.AcceptedTargetLocator);
        Equal(privacyA.PublicDescriptor, privacyB.PublicDescriptor);

        Type? accidentalProductionCarrier =
            typeof(CurrentFlatPromptBindingV1).Assembly.GetType(
                "OCGForge.Ignis.Gameplay.PrivateCrossLocatorBindingV1",
                throwOnError: false);
        Null(accidentalProductionCarrier);
    }

    private static bool ContainsType(Type value, IReadOnlyList<Type> forbidden)
    {
        if (forbidden.Contains(value))
        {
            return true;
        }

        if (value.IsArray)
        {
            return ContainsType(value.GetElementType()!, forbidden);
        }

        return value.IsGenericType &&
            value.GetGenericArguments().Any(argument =>
                ContainsType(argument, forbidden));
    }

    private static I6DCrossLocatorMappingObservationV1 Analyze(
        I6DCrossLocatorCaseV1 value,
        int candidateIndex,
        IReadOnlyList<PerspectiveSafeEntityV1>? entityOverride = null,
        bool frameIsCurrent = true)
    {
        PublicCardStateV1 card = value.PublicState.Cards[candidateIndex];
        True(card.CardCode.HasValue, "characterization card must have a public code");
        FlatIdleSummonCardCodePublicCandidateV1 candidate = new(
            $"I6D_CHARACTERIZATION:{candidateIndex}",
            candidateIndex,
            card.Locator,
            card.CardCode!.Value);
        I6DCrossLocatorMappingResultV1 result =
            I6DCrossLocatorMappingAuthorityCharacterizationV1.Characterize(
                candidate,
                value.PublicState,
                entityOverride ?? value.I6C5Entities,
                frameIsCurrent);
        True(result.IsSuccess);
        return result.Observation;
    }

    private static I6DCrossLocatorMappingObservationV1[] OwnHandDuplicate()
    {
        I6DCrossLocatorCaseV1 value = CreateCase(
            new[]
            {
                MoveMessage(
                    0x1100,
                    new ModernLocInfoV1(0, 0, 0, 0),
                    new ModernLocInfoV1(0, 0x02, 0, 0x08),
                    0),
                MoveMessage(
                    0x1100,
                    new ModernLocInfoV1(0, 0, 0, 0),
                    new ModernLocInfoV1(0, 0x02, 1, 0x08),
                    0),
                new byte[] { 40, 0 }
            },
            deckCount0: 2,
            deckCount1: 0,
            extraCount0: 0,
            extraCount1: 0);
        return value.PublicState.Cards
            .Where(card => card.Zone == PublicSemanticZoneV1.Hand)
            .OrderBy(card => card.Locator.Value, StringComparer.Ordinal)
            .Select((_, index) => Analyze(value, index))
            .ToArray();
    }

    private static I6DCrossLocatorCaseV1 OwnHandUnique() =>
        CreateCase(
            new[]
            {
                MoveMessage(
                    0x1000,
                    new ModernLocInfoV1(0, 0, 0, 0),
                    new ModernLocInfoV1(0, 0x02, 0, 0x08),
                    0),
                new byte[] { 40, 0 }
            },
            deckCount0: 1,
            deckCount1: 0,
            extraCount0: 0,
            extraCount1: 0);

    private static I6DCrossLocatorCaseV1 OpponentPublicHand() =>
        CreateCase(
            new[]
            {
                DrawMessage(1, (0x1200u, 0x05u)),
                new byte[] { 40, 0 }
            },
            deckCount0: 0,
            deckCount1: 1,
            extraCount0: 0,
            extraCount1: 0);

    private static I6DCrossLocatorCaseV1 ExtraDeck() =>
        CreateCase(
            new[]
            {
                MoveMessage(
                    0x1300,
                    new ModernLocInfoV1(0, 0, 0, 0),
                    new ModernLocInfoV1(1, 0x40, 0, 0x05),
                    0),
                new byte[] { 40, 0 }
            },
            deckCount0: 0,
            deckCount1: 0,
            extraCount0: 0,
            extraCount1: 0);

    private static I6DCrossLocatorCaseV1 CrossPile() =>
        CreateCase(
            new[]
            {
                DrawMessage(
                    1,
                    (0x1400u, 0x04u),
                    (0x1500u, 0x04u)),
                MoveMessage(
                    0x1400,
                    new ModernLocInfoV1(0, 0, 0, 0),
                    new ModernLocInfoV1(1, 0x40, 0, 0x05),
                    0),
                new byte[] { 40, 0 }
            },
            deckCount0: 0,
            deckCount1: 2,
            extraCount0: 0,
            extraCount1: 1);

    private static I6DCrossLocatorCaseV1 HiddenOpponentHand() =>
        CreateCase(
            new[]
            {
                DrawMessage(1, (0x1600u, 0x08u)),
                new byte[] { 40, 0 }
            },
            deckCount0: 0,
            deckCount1: 1,
            extraCount0: 0,
            extraCount1: 0);

    private static I6DCrossLocatorCaseV1 CreateCase(
        IReadOnlyList<byte[]> messages,
        ushort deckCount0,
        ushort deckCount1,
        ushort extraCount0,
        ushort extraCount1)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                deckCount0,
                extraCount0,
                deckCount1,
                extraCount1);
        foreach (byte[] message in messages)
        {
            GameplayMessageV1 decoded = DecodeMessage(decoder, message);
            MirrorApplyResult applied = mirror.Apply(decoded);
            True(applied.IsSuccess, applied.Error.ToString());
        }

        PerspectiveSafeI6C3SourceResultV1 i6C5State =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C3(mirror);
        True(i6C5State.IsSuccess, i6C5State.Error?.ToString() ?? "I6C3 rejected");
        PublicStateProjectionResultV1 i4State =
            PublicStateProjectionV1.TryProject(
                mirror.Snapshot,
                new PublicStateProjectionContextV1(0));
        True(i4State.IsSuccess, i4State.Error.ToString());
        NotNull(i4State.Snapshot);
        return new(i4State.Snapshot!, i6C5State.Source!.Entities);
    }

    private sealed record I6DCrossLocatorCaseV1(
        PublicStateSnapshotV1 PublicState,
        IReadOnlyList<PerspectiveSafeEntityV1> I6C5Entities);
}
