using System.Collections.ObjectModel;
using System.Globalization;

namespace OCGForge.Ignis.Gameplay;

public enum FlatPromptFamilyV1 : byte
{
    MsgSelectYesNo = 13,
    MsgSelectOption = 14,
    MsgSelectEffectYn = 12,
    MsgSelectPosition = 19,
    MsgSelectChain = 16
}

public enum FlatPromptChoiceKindV1 : byte
{
    No = 0,
    Yes = 1,
    Option = 2,
    FaceupAttack = 3,
    FacedownAttack = 4,
    FaceupDefense = 5,
    FacedownDefense = 6,
    ChainEntry = 7,
    NoChain = 8
}

public enum FlatPromptSourceSectionV1 : byte
{
    Options = 0,
    ChainChoices = 1
}

public enum FlatPromptErrorCodeV1 : byte
{
    None = 0,
    MalformedPrompt = 1,
    UnsupportedPromptLayout = 2,
    UnprovenPublicReference = 3,
    UnprovenCandidateDomain = 4,
    InvalidI4LocalCandidateKey = 5,
    StalePromptBinding = 6,
    InvalidResponseBinding = 7,
    InvalidParticipant = 8,
    InvalidPositionMask = 9,
    ZeroOptionDomain = 10,
    ArithmeticFailure = 11,
    InvalidLocation = 12,
    InvalidBoolean = 13,
    InvalidClientMode = 14,
    AuthorityMismatch = 15
}

public abstract record FlatPromptPublicContextV1
{
    private const string ContractIdValue =
        "ocgforge-ignis.flat-prompt-projection.v1";

    protected FlatPromptPublicContextV1(
        FlatPromptFamilyV1 promptFamily,
        byte actingPlayer)
    {
        ContractId = ContractIdValue;
        PromptFamily = promptFamily;
        ActingPlayer = actingPlayer;
    }

    public string ContractId { get; }

    public FlatPromptFamilyV1 PromptFamily { get; }

    public byte ActingPlayer { get; }
}

public sealed record FlatPromptYesNoPublicContextV1 : FlatPromptPublicContextV1
{
    internal FlatPromptYesNoPublicContextV1(
        byte actingPlayer,
        ulong yesNoDescriptionId)
        : base(FlatPromptFamilyV1.MsgSelectYesNo, actingPlayer)
    {
        YesNoDescriptionId = yesNoDescriptionId;
    }

    public ulong YesNoDescriptionId { get; }
}

public sealed record FlatPromptOptionPublicContextV1 : FlatPromptPublicContextV1
{
    internal FlatPromptOptionPublicContextV1(byte actingPlayer)
        : base(FlatPromptFamilyV1.MsgSelectOption, actingPlayer)
    {
    }
}

public sealed record FlatPromptPositionPublicContextV1 : FlatPromptPublicContextV1
{
    internal FlatPromptPositionPublicContextV1(
        byte actingPlayer,
        byte positionAllowedPositionsMask)
        : base(FlatPromptFamilyV1.MsgSelectPosition, actingPlayer)
    {
        PositionAllowedPositionsMask = positionAllowedPositionsMask;
    }

    public byte PositionAllowedPositionsMask { get; }
}

public abstract record FlatPromptEffectYnPublicContextBaseV1
    : FlatPromptPublicContextV1
{
    protected FlatPromptEffectYnPublicContextBaseV1(
        byte actingPlayer,
        PublicSemanticLocatorV1 effectCardLocator,
        ulong effectDescriptionId)
        : base(FlatPromptFamilyV1.MsgSelectEffectYn, actingPlayer)
    {
        EffectCardLocator = effectCardLocator ??
            throw new ArgumentNullException(nameof(effectCardLocator));
        EffectDescriptionId = effectDescriptionId;
    }

    public PublicSemanticLocatorV1 EffectCardLocator { get; }

    public ulong EffectDescriptionId { get; }
}

public sealed record FlatPromptEffectYnPublicContextV1
    : FlatPromptEffectYnPublicContextBaseV1
{
    internal FlatPromptEffectYnPublicContextV1(
        byte actingPlayer,
        PublicSemanticLocatorV1 effectCardLocator,
        ulong effectDescriptionId)
        : base(actingPlayer, effectCardLocator, effectDescriptionId)
    {
    }
}

