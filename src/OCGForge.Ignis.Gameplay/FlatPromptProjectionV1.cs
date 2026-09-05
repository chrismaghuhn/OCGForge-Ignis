using System.Buffers.Binary;
using System.Numerics;

namespace OCGForge.Ignis.Gameplay;

internal static class FlatPromptProjectionV1
{
    private const int YesNoMessageLength = 10;
    private const int EffectYnMessageLength = 24;
    private const int ChainHeaderLength = 16;
    private const int ChainEntryLength = 23;
    private const int BattleHeaderLength = 12;
    private const int BattleActivatableEntryLength = 19;
    private const int BattleAttackableEntryLength = 8;
    private const int IdleHeaderLength = 29;
    private const int IdleCardEntryLength = 10;
    private const int IdleRepositionEntryLength = 7;
    private const int IdleActivatableEntryLength = 19;
    private const uint MaximumSectionEntryCount = ushort.MaxValue + 1u;
    private const int OptionHeaderLength = 3;
    private const int OptionDescriptionLength = 8;
    private const int PositionMessageLength = 7;
    private const byte ValidPositionMask = 0x0F;
    private const int SelectCardHeaderLength = 15;
    private const int SelectCardEntryLength = 14;
    private const int SelectTributeHeaderLength = 15;
    private const int SelectTributeEntryLength = 11;
    private const int SelectUnselectHeaderLength = 20;
    private const int SelectUnselectEntryLength = 14;
    private const int AnnounceNumberHeaderLength = 3;
    private const int AnnounceNumberOptionLength = 8;
    private const uint MaximumTributeCardCount = 5;

    private static readonly int[] YesNoResponses = { 0, 1 };
    private static readonly int[] EffectYnResponses = { 0, 1 };

    private static readonly (
        byte Bit,
        FlatPromptChoiceKindV1 ChoiceKind,
        string Key)[] PositionChoices =
    {
        (0x01, FlatPromptChoiceKindV1.FaceupAttack,
            "MSG_SELECT_POSITION:FACEUP_ATTACK"),
        (0x02, FlatPromptChoiceKindV1.FacedownAttack,
            "MSG_SELECT_POSITION:FACEDOWN_ATTACK"),
        (0x04, FlatPromptChoiceKindV1.FaceupDefense,
            "MSG_SELECT_POSITION:FACEUP_DEFENSE"),
        (0x08, FlatPromptChoiceKindV1.FacedownDefense,
            "MSG_SELECT_POSITION:FACEDOWN_DEFENSE")
    };

    internal static bool TryProject(
        ReadOnlySpan<byte> bytes,
        out FlatPromptProjectionDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = FlatPromptErrorCodeV1.None;
        if (bytes.IsEmpty)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        return bytes[0] switch
        {
            13 => TryProjectYesNo(bytes, out draft, out error),
            14 => TryProjectOption(bytes, out draft, out error),
            19 => TryProjectPosition(bytes, out draft, out error),
            _ => Fail(
                FlatPromptErrorCodeV1.UnsupportedPromptLayout,
                out draft,
                out error)
        };
    }

    internal static bool TryParseWireDraft(
        ReadOnlySpan<byte> bytes,
        out FlatPromptWireDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = FlatPromptErrorCodeV1.None;
        if (bytes.IsEmpty)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        return bytes[0] switch
        {
            10 => TryParseBattleWireDraft(bytes, out draft, out error),
            11 => TryParseIdleWireDraft(bytes, out draft, out error),
            12 => TryParseEffectYnWireDraft(bytes, out draft, out error),
            16 => TryParseChainWireDraft(bytes, out draft, out error),
            _ => FailWireDraft(
                FlatPromptErrorCodeV1.UnsupportedPromptLayout,
                out draft,
                out error)
        };
    }

    internal static bool TryParseI5WireDraft(
        ReadOnlySpan<byte> bytes,
        out FlatPromptWireDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = FlatPromptErrorCodeV1.None;
        if (bytes.IsEmpty)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        return bytes[0] switch
        {
            15 => TryParseSelectCardWireDraft(bytes, out draft, out error),
            20 => TryParseSelectTributeWireDraft(bytes, out draft, out error),
            26 => TryParseSelectUnselectWireDraft(bytes, out draft, out error),
            143 => TryParseAnnounceNumberWireDraft(bytes, out draft, out error),
            23 => FailWireDraft(
                FlatPromptErrorCodeV1.UnsupportedPromptFamily,
                out draft,
                out error),
            _ => FailWireDraft(
                FlatPromptErrorCodeV1.UnsupportedPromptLayout,
                out draft,
                out error)
        };
    }

    internal static bool TryBuildProjectedDraft(
        FlatPromptWireDraftV1 draft,
        FlatPromptCardAuthorityContextV1? authority,
        out FlatPromptProjectionDraftV1? projected,
        out FlatPromptErrorCodeV1 error)
    {
        ArgumentNullException.ThrowIfNull(draft);
        projected = null;
        error = FlatPromptErrorCodeV1.None;
        if (authority is null &&
            draft.Family is not
                (FlatPromptFamilyValueV1.MsgSelectCard or
                 FlatPromptFamilyValueV1.MsgSelectTribute or
                 FlatPromptFamilyValueV1.MsgSelectUnselectCard or
                 FlatPromptFamilyValueV1.MsgAnnounceNumber))
        {
            error = FlatPromptErrorCodeV1.UnprovenPublicReference;
            return false;
        }

        FlatPromptCardAuthorityContextV1 requiredAuthority = authority!;

        return draft switch
        {
            FlatPromptBattleWireDraftV1 battle =>
                TryBuildBattleProjection(
                    battle,
                    requiredAuthority,
                    out projected,
                    out error),
            FlatPromptIdleWireDraftV1 idle =>
                TryBuildIdleProjection(
                    idle,
                    requiredAuthority,
                    out projected,
                    out error),
            FlatPromptEffectYnWireDraftV1 effect =>
                TryBuildEffectYnProjection(
                    effect,
                    requiredAuthority,
                    out projected,
                    out error),
            FlatPromptChainWireDraftV1 chain =>
                TryBuildChainProjection(
                    chain,
                    requiredAuthority,
                    out projected,
                    out error),
            FlatPromptSelectCardWireDraftV1 selectCard =>
                TryBuildSelectCardProjection(
                    selectCard,
                    authority,
                    out projected,
                    out error),
            FlatPromptSelectTributeWireDraftV1 tribute =>
                TryBuildSelectTributeProjection(
                    tribute,
                    authority,
                    out projected,
                    out error),
            FlatPromptSelectUnselectWireDraftV1 selectUnselect =>
                TryBuildSelectUnselectProjection(
                    selectUnselect,
                    authority,
                    out projected,
                    out error),
            FlatPromptAnnounceNumberWireDraftV1 announceNumber =>
                TryBuildAnnounceNumberProjection(
                    announceNumber,
                    out projected,
                    out error),
            _ => FailProjection(
                FlatPromptErrorCodeV1.UnsupportedPromptLayout,
                out projected,
                out error)
        };
    }

    private static bool TryProjectI5A1(
        ReadOnlySpan<byte> bytes,
        out FlatPromptProjectionDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        if (!TryParseI5WireDraft(
                bytes,
                out FlatPromptWireDraftV1? wireDraft,
                out error) ||
            wireDraft is null)
        {
            return false;
        }

        return TryBuildProjectedDraft(
            wireDraft,
            null,
            out draft,
            out error);
    }

    private static bool TryParseSelectCardWireDraft(
        ReadOnlySpan<byte> bytes,
        out FlatPromptWireDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = FlatPromptErrorCodeV1.None;
        if (bytes.Length < SelectCardHeaderLength)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        byte actingPlayer = bytes[1];
        if (actingPlayer > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidParticipant;
            return false;
        }

        byte cancelable = bytes[2];
        if (cancelable > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidBoolean;
            return false;
        }

        uint minimumCount = BinaryPrimitives.ReadUInt32LittleEndian(
            bytes.Slice(3, sizeof(uint)));
        uint maximumCount = BinaryPrimitives.ReadUInt32LittleEndian(
            bytes.Slice(7, sizeof(uint)));
        uint occurrenceCount = BinaryPrimitives.ReadUInt32LittleEndian(
            bytes.Slice(11, sizeof(uint)));
        if (occurrenceCount > MaximumSectionEntryCount)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (occurrenceCount == 0 ||
            maximumCount == 0 ||
            minimumCount > byte.MaxValue ||
            maximumCount > byte.MaxValue ||
            minimumCount == 0 && cancelable == 0 ||
            minimumCount > maximumCount ||
            maximumCount > occurrenceCount)
        {
            error = FlatPromptErrorCodeV1.UnprovenCandidateDomain;
            return false;
        }

        ulong requiredLength;
        try
        {
            requiredLength = checked(
                (ulong)SelectCardHeaderLength +
                checked((ulong)SelectCardEntryLength * occurrenceCount));
        }
        catch (OverflowException)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (requiredLength > int.MaxValue)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (requiredLength != (ulong)bytes.Length)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        FlatPromptSelectCardWireEntryV1[] entries =
            new FlatPromptSelectCardWireEntryV1[(int)occurrenceCount];
        int offset = SelectCardHeaderLength;
        for (int ordinal = 0; ordinal < entries.Length; ordinal++)
        {
            if (!TryReadSelectCardEntry(
                    bytes,
                    ref offset,
                    actingPlayer,
                    true,
                    out entries[ordinal],
                    out error))
            {
                return false;
            }
        }

        draft = new FlatPromptSelectCardWireDraftV1(
            actingPlayer,
            cancelable == 1,
            minimumCount,
            maximumCount,
            entries);
        return true;
    }

