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

    internal static bool TryBuildProjectedDraft(
        FlatPromptWireDraftV1 draft,
        FlatPromptCardAuthorityContextV1? authority,
        out FlatPromptProjectionDraftV1? projected,
        out FlatPromptErrorCodeV1 error)
    {
        ArgumentNullException.ThrowIfNull(draft);
        projected = null;
        error = FlatPromptErrorCodeV1.None;
        if (authority is null)
        {
            error = FlatPromptErrorCodeV1.UnprovenPublicReference;
            return false;
        }

        return draft switch
        {
            FlatPromptBattleWireDraftV1 battle =>
                TryBuildBattleProjection(
                    battle,
                    authority,
                    out projected,
                    out error),
            FlatPromptIdleWireDraftV1 idle =>
                TryBuildIdleProjection(
                    idle,
                    authority,
                    out projected,
                    out error),
            FlatPromptEffectYnWireDraftV1 effect =>
                TryBuildEffectYnProjection(
                    effect,
                    authority,
                    out projected,
                    out error),
            FlatPromptChainWireDraftV1 chain =>
                TryBuildChainProjection(
                    chain,
                    authority,
                    out projected,
                    out error),
            _ => FailProjection(
                FlatPromptErrorCodeV1.UnsupportedPromptLayout,
                out projected,
                out error)
        };
    }

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
