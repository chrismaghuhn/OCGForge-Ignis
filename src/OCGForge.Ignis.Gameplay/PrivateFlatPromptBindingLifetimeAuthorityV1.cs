namespace OCGForge.Ignis.Gameplay;

/// <summary>
/// Process-local lifetime authority for one accepted prompt binding. The
/// authority is deliberately opaque and never contributes to public identity.
/// </summary>
internal sealed class PrivateFlatPromptBindingLifetimeAuthorityV1
{
    private readonly object lifecycleGate = new();
    private Action<bool>? testLeaseAcquisitionHook;
    private Action<bool>? testInvalidationHook;
    private bool invalidated;

    internal PrivateFlatPromptBindingLifetimeLeaseV1? TryAcquire()
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
        return new PrivateFlatPromptBindingLifetimeLeaseV1(lifecycleGate);
    }

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

    internal void Invalidate()
    {
        Action<bool>? hook = Volatile.Read(ref testInvalidationHook);
        hook?.Invoke(false);
        lock (lifecycleGate)
        {
            invalidated = true;
            hook?.Invoke(true);
        }
    }

    /// <summary>
    /// Installs or clears an internal, test-only coordination hook at the real
    /// prompt lease boundary. The boolean is false before the monitor attempt
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
    /// Installs or clears an internal, test-only coordination hook around
    /// invalidation. The true callback runs while lifecycleGate is held and
    /// after invalidated has been set, allowing tests to force the real
    /// invalidation-versus-acquisition ordering without timing assumptions.
    /// </summary>
    internal void SetTestInvalidationHook(Action<bool>? hook)
    {
        if (hook is null)
        {
            Volatile.Write(ref testInvalidationHook, null);
            return;
        }

        if (Interlocked.CompareExchange(
                ref testInvalidationHook,
                hook,
                null) is not null)
        {
            throw new InvalidOperationException(
                "A test invalidation hook is already installed.");
        }
    }

    /// <summary>
    /// Internal test-only observation of the same monitor used by
    /// <see cref="TryAcquire"/>. It does not acquire or mutate authority;
    /// it only proves that a held lease excludes another entrant.
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
}

internal sealed class PrivateFlatPromptBindingLifetimeLeaseV1 : IDisposable
{
    private object? lifecycleGate;

    internal PrivateFlatPromptBindingLifetimeLeaseV1(object lifecycleGate)
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
