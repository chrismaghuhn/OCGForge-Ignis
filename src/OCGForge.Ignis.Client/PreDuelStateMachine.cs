using System.Collections.ObjectModel;
using OCGForge.Ignis.Protocol;

namespace OCGForge.Ignis.Client;

internal sealed class I2TransitionResult
{
    private readonly I2Event[] events;
    private readonly ReadOnlyCollection<I2Event> eventsView;

    private I2TransitionResult(
        bool isSuccess,
        I2SessionState state,
        I2ErrorCode error,
        IEnumerable<I2Event> events,
        PreDuelChoiceRequest? choiceRequest,
        PreDuelOutcome? terminalOutcome)
    {
        IsSuccess = isSuccess;
        State = state;
        Error = error;
        this.events = events.ToArray();
        eventsView = Array.AsReadOnly(this.events);
        ChoiceRequest = choiceRequest;
        TerminalOutcome = terminalOutcome;
    }

    public bool IsSuccess { get; }

    public I2SessionState State { get; }

    public I2ErrorCode Error { get; }

    public IReadOnlyList<I2Event> Events => eventsView;

    public PreDuelChoiceRequest? ChoiceRequest { get; }

    public PreDuelOutcome? TerminalOutcome { get; }

    internal static I2TransitionResult Success(
        I2SessionState state,
        IEnumerable<I2Event> events,
        PreDuelChoiceRequest? choiceRequest = null,
        PreDuelOutcome? terminalOutcome = null) =>
        new(
            true,
            state,
            I2ErrorCode.None,
            events,
            choiceRequest,
            terminalOutcome);

    internal static I2TransitionResult Failure(
        I2SessionState state,
        I2ErrorCode error,
        IEnumerable<I2Event> events) =>
        new(false, state, error, events, null, null);
}

internal sealed class PreDuelStateMachine
{
    private readonly List<I2Event> eventHistory = new();
    private readonly ReadOnlyCollection<I2Event> eventHistoryView;
    private readonly LobbyState lobby = new();
    private I2SessionState state = I2SessionState.Created;
    private PreDuelChoiceRequest? pendingChoice;
    private ulong nextChoiceOrdinal;
    private PreDuelOutcome? terminalOutcome;

    internal PreDuelStateMachine()
    {
        eventHistoryView = eventHistory.AsReadOnly();
    }

    internal I2SessionState State => state;

    internal LobbyState Lobby => lobby;

    internal PreDuelChoiceRequest? PendingChoice => pendingChoice;

    internal IReadOnlyList<I2Event> EventHistory => eventHistoryView;

    internal I2TransitionResult BeginConnection() =>
        Move(I2SessionState.Created, I2SessionState.Connecting);

    internal I2TransitionResult MarkTransportConnected() =>
        MoveWithEvent(
            I2SessionState.Connecting,
            I2SessionState.TransportConnected,
            new I2Event(I2EventKind.TransportConnected));

    internal I2TransitionResult MarkPlayerInfoSent() =>
        MoveWithEvent(
            I2SessionState.TransportConnected,
            I2SessionState.PlayerInfoSent,
            new I2Event(I2EventKind.PlayerInfoSent));

    internal I2TransitionResult MarkJoinRequestSent() =>
        MoveWithEvent(
            I2SessionState.PlayerInfoSent,
            I2SessionState.JoinRequestSent,
            new I2Event(I2EventKind.JoinRequestSent));

    internal I2TransitionResult MarkDeckSubmitted() =>
        MoveWithEvent(
            I2SessionState.LobbyJoined,
            I2SessionState.DeckSubmitted,
            new I2Event(I2EventKind.DeckSubmitted));

    internal I2TransitionResult MarkReadyRequested() =>
        ValidateReadyRequest() == I2ErrorCode.None
            ? MoveWithEvent(
                I2SessionState.DeckSubmitted,
                I2SessionState.ReadyRequested,
                new I2Event(I2EventKind.ReadyRequested))
            : Fail(I2ErrorCode.InvalidStateTransition);

