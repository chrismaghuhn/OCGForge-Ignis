using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;

namespace OCGForge.Ignis.Gameplay;

public enum FlatPromptFamilyV1 : byte
{
    MsgSelectYesNo = 13,
    MsgSelectOption = 14,
    MsgSelectEffectYn = 12,
    MsgSelectPosition = 19,
    MsgSelectChain = 16,
    MsgSelectBattleCmd = 10,
    MsgSelectIdleCmd = 11
}

internal static class FlatPromptFamilyValueV1
{
    internal const FlatPromptFamilyV1 MsgSelectCard =
        (FlatPromptFamilyV1)15;

    internal const FlatPromptFamilyV1 MsgSelectTribute =
        (FlatPromptFamilyV1)20;

    internal const FlatPromptFamilyV1 MsgSelectUnselectCard =
        (FlatPromptFamilyV1)26;

    internal const FlatPromptFamilyV1 MsgAnnounceNumber =
        (FlatPromptFamilyV1)143;

    internal const FlatPromptFamilyV1 MsgSelectPlace =
        (FlatPromptFamilyV1)18;

    internal const FlatPromptFamilyV1 MsgSelectDisfield =
        (FlatPromptFamilyV1)24;

    internal const FlatPromptFamilyV1 MsgAnnounceRace =
        (FlatPromptFamilyV1)140;

    internal const FlatPromptFamilyV1 MsgAnnounceAttrib =
        (FlatPromptFamilyV1)141;
}

internal static class FlatPromptContractIdV1
{
    internal const string FlatPrompt =
        "ocgforge-ignis.flat-prompt-projection.v1";

    internal const string Combinatorial =
        "ocgforge-ignis.combinatorial-prompt-continuation.v1";
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
    NoChain = 8,
    Activate = 9,
    Attack = 10,
    ToM2 = 11,
    ToEp = 12,
    Summon = 13,
    SpecialSummon = 14,
    Reposition = 15,
    Mset = 16,
    Sset = 17,
    ToBp = 18,
    ShuffleHand = 19,
    Pick = 20,
    Finish = 21,
    Cancel = 22,
    Select = 23,
    Unselect = 24,
    FinishOrCancel = 25,
    NumberOption = 26
}

public enum FlatPromptSourceSectionV1 : byte
{
    Options = 0,
    ChainChoices = 1,
    Activatable = 2,
    Attackable = 3,
    Summon = 4,
    SpecialSummon = 5,
    Reposition = 6,
    Mset = 7,
    Sset = 8,
    Activate = 9,
    SelectCard = 10,
    SelectTribute = 11,
    Selectable = 12,
    Unselectable = 13,
    NumberOptions = 14
}

public enum FlatPromptFieldZoneV1 : byte
{
    MonsterZone = 0,
    SpellTrapZone = 1
}

internal static class FlatPromptMaskValueV1
{
    internal const ulong RaceAllowedMask =
        ((1UL << 33) - 1) | (1UL << 62);

    internal const uint AttributeAllowedMask = 0x7F;

    internal static bool IsRaceBit(int bitIndex) =>
        bitIndex is >= 0 and <= 32 or 62;

    internal static bool IsAttributeBit(int bitIndex) =>
        bitIndex is >= 0 and <= 6;
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
    AuthorityMismatch = 15,
    InvalidContinuationInstance = 16,
    StaleContinuationStep = 17,
    InvalidContinuationAction = 18,
    UnsupportedPromptFamily = 19
}

public abstract record FlatPromptPublicContextV1
{
    protected FlatPromptPublicContextV1(
        FlatPromptFamilyV1 promptFamily,
        byte actingPlayer,
        string contractId = FlatPromptContractIdV1.FlatPrompt)
    {
        ContractId = contractId ??
            throw new ArgumentNullException(nameof(contractId));
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

public sealed record FlatPromptBattlePublicContextV1
    : FlatPromptPublicContextV1
{
    internal FlatPromptBattlePublicContextV1(byte actingPlayer)
        : base(FlatPromptFamilyV1.MsgSelectBattleCmd, actingPlayer)
    {
    }
}

public sealed record FlatPromptIdlePublicContextV1
    : FlatPromptPublicContextV1
{
    internal FlatPromptIdlePublicContextV1(byte actingPlayer)
        : base(FlatPromptFamilyV1.MsgSelectIdleCmd, actingPlayer)
    {
    }
}

public sealed record FlatPromptCardSelectionPublicContextV1
    : FlatPromptPublicContextV1
{
    internal FlatPromptCardSelectionPublicContextV1(
        byte actingPlayer,
        uint minimumCount,
        uint maximumCount,
        bool effectiveCancellation)
        : base(
            FlatPromptFamilyValueV1.MsgSelectCard,
            actingPlayer,
            FlatPromptContractIdV1.Combinatorial)
    {
        MinimumCount = minimumCount;
        MaximumCount = maximumCount;
        EffectiveCancellation = effectiveCancellation;
    }

    public uint MinimumCount { get; }

    public uint MaximumCount { get; }

    public bool EffectiveCancellation { get; }
}

public sealed record FlatPromptTributeSelectionPublicContextV1
    : FlatPromptPublicContextV1
{
    internal FlatPromptTributeSelectionPublicContextV1(
        byte actingPlayer,
        uint minimumRequiredTributeValue,
        uint maximumSelectedCardCount,
        bool effectiveCancellation)
        : base(
            FlatPromptFamilyValueV1.MsgSelectTribute,
            actingPlayer,
            FlatPromptContractIdV1.Combinatorial)
    {
        MinimumRequiredTributeValue = minimumRequiredTributeValue;
        MaximumSelectedCardCount = maximumSelectedCardCount;
        EffectiveCancellation = effectiveCancellation;
    }

    public uint MinimumRequiredTributeValue { get; }

    public uint MaximumSelectedCardCount { get; }

    public bool EffectiveCancellation { get; }
}

public sealed record FlatPromptSelectUnselectCardPublicContextV1
    : FlatPromptPublicContextV1
{
    internal FlatPromptSelectUnselectCardPublicContextV1(
        byte actingPlayer,
        bool finishable,
        bool cancelable,
        uint minimumCount,
        uint maximumCount,
        int selectableCount,
        int unselectableCount)
        : base(
            FlatPromptFamilyValueV1.MsgSelectUnselectCard,
            actingPlayer,
            FlatPromptContractIdV1.Combinatorial)
    {
        Finishable = finishable;
        Cancelable = cancelable;
        MinimumCount = minimumCount;
        MaximumCount = maximumCount;
        SelectableCount = selectableCount;
        UnselectableCount = unselectableCount;
    }

    public bool Finishable { get; }

    public bool Cancelable { get; }

    public uint MinimumCount { get; }

    public uint MaximumCount { get; }

    public int SelectableCount { get; }

    public int UnselectableCount { get; }
}

public sealed record FlatPromptAnnounceNumberPublicContextV1
    : FlatPromptPublicContextV1
{
    internal FlatPromptAnnounceNumberPublicContextV1(
        byte actingPlayer,
        int optionCount)
        : base(
            FlatPromptFamilyValueV1.MsgAnnounceNumber,
            actingPlayer,
            FlatPromptContractIdV1.Combinatorial)
    {
        OptionCount = optionCount;
    }

    public int OptionCount { get; }
}

public sealed record FlatPromptFieldPlaceV1
{
    internal FlatPromptFieldPlaceV1(
        byte absolutePlayer,
        FlatPromptFieldZoneV1 zone,
        byte sequence)
    {
        if (absolutePlayer > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(absolutePlayer));
        }

        if (zone is not
            (FlatPromptFieldZoneV1.MonsterZone or
             FlatPromptFieldZoneV1.SpellTrapZone))
        {
            throw new ArgumentOutOfRangeException(nameof(zone));
        }

        byte maximumSequence = zone == FlatPromptFieldZoneV1.MonsterZone
            ? (byte)6
            : (byte)7;
        if (sequence > maximumSequence)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        AbsolutePlayer = absolutePlayer;
        Zone = zone;
        Sequence = sequence;
    }

    public byte AbsolutePlayer { get; }

    public FlatPromptFieldZoneV1 Zone { get; }

    public byte Sequence { get; }
}

public abstract record FlatPromptPlaceSelectionPublicContextBaseV1
    : FlatPromptPublicContextV1
{
    private readonly FlatPromptFieldPlaceV1[] eligiblePlaces;
    private readonly ReadOnlyCollection<FlatPromptFieldPlaceV1>
        eligiblePlacesView;

    protected FlatPromptPlaceSelectionPublicContextBaseV1(
        FlatPromptFamilyV1 family,
        byte actingPlayer,
        byte requiredPlaceCount,
        IEnumerable<FlatPromptFieldPlaceV1> eligiblePlaces)
        : base(family, actingPlayer, FlatPromptContractIdV1.Combinatorial)
    {
        ArgumentNullException.ThrowIfNull(eligiblePlaces);
        this.eligiblePlaces = eligiblePlaces.ToArray();
        if (this.eligiblePlaces.Length == 0)
        {
            throw new ArgumentException(
                "Eligible place list must not be empty.",
                nameof(eligiblePlaces));
        }

        eligiblePlacesView = Array.AsReadOnly(this.eligiblePlaces);
        RequiredPlaceCount = requiredPlaceCount;
    }

    public byte RequiredPlaceCount { get; }

    public IReadOnlyList<FlatPromptFieldPlaceV1> EligiblePlaces =>
        eligiblePlacesView;
}

public sealed record FlatPromptPlaceSelectionPublicContextV1
    : FlatPromptPlaceSelectionPublicContextBaseV1
{
    internal FlatPromptPlaceSelectionPublicContextV1(
        byte actingPlayer,
        byte requiredPlaceCount,
        IEnumerable<FlatPromptFieldPlaceV1> eligiblePlaces)
        : base(
            FlatPromptFamilyValueV1.MsgSelectPlace,
            actingPlayer,
            requiredPlaceCount,
            eligiblePlaces)
    {
    }
}

public sealed record FlatPromptDisfieldSelectionPublicContextV1
    : FlatPromptPlaceSelectionPublicContextBaseV1
{
    internal FlatPromptDisfieldSelectionPublicContextV1(
        byte actingPlayer,
        byte requiredPlaceCount,
        IEnumerable<FlatPromptFieldPlaceV1> eligiblePlaces)
        : base(
            FlatPromptFamilyValueV1.MsgSelectDisfield,
            actingPlayer,
            requiredPlaceCount,
            eligiblePlaces)
    {
    }
}

public abstract record FlatPromptMaskSelectionPublicContextBaseV1
    : FlatPromptPublicContextV1
{
    protected FlatPromptMaskSelectionPublicContextBaseV1(
        FlatPromptFamilyV1 family,
        byte actingPlayer,
        byte requiredBitCount)
        : base(family, actingPlayer, FlatPromptContractIdV1.Combinatorial)
    {
        RequiredBitCount = requiredBitCount;
    }

    public byte RequiredBitCount { get; }
}

public sealed record FlatPromptRaceSelectionPublicContextV1
    : FlatPromptMaskSelectionPublicContextBaseV1
{
    internal FlatPromptRaceSelectionPublicContextV1(
        byte actingPlayer,
        byte requiredBitCount,
        ulong availableRaceMask)
        : base(
            FlatPromptFamilyValueV1.MsgAnnounceRace,
            actingPlayer,
            requiredBitCount)
    {
        AvailableRaceMask = availableRaceMask;
    }

    public ulong AvailableRaceMask { get; }
}

public sealed record FlatPromptAttributeSelectionPublicContextV1
    : FlatPromptMaskSelectionPublicContextBaseV1
{
    internal FlatPromptAttributeSelectionPublicContextV1(
        byte actingPlayer,
        byte requiredBitCount,
        uint availableAttributeMask)
        : base(
            FlatPromptFamilyValueV1.MsgAnnounceAttrib,
            actingPlayer,
            requiredBitCount)
    {
        AvailableAttributeMask = availableAttributeMask;
    }

    public uint AvailableAttributeMask { get; }
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

public abstract record FlatBattleActivatablePublicCandidateBaseV1
    : FlatPublicCandidateDescriptorV1
{
    protected FlatBattleActivatablePublicCandidateBaseV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        ulong descriptionOrEffectId,
        byte clientMode)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.Activate)
    {
        SourceSection = FlatPromptSourceSectionV1.Activatable;
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

public sealed record FlatBattleActivatablePublicCandidateV1
    : FlatBattleActivatablePublicCandidateBaseV1
{
    internal FlatBattleActivatablePublicCandidateV1(
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

public sealed record FlatBattleActivatableCardCodePublicCandidateV1
    : FlatBattleActivatablePublicCandidateBaseV1
{
    internal FlatBattleActivatableCardCodePublicCandidateV1(
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

public abstract record FlatBattleAttackPublicCandidateBaseV1
    : FlatPublicCandidateDescriptorV1
{
    protected FlatBattleAttackPublicCandidateBaseV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        bool directAttackable)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.Attack)
    {
        SourceSection = FlatPromptSourceSectionV1.Attackable;
        SourceOrdinal = sourceOrdinal;
        PublicSemanticCardLocator = publicSemanticCardLocator ??
            throw new ArgumentNullException(nameof(publicSemanticCardLocator));
        DirectAttackable = directAttackable;
    }

    public FlatPromptSourceSectionV1 SourceSection { get; }

    public int SourceOrdinal { get; }

    public PublicSemanticLocatorV1 PublicSemanticCardLocator { get; }

    public bool DirectAttackable { get; }
}

public sealed record FlatBattleAttackPublicCandidateV1
    : FlatBattleAttackPublicCandidateBaseV1
{
    internal FlatBattleAttackPublicCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        bool directAttackable)
        : base(
            i4LocalCandidateKey,
            sourceOrdinal,
            publicSemanticCardLocator,
            directAttackable)
    {
    }
}

public sealed record FlatBattleAttackCardCodePublicCandidateV1
    : FlatBattleAttackPublicCandidateBaseV1
{
    internal FlatBattleAttackCardCodePublicCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        bool directAttackable,
        uint cardCode)
        : base(
            i4LocalCandidateKey,
            sourceOrdinal,
            publicSemanticCardLocator,
            directAttackable)
    {
        CardCode = cardCode;
    }

    public uint CardCode { get; }
}

public sealed record FlatBattleToMainPhase2PublicCandidateV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatBattleToMainPhase2PublicCandidateV1(
        string i4LocalCandidateKey)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.ToM2)
    {
        TransitionToken = "MAIN_PHASE_2";
    }