public sealed record FlatPromptEffectYnCardCodePublicContextV1
    : FlatPromptEffectYnPublicContextBaseV1
{
    internal FlatPromptEffectYnCardCodePublicContextV1(
        byte actingPlayer,
        PublicSemanticLocatorV1 effectCardLocator,
        ulong effectDescriptionId,
        uint effectCardCode)
        : base(actingPlayer, effectCardLocator, effectDescriptionId)
    {
        EffectCardCode = effectCardCode;
    }

    public uint EffectCardCode { get; }
}

public sealed record FlatPromptChainPublicContextV1 : FlatPromptPublicContextV1
{
    internal FlatPromptChainPublicContextV1(
        byte actingPlayer,
        byte chainSpeCount,
        bool chainForced,
        uint chainHintTimingForPlayer,
        uint chainHintTimingForOtherPlayer)
        : base(FlatPromptFamilyV1.MsgSelectChain, actingPlayer)
    {
        ChainSpeCount = chainSpeCount;
        ChainForced = chainForced;
        ChainHintTimingForPlayer = chainHintTimingForPlayer;
        ChainHintTimingForOtherPlayer = chainHintTimingForOtherPlayer;
    }

    public byte ChainSpeCount { get; }

    public bool ChainForced { get; }

    public uint ChainHintTimingForPlayer { get; }

    public uint ChainHintTimingForOtherPlayer { get; }
}

public abstract record FlatPublicCandidateDescriptorV1
{
    protected FlatPublicCandidateDescriptorV1(
        string i4LocalCandidateKey,
        FlatPromptChoiceKindV1 choiceKind)
    {
        I4LocalCandidateKey = i4LocalCandidateKey ??
            throw new ArgumentNullException(nameof(i4LocalCandidateKey));
        ChoiceKind = choiceKind;
    }

    public string I4LocalCandidateKey { get; }

    public FlatPromptChoiceKindV1 ChoiceKind { get; }
}

public sealed record FlatYesNoPublicCandidateDescriptorV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatYesNoPublicCandidateDescriptorV1(
        string i4LocalCandidateKey,
        FlatPromptChoiceKindV1 choiceKind)
        : base(i4LocalCandidateKey, choiceKind)
    {
    }
}

public sealed record FlatOptionPublicCandidateDescriptorV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatOptionPublicCandidateDescriptorV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        ulong optionValue)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.Option)
    {
        SourceSection = FlatPromptSourceSectionV1.Options;
        SourceOrdinal = sourceOrdinal;
        OptionValue = optionValue;
    }

    public FlatPromptSourceSectionV1 SourceSection { get; }

    public int SourceOrdinal { get; }

    public ulong OptionValue { get; }
}

public sealed record FlatPositionPublicCandidateDescriptorV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatPositionPublicCandidateDescriptorV1(
        string i4LocalCandidateKey,
        FlatPromptChoiceKindV1 choiceKind,
        byte positionValue)
        : base(i4LocalCandidateKey, choiceKind)
    {
        PositionValue = positionValue;
    }

    public byte PositionValue { get; }
}

public sealed record FlatEffectYnPublicCandidateDescriptorV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatEffectYnPublicCandidateDescriptorV1(
        string i4LocalCandidateKey,
        FlatPromptChoiceKindV1 choiceKind)
        : base(i4LocalCandidateKey, choiceKind)
    {
    }
}

public sealed record FlatChainNoChainPublicCandidateDescriptorV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatChainNoChainPublicCandidateDescriptorV1(
        string i4LocalCandidateKey)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.NoChain)
    {
    }
}

public abstract record FlatChainEntryPublicCandidateDescriptorBaseV1
    : FlatPublicCandidateDescriptorV1
{
    protected FlatChainEntryPublicCandidateDescriptorBaseV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        ulong descriptionOrEffectId,
        byte clientMode)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.ChainEntry)
    {
        SourceSection = FlatPromptSourceSectionV1.ChainChoices;
        SourceOrdinal = sourceOrdinal;
        PublicSemanticCardLocator = publicSemanticCardLocator ??
            throw new ArgumentNullException(nameof(publicSemanticCardLocator));
        DescriptionOrEffectId = descriptionOrEffectId;
        ClientMode = clientMode;
    }

    public FlatPromptSourceSectionV1 SourceSection { get; }

    public int SourceOrdinal { get; }

    public PublicSemanticLocatorV1 PublicSemanticCardLocator { get; }

    public ulong DescriptionOrEffectId { get; }

    public byte ClientMode { get; }
}