    internal I2TransitionResult MarkNotReadyRequested() =>
        ValidateNotReadyRequest() == I2ErrorCode.None
            ? MoveWithEvent(
                I2SessionState.Ready,
                I2SessionState.NotReadyRequested,
                new I2Event(I2EventKind.NotReadyRequested))
            : Fail(I2ErrorCode.InvalidStateTransition);

    internal I2ErrorCode ValidateReadyRequest() =>
        state == I2SessionState.DeckSubmitted
            ? I2ErrorCode.None
            : I2ErrorCode.InvalidStateTransition;

    internal I2ErrorCode ValidateNotReadyRequest() =>
        state == I2SessionState.Ready
            ? I2ErrorCode.None
            : I2ErrorCode.InvalidStateTransition;

    internal I2ErrorCode ValidateDuelStartRequest()
    {
        return state != I2SessionState.Ready ||
            !lobby.IsHost ||
            lobby.PreDuelLobbyPosition is not (0 or 1) ||
            !lobby.HasOccupiedPlayer(ClientContractV1.FirstDuelistPosition) ||
            !lobby.HasOccupiedPlayer(ClientContractV1.SecondDuelistPosition) ||
            !IsReady(ClientContractV1.FirstDuelistPosition) ||
            !IsReady(ClientContractV1.SecondDuelistPosition)
            ? I2ErrorCode.InvalidStateTransition
            : I2ErrorCode.None;
    }

    internal I2ErrorCode ValidateLeave() =>
        state is I2SessionState.PlayerInfoSent or
            I2SessionState.JoinRequestSent or
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
            I2SessionState.WaitingForTpChoice
            ? I2ErrorCode.None
            : I2ErrorCode.InvalidStateTransition;

    internal I2TransitionResult MarkLeaveSent()
    {
        if (ValidateLeave() != I2ErrorCode.None)
        {
            return Fail(I2ErrorCode.InvalidStateTransition);
        }

        state = I2SessionState.Closed;
        return Succeed(new I2Event(I2EventKind.Closed));
    }

    internal I2TransitionResult MarkDuelStartRequested()
    {
        if (ValidateDuelStartRequest() != I2ErrorCode.None)
        {
            return Fail(I2ErrorCode.InvalidStateTransition);
        }

        state = I2SessionState.Starting;
        return Succeed(new I2Event(I2EventKind.DuelStartRequested));
    }

    internal I2TransitionResult ApplyPacket(ValidatedStocPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if (state is I2SessionState.Failed or
            I2SessionState.Closed or
            I2SessionState.HandedOff)
        {
            return I2TransitionResult.Failure(
                state,
                I2ErrorCode.InvalidStateTransition,
                Array.Empty<I2Event>());
        }

        return packet.Type switch
        {
            StocPacketType.ErrorMsg => ApplyError(packet.Payload),
            StocPacketType.JoinGame => ApplyJoinGame(packet.Payload),
            StocPacketType.TypeChange => ApplyTypeChange(packet.Payload),
            StocPacketType.HsPlayerEnter => ApplyPlayerEntered(packet.Payload),
            StocPacketType.HsPlayerChange => ApplyPlayerChange(packet.Payload),
            StocPacketType.HsWatchChange => ApplyWatcherChange(packet.Payload),
            StocPacketType.DuelStart => ApplyDuelStart(),
            StocPacketType.SelectHand => ApplySelectHand(),
            StocPacketType.HandResult => ApplyHandResult(packet.Payload),
            StocPacketType.SelectTp => ApplySelectTp(),
            StocPacketType.GameMsg or StocPacketType.TpResult =>
                Fail(I2ErrorCode.UnexpectedPacketForState),
            StocPacketType.LeaveGame or StocPacketType.DuelEnd =>
                Fail(I2ErrorCode.ServerLeft),
            _ => Fail(I2ErrorCode.UnsupportedPacket)
        };
    }

