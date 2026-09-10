using System.Reflection;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;
using OCGForge.Ignis.Gameplay.Tests.Fixtures;
using static OCGForge.Ignis.Gameplay.Tests.GameplayMessageFixtures;
using static OCGForge.Ignis.Gameplay.Tests.MirrorFixtures;
using static OCGForge.Ignis.Gameplay.Tests.TestAssert;
using static OCGForge.Ignis.Gameplay.Tests.TransportFixtures;

namespace OCGForge.Ignis.Gameplay.Tests;

internal static class I4FrameOwnedSidecarLifecycleReconciliationTests
{
    internal static void TestFrameAuthorityIsSessionOwned()
    {
        Equal(
            "GameplayMirrorSessionV1 owns current Mirror and FrameInstanceOrdinal",
            I4FrameOwnedSidecarLifecycleReconciliationV1.ExistingFrameOwner);
        Equal(
            "SESSION_OWNED",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ExistingFrameOrdinalAuthority);
        Equal(
            "GameplayMirrorSessionV1 current authority supplies PublicStateProjectionV1.TryProject(..., ulong)",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ExistingProjectionOrdinalSource);

        True(HasNamedFrameCoordinate(typeof(GameplayMirrorSessionV1)));
        Type[] publicFrameConsumers =
        {
            typeof(FlatPromptSessionV1),
            typeof(PerspectiveSafeFrameV1)
        };
        foreach (Type type in publicFrameConsumers)
        {
            False(HasNamedFrameCoordinate(type));
        }

        MethodInfo[] frameOwnedProjectionMethods = typeof(
            PublicStateProjectionV1).GetMethods(
                BindingFlags.Static | BindingFlags.NonPublic)
            .Where(method => method.Name == "TryProject")
            .Where(method => method.GetParameters().Length == 3)
            .ToArray();
        Equal(1, frameOwnedProjectionMethods.Length);
        Equal(
            typeof(ulong),
            frameOwnedProjectionMethods[0].GetParameters()[2].ParameterType);

        PropertyInfo? sidecarFrameOrdinal = typeof(
            PrivateI4OccurrencePublicLocatorSidecarV1).GetProperty(
                "FrameInstanceOrdinal",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
        NotNull(sidecarFrameOrdinal);
    }

    internal static void TestSidecarFrameOrdinalRequiresIndependentAuthority()
    {
        const uint cardCode = 0x11223344;
        (PerspectiveStateMirrorV1 mirror,
            PublicStateProjectionResultV1 projection) =
            CreateOwnHandFrameAuthority(cardCode, frameInstanceOrdinal: 41);
        PrivateI4OccurrencePublicLocatorSidecarV1 originalSidecar =
            projection.PrivateOccurrenceSidecar!;
        PublicStateProjectionResultV1 futureProjection =
            RebindSidecar(projection, 99);
        FlatPromptProjectionResultV1 result = new FlatPromptSessionV1()
            .TryAcceptPrompt(
                SingleOwnHandIdleMessage(cardCode),
                mirror,
                futureProjection);

        False(result.IsSuccess);
        Equal(FlatPromptErrorCodeV1.AuthorityMismatch, result.Error);
        PrivateGameplayFrameAuthorityV1 currentFrame =
            new(41, mirror.Snapshot);
        Null(result.Context);
        Null(result.Candidates);

        foreach (ulong mismatchedOrdinal in new[] { 40ul, 42ul })
        {
            PublicStateProjectionResultV1 mismatchedProjection =
                RebindSidecar(projection, mismatchedOrdinal);
            FlatPromptProjectionResultV1 currentFrameResult =
                new FlatPromptSessionV1().TryAcceptFrameOwnedPrompt(
                    SingleOwnHandIdleMessage(cardCode),
                    currentFrame,
                    mismatchedProjection);
            False(currentFrameResult.IsSuccess);
            Equal(
                FlatPromptErrorCodeV1.AuthorityMismatch,
                currentFrameResult.Error);
            Null(currentFrameResult.Context);
            Null(currentFrameResult.Candidates);
        }

        currentFrame.Invalidate();
        FlatPromptProjectionResultV1 invalidatedFrameResult =
            new FlatPromptSessionV1().TryAcceptFrameOwnedPrompt(
                SingleOwnHandIdleMessage(cardCode),
                currentFrame,
                projection);
        False(invalidatedFrameResult.IsSuccess);
        Equal(
            FlatPromptErrorCodeV1.AuthorityMismatch,
            invalidatedFrameResult.Error);
    }

    internal static void TestInitialFrameBindsExistingInitializedMirror()
    {
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                deckCount0: 2,
                extraCount0: 1,
                deckCount1: 2,
                extraCount1: 1);
        _ = decoder;
        GameplayMessageDecodeResult duplicateStart =
            new GameplayMessageDecoderV1().Decode(
            new StocGameMessagePayload(
                CreateStartBytes(
                    0,
                    deckCount0: 2,
                    extraCount0: 1,
                    deckCount1: 2,
                    extraCount1: 1)));
        True(duplicateStart.IsSuccess, duplicateStart.Error.ToString());
        MirrorApplyResult applied = mirror.Apply(duplicateStart.Message!);
        False(applied.IsSuccess);
        Equal(GameplayErrorCode.DuplicatePerspective, applied.Error);

        ConstructorInfo[] constructors = typeof(GameplayMirrorSessionV1)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        True(constructors.Any(constructor =>
            constructor.GetParameters().Any(parameter =>
                parameter.ParameterType == typeof(PerspectiveStateMirrorV1))));
        Equal(
            "PerspectiveStateMirrorV1.TryCreate(MSG_START) -> GameplayMirrorSessionV1 binds existing mirror -> FRAME_0",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ImplementedCreationBoundary);
    }