public sealed record FlatChainPublicCandidateDescriptorV1
    : FlatChainEntryPublicCandidateDescriptorBaseV1
{
    internal FlatChainPublicCandidateDescriptorV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        ulong descriptionOrEffectId,
        byte clientMode)
        : base(
            i4LocalCandidateKey,
            sourceOrdinal,
            publicSemanticCardLocator,
            descriptionOrEffectId,
            clientMode)
    {
    }
}

public sealed record FlatChainCardCodePublicCandidateDescriptorV1
    : FlatChainEntryPublicCandidateDescriptorBaseV1
{
    internal FlatChainCardCodePublicCandidateDescriptorV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        ulong descriptionOrEffectId,
        byte clientMode,
        uint cardCode)
        : base(
            i4LocalCandidateKey,
            sourceOrdinal,
            publicSemanticCardLocator,
            descriptionOrEffectId,
            clientMode)
    {
        CardCode = cardCode;
    }

    public uint CardCode { get; }
}

public sealed class FlatPromptProjectionResultV1
{
    private FlatPromptProjectionResultV1(
        bool isSuccess,
        FlatPromptErrorCodeV1 error,
        FlatPromptPublicContextV1? context,
        IReadOnlyList<FlatPublicCandidateDescriptorV1>? candidates)
    {
        IsSuccess = isSuccess;
        Error = error;
        Context = context;
        Candidates = candidates;
    }

    public bool IsSuccess { get; }

    public FlatPromptErrorCodeV1 Error { get; }

    public FlatPromptPublicContextV1? Context { get; }

    public IReadOnlyList<FlatPublicCandidateDescriptorV1>? Candidates { get; }

    internal static FlatPromptProjectionResultV1 Success(
        FlatPromptPublicContextV1 context,
        IEnumerable<FlatPublicCandidateDescriptorV1> candidates)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);
        FlatPublicCandidateDescriptorV1[] copy = candidates.ToArray();
        return new FlatPromptProjectionResultV1(
            true,
            FlatPromptErrorCodeV1.None,
            context,
            Array.AsReadOnly(copy));
    }

    internal static FlatPromptProjectionResultV1 Failure(
        FlatPromptErrorCodeV1 error) =>
        new(false, error, null, null);
}

internal sealed class FlatPromptProjectionDraftV1
{
    private readonly FlatPublicCandidateDescriptorV1[] candidates;
    private readonly string[] localKeys;
    private readonly int[] responses;

    internal FlatPromptProjectionDraftV1(
        FlatPromptPublicContextV1 context,
        IEnumerable<FlatPublicCandidateDescriptorV1> candidates,
        IEnumerable<string> localKeys,
        IEnumerable<int> responses)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        this.candidates = candidates?.ToArray() ??
            throw new ArgumentNullException(nameof(candidates));
        this.localKeys = localKeys?.ToArray() ??
            throw new ArgumentNullException(nameof(localKeys));
        this.responses = responses?.ToArray() ??
            throw new ArgumentNullException(nameof(responses));
        if (this.candidates.Length != this.localKeys.Length ||
            this.candidates.Length != this.responses.Length)
        {
            throw new ArgumentException("Projection draft arrays must align.");
        }
    }

    internal FlatPromptPublicContextV1 Context { get; }

    internal int Count => candidates.Length;

    internal FlatPublicCandidateDescriptorV1[] CopyCandidates() =>
        candidates.ToArray();

    internal string[] CopyLocalKeys() => localKeys.ToArray();

    internal int[] CopyResponses() => responses.ToArray();
}

internal abstract record FlatPromptWireDraftV1(
    FlatPromptFamilyV1 Family);

internal sealed record FlatPromptEffectYnWireDraftV1(
    byte ActingPlayer,
    uint SourceCardCode,
    ModernLocInfoV1 SourceLocation,
    ulong EffectDescriptionId)
    : FlatPromptWireDraftV1(FlatPromptFamilyV1.MsgSelectEffectYn);

