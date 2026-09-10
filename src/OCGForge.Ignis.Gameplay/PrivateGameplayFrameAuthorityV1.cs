namespace OCGForge.Ignis.Gameplay;

/// <summary>
/// Internal current-frame authority owned by one gameplay session. The
/// coordinate and its immutable mirror snapshot are never public, serialized,
/// or used as semantic identity.
/// </summary>
internal sealed class PrivateGameplayFrameAuthorityV1
{
    private int invalidated;

    internal PrivateGameplayFrameAuthorityV1(
        ulong frameInstanceOrdinal,
        MirrorSnapshotV1 mirrorSnapshot)
    {
        FrameInstanceOrdinal = frameInstanceOrdinal;
        MirrorSnapshot = mirrorSnapshot ??
            throw new ArgumentNullException(nameof(mirrorSnapshot));
    }

    internal ulong FrameInstanceOrdinal { get; }

    internal MirrorSnapshotV1 MirrorSnapshot { get; }

    internal bool IsCurrent => Volatile.Read(ref invalidated) == 0;

    internal void Invalidate() => Volatile.Write(ref invalidated, 1);
}
