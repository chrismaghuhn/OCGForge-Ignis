using System.Buffers.Binary;
using System.Reflection;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I4AFlatPromptProjectionTests
{
    private static readonly byte[] YesNoDescription =
    {
        0x0D, 0x00, 0x08, 0x07, 0x06,
        0x05, 0x04, 0x03, 0x02, 0x01
    };

    private static readonly byte[] OptionWireOrder =
    {
        0x0E, 0x00, 0x03,
        0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11,
        0x11, 0x00, 0xFF, 0xEE, 0xDD, 0xCC, 0xBB, 0xAA
    };

    private static readonly byte[] OptionDuplicates =
    {
        0x0E, 0x00, 0x02,
        0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11,
        0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11
    };

    private static readonly byte[] PositionThreeBits =
    {
        0x13, 0x00, 0xBE, 0xBA, 0xFE, 0xCA, 0x0D
    };

    internal static void TestYesNoExactDomain()
    {
        // YESNO_DESCRIPTION_U64
        FlatPromptProjectionResultV1 result =
            new FlatPromptSessionV1().TryAcceptPrompt(YesNoDescription);

        True(result.IsSuccess);
        Equal(FlatPromptErrorCodeV1.None, result.Error);
        NotNull(result.Context);
        NotNull(result.Candidates);
        FlatPromptYesNoPublicContextV1 context =
            result.Context as FlatPromptYesNoPublicContextV1 ??
            throw new InvalidOperationException("expected YESNO context");
        Equal("ocgforge-ignis.flat-prompt-projection.v1", context.ContractId);
        Equal(FlatPromptFamilyV1.MsgSelectYesNo, context.PromptFamily);
        Equal((byte)0, context.ActingPlayer);
        Equal(0x0102030405060708ul, context.YesNoDescriptionId);
        Equal(2, result.Candidates!.Count);
        Equal("MSG_SELECT_YESNO:NO", result.Candidates[0].I4LocalCandidateKey);
        Equal("MSG_SELECT_YESNO:YES", result.Candidates[1].I4LocalCandidateKey);
        Equal(FlatPromptChoiceKindV1.No, result.Candidates[0].ChoiceKind);
        Equal(FlatPromptChoiceKindV1.Yes, result.Candidates[1].ChoiceKind);
        True(result.Candidates[0] is FlatYesNoPublicCandidateDescriptorV1);
        True(result.Candidates[1] is FlatYesNoPublicCandidateDescriptorV1);
    }

    internal static void TestYesNoFailuresAndOwnership()
    {
        byte[] payload = YesNoDescription.ToArray();
        FlatPromptProjectionResultV1 accepted =
            new FlatPromptSessionV1().TryAcceptPrompt(payload);
        payload[2] = 0xFF;
        FlatPromptYesNoPublicContextV1 context =
            accepted.Context as FlatPromptYesNoPublicContextV1 ??
            throw new InvalidOperationException("expected YESNO context");
        Equal(0x0102030405060708ul, context.YesNoDescriptionId);

        AssertFailure(
            YesNoDescription[..5],
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            Append(YesNoDescription, 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new byte[] { 0x0D, 0x02, 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 },
            FlatPromptErrorCodeV1.InvalidParticipant);
        AssertFailure(
            new byte[] { 0x0D, 0x00, 0x08, 0x07, 0x06, 0x05 },
            FlatPromptErrorCodeV1.UnsupportedPromptLayout);
    }

    internal static void TestOptionSourceOrderAndValues()
    {
        // OPTION_WIRE_ORDER
        FlatPromptProjectionResultV1 result =
            new FlatPromptSessionV1().TryAcceptPrompt(OptionWireOrder);

        True(result.IsSuccess);
        Equal(FlatPromptFamilyV1.MsgSelectOption, result.Context!.PromptFamily);
        Equal((byte)0, result.Context.ActingPlayer);
        Equal(3, result.Candidates!.Count);
        ulong[] expectedValues =
        {
            0x0000000000000001ul,
            0x1122334455667788ul,
            0xAABBCCDDEEFF0011ul
        };
        for (int i = 0; i < expectedValues.Length; i++)
        {
            FlatOptionPublicCandidateDescriptorV1 candidate =
                result.Candidates[i] as FlatOptionPublicCandidateDescriptorV1 ??
                throw new InvalidOperationException("expected OPTION candidate");
            Equal($"MSG_SELECT_OPTION:OPTION:{i}", candidate.I4LocalCandidateKey);
            Equal(FlatPromptChoiceKindV1.Option, candidate.ChoiceKind);
            Equal(FlatPromptSourceSectionV1.Options, candidate.SourceSection);
            Equal(i, candidate.SourceOrdinal);
            Equal(expectedValues[i], candidate.OptionValue);
        }
    }

    internal static void TestOptionDuplicatesAndMetamorphicKey()
    {
        // OPTION_DUPLICATE_DESCRIPTIONS
        FlatPromptProjectionResultV1 duplicateResult =
            new FlatPromptSessionV1().TryAcceptPrompt(OptionDuplicates);
        Equal(2, duplicateResult.Candidates!.Count);
        FlatOptionPublicCandidateDescriptorV1 first =
            (FlatOptionPublicCandidateDescriptorV1)duplicateResult.Candidates[0];
        FlatOptionPublicCandidateDescriptorV1 second =
            (FlatOptionPublicCandidateDescriptorV1)duplicateResult.Candidates[1];
        Equal(0x1111111111111111ul, first.OptionValue);
        Equal(first.OptionValue, second.OptionValue);
        Equal(0, first.SourceOrdinal);
        Equal(1, second.SourceOrdinal);
        NotEqual(first.I4LocalCandidateKey, second.I4LocalCandidateKey);

        FlatPromptProjectionResultV1 firstValue =
            new FlatPromptSessionV1().TryAcceptPrompt(OptionSingle(1));
        FlatPromptProjectionResultV1 secondValue =
            new FlatPromptSessionV1().TryAcceptPrompt(OptionSingle(2));
        Equal(
            firstValue.Candidates![0].I4LocalCandidateKey,
            secondValue.Candidates![0].I4LocalCandidateKey);
        NotEqual(
            ((FlatOptionPublicCandidateDescriptorV1)firstValue.Candidates[0]).OptionValue,
            ((FlatOptionPublicCandidateDescriptorV1)secondValue.Candidates[0]).OptionValue);
    }

    internal static void TestOptionFailures()
    {
        AssertFailure(
            new byte[] { 0x0E, 0x00, 0x00 },
            FlatPromptErrorCodeV1.ZeroOptionDomain);
        AssertFailure(
            new byte[] { 0x0E, 0x00, 0x02, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            Append(OptionSingle(1), 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new byte[] { 0x0E, 0x00, 0xFF },
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new byte[] { 0x0E, 0x02, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
            FlatPromptErrorCodeV1.InvalidParticipant);
        AssertFailure(
            new byte[] { 0x0E, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00 },
            FlatPromptErrorCodeV1.UnsupportedPromptLayout);
    }

    internal static void TestPositionValidMasks()
    {
        // POSITION_THREE_BITS_IN_EDOPRO_ORDER
        FlatPromptProjectionResultV1 three =
            new FlatPromptSessionV1().TryAcceptPrompt(PositionThreeBits);
        AssertPositionDomain(
            three,
            new[]
            {
                (FlatPromptChoiceKindV1.FaceupAttack, (byte)0x01),
                (FlatPromptChoiceKindV1.FaceupDefense, (byte)0x04),
                (FlatPromptChoiceKindV1.FacedownDefense, (byte)0x08)
            });

        AssertPositionDomain(
            new FlatPromptSessionV1().TryAcceptPrompt(Position(0x03)),
            new[]
            {
                (FlatPromptChoiceKindV1.FaceupAttack, (byte)0x01),
                (FlatPromptChoiceKindV1.FacedownAttack, (byte)0x02)
            });
        AssertPositionDomain(
            new FlatPromptSessionV1().TryAcceptPrompt(Position(0x0F)),
            new[]
            {
                (FlatPromptChoiceKindV1.FaceupAttack, (byte)0x01),
                (FlatPromptChoiceKindV1.FacedownAttack, (byte)0x02),
                (FlatPromptChoiceKindV1.FaceupDefense, (byte)0x04),
                (FlatPromptChoiceKindV1.FacedownDefense, (byte)0x08)
            });
    }

    internal static void TestPositionFailures()
    {
        AssertFailure(Position(0x00), FlatPromptErrorCodeV1.InvalidPositionMask);
        AssertFailure(Position(0x01), FlatPromptErrorCodeV1.InvalidPositionMask);
        AssertFailure(Position(0x10), FlatPromptErrorCodeV1.InvalidPositionMask);
        AssertFailure(
            Append(Position(0x03), 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);
        byte[] invalidPlayer = Position(0x03);
        invalidPlayer[1] = 0x02;
        AssertFailure(invalidPlayer, FlatPromptErrorCodeV1.InvalidParticipant);
    }

    internal static void TestPositionUnboundCardCode()
    {
        // POSITION_VALID_MASK_WITH_UNBOUND_CARD_CODE
        FlatPromptProjectionResultV1 result =
            new FlatPromptSessionV1().TryAcceptPrompt(Position(0x03, 0xCAFEBABE));
        True(result.IsSuccess);
        FlatPromptPositionPublicContextV1 context =
            result.Context as FlatPromptPositionPublicContextV1 ??
            throw new InvalidOperationException("expected POSITION context");
        Equal((byte)0x03, context.PositionAllowedPositionsMask);
        Equal(2, result.Candidates!.Count);
        Null(typeof(FlatPromptPositionPublicContextV1).GetProperty("PositionCardCode"));
        Null(typeof(FlatPromptPositionPublicContextV1).GetProperty("CardLocator"));
    }

    internal static void TestExactResponseBindings()
    {
        FlatPromptSessionV1 yesNo = new();
        yesNo.TryAcceptPrompt(YesNoDescription);
        AssertResponse(yesNo, "MSG_SELECT_YESNO:NO", 0);
        AssertResponse(yesNo, "MSG_SELECT_YESNO:YES", 1);

        FlatPromptSessionV1 option = new();
        option.TryAcceptPrompt(OptionWireOrder);
        AssertResponse(option, "MSG_SELECT_OPTION:OPTION:0", 0);
        AssertResponse(option, "MSG_SELECT_OPTION:OPTION:2", 2);

        FlatPromptSessionV1 position = new();
        position.TryAcceptPrompt(Position(0x0F));
        AssertResponse(position, "MSG_SELECT_POSITION:FACEUP_ATTACK", 1);
        AssertResponse(position, "MSG_SELECT_POSITION:FACEDOWN_ATTACK", 2);
        AssertResponse(position, "MSG_SELECT_POSITION:FACEUP_DEFENSE", 4);
        AssertResponse(position, "MSG_SELECT_POSITION:FACEDOWN_DEFENSE", 8);
    }

    internal static void TestStaleSelection()
    {
        FlatPromptSessionV1 session = new();
        True(session.TryAcceptPrompt(YesNoDescription).IsSuccess);
        True(session.TryCaptureSelection(
            "MSG_SELECT_YESNO:YES",
            out FlatPromptSelectionHandleV1? oldHandle,
            out FlatPromptErrorCodeV1 captureError));
        Equal(FlatPromptErrorCodeV1.None, captureError);
        NotNull(oldHandle);
        Equal(0ul, oldHandle!.PromptInstanceOrdinal);

        True(session.TryAcceptPrompt(YesNoDescription).IsSuccess);
        Equal(1ul, CaptureOrdinal(session, "MSG_SELECT_YESNO:YES"));
        False(session.TryResolveSelection(
            oldHandle,
            out _,
            out FlatPromptErrorCodeV1 staleError));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, staleError);
    }

    internal static void TestBindingValidationFailures()
    {
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 result =
            session.TryAcceptPrompt(OptionWireOrder);
        FlatPublicCandidateDescriptorV1[] candidates =
            result.Candidates!.ToArray();
        string[] keys = candidates
            .Select(candidate => candidate.I4LocalCandidateKey)
            .Reverse()
            .ToArray();
        int[] responses = { 0, 1, 2 };
        False(CurrentFlatPromptBindingV1.TryCreate(
            0,
            FlatPromptFamilyV1.MsgSelectOption,
            candidates,
            keys,
            responses,
            out CurrentFlatPromptBindingV1? swapped,
            out FlatPromptErrorCodeV1 swappedError));
        Null(swapped);
        Equal(FlatPromptErrorCodeV1.InvalidResponseBinding, swappedError);

        False(session.TryCaptureSelection(
            null,
            out FlatPromptSelectionHandleV1? nullHandle,
            out FlatPromptErrorCodeV1 nullError));
        Null(nullHandle);
        Equal(FlatPromptErrorCodeV1.InvalidI4LocalCandidateKey, nullError);
        False(session.TryCaptureSelection(
            string.Empty,
            out FlatPromptSelectionHandleV1? emptyHandle,
            out FlatPromptErrorCodeV1 emptyError));
        Null(emptyHandle);
        Equal(FlatPromptErrorCodeV1.InvalidI4LocalCandidateKey, emptyError);
        False(session.TryCaptureSelection(
            "MSG_SELECT_OPTION:OPTION:99",
            out FlatPromptSelectionHandleV1? unknownHandle,
            out FlatPromptErrorCodeV1 unknownError));
        Null(unknownHandle);
        Equal(FlatPromptErrorCodeV1.InvalidI4LocalCandidateKey, unknownError);

        True(session.TryCaptureSelection(
            "MSG_SELECT_OPTION:OPTION:0",
            out FlatPromptSelectionHandleV1? currentHandle,
            out _));
        NotNull(currentHandle);
        FlatPromptSelectionHandleV1 familyMismatch =
            new(
                currentHandle!.PromptInstanceOrdinal,
                FlatPromptFamilyV1.MsgSelectYesNo,
                currentHandle.I4LocalCandidateKey,
                currentHandle.OrderedDomain.ToArray());
        False(session.TryResolveSelection(
            familyMismatch,
            out _,
            out FlatPromptErrorCodeV1 familyError));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, familyError);

        FlatPromptSelectionHandleV1 domainMismatch =
            new(
                currentHandle.PromptInstanceOrdinal,
                currentHandle.Family,
                currentHandle.I4LocalCandidateKey,
                new[] { currentHandle.OrderedDomain[0] });
        False(session.TryResolveSelection(
            domainMismatch,
            out _,
            out FlatPromptErrorCodeV1 domainError));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, domainError);

        FlatPromptSelectionHandleV1 missingResponse =
            new(
                currentHandle.PromptInstanceOrdinal,
                currentHandle.Family,
                "MSG_SELECT_OPTION:OPTION:99",
                currentHandle.OrderedDomain.ToArray());
        False(session.TryResolveSelection(
            missingResponse,
            out _,
            out FlatPromptErrorCodeV1 missingResponseError));
        Equal(FlatPromptErrorCodeV1.InvalidResponseBinding, missingResponseError);
    }

    internal static void TestFailureAtomicityAndOrdinal()
    {
        FlatPromptSessionV1 session = new();
        AssertFailureResult(session, new byte[] { 0x0E, 0x00, 0x00 });
        True(session.TryAcceptPrompt(YesNoDescription).IsSuccess);
        Equal(0ul, CaptureOrdinal(session, "MSG_SELECT_YESNO:NO"));

        True(session.TryCaptureSelection(
            "MSG_SELECT_YESNO:NO",
            out FlatPromptSelectionHandleV1? oldHandle,
            out _));
        AssertFailureResult(session, Append(YesNoDescription, 0xAA));
        False(session.TryResolveSelection(
            oldHandle,
            out _,
            out FlatPromptErrorCodeV1 staleError));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, staleError);

        True(session.TryAcceptPrompt(OptionSingle(7)).IsSuccess);
        Equal(1ul, CaptureOrdinal(session, "MSG_SELECT_OPTION:OPTION:0"));

        FlatPromptSessionV1 firstFailure = new();
        AssertFailureResult(firstFailure, Position(0x01));
        True(firstFailure.TryAcceptPrompt(Position(0x03)).IsSuccess);
        Equal(0ul, CaptureOrdinal(firstFailure, "MSG_SELECT_POSITION:FACEUP_ATTACK"));

        foreach (byte unsupportedMessageId in new byte[] { 10, 11, 12, 16 })
        {
            FlatPromptSessionV1 unsupportedSession = new();
            True(unsupportedSession.TryAcceptPrompt(YesNoDescription).IsSuccess);
            True(unsupportedSession.TryCaptureSelection(
                "MSG_SELECT_YESNO:YES",
                out FlatPromptSelectionHandleV1? oldUnsupportedHandle,
                out FlatPromptErrorCodeV1 captureError));
            Equal(FlatPromptErrorCodeV1.None, captureError);
            NotNull(oldUnsupportedHandle);
            Equal(0ul, oldUnsupportedHandle!.PromptInstanceOrdinal);

            FlatPromptProjectionResultV1 unsupported =
                unsupportedSession.TryAcceptPrompt(new[] { unsupportedMessageId });
            False(unsupported.IsSuccess);
            Equal(
                FlatPromptErrorCodeV1.UnsupportedPromptLayout,
                unsupported.Error);
            Null(unsupported.Context);
            Null(unsupported.Candidates);
            False(unsupportedSession.TryResolveSelection(
                oldUnsupportedHandle,
                out _,
                out FlatPromptErrorCodeV1 unsupportedStaleError));
            Equal(
                FlatPromptErrorCodeV1.StalePromptBinding,
                unsupportedStaleError);

            True(unsupportedSession.TryAcceptPrompt(YesNoDescription).IsSuccess);
            Equal(
                1ul,
                CaptureOrdinal(unsupportedSession, "MSG_SELECT_YESNO:YES"));
        }
    }

    internal static void TestPublicApiBoundary()
    {
        string[] forbidden =
        {
            "PromptInstanceOrdinal", "PublicActionKey", "ResponseI32",
            "ResponseBody", "RawBytes", "MirrorEntityId", "ModernLocInfo",
            "ProtocolOffset", "Socket", "Path", "Timestamp", "Pid",
            "CardLocator"
        };
        Type[] publicTypes =
        {
            typeof(FlatPromptSessionV1),
            typeof(FlatPromptProjectionResultV1),
            typeof(FlatPromptPublicContextV1),
            typeof(FlatPromptYesNoPublicContextV1),
            typeof(FlatPromptOptionPublicContextV1),
            typeof(FlatPromptPositionPublicContextV1),
            typeof(FlatPublicCandidateDescriptorV1),
            typeof(FlatYesNoPublicCandidateDescriptorV1),
            typeof(FlatOptionPublicCandidateDescriptorV1),
            typeof(FlatPositionPublicCandidateDescriptorV1)
        };
        foreach (Type type in publicTypes)
        {
            foreach (PropertyInfo property in type.GetProperties())
            {
                False(forbidden.Contains(property.Name, StringComparer.Ordinal),
                    $"forbidden public property {type.Name}.{property.Name}");
            }
        }

        False(typeof(FlatPromptProjectionV1).IsPublic);
        False(typeof(CurrentFlatPromptBindingV1).IsPublic);
        False(typeof(FlatPromptSelectionHandleV1).IsPublic);
        False(typeof(FlatPromptResponseResolutionV1).IsPublic);
        Null(typeof(FlatPromptSessionV1).GetProperty("CurrentBinding"));
        Null(typeof(FlatPromptSessionV1).GetProperty("PromptInstanceOrdinal"));
        Null(typeof(FlatPromptSessionV1).GetProperty("Mirror"));
        Null(typeof(FlatPromptSessionV1).GetProperty("Projection"));
        False(typeof(FlatPromptSessionV1).GetMethods().Any(
            method => method.IsPublic && method.Name.Contains("Send", StringComparison.OrdinalIgnoreCase)));
    }

    internal static void TestValueOwnership()
    {
        byte[] payload = OptionWireOrder.ToArray();
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 result = session.TryAcceptPrompt(payload);
        payload[3] = 0xFF;
        payload[4] = 0xFF;
        FlatOptionPublicCandidateDescriptorV1 candidate =
            (FlatOptionPublicCandidateDescriptorV1)result.Candidates![0];
        Equal(1ul, candidate.OptionValue);
        Equal("MSG_SELECT_OPTION:OPTION:0", candidate.I4LocalCandidateKey);

        IList<FlatPublicCandidateDescriptorV1> list =
            (IList<FlatPublicCandidateDescriptorV1>)result.Candidates;
        True(list.IsReadOnly);
        True(list.Contains(candidate));
        try
        {
            list.Add(candidate);
            throw new InvalidOperationException("candidate list was mutable");
        }
        catch (NotSupportedException)
        {
        }

        True(session.TryCaptureSelection(
            candidate.I4LocalCandidateKey,
            out FlatPromptSelectionHandleV1? handle,
            out _));
        FlatPublicCandidateDescriptorV1 captured = handle!.OrderedDomain[0];
        True(session.TryAcceptPrompt(OptionSingle(3)).IsSuccess);
        Equal(candidate.I4LocalCandidateKey, captured.I4LocalCandidateKey);
        Equal(1ul, ((FlatOptionPublicCandidateDescriptorV1)captured).OptionValue);
    }

    private static void AssertPositionDomain(
        FlatPromptProjectionResultV1 result,
        IReadOnlyList<(FlatPromptChoiceKindV1 Kind, byte Bit)> expected)
    {
        True(result.IsSuccess, result.Error.ToString());
        FlatPromptPositionPublicContextV1 context =
            result.Context as FlatPromptPositionPublicContextV1 ??
            throw new InvalidOperationException("expected POSITION context");
        Equal(expected.Count, result.Candidates!.Count);
        foreach (int index in Enumerable.Range(0, expected.Count))
        {
            FlatPositionPublicCandidateDescriptorV1 candidate =
                result.Candidates[index] as FlatPositionPublicCandidateDescriptorV1 ??
                throw new InvalidOperationException("expected POSITION candidate");
            Equal(expected[index].Kind, candidate.ChoiceKind);
            Equal(expected[index].Bit, candidate.PositionValue);
            Equal(expected[index].Bit, ResponseFor(candidate.I4LocalCandidateKey));
        }
        Equal(expected.Aggregate((byte)0, (mask, value) => (byte)(mask | value.Bit)),
            context.PositionAllowedPositionsMask);
    }

    private static void AssertResponse(
        FlatPromptSessionV1 session,
        string key,
        int expected)
    {
        True(session.TryCaptureSelection(
            key,
            out FlatPromptSelectionHandleV1? handle,
            out FlatPromptErrorCodeV1 captureError),
            captureError.ToString());
        NotNull(handle);
        True(session.TryResolveSelection(
            handle,
            out FlatPromptResponseResolutionV1 response,
            out FlatPromptErrorCodeV1 resolveError),
            resolveError.ToString());
        Equal(expected, response.ResponseI32);
    }

    private static ulong CaptureOrdinal(
        FlatPromptSessionV1 session,
        string key)
    {
        True(session.TryCaptureSelection(
            key,
            out FlatPromptSelectionHandleV1? handle,
            out FlatPromptErrorCodeV1 error),
            error.ToString());
        NotNull(handle);
        return handle!.PromptInstanceOrdinal;
    }

    private static void AssertFailure(
        byte[] payload,
        FlatPromptErrorCodeV1 expectedError)
    {
        FlatPromptProjectionResultV1 result =
            new FlatPromptSessionV1().TryAcceptPrompt(payload);
        False(result.IsSuccess);
        Equal(expectedError, result.Error);
        Null(result.Context);
        Null(result.Candidates);
    }

    private static void AssertFailureResult(
        FlatPromptSessionV1 session,
        byte[] payload)
    {
        FlatPromptProjectionResultV1 result = session.TryAcceptPrompt(payload);
        False(result.IsSuccess);
        Null(result.Context);
        Null(result.Candidates);
    }

    private static byte[] OptionSingle(ulong value)
    {
        byte[] bytes = new byte[11];
        bytes[0] = 0x0E;
        bytes[1] = 0x00;
        bytes[2] = 0x01;
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(3), value);
        return bytes;
    }

    private static byte[] Position(byte mask, uint cardCode = 0xCAFEBABE)
    {
        byte[] bytes = new byte[7];
        bytes[0] = 0x13;
        bytes[1] = 0x00;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2, 4), cardCode);
        bytes[6] = mask;
        return bytes;
    }

    private static byte[] Append(byte[] bytes, byte value) =>
        bytes.Concat(new[] { value }).ToArray();

    private static int ResponseFor(string key) =>
        key switch
        {
            "MSG_SELECT_POSITION:FACEUP_ATTACK" => 1,
            "MSG_SELECT_POSITION:FACEDOWN_ATTACK" => 2,
            "MSG_SELECT_POSITION:FACEUP_DEFENSE" => 4,
            "MSG_SELECT_POSITION:FACEDOWN_DEFENSE" => 8,
            _ => throw new InvalidOperationException($"unexpected position key {key}")
        };
}