    private static bool TryParseSelectTributeWireDraft(
        ReadOnlySpan<byte> bytes,
        out FlatPromptWireDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = FlatPromptErrorCodeV1.None;
        if (bytes.Length < SelectTributeHeaderLength)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        byte actingPlayer = bytes[1];
        if (actingPlayer > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidParticipant;
            return false;
        }

        byte cancelable = bytes[2];
        if (cancelable > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidBoolean;
            return false;
        }

        uint minimumRequiredTributeValue =
            BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(3, sizeof(uint)));
        uint maximumSelectedCardCount =
            BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(7, sizeof(uint)));
        uint occurrenceCount = BinaryPrimitives.ReadUInt32LittleEndian(
            bytes.Slice(11, sizeof(uint)));
        if (occurrenceCount > MaximumSectionEntryCount)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (occurrenceCount == 0 ||
            maximumSelectedCardCount == 0 ||
            maximumSelectedCardCount > MaximumTributeCardCount ||
            minimumRequiredTributeValue == 0 && cancelable == 0 ||
            minimumRequiredTributeValue > maximumSelectedCardCount)
        {
            error = FlatPromptErrorCodeV1.UnprovenCandidateDomain;
            return false;
        }

        ulong requiredLength;
        try
        {
            requiredLength = checked(
                (ulong)SelectTributeHeaderLength +
                checked((ulong)SelectTributeEntryLength * occurrenceCount));
        }
        catch (OverflowException)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (requiredLength > int.MaxValue)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (requiredLength != (ulong)bytes.Length)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        FlatPromptSelectTributeWireEntryV1[] entries =
            new FlatPromptSelectTributeWireEntryV1[(int)occurrenceCount];
        int offset = SelectTributeHeaderLength;
        uint accumulatedReleaseValue = 0;
        for (int ordinal = 0; ordinal < entries.Length; ordinal++)
        {
            uint sourceCardCode = BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(offset, sizeof(uint)));
            byte controller = bytes[offset + 4];
            byte location = bytes[offset + 5];
            uint sequence = BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(offset + 6, sizeof(uint)));
            byte releaseValue = bytes[offset + 10];
            if (releaseValue is < 1 or > 3 ||
                !TryValidatePrivateLocation(
                    new ModernLocInfoV1(controller, location, sequence, 0),
                    out error))
            {
                if (error == FlatPromptErrorCodeV1.None)
                {
                    error = FlatPromptErrorCodeV1.UnprovenCandidateDomain;
                }

                return false;
            }

            entries[ordinal] = new FlatPromptSelectTributeWireEntryV1(
                sourceCardCode,
                new ModernLocInfoV1(controller, location, sequence, 0),
                releaseValue);
            accumulatedReleaseValue = checked(
                accumulatedReleaseValue + releaseValue);
            offset += SelectTributeEntryLength;
        }

        if (maximumSelectedCardCount > accumulatedReleaseValue)
        {
            error = FlatPromptErrorCodeV1.UnprovenCandidateDomain;
            return false;
        }

        draft = new FlatPromptSelectTributeWireDraftV1(
            actingPlayer,
            cancelable == 1,
            minimumRequiredTributeValue,
            maximumSelectedCardCount,
            entries);
        return true;
    }

    private static bool TryParseSelectUnselectWireDraft(
        ReadOnlySpan<byte> bytes,
        out FlatPromptWireDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = FlatPromptErrorCodeV1.None;
        if (bytes.Length < SelectUnselectHeaderLength)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        byte actingPlayer = bytes[1];
        if (actingPlayer > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidParticipant;
            return false;
        }

        byte finishable = bytes[2];
        byte cancelable = bytes[3];
        if (finishable > 1 || cancelable > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidBoolean;
            return false;
        }

        uint minimumCount = BinaryPrimitives.ReadUInt32LittleEndian(
            bytes.Slice(4, sizeof(uint)));
        uint maximumCount = BinaryPrimitives.ReadUInt32LittleEndian(
            bytes.Slice(8, sizeof(uint)));
        uint selectableCount = BinaryPrimitives.ReadUInt32LittleEndian(
            bytes.Slice(12, sizeof(uint)));
        if (selectableCount > MaximumSectionEntryCount)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (minimumCount > maximumCount)
        {
            error = FlatPromptErrorCodeV1.UnprovenCandidateDomain;
            return false;
        }

        ulong unselectableCountOffset;
        try
        {
            unselectableCountOffset = checked(
                (ulong)sizeof(byte) * 4 +
                (ulong)sizeof(uint) * 3 +
                checked((ulong)SelectUnselectEntryLength * selectableCount));
        }
        catch (OverflowException)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (unselectableCountOffset > int.MaxValue ||
            unselectableCountOffset > (ulong)bytes.Length ||
            bytes.Length - (int)unselectableCountOffset < sizeof(uint))
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        int unselectableCountOffsetInt = (int)unselectableCountOffset;
        uint unselectableCount = BinaryPrimitives.ReadUInt32LittleEndian(
            bytes.Slice(unselectableCountOffsetInt, sizeof(uint)));
        if (unselectableCount > MaximumSectionEntryCount)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (selectableCount == 0 && unselectableCount == 0)
        {
            error = FlatPromptErrorCodeV1.UnprovenCandidateDomain;
            return false;
        }

        ulong requiredLength;
        try
        {
            requiredLength = checked(
                (ulong)SelectUnselectHeaderLength +
                checked((ulong)SelectUnselectEntryLength *
                    checked(selectableCount + unselectableCount)));
        }
        catch (OverflowException)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (requiredLength > int.MaxValue)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (requiredLength != (ulong)bytes.Length)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        FlatPromptSelectCardWireEntryV1[] selectableEntries =
            new FlatPromptSelectCardWireEntryV1[(int)selectableCount];
        FlatPromptSelectCardWireEntryV1[] unselectableEntries =
            new FlatPromptSelectCardWireEntryV1[(int)unselectableCount];
        int offset = sizeof(byte) * 4 + sizeof(uint) * 3;
        if (!TryReadSelectCardEntries(
                bytes,
                ref offset,
                actingPlayer,
                false,
                selectableEntries,
                out error))
        {
            return false;
        }

        offset += sizeof(uint);
        if (!TryReadSelectCardEntries(
                bytes,
                ref offset,
                actingPlayer,
                false,
                unselectableEntries,
                out error))
        {
            return false;
        }

        draft = new FlatPromptSelectUnselectWireDraftV1(
            actingPlayer,
            finishable == 1,
            cancelable == 1,
            minimumCount,
            maximumCount,
            selectableEntries,
            unselectableEntries);
        return true;
    }

    private static bool TryParseAnnounceNumberWireDraft(
        ReadOnlySpan<byte> bytes,
        out FlatPromptWireDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = FlatPromptErrorCodeV1.None;
        if (bytes.Length < AnnounceNumberHeaderLength)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        byte actingPlayer = bytes[1];
        if (actingPlayer > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidParticipant;
            return false;
        }

        byte optionCount = bytes[2];
        if (optionCount == 0)
        {
            error = FlatPromptErrorCodeV1.ZeroOptionDomain;
            return false;
        }

        ulong requiredLength;
        try
        {
            requiredLength = checked(
                (ulong)AnnounceNumberHeaderLength +
                checked((ulong)AnnounceNumberOptionLength * optionCount));
        }
        catch (OverflowException)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (requiredLength != (ulong)bytes.Length)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        ulong[] values = new ulong[optionCount];
        int offset = AnnounceNumberHeaderLength;
        for (int ordinal = 0; ordinal < values.Length; ordinal++)
        {
            values[ordinal] = BinaryPrimitives.ReadUInt64LittleEndian(
                bytes.Slice(offset, AnnounceNumberOptionLength));
            offset += AnnounceNumberOptionLength;
        }

        draft = new FlatPromptAnnounceNumberWireDraftV1(
            actingPlayer,
            values);
        return true;
    }

    private static bool TryReadSelectCardEntry(
        ReadOnlySpan<byte> bytes,
        ref int offset,
        byte actingPlayer,
        bool allowCodeOnlyPlaceholder,
        out FlatPromptSelectCardWireEntryV1 entry,
        out FlatPromptErrorCodeV1 error)
    {
        entry = default;
        error = FlatPromptErrorCodeV1.None;
        uint sourceCardCode = BinaryPrimitives.ReadUInt32LittleEndian(
            bytes.Slice(offset, sizeof(uint)));
        ReadOnlySpan<byte> locationBytes = bytes.Slice(
            offset + sizeof(uint),
            GameplayWirePrimitivesV1.ModernLocInfoByteLength);
        ModernLocInfoV1 location;
        if (allowCodeOnlyPlaceholder &&
            IsSelectCardCodeOnlyLocation(locationBytes, actingPlayer))
        {
            location = new ModernLocInfoV1(0, 0, 0, 0);
        }
        else
        {
            if (!GameplayWirePrimitivesV1.TryDecodeModernLocInfo(
                    locationBytes,
                    out location,
                    out GameplayErrorCode locationError))
            {
                error = FlatPromptErrorCodeV1.MalformedPrompt;
                return false;
            }

            if (!TryMapLocationError(locationError, out error) ||
                !TryValidatePrivateLocation(location, out error))
            {
                return false;
            }
        }

        entry = new FlatPromptSelectCardWireEntryV1(
            sourceCardCode,
            location);
        offset += SelectCardEntryLength;
        return true;
    }

    private static bool TryReadSelectCardEntries(
        ReadOnlySpan<byte> bytes,
        ref int offset,
        byte actingPlayer,
        bool allowCodeOnlyPlaceholder,
        FlatPromptSelectCardWireEntryV1[] entries,
        out FlatPromptErrorCodeV1 error)
    {
        error = FlatPromptErrorCodeV1.None;
        foreach (int ordinal in Enumerable.Range(0, entries.Length))
        {
            if (!TryReadSelectCardEntry(
                    bytes,
                    ref offset,
                    actingPlayer,
                    allowCodeOnlyPlaceholder,
                    out entries[ordinal],
                    out error))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSelectCardCodeOnlyLocation(
        ReadOnlySpan<byte> bytes,
        byte actingPlayer)
    {
        if (bytes.Length != GameplayWirePrimitivesV1.ModernLocInfoByteLength ||
            bytes[0] != actingPlayer)
        {
            return false;
        }

        for (int index = 1; index < bytes.Length; index++)
        {
            if (bytes[index] != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsZeroModernLocInfo(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != GameplayWirePrimitivesV1.ModernLocInfoByteLength)
        {
            return false;
        }

        foreach (byte value in bytes)
        {
            if (value != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsZeroModernLocInfo(ModernLocInfoV1 value) =>
        value.Controller == 0 &&
        value.Location == 0 &&
        value.Sequence == 0 &&
        value.Position == 0;

    private static bool TryParseEffectYnWireDraft(
        ReadOnlySpan<byte> bytes,
        out FlatPromptWireDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = FlatPromptErrorCodeV1.None;
        if (bytes.Length != EffectYnMessageLength)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        if (bytes[1] > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidParticipant;
            return false;
        }

        if (!GameplayWirePrimitivesV1.TryDecodeModernLocInfo(
                bytes.Slice(6, GameplayWirePrimitivesV1.ModernLocInfoByteLength),
                out ModernLocInfoV1 location,
                out GameplayErrorCode locationError) ||
            !TryMapLocationError(locationError, out error) ||
            !TryValidatePrivateLocation(location, out error))
        {
            return false;
        }

        draft = new FlatPromptEffectYnWireDraftV1(
            bytes[1],
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(2, 4)),
            location,
            BinaryPrimitives.ReadUInt64LittleEndian(bytes.Slice(16, 8)));
        return true;
    }

    private static bool TryParseBattleWireDraft(
        ReadOnlySpan<byte> bytes,
        out FlatPromptWireDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = FlatPromptErrorCodeV1.None;
        if (IsKnownBattleLegacyLayout(bytes))
        {
            error = FlatPromptErrorCodeV1.UnsupportedPromptLayout;
            return false;
        }

        if (bytes.Length < BattleHeaderLength)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        byte actingPlayer = bytes[1];
        if (actingPlayer > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidParticipant;
            return false;
        }

        uint activatableCount =
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(2, 4));
        if (activatableCount > MaximumSectionEntryCount)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        ulong attackableCountOffset = checked(
            (ulong)(sizeof(byte) + sizeof(byte) + sizeof(uint)) +
            checked((ulong)BattleActivatableEntryLength * activatableCount));
        if (attackableCountOffset > int.MaxValue ||
            bytes.Length - (int)attackableCountOffset < sizeof(uint))
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        int attackableCountOffsetInt = (int)attackableCountOffset;
        uint attackableCount = BinaryPrimitives.ReadUInt32LittleEndian(
            bytes.Slice(attackableCountOffsetInt, sizeof(uint)));
        if (attackableCount > MaximumSectionEntryCount)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        ulong requiredLength;
        try
        {
            requiredLength = checked(
                (ulong)BattleHeaderLength +
                checked(
                    (ulong)BattleActivatableEntryLength *
                    activatableCount) +
                checked(
                    (ulong)BattleAttackableEntryLength *
                    attackableCount));
        }
        catch (OverflowException)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (requiredLength > int.MaxValue)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (requiredLength != (ulong)bytes.Length)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        FlatPromptBattleActivatableWireEntryV1[] activatableEntries =
            new FlatPromptBattleActivatableWireEntryV1[(int)activatableCount];
        int offset = sizeof(byte) + sizeof(byte) + sizeof(uint);
        for (int ordinal = 0; ordinal < activatableEntries.Length; ordinal++)
        {
            uint sourceCardCode = BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(offset, sizeof(uint)));
            byte controller = bytes[offset + 4];
            byte location = bytes[offset + 5];
            uint sequence = BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(offset + 6, sizeof(uint)));
            if (!TryValidateSourceFields(
                    controller,
                    location,
                    sequence,
                    out error))
            {
                return false;
            }

            activatableEntries[ordinal] =
                new FlatPromptBattleActivatableWireEntryV1(
                    sourceCardCode,
                    controller,
                    location,
                    sequence,
                    BinaryPrimitives.ReadUInt64LittleEndian(
                        bytes.Slice(offset + 10, sizeof(ulong))),
                    bytes[offset + 18]);
            if (activatableEntries[ordinal].ClientMode > 2)
            {
                error = FlatPromptErrorCodeV1.InvalidClientMode;
                return false;
            }

            offset += BattleActivatableEntryLength;
        }

        offset += sizeof(uint);
        FlatPromptBattleAttackableWireEntryV1[] attackableEntries =
            new FlatPromptBattleAttackableWireEntryV1[(int)attackableCount];
        for (int ordinal = 0; ordinal < attackableEntries.Length; ordinal++)
        {
            uint sourceCardCode = BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(offset, sizeof(uint)));
            byte controller = bytes[offset + 4];
            byte location = bytes[offset + 5];
            byte sequence = bytes[offset + 6];
            if (!TryValidateSourceFields(
                    controller,
                    location,
                    sequence,
                    out error))
            {
                return false;
            }

            byte directAttackable = bytes[offset + 7];
            if (directAttackable > 1)
            {
                error = FlatPromptErrorCodeV1.InvalidBoolean;
                return false;
            }

            attackableEntries[ordinal] =
                new FlatPromptBattleAttackableWireEntryV1(
                    sourceCardCode,
                    controller,
                    location,
                    sequence,
                    directAttackable == 1);
            offset += BattleAttackableEntryLength;
        }

        byte toMainPhase2 = bytes[offset];
        byte toEndPhase = bytes[offset + 1];
        if (toMainPhase2 > 1 || toEndPhase > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidBoolean;
            return false;
        }

        draft = new FlatPromptBattleWireDraftV1(
            actingPlayer,
            activatableEntries,
            attackableEntries,
            toMainPhase2 == 1,
            toEndPhase == 1);
        return true;
    }

    private static bool TryParseIdleWireDraft(
        ReadOnlySpan<byte> bytes,
        out FlatPromptWireDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = FlatPromptErrorCodeV1.None;
        if (IsKnownIdleLegacyLayout(bytes))
        {
            error = FlatPromptErrorCodeV1.UnsupportedPromptLayout;
            return false;
        }

        if (bytes.Length < sizeof(byte) + sizeof(byte))
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        byte actingPlayer = bytes[1];
        if (actingPlayer > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidParticipant;
            return false;
        }

        if (bytes.Length < IdleHeaderLength)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        int scanOffset = sizeof(byte) + sizeof(byte);
        if (!TryReadAndSkipIdleSection(
                bytes,
                ref scanOffset,
                IdleCardEntryLength,
                out uint summonCount,
                out error) ||
            !TryReadAndSkipIdleSection(
                bytes,
                ref scanOffset,
                IdleCardEntryLength,
                out uint specialSummonCount,
                out error) ||
            !TryReadAndSkipIdleSection(
                bytes,
                ref scanOffset,
                IdleRepositionEntryLength,
                out uint repositionCount,
                out error) ||
            !TryReadAndSkipIdleSection(
                bytes,
                ref scanOffset,
                IdleCardEntryLength,
                out uint monsterSetCount,
                out error) ||
            !TryReadAndSkipIdleSection(
                bytes,
                ref scanOffset,
                IdleCardEntryLength,
                out uint spellTrapSetCount,
                out error) ||
            !TryReadAndSkipIdleSection(
                bytes,
                ref scanOffset,
                IdleActivatableEntryLength,
                out uint activatableCount,
                out error))
        {
            return false;
        }

        if (bytes.Length - scanOffset != 3)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        FlatPromptIdleCardWireEntryV1[] summonEntries =
            new FlatPromptIdleCardWireEntryV1[(int)summonCount];
        FlatPromptIdleCardWireEntryV1[] specialSummonEntries =
            new FlatPromptIdleCardWireEntryV1[(int)specialSummonCount];
        FlatPromptIdleRepositionWireEntryV1[] repositionEntries =
            new FlatPromptIdleRepositionWireEntryV1[(int)repositionCount];
        FlatPromptIdleCardWireEntryV1[] monsterSetEntries =
            new FlatPromptIdleCardWireEntryV1[(int)monsterSetCount];
        FlatPromptIdleCardWireEntryV1[] spellTrapSetEntries =
            new FlatPromptIdleCardWireEntryV1[(int)spellTrapSetCount];
        FlatPromptIdleActivatableWireEntryV1[] activatableEntries =
            new FlatPromptIdleActivatableWireEntryV1[(int)activatableCount];
        int offset = sizeof(byte) + sizeof(byte);
        offset += sizeof(uint);
        if (!TryReadIdleCardEntries(
                bytes,
                ref offset,
                summonEntries,
                out error) ||
            !TryAdvanceIdleCount(ref offset) ||
            !TryReadIdleCardEntries(
                bytes,
                ref offset,
                specialSummonEntries,
                out error) ||
            !TryAdvanceIdleCount(ref offset) ||
            !TryReadIdleRepositionEntries(
                bytes,
                ref offset,
                repositionEntries,
                out error) ||
            !TryAdvanceIdleCount(ref offset) ||
            !TryReadIdleCardEntries(
                bytes,
                ref offset,
                monsterSetEntries,
                out error) ||
            !TryAdvanceIdleCount(ref offset) ||
            !TryReadIdleCardEntries(
                bytes,
                ref offset,
                spellTrapSetEntries,
                out error) ||
            !TryAdvanceIdleCount(ref offset) ||
            !TryReadIdleActivatableEntries(
                bytes,
                ref offset,
                activatableEntries,
                out error))
        {
            return false;
        }

        byte toBattlePhase = bytes[offset];
        byte toEndPhase = bytes[offset + 1];
        byte shuffleHand = bytes[offset + 2];
        if (toBattlePhase > 1 || toEndPhase > 1 || shuffleHand > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidBoolean;
            return false;
        }

        draft = new FlatPromptIdleWireDraftV1(
            actingPlayer,
            summonEntries,
            specialSummonEntries,
            repositionEntries,
            monsterSetEntries,
            spellTrapSetEntries,
            activatableEntries,
            toBattlePhase == 1,
            toEndPhase == 1,
            shuffleHand == 1);
        return true;
    }

    private static bool TryAdvanceIdleCount(ref int offset)
    {
        if (offset > int.MaxValue - sizeof(uint))
        {
            return false;
        }

        offset += sizeof(uint);
        return true;
    }

    private static bool TryReadAndSkipIdleSection(
        ReadOnlySpan<byte> bytes,
        ref int offset,
        int entryLength,
        out uint count,
        out FlatPromptErrorCodeV1 error)
    {
        count = default;
        error = FlatPromptErrorCodeV1.None;
        if (bytes.Length - offset < sizeof(uint))
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        count = BinaryPrimitives.ReadUInt32LittleEndian(
            bytes.Slice(offset, sizeof(uint)));
        if (count > MaximumSectionEntryCount)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        ulong end;
        try
        {
            end = checked(
                (ulong)offset +
                (ulong)sizeof(uint) +
                checked((ulong)entryLength * count));
        }
        catch (OverflowException)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (end > (ulong)bytes.Length)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        offset = (int)end;
        return true;
    }

    private static bool TryReadIdleCardEntries(
        ReadOnlySpan<byte> bytes,
        ref int offset,
        FlatPromptIdleCardWireEntryV1[] entries,
        out FlatPromptErrorCodeV1 error)
    {
        error = FlatPromptErrorCodeV1.None;
        foreach (int ordinal in Enumerable.Range(0, entries.Length))
        {
            uint sourceCardCode = BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(offset, sizeof(uint)));
            byte controller = bytes[offset + 4];
            byte location = bytes[offset + 5];
            uint sequence = BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(offset + 6, sizeof(uint)));
            if (!TryValidateSourceFields(
                    controller,
                    location,
                    sequence,
                    out error))
            {
                return false;
            }

            entries[ordinal] = new FlatPromptIdleCardWireEntryV1(
                sourceCardCode,
                controller,
                location,
                sequence);
            offset += IdleCardEntryLength;
        }

        return true;
    }

    private static bool TryReadIdleRepositionEntries(
        ReadOnlySpan<byte> bytes,
        ref int offset,
        FlatPromptIdleRepositionWireEntryV1[] entries,
        out FlatPromptErrorCodeV1 error)
    {
        error = FlatPromptErrorCodeV1.None;
        foreach (int ordinal in Enumerable.Range(0, entries.Length))
        {
            uint sourceCardCode = BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(offset, sizeof(uint)));
            byte controller = bytes[offset + 4];
            byte location = bytes[offset + 5];
            byte sequence = bytes[offset + 6];
            if (!TryValidateSourceFields(
                    controller,
                    location,
                    sequence,
                    out error))
            {
                return false;
            }

            entries[ordinal] = new FlatPromptIdleRepositionWireEntryV1(
                sourceCardCode,
                controller,
                location,
                sequence);
            offset += IdleRepositionEntryLength;
        }

        return true;
    }

    private static bool TryReadIdleActivatableEntries(
        ReadOnlySpan<byte> bytes,
        ref int offset,
        FlatPromptIdleActivatableWireEntryV1[] entries,
        out FlatPromptErrorCodeV1 error)
    {
        error = FlatPromptErrorCodeV1.None;
        foreach (int ordinal in Enumerable.Range(0, entries.Length))
        {
            uint sourceCardCode = BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(offset, sizeof(uint)));
            byte controller = bytes[offset + 4];
            byte location = bytes[offset + 5];
            uint sequence = BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(offset + 6, sizeof(uint)));
            if (!TryValidateSourceFields(
                    controller,
                    location,
                    sequence,
                    out error))
            {
                return false;
            }

            byte clientMode = bytes[offset + 18];
            if (clientMode > 2)
            {
                error = FlatPromptErrorCodeV1.InvalidClientMode;
                return false;
            }

            entries[ordinal] = new FlatPromptIdleActivatableWireEntryV1(
                sourceCardCode,
                controller,
                location,
                sequence,
                BinaryPrimitives.ReadUInt64LittleEndian(
                    bytes.Slice(offset + 10, sizeof(ulong))),
                clientMode);
            offset += IdleActivatableEntryLength;
        }

        return true;
    }

    private static bool TryValidateSourceFields(
        byte controller,
        byte location,
        uint sequence,
        out FlatPromptErrorCodeV1 error)
    {
        if (controller > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidParticipant;
            return false;
        }

        ModernLocInfoV1 value = new(controller, location, sequence, 0);
        if (MirrorAddressNormalizationV1.TryNormalize(
                value,
                out _,
                out GameplayErrorCode normalizationError))
        {
            error = FlatPromptErrorCodeV1.None;
            return true;
        }

        return TryMapLocationError(normalizationError, out error);
    }

    private static bool IsKnownBattleLegacyLayout(
        ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != BattleHeaderLength - 1 ||
            bytes[0] != 10 ||
            bytes[1] > 1)
        {
            return false;
        }

        for (int index = 2; index < bytes.Length; index++)
        {
            if (bytes[index] != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsKnownIdleLegacyLayout(
        ReadOnlySpan<byte> bytes)
    {
        const int legacyZeroEntryLength = 11;
        if (bytes.Length != legacyZeroEntryLength ||
            bytes[0] != 11 ||
            bytes[1] > 1)
        {
            return false;
        }

        for (int index = 2; index < 8; index++)
        {
            if (bytes[index] != 0)
            {
                return false;
            }
        }

        return bytes[8] <= 1 && bytes[9] <= 1 && bytes[10] <= 1;
    }

    private static bool TryParseChainWireDraft(
        ReadOnlySpan<byte> bytes,
        out FlatPromptWireDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = FlatPromptErrorCodeV1.None;
        if (bytes.Length < ChainHeaderLength)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        byte actingPlayer = bytes[1];
        if (actingPlayer > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidParticipant;
            return false;
        }

        byte speCount = bytes[2];
        byte forcedByte = bytes[3];
        if (forcedByte > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidBoolean;
            return false;
        }

        uint hintTimingForPlayer =
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(4, 4));
        uint hintTimingForOtherPlayer =
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(8, 4));
        uint chainCount =
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(12, 4));
        ulong requiredLength;
        try
        {
            requiredLength = checked(
                (ulong)ChainHeaderLength +
                checked((ulong)ChainEntryLength * chainCount));
        }
        catch (OverflowException)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (requiredLength > int.MaxValue)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (requiredLength != (ulong)bytes.Length)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        FlatPromptChainWireEntryV1[] entries =
            new FlatPromptChainWireEntryV1[(int)chainCount];
        int offset = ChainHeaderLength;
        for (int ordinal = 0; ordinal < entries.Length; ordinal++)
        {
            uint sourceCardCode =
                BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset, 4));
            if (!GameplayWirePrimitivesV1.TryDecodeModernLocInfo(
                    bytes.Slice(
                        offset + 4,
                        GameplayWirePrimitivesV1.ModernLocInfoByteLength),
                    out ModernLocInfoV1 location,
                    out GameplayErrorCode locationError) ||
                !TryMapLocationError(locationError, out error) ||
                !TryValidatePrivateLocation(location, out error))
            {
                return false;
            }

            byte clientMode = bytes[offset + 22];
            if (clientMode > 2)
            {
                error = FlatPromptErrorCodeV1.InvalidClientMode;
                return false;
            }

            entries[ordinal] = new FlatPromptChainWireEntryV1(
                sourceCardCode,
                location,
                BinaryPrimitives.ReadUInt64LittleEndian(
                    bytes.Slice(offset + 14, 8)),
                clientMode);
            offset += ChainEntryLength;
        }

        draft = new FlatPromptChainWireDraftV1(
            actingPlayer,
            speCount,
            forcedByte == 1,
            hintTimingForPlayer,
            hintTimingForOtherPlayer,
            entries);
        return true;
    }

    private static bool TryBuildSelectCardProjection(
        FlatPromptSelectCardWireDraftV1 wire,
        FlatPromptCardAuthorityContextV1? authority,
        out FlatPromptProjectionDraftV1? projected,
        out FlatPromptErrorCodeV1 error)
    {
        projected = null;
        error = FlatPromptErrorCodeV1.None;
        FlatPublicCandidateDescriptorV1[] sourceCandidates =
            new FlatPublicCandidateDescriptorV1[wire.Entries.Count];
        for (int ordinal = 0; ordinal < wire.Entries.Count; ordinal++)
        {
            FlatPromptSelectCardWireEntryV1 entry = wire.Entries[ordinal];
            if (!TryCreateI5CardCandidate(
                    FlatPromptFamilyValueV1.MsgSelectCard,
                    FlatPromptChoiceKindV1.Pick,
                    FlatPromptSourceSectionV1.SelectCard,
                    FlatPromptKeyV1.SelectCardPickPrefix,
                    ordinal,
                    entry.SourceCardCode,
                    entry.SourceLocation,
                    authority,
                    out FlatPublicCandidateDescriptorV1? candidate,
                    out error) ||
                candidate is null)
            {
                if (error == FlatPromptErrorCodeV1.None)
                {
                    error = FlatPromptErrorCodeV1.InvalidResponseBinding;
                }

                return false;
            }

            sourceCandidates[ordinal] = candidate;
        }

        FlatPromptCardContinuationStateV1 state;
        try
        {
            state = new FlatPromptCardContinuationStateV1(
                FlatPromptFamilyValueV1.MsgSelectCard,
                wire.ActingPlayer,
                wire.MinimumCount,
                wire.MaximumCount,
                wire.Cancelable,
                sourceCandidates,
                new byte[sourceCandidates.Length],
                Array.Empty<int>(),
                0);
        }
        catch (ArgumentException)
        {
            error = FlatPromptErrorCodeV1.UnprovenCandidateDomain;
            return false;
        }

        return TryBuildCardContinuationProjection(
            state,
            out projected,
            out error);
    }

    private static bool TryBuildSelectTributeProjection(
        FlatPromptSelectTributeWireDraftV1 wire,
        FlatPromptCardAuthorityContextV1? authority,
        out FlatPromptProjectionDraftV1? projected,
        out FlatPromptErrorCodeV1 error)
    {
        projected = null;
        error = FlatPromptErrorCodeV1.None;
        FlatPublicCandidateDescriptorV1[] sourceCandidates =
            new FlatPublicCandidateDescriptorV1[wire.Entries.Count];
        byte[] releaseValues = new byte[wire.Entries.Count];
        for (int ordinal = 0; ordinal < wire.Entries.Count; ordinal++)
        {
            FlatPromptSelectTributeWireEntryV1 entry = wire.Entries[ordinal];
            if ((entry.SourceLocation.Location & 0x80) != 0)
            {
                error = FlatPromptErrorCodeV1.UnprovenPublicReference;
                return false;
            }

            if (!TryCreateI5CardCandidate(
                    FlatPromptFamilyValueV1.MsgSelectTribute,
                    FlatPromptChoiceKindV1.Pick,
                    FlatPromptSourceSectionV1.SelectTribute,
                    FlatPromptKeyV1.SelectTributePickPrefix,
                    ordinal,
                    entry.SourceCardCode,
                    entry.SourceLocation,
                    authority,
                    out FlatPublicCandidateDescriptorV1? candidate,
                    out error) ||
                candidate is null)
            {
                if (error == FlatPromptErrorCodeV1.None)
                {
                    error = FlatPromptErrorCodeV1.InvalidResponseBinding;
                }

                return false;
            }

            sourceCandidates[ordinal] = candidate;

            releaseValues[ordinal] = entry.ReleaseValue;
        }

        FlatPromptCardContinuationStateV1 state;
        try
        {
            state = new FlatPromptCardContinuationStateV1(
                FlatPromptFamilyValueV1.MsgSelectTribute,
                wire.ActingPlayer,
                wire.MinimumRequiredTributeValue,
                wire.MaximumSelectedCardCount,
                wire.Cancelable,
                sourceCandidates,
                releaseValues,
                Array.Empty<int>(),
                0);
        }
        catch (ArgumentException)
        {
            error = FlatPromptErrorCodeV1.UnprovenCandidateDomain;
            return false;
        }

        return TryBuildCardContinuationProjection(
            state,
            out projected,
            out error);
    }

    private static bool TryBuildSelectUnselectProjection(
        FlatPromptSelectUnselectWireDraftV1 wire,
        FlatPromptCardAuthorityContextV1? authority,
        out FlatPromptProjectionDraftV1? projected,
        out FlatPromptErrorCodeV1 error)
    {
        projected = null;
        error = FlatPromptErrorCodeV1.None;
        List<FlatPublicCandidateDescriptorV1> candidates = new();
        List<string> keys = new();
        List<int> responses = new();
        List<byte[]> responseBodies = new();
        if (!TryAppendSelectUnselectCards(
                wire.SelectableEntries,
                FlatPromptChoiceKindV1.Select,
                FlatPromptSourceSectionV1.Selectable,
                FlatPromptKeyV1.SelectUnselectSelectPrefix,
                0,
                authority,
                candidates,
                keys,
                responses,
                responseBodies,
                out error) ||
            !TryAppendSelectUnselectCards(
                wire.UnselectableEntries,
                FlatPromptChoiceKindV1.Unselect,
                FlatPromptSourceSectionV1.Unselectable,
                FlatPromptKeyV1.SelectUnselectUnselectPrefix,
                wire.SelectableEntries.Count,
                authority,
                candidates,
                keys,
                responses,
                responseBodies,
                out error))
        {
            return false;
        }

        if (wire.Finishable && wire.Cancelable)
        {
            candidates.Add(new FlatPromptFinishOrCancelPublicCandidateV1(
                FlatPromptKeyV1.SelectUnselectFinishOrCancel));
            keys.Add(FlatPromptKeyV1.SelectUnselectFinishOrCancel);
            responses.Add(-1);
            responseBodies.Add(CreateInt32Response(-1));
        }
        else if (wire.Finishable)
        {
            candidates.Add(new FlatPromptFinishPublicCandidateV1(
                FlatPromptKeyV1.SelectUnselectFinish));
            keys.Add(FlatPromptKeyV1.SelectUnselectFinish);
            responses.Add(-1);
            responseBodies.Add(CreateInt32Response(-1));
        }
        else if (wire.Cancelable)
        {
            candidates.Add(new FlatPromptCancelPublicCandidateV1(
                FlatPromptKeyV1.SelectUnselectCancel));
            keys.Add(FlatPromptKeyV1.SelectUnselectCancel);
            responses.Add(-1);
            responseBodies.Add(CreateInt32Response(-1));
        }

        if (candidates.Count == 0)
        {
            error = FlatPromptErrorCodeV1.ZeroOptionDomain;
            return false;
        }

        projected = new FlatPromptProjectionDraftV1(
            new FlatPromptSelectUnselectCardPublicContextV1(
                wire.ActingPlayer,
                wire.Finishable,
                wire.Cancelable,
                wire.MinimumCount,
                wire.MaximumCount,
                wire.SelectableEntries.Count,
                wire.UnselectableEntries.Count),
            candidates,
            keys,
            responses,
            responseBodies: responseBodies);
        return true;
    }

    private static bool TryBuildAnnounceNumberProjection(
        FlatPromptAnnounceNumberWireDraftV1 wire,
        out FlatPromptProjectionDraftV1? projected,
        out FlatPromptErrorCodeV1 error)
    {
        projected = null;
        error = FlatPromptErrorCodeV1.None;
        List<FlatPublicCandidateDescriptorV1> candidates = new();
        List<string> keys = new();
        List<int> responses = new();
        List<byte[]> responseBodies = new();
        for (int ordinal = 0; ordinal < wire.Values.Count; ordinal++)
        {
            if (!FlatPromptKeyV1.TryCreateOrdinalKey(
                    FlatPromptKeyV1.AnnounceNumberOptionPrefix,
                    ordinal,
                    out string key))
            {
                error = FlatPromptErrorCodeV1.InvalidI4LocalCandidateKey;
                return false;
            }

            candidates.Add(new FlatPromptAnnounceNumberPublicCandidateV1(
                key,
                ordinal,
                wire.Values[ordinal]));
            keys.Add(key);
            responses.Add(ordinal);
            responseBodies.Add(CreateInt32Response(ordinal));
        }

        projected = new FlatPromptProjectionDraftV1(
            new FlatPromptAnnounceNumberPublicContextV1(
                wire.ActingPlayer,
                wire.Values.Count),
            candidates,
            keys,
            responses,
            responseBodies: responseBodies);
        return true;
    }

    private static bool TryAppendSelectUnselectCards(
        IReadOnlyList<FlatPromptSelectCardWireEntryV1> entries,
        FlatPromptChoiceKindV1 choiceKind,
        FlatPromptSourceSectionV1 sourceSection,
        string keyPrefix,
        int responseOffset,
        FlatPromptCardAuthorityContextV1? authority,
        List<FlatPublicCandidateDescriptorV1> candidates,
        List<string> keys,
        List<int> responses,
        List<byte[]> responseBodies,
        out FlatPromptErrorCodeV1 error)
    {
        error = FlatPromptErrorCodeV1.None;
        for (int ordinal = 0; ordinal < entries.Count; ordinal++)
        {
            FlatPromptSelectCardWireEntryV1 entry = entries[ordinal];
            if (!FlatPromptKeyV1.TryCreateOrdinalKey(
                    keyPrefix,
                    ordinal,
                    out string key) ||
                !TryCreateI5CardCandidate(
                    FlatPromptFamilyValueV1.MsgSelectUnselectCard,
                    choiceKind,
                    sourceSection,
                    keyPrefix,
                    ordinal,
                    entry.SourceCardCode,
                    entry.SourceLocation,
                    authority,
                    out FlatPublicCandidateDescriptorV1? candidate,
                    out error) ||
                candidate is null)
            {
                if (error == FlatPromptErrorCodeV1.None)
                {
                    error = FlatPromptErrorCodeV1.InvalidI4LocalCandidateKey;
                }

                return false;
            }

            int combinedIndex;
            try
            {
                combinedIndex = checked(responseOffset + ordinal);
            }
            catch (OverflowException)
            {
                error = FlatPromptErrorCodeV1.ArithmeticFailure;
                return false;
            }

            candidates.Add(candidate);
            keys.Add(key);
            responses.Add(combinedIndex);
            responseBodies.Add(CreateSelectUnselectResponse(combinedIndex));
        }

        return true;
    }

    private static bool TryCreateI5CardCandidate(
        FlatPromptFamilyV1 family,
        FlatPromptChoiceKindV1 choiceKind,
        FlatPromptSourceSectionV1 sourceSection,
        string keyPrefix,
        int sourceOrdinal,
        uint sourceCardCode,
        ModernLocInfoV1 sourceLocation,
        FlatPromptCardAuthorityContextV1? authority,
        out FlatPublicCandidateDescriptorV1? candidate,
        out FlatPromptErrorCodeV1 error)
    {
        candidate = null;
        error = FlatPromptErrorCodeV1.None;
        if (!FlatPromptKeyV1.TryCreateOrdinalKey(
                keyPrefix,
                sourceOrdinal,
                out string key))
        {
            error = FlatPromptErrorCodeV1.InvalidI4LocalCandidateKey;
            return false;
        }

        PublicSemanticLocatorV1? locator = null;
        bool addressed = !IsZeroModernLocInfo(sourceLocation);
        bool correlationRequired =
            family == FlatPromptFamilyValueV1.MsgSelectUnselectCard &&
            addressed;
        if (correlationRequired && authority is null)
        {
            error = FlatPromptErrorCodeV1.UnprovenPublicReference;
            return false;
        }

        if (addressed && authority is not null)
        {
            bool correlated = FlatPromptCardCorrelationV1.TryCorrelate(
                authority.CapturedMirror,
                authority.AcceptedSnapshot,
                sourceCardCode,
                sourceLocation,
                out FlatPromptCardCorrelationResultV1? correlation,
                out FlatPromptErrorCodeV1 correlationError);
            if (correlationRequired &&
                (!correlated || correlation is null))
            {
                error = correlationError == FlatPromptErrorCodeV1.None
                    ? FlatPromptErrorCodeV1.UnprovenPublicReference
                    : correlationError;
                return false;
            }

            if (correlated && correlation is not null)
            {
                locator = correlation.AcceptedLocator;
            }
        }

        bool hasCode = sourceCardCode != 0;
        if (family == FlatPromptFamilyValueV1.MsgSelectCard)
        {
            candidate = CreateSelectCardCandidate(
                key,
                sourceOrdinal,
                locator,
                sourceCardCode,
                hasCode);
            return true;
        }

        if (family == FlatPromptFamilyValueV1.MsgSelectTribute)
        {
            candidate = CreateTributeCandidate(
                key,
                sourceOrdinal,
                locator,
                sourceCardCode,
                hasCode);
            return true;
        }

        if (family == FlatPromptFamilyValueV1.MsgSelectUnselectCard)
        {
            candidate = CreateSelectUnselectCandidate(
                key,
                choiceKind,
                sourceSection,
                sourceOrdinal,
                locator,
                sourceCardCode,
                hasCode);
            return true;
        }

        error = FlatPromptErrorCodeV1.InvalidResponseBinding;
        return false;
    }

    private static FlatPublicCandidateDescriptorV1 CreateSelectCardCandidate(
        string key,
        int ordinal,
        PublicSemanticLocatorV1? locator,
        uint sourceCardCode,
        bool hasCode) =>
        locator is not null
            ? hasCode
                ? new FlatPromptCardSelectionLocatorPromptCodeCandidateV1(
                    key,
                    ordinal,
                    locator,
                    sourceCardCode)
                : new FlatPromptCardSelectionLocatorCandidateV1(
                    key,
                    ordinal,
                    locator)
            : hasCode
                ? new FlatPromptCardSelectionPromptCodeCandidateV1(
                    key,
                    ordinal,
                    sourceCardCode)
                : new FlatPromptCardSelectionAnonymousCandidateV1(
                    key,
                    ordinal);

    private static FlatPublicCandidateDescriptorV1 CreateTributeCandidate(
        string key,
        int ordinal,
        PublicSemanticLocatorV1? locator,
        uint sourceCardCode,
        bool hasCode) =>
        locator is not null
            ? hasCode
                ? new FlatPromptTributeSelectionLocatorPromptCodeCandidateV1(
                    key,
                    ordinal,
                    locator,
                    sourceCardCode)
                : new FlatPromptTributeSelectionLocatorCandidateV1(
                    key,
                    ordinal,
                    locator)
            : hasCode
                ? new FlatPromptTributeSelectionPromptCodeCandidateV1(
                    key,
                    ordinal,
                    sourceCardCode)
                : new FlatPromptTributeSelectionAnonymousCandidateV1(
                    key,
                    ordinal);

    private static FlatPublicCandidateDescriptorV1 CreateSelectUnselectCandidate(
        string key,
        FlatPromptChoiceKindV1 choiceKind,
        FlatPromptSourceSectionV1 sourceSection,
        int ordinal,
        PublicSemanticLocatorV1? locator,
        uint sourceCardCode,
        bool hasCode) =>
        locator is not null
            ? hasCode
                ? new FlatPromptSelectUnselectLocatorPromptCodeCandidateV1(
                    key,
                    choiceKind,
                    sourceSection,
                    ordinal,
                    locator,
                    sourceCardCode)
                : new FlatPromptSelectUnselectLocatorCandidateV1(
                    key,
                    choiceKind,
                    sourceSection,
                    ordinal,
                    locator)
            : hasCode
                ? new FlatPromptSelectUnselectPromptCodeCandidateV1(
                    key,
                    choiceKind,
                    sourceSection,
                    ordinal,
                    sourceCardCode)
                : new FlatPromptSelectUnselectAnonymousCandidateV1(
                    key,
                    choiceKind,
                    sourceSection,
                    ordinal);

    private static bool TryBuildCardContinuationProjection(
        FlatPromptCardContinuationStateV1 state,
        out FlatPromptProjectionDraftV1? projected,
        out FlatPromptErrorCodeV1 error)
    {
        projected = null;
        error = FlatPromptErrorCodeV1.None;
        List<FlatPublicCandidateDescriptorV1> candidates = new();
        List<string> keys = new();
        List<int> responses = new();
        string pickPrefix = state.Family == FlatPromptFamilyValueV1.MsgSelectCard
            ? FlatPromptKeyV1.SelectCardPickPrefix
            : FlatPromptKeyV1.SelectTributePickPrefix;
        for (int ordinal = state.LastSelectedOrdinal + 1;
             ordinal < state.SourceCandidates.Count;
             ordinal++)
        {
            if (!CanPick(state, ordinal))
            {
                continue;
            }

            FlatPublicCandidateDescriptorV1 candidate =
                state.SourceCandidates[ordinal];
            if (!FlatPromptKeyV1.TryCreateOrdinalKey(
                    pickPrefix,
                    ordinal,
                    out string key) ||
                !string.Equals(
                    candidate.I4LocalCandidateKey,
                    key,
                    StringComparison.Ordinal))
            {
                error = FlatPromptErrorCodeV1.InvalidResponseBinding;
                return false;
            }

            candidates.Add(candidate);
            keys.Add(key);
            responses.Add(ordinal);
        }

        if (CanFinish(state))
        {
            string key = state.Family == FlatPromptFamilyValueV1.MsgSelectCard
                ? FlatPromptKeyV1.SelectCardFinish
                : FlatPromptKeyV1.SelectTributeFinish;
            candidates.Add(new FlatPromptFinishPublicCandidateV1(key));
            keys.Add(key);
            responses.Add(-1);
        }

        if (state.Cancelable)
        {
            string key = state.Family == FlatPromptFamilyValueV1.MsgSelectCard
                ? FlatPromptKeyV1.SelectCardCancel
                : FlatPromptKeyV1.SelectTributeCancel;
            candidates.Add(new FlatPromptCancelPublicCandidateV1(key));
            keys.Add(key);
            responses.Add(-1);
        }

        if (candidates.Count == 0)
        {
            error = FlatPromptErrorCodeV1.UnprovenCandidateDomain;
            return false;
        }

        FlatPromptPublicContextV1 context =
            state.Family == FlatPromptFamilyValueV1.MsgSelectCard
                ? new FlatPromptCardSelectionPublicContextV1(
                    state.ActingPlayer,
                    state.Minimum,
                    state.Maximum,
                    state.Cancelable)
                : new FlatPromptTributeSelectionPublicContextV1(
                    state.ActingPlayer,
                    state.Minimum,
                    state.Maximum,
                    state.Cancelable);
        projected = new FlatPromptProjectionDraftV1(
            context,
            candidates,
            keys,
            responses,
            state);
        return true;
    }

    internal static bool TryAdvanceCardContinuation(
        FlatPromptCardContinuationStateV1 state,
        int sourceOrdinal,
        out FlatPromptProjectionDraftV1? projected,
        out FlatPromptErrorCodeV1 error)
    {
        projected = null;
        error = FlatPromptErrorCodeV1.None;
        if (!CanPick(state, sourceOrdinal))
        {
            error = FlatPromptErrorCodeV1.InvalidContinuationAction;
            return false;
        }

        try
        {
            return TryBuildCardContinuationProjection(
                state.WithSelected(sourceOrdinal),
                out projected,
                out error);
        }
        catch (OverflowException)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }
    }

    internal static bool TryEncodeCardIndexResponse(
        IReadOnlyList<int> selectedOrdinals,
        out byte[] responseBody,
        out FlatPromptErrorCodeV1 error)
    {
        responseBody = Array.Empty<byte>();
        error = FlatPromptErrorCodeV1.None;
        ArgumentNullException.ThrowIfNull(selectedOrdinals);
        int previousOrdinal = -1;
        int maximumOrdinal = 0;
        for (int index = 0; index < selectedOrdinals.Count; index++)
        {
            int ordinal = selectedOrdinals[index];
            if (ordinal < 0 || ordinal <= previousOrdinal)
            {
                error = FlatPromptErrorCodeV1.InvalidResponseBinding;
                return false;
            }

            previousOrdinal = ordinal;
            maximumOrdinal = ordinal;
        }

        ulong size = (ulong)selectedOrdinals.Count;
        int responseType;
        if (maximumOrdinal < byte.MaxValue &&
            (ulong)maximumOrdinal >= checked(size * 8))
        {
            responseType = 2;
        }
        else if (maximumOrdinal < ushort.MaxValue &&
                 (ulong)maximumOrdinal >= checked(size * 16))
        {
            responseType = 1;
        }
        else if ((ulong)maximumOrdinal >= checked(size * 32))
        {
            responseType = 0;
        }
        else
        {
            responseType = 3;
        }

        try
        {
            responseBody = responseType switch
            {
                2 => new byte[checked(sizeof(int) + sizeof(uint) +
                    selectedOrdinals.Count)],
                1 => new byte[checked(sizeof(int) + sizeof(uint) +
                    sizeof(ushort) * selectedOrdinals.Count)],
                0 => new byte[checked(sizeof(int) + sizeof(uint) +
                    sizeof(uint) * selectedOrdinals.Count)],
                _ => new byte[checked(sizeof(int) + sizeof(byte) +
                    maximumOrdinal / 8)]
            };
        }
        catch (OverflowException)
        {
            responseBody = Array.Empty<byte>();
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        BinaryPrimitives.WriteInt32LittleEndian(
            responseBody,
            responseType);
        switch (responseType)
        {
            case 2:
                BinaryPrimitives.WriteUInt32LittleEndian(
                    responseBody.AsSpan(sizeof(int)),
                    checked((uint)selectedOrdinals.Count));
                for (int index = 0; index < selectedOrdinals.Count; index++)
                {
                    responseBody[sizeof(int) + sizeof(uint) + index] =
                        checked((byte)selectedOrdinals[index]);
                }

                break;
            case 1:
                BinaryPrimitives.WriteUInt32LittleEndian(
                    responseBody.AsSpan(sizeof(int)),
                    checked((uint)selectedOrdinals.Count));
                for (int index = 0; index < selectedOrdinals.Count; index++)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(
                        responseBody.AsSpan(
                            sizeof(int) + sizeof(uint) +
                            sizeof(ushort) * index),
                        checked((ushort)selectedOrdinals[index]));
                }

                break;
            case 0:
                BinaryPrimitives.WriteUInt32LittleEndian(
                    responseBody.AsSpan(sizeof(int)),
                    checked((uint)selectedOrdinals.Count));
                for (int index = 0; index < selectedOrdinals.Count; index++)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(
                        responseBody.AsSpan(
                            sizeof(int) + sizeof(uint) +
                            sizeof(uint) * index),
                        checked((uint)selectedOrdinals[index]));
                }

                break;
            case 3:
                foreach (int ordinal in selectedOrdinals)
                {
                    int bit = checked(32 + ordinal);
                    int byteIndex = checked(bit / 8);
                    responseBody[byteIndex] |=
                        (byte)(1 << (bit % 8));
                }

                break;
        }

        return true;
    }

    private static bool CanPick(
        FlatPromptCardContinuationStateV1 state,
        int ordinal)
    {
        int selectedCount = state.SelectedOrdinals.Count;
        int afterCount = selectedCount + 1;
        if (ordinal < 0 ||
            ordinal >= state.SourceCandidates.Count ||
            ordinal <= state.LastSelectedOrdinal ||
            afterCount > state.Maximum)
        {
            return false;
        }

        if (state.Family == FlatPromptFamilyValueV1.MsgSelectCard)
        {
            int laterCount = state.SourceCandidates.Count - ordinal - 1;
            return (ulong)afterCount + (ulong)laterCount >= state.Minimum;
        }

        uint selectedValue;
        try
        {
            selectedValue = checked(
                state.SelectedTributeValue + state.ReleaseValues[ordinal]);
        }
        catch (OverflowException)
        {
            return false;
        }

        return HasTributeCompletion(
            state,
            ordinal + 1,
            afterCount,
            selectedValue);
    }

    private static bool CanFinish(FlatPromptCardContinuationStateV1 state)
    {
        if (state.SelectedOrdinals.Count > state.Maximum)
        {
            return false;
        }

        return state.Family == FlatPromptFamilyValueV1.MsgSelectCard
            ? (uint)state.SelectedOrdinals.Count >= state.Minimum
            : state.SelectedTributeValue >= state.Minimum;
    }

    private static bool HasTributeCompletion(
        FlatPromptCardContinuationStateV1 state,
        int startOrdinal,
        int selectedCount,
        uint selectedValue)
    {
        if (selectedValue >= state.Minimum)
        {
            return true;
        }

        if (selectedCount >= state.Maximum || state.Minimum > 15)
        {
            return false;
        }

        int target = checked((int)state.Minimum);
        int maximumAdditional = checked(
            (int)state.Maximum - selectedCount);
        bool[,] reachable = new bool[maximumAdditional + 1, target + 1];
        reachable[0, checked((int)selectedValue)] = true;
        for (int ordinal = startOrdinal;
             ordinal < state.ReleaseValues.Count;
             ordinal++)
        {
            int weight = state.ReleaseValues[ordinal];
            for (int used = maximumAdditional - 1; used >= 0; used--)
            {
                for (int sum = 0; sum <= target; sum++)
                {
                    if (!reachable[used, sum])
                    {
                        continue;
                    }

                    int next = Math.Min(target, checked(sum + weight));
                    reachable[used + 1, next] = true;
                }
            }
        }

        for (int used = 0; used <= maximumAdditional; used++)
        {
            if (reachable[used, target])
            {
                return true;
            }
        }

        return false;
    }

    private static byte[] CreateInt32Response(int value)
    {
        byte[] body = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(body, value);
        return body;
    }

    private static byte[] CreateSelectUnselectResponse(int combinedIndex)
    {
        byte[] body = new byte[sizeof(uint) * 2];
        BinaryPrimitives.WriteUInt32LittleEndian(body, 1);
        BinaryPrimitives.WriteUInt32LittleEndian(
            body.AsSpan(sizeof(uint)),
            checked((uint)combinedIndex));
        return body;
    }

    private static bool TryBuildEffectYnProjection(
        FlatPromptEffectYnWireDraftV1 wire,
        FlatPromptCardAuthorityContextV1 authority,
        out FlatPromptProjectionDraftV1? projected,
        out FlatPromptErrorCodeV1 error)
    {
        projected = null;
        error = FlatPromptErrorCodeV1.None;
        if (!FlatPromptCardCorrelationV1.TryCorrelate(
                authority.CapturedMirror,
                authority.AcceptedSnapshot,
                wire.SourceCardCode,
                wire.SourceLocation,
                out FlatPromptCardCorrelationResultV1? correlation,
                out error) ||
            correlation is null)
        {
            return false;
        }

        FlatPromptEffectYnPublicContextBaseV1 context =
            correlation.SafeCardCode.HasValue
                ? new FlatPromptEffectYnCardCodePublicContextV1(
                    wire.ActingPlayer,
                    correlation.AcceptedLocator,
                    wire.EffectDescriptionId,
                    correlation.SafeCardCode.Value)
                : new FlatPromptEffectYnPublicContextV1(
                    wire.ActingPlayer,
                    correlation.AcceptedLocator,
                    wire.EffectDescriptionId);
        FlatEffectYnPublicCandidateDescriptorV1[] candidates =
        {
            new(
                FlatPromptKeyV1.EffectYnNo,
                FlatPromptChoiceKindV1.No),
            new(
                FlatPromptKeyV1.EffectYnYes,
                FlatPromptChoiceKindV1.Yes)
        };
        projected = new FlatPromptProjectionDraftV1(
            context,
            candidates,
            new[] { FlatPromptKeyV1.EffectYnNo, FlatPromptKeyV1.EffectYnYes },
            EffectYnResponses);
        return true;
    }

    private static bool TryBuildChainProjection(
        FlatPromptChainWireDraftV1 wire,
        FlatPromptCardAuthorityContextV1 authority,
        out FlatPromptProjectionDraftV1? projected,
        out FlatPromptErrorCodeV1 error)
    {
        projected = null;
        error = FlatPromptErrorCodeV1.None;
        if (wire.Forced && wire.Entries.Count == 0)
        {
            error = FlatPromptErrorCodeV1.UnprovenCandidateDomain;
            return false;
        }

        List<FlatPublicCandidateDescriptorV1> candidates = new();
        List<string> keys = new();
        List<int> responses = new();
        for (int ordinal = 0; ordinal < wire.Entries.Count; ordinal++)
        {
            FlatPromptChainWireEntryV1 entry = wire.Entries[ordinal];
            if (!FlatPromptKeyV1.TryCreateChainEntry(
                    ordinal,
                    out string key) ||
                !FlatPromptCardCorrelationV1.TryCorrelate(
                    authority.CapturedMirror,
                    authority.AcceptedSnapshot,
                    entry.SourceCardCode,
                    entry.SourceLocation,
                    out FlatPromptCardCorrelationResultV1? correlation,
                    out error) ||
                correlation is null)
            {
                if (error == FlatPromptErrorCodeV1.None)
                {
                    error = FlatPromptErrorCodeV1.UnprovenPublicReference;
                }

                return false;
            }

            FlatChainEntryPublicCandidateDescriptorBaseV1 candidate =
                correlation.SafeCardCode.HasValue
                    ? new FlatChainCardCodePublicCandidateDescriptorV1(
                        key,
                        ordinal,
                        correlation.AcceptedLocator,
                        entry.DescriptionOrEffectId,
                        entry.ClientMode,
                        correlation.SafeCardCode.Value)
                    : new FlatChainPublicCandidateDescriptorV1(
                        key,
                        ordinal,
                        correlation.AcceptedLocator,
                        entry.DescriptionOrEffectId,
                        entry.ClientMode);
            candidates.Add(candidate);
            keys.Add(key);
            responses.Add(ordinal);
        }

        if (!wire.Forced)
        {
            candidates.Add(new FlatChainNoChainPublicCandidateDescriptorV1(
                FlatPromptKeyV1.ChainNoChain));
            keys.Add(FlatPromptKeyV1.ChainNoChain);
            responses.Add(-1);
        }

        if (candidates.Count == 0)
        {
            error = FlatPromptErrorCodeV1.UnprovenCandidateDomain;
            return false;
        }

        projected = new FlatPromptProjectionDraftV1(
            new FlatPromptChainPublicContextV1(
                wire.ActingPlayer,
                wire.SpeCount,
                wire.Forced,
                wire.HintTimingForPlayer,
                wire.HintTimingForOtherPlayer),
            candidates,
            keys,
            responses);
        return true;
    }

    private static bool TryBuildBattleProjection(
        FlatPromptBattleWireDraftV1 wire,
        FlatPromptCardAuthorityContextV1 authority,
        out FlatPromptProjectionDraftV1? projected,
        out FlatPromptErrorCodeV1 error)
    {
        projected = null;
        error = FlatPromptErrorCodeV1.None;
        List<FlatPublicCandidateDescriptorV1> candidates = new();
        List<string> keys = new();
        List<int> responses = new();

        for (int ordinal = 0;
             ordinal < wire.ActivatableEntries.Count;
             ordinal++)
        {
            FlatPromptBattleActivatableWireEntryV1 entry =
                wire.ActivatableEntries[ordinal];
        if (!TryCorrelateI4CCard(
                    authority,
                    entry.SourceCardCode,
                    entry.Controller,
                    entry.Location,
                    entry.Sequence,
                    out FlatPromptCardCorrelationResultV1? correlation,
                    out error) ||
                correlation is null ||
                !FlatPromptKeyV1.TryCreateBattleActivatable(
                    ordinal,
                    out string key) ||
                !FlatPromptKeyV1.TryEncodeIndexedResponse(
                    ordinal,
                    0,
                    out int response))
            {
                if (error == FlatPromptErrorCodeV1.None)
                {
                    error = FlatPromptErrorCodeV1.ArithmeticFailure;
                }

                return false;
            }

            candidates.Add(
                correlation.SafeCardCode.HasValue
                    ? new FlatBattleActivatableCardCodePublicCandidateV1(
                        key,
                        ordinal,
                        correlation.AcceptedLocator,
                        entry.DescriptionOrEffectId,
                        entry.ClientMode,
                        correlation.SafeCardCode.Value)
                    : new FlatBattleActivatablePublicCandidateV1(
                        key,
                        ordinal,
                        correlation.AcceptedLocator,
                        entry.DescriptionOrEffectId,
                        entry.ClientMode));
            keys.Add(key);
            responses.Add(response);
        }

        for (int ordinal = 0;
             ordinal < wire.AttackableEntries.Count;
             ordinal++)
        {
            FlatPromptBattleAttackableWireEntryV1 entry =
                wire.AttackableEntries[ordinal];
            if (!TryCorrelateI4CCard(
                    authority,
                    entry.SourceCardCode,
                    entry.Controller,
                    entry.Location,
                    entry.Sequence,
                    out FlatPromptCardCorrelationResultV1? correlation,
                    out error) ||
                correlation is null ||
                !FlatPromptKeyV1.TryCreateBattleAttack(
                    ordinal,
                    out string key) ||
                !FlatPromptKeyV1.TryEncodeIndexedResponse(
                    ordinal,
                    1,
                    out int response))
            {
                if (error == FlatPromptErrorCodeV1.None)
                {
                    error = FlatPromptErrorCodeV1.ArithmeticFailure;
                }

                return false;
            }

            candidates.Add(
                correlation.SafeCardCode.HasValue
                    ? new FlatBattleAttackCardCodePublicCandidateV1(
                        key,
                        ordinal,
                        correlation.AcceptedLocator,
                        entry.DirectAttackable,
                        correlation.SafeCardCode.Value)
                    : new FlatBattleAttackPublicCandidateV1(
                        key,
                        ordinal,
                        correlation.AcceptedLocator,
                        entry.DirectAttackable));
            keys.Add(key);
            responses.Add(response);
        }

        if (wire.ToMainPhase2)
        {
            candidates.Add(new FlatBattleToMainPhase2PublicCandidateV1(
                FlatPromptKeyV1.BattleToM2));
            keys.Add(FlatPromptKeyV1.BattleToM2);
            responses.Add(2);
        }

        if (wire.ToEndPhase)
        {
            candidates.Add(new FlatBattleToEndPhasePublicCandidateV1(
                FlatPromptKeyV1.BattleToEp));
            keys.Add(FlatPromptKeyV1.BattleToEp);
            responses.Add(3);
        }

        if (candidates.Count == 0)
        {
            error = FlatPromptErrorCodeV1.ZeroOptionDomain;
            return false;
        }

        projected = new FlatPromptProjectionDraftV1(
            new FlatPromptBattlePublicContextV1(wire.ActingPlayer),
            candidates,
            keys,
            responses);
        return true;
    }

    private static bool TryBuildIdleProjection(
        FlatPromptIdleWireDraftV1 wire,
        FlatPromptCardAuthorityContextV1 authority,
        out FlatPromptProjectionDraftV1? projected,
        out FlatPromptErrorCodeV1 error)
    {
        projected = null;
        error = FlatPromptErrorCodeV1.None;
        List<FlatPublicCandidateDescriptorV1> candidates = new();
        List<string> keys = new();
        List<int> responses = new();

        if (!TryAppendIdleSimpleSection(
                authority,
                wire.SummonEntries,
                FlatPromptKeyV1.IdleSummonPrefix,
                0,
                static (key, ordinal, locator) =>
                    new FlatIdleSummonPublicCandidateV1(
                        key,
                        ordinal,
                        locator),
                static (key, ordinal, locator, code) =>
                    new FlatIdleSummonCardCodePublicCandidateV1(
                        key,
                        ordinal,
                        locator,
                        code),
                candidates,
                keys,
                responses,
                out error) ||
            !TryAppendIdleSimpleSection(
                authority,
                wire.SpecialSummonEntries,
                FlatPromptKeyV1.IdleSpecialSummonPrefix,
                1,
                static (key, ordinal, locator) =>
                    new FlatIdleSpecialSummonPublicCandidateV1(
                        key,
                        ordinal,
                        locator),
                static (key, ordinal, locator, code) =>
                    new FlatIdleSpecialSummonCardCodePublicCandidateV1(
                        key,
                        ordinal,
                        locator,
                        code),
                candidates,
                keys,
                responses,
                out error) ||
            !TryAppendIdleRepositionSection(
                authority,
                wire.RepositionEntries,
                FlatPromptKeyV1.IdleRepositionPrefix,
                2,
                static (key, ordinal, locator) =>
                    new FlatIdleRepositionPublicCandidateV1(
                        key,
                        ordinal,
                        locator),
                static (key, ordinal, locator, code) =>
                    new FlatIdleRepositionCardCodePublicCandidateV1(
                        key,
                        ordinal,
                        locator,
                        code),
                candidates,
                keys,
                responses,
                out error) ||
            !TryAppendIdleSimpleSection(
                authority,
                wire.MonsterSetEntries,
                FlatPromptKeyV1.IdleMsetPrefix,
                3,
                static (key, ordinal, locator) =>
                    new FlatIdleMsetPublicCandidateV1(
                        key,
                        ordinal,
                        locator),
                static (key, ordinal, locator, code) =>
                    new FlatIdleMsetCardCodePublicCandidateV1(
                        key,
                        ordinal,
                        locator,
                        code),
                candidates,
                keys,
                responses,
                out error) ||
            !TryAppendIdleSimpleSection(
                authority,
                wire.SpellTrapSetEntries,
                FlatPromptKeyV1.IdleSsetPrefix,
                4,
                static (key, ordinal, locator) =>
                    new FlatIdleSsetPublicCandidateV1(
                        key,
                        ordinal,
                        locator),
                static (key, ordinal, locator, code) =>
                    new FlatIdleSsetCardCodePublicCandidateV1(
                        key,
                        ordinal,
                        locator,
                        code),
                candidates,
                keys,
                responses,
                out error) ||
            !TryAppendIdleActivatableSection(
                authority,
                wire.ActivatableEntries,
                candidates,
                keys,
                responses,
                out error))
        {
            return false;
        }

        if (wire.ToBattlePhase)
        {
            candidates.Add(new FlatIdleToBattlePhasePublicCandidateV1(
                FlatPromptKeyV1.IdleToBp));
            keys.Add(FlatPromptKeyV1.IdleToBp);
            responses.Add(6);
        }

        if (wire.ToEndPhase)
        {
            candidates.Add(new FlatIdleToEndPhasePublicCandidateV1(
                FlatPromptKeyV1.IdleToEp));
            keys.Add(FlatPromptKeyV1.IdleToEp);
            responses.Add(7);
        }

        if (wire.ShuffleHand)
        {
            candidates.Add(new FlatIdleShuffleHandPublicCandidateV1(
                FlatPromptKeyV1.IdleShuffleHand));
            keys.Add(FlatPromptKeyV1.IdleShuffleHand);
            responses.Add(8);
        }

        if (candidates.Count == 0)
        {
            error = FlatPromptErrorCodeV1.ZeroOptionDomain;
            return false;
        }

        projected = new FlatPromptProjectionDraftV1(
            new FlatPromptIdlePublicContextV1(wire.ActingPlayer),
            candidates,
            keys,
            responses);
        return true;
    }

    private static bool TryAppendIdleSimpleSection(
        FlatPromptCardAuthorityContextV1 authority,
        IReadOnlyList<FlatPromptIdleCardWireEntryV1> entries,
        string keyPrefix,
        int responseKind,
        Func<string, int, PublicSemanticLocatorV1,
            FlatPublicCandidateDescriptorV1> noCodeFactory,
        Func<string, int, PublicSemanticLocatorV1, uint,
            FlatPublicCandidateDescriptorV1> cardCodeFactory,
        List<FlatPublicCandidateDescriptorV1> candidates,
        List<string> keys,
        List<int> responses,
        out FlatPromptErrorCodeV1 error)
    {
        error = FlatPromptErrorCodeV1.None;
        for (int ordinal = 0; ordinal < entries.Count; ordinal++)
        {
            FlatPromptIdleCardWireEntryV1 entry = entries[ordinal];
            if (!TryBuildIdleCardCandidate(
                    authority,
                    entry.SourceCardCode,
                    entry.Controller,
                    entry.Location,
                    entry.Sequence,
                    keyPrefix,
                    responseKind,
                    ordinal,
                    noCodeFactory,
                    cardCodeFactory,
                    out FlatPublicCandidateDescriptorV1? candidate,
                    out string key,
                    out int response,
                    out error) ||
                candidate is null)
            {
                return false;
            }

            candidates.Add(candidate);
            keys.Add(key);
            responses.Add(response);
        }

        return true;
    }

    private static bool TryAppendIdleRepositionSection(
        FlatPromptCardAuthorityContextV1 authority,
        IReadOnlyList<FlatPromptIdleRepositionWireEntryV1> entries,
        string keyPrefix,
        int responseKind,
        Func<string, int, PublicSemanticLocatorV1,
            FlatPublicCandidateDescriptorV1> noCodeFactory,
        Func<string, int, PublicSemanticLocatorV1, uint,
            FlatPublicCandidateDescriptorV1> cardCodeFactory,
        List<FlatPublicCandidateDescriptorV1> candidates,
        List<string> keys,
        List<int> responses,
        out FlatPromptErrorCodeV1 error)
    {
        error = FlatPromptErrorCodeV1.None;
        for (int ordinal = 0; ordinal < entries.Count; ordinal++)
        {
            FlatPromptIdleRepositionWireEntryV1 entry = entries[ordinal];
            if (!TryBuildIdleCardCandidate(
                    authority,
                    entry.SourceCardCode,
                    entry.Controller,
                    entry.Location,
                    entry.Sequence,
                    keyPrefix,
                    responseKind,
                    ordinal,
                    noCodeFactory,
                    cardCodeFactory,
                    out FlatPublicCandidateDescriptorV1? candidate,
                    out string key,
                    out int response,
                    out error) ||
                candidate is null)
            {
                return false;
            }

            candidates.Add(candidate);
            keys.Add(key);
            responses.Add(response);
        }

        return true;
    }

    private static bool TryAppendIdleActivatableSection(
        FlatPromptCardAuthorityContextV1 authority,
        IReadOnlyList<FlatPromptIdleActivatableWireEntryV1> entries,
        List<FlatPublicCandidateDescriptorV1> candidates,
        List<string> keys,
        List<int> responses,
        out FlatPromptErrorCodeV1 error)
    {
        error = FlatPromptErrorCodeV1.None;
        for (int ordinal = 0; ordinal < entries.Count; ordinal++)
        {
            FlatPromptIdleActivatableWireEntryV1 entry = entries[ordinal];
            if (!TryCorrelateI4CCard(
                    authority,
                    entry.SourceCardCode,
                    entry.Controller,
                    entry.Location,
                    entry.Sequence,
                    out FlatPromptCardCorrelationResultV1? correlation,
                    out error) ||
                correlation is null ||
                !FlatPromptKeyV1.TryCreateIdleActivatable(
                    ordinal,
                    out string key) ||
                !FlatPromptKeyV1.TryEncodeIndexedResponse(
                    ordinal,
                    5,
                    out int response))
            {
                if (error == FlatPromptErrorCodeV1.None)
                {
                    error = FlatPromptErrorCodeV1.ArithmeticFailure;
                }

                return false;
            }

            candidates.Add(
                correlation.SafeCardCode.HasValue
                    ? new FlatIdleActivatableCardCodePublicCandidateV1(
                        key,
                        ordinal,
                        correlation.AcceptedLocator,
                        entry.DescriptionOrEffectId,
                        entry.ClientMode,
                        correlation.SafeCardCode.Value)
                    : new FlatIdleActivatablePublicCandidateV1(
                        key,
                        ordinal,
                        correlation.AcceptedLocator,
                        entry.DescriptionOrEffectId,
                        entry.ClientMode));
            keys.Add(key);
            responses.Add(response);
        }

        return true;
    }

    private static bool TryBuildIdleCardCandidate(
        FlatPromptCardAuthorityContextV1 authority,
        uint sourceCardCode,
        byte controller,
        byte location,
        uint sequence,
        string keyPrefix,
        int responseKind,
        int ordinal,
        Func<string, int, PublicSemanticLocatorV1,
            FlatPublicCandidateDescriptorV1> noCodeFactory,
        Func<string, int, PublicSemanticLocatorV1, uint,
            FlatPublicCandidateDescriptorV1> cardCodeFactory,
        out FlatPublicCandidateDescriptorV1? candidate,
        out string key,
        out int response,
        out FlatPromptErrorCodeV1 error)
    {
        candidate = null;
        key = string.Empty;
        response = default;
        if (!TryCorrelateI4CCard(
                authority,
                sourceCardCode,
                controller,
                location,
                sequence,
                out FlatPromptCardCorrelationResultV1? correlation,
                out error) ||
            correlation is null ||
            !FlatPromptKeyV1.TryCreateOrdinalKey(
                keyPrefix,
                ordinal,
                out key) ||
            !FlatPromptKeyV1.TryEncodeIndexedResponse(
                ordinal,
                responseKind,
                out response))
        {
            if (error == FlatPromptErrorCodeV1.None)
            {
                error = FlatPromptErrorCodeV1.ArithmeticFailure;
            }

            return false;
        }

        candidate = correlation.SafeCardCode.HasValue
            ? cardCodeFactory(
                key,
                ordinal,
                correlation.AcceptedLocator,
                correlation.SafeCardCode.Value)
            : noCodeFactory(
                key,
                ordinal,
                correlation.AcceptedLocator);
        return true;
    }

    private static bool TryCorrelateI4CCard(
        FlatPromptCardAuthorityContextV1 authority,
        uint sourceCardCode,
        byte controller,
        byte location,
        uint sequence,
        out FlatPromptCardCorrelationResultV1? correlation,
        out FlatPromptErrorCodeV1 error)
    {
        correlation = null;
        error = FlatPromptErrorCodeV1.None;
        if ((location & 0x80) != 0)
        {
            error = FlatPromptErrorCodeV1.UnprovenPublicReference;
            return false;
        }

        return FlatPromptCardCorrelationV1.TryCorrelate(
            authority.CapturedMirror,
            authority.AcceptedSnapshot,
            sourceCardCode,
            new ModernLocInfoV1(controller, location, sequence, 0),
            out correlation,
            out error);
    }

    private static bool TryValidatePrivateLocation(
        ModernLocInfoV1 location,
        out FlatPromptErrorCodeV1 error)
    {
        if (MirrorAddressNormalizationV1.TryNormalize(
                location,
                out _,
                out GameplayErrorCode locationError))
        {
            error = FlatPromptErrorCodeV1.None;
            return true;
        }

        return TryMapLocationError(locationError, out error);
    }

    private static bool TryMapLocationError(
        GameplayErrorCode locationError,
        out FlatPromptErrorCodeV1 error)
    {
        if (locationError == GameplayErrorCode.None)
        {
            error = FlatPromptErrorCodeV1.None;
            return true;
        }

        error = locationError switch
        {
            GameplayErrorCode.InvalidParticipant =>
                FlatPromptErrorCodeV1.InvalidParticipant,
            GameplayErrorCode.InvalidLocation or
                GameplayErrorCode.StateCapacityExceeded =>
                FlatPromptErrorCodeV1.InvalidLocation,
            _ => FlatPromptErrorCodeV1.UnprovenPublicReference
        };
        return false;
    }

    private static bool FailWireDraft(
        FlatPromptErrorCodeV1 failure,
        out FlatPromptWireDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = failure;
        return false;
    }

    private static bool FailProjection(
        FlatPromptErrorCodeV1 failure,
        out FlatPromptProjectionDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = failure;
        return false;
    }

    private static bool TryProjectYesNo(
        ReadOnlySpan<byte> bytes,
        out FlatPromptProjectionDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = FlatPromptErrorCodeV1.None;
        if (bytes.Length == 6)
        {
            error = FlatPromptErrorCodeV1.UnsupportedPromptLayout;
            return false;
        }

        if (bytes.Length != YesNoMessageLength)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        if (bytes[1] > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidParticipant;
            return false;
        }

        ulong description = BinaryPrimitives.ReadUInt64LittleEndian(
            bytes.Slice(2, sizeof(ulong)));
        FlatYesNoPublicCandidateDescriptorV1[] candidates =
        {
            new FlatYesNoPublicCandidateDescriptorV1(
                FlatPromptKeyV1.YesNoNo,
                FlatPromptChoiceKindV1.No),
            new FlatYesNoPublicCandidateDescriptorV1(
                FlatPromptKeyV1.YesNoYes,
                FlatPromptChoiceKindV1.Yes)
        };
        draft = new FlatPromptProjectionDraftV1(
            new FlatPromptYesNoPublicContextV1(bytes[1], description),
            candidates,
            new[] { FlatPromptKeyV1.YesNoNo, FlatPromptKeyV1.YesNoYes },
            YesNoResponses);
        return true;
    }

    private static bool TryProjectOption(
        ReadOnlySpan<byte> bytes,
        out FlatPromptProjectionDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = FlatPromptErrorCodeV1.None;
        if (bytes.Length < OptionHeaderLength)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        if (bytes[1] > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidParticipant;
            return false;
        }

        byte count = bytes[2];
        int expectedLength;
        int legacyLength;
        try
        {
            expectedLength = checked(
                OptionHeaderLength +
                checked(OptionDescriptionLength * count));
            legacyLength = checked(
                OptionHeaderLength +
                checked(4 * count));
        }
        catch (OverflowException)
        {
            error = FlatPromptErrorCodeV1.ArithmeticFailure;
            return false;
        }

        if (count == 0)
        {
            error = FlatPromptErrorCodeV1.ZeroOptionDomain;
            return false;
        }

        if (bytes.Length == legacyLength && legacyLength != expectedLength)
        {
            error = FlatPromptErrorCodeV1.UnsupportedPromptLayout;
            return false;
        }

        if (bytes.Length != expectedLength)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        FlatOptionPublicCandidateDescriptorV1[] candidates =
            new FlatOptionPublicCandidateDescriptorV1[count];
        string[] keys = new string[count];
        int[] responses = new int[count];
        for (int ordinal = 0; ordinal < count; ordinal++)
        {
            if (!FlatPromptKeyV1.TryCreateOption(
                    ordinal,
                    out string key))
            {
                error = FlatPromptErrorCodeV1.InvalidI4LocalCandidateKey;
                return false;
            }

            int offset = checked(
                OptionHeaderLength +
                checked(OptionDescriptionLength * ordinal));
            ulong optionValue = BinaryPrimitives.ReadUInt64LittleEndian(
                bytes.Slice(offset, OptionDescriptionLength));
            candidates[ordinal] = new FlatOptionPublicCandidateDescriptorV1(
                key,
                ordinal,
                optionValue);
            keys[ordinal] = key;
            responses[ordinal] = ordinal;
        }

        draft = new FlatPromptProjectionDraftV1(
            new FlatPromptOptionPublicContextV1(bytes[1]),
            candidates,
            keys,
            responses);
        return true;
    }

    private static bool TryProjectPosition(
        ReadOnlySpan<byte> bytes,
        out FlatPromptProjectionDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = FlatPromptErrorCodeV1.None;
        if (bytes.Length != PositionMessageLength)
        {
            error = FlatPromptErrorCodeV1.MalformedPrompt;
            return false;
        }

        if (bytes[1] > 1)
        {
            error = FlatPromptErrorCodeV1.InvalidParticipant;
            return false;
        }

        _ = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(2, sizeof(uint)));
        byte mask = bytes[6];
        if (mask == 0 ||
            (mask & ~ValidPositionMask) != 0 ||
            BitOperations.PopCount((uint)mask) < 2)
        {
            error = FlatPromptErrorCodeV1.InvalidPositionMask;
            return false;
        }

        List<FlatPositionPublicCandidateDescriptorV1> candidates = new();
        List<string> keys = new();
        List<int> responses = new();
        foreach ((byte bit, FlatPromptChoiceKindV1 choiceKind, string key)
                 in PositionChoices)
        {
            if ((mask & bit) == 0)
            {
                continue;
            }

            candidates.Add(
                new FlatPositionPublicCandidateDescriptorV1(
                    key,
                    choiceKind,
                    bit));
            keys.Add(key);
            responses.Add(bit);
        }

        if (candidates.Count < 2)
        {
            error = FlatPromptErrorCodeV1.UnprovenCandidateDomain;
            return false;
        }

        draft = new FlatPromptProjectionDraftV1(
            new FlatPromptPositionPublicContextV1(bytes[1], mask),
            candidates,
            keys,
            responses);
        return true;
    }

    private static bool Fail(
        FlatPromptErrorCodeV1 failure,
        out FlatPromptProjectionDraftV1? draft,
        out FlatPromptErrorCodeV1 error)
    {
        draft = null;
        error = failure;
        return false;
    }
}
