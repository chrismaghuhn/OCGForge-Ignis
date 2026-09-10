using System.Reflection;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I4FrameOwnedSidecarLifecycleReconciliationTests
{
    internal static void TestExistingFrameAuthorityIsAbsent()
    {
        Equal(
            "GameplayMirrorSessionV1 owns Mirror only",
            I4FrameOwnedSidecarLifecycleReconciliationV1.ExistingFrameOwner);
        Equal(
            "ABSENT",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ExistingFrameOrdinalAuthority);
        Equal(
            "caller-supplied PublicStateProjectionV1.TryProject(..., ulong)",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ExistingProjectionOrdinalSource);

        Type[] currentFrameConsumers =
        {
            typeof(GameplayMirrorSessionV1),
            typeof(FlatPromptSessionV1),
            typeof(PerspectiveSafeFrameV1)
        };
        foreach (Type type in currentFrameConsumers)
        {
            False(HasNamedFrameCoordinate(type));
        }

        MethodInfo[] frameOwnedProjectionMethods = typeof(
            PublicStateProjectionV1).GetMethods(
                BindingFlags.Static | BindingFlags.NonPublic)
            .Where(method => method.Name == "TryProject")
            .Where(method => method.GetParameters().Length == 3)
            .ToArray();
        Equal(1, frameOwnedProjectionMethods.Length);
        Equal(
            typeof(ulong),
            frameOwnedProjectionMethods[0].GetParameters()[2].ParameterType);

        PropertyInfo? sidecarFrameOrdinal = typeof(
            PrivateI4OccurrencePublicLocatorSidecarV1).GetProperty(
                "FrameInstanceOrdinal",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
        NotNull(sidecarFrameOrdinal);
    }

    internal static void TestCurrentSidecarFrameOrdinalIsSelfAuthenticated()
    {
        const uint cardCode = 0x11223344;
        (PerspectiveStateMirrorV1 mirror,
            PublicStateProjectionResultV1 projection) =
            CreateOwnHandFrameAuthority(cardCode, frameInstanceOrdinal: 41);
        PrivateI4OccurrencePublicLocatorSidecarV1 originalSidecar =
            projection.PrivateOccurrenceSidecar!;
        True(PrivateI4OccurrencePublicLocatorSidecarV1.TryCreate(
                99,
                projection.PublicProjectionId,
                originalSidecar.Entries,
                out PrivateI4OccurrencePublicLocatorSidecarV1? alternateSidecar));

        PublicStateProjectionResultV1 alternateProjection =
            PublicStateProjectionResultV1.Success(
                projection.Snapshot!,
                projection.CanonicalBytes.ToArray(),
                projection.Sha256!,
                alternateSidecar!);
        FlatPromptProjectionResultV1 result = new FlatPromptSessionV1()
            .TryAcceptPrompt(
                SingleOwnHandIdleMessage(cardCode),
                mirror,
                alternateProjection);

        True(result.IsSuccess, result.Error.ToString());
        Equal(
            "sidecar.FrameInstanceOrdinal -> same ordinal re-projection",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ExistingConsumerCheck);
        Equal(
            "NOT_PROVEN",
            I4FrameOwnedSidecarLifecycleReconciliationV1.CurrentFrameMatch);
        Equal(
            "NOT_PROVEN",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .StaleFrameRejection);
    }

    internal static void TestCurrentI5SidecarEnablementIsOutOfScope()
    {
        const uint cardCode = 0x11223344;
        (PerspectiveStateMirrorV1 mirror,
            PublicStateProjectionResultV1 projection) =
            CreateOwnHandFrameAuthority(
                cardCode,
                cardCode,
                frameInstanceOrdinal: 41);
        FlatPromptProjectionResultV1 result = new FlatPromptSessionV1()
            .TryAcceptI5Prompt(
                DuplicateOwnHandSelectCardMessage(cardCode),
                mirror,
                projection);

        True(result.IsSuccess, result.Error.ToString());
        NotNull(result.Candidates);
        Equal(2, result.Candidates!.Count);
        Equal(
            "OUT_OF_SCOPE",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .I5SidecarEnablement);
        Equal(
            "restore exact 65ccf707c802f42a942423e4e6fd0939fd6d342a behavior",
            I4FrameOwnedSidecarLifecycleReconciliationV1.I5Baseline);
    }

    internal static void TestProposedFrameAuthorityHasSafeLifecycle()
    {
        Equal(
            "GameplayMirrorSessionV1",
            I4FrameOwnedSidecarLifecycleReconciliationV1.ProposedOwner);
        Equal(
            "PrivateGameplayFrameAuthorityV1",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ProposedAuthorityType);
        Equal(
            "ulong FrameInstanceOrdinal",
            I4FrameOwnedSidecarLifecycleReconciliationV1.ProposedCoordinate);
        Equal(
            "initial MSG_START or one successful state-message mirror commit",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ProposedCreationBoundary);
        Equal(
            "presentation packet, failed apply, projection read, prompt acceptance",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ProposedNonCreationEvents);
        Equal(
            "next committed mirror frame, failed boundary, session disposal",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ProposedInvalidation);
        True(I4FrameOwnedSidecarLifecycleReconciliationV1
            .PrivateAuthorityIsNotPublicIdentity);
        True(I4FrameOwnedSidecarLifecycleReconciliationV1
            .PrivateAuthorityIsNotSerialized);
        True(I4FrameOwnedSidecarLifecycleReconciliationV1
            .PrivateAuthorityUsesNoWallClock);
        True(I4FrameOwnedSidecarLifecycleReconciliationV1
            .PrivateAuthorityUsesNoPid);
    }

    private static bool HasNamedFrameCoordinate(Type type)
    {
        BindingFlags flags = BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;
        return type.GetFields(flags).Any(field =>
                   field.Name.Contains(
                       "FrameInstanceOrdinal",
                       StringComparison.Ordinal)) ||
            type.GetProperties(flags).Any(property =>
                property.Name.Contains(
                    "FrameInstanceOrdinal",
                    StringComparison.Ordinal));
    }

    private static (
        PerspectiveStateMirrorV1 Mirror,
        PublicStateProjectionResultV1 Projection)
        CreateOwnHandFrameAuthority(
            uint firstCardCode,
            uint? secondCardCode = null,
            ulong frameInstanceOrdinal = 0)
    {
        uint[] cardCodes = secondCardCode.HasValue
            ? new[] { firstCardCode, secondCardCode.Value }
            : new[] { firstCardCode };
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                deckCount0: checked((ushort)cardCodes.Length),
                extraCount0: 0,
                deckCount1: 0,
                extraCount1: 0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        for (uint sequence = 0; sequence < cardCodes.Length; sequence++)
        {
            MirrorApplyResult moved = mirror.Apply(DecodeMessage(
                decoder,
                MoveMessage(
                    cardCodes[checked((int)sequence)],
                    empty,
                    new ModernLocInfoV1(0, 0x02, sequence, 0x08),
                    0)));
            True(moved.IsSuccess, moved.Error.ToString());
        }

        PublicStateProjectionResultV1 projection =
            PublicStateProjectionV1.TryProject(
                mirror.Snapshot,
                new PublicStateProjectionContextV1(0),
                frameInstanceOrdinal);
        True(projection.IsSuccess, projection.Error.ToString());
        NotNull(projection.PrivateOccurrenceSidecar);
        return (mirror, projection);
    }

    private static byte[] SingleOwnHandIdleMessage(uint cardCode) =>
        Join(
            new byte[] { 11, 0 },
            U32(1),
            U32(cardCode),
            new byte[] { 0, 0x02 },
            U32(0),
            U32(0),
            U32(0),
            U32(0),
            U32(0),
            U32(0),
            new byte[] { 0, 0, 0 });

    private static byte[] DuplicateOwnHandSelectCardMessage(uint cardCode) =>
        Join(
            new byte[] { 15, 0, 0 },
            U32(1),
            U32(2),
            U32(2),
            U32(cardCode),
            LocInfo(0, 0x02, 0, 0x08),
            U32(cardCode),
            LocInfo(0, 0x02, 1, 0x08));
}
