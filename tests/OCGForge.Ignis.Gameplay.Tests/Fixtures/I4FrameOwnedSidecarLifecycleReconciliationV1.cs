namespace OCGForge.Ignis.Gameplay.Tests.Fixtures;

internal static class I4FrameOwnedSidecarLifecycleReconciliationV1
{
    internal const string ExistingFrameOwner =
        "GameplayMirrorSessionV1 owns current Mirror and FrameInstanceOrdinal";

    internal const string ExistingFrameOrdinalAuthority = "SESSION_OWNED";

    internal const string ExistingProjectionOrdinalSource =
        "GameplayMirrorSessionV1 current authority supplies PublicStateProjectionV1.TryProject(..., ulong)";

    internal const string ExistingConsumerCheck =
        "current frame authority independently matches sidecar";

    internal const string CurrentFrameMatch = "PASS";

    internal const string StaleFrameRejection = "PASS";

    internal const string ImplementedOwner = "GameplayMirrorSessionV1";

    internal const string ImplementedAuthorityType =
        "PrivateGameplayFrameAuthorityV1";

    internal const string ImplementedCoordinate = "ulong FrameInstanceOrdinal";

    internal const string ImplementedCreationBoundary =
        "PerspectiveStateMirrorV1.TryCreate(MSG_START) -> GameplayMirrorSessionV1 binds existing mirror -> FRAME_0";

    internal const string ImplementedNonCreationEvents =
        "presentation packet, failed apply, projection read, prompt acceptance";

    internal const string ImplementedInvalidation =
        "next committed mirror frame, failed boundary, session disposal";

    internal const string I5SidecarEnablement = "OUT_OF_SCOPE";

    internal const string I5Baseline =
        "restore exact 65ccf707c802f42a942423e4e6fd0939fd6d342a behavior";

    internal const bool PrivateAuthorityIsNotPublicIdentity = true;

    internal const bool PrivateAuthorityIsNotSerialized = true;

    internal const bool PrivateAuthorityUsesNoWallClock = true;

    internal const bool PrivateAuthorityUsesNoPid = true;
}
