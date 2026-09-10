namespace OCGForge.Ignis.Gameplay;

public enum I6DFrameOwnedCompositionErrorCodeV1 : byte
{
    None = 0,
    InvalidInput = 1,
    InvalidState = 2,
    PromptBindingMismatch = 3,
    FrameSourceFailure = 4,
    CrossLocatorBindingFailure = 5
}

public readonly record struct I6DFrameOwnedCompositionErrorV1(
    I6DFrameOwnedCompositionErrorCodeV1 Code,
    string FieldPath);

/// <summary>
/// Safe output of the Gameplay-owned same-snapshot I6D composition. The
/// handoff is opaque; no frame lifecycle coordinate or private occurrence is
/// exposed by this result.
/// </summary>
public sealed class I6DFrameOwnedCompositionResultV1
{
    private I6DFrameOwnedCompositionResultV1(
        bool isSuccess,
        PerspectiveSafeFrameV1? frame,
        I6DPrivateCrossLocatorBindingHandoffV1? handoff,
        I6DFrameOwnedCompositionErrorV1? error)
    {
        IsSuccess = isSuccess;
        Frame = frame;
        Handoff = handoff;
        Error = error;
    }

    public bool IsSuccess { get; }

    public PerspectiveSafeFrameV1? Frame { get; }

    public I6DPrivateCrossLocatorBindingHandoffV1? Handoff { get; }

    public I6DFrameOwnedCompositionErrorV1? Error { get; }

    internal static I6DFrameOwnedCompositionResultV1 Success(
        PerspectiveSafeFrameV1 frame,
        I6DPrivateCrossLocatorBindingHandoffV1? handoff) =>
        new(
            true,
            frame ?? throw new ArgumentNullException(nameof(frame)),
            handoff,
            null);

    internal static I6DFrameOwnedCompositionResultV1 Failure(
        I6DFrameOwnedCompositionErrorCodeV1 code,
        string fieldPath) =>
        new(false, null, null, new(code, fieldPath));
}
