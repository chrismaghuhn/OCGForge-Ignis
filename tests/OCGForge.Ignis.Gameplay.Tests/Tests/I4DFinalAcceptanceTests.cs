using System.Buffers.Binary;
using System.Globalization;
using System.Reflection;
using OCGForge.Ignis.Gameplay;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I4DFinalAcceptanceTests
{
    private static readonly byte[] ExpectedFamilyIds =
    {
        10, 11, 12, 13, 14, 16, 19
    };

    internal static void TestSevenFamilySupportAndUnsupportedBoundary()
    {
        byte[] actualFamilyIds = Enum.GetValues<FlatPromptFamilyV1>()
            .Select(value => (byte)value)
            .OrderBy(value => value)
            .ToArray();
        True(
            ExpectedFamilyIds.SequenceEqual(actualFamilyIds),
            "I4 family enum set changed");

        Authority authority = CreateAuthority(
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));

        AssertSuccess(
            new FlatPromptSessionV1().TryAcceptPrompt(YesNoMessage()),
            FlatPromptFamilyV1.MsgSelectYesNo);
        AssertSuccess(
            new FlatPromptSessionV1().TryAcceptPrompt(OptionMessage(7)),
            FlatPromptFamilyV1.MsgSelectOption);
        AssertSuccess(
            new FlatPromptSessionV1().TryAcceptPrompt(PositionMessage()),
            FlatPromptFamilyV1.MsgSelectPosition);
        AssertSuccess(
            Accept(
                new FlatPromptSessionV1(),
                FlatPromptFamilyV1.MsgSelectEffectYn,
                EffectMessage(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    7),
                authority),
            FlatPromptFamilyV1.MsgSelectEffectYn);
        AssertSuccess(
            Accept(
                new FlatPromptSessionV1(),
                FlatPromptFamilyV1.MsgSelectChain,
                ChainMessage(
                    false,
                    new ChainEntrySpec(
                        0x11223344,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        7,
                        0)),
                authority),
            FlatPromptFamilyV1.MsgSelectChain);
        AssertSuccess(
            Accept(
                new FlatPromptSessionV1(),
                FlatPromptFamilyV1.MsgSelectBattleCmd,
                BattleMessage(
                    new[]
                    {
                        new BattleActivationSpec(
                            0x11223344,
                            new ModernLocInfoV1(0, 0x04, 0, 0),
                            7,
                            0)
                    },
                    Array.Empty<BattleAttackSpec>(),
                    0,
                    0),
                authority),
            FlatPromptFamilyV1.MsgSelectBattleCmd);
        AssertSuccess(
            Accept(
                new FlatPromptSessionV1(),
                FlatPromptFamilyV1.MsgSelectIdleCmd,
                IdleMessage(
                    Array.Empty<IdleCardSpec>(),
                    Array.Empty<IdleCardSpec>(),
                    Array.Empty<IdleCardSpec>(),
                    Array.Empty<IdleCardSpec>(),
                    Array.Empty<IdleCardSpec>(),
                    new[]
                    {
                        new IdleActivationSpec(
                            0x11223344,
                            new ModernLocInfoV1(0, 0x04, 0, 0),
                            7,
                            0)
                    },
                    0,
                    0,
                    0),
                authority),
            FlatPromptFamilyV1.MsgSelectIdleCmd);

        HashSet<byte> supported = ExpectedFamilyIds.ToHashSet();
        for (int rawId = byte.MinValue; rawId <= byte.MaxValue; rawId++)
        {
            byte id = (byte)rawId;
            if (supported.Contains(id))
            {
                continue;
            }

            AssertFailureResult(
                new FlatPromptSessionV1().TryAcceptPrompt(new[] { id }),
                FlatPromptErrorCodeV1.UnsupportedPromptLayout);
            AssertFailureResult(
                new FlatPromptSessionV1().TryAcceptPrompt(
                    new[] { id },
                    authority.Mirror,
                    authority.Projection),
                FlatPromptErrorCodeV1.UnsupportedPromptLayout);
        }
    }

    internal static void TestCrossFamilyBindingLifecycle()
    {
        Authority authority = CreateAuthority(
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));
        FlatPromptSessionV1 session = new();
        List<FlatPromptSelectionHandleV1> previousHandles = new();

        FlatPromptProjectionResultV1 yesNo = Accept(
            session,
            FlatPromptFamilyV1.MsgSelectYesNo,
            YesNoMessage(),
            authority);
        AssertSuccess(yesNo, FlatPromptFamilyV1.MsgSelectYesNo);
        FlatPromptSelectionHandleV1 currentHandle = CaptureAndResolve(
            session,
            "MSG_SELECT_YESNO:NO",
            0);
        previousHandles.Add(currentHandle);

        FlatPromptProjectionResultV1 option = Accept(
            session,
            FlatPromptFamilyV1.MsgSelectOption,
            OptionMessage(7),
            authority);
        AssertSuccess(option, FlatPromptFamilyV1.MsgSelectOption);
        AssertAllStale(session, previousHandles);
        FlatPromptSelectionHandleV1 nextHandle = CaptureAndResolve(
            session,
            "MSG_SELECT_OPTION:OPTION:0",
            0);
        AssertNextOrdinal(currentHandle, nextHandle);
        previousHandles.Add(nextHandle);
        currentHandle = nextHandle;

        FlatPromptProjectionResultV1 position = Accept(
            session,
            FlatPromptFamilyV1.MsgSelectPosition,
            PositionMessage(),
            authority);
        AssertSuccess(position, FlatPromptFamilyV1.MsgSelectPosition);
        AssertAllStale(session, previousHandles);
        nextHandle = CaptureAndResolve(
            session,
            "MSG_SELECT_POSITION:FACEUP_ATTACK",
            1);
        AssertNextOrdinal(currentHandle, nextHandle);
        previousHandles.Add(nextHandle);
        currentHandle = nextHandle;

        FlatPromptProjectionResultV1 effect = Accept(
            session,
            FlatPromptFamilyV1.MsgSelectEffectYn,
            EffectMessage(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0),
                7),
            authority);
        AssertSuccess(effect, FlatPromptFamilyV1.MsgSelectEffectYn);
        AssertAllStale(session, previousHandles);
        nextHandle = CaptureAndResolve(
            session,
            "MSG_SELECT_EFFECTYN:NO",
            0);
        AssertNextOrdinal(currentHandle, nextHandle);
        previousHandles.Add(nextHandle);
        currentHandle = nextHandle;

        FlatPromptProjectionResultV1 chain = Accept(
            session,
            FlatPromptFamilyV1.MsgSelectChain,
            ChainMessage(
                false,
                new ChainEntrySpec(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    7,
                    0)),
            authority);
        AssertSuccess(chain, FlatPromptFamilyV1.MsgSelectChain);
        AssertAllStale(session, previousHandles);
        nextHandle = CaptureAndResolve(
            session,
            "MSG_SELECT_CHAIN:CHAIN_ENTRY:0",
            0);
        AssertNextOrdinal(currentHandle, nextHandle);
        previousHandles.Add(nextHandle);
        currentHandle = nextHandle;

        FlatPromptProjectionResultV1 battle = Accept(
            session,
            FlatPromptFamilyV1.MsgSelectBattleCmd,
            BattleMessage(
                new[]
                {
                    new BattleActivationSpec(
                        0x11223344,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        7,
                        0)
                },
                Array.Empty<BattleAttackSpec>(),
                0,
                0),
            authority);
        AssertSuccess(battle, FlatPromptFamilyV1.MsgSelectBattleCmd);
        AssertAllStale(session, previousHandles);
        nextHandle = CaptureAndResolve(
            session,
            "MSG_SELECT_BATTLECMD:ACTIVATE:0",
            0);
        AssertNextOrdinal(currentHandle, nextHandle);
        previousHandles.Add(nextHandle);
        currentHandle = nextHandle;

        FlatPromptProjectionResultV1 idle = Accept(
            session,
            FlatPromptFamilyV1.MsgSelectIdleCmd,
            IdleMessage(
                Array.Empty<IdleCardSpec>(),
                Array.Empty<IdleCardSpec>(),
                Array.Empty<IdleCardSpec>(),
                Array.Empty<IdleCardSpec>(),
                Array.Empty<IdleCardSpec>(),
                new[]
                {
                    new IdleActivationSpec(
                        0x11223344,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        7,
                        0)
                },
                0,
                0,
                0),
            authority);
        AssertSuccess(idle, FlatPromptFamilyV1.MsgSelectIdleCmd);
        AssertAllStale(session, previousHandles);
        nextHandle = CaptureAndResolve(
            session,
            "MSG_SELECT_IDLECMD:ACTIVATE:0",
            5);
        AssertNextOrdinal(currentHandle, nextHandle);
        previousHandles.Add(nextHandle);
        currentHandle = nextHandle;

        FlatPromptProjectionResultV1 repeatedYesNo = Accept(
            session,
            FlatPromptFamilyV1.MsgSelectYesNo,
            YesNoMessage(),
            authority);
        AssertSuccess(repeatedYesNo, FlatPromptFamilyV1.MsgSelectYesNo);
        AssertAllStale(session, previousHandles);
        nextHandle = CaptureAndResolve(
            session,
            "MSG_SELECT_YESNO:NO",
            0);
        AssertNextOrdinal(currentHandle, nextHandle);

        FlatPromptSelectionHandleV1 wrongFamilyHandle =
            new(
                nextHandle.PromptInstanceOrdinal,
                FlatPromptFamilyV1.MsgSelectOption,
                nextHandle.I4LocalCandidateKey,
                nextHandle.OrderedDomain);
        AssertStale(session, wrongFamilyHandle);
    }

    internal static void TestFailureAtomicityAndOrdinalIsolation()
    {
        Authority authority = CreateAuthority(
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));

        FlatPromptSessionV1 yesNoSession = new();
        AssertSuccess(
            Accept(
                yesNoSession,
                FlatPromptFamilyV1.MsgSelectYesNo,
                YesNoMessage(),
                authority),
            FlatPromptFamilyV1.MsgSelectYesNo);
        FlatPromptSelectionHandleV1 yesNoHandle = CaptureAndResolve(
            yesNoSession,
            "MSG_SELECT_YESNO:NO",
            0);
        AssertFailedPromptDoesNotAdvance(
            yesNoSession,
            yesNoHandle,
            yesNoSession.TryAcceptPrompt(new byte[] { 14, 0 }),
            FlatPromptErrorCodeV1.MalformedPrompt,
            FlatPromptFamilyV1.MsgSelectYesNo,
            YesNoMessage(),
            authority,
            "MSG_SELECT_YESNO:NO");

        FlatPromptSessionV1 optionSession = new();
        AssertSuccess(
            Accept(
                optionSession,
                FlatPromptFamilyV1.MsgSelectOption,
                OptionMessage(7),
                authority),
            FlatPromptFamilyV1.MsgSelectOption);
        FlatPromptSelectionHandleV1 optionHandle = CaptureAndResolve(
            optionSession,
            "MSG_SELECT_OPTION:OPTION:0",
            0);
        AssertFailedPromptDoesNotAdvance(
            optionSession,
            optionHandle,
            optionSession.TryAcceptPrompt(
                EffectMessage(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    7),
                authority.Mirror,
                null),
            FlatPromptErrorCodeV1.UnprovenPublicReference,
            FlatPromptFamilyV1.MsgSelectOption,
            OptionMessage(7),
            authority,
            "MSG_SELECT_OPTION:OPTION:0");

        FlatPromptSessionV1 effectSession = new();
        AssertSuccess(
            Accept(
                effectSession,
                FlatPromptFamilyV1.MsgSelectEffectYn,
                EffectMessage(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    7),
                authority),
            FlatPromptFamilyV1.MsgSelectEffectYn);
        FlatPromptSelectionHandleV1 effectHandle = CaptureAndResolve(
            effectSession,
            "MSG_SELECT_EFFECTYN:NO",
            0);
        AssertFailedPromptDoesNotAdvance(
            effectSession,
            effectHandle,
            Accept(
                effectSession,
                FlatPromptFamilyV1.MsgSelectBattleCmd,
                BattleMessage(
                    Array.Empty<BattleActivationSpec>(),
                    Array.Empty<BattleAttackSpec>(),
                    0,
                    0),
                authority),
            FlatPromptErrorCodeV1.ZeroOptionDomain,
            FlatPromptFamilyV1.MsgSelectEffectYn,
            EffectMessage(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0),
                7),
            authority,
            "MSG_SELECT_EFFECTYN:NO");

        FlatPromptSessionV1 battleSession = new();
        AssertSuccess(
            Accept(
                battleSession,
                FlatPromptFamilyV1.MsgSelectBattleCmd,
                BattleMessage(
                    new[]
                    {
                        new BattleActivationSpec(
                            0x11223344,
                            new ModernLocInfoV1(0, 0x04, 0, 0),
                            7,
                            0)
                    },
                    Array.Empty<BattleAttackSpec>(),
                    0,
                    0),
                authority),
            FlatPromptFamilyV1.MsgSelectBattleCmd);
        FlatPromptSelectionHandleV1 battleHandle = CaptureAndResolve(
            battleSession,
            "MSG_SELECT_BATTLECMD:ACTIVATE:0",
            0);
        AssertFailedPromptDoesNotAdvance(
            battleSession,
            battleHandle,
            Accept(
                battleSession,
                FlatPromptFamilyV1.MsgSelectIdleCmd,
                IdleMessage(
                    Array.Empty<IdleCardSpec>(),
                    Array.Empty<IdleCardSpec>(),
                    Array.Empty<IdleCardSpec>(),
                    Array.Empty<IdleCardSpec>(),
                    Array.Empty<IdleCardSpec>(),
                    Array.Empty<IdleActivationSpec>(),
                    0,
                    0,
                    0),
                authority),
            FlatPromptErrorCodeV1.ZeroOptionDomain,
            FlatPromptFamilyV1.MsgSelectBattleCmd,
            BattleMessage(
                new[]
                {
                    new BattleActivationSpec(
                        0x11223344,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        7,
                        0)
                },
                Array.Empty<BattleAttackSpec>(),
                0,
                0),
            authority,
            "MSG_SELECT_BATTLECMD:ACTIVATE:0");

        FlatPromptProjectionResultV1 yesNoResult =
            new FlatPromptSessionV1().TryAcceptPrompt(YesNoMessage());
        FlatPromptProjectionResultV1 optionResult =
            new FlatPromptSessionV1().TryAcceptPrompt(OptionMessage(7));
        FlatPublicCandidateDescriptorV1 noCandidate =
            yesNoResult.Candidates![0];
        FlatPublicCandidateDescriptorV1 optionCandidate =
            optionResult.Candidates![0];

        AssertInvalidBinding(
            FlatPromptFamilyV1.MsgSelectOption,
            new[] { noCandidate },
            new[] { noCandidate.I4LocalCandidateKey },
            new[] { 0 });
        AssertInvalidBinding(
            FlatPromptFamilyV1.MsgSelectYesNo,
            new[] { optionCandidate },
            new[] { optionCandidate.I4LocalCandidateKey },
            new[] { 0 });
        AssertInvalidBinding(
            FlatPromptFamilyV1.MsgSelectYesNo,
            new[] { noCandidate },
            new[] { "MSG_SELECT_YESNO:YES" },
            new[] { 0 });
        AssertInvalidBinding(
            FlatPromptFamilyV1.MsgSelectYesNo,
            new[] { noCandidate },
            new[] { noCandidate.I4LocalCandidateKey },
            new[] { 1 });
        AssertInvalidBinding(
            FlatPromptFamilyV1.MsgSelectYesNo,
            new[] { noCandidate, noCandidate },
            new[] { noCandidate.I4LocalCandidateKey, noCandidate.I4LocalCandidateKey },
            new[] { 0, 0 });
    }

    internal static void TestCompleteDomainsAndResponseIsolation()
    {
        Authority authority = CreateAuthority(
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new CardSpec(
                0x55667788,
                new ModernLocInfoV1(0, 0x04, 1, 0x05)),
            new CardSpec(
                0x01020304,
                new ModernLocInfoV1(0, 0x02, 0, 0x08)),
            new CardSpec(
                0x05060708,
                new ModernLocInfoV1(0, 0x02, 1, 0x08)));

        AssertResponseForFamily(
            FlatPromptFamilyV1.MsgSelectYesNo,
            YesNoMessage(),
            authority,
            "MSG_SELECT_YESNO:NO",
            0);
        AssertResponseForFamily(
            FlatPromptFamilyV1.MsgSelectOption,
            OptionMessage(7),
            authority,
            "MSG_SELECT_OPTION:OPTION:0",
            0);
        AssertResponseForFamily(
            FlatPromptFamilyV1.MsgSelectPosition,
            PositionMessage(),
            authority,
            "MSG_SELECT_POSITION:FACEUP_ATTACK",
            1);
        AssertResponseForFamily(
            FlatPromptFamilyV1.MsgSelectEffectYn,
            EffectMessage(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0),
                7),
            authority,
            "MSG_SELECT_EFFECTYN:NO",
            0);
        AssertResponseForFamily(
            FlatPromptFamilyV1.MsgSelectChain,
            ChainMessage(
                false),
            authority,
            "MSG_SELECT_CHAIN:NO_CHAIN",
            -1);
        AssertResponseForFamily(
            FlatPromptFamilyV1.MsgSelectBattleCmd,
            BattleMessage(
                Array.Empty<BattleActivationSpec>(),
                new[]
                {
                    new BattleAttackSpec(
                        0x11223344,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        true)
                },
                0,
                0),
            authority,
            "MSG_SELECT_BATTLECMD:ATTACK:0",
            1);
        AssertResponseForFamily(
            FlatPromptFamilyV1.MsgSelectIdleCmd,
            IdleMessage(
                Array.Empty<IdleCardSpec>(),
                Array.Empty<IdleCardSpec>(),
                Array.Empty<IdleCardSpec>(),
                Array.Empty<IdleCardSpec>(),
                Array.Empty<IdleCardSpec>(),
                new[]
                {
                    new IdleActivationSpec(
                        0x11223344,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        7,
                        0)
                },
                0,
                0,
                0),
            authority,
            "MSG_SELECT_IDLECMD:ACTIVATE:0",
            5);

        FlatPromptProjectionResultV1 duplicateOptions =
            new FlatPromptSessionV1().TryAcceptPrompt(
                OptionMessage(7, 7));
        AssertSuccess(
            duplicateOptions,
            FlatPromptFamilyV1.MsgSelectOption);
        Equal(2, duplicateOptions.Candidates!.Count);
        Equal(
            "MSG_SELECT_OPTION:OPTION:0",
            duplicateOptions.Candidates[0].I4LocalCandidateKey);
        Equal(
            "MSG_SELECT_OPTION:OPTION:1",
            duplicateOptions.Candidates[1].I4LocalCandidateKey);
        Equal(
            7ul,
            ((FlatOptionPublicCandidateDescriptorV1)
                duplicateOptions.Candidates[0]).OptionValue);
        Equal(
            7ul,
            ((FlatOptionPublicCandidateDescriptorV1)
                duplicateOptions.Candidates[1]).OptionValue);

        FlatPromptProjectionResultV1 duplicateChain = Accept(
            new FlatPromptSessionV1(),
            FlatPromptFamilyV1.MsgSelectChain,
            ChainMessage(
                false,
                new ChainEntrySpec(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    7,
                    0),
                new ChainEntrySpec(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    7,
                    0)),
            authority);
        AssertSuccess(duplicateChain, FlatPromptFamilyV1.MsgSelectChain);
        Equal(3, duplicateChain.Candidates!.Count);
        Equal(
            "MSG_SELECT_CHAIN:CHAIN_ENTRY:0",
            duplicateChain.Candidates[0].I4LocalCandidateKey);
        Equal(
            "MSG_SELECT_CHAIN:CHAIN_ENTRY:1",
            duplicateChain.Candidates[1].I4LocalCandidateKey);
        Equal(
            "MSG_SELECT_CHAIN:NO_CHAIN",
            duplicateChain.Candidates[2].I4LocalCandidateKey);

        FlatPromptProjectionResultV1 duplicateBattle = Accept(
            new FlatPromptSessionV1(),
            FlatPromptFamilyV1.MsgSelectBattleCmd,
            BattleMessage(
                Array.Empty<BattleActivationSpec>(),
                new[]
                {
                    new BattleAttackSpec(
                        0x11223344,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        true),
                    new BattleAttackSpec(
                        0x11223344,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        true)
                },
                0,
                0),
            authority);
        AssertSuccess(duplicateBattle, FlatPromptFamilyV1.MsgSelectBattleCmd);
        Equal(2, duplicateBattle.Candidates!.Count);
        NotEqual(
            duplicateBattle.Candidates[0].I4LocalCandidateKey,
            duplicateBattle.Candidates[1].I4LocalCandidateKey);

        FlatPromptProjectionResultV1 duplicateIdle = Accept(
            new FlatPromptSessionV1(),
            FlatPromptFamilyV1.MsgSelectIdleCmd,
            IdleMessage(
                new[]
                {
                    new IdleCardSpec(
                        0x01020304,
                        new ModernLocInfoV1(0, 0x02, 0, 0)),
                    new IdleCardSpec(
                        0x05060708,
                        new ModernLocInfoV1(0, 0x02, 1, 0))
                },
                Array.Empty<IdleCardSpec>(),
                Array.Empty<IdleCardSpec>(),
                Array.Empty<IdleCardSpec>(),
                Array.Empty<IdleCardSpec>(),
                Array.Empty<IdleActivationSpec>(),
                0,
                0,
                0),
            authority);
        AssertSuccess(duplicateIdle, FlatPromptFamilyV1.MsgSelectIdleCmd);
        Equal(2, duplicateIdle.Candidates!.Count);
        Equal(
            "MSG_SELECT_IDLECMD:SUMMON:0",
            duplicateIdle.Candidates[0].I4LocalCandidateKey);
        Equal(
            "MSG_SELECT_IDLECMD:SUMMON:1",
            duplicateIdle.Candidates[1].I4LocalCandidateKey);

        FlatPromptProjectionResultV1 oneCandidate = Accept(
            new FlatPromptSessionV1(),
            FlatPromptFamilyV1.MsgSelectBattleCmd,
            BattleMessage(
                new[]
                {
                    new BattleActivationSpec(
                        0x11223344,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        7,
                        0)
                },
                Array.Empty<BattleAttackSpec>(),
                0,
                0),
            authority);
        AssertSuccess(oneCandidate, FlatPromptFamilyV1.MsgSelectBattleCmd);
        Equal(1, oneCandidate.Candidates!.Count);
        AssertResponseForFamily(
            FlatPromptFamilyV1.MsgSelectYesNo,
            YesNoMessage(),
            authority,
            "MSG_SELECT_YESNO:NO",
            0);
        FlatPromptSessionV1 collisionSession = new();
        AssertSuccess(
            Accept(
                collisionSession,
                FlatPromptFamilyV1.MsgSelectYesNo,
                YesNoMessage(),
                authority),
            FlatPromptFamilyV1.MsgSelectYesNo);
        FlatPromptSelectionHandleV1 collisionHandle = CaptureAndResolve(
            collisionSession,
            "MSG_SELECT_YESNO:NO",
            0);
        AssertSuccess(
            Accept(
                collisionSession,
                FlatPromptFamilyV1.MsgSelectEffectYn,
                EffectMessage(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    7),
                authority),
            FlatPromptFamilyV1.MsgSelectEffectYn);
        AssertStale(collisionSession, collisionHandle);
    }

    internal static void TestPublicPrivateAuthorityDeterminismBarrier()
    {
        Assembly gameplayAssembly = typeof(FlatPromptSessionV1).Assembly;
        Type[] publicI4Types = gameplayAssembly.GetTypes()
            .Where(type =>
                type.IsPublic &&
                (typeof(FlatPromptPublicContextV1).IsAssignableFrom(type) ||
                 typeof(FlatPublicCandidateDescriptorV1)
                     .IsAssignableFrom(type)))
            .ToArray();
        True(publicI4Types.Length > 0);

        string[] forbiddenPropertyNames =
        {
            "ResponseI32", "ResponseBody", "RawBytes", "ModernLocInfo",
            "MirrorEntityId", "MirrorSnapshot", "ProtocolOffset", "Socket",
            "Network", "Room", "Password", "Host", "Port", "Timestamp",
            "Pid", "Thread", "Task", "Path", "PromptInstanceOrdinal",
            "PublicActionKey", "Model", "Checkpoint"
        };
        foreach (Type type in publicI4Types)
        {
            True(type.IsAbstract || type.IsSealed, type.Name);
            foreach (PropertyInfo property in type.GetProperties())
            {
                False(
                    forbiddenPropertyNames.Contains(
                        property.Name,
                        StringComparer.OrdinalIgnoreCase),
                    type.Name + "." + property.Name);
            }
        }

        PropertyInfo[] cardCodeProperties = publicI4Types
            .Select(type => type.GetProperty(
                "CardCode",
                BindingFlags.Instance | BindingFlags.Public))
            .Where(property => property is not null)
            .Cast<PropertyInfo>()
            .ToArray();
        True(cardCodeProperties.Length > 0);
        foreach (PropertyInfo property in cardCodeProperties)
        {
            True(
                property.DeclaringType!.Name.Contains(
                    "CardCode",
                    StringComparison.Ordinal),
                property.DeclaringType.Name);
        }

        Type[] transitionTypes = publicI4Types
            .Where(type => type.Name.Contains(
                "Transition",
                StringComparison.Ordinal) ||
                type.Name.Contains("Phase", StringComparison.Ordinal) ||
                type.Name.Contains("Shuffle", StringComparison.Ordinal))
            .ToArray();
        foreach (Type type in transitionTypes)
        {
            False(type.GetProperties().Any(property =>
                property.Name is "CardCode" or
                    "PublicSemanticCardLocator" or
                    "SourceOrdinal"));
        }

        False(typeof(FlatPromptSessionV1).GetMethods(
            BindingFlags.Instance | BindingFlags.Public)
            .Any(method =>
                method.Name.Contains("Send", StringComparison.OrdinalIgnoreCase) ||
                method.Name.Contains("Network", StringComparison.OrdinalIgnoreCase) ||
                method.Name.Contains("Model", StringComparison.OrdinalIgnoreCase)));

        Authority authority = CreateAuthority(
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)));

        FlatPromptProjectionResultV1 effect = Accept(
            new FlatPromptSessionV1(),
            FlatPromptFamilyV1.MsgSelectEffectYn,
            EffectMessage(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0),
                7),
            authority);
        AssertSuccess(effect, FlatPromptFamilyV1.MsgSelectEffectYn);
        Equal(
            authority.Projection.Snapshot!.Cards.Single().Locator,
            ((FlatPromptEffectYnPublicContextBaseV1)effect.Context!)
                .EffectCardLocator);

        FlatPromptProjectionResultV1 unsafeCode = Accept(
            new FlatPromptSessionV1(),
            FlatPromptFamilyV1.MsgSelectBattleCmd,
            BattleMessage(
                new[]
                {
                    new BattleActivationSpec(
                        0,
                        new ModernLocInfoV1(0, 0x04, 0, 0),
                        7,
                        0)
                },
                Array.Empty<BattleAttackSpec>(),
                0,
                0),
            authority);
        AssertSuccess(unsafeCode, FlatPromptFamilyV1.MsgSelectBattleCmd);
        True(unsafeCode.Candidates![0].GetType() ==
            typeof(FlatBattleActivatablePublicCandidateV1));

        Authority mainDeckAuthority = CreateAuthority(
            new CardSpec(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0x05)),
            new CardSpec(
                0x55667788,
                new ModernLocInfoV1(0, 0x01, 0, 0x01)));
        AssertFailureResult(
            Accept(
                new FlatPromptSessionV1(),
                FlatPromptFamilyV1.MsgSelectBattleCmd,
                BattleMessage(
                    new[]
                    {
                        new BattleActivationSpec(
                            0x55667788,
                            new ModernLocInfoV1(0, 0x01, 0, 0),
                            7,
                            0)
                    },
                    Array.Empty<BattleAttackSpec>(),
                    0,
                    0),
                mainDeckAuthority),
            FlatPromptErrorCodeV1.UnprovenPublicReference);

        PublicCardStateV1 acceptedCard = authority.Projection.Snapshot!.Cards.Single();
        PublicStateSnapshotV1 ambiguousSnapshot = WithCards(
            authority.Projection.Snapshot,
            new[] { acceptedCard, acceptedCard });
        PublicStateProjectionResultV1 ambiguousProjection =
            PublicStateProjectionResultV1.Success(
                ambiguousSnapshot,
                authority.Projection.CanonicalBytes.ToArray(),
                authority.Projection.Sha256!);
        AssertFailureResult(
            Accept(
                new FlatPromptSessionV1(),
                FlatPromptFamilyV1.MsgSelectBattleCmd,
                BattleMessage(
                    new[]
                    {
                        new BattleActivationSpec(
                            0x11223344,
                            new ModernLocInfoV1(0, 0x04, 0, 0),
                            7,
                            0)
                    },
                    Array.Empty<BattleAttackSpec>(),
                    0,
                    0),
                new Authority(
                    authority.Mirror,
                    authority.Decoder,
                    ambiguousProjection)),
            FlatPromptErrorCodeV1.UnprovenPublicReference);

        AssertFailureResult(
            Accept(
                new FlatPromptSessionV1(),
                FlatPromptFamilyV1.MsgSelectBattleCmd,
                BattleMessage(
                    new[]
                    {
                        new BattleActivationSpec(
                            0,
                            new ModernLocInfoV1(0, 0x84, 0, 0),
                            7,
                            0)
                    },
                    Array.Empty<BattleAttackSpec>(),
                    0,
                    0),
                authority),
            FlatPromptErrorCodeV1.UnprovenPublicReference);
        AssertFailureResult(
            Accept(
                new FlatPromptSessionV1(),
                FlatPromptFamilyV1.MsgSelectIdleCmd,
                IdleMessage(
                    Array.Empty<IdleCardSpec>(),
                    Array.Empty<IdleCardSpec>(),
                    Array.Empty<IdleCardSpec>(),
                    Array.Empty<IdleCardSpec>(),
                    Array.Empty<IdleCardSpec>(),
                    new[]
                    {
                        new IdleActivationSpec(
                            0,
                            new ModernLocInfoV1(0, 0x84, 0, 0),
                            7,
                            0)
                    },
                    0,
                    0,
                    0),
                authority),
            FlatPromptErrorCodeV1.UnprovenPublicReference);

        byte[] changedCanonical = authority.Projection.CanonicalBytes.ToArray();
        changedCanonical[0] ^= 1;
        AssertFailureResult(
            AcceptWithProjection(
                FlatPromptFamilyV1.MsgSelectEffectYn,
                EffectMessage(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    7),
                authority,
                PublicStateProjectionResultV1.Success(
                    authority.Projection.Snapshot!,
                    changedCanonical,
                    authority.Projection.Sha256!)),
            FlatPromptErrorCodeV1.AuthorityMismatch);

        AssertFailureResult(
            AcceptWithProjection(
                FlatPromptFamilyV1.MsgSelectEffectYn,
                EffectMessage(
                    0x11223344,
                    new ModernLocInfoV1(0, 0x04, 0, 0),
                    7),
                authority,
                PublicStateProjectionResultV1.Success(
                    authority.Projection.Snapshot!,
                    authority.Projection.CanonicalBytes.ToArray(),
                    authority.Projection.Sha256! + "0")),
            FlatPromptErrorCodeV1.AuthorityMismatch);

        FlatPromptProjectionResultV1 position =
            new FlatPromptSessionV1().TryAcceptPrompt(
                PositionMessage(0xCAFEBABE, 0x0D));
        AssertSuccess(position, FlatPromptFamilyV1.MsgSelectPosition);
        False(position.Context!.GetType().GetProperties()
            .Any(property => property.Name is
                "PositionCardCode" or
                "CardCode" or
                "PublicSemanticCardLocator"));
        True(position.Candidates!.All(candidate =>
            !candidate.GetType().GetProperties().Any(property =>
                property.Name is "CardCode" or
                    "PublicSemanticCardLocator")));

        string firstSignature = PublicResultSignature(effect);
        FlatPromptProjectionResultV1 secondEffect = Accept(
            new FlatPromptSessionV1(),
            FlatPromptFamilyV1.MsgSelectEffectYn,
            EffectMessage(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0),
                7),
            authority);
        string secondSignature = PublicResultSignature(secondEffect);
        Equal(firstSignature, secondSignature);
        AssertResponseForFamily(
            FlatPromptFamilyV1.MsgSelectEffectYn,
            EffectMessage(
                0x11223344,
                new ModernLocInfoV1(0, 0x04, 0, 0),
                7),
            authority,
            "MSG_SELECT_EFFECTYN:NO",
            0);
    }

    private static void AssertSuccess(
        FlatPromptProjectionResultV1 result,
        FlatPromptFamilyV1 family)
    {
        True(result.IsSuccess, result.Error.ToString());
        Equal(FlatPromptErrorCodeV1.None, result.Error);
        NotNull(result.Context);
        NotNull(result.Candidates);
        Equal(family, result.Context!.PromptFamily);
    }

    private static void AssertFailureResult(
        FlatPromptProjectionResultV1 result,
        FlatPromptErrorCodeV1 error)
    {
        False(result.IsSuccess);
        Equal(error, result.Error);
        Null(result.Context);
        Null(result.Candidates);
    }

    private static FlatPromptProjectionResultV1 Accept(
        FlatPromptSessionV1 session,
        FlatPromptFamilyV1 family,
        byte[] message,
        Authority authority)
    {
        return family is
            FlatPromptFamilyV1.MsgSelectYesNo or
            FlatPromptFamilyV1.MsgSelectOption or
            FlatPromptFamilyV1.MsgSelectPosition
            ? session.TryAcceptPrompt(message)
            : session.TryAcceptPrompt(
                message,
                authority.Mirror,
                authority.Projection);
    }

    private static FlatPromptProjectionResultV1 AcceptWithProjection(
        FlatPromptFamilyV1 family,
        byte[] message,
        Authority authority,
        PublicStateProjectionResultV1 projection)
    {
        return new FlatPromptSessionV1().TryAcceptPrompt(
            message,
            authority.Mirror,
            projection);
    }

    private static FlatPromptSelectionHandleV1 CaptureAndResolve(
        FlatPromptSessionV1 session,
        string key,
        int expectedResponse)
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
        Equal(expectedResponse, response.ResponseI32);
        return handle!;
    }

    private static void AssertAllStale(
        FlatPromptSessionV1 session,
        IEnumerable<FlatPromptSelectionHandleV1> handles)
    {
        foreach (FlatPromptSelectionHandleV1 handle in handles)
        {
            AssertStale(session, handle);
        }
    }

    private static void AssertStale(
        FlatPromptSessionV1 session,
        FlatPromptSelectionHandleV1 handle)
    {
        False(session.TryResolveSelection(
            handle,
            out _,
            out FlatPromptErrorCodeV1 error));
        Equal(FlatPromptErrorCodeV1.StalePromptBinding, error);
    }

    private static void AssertNextOrdinal(
        FlatPromptSelectionHandleV1 previous,
        FlatPromptSelectionHandleV1 next) =>
        Equal(previous.PromptInstanceOrdinal + 1, next.PromptInstanceOrdinal);

    private static void AssertFailedPromptDoesNotAdvance(
        FlatPromptSessionV1 session,
        FlatPromptSelectionHandleV1 oldHandle,
        FlatPromptProjectionResultV1 failure,
        FlatPromptErrorCodeV1 expectedError,
        FlatPromptFamilyV1 validFamily,
        byte[] validMessage,
        Authority authority,
        string validKey)
    {
        AssertFailureResult(failure, expectedError);
        AssertStale(session, oldHandle);
        FlatPromptProjectionResultV1 accepted = Accept(
            session,
            validFamily,
            validMessage,
            authority);
        AssertSuccess(accepted, validFamily);
        FlatPromptSelectionHandleV1 newHandle = CaptureAndResolve(
            session,
            validKey,
            ResponseForKey(validFamily, validKey));
        AssertNextOrdinal(oldHandle, newHandle);
    }

    private static int ResponseForKey(
        FlatPromptFamilyV1 family,
        string key) =>
        (family, key) switch
        {
            (FlatPromptFamilyV1.MsgSelectYesNo, "MSG_SELECT_YESNO:NO") => 0,
            (FlatPromptFamilyV1.MsgSelectOption, "MSG_SELECT_OPTION:OPTION:0") => 0,
            (FlatPromptFamilyV1.MsgSelectEffectYn, "MSG_SELECT_EFFECTYN:NO") => 0,
            (FlatPromptFamilyV1.MsgSelectBattleCmd,
                "MSG_SELECT_BATTLECMD:ACTIVATE:0") => 0,
            _ => throw new InvalidOperationException(
                "missing I4D response table entry")
        };

    private static void AssertInvalidBinding(
        FlatPromptFamilyV1 family,
        FlatPublicCandidateDescriptorV1[] candidates,
        string[] keys,
        int[] responses)
    {
        False(CurrentFlatPromptBindingV1.TryCreate(
            0,
            family,
            candidates,
            keys,
            responses,
            out CurrentFlatPromptBindingV1? binding,
            out FlatPromptErrorCodeV1 error));
        Null(binding);
        Equal(FlatPromptErrorCodeV1.InvalidResponseBinding, error);
    }

    private static void AssertResponseForFamily(
        FlatPromptFamilyV1 family,
        byte[] message,
        Authority authority,
        string key,
        int response)
    {
        FlatPromptSessionV1 session = new();
        FlatPromptProjectionResultV1 result = Accept(
            session,
            family,
            message,
            authority);
        AssertSuccess(result, family);
        _ = CaptureAndResolve(session, key, response);
    }

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
        return new Authority(mirror, decoder, projection);
    }

    private static PublicStateSnapshotV1 WithCards(
        PublicStateSnapshotV1 source,
        IEnumerable<PublicCardStateV1> cards) =>
        new(
            source.PerspectivePlayer,
            source.DuelFlags,
            source.TurnCount,
            source.TurnPlayer,
            source.Phase,
            source.Terminal,
            source.Participants,
            cards);

    private static string PublicResultSignature(
        FlatPromptProjectionResultV1 result)
    {
        string context = result.Context is null
            ? "null"
            : result.Context.GetType().Name + ":" +
              string.Join(
                  "|",
                  result.Context.GetType()
                      .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                      .OrderBy(property => property.Name)
                      .Select(property => FormatValue(
                          property.GetValue(result.Context))));
        string candidates = result.Candidates is null
            ? "null"
            : string.Join(
                ";",
                result.Candidates.Select(candidate =>
                    candidate.GetType().Name + ":" +
                    string.Join(
                        "|",
                        candidate.GetType()
                            .GetProperties(
                                BindingFlags.Instance | BindingFlags.Public)
                            .OrderBy(property => property.Name)
                            .Select(property => FormatValue(
                                property.GetValue(candidate))))));
        return result.IsSuccess + "|" + result.Error + "|" + context + "|" +
               candidates;
    }

    private static string FormatValue(object? value) =>
        value switch
        {
            null => "null",
            IFormattable formattable => formattable.ToString(
                null,
                CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };

    private static byte[] YesNoMessage(ulong description = 7) =>
        Join(new[] { (byte)13, (byte)0 }, U64(description));

    private static byte[] OptionMessage(params ulong[] values)
    {
        if (values.Length > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(values));
        }

        List<byte[]> parts = new()
        {
            new[] { (byte)14, (byte)0, (byte)values.Length }
        };
        parts.AddRange(values.Select(U64));
        return Join(parts.ToArray());
    }

    private static byte[] PositionMessage(
        uint cardCode = 0xCAFEBABE,
        byte mask = 0x0D) =>
        Join(
            new[] { (byte)19, (byte)0 },
            U32(cardCode),
            new[] { mask });

    private static byte[] EffectMessage(
        uint cardCode,
        ModernLocInfoV1 location,
        ulong description) =>
        Join(
            new[] { (byte)12, (byte)0 },
            U32(cardCode),
            LocInfo(
                location.Controller,
                location.Location,
                location.Sequence,
                location.Position),
            U64(description));

    private static byte[] ChainMessage(
        bool forced,
        params ChainEntrySpec[] entries)
    {
        List<byte[]> parts = new()
        {
            new[] { (byte)16, (byte)0, (byte)0, forced ? (byte)1 : (byte)0 },
            U32(0),
            U32(0),
            U32((uint)entries.Length)
        };
        parts.AddRange(entries.Select(entry => Join(
            U32(entry.CardCode),
            LocInfo(
                entry.Location.Controller,
                entry.Location.Location,
                entry.Location.Sequence,
                entry.Location.Position),
            U64(entry.Description),
            new[] { entry.ClientMode })));
        return Join(parts.ToArray());
    }

    private static byte[] BattleMessage(
        IReadOnlyList<BattleActivationSpec> activatable,
        IReadOnlyList<BattleAttackSpec> attackable,
        byte toMainPhase2,
        byte toEndPhase)
    {
        List<byte[]> parts = new()
        {
            new[] { (byte)10, (byte)0 },
            U32((uint)activatable.Count)
        };
        parts.AddRange(activatable.Select(entry => Join(
            U32(entry.CardCode),
            new[] { entry.Location.Controller, entry.Location.Location },
            U32(entry.Location.Sequence),
            U64(entry.Description),
            new[] { entry.ClientMode })));
        parts.Add(U32((uint)attackable.Count));
        parts.AddRange(attackable.Select(entry => Join(
            U32(entry.CardCode),
            new[] { entry.Location.Controller, entry.Location.Location },
            new[] { checked((byte)entry.Location.Sequence),
                entry.DirectAttackable ? (byte)1 : (byte)0 })));
        parts.Add(new[] { toMainPhase2, toEndPhase });
        return Join(parts.ToArray());
    }

    private static byte[] IdleMessage(
        IReadOnlyList<IdleCardSpec> summon,
        IReadOnlyList<IdleCardSpec> specialSummon,
        IReadOnlyList<IdleCardSpec> reposition,
        IReadOnlyList<IdleCardSpec> mset,
        IReadOnlyList<IdleCardSpec> sset,
        IReadOnlyList<IdleActivationSpec> activatable,
        byte toBattlePhase,
        byte toEndPhase,
        byte shuffleHand)
    {
        List<byte[]> parts = new()
        {
            new[] { (byte)11, (byte)0 }
        };
        AddIdleSection(parts, summon, true);
        AddIdleSection(parts, specialSummon, true);
        AddIdleSection(parts, reposition, false);
        AddIdleSection(parts, mset, true);
        AddIdleSection(parts, sset, true);
        parts.Add(U32((uint)activatable.Count));
        parts.AddRange(activatable.Select(entry => Join(
            U32(entry.CardCode),
            new[] { entry.Location.Controller, entry.Location.Location },
            U32(entry.Location.Sequence),
            U64(entry.Description),
            new[] { entry.ClientMode })));
        parts.Add(new[] { toBattlePhase, toEndPhase, shuffleHand });
        return Join(parts.ToArray());
    }

    private static void AddIdleSection(
        List<byte[]> parts,
        IReadOnlyList<IdleCardSpec> entries,
        bool wideSequence)
    {
        parts.Add(U32((uint)entries.Count));
        parts.AddRange(entries.Select(entry => Join(
            U32(entry.CardCode),
            new[] { entry.Location.Controller, entry.Location.Location },
            wideSequence
                ? U32(entry.Location.Sequence)
                : new[] { checked((byte)entry.Location.Sequence) })));
    }

    private readonly record struct Authority(
        PerspectiveStateMirrorV1 Mirror,
        GameplayMessageDecoderV1 Decoder,
        PublicStateProjectionResultV1 Projection);

    private readonly record struct CardSpec(
        uint CardCode,
        ModernLocInfoV1 Location);

    private readonly record struct ChainEntrySpec(
        uint CardCode,
        ModernLocInfoV1 Location,
        ulong Description,
        byte ClientMode);

    private readonly record struct BattleActivationSpec(
        uint CardCode,
        ModernLocInfoV1 Location,
        ulong Description,
        byte ClientMode);

    private readonly record struct BattleAttackSpec(
        uint CardCode,
        ModernLocInfoV1 Location,
        bool DirectAttackable);

    private readonly record struct IdleCardSpec(
        uint CardCode,
        ModernLocInfoV1 Location);

    private readonly record struct IdleActivationSpec(
        uint CardCode,
        ModernLocInfoV1 Location,
        ulong Description,
        byte ClientMode);
}