    public string TransitionToken { get; }
}

public sealed record FlatBattleToEndPhasePublicCandidateV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatBattleToEndPhasePublicCandidateV1(
        string i4LocalCandidateKey)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.ToEp)
    {
        TransitionToken = "END_PHASE";
    }

    public string TransitionToken { get; }
}

public abstract record FlatIdleCardActionPublicCandidateBaseV1
    : FlatPublicCandidateDescriptorV1
{
    protected FlatIdleCardActionPublicCandidateBaseV1(
        string i4LocalCandidateKey,
        FlatPromptChoiceKindV1 choiceKind,
        FlatPromptSourceSectionV1 sourceSection,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator)
        : base(i4LocalCandidateKey, choiceKind)
    {
        SourceSection = sourceSection;
        SourceOrdinal = sourceOrdinal;
        PublicSemanticCardLocator = publicSemanticCardLocator ??
            throw new ArgumentNullException(nameof(publicSemanticCardLocator));
    }

    public FlatPromptSourceSectionV1 SourceSection { get; }

    public int SourceOrdinal { get; }

    public PublicSemanticLocatorV1 PublicSemanticCardLocator { get; }
}

public abstract record FlatIdleSummonPublicCandidateBaseV1
    : FlatIdleCardActionPublicCandidateBaseV1
{
    protected FlatIdleSummonPublicCandidateBaseV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator)
        : base(
            i4LocalCandidateKey,
            FlatPromptChoiceKindV1.Summon,
            FlatPromptSourceSectionV1.Summon,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
    }
}

public abstract record FlatIdleSpecialSummonPublicCandidateBaseV1
    : FlatIdleCardActionPublicCandidateBaseV1
{
    protected FlatIdleSpecialSummonPublicCandidateBaseV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator)
        : base(
            i4LocalCandidateKey,
            FlatPromptChoiceKindV1.SpecialSummon,
            FlatPromptSourceSectionV1.SpecialSummon,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
    }
}

public abstract record FlatIdleRepositionPublicCandidateBaseV1
    : FlatIdleCardActionPublicCandidateBaseV1
{
    protected FlatIdleRepositionPublicCandidateBaseV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator)
        : base(
            i4LocalCandidateKey,
            FlatPromptChoiceKindV1.Reposition,
            FlatPromptSourceSectionV1.Reposition,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
    }
}

public abstract record FlatIdleMsetPublicCandidateBaseV1
    : FlatIdleCardActionPublicCandidateBaseV1
{
    protected FlatIdleMsetPublicCandidateBaseV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator)
        : base(
            i4LocalCandidateKey,
            FlatPromptChoiceKindV1.Mset,
            FlatPromptSourceSectionV1.Mset,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
    }
}

public abstract record FlatIdleSsetPublicCandidateBaseV1
    : FlatIdleCardActionPublicCandidateBaseV1
{
    protected FlatIdleSsetPublicCandidateBaseV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator)
        : base(
            i4LocalCandidateKey,
            FlatPromptChoiceKindV1.Sset,
            FlatPromptSourceSectionV1.Sset,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
    }
}

public abstract record FlatIdleActivatablePublicCandidateBaseV1
    : FlatPublicCandidateDescriptorV1
{
    protected FlatIdleActivatablePublicCandidateBaseV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        ulong descriptionOrEffectId,
        byte clientMode)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.Activate)
    {
        SourceSection = FlatPromptSourceSectionV1.Activate;
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

public sealed record FlatIdleActivatablePublicCandidateV1
    : FlatIdleActivatablePublicCandidateBaseV1
{
    internal FlatIdleActivatablePublicCandidateV1(
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

public sealed record FlatIdleActivatableCardCodePublicCandidateV1
    : FlatIdleActivatablePublicCandidateBaseV1
{
    internal FlatIdleActivatableCardCodePublicCandidateV1(
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

public sealed record FlatIdleToBattlePhasePublicCandidateV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatIdleToBattlePhasePublicCandidateV1(
        string i4LocalCandidateKey)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.ToBp)
    {
        TransitionToken = "BATTLE_PHASE";
    }

    public string TransitionToken { get; }
}

public sealed record FlatIdleToEndPhasePublicCandidateV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatIdleToEndPhasePublicCandidateV1(
        string i4LocalCandidateKey)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.ToEp)
    {
        TransitionToken = "END_PHASE";
    }

    public string TransitionToken { get; }
}

public sealed record FlatIdleShuffleHandPublicCandidateV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatIdleShuffleHandPublicCandidateV1(
        string i4LocalCandidateKey)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.ShuffleHand)
    {
        TransitionToken = "SHUFFLE_HAND";
    }

    public string TransitionToken { get; }
}

public sealed record FlatIdleSummonPublicCandidateV1
    : FlatIdleSummonPublicCandidateBaseV1
{
    internal FlatIdleSummonPublicCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator)
        : base(
            i4LocalCandidateKey,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
    }
}

public sealed record FlatIdleSummonCardCodePublicCandidateV1
    : FlatIdleSummonPublicCandidateBaseV1
{
    internal FlatIdleSummonCardCodePublicCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        uint cardCode)
        : base(
            i4LocalCandidateKey,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
        CardCode = cardCode;
    }

    public uint CardCode { get; }
}

public sealed record FlatIdleSpecialSummonPublicCandidateV1
    : FlatIdleSpecialSummonPublicCandidateBaseV1
{
    internal FlatIdleSpecialSummonPublicCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator)
        : base(
            i4LocalCandidateKey,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
    }
}

public sealed record FlatIdleSpecialSummonCardCodePublicCandidateV1
    : FlatIdleSpecialSummonPublicCandidateBaseV1
{
    internal FlatIdleSpecialSummonCardCodePublicCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        uint cardCode)
        : base(
            i4LocalCandidateKey,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
        CardCode = cardCode;
    }

    public uint CardCode { get; }
}

public sealed record FlatIdleRepositionPublicCandidateV1
    : FlatIdleRepositionPublicCandidateBaseV1
{
    internal FlatIdleRepositionPublicCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator)
        : base(
            i4LocalCandidateKey,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
    }
}

public sealed record FlatIdleRepositionCardCodePublicCandidateV1
    : FlatIdleRepositionPublicCandidateBaseV1
{
    internal FlatIdleRepositionCardCodePublicCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        uint cardCode)
        : base(
            i4LocalCandidateKey,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
        CardCode = cardCode;
    }

    public uint CardCode { get; }
}

public sealed record FlatIdleMsetPublicCandidateV1
    : FlatIdleMsetPublicCandidateBaseV1
{
    internal FlatIdleMsetPublicCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator)
        : base(
            i4LocalCandidateKey,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
    }
}

public sealed record FlatIdleMsetCardCodePublicCandidateV1
    : FlatIdleMsetPublicCandidateBaseV1
{
    internal FlatIdleMsetCardCodePublicCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        uint cardCode)
        : base(
            i4LocalCandidateKey,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
        CardCode = cardCode;
    }

    public uint CardCode { get; }
}

public sealed record FlatIdleSsetPublicCandidateV1
    : FlatIdleSsetPublicCandidateBaseV1
{
    internal FlatIdleSsetPublicCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator)
        : base(
            i4LocalCandidateKey,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
    }
}

public sealed record FlatIdleSsetCardCodePublicCandidateV1
    : FlatIdleSsetPublicCandidateBaseV1
{
    internal FlatIdleSsetCardCodePublicCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        uint cardCode)
        : base(
            i4LocalCandidateKey,
            sourceOrdinal,
            publicSemanticCardLocator)
    {
        CardCode = cardCode;
    }

    public uint CardCode { get; }
}

internal readonly record struct FlatPromptSelectCardWireEntryV1(
    uint SourceCardCode,
    ModernLocInfoV1 SourceLocation);

internal sealed record FlatPromptSelectCardWireDraftV1
    : FlatPromptWireDraftV1
{
    private readonly FlatPromptSelectCardWireEntryV1[] entries;
    private readonly ReadOnlyCollection<FlatPromptSelectCardWireEntryV1>
        entriesView;

    internal FlatPromptSelectCardWireDraftV1(
        byte actingPlayer,
        bool cancelable,
        uint minimumCount,
        uint maximumCount,
        FlatPromptSelectCardWireEntryV1[] entries)
        : base(FlatPromptFamilyValueV1.MsgSelectCard)
    {
        ArgumentNullException.ThrowIfNull(entries);
        this.entries = entries.ToArray();
        entriesView = Array.AsReadOnly(this.entries);
        ActingPlayer = actingPlayer;
        Cancelable = cancelable;
        MinimumCount = minimumCount;
        MaximumCount = maximumCount;
    }

    internal byte ActingPlayer { get; }

    internal bool Cancelable { get; }

    internal uint MinimumCount { get; }

    internal uint MaximumCount { get; }

    internal IReadOnlyList<FlatPromptSelectCardWireEntryV1> Entries =>
        entriesView;
}

internal readonly record struct FlatPromptSelectTributeWireEntryV1(
    uint SourceCardCode,
    ModernLocInfoV1 SourceLocation,
    byte ReleaseValue);

internal sealed record FlatPromptSelectTributeWireDraftV1
    : FlatPromptWireDraftV1
{
    private readonly FlatPromptSelectTributeWireEntryV1[] entries;
    private readonly ReadOnlyCollection<FlatPromptSelectTributeWireEntryV1>
        entriesView;

    internal FlatPromptSelectTributeWireDraftV1(
        byte actingPlayer,
        bool cancelable,
        uint minimumRequiredTributeValue,
        uint maximumSelectedCardCount,
        FlatPromptSelectTributeWireEntryV1[] entries)
        : base(FlatPromptFamilyValueV1.MsgSelectTribute)
    {
        ArgumentNullException.ThrowIfNull(entries);
        this.entries = entries.ToArray();
        entriesView = Array.AsReadOnly(this.entries);
        ActingPlayer = actingPlayer;
        Cancelable = cancelable;
        MinimumRequiredTributeValue = minimumRequiredTributeValue;
        MaximumSelectedCardCount = maximumSelectedCardCount;
    }

    internal byte ActingPlayer { get; }

    internal bool Cancelable { get; }

    internal uint MinimumRequiredTributeValue { get; }

    internal uint MaximumSelectedCardCount { get; }

    internal IReadOnlyList<FlatPromptSelectTributeWireEntryV1> Entries =>
        entriesView;
}

internal sealed record FlatPromptSelectUnselectWireDraftV1
    : FlatPromptWireDraftV1
{
    private readonly FlatPromptSelectCardWireEntryV1[] selectableEntries;
    private readonly FlatPromptSelectCardWireEntryV1[] unselectableEntries;
    private readonly ReadOnlyCollection<FlatPromptSelectCardWireEntryV1>
        selectableEntriesView;
    private readonly ReadOnlyCollection<FlatPromptSelectCardWireEntryV1>
        unselectableEntriesView;

    internal FlatPromptSelectUnselectWireDraftV1(
        byte actingPlayer,
        bool finishable,
        bool cancelable,
        uint minimumCount,
        uint maximumCount,
        FlatPromptSelectCardWireEntryV1[] selectableEntries,
        FlatPromptSelectCardWireEntryV1[] unselectableEntries)
        : base(FlatPromptFamilyValueV1.MsgSelectUnselectCard)
    {
        ArgumentNullException.ThrowIfNull(selectableEntries);
        ArgumentNullException.ThrowIfNull(unselectableEntries);
        this.selectableEntries = selectableEntries.ToArray();
        this.unselectableEntries = unselectableEntries.ToArray();
        selectableEntriesView = Array.AsReadOnly(this.selectableEntries);
        unselectableEntriesView = Array.AsReadOnly(this.unselectableEntries);
        ActingPlayer = actingPlayer;
        Finishable = finishable;
        Cancelable = cancelable;
        MinimumCount = minimumCount;
        MaximumCount = maximumCount;
    }

    internal byte ActingPlayer { get; }

    internal bool Finishable { get; }

    internal bool Cancelable { get; }

    internal uint MinimumCount { get; }

    internal uint MaximumCount { get; }

    internal IReadOnlyList<FlatPromptSelectCardWireEntryV1>
        SelectableEntries => selectableEntriesView;

    internal IReadOnlyList<FlatPromptSelectCardWireEntryV1>
        UnselectableEntries => unselectableEntriesView;
}

