namespace OCGForge.Ignis.Gameplay;

/// <summary>
/// Internal current-frame authority owned by one gameplay session. The
/// coordinate and its immutable mirror snapshot are never public, serialized,
/// or used as semantic identity.
/// </summary>
internal sealed class PrivateGameplayFrameAuthorityV1
{
    private readonly object lifecycleGate = new();
    private Action<bool>? testLeaseAcquisitionHook;
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
        Action<bool>? hook = Volatile.Read(ref testLeaseAcquisitionHook);
        hook?.Invoke(false);
        Monitor.Enter(lifecycleGate);
        if (invalidated)
        {
            Monitor.Exit(lifecycleGate);
            return null;
        }

        hook?.Invoke(true);
        return new PrivateGameplayFrameAuthorityLeaseV1(lifecycleGate);
    }

    /// <summary>
    /// Installs or clears an internal, test-only coordination hook at the
    /// real lease boundary. The boolean is false before the monitor attempt
    /// and true after a successful acquisition. It has no effect unless a
    /// test explicitly installs it and is never part of public, serialized,
    /// or semantic state.
    /// </summary>
    internal void SetTestLeaseAcquisitionHook(Action<bool>? hook)
    {
        if (hook is null)
        {
            Volatile.Write(ref testLeaseAcquisitionHook, null);
            return;
        }

        if (Interlocked.CompareExchange(
                ref testLeaseAcquisitionHook,
                hook,
                null) is not null)
        {
            throw new InvalidOperationException(
                "A test lease hook is already installed.");
        }
    }

    /// <summary>
    /// Internal test-only observation of the same monitor used by
    /// <see cref="TryAcquire"/>. This does not acquire or mutate authority;
    /// it only proves that a held lease still excludes another entrant.
    /// </summary>
    internal bool IsLeaseAvailableForTest()
    {
        if (!Monitor.TryEnter(lifecycleGate))
        {
            return false;
        }

        Monitor.Exit(lifecycleGate);
        return true;
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