    internal I2TransitionResult SubmitChoice(
        PreDuelChoiceTokenV1 token,
        byte value)
    {
        if (pendingChoice is null)
        {
            return Fail(I2ErrorCode.ChoiceNotPending);
        }

        if (pendingChoice.Token != token)
        {
            return Fail(I2ErrorCode.StaleChoice);
        }

        if (!pendingChoice.LegalValues.Contains(value))
        {
            return Fail(I2ErrorCode.InvalidChoice);
        }

        PreDuelChoiceKind kind = pendingChoice.Kind;
        pendingChoice = null;
        if (kind == PreDuelChoiceKind.Rps)
        {
            state = I2SessionState.WaitingForHandResult;
            return Succeed(
                new I2Event(
                    I2EventKind.RpsChoiceSent,
                    Value: value));
        }

        state = I2SessionState.HandedOff;
        terminalOutcome = PreDuelOutcome.RpsWin;
        return Succeed(
            new I2Event(I2EventKind.TurnPreferenceSent, Value: value),
            new I2Event(I2EventKind.HandedOff),
            terminalOutcome: terminalOutcome);
    }

    internal I2ErrorCode ValidateChoice(
        PreDuelChoiceTokenV1 token,
        byte value)
    {
        if (pendingChoice is null)
        {
            return I2ErrorCode.ChoiceNotPending;
        }

        if (pendingChoice.Token != token)
        {
            return I2ErrorCode.StaleChoice;
        }

        return pendingChoice.LegalValues.Contains(value)
            ? I2ErrorCode.None
            : I2ErrorCode.InvalidChoice;
    }

    internal I2TransitionResult MarkClosed()
    {
        if (state is I2SessionState.HandedOff or
            I2SessionState.Failed or
            I2SessionState.Closed)
        {
            return I2TransitionResult.Failure(
                state,
                I2ErrorCode.InvalidStateTransition,
                Array.Empty<I2Event>());
        }

        state = I2SessionState.Closed;
        return Succeed(new I2Event(I2EventKind.Closed));
    }

    internal I2TransitionResult FailExternal(I2ErrorCode error) => Fail(error);

    internal PreDuelSessionV1 CreatePublicSession()
    {
        if (state != I2SessionState.HandedOff ||
            terminalOutcome is not PreDuelOutcome outcome ||
            lobby.HostInfo is not HostInfoPayload hostInfo ||
            lobby.PreDuelLobbyPosition is not byte position)
        {
            throw new InvalidOperationException(
                "A public pre-duel session exists only after handoff.");
        }

        return new PreDuelSessionV1(
            hostInfo,
            position,
            lobby.IsHost,
            outcome,
            eventHistory);
    }

    private I2TransitionResult ApplyError(object? payload)
    {
        return payload switch
        {
            JoinErrorPayload => Fail(I2ErrorCode.JoinRejected),
            LegacyVersionErrorPayload or VersionError2Payload =>
                Fail(I2ErrorCode.VersionMismatch),
            DeckErrorCardCodePayload or
            DeckErrorCountPayload or
            DeckErrorTypeOnlyPayload => Fail(I2ErrorCode.DeckRejected),
            SideErrorPayload => Fail(I2ErrorCode.SideFlowUnsupported),
            _ => Fail(I2ErrorCode.ProtocolFailure)
        };
    }

    private I2TransitionResult ApplyJoinGame(object? payload)
    {
        if (state != I2SessionState.JoinRequestSent ||
            payload is not HostInfoPayload hostInfo)
        {
            return Fail(I2ErrorCode.InvalidStateTransition);
        }

        if (hostInfo.Handshake != ClientContractV1.ExpectedServerHandshake)
        {
            return Fail(I2ErrorCode.ServerHandshakeMismatch);
        }

        if (hostInfo.Team1 != ClientContractV1.ExpectedTeam1Size ||
            hostInfo.Team2 != ClientContractV1.ExpectedTeam2Size ||
            (hostInfo.DuelFlagLow & ClientContractV1.DuelRelayFlag) != 0 ||
            hostInfo.BestOf != ClientContractV1.BestOfRequired)
        {
            return Fail(I2ErrorCode.UnsupportedRoomTopology);
        }

        lobby.HostInfo = hostInfo;
        state = I2SessionState.LobbyJoined;
        return Succeed(new I2Event(I2EventKind.LobbyJoined));
    }

