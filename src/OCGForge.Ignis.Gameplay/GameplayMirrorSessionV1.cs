using OCGForge.Ignis.Protocol;

namespace OCGForge.Ignis.Gameplay;

public sealed class GameplayMirrorPumpResult
{
    private GameplayMirrorPumpResult(
        bool isSuccess,
        GameplayErrorCode error,
        GameplayMessageV1? message,
        MirrorSnapshotV1 snapshot)
    {
        IsSuccess = isSuccess;
        Error = error;
        Message = message;
        Snapshot = snapshot;
    }

    public bool IsSuccess { get; }

    public GameplayErrorCode Error { get; }

    public GameplayMessageV1? Message { get; }

    public MirrorSnapshotV1 Snapshot { get; }

    internal static GameplayMirrorPumpResult Success(
        GameplayMessageV1 message,
        MirrorSnapshotV1 snapshot) =>
        new(true, GameplayErrorCode.None, message, snapshot);

    internal static GameplayMirrorPumpResult Failure(
        GameplayErrorCode error,
        MirrorSnapshotV1 snapshot) =>
        new(false, error, null, snapshot);
}

public sealed class GameplayMirrorSessionV1 : IAsyncDisposable
{
    private const byte MsgHint = 2;
    private const byte MsgWaiting = 3;
    private const int MsgHintLength = 11;
    private const int MsgWaitingLength = 1;
    private const int MsgHintPlayerOffset = 2;

    private readonly GameplaySessionV1 transportSession;
    private readonly PerspectiveStateMirrorV1 mirror;
    private readonly GameplayMessageDecoderV1 decoder;
    private readonly PerspectiveSafeMatchContextV1? boundMatchContext;
    private readonly PerspectiveSafePrintedProviderV1? boundPrintedProvider;
    private readonly SemaphoreSlim operationGate = new(1, 1);
    private readonly byte[] receiveBuffer = new byte[
        ProtocolContractV1.MaxPacketLength +
        ProtocolContractV1.LengthPrefixSize];
    private readonly PrivateGameplayMirrorOwnershipV1 mirrorOwnership;
    private int receiveCount;
    private int presentationMessagesConsumed;
    private int terminal;
    private ulong frameInstanceOrdinal;
    private PrivateGameplayFrameAuthorityV1 currentFrameAuthority;

    public GameplayMirrorSessionV1(
        GameplaySessionV1 transportSession,
        PerspectiveStateMirrorV1 mirror)
        : this(transportSession, mirror, null, null)
    {
    }

    public GameplayMirrorSessionV1(
        GameplaySessionV1 transportSession,
        PerspectiveStateMirrorV1 mirror,
        PerspectiveSafeMatchContextV1? matchContext)
        : this(transportSession, mirror, matchContext, null)
    {
    }

    public GameplayMirrorSessionV1(
        GameplaySessionV1 transportSession,
        PerspectiveStateMirrorV1 mirror,
        PerspectiveSafeMatchContextV1? matchContext,
        PerspectiveSafePrintedProviderV1? printedProvider)
    {
        this.transportSession = transportSession ??
            throw new ArgumentNullException(nameof(transportSession));
        this.mirror = mirror ?? throw new ArgumentNullException(nameof(mirror));
        if (!transportSession.Perspective.Equals(mirror.Snapshot.Perspective))
        {
            throw new ArgumentException(
                "The gameplay transport and mirror perspectives must match.",
                nameof(mirror));
        }

        if (matchContext is not null &&
            !PerspectiveSafeMatchContextValidationV1.TryValidate(
                matchContext,
                mirror.Snapshot.Perspective.PlayerType,
                out _))
        {
            throw new ArgumentException(
                "The I6C5 match context is invalid.",
                nameof(matchContext));
        }

        if (!mirror.TryClaimGameplaySessionOwnership(
                out PrivateGameplayMirrorOwnershipV1? ownership) ||
            ownership is null)
        {
            throw new InvalidOperationException(
                "The gameplay mirror is already owned by another session.");
        }

        mirrorOwnership = ownership;
        boundMatchContext = matchContext;
        boundPrintedProvider = printedProvider;
        frameInstanceOrdinal = 0;
        currentFrameAuthority = new PrivateGameplayFrameAuthorityV1(
            frameInstanceOrdinal,
            mirror.Snapshot);
        decoder = new GameplayMessageDecoderV1(transportSession.Perspective);
    }

