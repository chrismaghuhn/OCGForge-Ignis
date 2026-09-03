using System.Collections.ObjectModel;
using OCGForge.Ignis.Protocol;

namespace OCGForge.Ignis.Client;

public sealed class I2PumpResult
{
    private readonly I2Event[] events;
    private readonly ReadOnlyCollection<I2Event> eventsView;

    internal I2PumpResult(
        bool isSuccess,
        I2SessionState state,
        I2ErrorCode error,
        IEnumerable<I2Event> events,
        PreDuelChoiceRequest? choiceRequest,
        GameplayTransportHandoffV1? runtimeHandoff)
    {
        IsSuccess = isSuccess;
        State = state;
        Error = error;
        this.events = events.ToArray();
        eventsView = Array.AsReadOnly(this.events);
        ChoiceRequest = choiceRequest;
        RuntimeHandoff = runtimeHandoff;
    }

    public bool IsSuccess { get; }

    public I2SessionState State { get; }

    public I2ErrorCode Error { get; }

    public IReadOnlyList<I2Event> Events => eventsView;

    public PreDuelChoiceRequest? ChoiceRequest { get; }

    public GameplayTransportHandoffV1? RuntimeHandoff { get; }

    internal static I2PumpResult Success(
        I2SessionState state,
        IEnumerable<I2Event> events,
        PreDuelChoiceRequest? choiceRequest,
        GameplayTransportHandoffV1? runtimeHandoff) =>
        new(
            true,
            state,
            I2ErrorCode.None,
            events,
            choiceRequest,
            runtimeHandoff);

    internal static I2PumpResult Failure(
        I2SessionState state,
        I2ErrorCode error,
        IEnumerable<I2Event> events,
        GameplayTransportHandoffV1? runtimeHandoff = null) =>
        new(
            false,
            state,
            error,
            events,
            null,
            runtimeHandoff);
}

public sealed class I2SessionRunner : IAsyncDisposable
{
    private readonly IByteTransport transport;
    private readonly ReceiveBuffer receiveBuffer =
        new(ClientContractV1.MaxReceiveBufferBytes);
    private readonly byte[] readStorage =
        new byte[ClientContractV1.MaxReceiveBufferBytes];
    private readonly PreDuelStateMachine stateMachine = new();
    private readonly SemaphoreSlim operationGate = new(1, 1);
    private GameplayTransportHandoffV1? runtimeHandoff;
    private int transportClosed;
    private bool disposed;

    public I2SessionRunner(IByteTransport transport)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public I2SessionState State => stateMachine.State;

    public IReadOnlyList<I2Event> Events => stateMachine.EventHistory;

    public PreDuelChoiceRequest? PendingChoice => stateMachine.PendingChoice;

    public LobbyState Lobby => stateMachine.Lobby;

    public GameplayTransportHandoffV1? RuntimeHandoff => runtimeHandoff;