    private I2TransitionResult ApplyTypeChange(object? payload)
    {
        if (!IsLobbyState() || payload is not StocTypeChangePayload typeChange)
        {
            return Fail(I2ErrorCode.InvalidStateTransition);
        }

        byte position = (byte)(typeChange.Type & 0x0f);
        if (position is not 0 and not 1)
        {
            return Fail(I2ErrorCode.UnsupportedRoomTopology);
        }

        lobby.PreDuelLobbyPosition = position;
        lobby.IsHost = (typeChange.Type & 0xf0) != 0;
        return Succeed(
            new I2Event(
                I2EventKind.OwnTypeChanged,
                Position: position,
                Value: lobby.IsHost ? (byte)1 : (byte)0));
    }

    private I2TransitionResult ApplyPlayerEntered(object? payload)
    {
        if (!IsLobbyState() ||
            payload is not StocHsPlayerEnterPayload playerEntered)
        {
            return Fail(I2ErrorCode.InvalidStateTransition);
        }

        if (playerEntered.Position is not 0 and not 1)
        {
            return Fail(I2ErrorCode.UnsupportedRoomTopology);
        }

        if (lobby.HasOccupiedPlayer(playerEntered.Position))
        {
            return Fail(I2ErrorCode.InvalidStateTransition);
        }

        lobby.ApplyPlayerEntered(playerEntered.Position, playerEntered.Name);
        return Succeed(
            new I2Event(
                I2EventKind.PlayerEntered,
                Position: playerEntered.Position,
                Name: playerEntered.Name));
    }

    private I2TransitionResult ApplyPlayerChange(object? payload)
    {
        if (!IsLobbyState() ||
            payload is not StocHsPlayerChangePayload playerChange)
        {
            return Fail(I2ErrorCode.InvalidStateTransition);
        }

        byte oldPosition = (byte)(playerChange.Status >> 4);
        byte newValue = (byte)(playerChange.Status & 0x0f);
        if (oldPosition > 5)
        {
            return Fail(I2ErrorCode.UnsupportedLobbyPositionMove);
        }

        if (newValue <= 5)
        {
            if (oldPosition > 1 || newValue > 1)
            {
                return Fail(I2ErrorCode.UnsupportedRoomTopology);
            }

            if (oldPosition == newValue ||
                !lobby.HasOccupiedPlayer(oldPosition) ||
                lobby.HasOccupiedPlayer(newValue))
            {
                return Fail(I2ErrorCode.InvalidStateTransition);
            }

            lobby.ApplyPlayerMoved(oldPosition, newValue);
            if (lobby.PreDuelLobbyPosition == oldPosition)
            {
                lobby.PreDuelLobbyPosition = newValue;
            }

            return Succeed(
                new I2Event(
                    I2EventKind.PlayerMoved,
                    Position: oldPosition,
                    Value: newValue));
        }

        if (newValue == 0x08)
        {
            if (oldPosition > 1)
            {
                return Fail(I2ErrorCode.UnsupportedRoomTopology);
            }

            if (!lobby.HasOccupiedPlayer(oldPosition))
            {
                return Fail(I2ErrorCode.InvalidStateTransition);
            }

            if (lobby.PreDuelLobbyPosition == oldPosition)
            {
                return Fail(I2ErrorCode.UnsupportedRoomTopology);
            }

            lobby.RemovePlayer(oldPosition);
            return Succeed(
                new I2Event(
                    I2EventKind.PlayerStatusChanged,
                    Position: oldPosition,
                    Status: newValue));
        }

        if (newValue is not (0x09 or 0x0a or 0x0b))
        {
            return Fail(I2ErrorCode.UnsupportedLobbyPositionMove);
        }

        if (oldPosition > 1)
        {
            return Fail(I2ErrorCode.UnsupportedRoomTopology);
        }

        if (!lobby.HasOccupiedPlayer(oldPosition))
        {
            return Fail(I2ErrorCode.InvalidStateTransition);
        }

        bool isOwnPosition = lobby.PreDuelLobbyPosition == oldPosition;
        if (isOwnPosition)
        {
            if (newValue == 0x09)
            {
                if (state != I2SessionState.ReadyRequested)
                {
                    return Fail(I2ErrorCode.InvalidStateTransition);
                }

                state = I2SessionState.Ready;
            }
            else if (newValue == 0x0a)
            {
                if (state is not (I2SessionState.ReadyRequested or
                    I2SessionState.NotReadyRequested))
                {
                    return Fail(I2ErrorCode.InvalidStateTransition);
                }

                state = I2SessionState.DeckSubmitted;
            }
            else
            {
                return Fail(I2ErrorCode.ServerLeft);
            }
        }

        if (newValue == 0x0b)
        {
            lobby.RemovePlayer(oldPosition);
        }
        else
        {
            lobby.ApplyPlayerStatus(oldPosition, newValue);
        }

        return Succeed(
            new I2Event(
                I2EventKind.PlayerStatusChanged,
                Position: oldPosition,
                Status: newValue));
    }