internal sealed record FlatPromptAnnounceNumberWireDraftV1
    : FlatPromptWireDraftV1
{
    private readonly ulong[] values;
    private readonly ReadOnlyCollection<ulong> valuesView;

    internal FlatPromptAnnounceNumberWireDraftV1(
        byte actingPlayer,
        ulong[] values)
        : base(FlatPromptFamilyValueV1.MsgAnnounceNumber)
    {
        ArgumentNullException.ThrowIfNull(values);
        this.values = values.ToArray();
        valuesView = Array.AsReadOnly(this.values);
        ActingPlayer = actingPlayer;
    }

    internal byte ActingPlayer { get; }

    internal IReadOnlyList<ulong> Values => valuesView;
}

internal sealed record FlatPromptPlaceWireDraftV1
    : FlatPromptWireDraftV1
{
    internal FlatPromptPlaceWireDraftV1(
        FlatPromptFamilyV1 family,
        byte actingPlayer,
        byte requiredPlaceCount,
        uint fieldFlag)
        : base(family)
    {
        if (family is not
            (FlatPromptFamilyValueV1.MsgSelectPlace or
             FlatPromptFamilyValueV1.MsgSelectDisfield))
        {
            throw new ArgumentOutOfRangeException(nameof(family));
        }

        ActingPlayer = actingPlayer;
        RequiredPlaceCount = requiredPlaceCount;
        FieldFlag = fieldFlag;
    }

    internal byte ActingPlayer { get; }

    internal byte RequiredPlaceCount { get; }

    internal uint FieldFlag { get; }
}

internal sealed record FlatPromptRaceWireDraftV1(
    byte ActingPlayer,
    byte RequiredBitCount,
    ulong AvailableMask)
    : FlatPromptWireDraftV1(FlatPromptFamilyValueV1.MsgAnnounceRace);

internal sealed record FlatPromptAttributeWireDraftV1(
    byte ActingPlayer,
    byte RequiredBitCount,
    uint AvailableMask)
    : FlatPromptWireDraftV1(FlatPromptFamilyValueV1.MsgAnnounceAttrib);

internal abstract class FlatPromptContinuationStateV1
{
    protected FlatPromptContinuationStateV1(
        FlatPromptFamilyV1 family,
        byte actingPlayer,
        int step)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(step);
        Family = family;
        ActingPlayer = actingPlayer;
        Step = step;
    }

    internal FlatPromptFamilyV1 Family { get; }

    internal byte ActingPlayer { get; }

    internal int Step { get; }
}

internal sealed class FlatPromptCardContinuationStateV1
    : FlatPromptContinuationStateV1
{
    private readonly FlatPublicCandidateDescriptorV1[] sourceCandidates;
    private readonly ReadOnlyCollection<FlatPublicCandidateDescriptorV1>
        sourceCandidatesView;
    private readonly byte[] releaseValues;
    private readonly ReadOnlyCollection<byte> releaseValuesView;
    private readonly int[] selectedOrdinals;
    private readonly ReadOnlyCollection<int> selectedOrdinalsView;

    internal FlatPromptCardContinuationStateV1(
        FlatPromptFamilyV1 family,
        byte actingPlayer,
        uint minimum,
        uint maximum,
        bool cancelable,
        IEnumerable<FlatPublicCandidateDescriptorV1> sourceCandidates,
        IEnumerable<byte> releaseValues,
        IEnumerable<int> selectedOrdinals,
        int step)
        : base(family, actingPlayer, step)
    {
        if (family is not
            (FlatPromptFamilyValueV1.MsgSelectCard or
             FlatPromptFamilyValueV1.MsgSelectTribute))
        {
            throw new ArgumentOutOfRangeException(nameof(family));
        }

        ArgumentNullException.ThrowIfNull(sourceCandidates);
        ArgumentNullException.ThrowIfNull(releaseValues);
        ArgumentNullException.ThrowIfNull(selectedOrdinals);

        this.sourceCandidates = sourceCandidates.ToArray();
        this.releaseValues = releaseValues.ToArray();
        this.selectedOrdinals = selectedOrdinals.ToArray();
        if (this.sourceCandidates.Length == 0 ||
            this.sourceCandidates.Length != this.releaseValues.Length ||
            !AreStrictlyIncreasing(this.selectedOrdinals) ||
            this.selectedOrdinals.Any(
                ordinal => ordinal < 0 || ordinal >= this.sourceCandidates.Length))
        {
            throw new ArgumentException(
                "Continuation state vectors must be complete and aligned.");
        }

        Minimum = minimum;
        Maximum = maximum;
        Cancelable = cancelable;
        sourceCandidatesView = Array.AsReadOnly(this.sourceCandidates);
        releaseValuesView = Array.AsReadOnly(this.releaseValues);
        selectedOrdinalsView = Array.AsReadOnly(this.selectedOrdinals);
    }

    internal uint Minimum { get; }

    internal uint Maximum { get; }

    internal bool Cancelable { get; }

    internal IReadOnlyList<FlatPublicCandidateDescriptorV1> SourceCandidates =>
        sourceCandidatesView;

    internal IReadOnlyList<byte> ReleaseValues => releaseValuesView;

    internal IReadOnlyList<int> SelectedOrdinals => selectedOrdinalsView;

    internal int LastSelectedOrdinal =>
        selectedOrdinals.Length == 0 ? -1 : selectedOrdinals[^1];

    internal uint SelectedTributeValue =>
        selectedOrdinals.Aggregate(
            0u,
            (sum, ordinal) => checked(sum + releaseValues[ordinal]));

    internal bool CanFinish =>
        selectedOrdinals.Length <= Maximum &&
        (Family == FlatPromptFamilyValueV1.MsgSelectCard
            ? (uint)selectedOrdinals.Length >= Minimum
            : SelectedTributeValue >= Minimum);

    internal FlatPromptCardContinuationStateV1 WithSelected(int ordinal) =>
        new(
            Family,
            ActingPlayer,
            Minimum,
            Maximum,
            Cancelable,
            sourceCandidates,
            releaseValues,
            selectedOrdinals.Append(ordinal),
            checked(Step + 1));

    private static bool AreStrictlyIncreasing(int[] values)
    {
        for (int index = 1; index < values.Length; index++)
        {
            if (values[index - 1] >= values[index])
            {
                return false;
            }
        }

        return true;
    }
}

internal readonly record struct FlatPromptPlaceContinuationEntryV1(
    FlatPromptFieldPlacePublicCandidateV1 Candidate,
    int CanonicalIndex);