    internal static void TestI5SidecarEnablementIsRestoredToBaseline()
    {
        const uint cardCode = 0x11223344;
        (PerspectiveStateMirrorV1 mirror,
            PublicStateProjectionResultV1 projection) =
            CreateOwnHandFrameAuthority(
                cardCode,
                cardCode,
                frameInstanceOrdinal: 41);
        FlatPromptProjectionResultV1 result = new FlatPromptSessionV1()
            .TryAcceptI5Prompt(
                DuplicateOwnHandSelectCardMessage(cardCode),
                mirror,
                projection);

        True(result.IsSuccess, result.Error.ToString());
        NotNull(result.Candidates);
        Equal(2, result.Candidates!.Count);
        True(result.Candidates.All(candidate =>
            candidate is FlatPromptCardSelectionPromptCodeCandidateV1));
        False(result.Candidates.Any(candidate =>
            candidate is FlatPromptCardSelectionLocatorPromptCodeCandidateV1));
        Equal(
            "OUT_OF_SCOPE",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .I5SidecarEnablement);
        Equal(
            "restore exact 65ccf707c802f42a942423e4e6fd0939fd6d342a behavior",
            I4FrameOwnedSidecarLifecycleReconciliationV1.I5Baseline);
    }

    internal static void TestGameplaySessionHasIndependentFrameAuthority()
    {
        Type sessionType = typeof(GameplayMirrorSessionV1);
        BindingFlags instanceFlags = BindingFlags.Instance |
            BindingFlags.NonPublic;
        True(sessionType.GetFields(instanceFlags).Any(field =>
            field.Name == "frameInstanceOrdinal" &&
            field.FieldType == typeof(ulong)));
        MethodInfo? authorityMethod = sessionType.GetMethod(
            "TryGetCurrentFrameAuthority",
            instanceFlags);
        NotNull(authorityMethod);
        Equal(typeof(bool), authorityMethod!.ReturnType);
        Equal(
            "SESSION_OWNED",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ExistingFrameOrdinalAuthority);
        False(typeof(PrivateGameplayFrameAuthorityV1).IsPublic);
        BindingFlags authorityFlags = BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;
        False(typeof(PrivateGameplayFrameAuthorityV1).GetProperties(
                authorityFlags)
            .Any(property => property.GetMethod?.IsPublic == true));
    }

