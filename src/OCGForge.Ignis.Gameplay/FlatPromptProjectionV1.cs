using System.Buffers.Binary;
using System.Numerics;

namespace OCGForge.Ignis.Gameplay;

internal static class FlatPromptProjectionV1
{
    private const int YesNoMessageLength = 10;
    private const int OptionHeaderLength = 3;
    private const int OptionDescriptionLength = 8;
    private const int PositionMessageLength = 7;
    private const byte ValidPositionMask = 0x0F;

    private static readonly int[] YesNoResponses = { 0, 1 };

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