internal sealed class FlatPromptPlaceContinuationStateV1
    : FlatPromptContinuationStateV1
{
    private readonly FlatPromptPlaceContinuationEntryV1[] sourcePlaces;
    private readonly ReadOnlyCollection<FlatPromptPlaceContinuationEntryV1>
        sourcePlacesView;
    private readonly int[] selectedEntryOrdinals;
    private readonly ReadOnlyCollection<int> selectedEntryOrdinalsView;

    internal FlatPromptPlaceContinuationStateV1(
        FlatPromptFamilyV1 family,
        byte actingPlayer,
        byte requiredPlaceCount,
        IEnumerable<FlatPromptPlaceContinuationEntryV1> sourcePlaces,
        IEnumerable<int> selectedEntryOrdinals,
        int step)
        : base(family, actingPlayer, step)
    {
        if (family is not
            (FlatPromptFamilyValueV1.MsgSelectPlace or
             FlatPromptFamilyValueV1.MsgSelectDisfield))
        {
            throw new ArgumentOutOfRangeException(nameof(family));
        }

        ArgumentNullException.ThrowIfNull(sourcePlaces);
        ArgumentNullException.ThrowIfNull(selectedEntryOrdinals);
        this.sourcePlaces = sourcePlaces.ToArray();
        this.selectedEntryOrdinals = selectedEntryOrdinals.ToArray();
        if (requiredPlaceCount == 0 ||
            this.sourcePlaces.Length == 0 ||
            this.selectedEntryOrdinals.Length > requiredPlaceCount ||
            !AreStrictlyIncreasing(this.sourcePlaces.Select(
                entry => entry.CanonicalIndex).ToArray()) ||
            !AreStrictlyIncreasing(this.selectedEntryOrdinals) ||
            this.sourcePlaces.Any(entry =>
                entry.Candidate is null ||
                entry.Candidate.ChoiceKind != FlatPromptChoiceKindV1.Pick ||
                entry.CanonicalIndex < 0 ||
                entry.CanonicalIndex > 31 ||
                !TryGetCanonicalIndex(
                    actingPlayer,
                    entry.Candidate,
                    out int expectedCanonicalIndex) ||
                expectedCanonicalIndex != entry.CanonicalIndex) ||
            this.selectedEntryOrdinals.Any(ordinal =>
                ordinal < 0 || ordinal >= this.sourcePlaces.Length))
        {
            throw new ArgumentException(
                "Place continuation state vectors must be valid and aligned.");
        }

        RequiredPlaceCount = requiredPlaceCount;
        sourcePlacesView = Array.AsReadOnly(this.sourcePlaces);
        selectedEntryOrdinalsView = Array.AsReadOnly(
            this.selectedEntryOrdinals);
    }

    internal byte RequiredPlaceCount { get; }

    internal IReadOnlyList<FlatPromptPlaceContinuationEntryV1> SourcePlaces =>
        sourcePlacesView;

    internal IReadOnlyList<int> SelectedEntryOrdinals =>
        selectedEntryOrdinalsView;

    internal int LastSelectedEntryOrdinal =>
        selectedEntryOrdinals.Length == 0
            ? -1
            : selectedEntryOrdinals[^1];

    internal int LastSelectedCanonicalIndex =>
        LastSelectedEntryOrdinal < 0
            ? -1
            : sourcePlaces[LastSelectedEntryOrdinal].CanonicalIndex;

    internal bool IsTerminal =>
        selectedEntryOrdinals.Length == RequiredPlaceCount;

    internal bool IsPickLegal(int sourceOrdinal)
    {
        if (sourceOrdinal < 0 ||
            sourceOrdinal >= sourcePlaces.Length ||
            sourceOrdinal <= LastSelectedEntryOrdinal)
        {
            return false;
        }

        int afterCount = selectedEntryOrdinals.Length + 1;
        if (afterCount > RequiredPlaceCount)
        {
            return false;
        }

        int canonicalIndex = sourcePlaces[sourceOrdinal].CanonicalIndex;
        int remaining = RequiredPlaceCount - afterCount;
        int higherEligibleCount = sourcePlaces.Count(entry =>
            entry.CanonicalIndex > canonicalIndex);
        return higherEligibleCount >= remaining;
    }

    internal bool TryGetSourceOrdinal(
        string key,
        out int sourceOrdinal)
    {
        sourceOrdinal = -1;
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        for (int index = 0; index < sourcePlaces.Length; index++)
        {
            if (string.Equals(
                    sourcePlaces[index].Candidate.I4LocalCandidateKey,
                    key,
                    StringComparison.Ordinal))
            {
                sourceOrdinal = index;
                return true;
            }
        }

        return false;
    }

    internal FlatPromptPlaceContinuationStateV1 WithSelected(
        int sourceOrdinal) =>
        new(
            Family,
            ActingPlayer,
            RequiredPlaceCount,
            sourcePlaces,
            selectedEntryOrdinals.Append(sourceOrdinal),
            checked(Step + 1));

    internal FlatPromptFieldPlaceV1[] CopySelectedPlaces() =>
        selectedEntryOrdinals
            .Select(ordinal =>
            {
                FlatPromptFieldPlacePublicCandidateV1 candidate =
                    sourcePlaces[ordinal].Candidate;
                return new FlatPromptFieldPlaceV1(
                    candidate.AbsolutePlayer,
                    candidate.Zone,
                    candidate.Sequence);
            })
            .ToArray();

    private static bool AreStrictlyIncreasing(int[] values)
    {
        for (int index = 1; index < values.Length; index++)
        {
            if (values[index - 1] >= values[index])
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryGetCanonicalIndex(
        byte actingPlayer,
        FlatPromptFieldPlacePublicCandidateV1 candidate,
        out int canonicalIndex)
    {
        canonicalIndex = -1;
        int groupOffset;
        if (candidate.Zone == FlatPromptFieldZoneV1.MonsterZone)
        {
            groupOffset = candidate.AbsolutePlayer == actingPlayer ? 0 : 16;
            if (candidate.Sequence > 6)
            {
                return false;
            }
        }
        else if (candidate.Zone == FlatPromptFieldZoneV1.SpellTrapZone)
        {
            groupOffset = candidate.AbsolutePlayer == actingPlayer ? 8 : 24;
            if (candidate.Sequence > 7)
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        if (candidate.AbsolutePlayer > 1 ||
            candidate.AbsolutePlayer != actingPlayer &&
            candidate.AbsolutePlayer != (byte)(1 - actingPlayer))
        {
            return false;
        }

        canonicalIndex = groupOffset + candidate.Sequence;
        return canonicalIndex is not 7 and not 23;
    }
}

internal sealed class FlatPromptMaskContinuationStateV1
    : FlatPromptContinuationStateV1
{
    private readonly int[] availableBitIndices;
    private readonly ReadOnlyCollection<int> availableBitIndicesView;
    private readonly int[] selectedBitIndices;
    private readonly ReadOnlyCollection<int> selectedBitIndicesView;

    internal FlatPromptMaskContinuationStateV1(
        FlatPromptFamilyV1 family,
        byte actingPlayer,
        byte requiredBitCount,
        ulong availableMask,
        IEnumerable<int> availableBitIndices,
        IEnumerable<int> selectedBitIndices,
        int step)
        : base(family, actingPlayer, step)
    {
        if (family is not
            (FlatPromptFamilyValueV1.MsgAnnounceRace or
             FlatPromptFamilyValueV1.MsgAnnounceAttrib))
        {
            throw new ArgumentOutOfRangeException(nameof(family));
        }

        ArgumentNullException.ThrowIfNull(availableBitIndices);
        ArgumentNullException.ThrowIfNull(selectedBitIndices);
        this.availableBitIndices = availableBitIndices.ToArray();
        this.selectedBitIndices = selectedBitIndices.ToArray();
        ulong allowedMask = family == FlatPromptFamilyValueV1.MsgAnnounceRace
            ? FlatPromptMaskValueV1.RaceAllowedMask
            : FlatPromptMaskValueV1.AttributeAllowedMask;
        if (requiredBitCount == 0 ||
            availableMask == 0 ||
            (availableMask & ~allowedMask) != 0 ||
            this.availableBitIndices.Length !=
                BitOperations.PopCount(availableMask) ||
            !AreStrictlyIncreasing(this.availableBitIndices) ||
            this.availableBitIndices.Any(bitIndex =>
                bitIndex < 0 ||
                bitIndex > 63 ||
                !IsBitInMask(availableMask, bitIndex)) ||
            this.selectedBitIndices.Length > requiredBitCount ||
            !AreStrictlyIncreasing(this.selectedBitIndices) ||
            this.selectedBitIndices.Any(bitIndex =>
                !this.availableBitIndices.Contains(bitIndex)))
        {
            throw new ArgumentException(
                "Mask continuation state vectors must be valid and aligned.");
        }

        RequiredBitCount = requiredBitCount;
        AvailableMask = availableMask;
        availableBitIndicesView = Array.AsReadOnly(this.availableBitIndices);
        selectedBitIndicesView = Array.AsReadOnly(this.selectedBitIndices);
    }

    internal byte RequiredBitCount { get; }

    internal ulong AvailableMask { get; }

    internal IReadOnlyList<int> AvailableBitIndices =>
        availableBitIndicesView;

    internal IReadOnlyList<int> SelectedBitIndices =>
        selectedBitIndicesView;

    internal int LastSelectedBitIndex =>
        selectedBitIndices.Length == 0 ? -1 : selectedBitIndices[^1];

    internal bool IsTerminal =>
        selectedBitIndices.Length == RequiredBitCount;

    internal ulong SelectedMask => selectedBitIndices.Aggregate(
        0UL,
        (mask, bitIndex) => mask | (1UL << bitIndex));

    internal bool IsPickLegal(int bitIndex)
    {
        if (!availableBitIndices.Contains(bitIndex) ||
            bitIndex <= LastSelectedBitIndex)
        {
            return false;
        }

        int afterCount = selectedBitIndices.Length + 1;
        if (afterCount > RequiredBitCount)
        {
            return false;
        }

        int remaining = RequiredBitCount - afterCount;
        int higherAvailableCount = availableBitIndices.Count(
            available => available > bitIndex);
        return higherAvailableCount >= remaining;
    }

    internal FlatPromptMaskContinuationStateV1 WithSelected(int bitIndex) =>
        new(
            Family,
            ActingPlayer,
            RequiredBitCount,
            AvailableMask,
            availableBitIndices,
            selectedBitIndices.Append(bitIndex),
            checked(Step + 1));

    private static bool IsBitInMask(ulong mask, int bitIndex) =>
        (mask & (1UL << bitIndex)) != 0;

    private static bool AreStrictlyIncreasing(int[] values)
    {
        for (int index = 1; index < values.Length; index++)
        {
            if (values[index - 1] >= values[index])
            {
                return false;
            }
        }

        return true;
    }
}

internal sealed class FlatPromptContinuationStepResultV1
{
    private readonly ReadOnlyCollection<byte> terminalResponseBodyView;

    private FlatPromptContinuationStepResultV1(
        bool isSuccess,
        FlatPromptErrorCodeV1 error,
        FlatPromptProjectionResultV1? projection,
        byte[]? terminalResponseBody,
        bool isTerminal)
    {
        IsSuccess = isSuccess;
        Error = error;
        Projection = projection;
        byte[] responseCopy = terminalResponseBody is null
            ? Array.Empty<byte>()
            : terminalResponseBody.ToArray();
        terminalResponseBodyView = Array.AsReadOnly(responseCopy);
        TerminalResponseBody = terminalResponseBodyView;
        IsTerminal = isTerminal;
    }

    internal bool IsSuccess { get; }

    internal FlatPromptErrorCodeV1 Error { get; }

    internal FlatPromptProjectionResultV1? Projection { get; }

    internal IReadOnlyList<byte> TerminalResponseBody { get; }

    internal bool IsTerminal { get; }

    internal static FlatPromptContinuationStepResultV1 Intermediate(
        FlatPromptProjectionResultV1 projection) =>
        new(
            true,
            FlatPromptErrorCodeV1.None,
            projection ?? throw new ArgumentNullException(nameof(projection)),
            null,
            false);

    internal static FlatPromptContinuationStepResultV1 Terminal(
        byte[] responseBody) =>
        new(
            true,
            FlatPromptErrorCodeV1.None,
            null,
            responseBody ?? throw new ArgumentNullException(nameof(responseBody)),
            true);

    internal static FlatPromptContinuationStepResultV1 Failure(
        FlatPromptErrorCodeV1 error) =>
        new(false, error, null, null, false);
}

public abstract record FlatPromptCardSelectionCandidateBaseV1
    : FlatPublicCandidateDescriptorV1
{
    protected FlatPromptCardSelectionCandidateBaseV1(
        string i4LocalCandidateKey,
        int sourceOrdinal)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.Pick)
    {
        SourceSection = FlatPromptSourceSectionV1.SelectCard;
        SourceOrdinal = sourceOrdinal;
    }

    public FlatPromptSourceSectionV1 SourceSection { get; }

    public int SourceOrdinal { get; }
}

public sealed record FlatPromptCardSelectionAnonymousCandidateV1
    : FlatPromptCardSelectionCandidateBaseV1
{
    internal FlatPromptCardSelectionAnonymousCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal)
        : base(i4LocalCandidateKey, sourceOrdinal)
    {
    }
}

public sealed record FlatPromptCardSelectionPromptCodeCandidateV1
    : FlatPromptCardSelectionCandidateBaseV1
{
    internal FlatPromptCardSelectionPromptCodeCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        uint promptLocalCardCode)
        : base(i4LocalCandidateKey, sourceOrdinal)
    {
        PromptLocalCardCode = promptLocalCardCode;
    }

    public uint PromptLocalCardCode { get; }
}

public sealed record FlatPromptCardSelectionLocatorCandidateV1
    : FlatPromptCardSelectionCandidateBaseV1
{
    internal FlatPromptCardSelectionLocatorCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator)
        : base(i4LocalCandidateKey, sourceOrdinal)
    {
        PublicSemanticCardLocator = publicSemanticCardLocator ??
            throw new ArgumentNullException(nameof(publicSemanticCardLocator));
    }

    public PublicSemanticLocatorV1 PublicSemanticCardLocator { get; }
}

public sealed record FlatPromptCardSelectionLocatorPromptCodeCandidateV1
    : FlatPromptCardSelectionCandidateBaseV1
{
    internal FlatPromptCardSelectionLocatorPromptCodeCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        uint promptLocalCardCode)
        : base(i4LocalCandidateKey, sourceOrdinal)
    {
        PublicSemanticCardLocator = publicSemanticCardLocator ??
            throw new ArgumentNullException(nameof(publicSemanticCardLocator));
        PromptLocalCardCode = promptLocalCardCode;
    }

    public PublicSemanticLocatorV1 PublicSemanticCardLocator { get; }

    public uint PromptLocalCardCode { get; }
}

public abstract record FlatPromptTributeSelectionCandidateBaseV1
    : FlatPublicCandidateDescriptorV1
{
    protected FlatPromptTributeSelectionCandidateBaseV1(
        string i4LocalCandidateKey,
        int sourceOrdinal)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.Pick)
    {
        SourceSection = FlatPromptSourceSectionV1.SelectTribute;
        SourceOrdinal = sourceOrdinal;
    }

    public FlatPromptSourceSectionV1 SourceSection { get; }

    public int SourceOrdinal { get; }
}

public sealed record FlatPromptTributeSelectionAnonymousCandidateV1
    : FlatPromptTributeSelectionCandidateBaseV1
{
    internal FlatPromptTributeSelectionAnonymousCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal)
        : base(i4LocalCandidateKey, sourceOrdinal)
    {
    }
}

public sealed record FlatPromptTributeSelectionPromptCodeCandidateV1
    : FlatPromptTributeSelectionCandidateBaseV1
{
    internal FlatPromptTributeSelectionPromptCodeCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        uint promptLocalCardCode)
        : base(i4LocalCandidateKey, sourceOrdinal)
    {
        PromptLocalCardCode = promptLocalCardCode;
    }

    public uint PromptLocalCardCode { get; }
}

public sealed record FlatPromptTributeSelectionLocatorCandidateV1
    : FlatPromptTributeSelectionCandidateBaseV1
{
    internal FlatPromptTributeSelectionLocatorCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator)
        : base(i4LocalCandidateKey, sourceOrdinal)
    {
        PublicSemanticCardLocator = publicSemanticCardLocator ??
            throw new ArgumentNullException(nameof(publicSemanticCardLocator));
    }

    public PublicSemanticLocatorV1 PublicSemanticCardLocator { get; }
}

public sealed record FlatPromptTributeSelectionLocatorPromptCodeCandidateV1
    : FlatPromptTributeSelectionCandidateBaseV1
{
    internal FlatPromptTributeSelectionLocatorPromptCodeCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        uint promptLocalCardCode)
        : base(i4LocalCandidateKey, sourceOrdinal)
    {
        PublicSemanticCardLocator = publicSemanticCardLocator ??
            throw new ArgumentNullException(nameof(publicSemanticCardLocator));
        PromptLocalCardCode = promptLocalCardCode;
    }

    public PublicSemanticLocatorV1 PublicSemanticCardLocator { get; }

    public uint PromptLocalCardCode { get; }
}

public sealed record FlatPromptFinishPublicCandidateV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatPromptFinishPublicCandidateV1(string i4LocalCandidateKey)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.Finish)
    {
    }
}

public sealed record FlatPromptCancelPublicCandidateV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatPromptCancelPublicCandidateV1(string i4LocalCandidateKey)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.Cancel)
    {
    }
}

public abstract record FlatPromptSelectUnselectCardCandidateBaseV1
    : FlatPublicCandidateDescriptorV1
{
    protected FlatPromptSelectUnselectCardCandidateBaseV1(
        string i4LocalCandidateKey,
        FlatPromptChoiceKindV1 choiceKind,
        FlatPromptSourceSectionV1 sourceSection,
        int sourceOrdinal)
        : base(i4LocalCandidateKey, choiceKind)
    {
        SourceSection = sourceSection;
        SourceOrdinal = sourceOrdinal;
    }

    public FlatPromptSourceSectionV1 SourceSection { get; }

    public int SourceOrdinal { get; }
}

public sealed record FlatPromptSelectUnselectAnonymousCandidateV1
    : FlatPromptSelectUnselectCardCandidateBaseV1
{
    internal FlatPromptSelectUnselectAnonymousCandidateV1(
        string i4LocalCandidateKey,
        FlatPromptChoiceKindV1 choiceKind,
        FlatPromptSourceSectionV1 sourceSection,
        int sourceOrdinal)
        : base(
            i4LocalCandidateKey,
            choiceKind,
            sourceSection,
            sourceOrdinal)
    {
    }
}

public sealed record FlatPromptSelectUnselectPromptCodeCandidateV1
    : FlatPromptSelectUnselectCardCandidateBaseV1
{
    internal FlatPromptSelectUnselectPromptCodeCandidateV1(
        string i4LocalCandidateKey,
        FlatPromptChoiceKindV1 choiceKind,
        FlatPromptSourceSectionV1 sourceSection,
        int sourceOrdinal,
        uint promptLocalCardCode)
        : base(
            i4LocalCandidateKey,
            choiceKind,
            sourceSection,
            sourceOrdinal)
    {
        PromptLocalCardCode = promptLocalCardCode;
    }

    public uint PromptLocalCardCode { get; }
}

