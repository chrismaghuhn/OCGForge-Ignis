namespace OCGForge.Ignis.Gameplay;

/// <summary>
/// Same-snapshot I6C3 locator provenance. The mirror entity key is retained
/// only while Gameplay assembles the I6D handoff and never crosses into Model.
/// </summary>
internal sealed class PrivateI6C3LocatorMapV1
{
    private readonly Dictionary<MirrorEntityIdV1, string> locatorById;

    private PrivateI6C3LocatorMapV1(
        IReadOnlyDictionary<MirrorEntityIdV1, string> locatorById)
    {
        this.locatorById = new Dictionary<MirrorEntityIdV1, string>(locatorById);
    }

    internal static bool TryCreate(
        IReadOnlyDictionary<MirrorEntityIdV1, string>? locatorById,
        out PrivateI6C3LocatorMapV1? result)
    {
        result = null;
        if (locatorById is null)
        {
            return false;
        }

        if (locatorById.Any(pair =>
                pair.Value is null ||
                !PublicSemanticLocatorV1.TryParse(
                    pair.Value,
                    out PublicSemanticLocatorV1? parsed) ||
                parsed is null))
        {
            return false;
        }

        result = new PrivateI6C3LocatorMapV1(locatorById);
        return true;
    }

    internal bool TryGet(
        MirrorEntityIdV1 entityId,
        out PublicSemanticLocatorV1? locator)
    {
        locator = null;
        return locatorById.TryGetValue(entityId, out string? value) &&
            PublicSemanticLocatorV1.TryParse(value, out locator) &&
            locator is not null;
    }
}

internal sealed class PrivateI6DFrameCompositionResultV1
{
    private PrivateI6DFrameCompositionResultV1(
        PerspectiveSafeFrameSourceResultV1 frameResult,
        PrivateI6C3LocatorMapV1? locatorMap)
    {
        FrameResult = frameResult ??
            throw new ArgumentNullException(nameof(frameResult));
        LocatorMap = locatorMap;
    }

    internal PerspectiveSafeFrameSourceResultV1 FrameResult { get; }

    internal PrivateI6C3LocatorMapV1? LocatorMap { get; }

    internal static PrivateI6DFrameCompositionResultV1 Success(
        PerspectiveSafeFrameSourceResultV1 frameResult,
        PrivateI6C3LocatorMapV1 locatorMap) =>
        new(
            frameResult.IsSuccess
                ? frameResult
                : throw new ArgumentException(
                    "A successful I6D composition needs a successful frame.",
                    nameof(frameResult)),
            locatorMap ?? throw new ArgumentNullException(nameof(locatorMap)));

    internal static PrivateI6DFrameCompositionResultV1 Failure(
        PerspectiveSafeFrameSourceResultV1 frameResult) =>
        new(
            frameResult.IsSuccess
                ? throw new ArgumentException(
                    "A failed I6D composition needs a failed frame.",
                    nameof(frameResult))
                : frameResult,
            null);
}