    internal static void TestGameplaySessionFrameAuthorityLifecycle()
    {
        const uint cardCode = 0x11223344;
        byte[] timeLimit = WireFrameCodec.EncodeStoc(
            StocPacketType.TimeLimit,
            PacketPayloadCodec.EncodeStocTimeLimit(
                new StocTimeLimitPayload(0, 120)));
        byte[] hint = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            Join(new byte[] { 2, 1, 0 }, U64(0x0102030405060708)));
        byte[] move = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            MoveMessage(
                cardCode,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x02, 0, 0x08),
                0));
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer) =
            CreateGameplaySession(timeLimit, hint, move);
        bool disposed = false;
        try
        {
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? initialFrame));
            NotNull(initialFrame);
            Equal(0ul, initialFrame!.FrameInstanceOrdinal);

            PublicStateProjectionResultV1 initialProjection =
                PublicStateProjectionV1.TryProject(
                    initialFrame.MirrorSnapshot,
                    new PublicStateProjectionContextV1(0),
                    initialFrame.FrameInstanceOrdinal);
            True(initialProjection.IsSuccess, initialProjection.Error.ToString());
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? afterProjection));
            NotNull(afterProjection);
            Equal(0ul, afterProjection!.FrameInstanceOrdinal);

            GameplayMirrorPumpResult applied = session.PumpAsync(
                CancellationToken.None).GetAwaiter().GetResult();
            True(applied.IsSuccess, applied.Error.ToString());
            Equal(GameplayMessageKindV1.Move, applied.Message!.Kind);
            Equal(1, session.PresentationMessagesConsumed);
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? afterApply));
            NotNull(afterApply);
            Equal(1ul, afterApply!.FrameInstanceOrdinal);
            False(initialFrame!.IsCurrent);

            PublicStateProjectionResultV1 projection =
                PublicStateProjectionV1.TryProject(
                    afterApply.MirrorSnapshot,
                    new PublicStateProjectionContextV1(0),
                    afterApply.FrameInstanceOrdinal);
            True(projection.IsSuccess, projection.Error.ToString());
            FlatPromptProjectionResultV1 prompt = new FlatPromptSessionV1()
                .TryAcceptFrameOwnedPrompt(
                    SingleOwnHandIdleMessage(cardCode),
                    afterApply,
                    projection);
            True(prompt.IsSuccess, prompt.Error.ToString());

            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? afterPrompt));
            NotNull(afterPrompt);
            Equal(1ul, afterPrompt!.FrameInstanceOrdinal);
            Equal(
                afterApply.MirrorSnapshot.ToDeterministicString(),
                afterPrompt.MirrorSnapshot.ToDeterministicString());

            session.DisposeAsync().GetAwaiter().GetResult();
            disposed = true;
            False(afterApply.IsCurrent);
            False(session.TryGetCurrentFrameAuthority(out _));
        }
        finally
        {
            if (!disposed)
            {
                session.DisposeAsync().GetAwaiter().GetResult();
            }

            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    internal static void TestFailedApplyDoesNotAdvanceFrameOrdinal()
    {
        const uint cardCode = 0x11223344;
        byte[] move = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            MoveMessage(
                cardCode,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x02, 0, 0x08),
                0));
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer) =
            CreateGameplaySession(move, move);
        try
        {
            GameplayMirrorPumpResult first = session.PumpAsync(
                CancellationToken.None).GetAwaiter().GetResult();
            True(first.IsSuccess, first.Error.ToString());
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? afterFirst));
            NotNull(afterFirst);
            Equal(1ul, afterFirst!.FrameInstanceOrdinal);

            GameplayMirrorPumpResult failed = session.PumpAsync(
                CancellationToken.None).GetAwaiter().GetResult();
            False(failed.IsSuccess);
            Equal(
                GameplayErrorCode.ConflictingSlotOccupancy,
                failed.Error);
            False(afterFirst!.IsCurrent);
            FieldInfo frameOrdinalField = typeof(GameplayMirrorSessionV1)
                .GetField(
                    "frameInstanceOrdinal",
                    BindingFlags.Instance | BindingFlags.NonPublic)!;
            Equal(1ul, frameOrdinalField.GetValue(session));
            False(session.TryGetCurrentFrameAuthority(out _));
        }
        finally
        {
            session.DisposeAsync().GetAwaiter().GetResult();
            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    internal static void TestFailedProjectionInvalidatesFrameAuthority()
    {
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer) =
            CreateGameplaySession();
        try
        {
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? authority));
            NotNull(authority);
            Equal(0ul, authority!.FrameInstanceOrdinal);

            PerspectiveSafeFrameSourceResultV1 projection =
                session.TryCreateI6C5Frame();
            False(projection.IsSuccess);
            Equal(
                PerspectiveSafeFrameSourceErrorCodeV1.MissingMatchContext,
                projection.Error!.Value.Code);
            False(authority.IsCurrent);
            False(session.TryGetCurrentFrameAuthority(out _));
        }
        finally
        {
            session.DisposeAsync().GetAwaiter().GetResult();
            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    internal static void TestFailedFrameOwnedPromptInvalidatesFrameAuthority()
    {
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer) =
            CreateGameplaySession();
        try
        {
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? authority));
            NotNull(authority);
            PublicStateProjectionResultV1 projection =
                PublicStateProjectionV1.TryProject(
                    authority!.MirrorSnapshot,
                    new PublicStateProjectionContextV1(0),
                    authority.FrameInstanceOrdinal);
            True(projection.IsSuccess, projection.Error.ToString());

            FlatPromptProjectionResultV1 result =
                new FlatPromptSessionV1().TryAcceptFrameOwnedPrompt(
                    SingleOwnHandIdleMessage(0x11223344),
                    authority,
                    projection);
            False(result.IsSuccess);
            Equal(
                FlatPromptErrorCodeV1.UnprovenPublicReference,
                result.Error);
            False(authority.IsCurrent);
            False(session.TryGetCurrentFrameAuthority(out _));
        }
        finally
        {
            session.DisposeAsync().GetAwaiter().GetResult();
            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    internal static void TestStandaloneMirrorApplyBeforeClaimIsUnchanged()
    {
        const uint cardCode = 0x11223344;
        (PerspectiveStateMirrorV1 mirror,
            GameplayMessageDecoderV1 decoder) =
            CreateMirror(0, deckCount0: 2, extraCount0: 1);
        MirrorApplyResult applied = mirror.Apply(DecodeMessage(
            decoder,
            MoveMessage(
                cardCode,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x02, 0, 0x08),
                0)));
        True(applied.IsSuccess, applied.Error.ToString());
        True(mirror.Snapshot.Cards.Any(card =>
            card.Zone == MirrorZoneV1.Hand &&
            card.Sequence == 0));
    }

    internal static void TestMirrorClaimIsOneShotAndPermanent()
    {
        (PerspectiveStateMirrorV1 mirror,
            GameplayMessageDecoderV1 decoder) =
            CreateMirror(0, deckCount0: 2, extraCount0: 1);
        MethodInfo? claimMethod = typeof(PerspectiveStateMirrorV1).GetMethod(
            "TryClaimGameplaySessionOwnership",
            BindingFlags.Instance | BindingFlags.NonPublic);
        NotNull(claimMethod);
        object?[] firstArguments = { null };
        object? firstResult = claimMethod!.Invoke(mirror, firstArguments);
        True(firstResult is bool && (bool)firstResult);
        NotNull(firstArguments[0]);
        object?[] secondArguments = { null };
        object? secondResult = claimMethod.Invoke(mirror, secondArguments);
        True(secondResult is bool && !(bool)secondResult);
        Null(secondArguments[0]);
        _ = decoder;
    }

    internal static void TestClaimedMirrorRejectsExternalApply()
    {
        const uint cardCode = 0x11223344;
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer) =
            CreateGameplaySession();
        try
        {
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? initialAuthority));
            NotNull(initialAuthority);
            string authoritySnapshot =
                initialAuthority!.MirrorSnapshot.ToDeterministicString();

            GameplayMessageV1 directMessage = DecodeMessage(
                new GameplayMessageDecoderV1(
                    session.Mirror.Snapshot.Perspective),
                MoveMessage(
                    cardCode,
                    new ModernLocInfoV1(0, 0, 0, 0),
                    new ModernLocInfoV1(0, 0x02, 0, 0x08),
                    0));
            MirrorApplyResult directApply = session.Mirror.Apply(directMessage);
            False(directApply.IsSuccess);
            Equal(GameplayErrorCode.InvalidState, directApply.Error);
            Equal(
                authoritySnapshot,
                session.Mirror.Snapshot.ToDeterministicString());
            Equal(authoritySnapshot, initialAuthority.MirrorSnapshot.ToDeterministicString());

            session.DisposeAsync().GetAwaiter().GetResult();
            MirrorApplyResult postDisposeApply = session.Mirror.Apply(directMessage);
            False(postDisposeApply.IsSuccess);
            Equal(GameplayErrorCode.InvalidState, postDisposeApply.Error);
            Equal(
                authoritySnapshot,
                session.Mirror.Snapshot.ToDeterministicString());
        }
        finally
        {
            session.DisposeAsync().GetAwaiter().GetResult();
            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    internal static void TestFrameBoundPromptBindingExpiresOnFrameAdvance()
    {
        const uint cardCode = 0x11223344;
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        byte[] firstMove = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            MoveMessage(
                cardCode,
                empty,
                new ModernLocInfoV1(0, 0x02, 0, 0x08),
                0));
        byte[] secondMove = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            MoveMessage(
                cardCode,
                new ModernLocInfoV1(0, 0x02, 0, 0x08),
                new ModernLocInfoV1(0, 0x04, 0, 0x04),
                0));
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer) =
            CreateGameplaySession(firstMove, secondMove);
        try
        {
            GameplayMirrorPumpResult firstApply = session.PumpAsync(
                CancellationToken.None).GetAwaiter().GetResult();
            True(firstApply.IsSuccess, firstApply.Error.ToString());
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? frame));
            NotNull(frame);
            PublicStateProjectionResultV1 projection =
                PublicStateProjectionV1.TryProject(
                    frame!.MirrorSnapshot,
                    new PublicStateProjectionContextV1(0),
                    frame.FrameInstanceOrdinal);
            True(projection.IsSuccess, projection.Error.ToString());
            FlatPromptSessionV1 capturePrompt = new();
            FlatPromptSessionV1 resolvePrompt = new();
            FlatPromptSessionV1 applyPrompt = new();
            FlatPromptProjectionResultV1 captureAccepted =
                capturePrompt.TryAcceptFrameOwnedPrompt(
                    SingleOwnHandIdleMessage(cardCode),
                    frame,
                    projection);
            FlatPromptProjectionResultV1 resolveAccepted =
                resolvePrompt.TryAcceptFrameOwnedPrompt(
                    SingleOwnHandIdleMessage(cardCode),
                    frame,
                    projection);
            FlatPromptProjectionResultV1 applyAccepted =
                applyPrompt.TryAcceptFrameOwnedPrompt(
                    SingleOwnHandIdleMessage(cardCode),
                    frame,
                    projection);
            True(captureAccepted.IsSuccess, captureAccepted.Error.ToString());
            True(resolveAccepted.IsSuccess, resolveAccepted.Error.ToString());
            True(applyAccepted.IsSuccess, applyAccepted.Error.ToString());
            string key = captureAccepted.Candidates![0].I4LocalCandidateKey;
            True(capturePrompt.TryCaptureSelection(
                key,
                out _,
                out FlatPromptErrorCodeV1 captureError),
                captureError.ToString());
            string resolveKey = resolveAccepted.Candidates![0].I4LocalCandidateKey;
            True(resolvePrompt.TryCaptureSelection(
                resolveKey,
                out FlatPromptSelectionHandleV1? oldResolveHandle,
                out FlatPromptErrorCodeV1 resolveCaptureError),
                resolveCaptureError.ToString());
            NotNull(oldResolveHandle);
            string applyKey = applyAccepted.Candidates![0].I4LocalCandidateKey;
            True(applyPrompt.TryCaptureSelection(
                applyKey,
                out FlatPromptSelectionHandleV1? oldApplyHandle,
                out FlatPromptErrorCodeV1 applyCaptureError),
                applyCaptureError.ToString());
            NotNull(oldApplyHandle);

            GameplayMirrorPumpResult secondApply = session.PumpAsync(
                CancellationToken.None).GetAwaiter().GetResult();
            True(secondApply.IsSuccess, secondApply.Error.ToString());
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? nextFrame));
            NotNull(nextFrame);
            Equal(2ul, nextFrame!.FrameInstanceOrdinal);

            False(capturePrompt.TryCaptureSelection(
                key,
                out _,
                out FlatPromptErrorCodeV1 staleCaptureError));
            Equal(
                FlatPromptErrorCodeV1.StalePromptBinding,
                staleCaptureError);
            False(resolvePrompt.TryResolveSelection(
                oldResolveHandle,
                out FlatPromptResponseResolutionV1 oldResponse,
                out FlatPromptErrorCodeV1 staleResolveError));
            Equal(FlatPromptErrorCodeV1.StalePromptBinding, staleResolveError);
            Equal(0, oldResponse.ResponseI32);
            FlatPromptContinuationStepResultV1 oldApply =
                applyPrompt.TryApplySelection(oldApplyHandle);
            False(oldApply.IsSuccess);
            Equal(
                FlatPromptErrorCodeV1.StalePromptBinding,
                oldApply.Error);
            Null(oldApply.Projection);
            False(oldApply.IsTerminal);
        }
        finally
        {
            session.DisposeAsync().GetAwaiter().GetResult();
            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    internal static void TestPromptWinsAgainstConcurrentPump()
    {
        LifecycleRaceTransport transport = new(CreateMoveFrame());
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer) =
            CreateBlockedGameplaySession(transport);
        Task<GameplayMirrorPumpResult>? pumpTask = null;
        try
        {
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? frame));
            NotNull(frame);
            PublicStateProjectionResultV1 projection =
                CreateFrameProjection(frame!);
            FlatPromptSessionV1 promptSession = new();

            pumpTask = StartPump(session);
            transport.ReadStarted.GetAwaiter().GetResult();

            Task<FlatPromptProjectionResultV1> promptTask = Task.Run(
                () => promptSession.TryAcceptFrameOwnedPrompt(
                    IdleTransitionOnly(),
                    frame,
                    projection));
            FlatPromptProjectionResultV1 prompt =
                promptTask.GetAwaiter().GetResult();
            True(prompt.IsSuccess, prompt.Error.ToString());
            Equal(0ul, frame!.FrameInstanceOrdinal);
            True(frame.IsCurrent);

            transport.Release();
            GameplayMirrorPumpResult pump =
                pumpTask.GetAwaiter().GetResult();
            True(pump.IsSuccess, pump.Error.ToString());
            False(frame!.IsCurrent);
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? nextFrame));
            NotNull(nextFrame);
            Equal(1ul, nextFrame!.FrameInstanceOrdinal);
        }
        finally
        {
            transport.Release();
            if (pumpTask is not null)
            {
                _ = pumpTask.GetAwaiter().GetResult();
            }

            session.DisposeAsync().GetAwaiter().GetResult();
            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    internal static void TestPumpWinsAgainstPrompt()
    {
        LifecycleRaceTransport transport = new(CreateMoveFrame());
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer) =
            CreateBlockedGameplaySession(transport);
        Task<GameplayMirrorPumpResult>? pumpTask = null;
        try
        {
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? frame));
            NotNull(frame);
            PublicStateProjectionResultV1 projection =
                CreateFrameProjection(frame!);
            FlatPromptSessionV1 promptSession = new();
            pumpTask = StartPump(session);
            transport.ReadStarted.GetAwaiter().GetResult();

            Task<FlatPromptProjectionResultV1> promptTask = Task.Run(
                () =>
                {
                    _ = pumpTask.GetAwaiter().GetResult();
                    return promptSession.TryAcceptFrameOwnedPrompt(
                        IdleTransitionOnly(),
                        frame,
                        projection);
                });

            transport.Release();
            GameplayMirrorPumpResult pump =
                pumpTask.GetAwaiter().GetResult();
            True(pump.IsSuccess, pump.Error.ToString());
            FlatPromptProjectionResultV1 prompt =
                promptTask.GetAwaiter().GetResult();
            False(prompt.IsSuccess);
            Equal(FlatPromptErrorCodeV1.AuthorityMismatch, prompt.Error);
            Null(prompt.Context);
            Null(prompt.Candidates);
            False(frame!.IsCurrent);
        }
        finally
        {
            transport.Release();
            if (pumpTask is not null)
            {
                _ = pumpTask.GetAwaiter().GetResult();
            }

            session.DisposeAsync().GetAwaiter().GetResult();
            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    internal static void TestResolveWinsAgainstConcurrentPump()
    {
        LifecycleRaceTransport transport = new(CreateMoveFrame());
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer) =
            CreateBlockedGameplaySession(transport);
        Task<GameplayMirrorPumpResult>? pumpTask = null;
        try
        {
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? frame));
            NotNull(frame);
            PublicStateProjectionResultV1 projection =
                CreateFrameProjection(frame!);
            (FlatPromptSessionV1 promptSession,
                FlatPromptSelectionHandleV1 handle) =
                CreateFrameBoundSelection(frame!, projection);

            pumpTask = StartPump(session);
            transport.ReadStarted.GetAwaiter().GetResult();
            Task<(bool IsSuccess,
                FlatPromptResponseResolutionV1 Response,
                FlatPromptErrorCodeV1 Error)> resolveTask = Task.Run(
                () =>
                {
                    bool resolved = promptSession.TryResolveSelection(
                        handle,
                        out FlatPromptResponseResolutionV1 response,
                        out FlatPromptErrorCodeV1 error);
                    return (resolved, response, error);
                });
            (bool resolved,
                FlatPromptResponseResolutionV1 response,
                FlatPromptErrorCodeV1 error) =
                resolveTask.GetAwaiter().GetResult();
            True(resolved);
            Equal(FlatPromptErrorCodeV1.None, error);
            Equal(7, response.ResponseI32);

            transport.Release();
            GameplayMirrorPumpResult pump =
                pumpTask.GetAwaiter().GetResult();
            True(pump.IsSuccess, pump.Error.ToString());
            False(frame!.IsCurrent);
        }
        finally
        {
            transport.Release();
            if (pumpTask is not null)
            {
                _ = pumpTask.GetAwaiter().GetResult();
            }

            session.DisposeAsync().GetAwaiter().GetResult();
            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    internal static void TestPumpWinsAgainstResolve()
    {
        LifecycleRaceTransport transport = new(CreateMoveFrame());
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer) =
            CreateBlockedGameplaySession(transport);
        Task<GameplayMirrorPumpResult>? pumpTask = null;
        try
        {
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? frame));
            NotNull(frame);
            PublicStateProjectionResultV1 projection =
                CreateFrameProjection(frame!);
            (FlatPromptSessionV1 promptSession,
                FlatPromptSelectionHandleV1 handle) =
                CreateFrameBoundSelection(frame!, projection);

            pumpTask = StartPump(session);
            transport.ReadStarted.GetAwaiter().GetResult();
            Task<(bool IsSuccess,
                FlatPromptResponseResolutionV1 Response,
                FlatPromptErrorCodeV1 Error)> resolveTask = Task.Run(
                () =>
                {
                    _ = pumpTask.GetAwaiter().GetResult();
                    bool resolved = promptSession.TryResolveSelection(
                        handle,
                        out FlatPromptResponseResolutionV1 response,
                        out FlatPromptErrorCodeV1 error);
                    return (resolved, response, error);
                });

            transport.Release();
            GameplayMirrorPumpResult pump =
                pumpTask.GetAwaiter().GetResult();
            True(pump.IsSuccess, pump.Error.ToString());
            (bool resolved,
                FlatPromptResponseResolutionV1 response,
                FlatPromptErrorCodeV1 error) =
                resolveTask.GetAwaiter().GetResult();
            False(resolved);
            Equal(FlatPromptErrorCodeV1.StalePromptBinding, error);
            Equal(0, response.ResponseI32);
            False(frame!.IsCurrent);
        }
        finally
        {
            transport.Release();
            if (pumpTask is not null)
            {
                _ = pumpTask.GetAwaiter().GetResult();
            }

            session.DisposeAsync().GetAwaiter().GetResult();
            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    internal static void TestSessionDisposalStalesAllFrameBoundConsumers()
    {
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer) =
            CreateGameplaySession();
        try
        {
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? frame));
            NotNull(frame);
            PublicStateProjectionResultV1 projection =
                CreateFrameProjection(frame!);
            (FlatPromptSessionV1 capturePrompt,
                FlatPromptSelectionHandleV1 captureHandle) =
                CreateFrameBoundSelection(frame!, projection);
            (FlatPromptSessionV1 resolvePrompt,
                FlatPromptSelectionHandleV1 resolveHandle) =
                CreateFrameBoundSelection(frame!, projection);
            (FlatPromptSessionV1 applyPrompt,
                FlatPromptSelectionHandleV1 applyHandle) =
                CreateFrameBoundSelection(frame!, projection);

            session.DisposeAsync().GetAwaiter().GetResult();

            False(capturePrompt.TryCaptureSelection(
                captureHandle.I4LocalCandidateKey,
                out FlatPromptSelectionHandleV1? staleCapture,
                out FlatPromptErrorCodeV1 captureError));
            Null(staleCapture);
            Equal(FlatPromptErrorCodeV1.StalePromptBinding, captureError);

            False(resolvePrompt.TryResolveSelection(
                resolveHandle,
                out FlatPromptResponseResolutionV1 staleResponse,
                out FlatPromptErrorCodeV1 resolveError));
            Equal(FlatPromptErrorCodeV1.StalePromptBinding, resolveError);
            Equal(0, staleResponse.ResponseI32);

            FlatPromptContinuationStepResultV1 staleApply =
                applyPrompt.TryApplySelection(applyHandle);
            False(staleApply.IsSuccess);
            Equal(FlatPromptErrorCodeV1.StalePromptBinding, staleApply.Error);
            Null(staleApply.Projection);
            False(staleApply.IsTerminal);
        }
        finally
        {
            session.DisposeAsync().GetAwaiter().GetResult();
            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    internal static void TestFailedFrameBoundaryStalesAllFrameBoundConsumers()
    {
        (GameplayMirrorSessionV1 session,
            GameplayHandoffConsumerV1 consumer) =
            CreateGameplaySession();
        try
        {
            True(session.TryGetCurrentFrameAuthority(
                out PrivateGameplayFrameAuthorityV1? frame));
            NotNull(frame);
            PublicStateProjectionResultV1 projection =
                CreateFrameProjection(frame!);
            (FlatPromptSessionV1 capturePrompt,
                FlatPromptSelectionHandleV1 captureHandle) =
                CreateFrameBoundSelection(frame!, projection);
            (FlatPromptSessionV1 resolvePrompt,
                FlatPromptSelectionHandleV1 resolveHandle) =
                CreateFrameBoundSelection(frame!, projection);
            (FlatPromptSessionV1 applyPrompt,
                FlatPromptSelectionHandleV1 applyHandle) =
                CreateFrameBoundSelection(frame!, projection);

            PerspectiveSafeFrameSourceResultV1 failedProjection =
                session.TryCreateI6C5Frame();
            False(failedProjection.IsSuccess);
            Equal(
                PerspectiveSafeFrameSourceErrorCodeV1.MissingMatchContext,
                failedProjection.Error!.Value.Code);

            False(capturePrompt.TryCaptureSelection(
                captureHandle.I4LocalCandidateKey,
                out FlatPromptSelectionHandleV1? staleCapture,
                out FlatPromptErrorCodeV1 captureError));
            Null(staleCapture);
            Equal(FlatPromptErrorCodeV1.StalePromptBinding, captureError);

            False(resolvePrompt.TryResolveSelection(
                resolveHandle,
                out FlatPromptResponseResolutionV1 staleResponse,
                out FlatPromptErrorCodeV1 resolveError));
            Equal(FlatPromptErrorCodeV1.StalePromptBinding, resolveError);
            Equal(0, staleResponse.ResponseI32);

            FlatPromptContinuationStepResultV1 staleApply =
                applyPrompt.TryApplySelection(applyHandle);
            False(staleApply.IsSuccess);
            Equal(FlatPromptErrorCodeV1.StalePromptBinding, staleApply.Error);
            Null(staleApply.Projection);
            False(staleApply.IsTerminal);
        }
        finally
        {
            session.DisposeAsync().GetAwaiter().GetResult();
            consumer.DisposeAsync().GetAwaiter().GetResult();
        }
    }

    internal static void TestFrameAuthorityHasSafeLifecycle()
    {
        Equal(
            "GameplayMirrorSessionV1",
            I4FrameOwnedSidecarLifecycleReconciliationV1.ImplementedOwner);
        Equal(
            "PrivateGameplayFrameAuthorityV1",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ImplementedAuthorityType);
        Equal(
            "ulong FrameInstanceOrdinal",
            I4FrameOwnedSidecarLifecycleReconciliationV1.ImplementedCoordinate);
        Equal(
            "PerspectiveStateMirrorV1.TryCreate(MSG_START) -> GameplayMirrorSessionV1 binds existing mirror -> FRAME_0",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ImplementedCreationBoundary);
        Equal(
            "presentation packet, failed apply, projection read, prompt acceptance",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ImplementedNonCreationEvents);
        Equal(
            "next committed mirror frame, failed boundary, session disposal",
            I4FrameOwnedSidecarLifecycleReconciliationV1
                .ImplementedInvalidation);
        True(I4FrameOwnedSidecarLifecycleReconciliationV1
            .PrivateAuthorityIsNotPublicIdentity);
        True(I4FrameOwnedSidecarLifecycleReconciliationV1
            .PrivateAuthorityIsNotSerialized);
        True(I4FrameOwnedSidecarLifecycleReconciliationV1
            .PrivateAuthorityUsesNoWallClock);
        True(I4FrameOwnedSidecarLifecycleReconciliationV1
            .PrivateAuthorityUsesNoPid);
    }

    private static bool HasNamedFrameCoordinate(Type type)
    {
        BindingFlags flags = BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;
        return type.GetFields(flags).Any(field =>
                   field.Name.Contains(
                       "FrameInstanceOrdinal",
                       StringComparison.OrdinalIgnoreCase)) ||
            type.GetProperties(flags).Any(property =>
                property.Name.Contains(
                    "FrameInstanceOrdinal",
                    StringComparison.OrdinalIgnoreCase));
    }

    private static Task<GameplayMirrorPumpResult> StartPump(
        GameplayMirrorSessionV1 session) =>
        Task.Run(async () =>
            await session.PumpAsync(CancellationToken.None));

    private static PublicStateProjectionResultV1 CreateFrameProjection(
        PrivateGameplayFrameAuthorityV1 frame)
    {
        PublicStateProjectionResultV1 projection =
            PublicStateProjectionV1.TryProject(
                frame.MirrorSnapshot,
                new PublicStateProjectionContextV1(0),
                frame.FrameInstanceOrdinal);
        True(projection.IsSuccess, projection.Error.ToString());
        NotNull(projection.PrivateOccurrenceSidecar);
        return projection;
    }

    private static (
        FlatPromptSessionV1 Prompt,
        FlatPromptSelectionHandleV1 Handle)
        CreateFrameBoundSelection(
            PrivateGameplayFrameAuthorityV1 frame,
            PublicStateProjectionResultV1 projection)
    {
        FlatPromptSessionV1 prompt = new();
        FlatPromptProjectionResultV1 accepted =
            prompt.TryAcceptFrameOwnedPrompt(
                IdleTransitionOnly(),
                frame,
                projection);
        True(accepted.IsSuccess, accepted.Error.ToString());
        FlatPublicCandidateDescriptorV1 toEp = accepted.Candidates!
            .Single(candidate =>
                candidate.ChoiceKind == FlatPromptChoiceKindV1.ToEp);
        True(prompt.TryCaptureSelection(
            toEp.I4LocalCandidateKey,
            out FlatPromptSelectionHandleV1? handle,
            out FlatPromptErrorCodeV1 captureError),
            captureError.ToString());
        NotNull(handle);
        return (prompt, handle!);
    }

    private static (
        PerspectiveStateMirrorV1 Mirror,
        PublicStateProjectionResultV1 Projection)
        CreateOwnHandFrameAuthority(
            uint firstCardCode,
            uint? secondCardCode = null,
            ulong frameInstanceOrdinal = 0)
    {
        uint[] cardCodes = secondCardCode.HasValue
            ? new[] { firstCardCode, secondCardCode.Value }
            : new[] { firstCardCode };
        (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
            CreateMirror(
                0,
                deckCount0: checked((ushort)cardCodes.Length),
                extraCount0: 0,
                deckCount1: 0,
                extraCount1: 0);
        ModernLocInfoV1 empty = new(0, 0, 0, 0);
        for (uint sequence = 0; sequence < cardCodes.Length; sequence++)
        {
            MirrorApplyResult moved = mirror.Apply(DecodeMessage(
                decoder,
                MoveMessage(
                    cardCodes[checked((int)sequence)],
                    empty,
                    new ModernLocInfoV1(0, 0x02, sequence, 0x08),
                    0)));
            True(moved.IsSuccess, moved.Error.ToString());
        }

        PublicStateProjectionResultV1 projection =
            PublicStateProjectionV1.TryProject(
                mirror.Snapshot,
                new PublicStateProjectionContextV1(0),
                frameInstanceOrdinal);
        True(projection.IsSuccess, projection.Error.ToString());
        NotNull(projection.PrivateOccurrenceSidecar);
        return (mirror, projection);
    }

    private static byte[] SingleOwnHandIdleMessage(uint cardCode) =>
        Join(
            new byte[] { 11, 0 },
            U32(1),
            U32(cardCode),
            new byte[] { 0, 0x02 },
            U32(0),
            U32(0),
            U32(0),
            U32(0),
            U32(0),
            U32(0),
            new byte[] { 0, 0, 0 });

    private static byte[] IdleTransitionOnly()
    {
        byte[] bytes = new byte[29];
        bytes[0] = 11;
        bytes[1] = 0;
        bytes[26] = 1;
        bytes[27] = 1;
        bytes[28] = 1;
        return bytes;
    }

    private static byte[] CreateMoveFrame()
    {
        const uint cardCode = 0x11223344;
        return WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            MoveMessage(
                cardCode,
                new ModernLocInfoV1(0, 0, 0, 0),
                new ModernLocInfoV1(0, 0x02, 0, 0x08),
                0));
    }

    private static byte[] DuplicateOwnHandSelectCardMessage(uint cardCode) =>
        Join(
            new byte[] { 15, 0, 0 },
            U32(1),
            U32(2),
            U32(2),
            U32(cardCode),
            LocInfo(0, 0x02, 0, 0x08),
            U32(cardCode),
            LocInfo(0, 0x02, 1, 0x08));

    private static PublicStateProjectionResultV1 RebindSidecar(
        PublicStateProjectionResultV1 projection,
        ulong frameInstanceOrdinal)
    {
        PrivateI4OccurrencePublicLocatorSidecarV1 originalSidecar =
            projection.PrivateOccurrenceSidecar!;
        True(PrivateI4OccurrencePublicLocatorSidecarV1.TryCreate(
                frameInstanceOrdinal,
                projection.PublicProjectionId,
                originalSidecar.Entries,
                out PrivateI4OccurrencePublicLocatorSidecarV1? sidecar));
        NotNull(sidecar);
        return PublicStateProjectionResultV1.Success(
            projection.Snapshot!,
            projection.CanonicalBytes.ToArray(),
            projection.Sha256!,
            sidecar!);
    }

    private static (
        GameplayMirrorSessionV1 Session,
        GameplayHandoffConsumerV1 Consumer)
        CreateGameplaySession(params byte[][] gameplayFrames)
    {
        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(0, deckCount0: 2, extraCount0: 1,
                deckCount1: 0, extraCount1: 0));
        byte[] transcript = Join(
            new[] { startFrame }.Concat(gameplayFrames).ToArray());
        TestTransport transport = new(new[] { transcript });
        GameplayHandoffAcquireResult acquired =
            GameplayHandoffConsumerV1.TryCreate(
                CreateHandoff(transport, Array.Empty<byte>()));
        True(acquired.IsSuccess, acquired.Error.ToString());
        GameplayHandoffConsumerV1 consumer = acquired.Consumer!;
        GameplayPumpResult first = consumer.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(first.IsSuccess, first.Error.ToString());
        MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
            first.Message!,
            first.Perspective!);
        True(created.IsSuccess, created.Error.ToString());
        return (
            new GameplayMirrorSessionV1(first.Session!, created.Mirror!),
            consumer);
    }

    private static (
        GameplayMirrorSessionV1 Session,
        GameplayHandoffConsumerV1 Consumer)
        CreateBlockedGameplaySession(LifecycleRaceTransport transport)
    {
        byte[] startFrame = WireFrameCodec.EncodeStoc(
            StocPacketType.GameMsg,
            CreateStartBytes(
                0,
                deckCount0: 2,
                extraCount0: 1,
                deckCount1: 0,
                extraCount1: 0));
        GameplayHandoffAcquireResult acquired =
            GameplayHandoffConsumerV1.TryCreate(
                CreateHandoff(transport, startFrame));
        True(acquired.IsSuccess, acquired.Error.ToString());
        GameplayHandoffConsumerV1 consumer = acquired.Consumer!;
        GameplayPumpResult first = consumer.PumpAsync(
            CancellationToken.None).GetAwaiter().GetResult();
        True(first.IsSuccess, first.Error.ToString());
        MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
            first.Message!,
            first.Perspective!);
        True(created.IsSuccess, created.Error.ToString());
        return (
            new GameplayMirrorSessionV1(first.Session!, created.Mirror!),
            consumer);
    }
}