internal readonly record struct FlatPromptChainWireEntryV1(
    uint SourceCardCode,
    ModernLocInfoV1 SourceLocation,
    ulong DescriptionOrEffectId,
    byte ClientMode);

internal sealed record FlatPromptChainWireDraftV1
    : FlatPromptWireDraftV1
{
    private readonly FlatPromptChainWireEntryV1[] entries;
    private readonly ReadOnlyCollection<FlatPromptChainWireEntryV1> entriesView;

    internal FlatPromptChainWireDraftV1(
        byte actingPlayer,
        byte speCount,
        bool forced,
        uint hintTimingForPlayer,
        uint hintTimingForOtherPlayer,
        FlatPromptChainWireEntryV1[] entries)
        : base(FlatPromptFamilyV1.MsgSelectChain)
    {
        ArgumentNullException.ThrowIfNull(entries);
        this.entries = entries.ToArray();
        entriesView = Array.AsReadOnly(this.entries);
        ActingPlayer = actingPlayer;
        SpeCount = speCount;
        Forced = forced;
        HintTimingForPlayer = hintTimingForPlayer;
        HintTimingForOtherPlayer = hintTimingForOtherPlayer;
    }

    internal byte ActingPlayer { get; }

    internal byte SpeCount { get; }

    internal bool Forced { get; }

    internal uint HintTimingForPlayer { get; }

    internal uint HintTimingForOtherPlayer { get; }

    internal IReadOnlyList<FlatPromptChainWireEntryV1> Entries =>
        entriesView;
}

internal sealed record FlatPromptCardAuthorityContextV1(
    MirrorSnapshotV1 CapturedMirror,
    PublicStateSnapshotV1 AcceptedSnapshot);

internal sealed class CurrentFlatPromptBindingV1
{
    private readonly FlatPublicCandidateDescriptorV1[] candidates;
    private readonly ReadOnlyCollection<FlatPublicCandidateDescriptorV1> candidatesView;
    private readonly string[] localKeys;
    private readonly ReadOnlyCollection<string> localKeysView;
    private readonly Dictionary<string, int> responseByKey;

    private CurrentFlatPromptBindingV1(
        ulong promptInstanceOrdinal,
        FlatPromptFamilyV1 family,
        FlatPublicCandidateDescriptorV1[] candidates,
        string[] localKeys,
        Dictionary<string, int> responseByKey)
    {
        PromptInstanceOrdinal = promptInstanceOrdinal;
        Family = family;
        this.candidates = candidates.ToArray();
        candidatesView = Array.AsReadOnly(this.candidates);
        this.localKeys = localKeys.ToArray();
        localKeysView = Array.AsReadOnly(this.localKeys);
        this.responseByKey = new Dictionary<string, int>(
            responseByKey,
            StringComparer.Ordinal);
    }

    internal ulong PromptInstanceOrdinal { get; }

    internal FlatPromptFamilyV1 Family { get; }

    internal IReadOnlyList<FlatPublicCandidateDescriptorV1> Candidates =>
        candidatesView;

    internal IReadOnlyList<string> LocalKeys => localKeysView;

    internal bool TryGetResponse(string? key, out int response)
    {
        if (string.IsNullOrEmpty(key))
        {
            response = default;
            return false;
        }

        return responseByKey.TryGetValue(key, out response);
    }

    internal static bool TryCreate(
        ulong promptInstanceOrdinal,
        FlatPromptFamilyV1 family,
        FlatPublicCandidateDescriptorV1[]? candidates,
        string[]? localKeys,
        int[]? responses,
        out CurrentFlatPromptBindingV1? binding,
        out FlatPromptErrorCodeV1 error)
    {
        binding = null;
        error = FlatPromptErrorCodeV1.None;
        if (candidates is null || localKeys is null || responses is null ||
            candidates.Length == 0 ||
            candidates.Length != localKeys.Length ||
            candidates.Length != responses.Length)
        {
            error = FlatPromptErrorCodeV1.InvalidResponseBinding;
            return false;
        }

        Dictionary<string, int> responseByKey =
            new(StringComparer.Ordinal);
        for (int i = 0; i < candidates.Length; i++)
        {
            FlatPublicCandidateDescriptorV1? candidate = candidates[i];
            string? key = localKeys[i];
            if (candidate is null ||
                string.IsNullOrEmpty(key) ||
                !string.Equals(
                    candidate.I4LocalCandidateKey,
                    key,
                    StringComparison.Ordinal) ||
                !TryGetExpectedBinding(
                    family,
                    candidate,
                    out string expectedKey,
                    out int expectedResponse) ||
                !string.Equals(key, expectedKey, StringComparison.Ordinal) ||
                responses[i] != expectedResponse ||
                !responseByKey.TryAdd(key, expectedResponse))
            {
                error = FlatPromptErrorCodeV1.InvalidResponseBinding;
                return false;
            }
        }

        binding = new CurrentFlatPromptBindingV1(
            promptInstanceOrdinal,
            family,
            candidates,
            localKeys,
            responseByKey);
        return true;
    }

