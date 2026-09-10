namespace OCGForge.Ignis.Gameplay;

/// <summary>
/// Process-local lifetime authority for one accepted prompt binding. The
/// authority is deliberately opaque and never contributes to public identity.
/// </summary>
internal sealed class PrivateFlatPromptBindingLifetimeAuthorityV1
{
    private readonly object lifecycleGate = new();
    private bool invalidated;

    internal PrivateFlatPromptBindingLifetimeLeaseV1? TryAcquire()
    {
        Monitor.Enter(lifecycleGate);
        if (invalidated)
        {
            Monitor.Exit(lifecycleGate);
            return null;
        }

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
        lock (lifecycleGate)
        {
            invalidated = true;
        }
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
