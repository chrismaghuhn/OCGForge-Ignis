namespace OCGForge.Ignis.Gameplay.Tests.Fixtures;

internal static class I4FrameOwnedSidecarLifecycleReconciliationV1
{
    internal const string ExistingFrameOwner =
        "GameplayMirrorSessionV1 owns Mirror only";

    internal const string ExistingFrameOrdinalAuthority = "ABSENT";

    internal const string ExistingProjectionOrdinalSource =
        "caller-supplied PublicStateProjectionV1.TryProject(..., ulong)";

    internal const string ExistingConsumerCheck =
        "sidecar.FrameInstanceOrdinal -> same ordinal re-projection";

    internal const string CurrentFrameMatch = "NOT_PROVEN";

    internal const string StaleFrameRejection = "NOT_PROVEN";

    internal const string ProposedOwner = "GameplayMirrorSessionV1";

    internal const string ProposedAuthorityType =
        "PrivateGameplayFrameAuthorityV1";

    internal const string ProposedCoordinate = "ulong FrameInstanceOrdinal";

    internal const string ProposedCreationBoundary =
        "PerspectiveStateMirrorV1.TryCreate(MSG_START) -> GameplayMirrorSessionV1 binds existing mirror -> FRAME_0";

    internal const string ProposedNonCreationEvents =
        "presentation packet, failed apply, projection read, prompt acceptance";

    internal const string ProposedInvalidation =
        "next committed mirror frame, failed boundary, session disposal";

    internal const string I5SidecarEnablement = "OUT_OF_SCOPE";

    internal const string I5Baseline =
        "restore exact 65ccf707c802f42a942423e4e6fd0939fd6d342a behavior";

    internal const bool PrivateAuthorityIsNotPublicIdentity = true;

    internal const bool PrivateAuthorityIsNotSerialized = true;

    internal const bool PrivateAuthorityUsesNoWallClock = true;

    internal const bool PrivateAuthorityUsesNoPid = true;
}