    public PerspectiveStateMirrorV1 Mirror => mirror;

    public int PresentationMessagesConsumed =>
        Volatile.Read(ref presentationMessagesConsumed);

    internal bool TryGetCurrentFrameAuthority(
        out PrivateGameplayFrameAuthorityV1? authority)
    {
        operationGate.Wait();
        try
        {
            authority = null;
            if (Volatile.Read(ref terminal) != 0 ||
                !currentFrameAuthority.IsCurrent)
            {
                return false;
            }

            authority = currentFrameAuthority;
            return true;
        }
        finally
        {
            operationGate.Release();
        }
    }

    public PerspectiveSafeFrameSourceResultV1 TryCreateI6C5Frame()
    {
        operationGate.Wait();
        try
        {
            if (Volatile.Read(ref terminal) != 0 ||
                !currentFrameAuthority.IsCurrent)
            {
                return PerspectiveSafeFrameSourceResultV1.Failure(
                    new PerspectiveSafeFrameSourceErrorV1(
                        PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                        PerspectiveSafeSourceSectionV1.Input));
            }

            using PrivateGameplayFrameAuthorityLeaseV1? lease =
                currentFrameAuthority.TryAcquire();
            if (lease is null)
            {
                return PerspectiveSafeFrameSourceResultV1.Failure(
                    new PerspectiveSafeFrameSourceErrorV1(
                        PerspectiveSafeFrameSourceErrorCodeV1.InvalidMirrorSnapshot,
                        PerspectiveSafeSourceSectionV1.Input));
            }

            PerspectiveSafeFrameSourceResultV1 result =
                PerspectiveSafePublicFrameSourceV1.TryCreateI6C5(
                mirror,
                boundMatchContext,
                boundPrintedProvider);
            if (!result.IsSuccess &&
                !IsProvisionalFrameSourceFailure(result))
            {
                currentFrameAuthority.Invalidate();
            }

            return result;
        }
        finally
        {
            operationGate.Release();
        }
    }