    private static bool TryGetExpectedBinding(
        FlatPromptFamilyV1 family,
        FlatPublicCandidateDescriptorV1 candidate,
        out string expectedKey,
        out int expectedResponse)
    {
        expectedKey = string.Empty;
        expectedResponse = default;
        switch (family)
        {
            case FlatPromptFamilyV1.MsgSelectYesNo:
                if (candidate is not FlatYesNoPublicCandidateDescriptorV1 yesNo ||
                    yesNo.ChoiceKind is not
                        (FlatPromptChoiceKindV1.No or FlatPromptChoiceKindV1.Yes))
                {
                    return false;
                }

                expectedKey = yesNo.ChoiceKind == FlatPromptChoiceKindV1.No
                    ? FlatPromptKeyV1.YesNoNo
                    : FlatPromptKeyV1.YesNoYes;
                expectedResponse = yesNo.ChoiceKind == FlatPromptChoiceKindV1.No
                    ? 0
                    : 1;
                return true;

            case FlatPromptFamilyV1.MsgSelectOption:
                if (candidate is not FlatOptionPublicCandidateDescriptorV1 option ||
                    option.ChoiceKind != FlatPromptChoiceKindV1.Option ||
                    option.SourceSection != FlatPromptSourceSectionV1.Options ||
                    !FlatPromptKeyV1.TryCreateOption(
                        option.SourceOrdinal,
                        out expectedKey))
                {
                    return false;
                }

                expectedResponse = option.SourceOrdinal;
                return true;

            case FlatPromptFamilyV1.MsgSelectPosition:
                if (candidate is not FlatPositionPublicCandidateDescriptorV1 position ||
                    !FlatPromptKeyV1.TryGetPosition(
                        position.ChoiceKind,
                        position.PositionValue,
                        out expectedKey))
                {
                    return false;
                }

                expectedResponse = position.PositionValue;
                return true;

            case FlatPromptFamilyV1.MsgSelectEffectYn:
                if (candidate is not FlatEffectYnPublicCandidateDescriptorV1 effect ||
                    effect.ChoiceKind is not
                        (FlatPromptChoiceKindV1.No or FlatPromptChoiceKindV1.Yes))
                {
                    return false;
                }

                expectedKey = effect.ChoiceKind == FlatPromptChoiceKindV1.No
                    ? FlatPromptKeyV1.EffectYnNo
                    : FlatPromptKeyV1.EffectYnYes;
                expectedResponse = effect.ChoiceKind == FlatPromptChoiceKindV1.No
                    ? 0
                    : 1;
                return true;

            case FlatPromptFamilyV1.MsgSelectChain:
                if (candidate is FlatChainNoChainPublicCandidateDescriptorV1 noChain)
                {
                    if (noChain.ChoiceKind != FlatPromptChoiceKindV1.NoChain)
                    {
                        return false;
                    }

                    expectedKey = FlatPromptKeyV1.ChainNoChain;
                    expectedResponse = -1;
                    return true;
                }

                if (candidate is not FlatChainEntryPublicCandidateDescriptorBaseV1
                        entry ||
                    (candidate.GetType() !=
                         typeof(FlatChainPublicCandidateDescriptorV1) &&
                     candidate.GetType() !=
                         typeof(FlatChainCardCodePublicCandidateDescriptorV1)) ||
                    entry.ChoiceKind != FlatPromptChoiceKindV1.ChainEntry ||
                    entry.SourceSection != FlatPromptSourceSectionV1.ChainChoices ||
                    entry.ClientMode > 2 ||
                    (entry is FlatChainCardCodePublicCandidateDescriptorV1
                         cardCodeCandidate &&
                     cardCodeCandidate.CardCode == 0) ||
                    !FlatPromptKeyV1.TryCreateChainEntry(
                        entry.SourceOrdinal,
                        out expectedKey))
                {
                    return false;
                }

                expectedResponse = entry.SourceOrdinal;
                return true;

            default:
                return false;
        }
    }
}

