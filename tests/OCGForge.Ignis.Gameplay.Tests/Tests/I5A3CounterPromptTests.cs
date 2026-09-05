using System.Buffers.Binary;
using System.Reflection;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I5A3CounterPromptTests
{
    internal static void TestSelectCounter()
    {
        AssertExactDomainAndTerminalResponses();
        AssertPruningAndDuplicateOccurrences();
        AssertWireAndDomainFailures();
        AssertAtomicityStalenessOwnershipAndCodec();
        AssertBindingAndPublicBoundary();
        AssertLaterFamilyBoundary();
    }

    private static void AssertExactDomainAndTerminalResponses()
    {
        Authority authority = CreateAuthority(
            new CardSpec(
                0x11111111,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new CardSpec(
                0x22222222,
                new ModernLocInfoV1(0, 0x04, 1, 0x05)));
        byte[] message = CounterMessage(
            0,
            0x0010,
            3,
            new CounterEntry(
                0x11111111,
                new ModernLocInfoV1(0, 0x04, 0, 0),
                2),
            new CounterEntry(
                0x22222222,
                new ModernLocInfoV1(0, 0x04, 1, 0),
                3));

        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 result = Accept(
            session,
            message,
            authority);
        AssertSuccess(result);
        FlatPromptCounterSelectionPublicContextV1 context =
            result.Context as FlatPromptCounterSelectionPublicContextV1 ??
            throw new InvalidOperationException("expected counter context");
        Equal((byte)0, context.ActingPlayer);
        Equal((ushort)0x0010, context.CounterType);
        Equal((ushort)3, context.RequiredTotal);
        Equal(2, context.Sources.Count);
        Equal(0, context.Sources[0].SourceOrdinal);
        Equal((ushort)2, context.Sources[0].Capacity);
        Equal("p0:MONSTER_ZONE:0",
            context.Sources[0].PublicSemanticCardLocator.Value);
        Equal(1, context.Sources[1].SourceOrdinal);
        Equal((ushort)3, context.Sources[1].Capacity);
        Equal("p0:MONSTER_ZONE:1",
            context.Sources[1].PublicSemanticCardLocator.Value);
        EqualKeys(
            new[]
            {
                "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:0",
                "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:1",
                "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:2"
            },
            result.Candidates!);
        foreach (FlatPublicCandidateDescriptorV1 candidate in result.Candidates!)
        {
            FlatPromptCounterAmountPublicCandidateV1 amount =
                candidate as FlatPromptCounterAmountPublicCandidateV1 ??
                throw new InvalidOperationException(
                    "expected counter amount candidate");
            Equal(FlatPromptChoiceKindV1.AssignAmount, amount.ChoiceKind);
            Equal(FlatPromptSourceSectionV1.CounterSources,
                amount.SourceSection);
            Equal(0, amount.SourceOrdinal);
            True(amount.Amount is >= 0 and <= 2);
        }

        FlatPromptSelectionHandleV1 firstHandle = Capture(
            session,
            "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:1");
        Equal(0UL, firstHandle.PromptInstanceOrdinal);
        Equal(0, firstHandle.ContinuationStep);
        False(session.TryResolveSelection(
            firstHandle,
            out _,
            out FlatPromptErrorCodeV1 scalarError));
        Equal(FlatPromptErrorCodeV1.InvalidContinuationAction, scalarError);
        FlatPromptContinuationStepResultV1 afterOne =
            session.TryApplySelection(firstHandle);
        True(afterOne.IsSuccess, afterOne.Error.ToString());
        False(afterOne.IsTerminal);
        NotNull(afterOne.Projection);
        Equal(0, afterOne.TerminalResponseBody.Count);
        EqualKeys(
            new[] { "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:1:2" },
            afterOne.Projection!.Candidates!);
        FlatPromptSelectionHandleV1 secondHandle = Capture(
            session,
            "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:1:2");
        FlatPromptContinuationStepResultV1 terminal =
            session.TryApplySelection(secondHandle);
        True(terminal.IsSuccess, terminal.Error.ToString());
        True(terminal.IsTerminal);
        Null(terminal.Projection);
        BytesEqual(
            new byte[] { 0x01, 0x00, 0x02, 0x00 },
            terminal.TerminalResponseBody.ToArray());
        False(session.TryResolveSelection(
            secondHandle,
            out _,
            out FlatPromptErrorCodeV1 staleAfterTerminal));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, staleAfterTerminal);

        RunTerminalPath(
            message,
            authority,
            "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:0",
            "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:1:3",
            new byte[] { 0x00, 0x00, 0x03, 0x00 });
        RunTerminalPath(
            message,
            authority,
            "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:2",
            "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:1:1",
            new byte[] { 0x02, 0x00, 0x01, 0x00 });
    }

    private static void AssertPruningAndDuplicateOccurrences()
    {
        Authority authority = CreateAuthority(
            new CardSpec(
                0x33333333,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new CardSpec(
                0x44444444,
                new ModernLocInfoV1(0, 0x04, 1, 0x05)));
        byte[] baseMessage = CounterMessage(
            0,
            7,
            4,
            new CounterEntry(
                0x33333333,
                new ModernLocInfoV1(0, 0x04, 0, 0),
                2),
            new CounterEntry(
                0x44444444,
                new ModernLocInfoV1(0, 0x04, 1, 0),
                3));
        FlatPromptProjectionResultV1 four = Accept(
            new FlatPromptSessionV1(),
            baseMessage,
            authority);
        AssertSuccess(four);
        EqualKeys(
            new[]
            {
                "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:1",
                "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:2"
            },
            four.Candidates!);

        byte[] exactTotal = CounterMessage(
            0,
            7,
            5,
            new CounterEntry(
                0x33333333,
                new ModernLocInfoV1(0, 0x04, 0, 0),
                2),
            new CounterEntry(
                0x44444444,
                new ModernLocInfoV1(0, 0x04, 1, 0),
                3));
        FlatPromptSessionV1 exactSession = new();
        FlatPromptProjectionResultV1 exact = Accept(
            exactSession,
            exactTotal,
            authority);
        AssertSuccess(exact);
        EqualKeys(
            new[] { "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:2" },
            exact.Candidates!);
        FlatPromptContinuationStepResultV1 exactAfter =
            exactSession.TryApplySelection(Capture(
                exactSession,
                "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:2"));
        True(exactAfter.IsSuccess, exactAfter.Error.ToString());
        EqualKeys(
            new[] { "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:1:3" },
            exactAfter.Projection!.Candidates!);

        Authority duplicateAuthority = CreateAuthority(
            new CardSpec(
                0x55555555,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        byte[] duplicateMessage = CounterMessage(
            0,
            8,
            2,
            new CounterEntry(
                0x55555555,
                new ModernLocInfoV1(0, 0x04, 0, 0),
                1),
            new CounterEntry(
                0x55555555,
                new ModernLocInfoV1(0, 0x04, 0, 0),
                2));
        FlatPromptSessionV1 duplicateSession = new();
        FlatPromptProjectionResultV1 duplicate = Accept(
            duplicateSession,
            duplicateMessage,
            duplicateAuthority);
        AssertSuccess(duplicate);
        FlatPromptCounterSelectionPublicContextV1 duplicateContext =
            (FlatPromptCounterSelectionPublicContextV1)duplicate.Context!;
        Equal(2, duplicateContext.Sources.Count);
        Equal(0, duplicateContext.Sources[0].SourceOrdinal);
        Equal(1, duplicateContext.Sources[1].SourceOrdinal);
        Equal("p0:MONSTER_ZONE:0",
            duplicateContext.Sources[0].PublicSemanticCardLocator.Value);
        Equal("p0:MONSTER_ZONE:0",
            duplicateContext.Sources[1].PublicSemanticCardLocator.Value);
        EqualKeys(
            new[]
            {
                "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:0",
                "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:1"
            },
            duplicate.Candidates!);
        RunTerminalPath(
            duplicateMessage,
            duplicateAuthority,
            "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:0",
            "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:1:2",
            new byte[] { 0x00, 0x00, 0x02, 0x00 });
        RunTerminalPath(
            duplicateMessage,
            duplicateAuthority,
            "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:1",
            "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:1:1",
            new byte[] { 0x01, 0x00, 0x01, 0x00 });
    }

    private static void AssertWireAndDomainFailures()
    {
        Authority authority = CreateAuthority(
            new CardSpec(
                0x66666666,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new CardSpec(
                0x77777777,
                new ModernLocInfoV1(0, 0x04, 1, 0x05)));
        byte[] valid = CounterMessage(
            0,
            1,
            3,
            new CounterEntry(
                0x66666666,
                new ModernLocInfoV1(0, 0x04, 0, 0),
                2),
            new CounterEntry(
                0x77777777,
                new ModernLocInfoV1(0, 0x04, 1, 0),
                3));

        AssertFailure(
            new FlatPromptSessionV1(),
            valid,
            FlatPromptErrorCodeV1.UnprovenPublicReference);
        AssertFailure(
            new FlatPromptSessionV1(),
            SetByte(valid, 1, 2),
            FlatPromptErrorCodeV1.InvalidParticipant);
        AssertFailure(
            new FlatPromptSessionV1(),
            SetByte(valid, 14, 2),
            FlatPromptErrorCodeV1.InvalidParticipant);
        AssertFailure(
            new FlatPromptSessionV1(),
            SetByte(valid, 15, 0x03),
            FlatPromptErrorCodeV1.InvalidLocation);
        AssertFailure(
            new FlatPromptSessionV1(),
            SetByte(valid, 15, 0x84),
            FlatPromptErrorCodeV1.UnprovenPublicReference);
        AssertFailure(
            new FlatPromptSessionV1(),
            SetU16(valid, 4, 0),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            new FlatPromptSessionV1(),
            CounterMessage(0, 1, 1),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            new FlatPromptSessionV1(),
            CounterMessage(
                0,
                1,
                1,
                new CounterEntry(
                    0x66666666,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    1)),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            new FlatPromptSessionV1(),
            CounterMessage(
                0,
                1,
                1,
                new CounterEntry(
                    0x66666666,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    0),
                new CounterEntry(
                    0x77777777,
                    new ModernLocInfoV1(0, 0x04, 1, 0),
                    1)),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            new FlatPromptSessionV1(),
            CounterMessage(
                0,
                1,
                3,
                new CounterEntry(
                    0x66666666,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    1),
                new CounterEntry(
                    0x77777777,
                    new ModernLocInfoV1(0, 0x04, 1, 0),
                    1)),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        AssertFailure(
            new FlatPromptSessionV1(),
            valid[..^1],
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            Append(valid, 0xAA),
            FlatPromptErrorCodeV1.MalformedPrompt);
        AssertFailure(
            new FlatPromptSessionV1(),
            SetU32(valid, 6, uint.MaxValue),
            FlatPromptErrorCodeV1.ArithmeticFailure);

        FlatPromptProjectionResultV1 addressed = Accept(
            new FlatPromptSessionV1(),
            valid,
            authority);
        AssertSuccess(addressed);
        FlatPromptCounterSelectionPublicContextV1 context =
            (FlatPromptCounterSelectionPublicContextV1)addressed.Context!;
        FlatPromptCounterContinuationStateV1 state =
            new(
                0,
                context.CounterType,
                context.RequiredTotal,
                context.Sources,
                Array.Empty<int>(),
                0);
        False(FlatPromptProjectionV1.TryAdvanceCounterContinuation(
            state,
            0,
            3,
            out _,
            out FlatPromptErrorCodeV1 overCapacityError));
        Equal(
            FlatPromptErrorCodeV1.InvalidContinuationAction,
            overCapacityError);
    }

    private static void AssertAtomicityStalenessOwnershipAndCodec()
    {
        Authority authority = CreateAuthority(
            new CardSpec(
                0x88888888,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new CardSpec(
                0x99999999,
                new ModernLocInfoV1(0, 0x04, 1, 0x05)));
        byte[] source = CounterMessage(
            0,
            2,
            3,
            new CounterEntry(
                0x88888888,
                new ModernLocInfoV1(0, 0x04, 0, 0),
                2),
            new CounterEntry(
                0x99999999,
                new ModernLocInfoV1(0, 0x04, 1, 0),
                3));
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 owned = Accept(
            session,
            source,
            authority);
        AssertSuccess(owned);
        string originalKey = owned.Candidates![0].I4LocalCandidateKey;
        FlatPromptCounterSelectionPublicContextV1 originalContext =
            (FlatPromptCounterSelectionPublicContextV1)owned.Context!;
        byte[] originalMessage = source.ToArray();
        source[0] = 0xFF;
        source[10] = 0xFE;
        Equal(
            (ushort)2,
            originalContext.Sources[0].Capacity);
        Equal(originalKey, owned.Candidates[0].I4LocalCandidateKey);
        FlatPromptContinuationStepResultV1 ownedAfter =
            session.TryApplySelection(Capture(session, originalKey));
        True(ownedAfter.IsSuccess, ownedAfter.Error.ToString());
        FlatPromptContinuationStepResultV1 ownedTerminal =
            session.TryApplySelection(Capture(
                session,
                "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:1:3"));
        True(ownedTerminal.IsTerminal);
        BytesEqual(
            new byte[] { 0x00, 0x00, 0x03, 0x00 },
            ownedTerminal.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 failureSession = new();
        AssertSuccess(Accept(failureSession, originalMessage, authority));
        FlatPromptSelectionHandleV1 oldHandle = Capture(
            failureSession,
            "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:0");
        FlatPromptProjectionResultV1 failed = failureSession.TryAcceptI5Prompt(
            Append(originalMessage, 0xAA),
            authority.Mirror,
            authority.Projection);
        AssertFailureResult(
            failed,
            FlatPromptErrorCodeV1.MalformedPrompt);
        False(failureSession.TryResolveSelection(
            oldHandle,
            out _,
            out FlatPromptErrorCodeV1 staleAfterFailure));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, staleAfterFailure);
        FlatPromptProjectionResultV1 reaccepted = Accept(
            failureSession,
            originalMessage,
            authority);
        AssertSuccess(reaccepted);
        FlatPromptSelectionHandleV1 reacceptedHandle = Capture(
            failureSession,
            "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:0");
        Equal(1UL, reacceptedHandle.PromptInstanceOrdinal);

        FlatPromptSelectionHandleV1 staleStepHandle = Capture(
            failureSession,
            "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:1");
        FlatPromptContinuationStepResultV1 afterStaleStep =
            failureSession.TryApplySelection(staleStepHandle);
        True(afterStaleStep.IsSuccess, afterStaleStep.Error.ToString());
        FlatPromptContinuationStepResultV1 staleStep =
            failureSession.TryApplySelection(staleStepHandle);
        False(staleStep.IsSuccess);
        Equal(FlatPromptErrorCodeV1.StaleContinuationStep, staleStep.Error);

        True(FlatPromptProjectionV1.TryEncodeCounterResponse(
                new[] { 0, ushort.MaxValue },
                out byte[] maxResponse,
                out FlatPromptErrorCodeV1 maxResponseError),
            maxResponseError.ToString());
        BytesEqual(
            new byte[] { 0x00, 0x00, 0xFF, 0xFF },
            maxResponse);
        False(FlatPromptProjectionV1.TryEncodeCounterResponse(
            new[] { -1 },
            out _,
            out FlatPromptErrorCodeV1 negativeError));
        Equal(FlatPromptErrorCodeV1.InvalidResponseBinding, negativeError);
        False(FlatPromptProjectionV1.TryEncodeCounterResponse(
            new[] { ushort.MaxValue + 1 },
            out _,
            out FlatPromptErrorCodeV1 overflowError));
        Equal(FlatPromptErrorCodeV1.InvalidResponseBinding, overflowError);

        True(PublicSemanticLocatorV1.TryCreateIndexed(
                0,
                PublicSemanticZoneV1.MonsterZone,
                0,
                out PublicSemanticLocatorV1? maximumLocator),
            "expected maximum-capacity locator");
        FlatPromptCounterSourcePublicDescriptorV1[] maximumSources =
        {
            new(0, ushort.MaxValue, maximumLocator!),
            new(1, 1, maximumLocator!)
        };
        FlatPromptCounterContinuationStateV1 maximumState = new(
            0,
            2,
            ushort.MaxValue,
            maximumSources,
            Array.Empty<int>(),
            0);
        True(FlatPromptProjectionV1.TryAdvanceCounterContinuation(
                maximumState,
                0,
                ushort.MaxValue,
                out FlatPromptProjectionDraftV1? maximumDraft,
                out FlatPromptErrorCodeV1 maximumError),
            maximumError.ToString());
        NotNull(maximumDraft);
        EqualKeys(
            new[] { "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:1:0" },
            maximumDraft!.CopyCandidates());
    }

    private static void AssertBindingAndPublicBoundary()
    {
        FlatPromptCounterAmountPublicCandidateV1 negative =
            new(
                "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:-1",
                0,
                -1);
        False(CurrentFlatPromptBindingV1.TryCreate(
            0,
            FlatPromptFamilyValueV1.MsgSelectCounter,
            new FlatPublicCandidateDescriptorV1[] { negative },
            new[] { negative.I4LocalCandidateKey },
            new[] { 0 },
            out CurrentFlatPromptBindingV1? binding,
            out FlatPromptErrorCodeV1 bindingError));
        Null(binding);
        Equal(FlatPromptErrorCodeV1.InvalidResponseBinding, bindingError);

        Type[] publicTypes =
        {
            typeof(FlatPromptCounterSourcePublicDescriptorV1),
            typeof(FlatPromptCounterSelectionPublicContextV1),
            typeof(FlatPromptCounterAmountPublicCandidateV1)
        };
        string[] forbidden =
        {
            "ResponseI32", "ResponseBody", "ModernLocInfo", "MirrorSnapshot",
            "MirrorEntityId", "ProtocolOffset", "SourceCardCode", "Raw",
            "Socket", "Network", "Timestamp", "Pid", "PrivateResponse",
            "ContinuationStep", "PromptInstanceOrdinal", "AssignedAmounts"
        };
        foreach (Type type in publicTypes)
        {
            True(type.IsPublic && type.IsSealed,
                "expected closed public counter type " + type.Name);
            foreach (PropertyInfo property in type.GetProperties())
            {
                False(
                    forbidden.Contains(property.Name, StringComparer.Ordinal),
                    "private counter property exposed by " + type.Name + "." +
                    property.Name);
            }
        }

        Null(typeof(FlatPromptCounterSourcePublicDescriptorV1).GetProperty(
            "SourceCardCode"));
        Null(typeof(FlatPromptCounterAmountPublicCandidateV1).GetProperty(
            "ResponseI32"));
        False(typeof(FlatPromptCounterContinuationStateV1).IsPublic);
        False(typeof(CurrentFlatPromptBindingV1).IsPublic);
        False(typeof(FlatPromptProjectionV1).IsPublic);
        False(typeof(FlatPromptSessionV1).GetMethods().Any(method =>
            method.IsPublic && method.Name.Contains(
                "Send",
                StringComparison.OrdinalIgnoreCase)));
    }

    private static void AssertLaterFamilyBoundary()
    {
        AssertFailureResult(
            new FlatPromptSessionV1().TryAcceptI5Prompt(new byte[] { 23 }),
            FlatPromptErrorCodeV1.UnsupportedPromptFamily);
        AssertFailureResult(
            new FlatPromptSessionV1().TryAcceptI5Prompt(new byte[] { 21 }),
            FlatPromptErrorCodeV1.UnsupportedPromptLayout);
        AssertFailureResult(
            new FlatPromptSessionV1().TryAcceptI5Prompt(new byte[] { 25 }),
            FlatPromptErrorCodeV1.UnsupportedPromptLayout);
        AssertFailureResult(
            new FlatPromptSessionV1().TryAcceptI5Prompt(new byte[] { 142 }),
            FlatPromptErrorCodeV1.UnsupportedPromptLayout);
    }

    private static void RunTerminalPath(
        byte[] message,
        Authority authority,
        string firstKey,
        string secondKey,
        byte[] expectedResponse)
    {
        FlatPromptSessionV1 session = new();
        AssertSuccess(Accept(session, message, authority));
        FlatPromptSelectionHandleV1 first = Capture(session, firstKey);
        FlatPromptContinuationStepResultV1 intermediate =
            session.TryApplySelection(first);
        True(intermediate.IsSuccess, intermediate.Error.ToString());
        False(intermediate.IsTerminal);
        FlatPromptContinuationStepResultV1 terminal =
            session.TryApplySelection(Capture(session, secondKey));
        True(terminal.IsSuccess, terminal.Error.ToString());
        True(terminal.IsTerminal);
        BytesEqual(expectedResponse, terminal.TerminalResponseBody.ToArray());
    }

    private static FlatPromptProjectionResultV1 Accept(
        FlatPromptSessionV1 session,
        byte[] message,
        Authority authority) =>
        session.TryAcceptI5Prompt(
            message,
            authority.Mirror,
            authority.Projection);

    private static void AssertSuccess(FlatPromptProjectionResultV1 result)
    {
        True(result.IsSuccess, result.Error.ToString());
        Equal(FlatPromptErrorCodeV1.None, result.Error);
        NotNull(result.Context);
        NotNull(result.Candidates);
        Equal(FlatPromptFamilyValueV1.MsgSelectCounter,
            result.Context!.PromptFamily);
        Equal(
            "ocgforge-ignis.combinatorial-prompt-continuation.v1",
            result.Context.ContractId);
    }

    private static void AssertFailure(
        FlatPromptSessionV1 session,
        byte[] message,
        FlatPromptErrorCodeV1 expectedError) =>
        AssertFailureResult(
            session.TryAcceptI5Prompt(message),
            expectedError);

    private static void AssertFailureResult(
        FlatPromptProjectionResultV1 result,
        FlatPromptErrorCodeV1 expectedError)
    {
        False(result.IsSuccess);
        Equal(expectedError, result.Error);
        Null(result.Context);
        Null(result.Candidates);
    }

    private static FlatPromptSelectionHandleV1 Capture(
        FlatPromptSessionV1 session,
        string key)
    {
        True(session.TryCaptureSelection(
                key,
                out FlatPromptSelectionHandleV1? handle,
                out FlatPromptErrorCodeV1 error),
            error.ToString());
        NotNull(handle);
        return handle!;
    }

    private static void EqualKeys(
        IReadOnlyList<string> expected,
        IReadOnlyList<FlatPublicCandidateDescriptorV1> candidates)
    {
        string[] actual = candidates
            .Select(candidate => candidate.I4LocalCandidateKey)
            .ToArray();
        True(expected.SequenceEqual(actual),
            $"expected keys [{string.Join(",", expected)}]; actual keys " +
            $"[{string.Join(",", actual)}]");
    }

    private static byte[] CounterMessage(
        byte actingPlayer,
        ushort counterType,
        ushort requiredTotal,
        params CounterEntry[] entries)
    {
        List<byte[]> parts = new()
        {
            new[] { (byte)22, actingPlayer },
            U16(counterType),
            U16(requiredTotal),
            U32((uint)entries.Length)
        };
        parts.AddRange(entries.Select(entry => Join(
            U32(entry.CardCode),
            new[]
            {
                entry.Location.Controller,
                entry.Location.Location,
                checked((byte)entry.Location.Sequence)
            },
            U16(entry.Capacity))));
        return Join(parts.ToArray());
    }

    private static byte[] U16(ushort value)
    {
        byte[] bytes = new byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        return bytes;
    }

    private static byte[] SetByte(byte[] source, int index, byte value)
    {
        byte[] copy = source.ToArray();
        copy[index] = value;
        return copy;
    }

    private static byte[] SetU16(byte[] source, int offset, ushort value)
    {
        byte[] copy = source.ToArray();
        BinaryPrimitives.WriteUInt16LittleEndian(copy.AsSpan(offset, 2), value);
        return copy;
    }

    private static byte[] SetU32(byte[] source, int offset, uint value)
    {
        byte[] copy = source.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(copy.AsSpan(offset, 4), value);
        return copy;
    }

    private static byte[] Append(byte[] source, byte value) =>
        source.Concat(new[] { value }).ToArray();

    private static Authority CreateAuthority(params CardSpec[] cards)
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                deckCount0: 8,
                extraCount0: 8,
                deckCount1: 8,
                extraCount1: 8);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        foreach (CardSpec card in cards)
        {
            MirrorApplyResult moved = mirror.Apply(DecodeMessage(
                decoder,
                MoveMessage(card.CardCode, empty, card.Location, 0)));
            True(moved.IsSuccess, moved.Error.ToString());
        }

        PublicStateProjectionResultV1 projection =
            PublicStateProjectionV1.TryProject(
                mirror.Snapshot,
                new PublicStateProjectionContextV1(0));
        True(projection.IsSuccess, projection.Error.ToString());
        return new Authority(mirror, projection);
    }

    private readonly record struct Authority(
        PerspectiveStateMirrorV1 Mirror,
        PublicStateProjectionResultV1 Projection);

    private readonly record struct CardSpec(
        uint CardCode,
        ModernLocInfoV1 Location);

    private readonly record struct CounterEntry(
        uint CardCode,
        ModernLocInfoV1 Location,
        ushort Capacity);
}
