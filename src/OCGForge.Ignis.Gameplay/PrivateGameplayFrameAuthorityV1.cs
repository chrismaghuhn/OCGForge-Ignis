namespace OCGForge.Ignis.Gameplay;

/// <summary>
/// Internal current-frame authority owned by one gameplay session. The
/// coordinate and its immutable mirror snapshot are never public, serialized,
/// or used as semantic identity.
/// </summary>
internal sealed class PrivateGameplayFrameAuthorityV1
{
    private readonly object lifecycleGate = new();
    private bool invalidated;

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

    internal bool IsCurrent
    {
        get
        {
            lock (lifecycleGate)
            {
                return !invalidated;
            }
        }
    }

    internal PrivateGameplayFrameAuthorityLeaseV1? TryAcquire()
    {
        Monitor.Enter(lifecycleGate);
        if (invalidated)
        {
            Monitor.Exit(lifecycleGate);
            return null;
        }

        return new PrivateGameplayFrameAuthorityLeaseV1(lifecycleGate);
    }

    internal void Invalidate()
    {
        lock (lifecycleGate)
        {
            invalidated = true;
        }
    }
}

internal sealed class PrivateGameplayFrameAuthorityLeaseV1 : IDisposable
{
    private object? lifecycleGate;

    internal PrivateGameplayFrameAuthorityLeaseV1(object lifecycleGate)
    {
        this.lifecycleGate = lifecycleGate ??
            throw new ArgumentNullException(nameof(lifecycleGate));
    }

    public void Dispose()
    {
        object? gate = Interlocked.Exchange(ref lifecycleGate, null);
        if (gate is not null)
        {
            Monitor.Exit(gate);
        }
    }
}
