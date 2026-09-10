using System.Reflection;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using OCGForge.Ignis.Model;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I6DFrameOwnedCrossLocatorIntegrationReconciliationTests
{
    internal static void TestFrameOwnedCrossLocatorIntegrationDesign()
    {
        BindingFlags flags = BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;
        True(typeof(GameplayMirrorSessionV1).GetFields(flags).Any(field =>
            field.Name == "frameInstanceOrdinal" &&
            field.FieldType == typeof(ulong)));
        False(HasFrameCoordinate(typeof(PerspectiveSafeFrameSourceResultV1)));
        False(HasFrameCoordinate(typeof(PerspectiveSafeFrameV1)));
        False(HasFrameCoordinate(typeof(PerspectiveSafeI6C3StateSourceV1)));

        Equal(
            "I6D_FRAME_OWNED_CROSS_LOCATOR_INTEGRATION_RECONCILIATION_01",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1.Task);
        Equal(
            "DESIGN_AND_CHARACTERIZATION_ONLY",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1.Status);
        Equal(
            "GameplayMirrorSessionV1 bind initialized mirror -> FRAME_0; successful owner Apply -> FRAME_N+1",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .FrameOrdinalCreationBoundary);
        Equal(
            "I6C5 projection acceptance",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .FrameOrdinalNotCreatedBy);
        Equal(
            "PerspectiveSafePublicFrameSourceV1.TryCreateI6C3 same-snapshot builder",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1.I6C3MapOwner);
        Equal(
            "MirrorEntityIdV1 -> accepted PublicSemanticLocatorV1",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1.I6C3MapShape);
        Equal(
            "one current source-build operation; discarded before public source result",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .I6C3MapLifetime);
        Equal(
            "exact normalized I4 source occurrence -> exactly one current MirrorEntityIdV1",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1.I4SourceJoin);
        Equal(
            "same-snapshot I6C3 locatorById[MirrorEntityIdV1]",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .TargetDerivation);
        Equal(
            "GameplayMirrorSessionV1 current FRAME_N lease + accepted frame-owned I4 binding",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1.HandoffProducer);
        Equal(
            "I6DPrivateCrossLocatorBindingHandoffV1",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1.HandoffType);
        Equal(
            "OcgForgeAcceptedDecisionBoundaryV1 stores one opaque handoff internally",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .HandoffToBoundary);
        Equal(
            "TryGetValidatedTarget(accepted public candidate, current accepted public frame, out safe target, out structured error)",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .HandoffOperation);
        Equal(
            "stored frame/prompt lifetime authorities + public candidate/frame cross-checks before boundary creation",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .HandoffValidation);
        Equal(
            "private retained PrivateGameplayFrameAuthorityV1 or exact revocable equivalent",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .FrameLifetimeAuthority);
        Equal(
            "private retained revocable FlatPromptSession/current-binding capability",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .PromptLifetimeAuthority);
        Equal(
            "accepted public candidate + current accepted public frame only",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .PublicConsumerInputs);
        Equal(
            "opaque handoff validates frame/projection/prompt atomically before accepted boundary creation",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .BoundaryValidation);
        Equal(
            "FRAME lifetime lease -> PROMPT lifetime lease; release in reverse order",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .LeaseAcquisitionOrder);
        Equal(
            "FlatPromptSessionV1/current frame-bound binding only",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .PromptLifetimeOwner);
        Equal(
            "stored revocable authorities, not caller coordinates, prove currentness",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .HandoffCurrentnessRule);
        Equal(
            "GameplayMirrorSessionV1",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .CompositionOwner);
        Equal(
            "FlatPromptSessionV1/current frame-bound binding only",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .PromptLifetimeOwner);
        Equal(
            "current frame authority + immutable snapshot + bound MatchContext + bound PrintedProvider",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .CompositionOwnerInputs);
        Equal(
            "FRAME lease -> PROMPT lease -> I6C3/I6C5 from FRAME snapshot -> exact I4 join -> complete binding set -> opaque handoff",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .CompositionSequence);
        Equal(
            "PrivateGameplayFrameAuthorityV1 + revocable PrivateFlatPromptBindingLifetimeAuthorityV1",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .HandoffStoredAuthorities);
        Equal(
            "handoff.TryAcquireBoundaryAcceptanceLease(accepted public frame, accepted public projection, out lease, out error)",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .BoundaryAcceptanceInterface);
        Equal(
            "FRAME lease -> PROMPT lease held through boundary construction and nextDecisionIndex increment",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .BoundaryAcceptanceLeaseLifetime);
        Equal(
            "OcgForgeAcceptedDecisionBoundaryProducerV1 acceptanceGate",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .BoundaryAcceptanceOwner);
        Equal(
            "accepted public frame + accepted public projection + opaque handoff; no private lifecycle coordinates",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .PublicHandoffInputs);
        Equal(
            "validation failure -> no boundary -> no decision-index consumption",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .BoundaryFailureSemantics);
        Equal(
            "existing boundary overload remains unchanged; non-equal locator mapping requires handoff",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .LegacyBoundaryBehavior);
        Equal(
            "CardCode search, first match, collection order, I4 ordinal arithmetic, source sequence public alias",
            I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
                .ForbiddenTargetReconstruction);

        True(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .FrameOrdinalIsSessionOwned);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .FrameOrdinalIsCreatedByI6C5Acceptance);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .I6C3MapIsPublic);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .I6C3MapIsSerialized);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .I6C3MapIsModelInput);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .I6C3MapIsReplayIdentity);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .MirrorEntityIdCrossesBoundary);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .RawLocInfoCrossesBoundary);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .HandoffIsDetachedCallerInput);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .HandoffReturnsPrivateOccurrence);
        True(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .HandoffReturnsOnlySafeTarget);
        True(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .HandoffStoresFrameLifetimeAuthority);
        True(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .HandoffStoresPromptLifetimeAuthority);
        True(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .HandoffCoordinatesAreDiagnosticOnly);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .FrameOrdinalCallerAuthority);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .PromptOrdinalCallerAuthority);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .ContinuationStepCallerAuthority);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .PublicConsumerNeedsPrivateCoordinates);
        True(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .BoundaryValidationIsAtomic);
        True(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .MismatchedHandoffRejectedBeforeBoundary);
        True(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .StaleHandoffRejected);
        True(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .CompositionOwnerIsExactlyOne);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .FlatPromptSessionAloneIsI6C5Owner);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .TransientLocatorMapDetached);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .TransientLocatorMapPublic);
        True(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .BoundaryAcceptanceLeaseIsExact);
        True(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .BoundaryAcceptanceLeaseHoldsAuthorities);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .FailedAcceptanceConsumesDecisionIndex);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .PublicConsumerNeedsFrameOrdinal);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .PublicConsumerNeedsPromptOrdinal);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .PublicConsumerNeedsContinuationStep);
        True(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .ExactTokenPathMayOmitHandoff);
        True(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .NonEqualLocatorRequiresHandoff);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .I6DImplementationPresent);
        False(I6DFrameOwnedCrossLocatorIntegrationReconciliationV1
            .I6DImplementationAuthorized);
    }

    internal static void TestI6C3LocatorMapIsTransientAndFrameBound()
    {
        const uint cardCode = 0x11223344;
        (PerspectiveStateMirrorV1 mirror,
            GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                deckCount0: 1,
                extraCount0: 0,
                deckCount1: 0,
                extraCount1: 0);
        MirrorApplyResult moved = mirror.Apply(DecodeMessage(
            decoder,
            MoveMessage(
                cardCode,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x02, 0, 0x08),
                0)));
        True(moved.IsSuccess, moved.Error.ToString());

        PerspectiveSafeI6C3SourceResultV1 i6c3 =
            PerspectiveSafePublicFrameSourceV1.TryCreateI6C3(mirror);
        True(i6c3.IsSuccess, i6c3.Error?.ToString() ?? "I6C3 rejected");
        NotNull(i6c3.Source);
        True(i6c3.Source!.Entities.Any(entity =>
            entity.IdentityKnown &&
            entity.Passcode == cardCode &&
            !string.IsNullOrEmpty(entity.Locator)));

        False(HasPrivateOccurrenceMapSurface(
            typeof(PerspectiveSafeI6C3SourceResultV1)));
        False(HasPrivateOccurrenceMapSurface(
            typeof(PerspectiveSafeI6C3StateSourceV1)));
        False(HasPrivateOccurrenceMapSurface(typeof(PerspectiveSafeFrameV1)));
        False(HasPrivateOccurrenceMapSurface(typeof(PerspectiveSafeEntityV1)));

        Assembly gameplayAssembly = typeof(PerspectiveSafeFrameV1).Assembly;
        Null(gameplayAssembly.GetType(
            "OCGForge.Ignis.Gameplay.I6DPrivateCrossLocatorBindingHandoffV1",
            throwOnError: false));
        Null(gameplayAssembly.GetType(
            "OCGForge.Ignis.Gameplay.PrivateCrossLocatorBindingV1",
            throwOnError: false));
    }

    internal static void TestI6DModelBoundaryHasNoCurrentPrivateHandoff()
    {
        Type boundaryType = typeof(OcgForgeAcceptedDecisionBoundaryV1);
        BindingFlags flags = BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic;
        False(boundaryType.GetFields(flags).Any(field =>
            ContainsI6DPrivateHandoffName(field.Name) ||
            ContainsI6DPrivateHandoffName(field.FieldType.Name)));
        False(boundaryType.GetProperties(flags).Any(property =>
            ContainsI6DPrivateHandoffName(property.Name) ||
            ContainsI6DPrivateHandoffName(property.PropertyType.Name)));
        False(boundaryType.GetConstructors(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic)
            .Any(constructor => constructor.GetParameters().Any(parameter =>
                ContainsI6DPrivateHandoffName(parameter.ParameterType.Name))));

        Type publicPromptContextType = typeof(FlatPromptPublicContextV1);
        False(publicPromptContextType.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic)
            .Any(property => property.Name is
                "FrameInstanceOrdinal" or
                "PromptInstanceOrdinal" or
                "ContinuationStep"));

        Type producerType = typeof(OcgForgeAcceptedDecisionBoundaryProducerV1);
        False(producerType.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic)
            .Any(method => method.GetParameters().Any(parameter =>
                ContainsI6DPrivateHandoffName(parameter.ParameterType.Name))));
    }

    private static bool HasPrivateOccurrenceMapSurface(Type type)
    {
        BindingFlags flags = BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic;
        return type.GetFields(flags).Any(field =>
                   ContainsPrivateMapName(field.Name) ||
                   ContainsPrivateMapName(field.FieldType.Name) ||
                   ContainsMirrorEntityType(field.FieldType)) ||
            type.GetProperties(flags).Any(property =>
                ContainsPrivateMapName(property.Name) ||
                ContainsPrivateMapName(property.PropertyType.Name) ||
                ContainsMirrorEntityType(property.PropertyType));
    }

    private static bool ContainsPrivateMapName(string name) =>
        name.Contains("locatorById", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("MirrorEntityId", StringComparison.Ordinal) ||
        name.Contains("OccurrenceMap", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsMirrorEntityType(Type type)
    {
        if (type == typeof(MirrorEntityIdV1))
        {
            return true;
        }

        if (type.IsArray)
        {
            return ContainsMirrorEntityType(type.GetElementType()!);
        }

        return type.IsGenericType && type.GetGenericArguments().Any(
            ContainsMirrorEntityType);
    }

    private static bool ContainsI6DPrivateHandoffName(string? name) =>
        name?.Contains(
            "I6DPrivateCrossLocatorBindingHandoff",
            StringComparison.Ordinal) == true;

    private static bool HasFrameCoordinate(Type type)
    {
        BindingFlags flags = BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;
        return type.GetFields(flags).Any(field =>
                   field.Name.Contains(
                       "FrameInstanceOrdinal",
                       StringComparison.OrdinalIgnoreCase)) ||
            type.GetProperties(flags).Any(property =>
                property.Name.Contains(
                    "FrameInstanceOrdinal",
                    StringComparison.OrdinalIgnoreCase));
    }
}
