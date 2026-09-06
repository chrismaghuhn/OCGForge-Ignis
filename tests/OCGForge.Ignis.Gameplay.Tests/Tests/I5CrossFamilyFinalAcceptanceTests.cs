using System.Collections;
using System.Reflection;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I5CrossFamilyFinalAcceptanceTests
{
    internal static void TestSupportedFamilyDispatchAndUnsupportedBoundary()
    {
        Authority authority = CreateAuthority();
        PromptCase[] cases = BuildCases(authority);
        foreach (PromptCase testCase in cases)
        {
            FlatPromptProjectionResultV1 result = Accept(testCase);
            AssertSuccess(result, testCase.Family);
        }

        HashSet<byte> supported = new()
        {
            15, 20, 26, 18, 24, 140, 141, 22, 25, 21, 143
        };
        for (int rawId = byte.MinValue; rawId <= byte.MaxValue; rawId++)
        {
            byte id = (byte)rawId;
            if (supported.Contains(id))
            {
                continue;
            }

            FlatPromptProjectionResultV1 unsupported =
                new FlatPromptSessionV1().TryAcceptI5Prompt(new[] { id });
            FlatPromptErrorCodeV1 expected = id == 23
                ? FlatPromptErrorCodeV1.UnsupportedPromptFamily
                : FlatPromptErrorCodeV1.UnsupportedPromptLayout;
            AssertFailureResult(unsupported, expected);
        }

        AssertFailureResult(
            new FlatPromptSessionV1().TryAcceptI5Prompt(new byte[] { 23 }),
            FlatPromptErrorCodeV1.UnsupportedPromptFamily);
        AssertFailureResult(
            new FlatPromptSessionV1().TryAcceptI5Prompt(new byte[] { 142 }),
            FlatPromptErrorCodeV1.UnsupportedPromptLayout);
    }

    internal static void TestCrossFamilyBindingLifecycle()
    {
        Authority authority = CreateAuthority();
        PromptCase[] cases = BuildCases(authority);
        FlatPromptSessionV1 session = new();
        FlatPromptSelectionHandleV1? previous = null;
        foreach (PromptCase testCase in cases)
        {
            FlatPromptProjectionResultV1 result = Accept(session, testCase);
            AssertSuccess(result, testCase.Family);
            if (previous is not null)
            {
                FlatPromptContinuationStepResultV1 stale =
                    session.TryApplySelection(previous);
                False(stale.IsSuccess,
                    "a prior family handle crossed a family boundary");
                Equal(
                    FlatPromptErrorCodeV1.InvalidContinuationInstance,
                    stale.Error);
            }

            previous = Capture(session, testCase.FirstKey);
        }
    }

    internal static void TestFailureAtomicityAndOrdinalIsolation()
    {
        byte[] valid = SortMessage(
            25,
            0,
            new SortEntry(1, 0, 0x01, 0),
            new SortEntry(2, 0, 0x01, 1));
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 first =
            session.TryAcceptI5Prompt(valid);
        AssertSuccess(first, FlatPromptFamilyValueV1.MsgSortCard);
        FlatPromptSelectionHandleV1 oldHandle = Capture(
            session,
            "MSG_SORT_CARD:PLACE:0");

        AssertFailureResult(
            session.TryAcceptI5Prompt(new byte[] { 25 }),
            FlatPromptErrorCodeV1.MalformedPrompt);
        False(session.TryResolveSelection(
            oldHandle,
            out _,
            out FlatPromptErrorCodeV1 staleAfterPromptFailure));
        Equal(
            FlatPromptErrorCodeV1.StalePromptBinding,
            staleAfterPromptFailure);
        FlatPromptProjectionResultV1 second =
            session.TryAcceptI5Prompt(valid);
        AssertSuccess(second, FlatPromptFamilyValueV1.MsgSortCard);
        Equal(
            1UL,
            Capture(session, "MSG_SORT_CARD:PLACE:0").PromptInstanceOrdinal);

        FlatPromptSessionV1 domainFailureSession = new();
        AssertSuccess(
            domainFailureSession.TryAcceptI5Prompt(valid),
            FlatPromptFamilyValueV1.MsgSortCard);
        FlatPromptSelectionHandleV1 domainFailureOld = Capture(
            domainFailureSession,
            "MSG_SORT_CARD:PLACE:0");
        AssertFailureResult(
            domainFailureSession.TryAcceptI5Prompt(
                new byte[] { 18, 0, 0, 0, 0, 0, 0 }),
            FlatPromptErrorCodeV1.UnprovenCandidateDomain);
        False(domainFailureSession.TryResolveSelection(
            domainFailureOld,
            out _,
            out FlatPromptErrorCodeV1 domainFailureStale));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, domainFailureStale);
        AssertSuccess(
            domainFailureSession.TryAcceptI5Prompt(valid),
            FlatPromptFamilyValueV1.MsgSortCard);
        Equal(
            1UL,
            Capture(domainFailureSession, "MSG_SORT_CARD:PLACE:0")
                .PromptInstanceOrdinal);

        FlatPromptSessionV1 continuationSession = new();
        AssertSuccess(
            continuationSession.TryAcceptI5Prompt(valid),
            FlatPromptFamilyValueV1.MsgSortCard);
        FlatPromptSelectionHandleV1 current = Capture(
            continuationSession,
            "MSG_SORT_CARD:PLACE:0");
        FlatPromptContinuationStepResultV1 intermediate =
            continuationSession.TryApplySelection(current);
        True(intermediate.IsSuccess, intermediate.Error.ToString());
        Equal(0, intermediate.TerminalResponseBody.Count);
        FlatPromptContinuationStepResultV1 staleStep =
            continuationSession.TryApplySelection(current);
        False(staleStep.IsSuccess, "stale continuation step was accepted");
        Equal(FlatPromptErrorCodeV1.StaleContinuationStep, staleStep.Error);
        FlatPromptContinuationStepResultV1 terminal =
            continuationSession.TryApplySelection(Capture(
                continuationSession,
                "MSG_SORT_CARD:PLACE:1"));
        True(terminal.IsTerminal, terminal.Error.ToString());
        BytesEqual(new byte[] { 0x00, 0x01 },
            terminal.TerminalResponseBody.ToArray());
    }

    internal static void TestCompleteDomainsAndResponseIsolation()
    {
        FlatPromptSessionV1 n1Session = new();
        FlatPromptProjectionResultV1 n1 = n1Session.TryAcceptI5Prompt(
            SelectCardMessage(
                0,
                1,
                1,
                (0x01020304,
                    new ModernLocInfoV1(0, 0, 0, 0))));
        AssertSuccess(n1, FlatPromptFamilyValueV1.MsgSelectCard);
        Equal(1, n1.Candidates!.Count);
        Equal(
            "MSG_SELECT_CARD:PICK:0",
            n1.Candidates[0].I4LocalCandidateKey);
        Equal(0UL, Capture(n1Session, "MSG_SELECT_CARD:PICK:0")
            .PromptInstanceOrdinal);

        FlatPromptSessionV1 duplicateSession = new();
        FlatPromptProjectionResultV1 duplicate =
            duplicateSession.TryAcceptI5Prompt(SelectCardMessage(
                0,
                1,
                2,
                (0xAABBCCDD, new ModernLocInfoV1(0, 0, 0, 0)),
                (0xAABBCCDD, new ModernLocInfoV1(0, 0, 0, 0))));
        AssertSuccess(duplicate, FlatPromptFamilyValueV1.MsgSelectCard);
        AssertKeys(
            new[]
            {
                "MSG_SELECT_CARD:PICK:0",
                "MSG_SELECT_CARD:PICK:1"
            },
            duplicate.Candidates!);
        False(duplicateSession.TryCaptureSelection(
                "MSG_SELECT_TRIBUTE:PICK:0",
                out FlatPromptSelectionHandleV1? wrongFamilyHandle,
                out FlatPromptErrorCodeV1 wrongFamilyError));
        Null(wrongFamilyHandle);
        Equal(
            FlatPromptErrorCodeV1.InvalidI4LocalCandidateKey,
            wrongFamilyError);
        FlatPromptContinuationStepResultV1 duplicateIntermediate =
            duplicateSession.TryApplySelection(Capture(
                duplicateSession,
                "MSG_SELECT_CARD:PICK:1"));
        True(duplicateIntermediate.IsSuccess,
            duplicateIntermediate.Error.ToString());
        FlatPromptContinuationStepResultV1 duplicateTerminal =
            duplicateSession.TryApplySelection(Capture(
                duplicateSession,
                "MSG_SELECT_CARD:FINISH"));
        True(duplicateTerminal.IsTerminal,
            duplicateTerminal.Error.ToString());
        BytesEqual(
            new byte[] { 0x03, 0x00, 0x00, 0x00, 0x02 },
            duplicateTerminal.TerminalResponseBody.ToArray());

        FlatPromptSessionV1 sortDuplicateSession = new();
        FlatPromptProjectionResultV1 sortDuplicate =
            sortDuplicateSession.TryAcceptI5Prompt(SortMessage(
                25,
                0,
                new SortEntry(0xAABBCCDD, 0, 0x01, 0),
                new SortEntry(0xAABBCCDD, 0, 0x01, 0)));
        AssertSuccess(sortDuplicate, FlatPromptFamilyValueV1.MsgSortCard);
        Equal(2, sortDuplicate.Candidates!.Count(candidate =>
            candidate is FlatPromptSortPublicCandidateBaseV1));

        Authority authority = CreateAuthority();
        FlatPromptSessionV1 counterSession = new();
        FlatPromptProjectionResultV1 counter = Accept(
            counterSession,
            new PromptCase(
                "counter",
                CounterMessage(
                    0,
                    4,
                    1,
                    new CounterEntry(
                        0x11111111,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        1),
                    new CounterEntry(
                        0x22222222,
                        new ModernLocInfoV1(0, 0x04, 1, 0),
                        1)),
                authority,
                FlatPromptFamilyValueV1.MsgSelectCounter,
                "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:0"));
        AssertSuccess(counter, FlatPromptFamilyValueV1.MsgSelectCounter);
        FlatPromptContinuationStepResultV1 counterAfter =
            counterSession.TryApplySelection(Capture(
                counterSession,
                "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:0"));
        True(counterAfter.IsSuccess, counterAfter.Error.ToString());
        FlatPromptContinuationStepResultV1 counterTerminal =
            counterSession.TryApplySelection(Capture(
                counterSession,
                "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:1:1"));
        True(counterTerminal.IsTerminal, counterTerminal.Error.ToString());
        BytesEqual(new byte[] { 0x00, 0x00, 0x01, 0x00 },
            counterTerminal.TerminalResponseBody.ToArray());
    }

    internal static void TestPublicPrivateAuthorityDeterminismBarrier()
    {
        AssertPublicBoundary();
        Authority authority = CreateAuthority();
        FlatPromptProjectionResultV1 authoritative = Accept(
            new PromptCase(
                "authoritative sort",
                SortMessage(
                    25,
                    0,
                    new SortEntry(
                        0x11111111,
                        0,
                        0x04,
                        0)),
                authority,
                FlatPromptFamilyValueV1.MsgSortCard,
                "MSG_SORT_CARD:PLACE:0"));
        AssertSuccess(authoritative, FlatPromptFamilyValueV1.MsgSortCard);
        True(authoritative.Candidates![0]
            is FlatPromptSortLocatorPromptCodePublicCandidateV1);
        Equal(
            "p0:MONSTER_ZONE:0",
            ((FlatPromptSortLocatorPromptCodePublicCandidateV1)
                authoritative.Candidates[0]).PublicSemanticCardLocator.Value);

        PromptCase[] cases = BuildCases(authority);
        foreach (PromptCase testCase in cases)
        {
            FlatPromptProjectionResultV1 first = Accept(testCase);
            FlatPromptProjectionResultV1 second = Accept(testCase);
            AssertSuccess(first, testCase.Family);
            AssertSuccess(second, testCase.Family);
            Equal(PublicSignature(first), PublicSignature(second));

            FlatPromptSessionV1 firstSession = new();
            FlatPromptSessionV1 secondSession = new();
            AssertSuccess(Accept(firstSession, testCase), testCase.Family);
            AssertSuccess(Accept(secondSession, testCase), testCase.Family);
            FlatPromptSelectionHandleV1 firstHandle = Capture(
                firstSession,
                testCase.FirstKey);
            FlatPromptSelectionHandleV1 secondHandle = Capture(
                secondSession,
                testCase.FirstKey);
            bool firstResolved = firstSession.TryResolveSelection(
                firstHandle,
                out FlatPromptResponseResolutionV1 firstResponse,
                out FlatPromptErrorCodeV1 firstError);
            bool secondResolved = secondSession.TryResolveSelection(
                secondHandle,
                out FlatPromptResponseResolutionV1 secondResponse,
                out FlatPromptErrorCodeV1 secondError);
            Equal(firstResolved, secondResolved);
            Equal(firstError, secondError);
            if (firstResolved)
            {
                Equal(firstResponse.ResponseI32, secondResponse.ResponseI32);
            }
        }

        FlatPromptSessionV1 promptLocalSession = new();
        PromptCase firstMainDeck = new(
            "main deck one",
            SortMessage(
                25,
                0,
                new SortEntry(0xCAFEBABE, 0, 0x01, 0)),
            null,
            FlatPromptFamilyValueV1.MsgSortCard,
            "MSG_SORT_CARD:PLACE:0");
        PromptCase secondMainDeck = new(
            "main deck two",
            SortMessage(
                25,
                0,
                new SortEntry(0xCAFEBABE, 0, 0x01, 1)),
            null,
            FlatPromptFamilyValueV1.MsgSortCard,
            "MSG_SORT_CARD:PLACE:0");
        AssertSuccess(Accept(promptLocalSession, firstMainDeck),
            FlatPromptFamilyValueV1.MsgSortCard);
        FlatPromptSelectionHandleV1 oldPromptHandle = Capture(
            promptLocalSession,
            firstMainDeck.FirstKey);
        AssertSuccess(Accept(promptLocalSession, secondMainDeck),
            FlatPromptFamilyValueV1.MsgSortCard);
        FlatPromptContinuationStepResultV1 oldPromptResult =
            promptLocalSession.TryApplySelection(oldPromptHandle);
        False(oldPromptResult.IsSuccess,
            "prompt-local CardCode became persistent continuity");
        Equal(
            FlatPromptErrorCodeV1.InvalidContinuationInstance,
            oldPromptResult.Error);
    }

    private static void AssertPublicBoundary()
    {
        string[] forbidden =
        {
            "ResponseI32", "ResponseBody", "ModernLocInfo", "MirrorSnapshot",
            "MirrorEntityId", "ProtocolOffset", "SourceCardCode",
            "SourceLocation", "RawBytes", "Socket", "Network", "PrivateResponse",
            "ContinuationStep", "PromptInstanceOrdinal", "AssignedAmounts",
            "PlacedSourceOrdinals"
        };
        Assembly assembly = typeof(FlatPromptSessionV1).Assembly;
        Type[] publicTypes = assembly.GetTypes()
            .Where(type => type.IsPublic &&
                type.Namespace == "OCGForge.Ignis.Gameplay" &&
                type.Name.StartsWith("FlatPrompt", StringComparison.Ordinal))
            .ToArray();
        foreach (Type type in publicTypes)
        {
            True(type.IsAbstract || type.IsSealed || type.IsEnum,
                "public I5 type must be closed: " + type.Name);
            foreach (PropertyInfo property in type.GetProperties())
            {
                False(
                    forbidden.Contains(property.Name, StringComparer.Ordinal),
                    "private value exposed by " + type.Name + "." +
                    property.Name);
            }
        }

        string[] publicMemberNames = publicTypes
            .SelectMany(type => type.GetProperties()
                .Select(property => property.Name)
                .Concat(type.GetMethods().Select(method => method.Name)))
            .ToArray();
        False(publicMemberNames.Any(name =>
            name.Contains("PublicActionKey", StringComparison.Ordinal) ||
            name.Contains("ModelInput", StringComparison.Ordinal) ||
            name.Contains("Checkpoint", StringComparison.Ordinal) ||
            name.Contains("Policy", StringComparison.Ordinal)));

        False(typeof(FlatPromptContinuationStateV1).IsPublic);
        False(typeof(FlatPromptSortContinuationStateV1).IsPublic);
        False(typeof(CurrentFlatPromptBindingV1).IsPublic);
        False(typeof(FlatPromptProjectionV1).IsPublic);
        False(typeof(FlatPromptSelectionHandleV1).IsPublic);
        False(typeof(FlatPromptSessionV1).GetMethods().Any(method =>
            method.IsPublic && method.Name.Contains(
                "Send",
                StringComparison.OrdinalIgnoreCase)));
    }

    private static void AssertKeys(
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

    private static PromptCase[] BuildCases(Authority authority) =>
        new[]
        {
            new PromptCase(
                "select-card",
                SelectCardMessage(
                    0,
                    1,
                    1,
                    (0x11111111, new ModernLocInfoV1(0, 0, 0, 0))),
                null,
                FlatPromptFamilyValueV1.MsgSelectCard,
                "MSG_SELECT_CARD:PICK:0"),
            new PromptCase(
                "select-tribute",
                SelectTributeMessage(
                    0,
                    false,
                    1,
                    2,
                    (0x11111111,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        (byte)1),
                    (0x22222222,
                        new ModernLocInfoV1(0, 0x04, 1, 0),
                        (byte)1)),
                authority,
                FlatPromptFamilyValueV1.MsgSelectTribute,
                "MSG_SELECT_TRIBUTE:PICK:0"),
            new PromptCase(
                "select-unselect",
                SelectUnselectMessage(
                    0,
                    false,
                    false,
                    1,
                    1,
                    new[]
                    {
                        (0x11111111u,
                            new ModernLocInfoV1(0, 0x04, 0, 0))
                    },
                    new[]
                    {
                        (0x22222222u,
                            new ModernLocInfoV1(0, 0x04, 1, 0))
                    }),
                authority,
                FlatPromptFamilyValueV1.MsgSelectUnselectCard,
                "MSG_SELECT_UNSELECT_CARD:SELECT:0"),
            new PromptCase(
                "announce-number",
                AnnounceNumberMessage(0, 17),
                null,
                FlatPromptFamilyValueV1.MsgAnnounceNumber,
                "MSG_ANNOUNCE_NUMBER:OPTION:0"),
            new PromptCase(
                "place",
                PlaceMessage(18, 0, 1, 0),
                null,
                FlatPromptFamilyValueV1.MsgSelectPlace,
                "MSG_SELECT_PLACE:PICK:0:MONSTER_ZONE:0"),
            new PromptCase(
                "disfield",
                PlaceMessage(24, 0, 1, 0),
                null,
                FlatPromptFamilyValueV1.MsgSelectDisfield,
                "MSG_SELECT_DISFIELD:PICK:0:MONSTER_ZONE:0"),
            new PromptCase(
                "race",
                RaceMessage(0, 1, 1UL),
                null,
                FlatPromptFamilyValueV1.MsgAnnounceRace,
                "MSG_ANNOUNCE_RACE:PICK:0"),
            new PromptCase(
                "attribute",
                AttributeMessage(0, 1, 1u),
                null,
                FlatPromptFamilyValueV1.MsgAnnounceAttrib,
                "MSG_ANNOUNCE_ATTRIB:PICK:0"),
            new PromptCase(
                "counter",
                CounterMessage(
                    0,
                    4,
                    1,
                    new CounterEntry(
                        0x11111111,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        1),
                    new CounterEntry(
                        0x22222222,
                        new ModernLocInfoV1(0, 0x04, 1, 0),
                        1)),
                authority,
                FlatPromptFamilyValueV1.MsgSelectCounter,
                "MSG_SELECT_COUNTER:ASSIGN_AMOUNT:0:0"),
            new PromptCase(
                "sort-card",
                SortMessage(
                    25,
                    0,
                    new SortEntry(1, 0, 0x01, 0)),
                null,
                FlatPromptFamilyValueV1.MsgSortCard,
                "MSG_SORT_CARD:PLACE:0"),
            new PromptCase(
                "sort-chain",
                SortMessage(
                    21,
                    0,
                    new SortEntry(1, 0, 0x01, 0)),
                null,
                FlatPromptFamilyValueV1.MsgSortChain,
                "MSG_SORT_CHAIN:PLACE:0")
        };

    private static FlatPromptProjectionResultV1 Accept(PromptCase testCase)
    {
        FlatPromptSessionV1 session = new();
        return Accept(session, testCase);
    }

    private static FlatPromptProjectionResultV1 Accept(
        FlatPromptSessionV1 session,
        PromptCase testCase) =>
        testCase.Authority is null
            ? session.TryAcceptI5Prompt(testCase.Message)
            : session.TryAcceptI5Prompt(
                testCase.Message,
                testCase.Authority!.Value.Mirror,
                testCase.Authority!.Value.Projection);

    private static FlatPromptProjectionResultV1 Accept(
        FlatPromptSessionV1 session,
        byte[] message,
        Authority authority) =>
        session.TryAcceptI5Prompt(
            message,
            authority.Mirror,
            authority.Projection);

    private static void AssertSuccess(
        FlatPromptProjectionResultV1 result,
        FlatPromptFamilyV1 family)
    {
        True(result.IsSuccess, result.Error.ToString());
        Equal(FlatPromptErrorCodeV1.None, result.Error);
        NotNull(result.Context);
        NotNull(result.Candidates);
        Equal(family, result.Context!.PromptFamily);
        Equal(
            "ocgforge-ignis.combinatorial-prompt-continuation.v1",
            result.Context.ContractId);
    }

    private static void AssertFailureResult(
        FlatPromptProjectionResultV1 result,
        FlatPromptErrorCodeV1 expectedError)
    {
        False(result.IsSuccess, "unexpected successful result: " + result.Error);
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

    private static string PublicSignature(FlatPromptProjectionResultV1 result)
    {
        return string.Join(
            "|",
            result.IsSuccess,
            result.Error,
            ValueSignature(result.Context),
            ValueSignature(result.Candidates));
    }

    private static string ValueSignature(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is string text)
        {
            return "string:" + text;
        }

        Type type = value.GetType();
        if (type.IsEnum || type.IsPrimitive)
        {
            return type.FullName + ":" + value;
        }

        if (value is IEnumerable sequence)
        {
            return "[" + string.Join(
                ",",
                sequence.Cast<object?>().Select(ValueSignature)) + "]";
        }

        PropertyInfo[] properties = type.GetProperties()
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();
        return type.FullName + "{" + string.Join(
            ",",
            properties.Select(property =>
                property.Name + "=" + ValueSignature(property.GetValue(value)))) +
            "}";
    }

    private static byte[] SelectCardMessage(
        byte actingPlayer,
        uint minimum,
        uint maximum,
        params (uint Code, ModernLocInfoV1 Location)[] entries)
    {
        List<byte[]> parts = new()
        {
            new byte[] { 15, actingPlayer, 0 },
            U32(minimum),
            U32(maximum),
            U32((uint)entries.Length)
        };
        parts.AddRange(entries.Select(entry => Join(
            U32(entry.Code),
            LocInfo(
                entry.Location.Controller,
                entry.Location.Location,
                entry.Location.Sequence,
                entry.Location.Position))));
        return Join(parts.ToArray());
    }

    private static byte[] SelectTributeMessage(
        byte actingPlayer,
        bool cancelable,
        uint minimum,
        uint maximum,
        params (uint Code, ModernLocInfoV1 Location, byte ReleaseValue)[] entries)
    {
        List<byte[]> parts = new()
        {
            new byte[]
            {
                20,
                actingPlayer,
                cancelable ? (byte)1 : (byte)0
            },
            U32(minimum),
            U32(maximum),
            U32((uint)entries.Length)
        };
        parts.AddRange(entries.Select(entry => Join(
            U32(entry.Code),
            new[] { entry.Location.Controller, entry.Location.Location },
            U32(entry.Location.Sequence),
            new[] { entry.ReleaseValue })));
        return Join(parts.ToArray());
    }

    private static byte[] SelectUnselectMessage(
        byte actingPlayer,
        bool finishable,
        bool cancelable,
        uint minimum,
        uint maximum,
        IReadOnlyList<(uint Code, ModernLocInfoV1 Location)> selectable,
        IReadOnlyList<(uint Code, ModernLocInfoV1 Location)> unselectable)
    {
        List<byte[]> parts = new()
        {
            new byte[]
            {
                26,
                actingPlayer,
                finishable ? (byte)1 : (byte)0,
                cancelable ? (byte)1 : (byte)0
            },
            U32(minimum),
            U32(maximum),
            U32((uint)selectable.Count)
        };
        parts.AddRange(selectable.Select(entry => Join(
            U32(entry.Code),
            LocInfo(
                entry.Location.Controller,
                entry.Location.Location,
                entry.Location.Sequence,
                entry.Location.Position))));
        parts.Add(U32((uint)unselectable.Count));
        parts.AddRange(unselectable.Select(entry => Join(
            U32(entry.Code),
            LocInfo(
                entry.Location.Controller,
                entry.Location.Location,
                entry.Location.Sequence,
                entry.Location.Position))));
        return Join(parts.ToArray());
    }

    private static byte[] AnnounceNumberMessage(
        byte actingPlayer,
        params ulong[] values)
    {
        List<byte[]> parts = new()
        {
            new byte[] { 143, actingPlayer, checked((byte)values.Length) }
        };
        parts.AddRange(values.Select(U64));
        return Join(parts.ToArray());
    }

    private static byte[] PlaceMessage(
        byte messageId,
        byte actingPlayer,
        byte requiredCount,
        uint fieldFlag) =>
        Join(
            new byte[] { messageId, actingPlayer, requiredCount },
            U32(fieldFlag));

    private static byte[] RaceMessage(
        byte actingPlayer,
        byte requiredCount,
        ulong availableMask) =>
        Join(new byte[] { 140, actingPlayer, requiredCount }, U64(availableMask));

    private static byte[] AttributeMessage(
        byte actingPlayer,
        byte requiredCount,
        uint availableMask) =>
        Join(
            new byte[] { 141, actingPlayer, requiredCount },
            U32(availableMask));

    private static byte[] CounterMessage(
        byte actingPlayer,
        ushort counterType,
        ushort requiredTotal,
        params CounterEntry[] entries)
    {
        List<byte[]> parts = new()
        {
            new byte[] { 22, actingPlayer },
            U16(counterType),
            U16(requiredTotal),
            U32((uint)entries.Length)
        };
        parts.AddRange(entries.Select(entry => Join(
            U32(entry.Code),
            new[]
            {
                entry.Location.Controller,
                entry.Location.Location,
                checked((byte)entry.Location.Sequence)
            },
            U16(entry.Capacity))));
        return Join(parts.ToArray());
    }

    private static byte[] SortMessage(
        byte messageId,
        byte actingPlayer,
        params SortEntry[] entries)
    {
        List<byte[]> parts = new()
        {
            new byte[] { messageId, actingPlayer },
            U32((uint)entries.Length)
        };
        parts.AddRange(entries.Select(entry => Join(
            U32(entry.Code),
            new[] { entry.Controller },
            U32(entry.Location),
            U32(entry.Sequence))));
        return Join(parts.ToArray());
    }

    private static byte[] U16(ushort value)
    {
        byte[] bytes = new byte[sizeof(ushort)];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(
            bytes,
            value);
        return bytes;
    }

    private static Authority CreateAuthority()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                deckCount0: 8,
                extraCount0: 8,
                deckCount1: 8,
                extraCount1: 8);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        CardSpec[] cards =
        {
            new(0x11111111, new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new(0x22222222, new ModernLocInfoV1(0, 0x04, 1, 0x05))
        };
        foreach (CardSpec card in cards)
        {
            MirrorApplyResult moved = mirror.Apply(DecodeMessage(
                decoder,
                MoveMessage(card.Code, empty, card.Location, 0)));
            True(moved.IsSuccess, moved.Error.ToString());
        }

        PublicStateProjectionResultV1 projection =
            PublicStateProjectionV1.TryProject(
                mirror.Snapshot,
                new PublicStateProjectionContextV1(0));
        True(projection.IsSuccess, projection.Error.ToString());
        return new Authority(mirror, projection);
    }

    private readonly record struct CardSpec(uint Code, ModernLocInfoV1 Location);

    private readonly record struct CounterEntry(
        uint Code,
        ModernLocInfoV1 Location,
        ushort Capacity);

    private readonly record struct SortEntry(
        uint Code,
        byte Controller,
        uint Location,
        uint Sequence);

    private readonly record struct PromptCase(
        string Name,
        byte[] Message,
        Authority? Authority,
        FlatPromptFamilyV1 Family,
        string FirstKey);

    private readonly record struct Authority(
        PerspectiveStateMirrorV1 Mirror,
        PublicStateProjectionResultV1 Projection);
}