internal sealed class FlatPromptSelectionHandleV1
{
    private readonly FlatPublicCandidateDescriptorV1[] orderedDomain;
    private readonly ReadOnlyCollection<FlatPublicCandidateDescriptorV1> orderedDomainView;

    internal FlatPromptSelectionHandleV1(
        ulong promptInstanceOrdinal,
        FlatPromptFamilyV1 family,
        string i4LocalCandidateKey,
        IReadOnlyList<FlatPublicCandidateDescriptorV1> orderedDomain)
    {
        PromptInstanceOrdinal = promptInstanceOrdinal;
        Family = family;
        I4LocalCandidateKey = i4LocalCandidateKey ??
            throw new ArgumentNullException(nameof(i4LocalCandidateKey));
        ArgumentNullException.ThrowIfNull(orderedDomain);
        this.orderedDomain = orderedDomain.ToArray();
        orderedDomainView = Array.AsReadOnly(this.orderedDomain);
    }

    internal ulong PromptInstanceOrdinal { get; }

    internal FlatPromptFamilyV1 Family { get; }

    internal string I4LocalCandidateKey { get; }

    internal IReadOnlyList<FlatPublicCandidateDescriptorV1> OrderedDomain =>
        orderedDomainView;
}

internal readonly record struct FlatPromptResponseResolutionV1(int ResponseI32);

internal static class FlatPromptKeyV1
{
    internal const string YesNoNo = "MSG_SELECT_YESNO:NO";
    internal const string YesNoYes = "MSG_SELECT_YESNO:YES";
    internal const string EffectYnNo = "MSG_SELECT_EFFECTYN:NO";
    internal const string EffectYnYes = "MSG_SELECT_EFFECTYN:YES";
    internal const string ChainNoChain = "MSG_SELECT_CHAIN:NO_CHAIN";

    internal static bool TryCreateOption(
        int sourceOrdinal,
        out string key)
    {
        key = string.Empty;
        if (sourceOrdinal < 0 || sourceOrdinal > 254)
        {
            return false;
        }

        string digits = sourceOrdinal.ToString(CultureInfo.InvariantCulture);
        if (!IsCanonicalAsciiDecimal(digits))
        {
            return false;
        }

        key = "MSG_SELECT_OPTION:OPTION:" + digits;
        return true;
    }

    internal static bool TryCreateChainEntry(
        int sourceOrdinal,
        out string key)
    {
        key = string.Empty;
        if (sourceOrdinal < 0)
        {
            return false;
        }

        string digits = sourceOrdinal.ToString(CultureInfo.InvariantCulture);
        if (!IsCanonicalAsciiDecimal(digits))
        {
            return false;
        }

        key = "MSG_SELECT_CHAIN:CHAIN_ENTRY:" + digits;
        return true;
    }

    internal static bool TryGetPosition(
        FlatPromptChoiceKindV1 choiceKind,
        byte positionValue,
        out string key)
    {
        key = (choiceKind, positionValue) switch
        {
            (FlatPromptChoiceKindV1.FaceupAttack, 0x01) =>
                "MSG_SELECT_POSITION:FACEUP_ATTACK",
            (FlatPromptChoiceKindV1.FacedownAttack, 0x02) =>
                "MSG_SELECT_POSITION:FACEDOWN_ATTACK",
            (FlatPromptChoiceKindV1.FaceupDefense, 0x04) =>
                "MSG_SELECT_POSITION:FACEUP_DEFENSE",
            (FlatPromptChoiceKindV1.FacedownDefense, 0x08) =>
                "MSG_SELECT_POSITION:FACEDOWN_DEFENSE",
            _ => string.Empty
        };
        return key.Length != 0;
    }

    private static bool IsCanonicalAsciiDecimal(string value)
    {
        if (value.Length == 0 ||
            (value.Length > 1 && value[0] == '0'))
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }
}
