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

            _ = session.TryCreateI6C5Frame();
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
}