    private I2TransitionResult ApplyWatcherChange(object? payload)
    {
        if (!IsLobbyState() ||
            payload is not StocHsWatchChangePayload watcherChange)
        {
            return Fail(I2ErrorCode.InvalidStateTransition);
        }

        lobby.WatcherCount = watcherChange.WatchCount;

        return Succeed(
            new I2Event(
                I2EventKind.WatcherCountChanged,
                WatcherCount: watcherChange.WatchCount));
    }

    private I2TransitionResult ApplyDuelStart()
    {
        if (state is not (I2SessionState.Ready or I2SessionState.Starting) ||
            lobby.PreDuelLobbyPosition is not (0 or 1) ||
            !lobby.HasOccupiedPlayer(ClientContractV1.FirstDuelistPosition) ||
            !lobby.HasOccupiedPlayer(ClientContractV1.SecondDuelistPosition) ||
            !IsReady(ClientContractV1.FirstDuelistPosition) ||
            !IsReady(ClientContractV1.SecondDuelistPosition))
        {
            return Fail(I2ErrorCode.InvalidStateTransition);
        }

        state = I2SessionState.DuelStarted;
        return Succeed(new I2Event(I2EventKind.DuelStarted));
    }

    private I2TransitionResult ApplySelectHand()
    {
        if (state != I2SessionState.DuelStarted)
        {
            return Fail(I2ErrorCode.InvalidStateTransition);
        }

        return PublishChoice(
            PreDuelChoiceKind.Rps,
            ClientContractV1.RpsValues,
            I2EventKind.RpsRequested);
    }

    private I2TransitionResult ApplyHandResult(object? payload)
    {
        if (state != I2SessionState.WaitingForHandResult ||
            payload is not StocHandResultPayload handResult)
        {
            return Fail(I2ErrorCode.InvalidStateTransition);
        }

        if (!ClientContractV1.RpsValues.Contains(handResult.Result1) ||
            !ClientContractV1.RpsValues.Contains(handResult.Result2))
        {
            return Fail(I2ErrorCode.ProtocolFailure);
        }

        I2Event resultEvent = new(
            I2EventKind.RpsResultReceived,
            Status: handResult.Result2,
            Value: handResult.Result1);
        if (handResult.Result1 == handResult.Result2)
        {
            state = I2SessionState.DuelStarted;
            return Succeed(resultEvent);
        }

        if (OwnsRpsWin(handResult.Result1, handResult.Result2))
        {
            state = I2SessionState.WaitingForTpRequest;
            return Succeed(resultEvent);
        }

        state = I2SessionState.HandedOff;
        terminalOutcome = PreDuelOutcome.RpsLoss;
        return Succeed(
            resultEvent,
            new I2Event(I2EventKind.HandedOff),
            terminalOutcome: terminalOutcome);
    }