public sealed record FlatPromptSelectUnselectLocatorCandidateV1
    : FlatPromptSelectUnselectCardCandidateBaseV1
{
    internal FlatPromptSelectUnselectLocatorCandidateV1(
        string i4LocalCandidateKey,
        FlatPromptChoiceKindV1 choiceKind,
        FlatPromptSourceSectionV1 sourceSection,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator)
        : base(
            i4LocalCandidateKey,
            choiceKind,
            sourceSection,
            sourceOrdinal)
    {
        PublicSemanticCardLocator = publicSemanticCardLocator ??
            throw new ArgumentNullException(nameof(publicSemanticCardLocator));
    }

    public PublicSemanticLocatorV1 PublicSemanticCardLocator { get; }
}

public sealed record FlatPromptSelectUnselectLocatorPromptCodeCandidateV1
    : FlatPromptSelectUnselectCardCandidateBaseV1
{
    internal FlatPromptSelectUnselectLocatorPromptCodeCandidateV1(
        string i4LocalCandidateKey,
        FlatPromptChoiceKindV1 choiceKind,
        FlatPromptSourceSectionV1 sourceSection,
        int sourceOrdinal,
        PublicSemanticLocatorV1 publicSemanticCardLocator,
        uint promptLocalCardCode)
        : base(
            i4LocalCandidateKey,
            choiceKind,
            sourceSection,
            sourceOrdinal)
    {
        PublicSemanticCardLocator = publicSemanticCardLocator ??
            throw new ArgumentNullException(nameof(publicSemanticCardLocator));
        PromptLocalCardCode = promptLocalCardCode;
    }

    public PublicSemanticLocatorV1 PublicSemanticCardLocator { get; }

    public uint PromptLocalCardCode { get; }
}

public sealed record FlatPromptFinishOrCancelPublicCandidateV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatPromptFinishOrCancelPublicCandidateV1(
        string i4LocalCandidateKey)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.FinishOrCancel)
    {
    }
}

public sealed record FlatPromptAnnounceNumberPublicCandidateV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatPromptAnnounceNumberPublicCandidateV1(
        string i4LocalCandidateKey,
        int sourceOrdinal,
        ulong numberValue)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.NumberOption)
    {
        SourceSection = FlatPromptSourceSectionV1.NumberOptions;
        SourceOrdinal = sourceOrdinal;
        NumberValue = numberValue;
    }

    public FlatPromptSourceSectionV1 SourceSection { get; }

    public int SourceOrdinal { get; }

    public ulong NumberValue { get; }
}

public sealed record FlatPromptFieldPlacePublicCandidateV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatPromptFieldPlacePublicCandidateV1(
        string i4LocalCandidateKey,
        byte absolutePlayer,
        FlatPromptFieldZoneV1 zone,
        byte sequence)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.Pick)
    {
        AbsolutePlayer = absolutePlayer;
        Zone = zone;
        Sequence = sequence;
    }

    public byte AbsolutePlayer { get; }

    public FlatPromptFieldZoneV1 Zone { get; }

    public byte Sequence { get; }
}

public sealed record FlatPromptMaskBitPublicCandidateV1
    : FlatPublicCandidateDescriptorV1
{
    internal FlatPromptMaskBitPublicCandidateV1(
        string i4LocalCandidateKey,
        int bitIndex,
        ulong bitValue)
        : base(i4LocalCandidateKey, FlatPromptChoiceKindV1.Pick)
    {
        BitIndex = bitIndex;
        BitValue = bitValue;
    }

    public int BitIndex { get; }

    public ulong BitValue { get; }
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
    private readonly byte[][]? responseBodies;

    internal FlatPromptProjectionDraftV1(
        FlatPromptPublicContextV1 context,
        IEnumerable<FlatPublicCandidateDescriptorV1> candidates,
        IEnumerable<string> localKeys,
        IEnumerable<int> responses,
        FlatPromptContinuationStateV1? continuationState = null,
        IEnumerable<byte[]>? responseBodies = null)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        this.candidates = candidates?.ToArray() ??
            throw new ArgumentNullException(nameof(candidates));
        this.localKeys = localKeys?.ToArray() ??
            throw new ArgumentNullException(nameof(localKeys));
        this.responses = responses?.ToArray() ??
            throw new ArgumentNullException(nameof(responses));
        this.responseBodies = responseBodies is null
            ? null
            : responseBodies.Select(
                body => body?.ToArray() ??
                    throw new ArgumentException(
                        "Response bodies must not contain null."))
                .ToArray();
        if (this.candidates.Length != this.localKeys.Length ||
            this.candidates.Length != this.responses.Length ||
            (this.responseBodies is not null &&
             this.candidates.Length != this.responseBodies.Length))
        {
            throw new ArgumentException("Projection draft arrays must align.");
        }

        ContinuationState = continuationState;
    }

    internal FlatPromptPublicContextV1 Context { get; }

    internal int Count => candidates.Length;

    internal FlatPublicCandidateDescriptorV1[] CopyCandidates() =>
        candidates.ToArray();

    internal string[] CopyLocalKeys() => localKeys.ToArray();

    internal int[] CopyResponses() => responses.ToArray();

    internal byte[][]? CopyResponseBodies() =>
        responseBodies?.Select(body => body.ToArray()).ToArray();

    internal FlatPromptContinuationStateV1? ContinuationState { get; }
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

internal readonly record struct FlatPromptBattleActivatableWireEntryV1(
    uint SourceCardCode,
    byte Controller,
    byte Location,
    uint Sequence,
    ulong DescriptionOrEffectId,
    byte ClientMode);

internal readonly record struct FlatPromptBattleAttackableWireEntryV1(
    uint SourceCardCode,
    byte Controller,
    byte Location,
    byte Sequence,
    bool DirectAttackable);

internal sealed record FlatPromptBattleWireDraftV1
    : FlatPromptWireDraftV1
{
    private readonly FlatPromptBattleActivatableWireEntryV1[] activatableEntries;
    private readonly FlatPromptBattleAttackableWireEntryV1[] attackableEntries;
    private readonly ReadOnlyCollection<FlatPromptBattleActivatableWireEntryV1>
        activatableEntriesView;
    private readonly ReadOnlyCollection<FlatPromptBattleAttackableWireEntryV1>
        attackableEntriesView;

    internal FlatPromptBattleWireDraftV1(
        byte actingPlayer,
        FlatPromptBattleActivatableWireEntryV1[] activatableEntries,
        FlatPromptBattleAttackableWireEntryV1[] attackableEntries,
        bool toMainPhase2,
        bool toEndPhase)
        : base(FlatPromptFamilyV1.MsgSelectBattleCmd)
    {
        ArgumentNullException.ThrowIfNull(activatableEntries);
        ArgumentNullException.ThrowIfNull(attackableEntries);
        this.activatableEntries = activatableEntries.ToArray();
        this.attackableEntries = attackableEntries.ToArray();
        activatableEntriesView = Array.AsReadOnly(this.activatableEntries);
        attackableEntriesView = Array.AsReadOnly(this.attackableEntries);
        ActingPlayer = actingPlayer;
        ToMainPhase2 = toMainPhase2;
        ToEndPhase = toEndPhase;
    }

    internal byte ActingPlayer { get; }

    internal IReadOnlyList<FlatPromptBattleActivatableWireEntryV1>
        ActivatableEntries => activatableEntriesView;

    internal IReadOnlyList<FlatPromptBattleAttackableWireEntryV1>
        AttackableEntries => attackableEntriesView;

    internal bool ToMainPhase2 { get; }

    internal bool ToEndPhase { get; }
}

internal readonly record struct FlatPromptIdleCardWireEntryV1(
    uint SourceCardCode,
    byte Controller,
    byte Location,
    uint Sequence);

internal readonly record struct FlatPromptIdleRepositionWireEntryV1(
    uint SourceCardCode,
    byte Controller,
    byte Location,
    byte Sequence);

internal readonly record struct FlatPromptIdleActivatableWireEntryV1(
    uint SourceCardCode,
    byte Controller,
    byte Location,
    uint Sequence,
    ulong DescriptionOrEffectId,
    byte ClientMode);

internal sealed record FlatPromptIdleWireDraftV1
    : FlatPromptWireDraftV1
{
    private readonly FlatPromptIdleCardWireEntryV1[] summonEntries;
    private readonly FlatPromptIdleCardWireEntryV1[] specialSummonEntries;
    private readonly FlatPromptIdleRepositionWireEntryV1[] repositionEntries;
    private readonly FlatPromptIdleCardWireEntryV1[] monsterSetEntries;
    private readonly FlatPromptIdleCardWireEntryV1[] spellTrapSetEntries;
    private readonly FlatPromptIdleActivatableWireEntryV1[] activatableEntries;
    private readonly ReadOnlyCollection<FlatPromptIdleCardWireEntryV1>
        summonEntriesView;
    private readonly ReadOnlyCollection<FlatPromptIdleCardWireEntryV1>
        specialSummonEntriesView;
    private readonly ReadOnlyCollection<FlatPromptIdleRepositionWireEntryV1>
        repositionEntriesView;
    private readonly ReadOnlyCollection<FlatPromptIdleCardWireEntryV1>
        monsterSetEntriesView;
    private readonly ReadOnlyCollection<FlatPromptIdleCardWireEntryV1>
        spellTrapSetEntriesView;
    private readonly ReadOnlyCollection<FlatPromptIdleActivatableWireEntryV1>
        activatableEntriesView;

    internal FlatPromptIdleWireDraftV1(
        byte actingPlayer,
        FlatPromptIdleCardWireEntryV1[] summonEntries,
        FlatPromptIdleCardWireEntryV1[] specialSummonEntries,
        FlatPromptIdleRepositionWireEntryV1[] repositionEntries,
        FlatPromptIdleCardWireEntryV1[] monsterSetEntries,
        FlatPromptIdleCardWireEntryV1[] spellTrapSetEntries,
        FlatPromptIdleActivatableWireEntryV1[] activatableEntries,
        bool toBattlePhase,
        bool toEndPhase,
        bool shuffleHand)
        : base(FlatPromptFamilyV1.MsgSelectIdleCmd)
    {
        ArgumentNullException.ThrowIfNull(summonEntries);
        ArgumentNullException.ThrowIfNull(specialSummonEntries);
        ArgumentNullException.ThrowIfNull(repositionEntries);
        ArgumentNullException.ThrowIfNull(monsterSetEntries);
        ArgumentNullException.ThrowIfNull(spellTrapSetEntries);
        ArgumentNullException.ThrowIfNull(activatableEntries);
        this.summonEntries = summonEntries.ToArray();
        this.specialSummonEntries = specialSummonEntries.ToArray();
        this.repositionEntries = repositionEntries.ToArray();
        this.monsterSetEntries = monsterSetEntries.ToArray();
        this.spellTrapSetEntries = spellTrapSetEntries.ToArray();
        this.activatableEntries = activatableEntries.ToArray();
        summonEntriesView = Array.AsReadOnly(this.summonEntries);
        specialSummonEntriesView = Array.AsReadOnly(this.specialSummonEntries);
        repositionEntriesView = Array.AsReadOnly(this.repositionEntries);
        monsterSetEntriesView = Array.AsReadOnly(this.monsterSetEntries);
        spellTrapSetEntriesView = Array.AsReadOnly(this.spellTrapSetEntries);
        activatableEntriesView = Array.AsReadOnly(this.activatableEntries);
        ActingPlayer = actingPlayer;
        ToBattlePhase = toBattlePhase;
        ToEndPhase = toEndPhase;
        ShuffleHand = shuffleHand;
    }

    internal byte ActingPlayer { get; }

    internal IReadOnlyList<FlatPromptIdleCardWireEntryV1> SummonEntries =>
        summonEntriesView;

    internal IReadOnlyList<FlatPromptIdleCardWireEntryV1>
        SpecialSummonEntries => specialSummonEntriesView;

    internal IReadOnlyList<FlatPromptIdleRepositionWireEntryV1>
        RepositionEntries => repositionEntriesView;

    internal IReadOnlyList<FlatPromptIdleCardWireEntryV1> MonsterSetEntries =>
        monsterSetEntriesView;

    internal IReadOnlyList<FlatPromptIdleCardWireEntryV1>
        SpellTrapSetEntries => spellTrapSetEntriesView;

    internal IReadOnlyList<FlatPromptIdleActivatableWireEntryV1>
        ActivatableEntries => activatableEntriesView;

    internal bool ToBattlePhase { get; }

    internal bool ToEndPhase { get; }

    internal bool ShuffleHand { get; }
}

internal sealed class CurrentFlatPromptBindingV1
{
    private readonly FlatPublicCandidateDescriptorV1[] candidates;
    private readonly ReadOnlyCollection<FlatPublicCandidateDescriptorV1> candidatesView;
    private readonly string[] localKeys;
    private readonly ReadOnlyCollection<string> localKeysView;
    private readonly Dictionary<string, int> responseByKey;
    private readonly Dictionary<string, byte[]> responseBodyByKey;

