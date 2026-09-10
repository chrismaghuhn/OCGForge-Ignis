namespace OCGForge.Ignis.Gameplay;

/// <summary>
/// Internal projection-scoped provenance for joining an exact current pile
/// occurrence to an already emitted I4 public locator. It is not a public
/// identity or a prompt-domain value.
/// </summary>
internal sealed class PrivateI4OccurrencePublicLocatorSidecarV1
{
    private readonly PrivateI4OccurrencePublicLocatorSidecarEntryV1[] entries;
    private readonly IReadOnlyList<
        PrivateI4OccurrencePublicLocatorSidecarEntryV1> entriesView;
    private readonly Dictionary<PrivateI4OccurrenceKeyV1,
        PrivateI4OccurrencePublicLocatorSidecarEntryV1> entryByOccurrence;

    private PrivateI4OccurrencePublicLocatorSidecarV1(
        ulong frameInstanceOrdinal,
        string acceptedPublicProjectionId,
        PrivateI4OccurrencePublicLocatorSidecarEntryV1[] entries,
        Dictionary<PrivateI4OccurrenceKeyV1,
            PrivateI4OccurrencePublicLocatorSidecarEntryV1> entryByOccurrence)
    {
        FrameInstanceOrdinal = frameInstanceOrdinal;
        AcceptedPublicProjectionId = acceptedPublicProjectionId;
        this.entries = entries.ToArray();
        entriesView = Array.AsReadOnly(this.entries);
        this.entryByOccurrence = entryByOccurrence;
    }

    internal ulong FrameInstanceOrdinal { get; }

    internal string AcceptedPublicProjectionId { get; }

    internal IReadOnlyList<PrivateI4OccurrencePublicLocatorSidecarEntryV1>
        Entries => entriesView;

    internal bool IsEquivalentTo(
        PrivateI4OccurrencePublicLocatorSidecarV1? other)
    {
        if (other is null ||
            FrameInstanceOrdinal != other.FrameInstanceOrdinal ||
            !string.Equals(
                AcceptedPublicProjectionId,
                other.AcceptedPublicProjectionId,
                StringComparison.Ordinal) ||
            entries.Length != other.entries.Length)
        {
            return false;
        }

        for (int index = 0; index < entries.Length; index++)
        {
            PrivateI4OccurrencePublicLocatorSidecarEntryV1 left =
                entries[index];
            PrivateI4OccurrencePublicLocatorSidecarEntryV1 right =
                other.entries[index];
            if (left.AbsoluteController != right.AbsoluteController ||
                left.NormalizedZone != right.NormalizedZone ||
                left.SourceSequence != right.SourceSequence ||
                left.IsOverlay != right.IsOverlay ||
                left.OverlayIndex != right.OverlayIndex ||
                left.AcceptedI4PublicLocator !=
                    right.AcceptedI4PublicLocator)
            {
                return false;
            }
        }

        return true;
    }

    internal bool TryGet(
        byte absoluteController,
        MirrorZoneV1 normalizedZone,
        uint sourceSequence,
        bool isOverlay,
        uint? overlayIndex,
        out PrivateI4OccurrencePublicLocatorSidecarEntryV1? entry)
    {
        entry = null;
        if (absoluteController > 1 ||
            normalizedZone is not (MirrorZoneV1.Hand or MirrorZoneV1.ExtraDeck) ||
            isOverlay != overlayIndex.HasValue)
        {
            return false;
        }

        return entryByOccurrence.TryGetValue(
            new PrivateI4OccurrenceKeyV1(
                absoluteController,
                normalizedZone,
                sourceSequence,
                isOverlay,
                overlayIndex),
            out entry);
    }

    internal static bool TryCreate(
        ulong frameInstanceOrdinal,
        string? acceptedPublicProjectionId,
        IEnumerable<PrivateI4OccurrencePublicLocatorSidecarEntryV1>?
            sourceEntries,
        out PrivateI4OccurrencePublicLocatorSidecarV1? sidecar)
    {
        sidecar = null;
        if (string.IsNullOrEmpty(acceptedPublicProjectionId) ||
            sourceEntries is null)
        {
            return false;
        }

        PrivateI4OccurrencePublicLocatorSidecarEntryV1[] source;
        try
        {
            source = sourceEntries.ToArray();
        }
        catch (ArgumentNullException)
        {
            return false;
        }

        if (source.Any(entry => entry is null))
        {
            return false;
        }

        PrivateI4OccurrencePublicLocatorSidecarEntryV1[] ordered;
        ordered = source
            .OrderBy(entry => entry.AbsoluteController)
            .ThenBy(entry => (byte)entry.NormalizedZone)
            .ThenBy(entry => entry.SourceSequence)
            .ThenBy(entry => entry.IsOverlay ? 1 : 0)
            .ThenBy(entry => entry.OverlayIndex ?? 0)
            .ToArray();

        Dictionary<PrivateI4OccurrenceKeyV1,
            PrivateI4OccurrencePublicLocatorSidecarEntryV1>
            byOccurrence = new();
        HashSet<string> targetLocators = new(StringComparer.Ordinal);
        foreach (PrivateI4OccurrencePublicLocatorSidecarEntryV1 entry in ordered)
        {
            if (!byOccurrence.TryAdd(entry.OccurrenceKey, entry) ||
                !targetLocators.Add(entry.AcceptedI4PublicLocator.Value))
            {
                return false;
            }
        }

        sidecar = new PrivateI4OccurrencePublicLocatorSidecarV1(
            frameInstanceOrdinal,
            acceptedPublicProjectionId,
            ordered,
            byOccurrence);
        return true;
    }
}

internal sealed class PrivateI4OccurrencePublicLocatorSidecarEntryV1
{
    internal PrivateI4OccurrencePublicLocatorSidecarEntryV1(
        byte absoluteController,
        MirrorZoneV1 normalizedZone,
        uint sourceSequence,
        bool isOverlay,
        uint? overlayIndex,
        PublicSemanticLocatorV1 acceptedI4PublicLocator)
    {
        if (absoluteController > 1 ||
            normalizedZone is not (MirrorZoneV1.Hand or MirrorZoneV1.ExtraDeck) ||
            isOverlay != overlayIndex.HasValue)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteController));
        }

        AbsoluteController = absoluteController;
        NormalizedZone = normalizedZone;
        SourceSequence = sourceSequence;
        IsOverlay = isOverlay;
        OverlayIndex = overlayIndex;
        AcceptedI4PublicLocator = acceptedI4PublicLocator ??
            throw new ArgumentNullException(nameof(acceptedI4PublicLocator));
    }

    internal byte AbsoluteController { get; }

    internal MirrorZoneV1 NormalizedZone { get; }

    internal uint SourceSequence { get; }

    internal bool IsOverlay { get; }

    internal uint? OverlayIndex { get; }

    internal PublicSemanticLocatorV1 AcceptedI4PublicLocator { get; }

    internal PrivateI4OccurrenceKeyV1 OccurrenceKey =>
        new(
            AbsoluteController,
            NormalizedZone,
            SourceSequence,
            IsOverlay,
            OverlayIndex);
}

internal readonly record struct PrivateI4OccurrenceKeyV1(
    byte AbsoluteController,
    MirrorZoneV1 NormalizedZone,
    uint SourceSequence,
    bool IsOverlay,
    uint? OverlayIndex);
