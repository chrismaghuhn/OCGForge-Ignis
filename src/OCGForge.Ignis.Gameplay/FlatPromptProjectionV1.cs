using System.Buffers.Binary;
using System.Numerics;

namespace OCGForge.Ignis.Gameplay;

internal static class FlatPromptProjectionV1
{
    private const int YesNoMessageLength = 10;
    private const int EffectYnMessageLength = 24;
    private const int ChainHeaderLength = 16;
    private const int ChainEntryLength = 23;
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