    private CurrentFlatPromptBindingV1(
        ulong promptInstanceOrdinal,
        FlatPromptFamilyV1 family,
        FlatPublicCandidateDescriptorV1[] candidates,
        string[] localKeys,
        Dictionary<string, int> responseByKey,
        byte[][]? responseBodies,
        FlatPromptContinuationStateV1? continuationState)
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
        responseBodyByKey = new Dictionary<string, byte[]>(
            StringComparer.Ordinal);
        if (responseBodies is not null)
        {
            for (int index = 0; index < responseBodies.Length; index++)
            {
                responseBodyByKey.Add(
                    this.localKeys[index],
                    responseBodies[index].ToArray());
            }
        }
        ContinuationState = continuationState;
        ContinuationStep = continuationState?.Step ?? 0;
    }

    internal ulong PromptInstanceOrdinal { get; }

    internal FlatPromptFamilyV1 Family { get; }

    internal int ContinuationStep { get; }

    internal FlatPromptContinuationStateV1? ContinuationState { get; }

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

    internal bool TryGetCandidate(
        string? key,
        out FlatPublicCandidateDescriptorV1? candidate)
    {
        candidate = null;
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        for (int index = 0; index < localKeys.Length; index++)
        {
            if (string.Equals(
                    localKeys[index],
                    key,
                    StringComparison.Ordinal))
            {
                candidate = candidates[index];
                return true;
            }
        }

        return false;
    }

    internal bool TryGetResponseBody(
        string? key,
        out byte[] responseBody)
    {
        responseBody = Array.Empty<byte>();
        if (string.IsNullOrEmpty(key) ||
            !responseBodyByKey.TryGetValue(key, out byte[]? stored))
        {
            return false;
        }

        responseBody = stored.ToArray();
        return true;
    }

    internal static bool TryCreate(
        ulong promptInstanceOrdinal,
        FlatPromptFamilyV1 family,
        FlatPublicCandidateDescriptorV1[]? candidates,
        string[]? localKeys,
        int[]? responses,
        out CurrentFlatPromptBindingV1? binding,
        out FlatPromptErrorCodeV1 error,
        byte[][]? responseBodies = null,
        FlatPromptContinuationStateV1? continuationState = null)
    {
        binding = null;
        error = FlatPromptErrorCodeV1.None;
        if (candidates is null || localKeys is null || responses is null ||
            candidates.Length == 0 ||
            candidates.Length != localKeys.Length ||
            candidates.Length != responses.Length ||
            (responseBodies is not null &&
             candidates.Length != responseBodies.Length) ||
            (responseBodies is not null &&
             responseBodies.Any(body => body is null || body.Length == 0)) ||
            (continuationState is not null &&
             !IsContinuationStateCompatible(
                 family,
                 candidates,
                 continuationState)))
        {
            error = FlatPromptErrorCodeV1.InvalidResponseBinding;
            return false;
        }

        Dictionary<string, int> responseByKey =
            new(StringComparer.Ordinal);
        int selectableCount = candidates.Count(candidate =>
            candidate is FlatPromptSelectUnselectCardCandidateBaseV1
                selectUnselect &&
            selectUnselect.SourceSection ==
                FlatPromptSourceSectionV1.Selectable);
        for (int i = 0; i < candidates.Length; i++)
        {
            FlatPublicCandidateDescriptorV1? candidate = candidates[i];
            string? key = localKeys[i];
            if (candidate is null || string.IsNullOrEmpty(key) ||
                !string.Equals(
                    candidate.I4LocalCandidateKey,
                    key,
                    StringComparison.Ordinal) ||
                !TryGetExpectedBinding(
                    family,
                    candidate,
                    selectableCount,
                    out string expectedKey,
                    out int expectedResponse) ||
                !string.Equals(key, expectedKey, StringComparison.Ordinal) ||
                responses[i] != expectedResponse)
            {
                error = FlatPromptErrorCodeV1.InvalidResponseBinding;
                return false;
            }

            if (responseBodies is not null &&
                !TryValidateResponseBody(
                    family,
                    candidate,
                    selectableCount,
                    expectedResponse,
                    responseBodies[i]))
            {
                error = FlatPromptErrorCodeV1.InvalidResponseBinding;
                return false;
            }

            if (!responseByKey.TryAdd(key!, expectedResponse))
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
            responseByKey,
            responseBodies,
            continuationState);
        return true;
    }

    private static bool IsContinuationStateCompatible(
        FlatPromptFamilyV1 family,
        FlatPublicCandidateDescriptorV1[] candidates,
        FlatPromptContinuationStateV1 state)
    {
        switch (state)
        {
            case FlatPromptCardContinuationStateV1 card:
                return card.Family == family &&
                    family is
                    (FlatPromptFamilyValueV1.MsgSelectCard or
                     FlatPromptFamilyValueV1.MsgSelectTribute) &&
                    card.SourceCandidates.Count > 0;

            case FlatPromptPlaceContinuationStateV1 place:
                if (place.Family != family ||
                    (family is not
                        (FlatPromptFamilyValueV1.MsgSelectPlace or
                         FlatPromptFamilyValueV1.MsgSelectDisfield)) ||
                    place.SourcePlaces.Count == 0)
                {
                    return false;
                }

                return candidates.All(candidate =>
                    candidate is FlatPromptFieldPlacePublicCandidateV1
                        placeCandidate &&
                    place.TryGetSourceOrdinal(
                        placeCandidate.I4LocalCandidateKey,
                        out int sourceOrdinal) &&
                    place.IsPickLegal(sourceOrdinal));

            case FlatPromptMaskContinuationStateV1 mask:
                if (mask.Family != family ||
                    (family is not
                        (FlatPromptFamilyValueV1.MsgAnnounceRace or
                         FlatPromptFamilyValueV1.MsgAnnounceAttrib)) ||
                    mask.AvailableBitIndices.Count == 0)
                {
                    return false;
                }

                return candidates.All(candidate =>
                    candidate is FlatPromptMaskBitPublicCandidateV1 bit &&
                    mask.IsPickLegal(bit.BitIndex));

            default:
                return false;
        }
    }

    private static bool TryValidateResponseBody(
        FlatPromptFamilyV1 family,
        FlatPublicCandidateDescriptorV1 candidate,
        int selectableCount,
        int expectedResponse,
        byte[] responseBody)
    {
        if (family == FlatPromptFamilyValueV1.MsgAnnounceNumber)
        {
            return responseBody.Length == sizeof(int) &&
                BinaryPrimitives.ReadInt32LittleEndian(responseBody) ==
                    expectedResponse;
        }

        if (family != FlatPromptFamilyValueV1.MsgSelectUnselectCard)
        {
            return false;
        }

        if (candidate is FlatPromptSelectUnselectCardCandidateBaseV1 card &&
            card.ChoiceKind is
                (FlatPromptChoiceKindV1.Select or
                 FlatPromptChoiceKindV1.Unselect))
        {
            if (responseBody.Length != sizeof(uint) * 2 ||
                BinaryPrimitives.ReadUInt32LittleEndian(responseBody) != 1 ||
                BinaryPrimitives.ReadUInt32LittleEndian(
                    responseBody.AsSpan(sizeof(uint))) !=
                    (uint)expectedResponse)
            {
                return false;
            }

            return card.ChoiceKind == FlatPromptChoiceKindV1.Select
                ? card.SourceSection == FlatPromptSourceSectionV1.Selectable &&
                    expectedResponse == card.SourceOrdinal
                : card.SourceSection == FlatPromptSourceSectionV1.Unselectable &&
                    card.SourceOrdinal >= 0 &&
                    selectableCount <= int.MaxValue - card.SourceOrdinal &&
                    expectedResponse == selectableCount + card.SourceOrdinal;
        }

        return candidate is
            (FlatPromptFinishPublicCandidateV1 or
             FlatPromptCancelPublicCandidateV1 or
             FlatPromptFinishOrCancelPublicCandidateV1) &&
            responseBody.Length == sizeof(int) &&
            BinaryPrimitives.ReadInt32LittleEndian(responseBody) == -1 &&
            expectedResponse == -1;
    }

    private static bool TryGetExpectedBinding(
        FlatPromptFamilyV1 family,
        FlatPublicCandidateDescriptorV1 candidate,
        int selectableCount,
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

            case FlatPromptFamilyV1.MsgSelectBattleCmd:
                return TryGetBattleBinding(
                    candidate,
                    out expectedKey,
                    out expectedResponse);

            case FlatPromptFamilyV1.MsgSelectIdleCmd:
                return TryGetIdleBinding(
                    candidate,
                    out expectedKey,
                    out expectedResponse);

            case FlatPromptFamilyValueV1.MsgSelectCard:
                return TryGetCardContinuationBinding(
                    candidate,
                    FlatPromptFamilyValueV1.MsgSelectCard,
                    FlatPromptSourceSectionV1.SelectCard,
                    FlatPromptKeyV1.SelectCardPickPrefix,
                    out expectedKey,
                    out expectedResponse);

            case FlatPromptFamilyValueV1.MsgSelectTribute:
                return TryGetCardContinuationBinding(
                    candidate,
                    FlatPromptFamilyValueV1.MsgSelectTribute,
                    FlatPromptSourceSectionV1.SelectTribute,
                    FlatPromptKeyV1.SelectTributePickPrefix,
                    out expectedKey,
                    out expectedResponse);

            case FlatPromptFamilyValueV1.MsgSelectUnselectCard:
                return TryGetSelectUnselectBinding(
                    candidate,
                    selectableCount,
                    out expectedKey,
                    out expectedResponse);

            case FlatPromptFamilyValueV1.MsgAnnounceNumber:
                if (candidate is not FlatPromptAnnounceNumberPublicCandidateV1
                        number ||
                    number.ChoiceKind != FlatPromptChoiceKindV1.NumberOption ||
                    number.SourceSection !=
                        FlatPromptSourceSectionV1.NumberOptions ||
                    !FlatPromptKeyV1.TryCreateOrdinalKey(
                        FlatPromptKeyV1.AnnounceNumberOptionPrefix,
                        number.SourceOrdinal,
                        out expectedKey))
                {
                    return false;
                }

                expectedResponse = number.SourceOrdinal;
                return true;

            case FlatPromptFamilyValueV1.MsgSelectPlace:
            case FlatPromptFamilyValueV1.MsgSelectDisfield:
                return TryGetPlaceBinding(
                    candidate,
                    family,
                    out expectedKey,
                    out expectedResponse);

            case FlatPromptFamilyValueV1.MsgAnnounceRace:
            case FlatPromptFamilyValueV1.MsgAnnounceAttrib:
                return TryGetMaskBinding(
                    candidate,
                    family,
                    out expectedKey,
                    out expectedResponse);

            default:
                return false;
        }
    }

    private static bool TryGetPlaceBinding(
        FlatPublicCandidateDescriptorV1 candidate,
        FlatPromptFamilyV1 family,
        out string expectedKey,
        out int expectedResponse)
    {
        expectedKey = string.Empty;
        expectedResponse = default;
        if (candidate.GetType() !=
                typeof(FlatPromptFieldPlacePublicCandidateV1) ||
            candidate is not FlatPromptFieldPlacePublicCandidateV1 place ||
            place.ChoiceKind != FlatPromptChoiceKindV1.Pick ||
            !FlatPromptKeyV1.TryCreateFieldPlace(
                family,
                place.AbsolutePlayer,
                place.Zone,
                place.Sequence,
                out expectedKey))
        {
            return false;
        }

        expectedResponse = 0;
        return true;
    }

    private static bool TryGetMaskBinding(
        FlatPublicCandidateDescriptorV1 candidate,
        FlatPromptFamilyV1 family,
        out string expectedKey,
        out int expectedResponse)
    {
        expectedKey = string.Empty;
        expectedResponse = default;
        if (candidate.GetType() !=
                typeof(FlatPromptMaskBitPublicCandidateV1) ||
            candidate is not FlatPromptMaskBitPublicCandidateV1 bit ||
            bit.ChoiceKind != FlatPromptChoiceKindV1.Pick ||
            !IsAllowedMaskBit(family, bit.BitIndex) ||
            bit.BitValue != (1UL << bit.BitIndex) ||
            !FlatPromptKeyV1.TryCreateMaskBit(
                family,
                bit.BitIndex,
                out expectedKey))
        {
            return false;
        }

        expectedResponse = 0;
        return true;
    }

    private static bool IsAllowedMaskBit(
        FlatPromptFamilyV1 family,
        int bitIndex) =>
        family == FlatPromptFamilyValueV1.MsgAnnounceRace
            ? FlatPromptMaskValueV1.IsRaceBit(bitIndex)
            : family == FlatPromptFamilyValueV1.MsgAnnounceAttrib &&
              FlatPromptMaskValueV1.IsAttributeBit(bitIndex);

    private static bool TryGetCardContinuationBinding(
        FlatPublicCandidateDescriptorV1 candidate,
        FlatPromptFamilyV1 family,
        FlatPromptSourceSectionV1 sourceSection,
        string pickPrefix,
        out string expectedKey,
        out int expectedResponse)
    {
        expectedKey = string.Empty;
        expectedResponse = default;
        if (candidate is FlatPromptCardSelectionCandidateBaseV1 card &&
            family == FlatPromptFamilyValueV1.MsgSelectCard &&
            card.ChoiceKind == FlatPromptChoiceKindV1.Pick &&
            card.SourceSection == sourceSection &&
            IsConcreteType(
                candidate,
                typeof(FlatPromptCardSelectionAnonymousCandidateV1),
                typeof(FlatPromptCardSelectionPromptCodeCandidateV1),
                typeof(FlatPromptCardSelectionLocatorCandidateV1),
                typeof(FlatPromptCardSelectionLocatorPromptCodeCandidateV1)) &&
            !HasNonZeroPromptCardCodeIfInvalid(candidate) &&
            FlatPromptKeyV1.TryCreateOrdinalKey(
                pickPrefix,
                card.SourceOrdinal,
                out expectedKey))
        {
            expectedResponse = card.SourceOrdinal;
            return true;
        }

        if (candidate is FlatPromptTributeSelectionCandidateBaseV1 tribute &&
            family == FlatPromptFamilyValueV1.MsgSelectTribute &&
            tribute.ChoiceKind == FlatPromptChoiceKindV1.Pick &&
            tribute.SourceSection == sourceSection &&
            IsConcreteType(
                candidate,
                typeof(FlatPromptTributeSelectionAnonymousCandidateV1),
                typeof(FlatPromptTributeSelectionPromptCodeCandidateV1),
                typeof(FlatPromptTributeSelectionLocatorCandidateV1),
                typeof(FlatPromptTributeSelectionLocatorPromptCodeCandidateV1)) &&
            !HasNonZeroPromptCardCodeIfInvalid(candidate) &&
            FlatPromptKeyV1.TryCreateOrdinalKey(
                pickPrefix,
                tribute.SourceOrdinal,
                out expectedKey))
        {
            expectedResponse = tribute.SourceOrdinal;
            return true;
        }

        if (candidate is FlatPromptFinishPublicCandidateV1 finish &&
            finish.ChoiceKind == FlatPromptChoiceKindV1.Finish)
        {
            expectedKey = family == FlatPromptFamilyValueV1.MsgSelectCard
                ? FlatPromptKeyV1.SelectCardFinish
                : family == FlatPromptFamilyValueV1.MsgSelectTribute
                    ? FlatPromptKeyV1.SelectTributeFinish
                    : string.Empty;
            expectedResponse = -1;
            return expectedKey.Length != 0 &&
                string.Equals(
                    finish.I4LocalCandidateKey,
                    expectedKey,
                    StringComparison.Ordinal);
        }

        if (candidate is FlatPromptCancelPublicCandidateV1 cancel &&
            cancel.ChoiceKind == FlatPromptChoiceKindV1.Cancel)
        {
            expectedKey = family == FlatPromptFamilyValueV1.MsgSelectCard
                ? FlatPromptKeyV1.SelectCardCancel
                : family == FlatPromptFamilyValueV1.MsgSelectTribute
                    ? FlatPromptKeyV1.SelectTributeCancel
                    : string.Empty;
            expectedResponse = -1;
            return expectedKey.Length != 0 &&
                string.Equals(
                    cancel.I4LocalCandidateKey,
                    expectedKey,
                    StringComparison.Ordinal);
        }

        return false;
    }

    private static bool TryGetSelectUnselectBinding(
        FlatPublicCandidateDescriptorV1 candidate,
        int selectableCount,
        out string expectedKey,
        out int expectedResponse)
    {
        expectedKey = string.Empty;
        expectedResponse = default;
        if (candidate is FlatPromptSelectUnselectCardCandidateBaseV1 card &&
            IsConcreteType(
                candidate,
                typeof(FlatPromptSelectUnselectAnonymousCandidateV1),
                typeof(FlatPromptSelectUnselectPromptCodeCandidateV1),
                typeof(FlatPromptSelectUnselectLocatorCandidateV1),
                typeof(FlatPromptSelectUnselectLocatorPromptCodeCandidateV1)) &&
            !HasNonZeroPromptCardCodeIfInvalid(candidate))
        {
            bool isSelect = card.ChoiceKind == FlatPromptChoiceKindV1.Select &&
                card.SourceSection == FlatPromptSourceSectionV1.Selectable;
            bool isUnselect = card.ChoiceKind == FlatPromptChoiceKindV1.Unselect &&
                card.SourceSection == FlatPromptSourceSectionV1.Unselectable;
            string prefix = isSelect
                ? FlatPromptKeyV1.SelectUnselectSelectPrefix
                : isUnselect
                    ? FlatPromptKeyV1.SelectUnselectUnselectPrefix
                    : string.Empty;
            if (prefix.Length != 0 &&
                FlatPromptKeyV1.TryCreateOrdinalKey(
                    prefix,
                    card.SourceOrdinal,
                    out expectedKey))
            {
                if (isUnselect &&
                    (card.SourceOrdinal < 0 ||
                     selectableCount > int.MaxValue - card.SourceOrdinal))
                {
                    return false;
                }

                expectedResponse = isUnselect
                    ? selectableCount + card.SourceOrdinal
                    : card.SourceOrdinal;
                return true;
            }
        }

        if (candidate is FlatPromptFinishPublicCandidateV1 finish &&
            finish.ChoiceKind == FlatPromptChoiceKindV1.Finish)
        {
            expectedKey = FlatPromptKeyV1.SelectUnselectFinish;
            expectedResponse = -1;
            return string.Equals(
                finish.I4LocalCandidateKey,
                expectedKey,
                StringComparison.Ordinal);
        }

        if (candidate is FlatPromptCancelPublicCandidateV1 cancel &&
            cancel.ChoiceKind == FlatPromptChoiceKindV1.Cancel)
        {
            expectedKey = FlatPromptKeyV1.SelectUnselectCancel;
            expectedResponse = -1;
            return string.Equals(
                cancel.I4LocalCandidateKey,
                expectedKey,
                StringComparison.Ordinal);
        }

        if (candidate is FlatPromptFinishOrCancelPublicCandidateV1 both &&
            both.ChoiceKind == FlatPromptChoiceKindV1.FinishOrCancel)
        {
            expectedKey = FlatPromptKeyV1.SelectUnselectFinishOrCancel;
            expectedResponse = -1;
            return string.Equals(
                both.I4LocalCandidateKey,
                expectedKey,
                StringComparison.Ordinal);
        }

        return false;
    }

    private static bool HasNonZeroPromptCardCodeIfInvalid(
        FlatPublicCandidateDescriptorV1 candidate) =>
        candidate switch
        {
            FlatPromptCardSelectionPromptCodeCandidateV1 value =>
                value.PromptLocalCardCode == 0,
            FlatPromptCardSelectionLocatorPromptCodeCandidateV1 value =>
                value.PromptLocalCardCode == 0,
            FlatPromptTributeSelectionPromptCodeCandidateV1 value =>
                value.PromptLocalCardCode == 0,
            FlatPromptTributeSelectionLocatorPromptCodeCandidateV1 value =>
                value.PromptLocalCardCode == 0,
            FlatPromptSelectUnselectPromptCodeCandidateV1 value =>
                value.PromptLocalCardCode == 0,
            FlatPromptSelectUnselectLocatorPromptCodeCandidateV1 value =>
                value.PromptLocalCardCode == 0,
            _ => false
        };

    private static bool IsConcreteType(
        FlatPublicCandidateDescriptorV1 candidate,
        params Type[] allowedTypes) =>
        allowedTypes.Contains(candidate.GetType());

    private static bool TryGetBattleBinding(
        FlatPublicCandidateDescriptorV1 candidate,
        out string expectedKey,
        out int expectedResponse)
    {
        expectedKey = string.Empty;
        expectedResponse = default;
        if (candidate is FlatBattleActivatablePublicCandidateBaseV1 activatable)
        {
            if (!IsConcreteType(
                candidate,
                typeof(FlatBattleActivatablePublicCandidateV1),
                typeof(FlatBattleActivatableCardCodePublicCandidateV1)) ||
                !HasNonZeroCardCodeIfPresent(candidate) ||
                activatable.ChoiceKind != FlatPromptChoiceKindV1.Activate ||
                activatable.SourceSection !=
                    FlatPromptSourceSectionV1.Activatable ||
                !FlatPromptKeyV1.TryCreateBattleActivatable(
                    activatable.SourceOrdinal,
                    out expectedKey))
            {
                return false;
            }

            return FlatPromptKeyV1.TryEncodeIndexedResponse(
                activatable.SourceOrdinal,
                0,
                out expectedResponse);
        }

        if (candidate is FlatBattleAttackPublicCandidateBaseV1 attack)
        {
            if (!IsConcreteType(
                candidate,
                typeof(FlatBattleAttackPublicCandidateV1),
                typeof(FlatBattleAttackCardCodePublicCandidateV1)) ||
                !HasNonZeroCardCodeIfPresent(candidate) ||
                attack.ChoiceKind != FlatPromptChoiceKindV1.Attack ||
                attack.SourceSection != FlatPromptSourceSectionV1.Attackable ||
                !FlatPromptKeyV1.TryCreateBattleAttack(
                    attack.SourceOrdinal,
                    out expectedKey))
            {
                return false;
            }

            return FlatPromptKeyV1.TryEncodeIndexedResponse(
                attack.SourceOrdinal,
                1,
                out expectedResponse);
        }

        if (candidate is FlatBattleToMainPhase2PublicCandidateV1 toM2)
        {
            if (candidate.GetType() !=
                    typeof(FlatBattleToMainPhase2PublicCandidateV1) ||
                toM2.ChoiceKind != FlatPromptChoiceKindV1.ToM2 ||
                !string.Equals(
                    toM2.I4LocalCandidateKey,
                    FlatPromptKeyV1.BattleToM2,
                    StringComparison.Ordinal) ||
                toM2.TransitionToken != "MAIN_PHASE_2")
            {
                return false;
            }

            expectedKey = FlatPromptKeyV1.BattleToM2;
            expectedResponse = 2;
            return true;
        }

        if (candidate is FlatBattleToEndPhasePublicCandidateV1 toEnd)
        {
            if (candidate.GetType() !=
                    typeof(FlatBattleToEndPhasePublicCandidateV1) ||
                toEnd.ChoiceKind != FlatPromptChoiceKindV1.ToEp ||
                toEnd.I4LocalCandidateKey != FlatPromptKeyV1.BattleToEp ||
                toEnd.TransitionToken != "END_PHASE")
            {
                return false;
            }

            expectedKey = FlatPromptKeyV1.BattleToEp;
            expectedResponse = 3;
            return true;
        }

        return false;
    }

    private static bool TryGetIdleBinding(
        FlatPublicCandidateDescriptorV1 candidate,
        out string expectedKey,
        out int expectedResponse)
    {
        expectedKey = string.Empty;
        expectedResponse = default;
        if (candidate is FlatIdleSummonPublicCandidateV1 summon ||
            candidate is FlatIdleSummonCardCodePublicCandidateV1)
        {
            return TryGetIdleSimpleBinding(
                candidate,
                FlatPromptChoiceKindV1.Summon,
                FlatPromptSourceSectionV1.Summon,
                "MSG_SELECT_IDLECMD:SUMMON:",
                0,
                typeof(FlatIdleSummonPublicCandidateV1),
                typeof(FlatIdleSummonCardCodePublicCandidateV1),
                out expectedKey,
                out expectedResponse);
        }

        if (candidate is FlatIdleSpecialSummonPublicCandidateV1 ||
            candidate is FlatIdleSpecialSummonCardCodePublicCandidateV1)
        {
            return TryGetIdleSimpleBinding(
                candidate,
                FlatPromptChoiceKindV1.SpecialSummon,
                FlatPromptSourceSectionV1.SpecialSummon,
                "MSG_SELECT_IDLECMD:SPECIAL_SUMMON:",
                1,
                typeof(FlatIdleSpecialSummonPublicCandidateV1),
                typeof(FlatIdleSpecialSummonCardCodePublicCandidateV1),
                out expectedKey,
                out expectedResponse);
        }

        if (candidate is FlatIdleRepositionPublicCandidateV1 ||
            candidate is FlatIdleRepositionCardCodePublicCandidateV1)
        {
            return TryGetIdleSimpleBinding(
                candidate,
                FlatPromptChoiceKindV1.Reposition,
                FlatPromptSourceSectionV1.Reposition,
                "MSG_SELECT_IDLECMD:REPOSITION:",
                2,
                typeof(FlatIdleRepositionPublicCandidateV1),
                typeof(FlatIdleRepositionCardCodePublicCandidateV1),
                out expectedKey,
                out expectedResponse);
        }

        if (candidate is FlatIdleMsetPublicCandidateV1 ||
            candidate is FlatIdleMsetCardCodePublicCandidateV1)
        {
            return TryGetIdleSimpleBinding(
                candidate,
                FlatPromptChoiceKindV1.Mset,
                FlatPromptSourceSectionV1.Mset,
                "MSG_SELECT_IDLECMD:MSET:",
                3,
                typeof(FlatIdleMsetPublicCandidateV1),
                typeof(FlatIdleMsetCardCodePublicCandidateV1),
                out expectedKey,
                out expectedResponse);
        }

        if (candidate is FlatIdleSsetPublicCandidateV1 ||
            candidate is FlatIdleSsetCardCodePublicCandidateV1)
        {
            return TryGetIdleSimpleBinding(
                candidate,
                FlatPromptChoiceKindV1.Sset,
                FlatPromptSourceSectionV1.Sset,
                "MSG_SELECT_IDLECMD:SSET:",
                4,
                typeof(FlatIdleSsetPublicCandidateV1),
                typeof(FlatIdleSsetCardCodePublicCandidateV1),
                out expectedKey,
                out expectedResponse);
        }

        if (candidate is FlatIdleActivatablePublicCandidateBaseV1 activatable)
        {
            if (!IsConcreteType(
                    candidate,
                    typeof(FlatIdleActivatablePublicCandidateV1),
                    typeof(FlatIdleActivatableCardCodePublicCandidateV1)) ||
                !HasNonZeroCardCodeIfPresent(candidate) ||
                activatable.ChoiceKind != FlatPromptChoiceKindV1.Activate ||
                activatable.SourceSection != FlatPromptSourceSectionV1.Activate ||
                !FlatPromptKeyV1.TryCreateIdleActivatable(
                    activatable.SourceOrdinal,
                    out expectedKey))
            {
                return false;
            }

            return FlatPromptKeyV1.TryEncodeIndexedResponse(
                activatable.SourceOrdinal,
                5,
                out expectedResponse);
        }

        if (candidate is FlatIdleToBattlePhasePublicCandidateV1 toBattle)
        {
            if (candidate.GetType() !=
                    typeof(FlatIdleToBattlePhasePublicCandidateV1) ||
                toBattle.ChoiceKind != FlatPromptChoiceKindV1.ToBp ||
                toBattle.I4LocalCandidateKey != FlatPromptKeyV1.IdleToBp ||
                toBattle.TransitionToken != "BATTLE_PHASE")
            {
                return false;
            }

            expectedKey = FlatPromptKeyV1.IdleToBp;
            expectedResponse = 6;
            return true;
        }

        if (candidate is FlatIdleToEndPhasePublicCandidateV1 toEnd)
        {
            if (candidate.GetType() !=
                    typeof(FlatIdleToEndPhasePublicCandidateV1) ||
                toEnd.ChoiceKind != FlatPromptChoiceKindV1.ToEp ||
                toEnd.I4LocalCandidateKey != FlatPromptKeyV1.IdleToEp ||
                toEnd.TransitionToken != "END_PHASE")
            {
                return false;
            }

            expectedKey = FlatPromptKeyV1.IdleToEp;
            expectedResponse = 7;
            return true;
        }

        if (candidate is FlatIdleShuffleHandPublicCandidateV1 shuffle)
        {
            if (candidate.GetType() !=
                    typeof(FlatIdleShuffleHandPublicCandidateV1) ||
                shuffle.ChoiceKind != FlatPromptChoiceKindV1.ShuffleHand ||
                shuffle.I4LocalCandidateKey != FlatPromptKeyV1.IdleShuffleHand ||
                shuffle.TransitionToken != "SHUFFLE_HAND")
            {
                return false;
            }

            expectedKey = FlatPromptKeyV1.IdleShuffleHand;
            expectedResponse = 8;
            return true;
        }

        return false;
    }

    private static bool TryGetIdleSimpleBinding(
        FlatPublicCandidateDescriptorV1 candidate,
        FlatPromptChoiceKindV1 choiceKind,
        FlatPromptSourceSectionV1 sourceSection,
        string keyPrefix,
        int responseKind,
        Type noCodeType,
        Type cardCodeType,
        out string expectedKey,
        out int expectedResponse)
    {
        expectedKey = string.Empty;
        expectedResponse = default;
        if (candidate is not FlatIdleCardActionPublicCandidateBaseV1 simple ||
            !IsConcreteType(candidate, noCodeType, cardCodeType) ||
            !HasNonZeroCardCodeIfPresent(candidate) ||
            simple.ChoiceKind != choiceKind ||
            simple.SourceSection != sourceSection ||
            !FlatPromptKeyV1.TryCreateOrdinalKey(
                keyPrefix,
                simple.SourceOrdinal,
                out expectedKey))
        {
            return false;
        }

        return FlatPromptKeyV1.TryEncodeIndexedResponse(
            simple.SourceOrdinal,
            responseKind,
            out expectedResponse);
    }

    private static bool IsConcreteType(
        FlatPublicCandidateDescriptorV1 candidate,
        Type noCodeType,
        Type cardCodeType) =>
        candidate.GetType() == noCodeType ||
        candidate.GetType() == cardCodeType;

    private static bool HasNonZeroCardCodeIfPresent(
        FlatPublicCandidateDescriptorV1 candidate) =>
        candidate switch
        {
            FlatChainCardCodePublicCandidateDescriptorV1 value =>
                value.CardCode != 0,
            FlatBattleActivatableCardCodePublicCandidateV1 value =>
                value.CardCode != 0,
            FlatBattleAttackCardCodePublicCandidateV1 value =>
                value.CardCode != 0,
            FlatIdleSummonCardCodePublicCandidateV1 value =>
                value.CardCode != 0,
            FlatIdleSpecialSummonCardCodePublicCandidateV1 value =>
                value.CardCode != 0,
            FlatIdleRepositionCardCodePublicCandidateV1 value =>
                value.CardCode != 0,
            FlatIdleMsetCardCodePublicCandidateV1 value =>
                value.CardCode != 0,
            FlatIdleSsetCardCodePublicCandidateV1 value =>
                value.CardCode != 0,
            FlatIdleActivatableCardCodePublicCandidateV1 value =>
                value.CardCode != 0,
            _ => true
        };
}