    private I2TransitionResult ApplySelectTp()
    {
        if (state != I2SessionState.WaitingForTpRequest)
        {
            return Fail(I2ErrorCode.InvalidStateTransition);
        }

        return PublishChoice(
            PreDuelChoiceKind.TurnPreference,
            ClientContractV1.TurnPreferenceValues,
            I2EventKind.TurnPreferenceRequested);
    }

    private I2TransitionResult PublishChoice(
        PreDuelChoiceKind kind,
        IReadOnlyList<byte> legalValues,
        I2EventKind eventKind)
    {
        if (nextChoiceOrdinal == ulong.MaxValue)
        {
            return Fail(I2ErrorCode.ChoiceOrdinalOverflow);
        }

        PreDuelChoiceTokenV1 token = new(nextChoiceOrdinal);
        nextChoiceOrdinal++;
        pendingChoice = new PreDuelChoiceRequest(kind, token, legalValues);
        state = kind == PreDuelChoiceKind.Rps
            ? I2SessionState.WaitingForHandChoice
            : I2SessionState.WaitingForTpChoice;
        return Succeed(
            new I2Event(eventKind, ChoiceToken: token),
            choiceRequest: pendingChoice);
    }

    private bool IsReady(byte position) =>
        lobby.TryGetPlayer(position, out LobbyPlayerSnapshot player) &&
        player.IsOccupied &&
        player.IsReady;

    private bool IsLobbyState() => state is
        I2SessionState.LobbyJoined or
        I2SessionState.DeckSubmitted or
        I2SessionState.ReadyRequested or
        I2SessionState.Ready or
        I2SessionState.NotReadyRequested or
        I2SessionState.Starting;

    private static bool OwnsRpsWin(byte own, byte opponent) =>
        (own == 1 && opponent == 3) ||
        (own == 2 && opponent == 1) ||
        (own == 3 && opponent == 2);

    private I2TransitionResult Move(
        I2SessionState expected,
        I2SessionState next)
    {
        if (state != expected)
        {
            return Fail(I2ErrorCode.InvalidStateTransition);
        }

        state = next;
        return Succeed();
    }

    private I2TransitionResult MoveWithEvent(
        I2SessionState expected,
        I2SessionState next,
        I2Event @event)
    {
        I2TransitionResult result = Move(expected, next);
        if (!result.IsSuccess)
        {
            return result;
        }

        eventHistory.Add(@event);
        return I2TransitionResult.Success(state, new[] { @event });
    }

    private I2TransitionResult Succeed(
        I2Event? first = null,
        I2Event? second = null,
        PreDuelChoiceRequest? choiceRequest = null,
        PreDuelOutcome? terminalOutcome = null)
    {
        List<I2Event> events = new();
        if (first is not null)
        {
            events.Add(first);
            eventHistory.Add(first);
        }

        if (second is not null)
        {
            events.Add(second);
            eventHistory.Add(second);
        }

        return I2TransitionResult.Success(
            state,
            events,
            choiceRequest,
            terminalOutcome);
    }

    private I2TransitionResult Fail(I2ErrorCode error)
    {
        if (state is I2SessionState.Failed or
            I2SessionState.Closed or
            I2SessionState.HandedOff)
        {
            return I2TransitionResult.Failure(
                state,
                I2ErrorCode.InvalidStateTransition,
                Array.Empty<I2Event>());
        }

        state = I2SessionState.Failed;
        I2Event failure = new(I2EventKind.Failed, Error: error);
        eventHistory.Add(failure);
        return I2TransitionResult.Failure(
            state,
            error,
            new[] { failure });
    }
}
