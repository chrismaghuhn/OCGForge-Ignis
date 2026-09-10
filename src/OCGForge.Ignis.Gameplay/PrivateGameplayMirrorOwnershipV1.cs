namespace OCGForge.Ignis.Gameplay;

/// <summary>
/// Process-local capability proving that one GameplayMirrorSessionV1 owns a
/// particular PerspectiveStateMirrorV1. It has no public or serializable
/// identity.
/// </summary>
internal sealed class PrivateGameplayMirrorOwnershipV1
{
    private readonly PerspectiveStateMirrorV1 mirror;

    private PrivateGameplayMirrorOwnershipV1(
        PerspectiveStateMirrorV1 mirror)
    {
        this.mirror = mirror ??
            throw new ArgumentNullException(nameof(mirror));
    }

    internal static PrivateGameplayMirrorOwnershipV1 Create(
        PerspectiveStateMirrorV1 mirror) =>
        new(mirror);

    internal bool BelongsTo(PerspectiveStateMirrorV1 candidate) =>
        ReferenceEquals(mirror, candidate);
}