internal sealed class FlatPromptSelectionHandleV1
{
    private readonly FlatPublicCandidateDescriptorV1[] orderedDomain;
    private readonly ReadOnlyCollection<FlatPublicCandidateDescriptorV1> orderedDomainView;

    internal FlatPromptSelectionHandleV1(
        ulong promptInstanceOrdinal,
        FlatPromptFamilyV1 family,
        string i4LocalCandidateKey,
        IReadOnlyList<FlatPublicCandidateDescriptorV1> orderedDomain,
        int continuationStep = 0)
    {
        PromptInstanceOrdinal = promptInstanceOrdinal;
        Family = family;
        I4LocalCandidateKey = i4LocalCandidateKey ??
            throw new ArgumentNullException(nameof(i4LocalCandidateKey));
        ArgumentNullException.ThrowIfNull(orderedDomain);
        this.orderedDomain = orderedDomain.ToArray();
        orderedDomainView = Array.AsReadOnly(this.orderedDomain);
        ArgumentOutOfRangeException.ThrowIfNegative(continuationStep);
        ContinuationStep = continuationStep;
    }

    internal ulong PromptInstanceOrdinal { get; }

    internal FlatPromptFamilyV1 Family { get; }

    internal int ContinuationStep { get; }

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
    internal const string BattleToM2 = "MSG_SELECT_BATTLECMD:TO_M2";
    internal const string BattleToEp = "MSG_SELECT_BATTLECMD:TO_EP";
    internal const string IdleToBp = "MSG_SELECT_IDLECMD:TO_BP";
    internal const string IdleToEp = "MSG_SELECT_IDLECMD:TO_EP";
    internal const string IdleShuffleHand =
        "MSG_SELECT_IDLECMD:SHUFFLE_HAND";
    internal const string IdleSummonPrefix =
        "MSG_SELECT_IDLECMD:SUMMON:";
    internal const string IdleSpecialSummonPrefix =
        "MSG_SELECT_IDLECMD:SPECIAL_SUMMON:";
    internal const string IdleRepositionPrefix =
        "MSG_SELECT_IDLECMD:REPOSITION:";
    internal const string IdleMsetPrefix =
        "MSG_SELECT_IDLECMD:MSET:";
    internal const string IdleSsetPrefix =
        "MSG_SELECT_IDLECMD:SSET:";
    internal const string SelectCardPickPrefix =
        "MSG_SELECT_CARD:PICK:";
    internal const string SelectCardFinish =
        "MSG_SELECT_CARD:FINISH";
    internal const string SelectCardCancel =
        "MSG_SELECT_CARD:CANCEL";
    internal const string SelectTributePickPrefix =
        "MSG_SELECT_TRIBUTE:PICK:";
    internal const string SelectTributeFinish =
        "MSG_SELECT_TRIBUTE:FINISH";
    internal const string SelectTributeCancel =
        "MSG_SELECT_TRIBUTE:CANCEL";
    internal const string SelectUnselectSelectPrefix =
        "MSG_SELECT_UNSELECT_CARD:SELECT:";
    internal const string SelectUnselectUnselectPrefix =
        "MSG_SELECT_UNSELECT_CARD:UNSELECT:";
    internal const string SelectUnselectFinish =
        "MSG_SELECT_UNSELECT_CARD:FINISH";
    internal const string SelectUnselectCancel =
        "MSG_SELECT_UNSELECT_CARD:CANCEL";
    internal const string SelectUnselectFinishOrCancel =
        "MSG_SELECT_UNSELECT_CARD:FINISH_OR_CANCEL";
    internal const string AnnounceNumberOptionPrefix =
        "MSG_ANNOUNCE_NUMBER:OPTION:";
    internal const string SelectPlacePickPrefix =
        "MSG_SELECT_PLACE:PICK:";
    internal const string SelectDisfieldPickPrefix =
        "MSG_SELECT_DISFIELD:PICK:";
    internal const string AnnounceRacePickPrefix =
        "MSG_ANNOUNCE_RACE:PICK:";
    internal const string AnnounceAttribPickPrefix =
        "MSG_ANNOUNCE_ATTRIB:PICK:";
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