    /// <summary>
    /// Creates the Gameplay-owned same-snapshot I6D composition. The returned
    /// frame is public-safe and the optional handoff is opaque; all occurrence
    /// and lifecycle provenance remains inside Gameplay.
    /// </summary>
    public I6DFrameOwnedCompositionResultV1
        TryCreateI6DFrameOwnedComposition(
            FlatPromptSessionV1? promptSession,
            FlatPromptProjectionResultV1? acceptedPromptProjection,
            PublicStateProjectionResultV1? acceptedI4Projection)
    {
        if (promptSession is null ||
            acceptedPromptProjection is null ||
            acceptedI4Projection is null)
        {
            return I6DFrameOwnedCompositionResultV1.Failure(
                I6DFrameOwnedCompositionErrorCodeV1.InvalidInput,
                "composition");
        }

        operationGate.Wait();
        try
        {
            if (Volatile.Read(ref terminal) != 0 ||
                !currentFrameAuthority.IsCurrent)
            {
                return I6DFrameOwnedCompositionResultV1.Failure(
                    I6DFrameOwnedCompositionErrorCodeV1.InvalidState,
                    "frame");
            }

            PrivateGameplayFrameAuthorityV1 frameAuthority =
                currentFrameAuthority;
            using PrivateGameplayFrameAuthorityLeaseV1? frameLease =
                frameAuthority.TryAcquire();
            if (frameLease is null)
            {
                return I6DFrameOwnedCompositionResultV1.Failure(
                    I6DFrameOwnedCompositionErrorCodeV1.InvalidState,
                    "frame");
            }

            if (!IsCurrentI4Projection(
                    frameAuthority,
                    acceptedI4Projection))
            {
                frameAuthority.Invalidate();
                return I6DFrameOwnedCompositionResultV1.Failure(
                    I6DFrameOwnedCompositionErrorCodeV1.PromptBindingMismatch,
                    "i4_projection");
            }

            if (!promptSession.TryGetCurrentFrameOwnedBinding(
                    frameAuthority,
                    acceptedPromptProjection,
                    out CurrentFlatPromptBindingV1? binding) ||
                binding is null)
            {
                frameAuthority.Invalidate();
                return I6DFrameOwnedCompositionResultV1.Failure(
                    I6DFrameOwnedCompositionErrorCodeV1.PromptBindingMismatch,
                    "prompt");
            }

            PrivateI6DFrameCompositionResultV1 composition =
                PerspectiveSafePublicFrameSourceV1.TryCreateI6DFrame(
                    mirror,
                    boundMatchContext,
                    boundPrintedProvider);
            if (!composition.FrameResult.IsSuccess ||
                composition.FrameResult.Frame is null ||
                composition.LocatorMap is null)
            {
                frameAuthority.Invalidate();
                return I6DFrameOwnedCompositionResultV1.Failure(
                    I6DFrameOwnedCompositionErrorCodeV1.FrameSourceFailure,
                    composition.FrameResult.Error?.Section.ToString() ??
                    "frame_source");
            }

            if (!I6DPrivateCrossLocatorBindingHandoffV1.TryCreate(
                    frameAuthority,
                    binding,
                    acceptedPromptProjection,
                    acceptedI4Projection,
                    composition.FrameResult.Frame,
                    frameAuthority.MirrorSnapshot,
                    composition.LocatorMap,
                    out I6DPrivateCrossLocatorBindingHandoffV1? handoff,
                    out I6DPrivateCrossLocatorHandoffErrorV1? handoffError))
            {
                frameAuthority.Invalidate();
                return I6DFrameOwnedCompositionResultV1.Failure(
                    I6DFrameOwnedCompositionErrorCodeV1.CrossLocatorBindingFailure,
                    handoffError?.FieldPath ?? "handoff");
            }

            return I6DFrameOwnedCompositionResultV1.Success(
                composition.FrameResult.Frame,
                handoff);
        }
        finally
        {
            operationGate.Release();
        }
    }

    private static bool IsCurrentI4Projection(
        PrivateGameplayFrameAuthorityV1 frameAuthority,
        PublicStateProjectionResultV1 acceptedProjection)
    {
        if (!acceptedProjection.IsSuccess ||
            acceptedProjection.Snapshot is null ||
            acceptedProjection.PrivateOccurrenceSidecar is null)
        {
            return false;
        }

        PublicStateProjectionResultV1 recomputed =
            PublicStateProjectionV1.TryProject(
                frameAuthority.MirrorSnapshot,
                new PublicStateProjectionContextV1(
                    acceptedProjection.Snapshot.DuelFlags),
                frameAuthority.FrameInstanceOrdinal);
        return recomputed.IsSuccess &&
            recomputed.Snapshot is not null &&
            recomputed.PrivateOccurrenceSidecar is not null &&
            recomputed.CanonicalBytes.Span.SequenceEqual(
                acceptedProjection.CanonicalBytes.Span) &&
            string.Equals(
                recomputed.Sha256,
                acceptedProjection.Sha256,
                StringComparison.Ordinal) &&
            string.Equals(
                recomputed.PublicProjectionId,
                acceptedProjection.PublicProjectionId,
                StringComparison.Ordinal) &&
            acceptedProjection.PrivateOccurrenceSidecar.IsEquivalentTo(
                recomputed.PrivateOccurrenceSidecar);
    }