    public async ValueTask<I2Result> StartAsync(
        ConnectionConfigurationV1 configuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (!await EnterOperationAsync(cancellationToken).ConfigureAwait(false))
        {
            return await HandleUnacquiredCancellationAsync().ConfigureAwait(false);
        }

        try
        {
            if (disposed || stateMachine.State != I2SessionState.Created)
            {
                return I2Result.Failure(I2ErrorCode.InvalidStateTransition);
            }

            if (!stateMachine.BeginConnection().IsSuccess)
            {
                return await FailAndCloseAsync(
                    I2ErrorCode.InvalidStateTransition).ConfigureAwait(false);
            }

            try
            {
                await transport.ConnectAsync(
                        configuration.Host,
                        configuration.Port,
                        configuration.ConnectionTimeout,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return await CloseForCancellationAsync().ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                return await FailAndCloseAsync(
                    I2ErrorCode.ConnectionTimeout).ConfigureAwait(false);
            }
            catch
            {
                return await FailAndCloseAsync(
                    I2ErrorCode.ConnectionFailed).ConfigureAwait(false);
            }

            if (!stateMachine.MarkTransportConnected().IsSuccess)
            {
                return await FailAndCloseAsync(
                    I2ErrorCode.InvalidStateTransition).ConfigureAwait(false);
            }

            try
            {
                byte[] playerInfoPayload = PacketPayloadCodec.EncodePlayerInfo(
                    new CtosPlayerInfoPayload(configuration.PlayerName));
                await WriteCtosAsync(
                        CtosPacketType.PlayerInfo,
                        playerInfoPayload,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return await CloseForCancellationAsync().ConfigureAwait(false);
            }
            catch
            {
                return await FailAndCloseAsync(I2ErrorCode.SendFailed).ConfigureAwait(false);
            }

            if (!stateMachine.MarkPlayerInfoSent().IsSuccess)
            {
                return await FailAndCloseAsync(
                    I2ErrorCode.InvalidStateTransition).ConfigureAwait(false);
            }

            try
            {
                byte[] joinPayload = PacketPayloadCodec.EncodeJoinGame(
                    new CtosJoinGamePayload(
                        ProtocolContractV1.ExpectedProVersion,
                        configuration.GameId,
                        configuration.RoomPassword.Value,
                        ProtocolContractV1.ExpectedClientVersion));
                await WriteCtosAsync(
                        CtosPacketType.JoinGame,
                        joinPayload,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return await CloseForCancellationAsync().ConfigureAwait(false);
            }
            catch
            {
                return await FailAndCloseAsync(I2ErrorCode.SendFailed).ConfigureAwait(false);
            }

            return stateMachine.MarkJoinRequestSent().IsSuccess
                ? I2Result.Success()
                : await FailAndCloseAsync(
                    I2ErrorCode.InvalidStateTransition).ConfigureAwait(false);
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async ValueTask<I2Result> SubmitDeckAsync(
        PrevalidatedProtocolDeck deck,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(deck);
        if (!await EnterOperationAsync(cancellationToken).ConfigureAwait(false))
        {
            return await HandleUnacquiredCancellationAsync().ConfigureAwait(false);
        }

        try
        {
            if (disposed || stateMachine.State != I2SessionState.LobbyJoined)
            {
                return await FailAndCloseAsync(
                    I2ErrorCode.InvalidStateTransition).ConfigureAwait(false);
            }

            byte[] payload;
            try
            {
                payload = PacketPayloadCodec.EncodeUpdateDeck(
                    new CtosUpdateDeckPayload(
                        deck.MainAndExtraCards,
                        deck.SideCards));
                await WriteCtosAsync(
                        CtosPacketType.UpdateDeck,
                        payload,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return await CloseForCancellationAsync().ConfigureAwait(false);
            }
            catch
            {
                return await FailAndCloseAsync(I2ErrorCode.SendFailed).ConfigureAwait(false);
            }

            I2TransitionResult result = stateMachine.MarkDeckSubmitted();
            return result.IsSuccess
                ? I2Result.Success()
                : await FailAndCloseAsync(result.Error).ConfigureAwait(false);
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async ValueTask<I2Result> RequestReadyAsync(
        CancellationToken cancellationToken)
    {
        return await SendEmptyCtosAndMarkAsync(
                CtosPacketType.HsReady,
                stateMachine.ValidateReadyRequest,
                stateMachine.MarkReadyRequested,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<I2Result> RequestNotReadyAsync(
        CancellationToken cancellationToken)
    {
        return await SendEmptyCtosAndMarkAsync(
                CtosPacketType.HsNotReady,
                stateMachine.ValidateNotReadyRequest,
                stateMachine.MarkNotReadyRequested,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<I2Result> RequestDuelStartAsync(
        CancellationToken cancellationToken)
    {
        return await SendEmptyCtosAndMarkAsync(
                CtosPacketType.HsStart,
                stateMachine.ValidateDuelStartRequest,
                stateMachine.MarkDuelStartRequested,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<I2Result> LeaveAsync(
        CancellationToken cancellationToken)
    {
        if (!await EnterOperationAsync(cancellationToken).ConfigureAwait(false))
        {
            return await HandleUnacquiredCancellationAsync().ConfigureAwait(false);
        }

        try
        {
            if (disposed || stateMachine.State is
                I2SessionState.Failed or
                I2SessionState.Closed or
                I2SessionState.HandedOff)
            {
                return I2Result.Failure(
                    stateMachine.State == I2SessionState.HandedOff
                        ? I2ErrorCode.TransportOwnershipError
                        : I2ErrorCode.InvalidStateTransition);
            }

            I2TransitionResult result = stateMachine.MarkClosed();
            await CloseTransportOnceAsync().ConfigureAwait(false);
            return result.IsSuccess
                ? I2Result.Success()
                : I2Result.Failure(result.Error);
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async ValueTask<I2Result> SubmitChoiceAsync(
        PreDuelChoiceTokenV1 token,
        byte value,
        CancellationToken cancellationToken)
    {
        if (!await EnterOperationAsync(cancellationToken).ConfigureAwait(false))
        {
            return await HandleUnacquiredCancellationAsync().ConfigureAwait(false);
        }

        try
        {
            if (disposed || stateMachine.State is
                I2SessionState.Failed or
                I2SessionState.Closed or
                I2SessionState.HandedOff)
            {
                return I2Result.Failure(I2ErrorCode.InvalidStateTransition);
            }

            I2ErrorCode validation = stateMachine.ValidateChoice(token, value);
            if (validation != I2ErrorCode.None)
            {
                return await FailAndCloseAsync(validation).ConfigureAwait(false);
            }

            PreDuelChoiceRequest request = stateMachine.PendingChoice!;
            CtosPacketType packetType = request.Kind == PreDuelChoiceKind.Rps
                ? CtosPacketType.HandResult
                : CtosPacketType.TpResult;
            byte[] payload = request.Kind == PreDuelChoiceKind.Rps
                ? PacketPayloadCodec.EncodeCtosHandResult(
                    new CtosHandResultPayload(value))
                : PacketPayloadCodec.EncodeCtosTpResult(
                    new CtosTpResultPayload(value));

            try
            {
                await WriteCtosAsync(packetType, payload, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return await CloseForCancellationAsync().ConfigureAwait(false);
            }
            catch
            {
                return await FailAndCloseAsync(I2ErrorCode.SendFailed).ConfigureAwait(false);
            }

            I2TransitionResult result = stateMachine.SubmitChoice(token, value);
            if (!result.IsSuccess)
            {
                return await FailAndCloseAsync(result.Error).ConfigureAwait(false);
            }

            if (stateMachine.State == I2SessionState.HandedOff)
            {
                FinalizeHandoff();
            }

            return I2Result.Success();
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async ValueTask<I2PumpResult> PumpReadAsync(
        CancellationToken cancellationToken)
    {
        if (!await EnterOperationAsync(cancellationToken).ConfigureAwait(false))
        {
            return await HandleUnacquiredPumpCancellationAsync().ConfigureAwait(false);
        }

        try
        {
            if (disposed || stateMachine.State is
                I2SessionState.Failed or
                I2SessionState.Closed)
            {
                return I2PumpResult.Failure(
                    stateMachine.State,
                    I2ErrorCode.InvalidStateTransition,
                    Array.Empty<I2Event>());
            }

            if (stateMachine.State == I2SessionState.HandedOff)
            {
                return I2PumpResult.Success(
                    stateMachine.State,
                    Array.Empty<I2Event>(),
                    null,
                    runtimeHandoff);
            }

            if (!CanPumpRead())
            {
                return await PumpFailureAsync(
                        I2ErrorCode.InvalidStateTransition)
                    .ConfigureAwait(false);
            }

            if (stateMachine.PendingChoice is not null)
            {
                return I2PumpResult.Success(
                    stateMachine.State,
                    Array.Empty<I2Event>(),
                    stateMachine.PendingChoice,
                    runtimeHandoff);
            }

            List<I2Event> newEvents = new();
            while (true)
            {
                I2PumpResult? bufferedResult =
                    await ProcessBufferedFramesAsync(newEvents)
                        .ConfigureAwait(false);
                if (bufferedResult is not null)
                {
                    return bufferedResult;
                }

                int readCount;
                try
                {
                    int available = receiveBuffer.Capacity - receiveBuffer.Count;
                    if (available == 0)
                    {
                        return await PumpFailureAsync(
                                I2ErrorCode.ReceiveBufferOverflow)
                            .ConfigureAwait(false);
                    }

                    readCount = await transport.ReadAsync(
                            readStorage.AsMemory(0, available),
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return await ClosePumpForCancellationAsync().ConfigureAwait(false);
                }
                catch
                {
                    return await PumpFailureAsync(I2ErrorCode.ConnectionFailed)
                        .ConfigureAwait(false);
                }

                if (readCount < 0 ||
                    readCount > receiveBuffer.Capacity - receiveBuffer.Count)
                {
                    return await PumpFailureAsync(
                            I2ErrorCode.ReceiveBufferOverflow)
                        .ConfigureAwait(false);
                }

                if (readCount == 0)
                {
                    return await PumpFailureAsync(
                            receiveBuffer.Count == 0
                                ? I2ErrorCode.RemoteClosed
                                : I2ErrorCode.TruncatedStream)
                        .ConfigureAwait(false);
                }

                I2Result appended = receiveBuffer.Append(
                    readStorage.AsSpan(0, readCount));
                if (!appended.IsSuccess)
                {
                    return await PumpFailureAsync(appended.Error)
                        .ConfigureAwait(false);
                }
            }

        }
        finally
        {
            operationGate.Release();
        }
    }

    public async ValueTask<I2Result> CloseAsync()
    {
        await operationGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (stateMachine.State == I2SessionState.HandedOff)
            {
                return I2Result.Failure(I2ErrorCode.TransportOwnershipError);
            }

            if (stateMachine.State is I2SessionState.Closed or I2SessionState.Failed)
            {
                await CloseTransportOnceAsync().ConfigureAwait(false);
                return I2Result.Success();
            }

            I2TransitionResult result = stateMachine.MarkClosed();
            await CloseTransportOnceAsync().ConfigureAwait(false);
            return result.IsSuccess
                ? I2Result.Success()
                : I2Result.Failure(result.Error);
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await operationGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (stateMachine.State != I2SessionState.HandedOff)
            {
                await CloseTransportOnceAsync().ConfigureAwait(false);
                stateMachine.MarkClosed();
            }
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async ValueTask<I2Result> SendEmptyCtosAndMarkAsync(
        CtosPacketType packetType,
        Func<I2ErrorCode> validate,
        Func<I2TransitionResult> mark,
        CancellationToken cancellationToken)
    {
        if (!await EnterOperationAsync(cancellationToken).ConfigureAwait(false))
        {
            return await HandleUnacquiredCancellationAsync().ConfigureAwait(false);
        }

        try
        {
            if (disposed || stateMachine.State is
                I2SessionState.Failed or
                I2SessionState.Closed or
                I2SessionState.HandedOff)
            {
                return I2Result.Failure(I2ErrorCode.InvalidStateTransition);
            }

            I2ErrorCode validation = validate();
            if (validation != I2ErrorCode.None)
            {
                return await FailAndCloseAsync(validation).ConfigureAwait(false);
            }

            try
            {
                await WriteCtosAsync(
                        packetType,
                        ReadOnlyMemory<byte>.Empty,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return await CloseForCancellationAsync().ConfigureAwait(false);
            }
            catch
            {
                return await FailAndCloseAsync(I2ErrorCode.SendFailed).ConfigureAwait(false);
            }

            I2TransitionResult result = mark();
            return result.IsSuccess
                ? I2Result.Success()
                : await FailAndCloseAsync(result.Error).ConfigureAwait(false);
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async ValueTask WriteCtosAsync(
        CtosPacketType packetType,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        byte[] frame = WireFrameCodec.EncodeCtos(packetType, payload.Span);
        await transport.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<I2Result> FailAndCloseAsync(I2ErrorCode error)
    {
        stateMachine.FailExternal(error);
        await CloseTransportOnceAsync().ConfigureAwait(false);
        return I2Result.Failure(error);
    }

    private async ValueTask<I2Result> CloseForCancellationAsync()
    {
        if (stateMachine.State == I2SessionState.HandedOff)
        {
            return I2Result.Failure(I2ErrorCode.TransportOwnershipError);
        }

        stateMachine.MarkClosed();
        await CloseTransportOnceAsync().ConfigureAwait(false);
        return I2Result.Failure(I2ErrorCode.Cancelled);
    }

    private async ValueTask<I2Result> HandleUnacquiredCancellationAsync()
    {
        if (!operationGate.Wait(0))
        {
            return stateMachine.State == I2SessionState.HandedOff
                ? I2Result.Failure(I2ErrorCode.TransportOwnershipError)
                : I2Result.Failure(I2ErrorCode.Cancelled);
        }

        try
        {
            return await CloseForCancellationAsync().ConfigureAwait(false);
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async ValueTask<I2PumpResult> ClosePumpForCancellationAsync()
    {
        if (stateMachine.State == I2SessionState.HandedOff)
        {
            return I2PumpResult.Failure(
                stateMachine.State,
                I2ErrorCode.TransportOwnershipError,
                Array.Empty<I2Event>(),
                runtimeHandoff);
        }

        I2TransitionResult closed = stateMachine.MarkClosed();
        await CloseTransportOnceAsync().ConfigureAwait(false);
        return I2PumpResult.Failure(
            stateMachine.State,
            I2ErrorCode.Cancelled,
            closed.Events);
    }

    private async ValueTask<I2PumpResult> HandleUnacquiredPumpCancellationAsync()
    {
        if (!operationGate.Wait(0))
        {
            return I2PumpResult.Failure(
                stateMachine.State,
                stateMachine.State == I2SessionState.HandedOff
                    ? I2ErrorCode.TransportOwnershipError
                    : I2ErrorCode.Cancelled,
                Array.Empty<I2Event>(),
                runtimeHandoff);
        }

        try
        {
            return await ClosePumpForCancellationAsync().ConfigureAwait(false);
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async ValueTask<I2PumpResult> PumpFailureAsync(
        I2ErrorCode error,
        IEnumerable<I2Event>? existingEvents = null)
    {
        I2TransitionResult failure = stateMachine.FailExternal(error);
        List<I2Event> events = existingEvents?.ToList() ?? new();
        events.AddRange(failure.Events);
        await CloseTransportOnceAsync().ConfigureAwait(false);
        return I2PumpResult.Failure(stateMachine.State, error, events);
    }

    private void FinalizeHandoff()
    {
        if (runtimeHandoff is not null)
        {
            return;
        }

        PreDuelSessionV1 publicSession = stateMachine.CreatePublicSession();
        runtimeHandoff = new GameplayTransportHandoffV1(
            transport,
            publicSession,
            receiveBuffer.CopyUnread());
    }

    private async ValueTask<I2PumpResult?> ProcessBufferedFramesAsync(
        List<I2Event> newEvents)
    {
        bool processedFrame = false;
        while (receiveBuffer.Count > 0)
        {
            FrameReadResult<ValidatedStocPacket> parsed =
                PacketPayloadValidator.TryReadValidatedStoc(
                    receiveBuffer.Unread.Span);
            if (parsed.Status == FrameReadStatus.NeedMoreData)
            {
                return null;
            }

            if (parsed.Status == FrameReadStatus.Invalid ||
                parsed.Frame is null ||
                parsed.ConsumedBytes <= 0)
            {
                I2ErrorCode error = parsed.Status == FrameReadStatus.Invalid
                    ? MapProtocolError(parsed.Error)
                    : I2ErrorCode.ProtocolFailure;
                return await PumpFailureAsync(error, newEvents)
                    .ConfigureAwait(false);
            }

            receiveBuffer.Consume(parsed.ConsumedBytes);
            processedFrame = true;
            I2TransitionResult transition = stateMachine.ApplyPacket(parsed.Frame);
            newEvents.AddRange(transition.Events);
            if (!transition.IsSuccess)
            {
                await CloseTransportOnceAsync().ConfigureAwait(false);
                return I2PumpResult.Failure(
                    stateMachine.State,
                    transition.Error,
                    newEvents);
            }

            if (stateMachine.State == I2SessionState.HandedOff)
            {
                FinalizeHandoff();
                return I2PumpResult.Success(
                    stateMachine.State,
                    newEvents,
                    null,
                    runtimeHandoff);
            }

            if (stateMachine.PendingChoice is not null)
            {
                if (receiveBuffer.Count > 0)
                {
                    return await PumpFailureAsync(
                            I2ErrorCode.UnexpectedPacketForState,
                            newEvents)
                        .ConfigureAwait(false);
                }

                return I2PumpResult.Success(
                    stateMachine.State,
                    newEvents,
                    stateMachine.PendingChoice,
                    runtimeHandoff);
            }
        }

        return processedFrame
            ? I2PumpResult.Success(
                stateMachine.State,
                newEvents,
                stateMachine.PendingChoice,
                runtimeHandoff)
            : null;
    }

    private bool CanPumpRead() => stateMachine.State is
        I2SessionState.JoinRequestSent or
        I2SessionState.JoinAccepted or
        I2SessionState.LobbyJoined or
        I2SessionState.DeckSubmitted or
        I2SessionState.ReadyRequested or
        I2SessionState.Ready or
        I2SessionState.NotReadyRequested or
        I2SessionState.Starting or
        I2SessionState.DuelStarted or
        I2SessionState.WaitingForHandChoice or
        I2SessionState.WaitingForHandResult or
        I2SessionState.WaitingForTpRequest or
        I2SessionState.WaitingForTpChoice;

    private async ValueTask CloseTransportOnceAsync()
    {
        if (Interlocked.Exchange(ref transportClosed, 1) != 0)
        {
            return;
        }

        try
        {
            await transport.CloseAsync().ConfigureAwait(false);
        }
        catch
        {
        }

        try
        {
            await transport.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private async ValueTask<bool> EnterOperationAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private static I2ErrorCode MapProtocolError(ProtocolErrorCode error) =>
        error switch
        {
            ProtocolErrorCode.UnsupportedPacketType or
            ProtocolErrorCode.UnknownPacketType => I2ErrorCode.UnsupportedPacket,
            ProtocolErrorCode.UnsupportedVersion => I2ErrorCode.VersionMismatch,
            _ => I2ErrorCode.ProtocolFailure
        };
}