    internal static bool TryCreateBattleActivatable(
        int sourceOrdinal,
        out string key) =>
        TryCreateOrdinalKey(
            "MSG_SELECT_BATTLECMD:ACTIVATE:",
            sourceOrdinal,
            out key);

    internal static bool TryCreateBattleAttack(
        int sourceOrdinal,
        out string key) =>
        TryCreateOrdinalKey(
            "MSG_SELECT_BATTLECMD:ATTACK:",
            sourceOrdinal,
            out key);

    internal static bool TryCreateIdleActivatable(
        int sourceOrdinal,
        out string key) =>
        TryCreateOrdinalKey(
            "MSG_SELECT_IDLECMD:ACTIVATE:",
            sourceOrdinal,
            out key);

    internal static bool TryCreateFieldPlace(
        FlatPromptFamilyV1 family,
        byte absolutePlayer,
        FlatPromptFieldZoneV1 zone,
        byte sequence,
        out string key)
    {
        key = string.Empty;
        string prefix = family == FlatPromptFamilyValueV1.MsgSelectPlace
            ? SelectPlacePickPrefix
            : family == FlatPromptFamilyValueV1.MsgSelectDisfield
                ? SelectDisfieldPickPrefix
                : string.Empty;
        if (prefix.Length == 0 ||
            absolutePlayer > 1 ||
            zone is not
                (FlatPromptFieldZoneV1.MonsterZone or
                 FlatPromptFieldZoneV1.SpellTrapZone) ||
            sequence >
                (zone == FlatPromptFieldZoneV1.MonsterZone ? 6 : 7))
        {
            return false;
        }

        string playerDigits = absolutePlayer.ToString(
            CultureInfo.InvariantCulture);
        string sequenceDigits = sequence.ToString(
            CultureInfo.InvariantCulture);
        string zoneToken = zone == FlatPromptFieldZoneV1.MonsterZone
            ? "MONSTER_ZONE"
            : "SPELL_TRAP_ZONE";
        if (!IsCanonicalAsciiDecimal(playerDigits) ||
            !IsCanonicalAsciiDecimal(sequenceDigits))
        {
            return false;
        }

        key = prefix + playerDigits + ":" + zoneToken + ":" +
            sequenceDigits;
        return true;
    }

    internal static bool TryCreateMaskBit(
        FlatPromptFamilyV1 family,
        int bitIndex,
        out string key)
    {
        key = string.Empty;
        string prefix;
        bool admitted;
        if (family == FlatPromptFamilyValueV1.MsgAnnounceRace)
        {
            prefix = AnnounceRacePickPrefix;
            admitted = FlatPromptMaskValueV1.IsRaceBit(bitIndex);
        }
        else if (family == FlatPromptFamilyValueV1.MsgAnnounceAttrib)
        {
            prefix = AnnounceAttribPickPrefix;
            admitted = FlatPromptMaskValueV1.IsAttributeBit(bitIndex);
        }
        else
        {
            return false;
        }

        if (!admitted || bitIndex < 0 || bitIndex > 63)
        {
            return false;
        }

        string digits = bitIndex.ToString(CultureInfo.InvariantCulture);
        if (!IsCanonicalAsciiDecimal(digits))
        {
            return false;
        }

        key = prefix + digits;
        return true;
    }

    internal static bool TryCreateOrdinalKey(
        string prefix,
        int sourceOrdinal,
        out string key)
    {
        key = string.Empty;
        if (sourceOrdinal < 0 || sourceOrdinal > ushort.MaxValue)
        {
            return false;
        }

        string digits = sourceOrdinal.ToString(CultureInfo.InvariantCulture);
        if (!IsCanonicalAsciiDecimal(digits))
        {
            return false;
        }

        key = prefix + digits;
        return true;
    }

    internal static bool TryEncodeIndexedResponse(
        int sourceOrdinal,
        int kind,
        out int response)
    {
        response = default;
        if (sourceOrdinal < 0 ||
            sourceOrdinal > ushort.MaxValue ||
            kind < 0 ||
            kind > ushort.MaxValue)
        {
            return false;
        }

        response = unchecked(
            (int)(((uint)sourceOrdinal << 16) | (uint)kind));
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