    public async ValueTask<GameplayMirrorPumpResult> PumpAsync(
        CancellationToken cancellationToken)
    {
        await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (Volatile.Read(ref terminal) != 0)
            {
                return GameplayMirrorPumpResult.Failure(
                    GameplayErrorCode.InvalidState,
                    mirror.Snapshot);
            }

            if (!currentFrameAuthority.IsCurrent)
            {
                return await FailAsync(GameplayErrorCode.InvalidState)
                    .ConfigureAwait(false);
            }

            while (true)
            {
                if (receiveCount > 0)
                {
                    FrameReadResult<ValidatedStocPacket> parsed =
                        PacketPayloadValidator.TryReadValidatedStoc(
                            receiveBuffer.AsSpan(0, receiveCount));
                    if (parsed.Status == FrameReadStatus.Invalid)
                    {
                        return await FailAsync(MapProtocolError(parsed.Error))
                            .ConfigureAwait(false);
                    }

                    if (parsed.Status == FrameReadStatus.Success)
                    {
                        if (parsed.Frame is null || parsed.ConsumedBytes <= 0)
                        {
                            return await FailAsync(
                                    GameplayErrorCode.MalformedOuterFrame)
                                .ConfigureAwait(false);
                        }

                        ValidatedStocPacket packet = parsed.Frame;
                        Consume(parsed.ConsumedBytes);
                        if (packet.Type == StocPacketType.TimeLimit)
                        {
                            if (packet.Payload is not StocTimeLimitPayload timeLimit)
                            {
                                return await FailAsync(
                                        GameplayErrorCode.MalformedOuterFrame)
                                    .ConfigureAwait(false);
                            }

                            if (timeLimit.Player > 1)
                            {
                                return await FailAsync(
                                        GameplayErrorCode.InvalidParticipant)
                                    .ConfigureAwait(false);
                            }

                            continue;
                        }

                        if (packet.Type != StocPacketType.GameMsg ||
                            packet.Payload is not StocGameMessagePayload gameMessage)
                        {
                            return await FailAsync(
                                    GameplayErrorCode.UnsupportedOuterPacket)
                                .ConfigureAwait(false);
                        }

                        ReadOnlySpan<byte> messageBytes = gameMessage.Bytes.Span;
                        if (!messageBytes.IsEmpty && messageBytes[0] == MsgHint)
                        {
                            if (!IsValidMsgHint(messageBytes))
                            {
                                return await FailAsync(
                                        GameplayErrorCode.MalformedGameMessage)
                                    .ConfigureAwait(false);
                            }

                            if (presentationMessagesConsumed == int.MaxValue)
                            {
                                return await FailAsync(
                                        GameplayErrorCode.MalformedGameMessage)
                                    .ConfigureAwait(false);
                            }

                            presentationMessagesConsumed++;
                            continue;
                        }

                        if (!messageBytes.IsEmpty &&
                            messageBytes[0] == MsgWaiting)
                        {
                            if (messageBytes.Length != MsgWaitingLength)
                            {
                                return await FailAsync(
                                        GameplayErrorCode.MalformedGameMessage)
                                    .ConfigureAwait(false);
                            }

                            if (presentationMessagesConsumed == int.MaxValue)
                            {
                                return await FailAsync(
                                        GameplayErrorCode.MalformedGameMessage)
                                    .ConfigureAwait(false);
                            }

                            presentationMessagesConsumed++;
                            continue;
                        }

                        GameplayMessageDecodeResult decoded = decoder.Decode(gameMessage);
                        if (!decoded.IsSuccess || decoded.Message is null)
                        {
                            return await FailAsync(decoded.Error)
                                .ConfigureAwait(false);
                        }

                        if (frameInstanceOrdinal == ulong.MaxValue)
                        {
                            return await FailAsync(
                                    GameplayErrorCode.ArithmeticFailure)
                                .ConfigureAwait(false);
                        }

                        PrivateGameplayFrameAuthorityLeaseV1? lease =
                            currentFrameAuthority.TryAcquire();
                        if (lease is null)
                        {
                            return await FailAsync(
                                    GameplayErrorCode.InvalidState)
                                .ConfigureAwait(false);
                        }

                        MirrorApplyResult applied;
                        using (lease)
                        {
                            applied = mirror.ApplyOwned(
                                mirrorOwnership,
                                decoded.Message);
                            if (!applied.IsSuccess)
                            {
                                currentFrameAuthority.Invalidate();
                            }

                            if (applied.IsSuccess)
                            {
                                currentFrameAuthority.Invalidate();
                                frameInstanceOrdinal++;
                                currentFrameAuthority =
                                    new PrivateGameplayFrameAuthorityV1(
                                        frameInstanceOrdinal,
                                        applied.Snapshot);
                            }
                        }

                        if (!applied.IsSuccess)
                        {
                            return await FailAsync(applied.Error)
                                .ConfigureAwait(false);
                        }

                        return GameplayMirrorPumpResult.Success(
                            decoded.Message,
                            applied.Snapshot);
                    }
                }

                int available = receiveBuffer.Length - receiveCount;
                if (available == 0)
                {
                    return await FailAsync(
                            GameplayErrorCode.MalformedOuterFrame)
                        .ConfigureAwait(false);
                }

                int readCount;
                try
                {
                    readCount = await transportSession.ReadAsync(
                            receiveBuffer.AsMemory(receiveCount, available),
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    return await FailAsync(GameplayErrorCode.Cancelled)
                        .ConfigureAwait(false);
                }
                catch
                {
                    return await FailAsync(GameplayErrorCode.TransportReadFailed)
                        .ConfigureAwait(false);
                }

                if (readCount < 0 || readCount > available)
                {
                    return await FailAsync(GameplayErrorCode.MalformedOuterFrame)
                        .ConfigureAwait(false);
                }

                if (readCount == 0)
                {
                    return await FailAsync(
                            receiveCount == 0
                                ? GameplayErrorCode.RemoteClosed
                                : GameplayErrorCode.TruncatedStream)
                        .ConfigureAwait(false);
                }

                receiveCount += readCount;
            }
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await operationGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            currentFrameAuthority.Invalidate();
            if (Interlocked.Exchange(ref terminal, 2) == 0)
            {
                await transportSession.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async ValueTask<GameplayMirrorPumpResult> FailAsync(
        GameplayErrorCode error)
    {
        currentFrameAuthority.Invalidate();
        if (Interlocked.Exchange(ref terminal, 2) == 0)
        {
            await transportSession.DisposeAsync().ConfigureAwait(false);
        }

        return GameplayMirrorPumpResult.Failure(error, mirror.Snapshot);
    }

    private void Consume(int count)
    {
        int remaining = receiveCount - count;
        if (remaining > 0)
        {
            Buffer.BlockCopy(receiveBuffer, count, receiveBuffer, 0, remaining);
        }

        receiveCount = remaining;
    }

    private static GameplayErrorCode MapProtocolError(
        ProtocolErrorCode error) =>
        error switch
        {
            ProtocolErrorCode.UnsupportedPacketType or
            ProtocolErrorCode.UnknownPacketType =>
                GameplayErrorCode.UnsupportedOuterPacket,
            ProtocolErrorCode.TruncatedFrame =>
                GameplayErrorCode.TruncatedStream,
            _ => GameplayErrorCode.MalformedOuterFrame
        };

    private static bool IsProvisionalFrameSourceFailure(
        PerspectiveSafeFrameSourceResultV1 result) =>
        !result.IsSuccess &&
        result.Error is
        {
            Code: PerspectiveSafeFrameSourceErrorCodeV1.UnprovenMirrorValue,
            Section: PerspectiveSafeSourceSectionV1.Entities
        };

    private static bool IsValidMsgHint(ReadOnlySpan<byte> bytes) =>
        bytes.Length == MsgHintLength &&
        bytes[MsgHintPlayerOffset] <= 1;
}
