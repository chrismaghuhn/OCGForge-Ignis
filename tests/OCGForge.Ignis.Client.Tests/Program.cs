using OCGForge.Ignis.Client;
using OCGForge.Ignis.Protocol;

var tests = new (string Name, Action Body)[]
{
    ("client contract constants", TestClientContractConstants),
    ("connection configuration validation", TestConnectionConfigurationValidation),
    ("room password redaction", TestRoomPasswordRedaction),
    ("scripted transport and receive buffer", TestScriptedTransportAndReceiveBuffer),
    ("states choices lobby and handoff values", TestStatesChoicesLobbyAndHandoffValues),
    ("pre-duel state machine transitions", TestPreDuelStateMachineTransitions),
    ("session runner transcript", TestSessionRunnerTranscript),
    ("illegal commands do not send packets", TestIllegalCommandsDoNotSendPackets),
    ("topology and ordering failures", TestTopologyAndOrderingFailures),
    ("failure ownership and cancellation", TestFailureOwnershipAndCancellation),
    ("chunking metamorphic transcript", TestChunkingMetamorphicTranscript),
    ("lobby message and packet failures", TestLobbyMessagesAndPacketFailures),
    ("RPS loss handoff", TestRpsLossHandoff),
    ("transport failure mapping", TestTransportFailureMapping),
    ("explicit leave lifecycle", TestExplicitLeaveLifecycle),
    ("buffered input and serialized cancellation", TestBufferedInputAndSerializedCancellation),
    ("I2 remediation barriers", TestI2RemediationBarriers)
};

int passed = 0;
int failed = 0;
foreach ((string name, Action body) in tests)
{
    try
    {
        body();
        Console.WriteLine($"PASS {name}");
        passed++;
    }
    catch (Exception exception)
    {
        Console.WriteLine($"FAIL {name}: {exception.GetType().Name}: {exception.Message}");
        failed++;
    }
}

Console.WriteLine($"RESULT passed={passed} failed={failed}");
Environment.ExitCode = failed == 0 ? 0 : 1;

static void TestClientContractConstants()
{
    Equal("ocgforge-ignis.client.preduel.v1", ClientContractV1.Id);
    Equal(0x00000080u, ClientContractV1.DuelRelayFlag);
    Equal(1, ClientContractV1.BestOfRequired);
    Equal(4043399681u, ClientContractV1.ExpectedServerHandshake);
    SequenceEqual(new byte[] { 1, 2, 3 }, ClientContractV1.RpsValues);
    SequenceEqual(new byte[] { 0, 1 }, ClientContractV1.TurnPreferenceValues);
}

static void TestConnectionConfigurationValidation()
{
    RoomPasswordV1 password = RoomPasswordV1.Create("synthetic-pass");
    ConnectionConfigurationV1 valid = new(
        "localhost",
        7911,
        "Ignis",
        7,
        password,
        TimeSpan.FromSeconds(5));
    Equal("localhost", valid.Host);
    Equal(7911, valid.Port);
    Equal("Ignis", valid.PlayerName);
    Equal(7u, valid.GameId);
    Equal(TimeSpan.FromSeconds(5), valid.ConnectionTimeout);

    AssertConfigurationFailure(
        " ",
        7911,
        "Ignis",
        password,
        TimeSpan.FromSeconds(5),
        I2ErrorCode.InvalidConfiguration);
    AssertConfigurationFailure(
        "localhost",
        0,
        "Ignis",
        password,
        TimeSpan.FromSeconds(5),
        I2ErrorCode.InvalidConfiguration);
    AssertConfigurationFailure(
        "localhost",
        65536,
        "Ignis",
        password,
        TimeSpan.FromSeconds(5),
        I2ErrorCode.InvalidConfiguration);
    AssertConfigurationFailure(
        "localhost",
        7911,
        new string('x', 20),
        password,
        TimeSpan.FromSeconds(5),
        I2ErrorCode.InvalidConfiguration);
    AssertConfigurationFailure(
        "localhost",
        7911,
        "a\0b",
        password,
        TimeSpan.FromSeconds(5),
        I2ErrorCode.InvalidConfiguration);
    AssertConfigurationFailure(
        "localhost",
        7911,
        "Ignis",
        password,
        TimeSpan.Zero,
        I2ErrorCode.InvalidConfiguration);
    AssertConfigurationFailure(
        "localhost",
        7911,
        "Ignis",
        password,
        Timeout.InfiniteTimeSpan,
        I2ErrorCode.InvalidConfiguration);
    AssertConfigurationFailure(
        "localhost",
        7911,
        "Ignis",
        password,
        TimeSpan.MaxValue,
        I2ErrorCode.InvalidConfiguration);

    AssertThrows<ClientConfigurationException>(
        () => RoomPasswordV1.Create(new string('p', 20)));
}

static void TestRoomPasswordRedaction()
{
    RoomPasswordV1 password = RoomPasswordV1.Create("synthetic-pass");
    ConnectionConfigurationV1 configuration = new(
        "localhost",
        7911,
        "Ignis",
        7,
        password,
        TimeSpan.FromSeconds(5));
    False(password.ToString().Contains("synthetic-pass", StringComparison.Ordinal));
    False(configuration.ToString().Contains("synthetic-pass", StringComparison.Ordinal));
}

static void TestScriptedTransportAndReceiveBuffer()
{
    ReceiveBuffer buffer = new(10);
    Equal(I2ErrorCode.None, buffer.Append(new byte[] { 1, 2, 3 }).Error);
    Equal(3, buffer.Count);
    BytesEqual(new byte[] { 1, 2, 3 }, buffer.Unread.Span);
    buffer.Consume(2);
    BytesEqual(new byte[] { 3 }, buffer.Unread.Span);
    Equal(I2ErrorCode.None, buffer.Append(new byte[] { 4, 5 }).Error);
    BytesEqual(new byte[] { 3, 4, 5 }, buffer.Unread.Span);
    Equal(I2ErrorCode.ReceiveBufferOverflow, buffer.Append(new byte[8]).Error);
    BytesEqual(new byte[] { 3, 4, 5 }, buffer.Unread.Span);
    BytesEqual(new byte[] { 3, 4, 5 }, buffer.CopyUnread());

    ScriptedTransport transport = new(new[]
    {
        new byte[] { 1, 2, 3 },
        new byte[] { 4 }
    });
    transport.ConnectAsync(
            "localhost",
            7911,
            TimeSpan.FromSeconds(1),
            CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(true, transport.IsConnected);

    byte[] destination = new byte[2];
    Equal(
        2,
        transport.ReadAsync(destination, CancellationToken.None)
            .GetAwaiter()
            .GetResult());
    BytesEqual(new byte[] { 1, 2 }, destination);
    Equal(
        1,
        transport.ReadAsync(destination, CancellationToken.None)
            .GetAwaiter()
            .GetResult());
    Equal(3, destination[0]);
    Equal(
        1,
        transport.ReadAsync(destination, CancellationToken.None)
            .GetAwaiter()
            .GetResult());
    Equal(4, destination[0]);
    Equal(
        0,
        transport.ReadAsync(destination, CancellationToken.None)
            .GetAwaiter()
            .GetResult());

    transport.WriteAsync(
            new byte[] { 0xaa, 0xbb },
            CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(1, transport.Writes.Count);
    BytesEqual(new byte[] { 0xaa, 0xbb }, transport.Writes[0]);
    transport.CloseAsync().GetAwaiter().GetResult();
    transport.DisposeAsync().GetAwaiter().GetResult();
    Equal(1, transport.CloseCallCount);
}

static void TestStatesChoicesLobbyAndHandoffValues()
{
    Equal(I2SessionState.Created, I2SessionState.Created);
    Equal(I2SessionState.WaitingForTpRequest, I2SessionState.WaitingForTpRequest);
    Equal(I2SessionState.HandedOff, I2SessionState.HandedOff);
    Equal(I2SessionState.Failed, I2SessionState.Failed);

    PreDuelChoiceTokenV1 firstToken = new(0);
    PreDuelChoiceTokenV1 secondToken = new(1);
    False(firstToken.Equals(secondToken));
    byte[] sourceValues = { 1, 2, 3 };
    PreDuelChoiceRequest request = new(
        PreDuelChoiceKind.Rps,
        firstToken,
        sourceValues);
    sourceValues[0] = 99;
    SequenceEqual(new byte[] { 1, 2, 3 }, request.LegalValues);
    Equal(PreDuelChoiceKind.Rps, request.Kind);
    Equal(firstToken, request.Token);

    LobbyState lobby = new();
    lobby.ApplyPlayerEntered(0, "duplicate");
    lobby.ApplyPlayerEntered(1, "duplicate");
    IReadOnlyList<LobbyPlayerSnapshot> players = lobby.SnapshotPlayers();
    Equal(2, players.Count);
    Equal((byte)0, players[0].Position);
    Equal((byte)1, players[1].Position);
    Equal("duplicate", players[0].Name);
    Equal("duplicate", players[1].Name);

    I2Event[] events =
    {
        new I2Event(I2EventKind.LobbyJoined),
        new I2Event(I2EventKind.HandedOff)
    };
    PreDuelSessionV1 session = new(
        default,
        0,
        false,
        PreDuelOutcome.RpsWin,
        events);
    False(session.ToString().Contains("synthetic-pass", StringComparison.Ordinal));
    False(session.ToString().Contains("localhost", StringComparison.Ordinal));
    Equal((byte)0, session.PreDuelLobbyPosition);
    Equal(PreDuelOutcome.RpsWin, session.Outcome);
    Equal(2, session.Events.Count);

    ScriptedTransport transport = new(Array.Empty<byte[]>());
    GameplayTransportHandoffV1 handoff = new(
        transport,
        session,
        new byte[] { 0xaa, 0xbb });
    BytesEqual(new byte[] { 0xaa, 0xbb }, handoff.PendingBytes.Span);
    Equal(I2ErrorCode.None, handoff.Claim().Error);
    Equal(I2ErrorCode.TransportOwnershipError, handoff.Claim().Error);
}

static void TestPreDuelStateMachineTransitions()
{
    PreDuelStateMachine machine = NewJoinRequestMachine();
    I2TransitionResult joined = machine.ApplyPacket(
        ValidatedStoc(StocPacketType.JoinGame, ValidHostInfoPayload()));
    True(joined.IsSuccess);
    Equal(I2SessionState.JoinAccepted, machine.State);

    True(machine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.TypeChange,
            PacketPayloadCodec.EncodeStocTypeChange(
                new StocTypeChangePayload(0x10)))).IsSuccess);
    True(machine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsPlayerEnter,
            PacketPayloadCodec.EncodeStocHsPlayerEnter(
                new StocHsPlayerEnterPayload("Ignis", 0)))).IsSuccess);
    True(machine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsPlayerEnter,
            PacketPayloadCodec.EncodeStocHsPlayerEnter(
                new StocHsPlayerEnterPayload("Opponent", 1)))).IsSuccess);

    Equal(I2SessionState.DeckSubmitted, machine.MarkDeckSubmitted().State);
    Equal(I2SessionState.ReadyRequested, machine.MarkReadyRequested().State);
    True(machine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsPlayerChange,
            PacketPayloadCodec.EncodeStocHsPlayerChange(
                new StocHsPlayerChangePayload(0x09)))).IsSuccess);
    Equal(I2SessionState.Ready, machine.State);
    True(machine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsPlayerChange,
            PacketPayloadCodec.EncodeStocHsPlayerChange(
                new StocHsPlayerChangePayload(0x19)))).IsSuccess);

    Equal(I2SessionState.Starting, machine.MarkDuelStartRequested().State);
    True(machine.ApplyPacket(
        ValidatedStoc(StocPacketType.DuelStart, Array.Empty<byte>())).IsSuccess);
    Equal(I2SessionState.DuelStarted, machine.State);
    I2TransitionResult handRequest = machine.ApplyPacket(
        ValidatedStoc(StocPacketType.SelectHand, Array.Empty<byte>()));
    True(handRequest.IsSuccess);
    Equal(I2SessionState.WaitingForHandChoice, machine.State);
    Equal(0UL, handRequest.ChoiceRequest!.Token.Ordinal);

    Equal(
        I2SessionState.WaitingForHandResult,
        machine.SubmitChoice(handRequest.ChoiceRequest.Token, 1).State);
    Equal(
        I2SessionState.WaitingForTpRequest,
        machine.ApplyPacket(
            ValidatedStoc(
                StocPacketType.HandResult,
                PacketPayloadCodec.EncodeStocHandResult(
                    new StocHandResultPayload(1, 3)))).State);

    I2TransitionResult tpRequest = machine.ApplyPacket(
        ValidatedStoc(StocPacketType.SelectTp, Array.Empty<byte>()));
    True(tpRequest.IsSuccess);
    Equal(I2SessionState.WaitingForTpChoice, machine.State);
    Equal(1UL, tpRequest.ChoiceRequest!.Token.Ordinal);
    I2TransitionResult handoff = machine.SubmitChoice(
        tpRequest.ChoiceRequest.Token,
        0);
    True(handoff.IsSuccess);
    Equal(I2SessionState.HandedOff, machine.State);
    Equal(PreDuelOutcome.RpsWin, handoff.TerminalOutcome);

    PreDuelStateMachine tieMachine = NewRpsMachine();
    I2TransitionResult firstRequest = tieMachine.ApplyPacket(
        ValidatedStoc(StocPacketType.SelectHand, Array.Empty<byte>()));
    True(tieMachine.SubmitChoice(firstRequest.ChoiceRequest!.Token, 1).IsSuccess);
    I2TransitionResult tie = tieMachine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HandResult,
            PacketPayloadCodec.EncodeStocHandResult(
                new StocHandResultPayload(1, 1))));
    True(tie.IsSuccess);
    Equal(I2SessionState.DuelStarted, tieMachine.State);
    I2TransitionResult secondRequest = tieMachine.ApplyPacket(
        ValidatedStoc(StocPacketType.SelectHand, Array.Empty<byte>()));
    Equal(1UL, secondRequest.ChoiceRequest!.Token.Ordinal);
    Equal(
        I2ErrorCode.StaleChoice,
        tieMachine.SubmitChoice(firstRequest.ChoiceRequest.Token, 1).Error);

    foreach ((byte Own, byte Opponent) in new[]
    {
        ((byte)1, (byte)3),
        ((byte)2, (byte)1),
        ((byte)3, (byte)2)
    })
    {
        PreDuelStateMachine winMachine = NewRpsMachine();
        I2TransitionResult request = winMachine.ApplyPacket(
            ValidatedStoc(StocPacketType.SelectHand, Array.Empty<byte>()));
        True(winMachine.SubmitChoice(request.ChoiceRequest!.Token, 1).IsSuccess);
        Equal(
            I2SessionState.WaitingForTpRequest,
            winMachine.ApplyPacket(
                ValidatedStoc(
                    StocPacketType.HandResult,
                    PacketPayloadCodec.EncodeStocHandResult(
                        new StocHandResultPayload(Own, Opponent)))).State);
    }

    foreach ((byte Own, byte Opponent) in new[]
    {
        ((byte)1, (byte)2),
        ((byte)2, (byte)3),
        ((byte)3, (byte)1)
    })
    {
        PreDuelStateMachine lossMachine = NewRpsMachine();
        I2TransitionResult request = lossMachine.ApplyPacket(
            ValidatedStoc(StocPacketType.SelectHand, Array.Empty<byte>()));
        True(lossMachine.SubmitChoice(request.ChoiceRequest!.Token, 1).IsSuccess);
        I2TransitionResult loss = lossMachine.ApplyPacket(
            ValidatedStoc(
                StocPacketType.HandResult,
                PacketPayloadCodec.EncodeStocHandResult(
                    new StocHandResultPayload(Own, Opponent))));
        Equal(I2SessionState.HandedOff, lossMachine.State);
        Equal(PreDuelOutcome.RpsLoss, loss.TerminalOutcome);
    }

    foreach ((byte Own, byte Opponent) in new[]
    {
        ((byte)0, (byte)1),
        ((byte)1, (byte)4),
        ((byte)4, (byte)1)
    })
    {
        PreDuelStateMachine invalidMachine = NewRpsMachine();
        I2TransitionResult request = invalidMachine.ApplyPacket(
            ValidatedStoc(StocPacketType.SelectHand, Array.Empty<byte>()));
        True(invalidMachine.SubmitChoice(request.ChoiceRequest!.Token, 1).IsSuccess);
        I2TransitionResult invalid = invalidMachine.ApplyPacket(
            ValidatedStoc(
                StocPacketType.HandResult,
                PacketPayloadCodec.EncodeStocHandResult(
                    new StocHandResultPayload(Own, Opponent))));
        Equal(I2ErrorCode.ProtocolFailure, invalid.Error);
        Equal(I2SessionState.Failed, invalidMachine.State);
    }

    I2TransitionResult duplicate = machine.ApplyPacket(
        ValidatedStoc(StocPacketType.HandResult, new byte[] { 1, 3 }));
    Equal(I2ErrorCode.InvalidStateTransition, duplicate.Error);
}

static void TestSessionRunnerTranscript()
{
    ScriptedTransport transport = new(Array.Empty<byte[]>());
    I2SessionRunner runner = new(transport);
    ConnectionConfigurationV1 configuration = new(
        "localhost",
        7911,
        "Ignis",
        7,
        RoomPasswordV1.Create("synthetic-pass"),
        TimeSpan.FromSeconds(5));

    I2Result started = runner.StartAsync(configuration, CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    True(started.IsSuccess);
    Equal(I2SessionState.JoinRequestSent, runner.State);
    Equal(2, transport.Writes.Count);
    Equal(I2EventKind.TransportConnected, runner.Events[0].Kind);
    Equal(I2EventKind.PlayerInfoSent, runner.Events[1].Kind);
    Equal(I2EventKind.JoinRequestSent, runner.Events[2].Kind);
    Equal(CtosPacketType.PlayerInfo, ReadCtos(transport.Writes[0]).Type);
    Equal(CtosPacketType.JoinGame, ReadCtos(transport.Writes[1]).Type);
    False(runner.Events.Any(@event => @event.ToString().Contains(
        "synthetic-pass",
        StringComparison.Ordinal)));

    transport.Enqueue(
        Concat(
            StocFrame(StocPacketType.JoinGame, ValidHostInfoPayload()),
            StocFrame(
                StocPacketType.TypeChange,
                PacketPayloadCodec.EncodeStocTypeChange(
                    new StocTypeChangePayload(0x10))),
            StocFrame(
                StocPacketType.HsPlayerEnter,
                PacketPayloadCodec.EncodeStocHsPlayerEnter(
                    new StocHsPlayerEnterPayload("Ignis", 0))),
            StocFrame(
                StocPacketType.HsPlayerEnter,
                PacketPayloadCodec.EncodeStocHsPlayerEnter(
                    new StocHsPlayerEnterPayload("Opponent", 1)))));
    I2PumpResult lobby = runner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    True(lobby.IsSuccess);
    Equal(I2SessionState.LobbyJoined, runner.State);

    PrevalidatedProtocolDeck deck = new(
        new uint[] { 0x11223344, 0x55667788 },
        new uint[] { 0xaabbccdd });
    True(runner.SubmitDeckAsync(deck, CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    CtosFrame deckFrame = ReadCtos(transport.Writes[2]);
    Equal(CtosPacketType.UpdateDeck, deckFrame.Type);
    CtosUpdateDeckPayload decodedDeck = PacketPayloadCodec
        .DecodeUpdateDeck(deckFrame.Payload.Span)
        .Value;
    SequenceEqual(
        new uint[] { 0x11223344, 0x55667788 },
        decodedDeck.MainAndExtraCards);
    SequenceEqual(new uint[] { 0xaabbccdd }, decodedDeck.SideCards);
    True(runner.RequestReadyAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    Equal(I2SessionState.ReadyRequested, runner.State);

    transport.Enqueue(
        Concat(
            StocFrame(
                StocPacketType.HsPlayerChange,
                PacketPayloadCodec.EncodeStocHsPlayerChange(
                    new StocHsPlayerChangePayload(0x09))),
            StocFrame(
                StocPacketType.HsPlayerChange,
                PacketPayloadCodec.EncodeStocHsPlayerChange(
                    new StocHsPlayerChangePayload(0x19)))));
    True(runner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    Equal(I2SessionState.Ready, runner.State);
    True(runner.RequestDuelStartAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    Equal(I2SessionState.Starting, runner.State);

    transport.Enqueue(
        Concat(
            StocFrame(StocPacketType.DuelStart, Array.Empty<byte>()),
            StocFrame(StocPacketType.SelectHand, Array.Empty<byte>())));
    I2PumpResult handRequest = runner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    True(handRequest.IsSuccess);
    Equal(I2SessionState.WaitingForHandChoice, runner.State);
    Equal(0UL, handRequest.ChoiceRequest!.Token.Ordinal);
    True(runner.SubmitChoiceAsync(
            handRequest.ChoiceRequest.Token,
            1,
            CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    Equal(I2SessionState.WaitingForHandResult, runner.State);

    transport.Enqueue(
        StocFrame(
            StocPacketType.HandResult,
            PacketPayloadCodec.EncodeStocHandResult(
                new StocHandResultPayload(1, 3))));
    True(runner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    Equal(I2SessionState.WaitingForTpRequest, runner.State);

    byte[] gameMessage = StocFrame(
        StocPacketType.GameMsg,
        new byte[] { 0x99, 0x88, 0x77 });
    transport.Enqueue(
        StocFrame(StocPacketType.SelectTp, Array.Empty<byte>()));
    I2PumpResult tpRequest = runner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    True(tpRequest.IsSuccess);
    Equal(I2SessionState.WaitingForTpChoice, runner.State);
    Equal(1UL, tpRequest.ChoiceRequest!.Token.Ordinal);
    int readsAtChoice = transport.ReadCallCount;
    I2PumpResult whileChoicePending = runner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    True(whileChoicePending.IsSuccess);
    Equal(readsAtChoice, transport.ReadCallCount);
    True(runner.SubmitChoiceAsync(
            tpRequest.ChoiceRequest.Token,
            0,
            CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    Equal(I2SessionState.HandedOff, runner.State);
    NotNull(runner.RuntimeHandoff);
    BytesEqual(Array.Empty<byte>(), runner.RuntimeHandoff!.PendingBytes.Span);
    Equal(0, transport.CloseCallCount);

    transport.Enqueue(gameMessage);
    byte[] futureGameMessage = new byte[gameMessage.Length];
    int futureBytes = runner.RuntimeHandoff!.Transport.ReadAsync(
            futureGameMessage,
            CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(gameMessage.Length, futureBytes);
    BytesEqual(gameMessage, futureGameMessage);
}

static void TestIllegalCommandsDoNotSendPackets()
{
    foreach (Func<I2SessionRunner, CancellationToken, I2Result> operation in
        new Func<I2SessionRunner, CancellationToken, I2Result>[]
    {
        static (runner, cancellationToken) => runner.RequestReadyAsync(cancellationToken)
            .GetAwaiter()
            .GetResult(),
        static (runner, cancellationToken) => runner.RequestNotReadyAsync(cancellationToken)
            .GetAwaiter()
            .GetResult(),
        static (runner, cancellationToken) => runner.RequestDuelStartAsync(cancellationToken)
            .GetAwaiter()
            .GetResult()
    })
    {
        ScriptedTransport transport = new(Array.Empty<byte[]>());
        I2SessionRunner runner = new(transport);
        True(runner.StartAsync(
                new ConnectionConfigurationV1(
                    "localhost",
                    7911,
                    "Ignis",
                    7,
                    RoomPasswordV1.Create("synthetic-pass"),
                    TimeSpan.FromSeconds(5)),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult()
            .IsSuccess);
        Equal(2, transport.Writes.Count);
        I2Result result = operation(runner, CancellationToken.None);
        Equal(I2ErrorCode.InvalidStateTransition, result.Error);
        Equal(2, transport.Writes.Count);
        Equal(I2SessionState.Failed, runner.State);
        Equal(1, transport.CloseCallCount);
    }
}

static void TestTopologyAndOrderingFailures()
{
    var invalidHosts = new (HostInfoPayload Host, I2ErrorCode Error)[]
    {
        (ValidHostInfo() with { Team1 = 2 }, I2ErrorCode.UnsupportedRoomTopology),
        (ValidHostInfo() with { Team2 = 2 }, I2ErrorCode.UnsupportedRoomTopology),
        (ValidHostInfo() with
        {
            Mode = 0,
            DuelFlagLow = ClientContractV1.DuelRelayFlag
        }, I2ErrorCode.UnsupportedRoomTopology),
        (ValidHostInfo() with { BestOf = 2 }, I2ErrorCode.UnsupportedRoomTopology),
        (ValidHostInfo() with { Handshake = 1 }, I2ErrorCode.ServerHandshakeMismatch)
    };
    foreach ((HostInfoPayload host, I2ErrorCode expectedError) in invalidHosts)
    {
        PreDuelStateMachine machine = NewJoinRequestMachine();
        I2TransitionResult result = machine.ApplyPacket(
            ValidatedStoc(
                StocPacketType.JoinGame,
                PacketPayloadCodec.EncodeStocJoinGame(host)));
        Equal(expectedError, result.Error);
        Equal(I2SessionState.Failed, machine.State);
    }

    PreDuelStateMachine observerMachine = NewJoinRequestMachine();
    True(observerMachine.ApplyPacket(
        ValidatedStoc(StocPacketType.JoinGame, ValidHostInfoPayload())).IsSuccess);
    I2TransitionResult observer = observerMachine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.TypeChange,
            PacketPayloadCodec.EncodeStocTypeChange(
            new StocTypeChangePayload(7))));
    Equal(I2ErrorCode.UnsupportedRoomTopology, observer.Error);

    PreDuelStateMachine missingOwnType = NewReadyMachine();
    missingOwnType.Lobby.PreDuelLobbyPosition = null;
    Equal(
        I2ErrorCode.InvalidStateTransition,
        missingOwnType.ValidateDuelStartRequest());

    PreDuelStateMachine notReadyMachine = NewReadyMachine();
    Equal(
        I2SessionState.NotReadyRequested,
        notReadyMachine.MarkNotReadyRequested().State);
    PreDuelStateMachine invalidNotReady = NewReadyMachine();
    True(invalidNotReady.MarkNotReadyRequested().IsSuccess);
    Equal(
        I2ErrorCode.InvalidStateTransition,
        invalidNotReady.MarkNotReadyRequested().Error);
    Equal(
        I2ErrorCode.InvalidStateTransition,
        invalidNotReady.MarkDuelStartRequested().Error);
    I2TransitionResult notReadyConfirmed = notReadyMachine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsPlayerChange,
            PacketPayloadCodec.EncodeStocHsPlayerChange(
                new StocHsPlayerChangePayload(0x0a))));
    True(notReadyConfirmed.IsSuccess);
    Equal(I2SessionState.DeckSubmitted, notReadyMachine.State);

    PreDuelStateMachine wrongOrder = NewJoinRequestMachine();
    I2TransitionResult typeBeforeJoin = wrongOrder.ApplyPacket(
        ValidatedStoc(
            StocPacketType.TypeChange,
            PacketPayloadCodec.EncodeStocTypeChange(
                new StocTypeChangePayload(0))));
    Equal(I2ErrorCode.InvalidStateTransition, typeBeforeJoin.Error);
    Equal(I2SessionState.Failed, wrongOrder.State);
}

static void TestFailureOwnershipAndCancellation()
{
    ScriptedTransport unsupportedTransport = new(new[]
    {
        new byte[] { 0x01, 0x00, 0xf0 }
    });
    I2SessionRunner unsupportedRunner = NewStartedRunner(unsupportedTransport);
    I2PumpResult unsupported = unsupportedRunner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(I2ErrorCode.UnsupportedPacket, unsupported.Error);
    Equal(I2SessionState.Failed, unsupportedRunner.State);
    Equal(1, unsupportedTransport.CloseCallCount);
    int unsupportedReads = unsupportedTransport.ReadCallCount;
    I2PumpResult afterUnsupported = unsupportedRunner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(I2ErrorCode.InvalidStateTransition, afterUnsupported.Error);
    Equal(unsupportedReads, unsupportedTransport.ReadCallCount);
    Equal(1, unsupportedTransport.CloseCallCount);

    ScriptedTransport truncatedTransport = new(new[]
    {
        new byte[] { 0x05, 0x00, (byte)StocPacketType.DuelStart }
    });
    I2SessionRunner truncatedRunner = NewStartedRunner(truncatedTransport);
    I2PumpResult truncated = truncatedRunner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(I2ErrorCode.TruncatedStream, truncated.Error);
    Equal(I2SessionState.Failed, truncatedRunner.State);
    Equal(1, truncatedTransport.CloseCallCount);

    BlockingTransport cancellationTransport = new();
    I2SessionRunner cancellationRunner = NewStartedRunner(cancellationTransport);
    using CancellationTokenSource cancellation = new();
    Task<I2PumpResult> pendingRead = cancellationRunner
        .PumpReadAsync(cancellation.Token)
        .AsTask();
    cancellation.Cancel();
    I2PumpResult cancelled = pendingRead.GetAwaiter().GetResult();
    Equal(I2ErrorCode.Cancelled, cancelled.Error);
    Equal(I2SessionState.Closed, cancellationRunner.State);
    True(cancelled.Events.Any(@event => @event.Kind == I2EventKind.Closed));
    Equal(1, cancellationTransport.CloseCallCount);
    int cancellationReads = cancellationTransport.ReadCallCount;
    I2PumpResult afterCancellation = cancellationRunner
        .PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(I2ErrorCode.InvalidStateTransition, afterCancellation.Error);
    Equal(cancellationReads, cancellationTransport.ReadCallCount);
}

static void TestChunkingMetamorphicTranscript()
{
    (I2SessionState State, string Events, string Writes, byte[] Pending) baseline =
        RunChunkedWinner(ChunkingMode.AllCoalesced);
    BytesEqual(
        Array.Empty<byte>(),
        baseline.Pending);

    foreach (ChunkingMode mode in new[]
    {
        ChunkingMode.OneByte,
        ChunkingMode.OneFrame,
        ChunkingMode.Irregular
    })
    {
        (I2SessionState State, string Events, string Writes, byte[] Pending) actual =
            RunChunkedWinner(mode);
        Equal(baseline.State, actual.State);
        Equal(baseline.Events, actual.Events);
        Equal(baseline.Writes, actual.Writes);
    }
}

static void TestLobbyMessagesAndPacketFailures()
{
    PreDuelStateMachine moveMachine = NewJoinRequestMachine();
    True(moveMachine.ApplyPacket(
        ValidatedStoc(StocPacketType.JoinGame, ValidHostInfoPayload())).IsSuccess);
    True(moveMachine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.TypeChange,
            PacketPayloadCodec.EncodeStocTypeChange(
                new StocTypeChangePayload(0x10)))).IsSuccess);
    True(moveMachine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsPlayerEnter,
            PacketPayloadCodec.EncodeStocHsPlayerEnter(
                new StocHsPlayerEnterPayload("Ignis", 0)))).IsSuccess);
    I2TransitionResult move = moveMachine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsPlayerChange,
            PacketPayloadCodec.EncodeStocHsPlayerChange(
                new StocHsPlayerChangePayload(0x01))));
    True(move.IsSuccess);
    Equal(I2EventKind.PlayerMoved, move.Events[0].Kind);
    Equal((byte)1, move.Events[0].Value);

    PreDuelStateMachine unsupportedMoveMachine = NewJoinRequestMachine();
    True(unsupportedMoveMachine.ApplyPacket(
        ValidatedStoc(StocPacketType.JoinGame, ValidHostInfoPayload())).IsSuccess);
    True(unsupportedMoveMachine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.TypeChange,
            PacketPayloadCodec.EncodeStocTypeChange(
                new StocTypeChangePayload(0x10)))).IsSuccess);
    True(unsupportedMoveMachine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsPlayerEnter,
            PacketPayloadCodec.EncodeStocHsPlayerEnter(
                new StocHsPlayerEnterPayload("Ignis", 0)))).IsSuccess);
    I2TransitionResult unsupportedMove = unsupportedMoveMachine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsPlayerChange,
            PacketPayloadCodec.EncodeStocHsPlayerChange(
                new StocHsPlayerChangePayload(0x02))));
    Equal(I2ErrorCode.UnsupportedRoomTopology, unsupportedMove.Error);

    foreach ((StocErrorPayload Error, I2ErrorCode Expected) in new[]
    {
        ((StocErrorPayload)new JoinErrorPayload(JoinErrorCode.Password), I2ErrorCode.JoinRejected),
        ((StocErrorPayload)new VersionError2Payload(new ProtocolClientVersion(40, 0, 11, 0)), I2ErrorCode.VersionMismatch),
        ((StocErrorPayload)new DeckErrorCardCodePayload(DeckErrorCode.CardCount, 7), I2ErrorCode.DeckRejected),
        ((StocErrorPayload)new SideErrorPayload(0), I2ErrorCode.SideFlowUnsupported)
    })
    {
        PreDuelStateMachine machine = NewJoinRequestMachine();
        I2TransitionResult result = machine.ApplyPacket(
            ValidatedStoc(
                StocPacketType.ErrorMsg,
                PacketPayloadCodec.EncodeStocErrorMessage(Error)));
        Equal(Expected, result.Error);
        Equal(I2SessionState.Failed, machine.State);
    }

    PreDuelStateMachine duplicateJoin = NewJoinRequestMachine();
    True(duplicateJoin.ApplyPacket(
        ValidatedStoc(StocPacketType.JoinGame, ValidHostInfoPayload())).IsSuccess);
    I2TransitionResult duplicateJoinResult = duplicateJoin.ApplyPacket(
        ValidatedStoc(StocPacketType.JoinGame, ValidHostInfoPayload()));
    Equal(I2ErrorCode.InvalidStateTransition, duplicateJoinResult.Error);

    PreDuelStateMachine beforeDuel = NewReadyMachine();
    I2TransitionResult earlyHand = beforeDuel.ApplyPacket(
        ValidatedStoc(StocPacketType.SelectHand, Array.Empty<byte>()));
    Equal(I2ErrorCode.InvalidStateTransition, earlyHand.Error);

    PreDuelStateMachine duplicateHandResult = NewRpsMachine();
    I2TransitionResult request = duplicateHandResult.ApplyPacket(
        ValidatedStoc(StocPacketType.SelectHand, Array.Empty<byte>()));
    True(duplicateHandResult.SubmitChoice(request.ChoiceRequest!.Token, 1).IsSuccess);
    True(duplicateHandResult.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HandResult,
            PacketPayloadCodec.EncodeStocHandResult(
                new StocHandResultPayload(1, 3)))).IsSuccess);
    I2TransitionResult duplicateResult = duplicateHandResult.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HandResult,
            PacketPayloadCodec.EncodeStocHandResult(
                new StocHandResultPayload(1, 3))));
    Equal(I2ErrorCode.InvalidStateTransition, duplicateResult.Error);
    Equal(I2SessionState.Failed, duplicateHandResult.State);

    PreDuelStateMachine gameMessageMachine = NewRpsMachine();
    I2TransitionResult gameMessage = gameMessageMachine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.GameMsg,
            new byte[] { 0x01, 0x02 }));
    Equal(I2ErrorCode.UnexpectedPacketForState, gameMessage.Error);

    PreDuelStateMachine tpResultMachine = NewRpsMachine();
    I2TransitionResult tpResult = tpResultMachine.ApplyPacket(
        ValidatedStoc(StocPacketType.TpResult, Array.Empty<byte>()));
    Equal(I2ErrorCode.UnexpectedPacketForState, tpResult.Error);

    PreDuelStateMachine watcherMachine = NewReadyMachine();
    True(watcherMachine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsWatchChange,
            PacketPayloadCodec.EncodeStocHsWatchChange(
            new StocHsWatchChangePayload(3)))).IsSuccess);
    Equal((ushort)3, watcherMachine.Lobby.WatcherCount);

    PreDuelStateMachine unknownStatusMachine = NewJoinRequestMachine();
    True(unknownStatusMachine.ApplyPacket(
        ValidatedStoc(StocPacketType.JoinGame, ValidHostInfoPayload())).IsSuccess);
    I2TransitionResult unknownStatus = unknownStatusMachine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsPlayerChange,
            PacketPayloadCodec.EncodeStocHsPlayerChange(
                new StocHsPlayerChangePayload(0x09))));
    Equal(I2ErrorCode.InvalidStateTransition, unknownStatus.Error);
}

static void TestRpsLossHandoff()
{
    ScriptedTransport transport = new(Array.Empty<byte[]>());
    I2SessionRunner runner = NewStartedRunner(transport);
    transport.Enqueue(
        Concat(
            StocFrame(StocPacketType.JoinGame, ValidHostInfoPayload()),
            StocFrame(
                StocPacketType.TypeChange,
                PacketPayloadCodec.EncodeStocTypeChange(
                    new StocTypeChangePayload(0x10))),
            StocFrame(
                StocPacketType.HsPlayerEnter,
                PacketPayloadCodec.EncodeStocHsPlayerEnter(
                    new StocHsPlayerEnterPayload("Ignis", 0))),
            StocFrame(
                StocPacketType.HsPlayerEnter,
                PacketPayloadCodec.EncodeStocHsPlayerEnter(
                    new StocHsPlayerEnterPayload("Opponent", 1)))));
    True(runner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    True(runner.SubmitDeckAsync(
            new PrevalidatedProtocolDeck(new uint[] { 1 }, Array.Empty<uint>()),
            CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    True(runner.RequestReadyAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    transport.Enqueue(
        Concat(
            StocFrame(
                StocPacketType.HsPlayerChange,
                PacketPayloadCodec.EncodeStocHsPlayerChange(
                    new StocHsPlayerChangePayload(0x09))),
            StocFrame(
                StocPacketType.HsPlayerChange,
                PacketPayloadCodec.EncodeStocHsPlayerChange(
                    new StocHsPlayerChangePayload(0x19)))));
    True(runner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    True(runner.RequestDuelStartAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    transport.Enqueue(
        Concat(
            StocFrame(StocPacketType.DuelStart, Array.Empty<byte>()),
            StocFrame(StocPacketType.SelectHand, Array.Empty<byte>())));
    True(runner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    PreDuelChoiceTokenV1 handToken = runner.PendingChoice!.Token;
    True(runner.SubmitChoiceAsync(handToken, 1, CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);

    byte[] gameMessage = StocFrame(
        StocPacketType.GameMsg,
        new byte[] { 0x12, 0x34 });
    transport.Enqueue(
        Concat(
            StocFrame(
                StocPacketType.HandResult,
                PacketPayloadCodec.EncodeStocHandResult(
                    new StocHandResultPayload(1, 2))),
            gameMessage));
    I2PumpResult loss = runner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    True(loss.IsSuccess);
    Equal(I2SessionState.HandedOff, runner.State);
    NotNull(runner.RuntimeHandoff);
    Equal(PreDuelOutcome.RpsLoss, runner.RuntimeHandoff!.PublicSession.Outcome);
    BytesEqual(gameMessage, runner.RuntimeHandoff.PendingBytes.Span);
    Equal(6, transport.Writes.Count);
    Equal(CtosPacketType.HandResult, ReadCtos(transport.Writes[^1]).Type);
    Equal(
        I2ErrorCode.TransportOwnershipError,
        runner.CloseAsync().GetAwaiter().GetResult().Error);
    using CancellationTokenSource cancelledAfterHandoff = new();
    cancelledAfterHandoff.Cancel();
    I2Result cancelledLeave = runner.LeaveAsync(cancelledAfterHandoff.Token)
        .GetAwaiter()
        .GetResult();
    Equal(I2ErrorCode.TransportOwnershipError, cancelledLeave.Error);
    runner.DisposeAsync().GetAwaiter().GetResult();
    Equal(0, transport.CloseCallCount);
}

static void TestTransportFailureMapping()
{
    ScriptedTransport preCancelledTransport = new(Array.Empty<byte[]>());
    I2SessionRunner preCancelledRunner = new(preCancelledTransport);
    using CancellationTokenSource preCancelled = new();
    preCancelled.Cancel();
    Task<I2Result> preCancelledTask = preCancelledRunner
        .StartAsync(Configuration(), preCancelled.Token)
        .AsTask();
    I2Result preCancelledResult = preCancelledTask.GetAwaiter().GetResult();
    Equal(I2ErrorCode.Cancelled, preCancelledResult.Error);
    Equal(I2SessionState.Closed, preCancelledRunner.State);
    Equal(0, preCancelledTransport.ConnectCallCount);
    Equal(1, preCancelledTransport.CloseCallCount);

    ScriptedTransport timeoutTransport = new(Array.Empty<byte[]>());
    timeoutTransport.ConnectFailure = new TimeoutException();
    I2SessionRunner timeoutRunner = new(timeoutTransport);
    I2Result timeout = timeoutRunner.StartAsync(
            Configuration(),
            CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(I2ErrorCode.ConnectionTimeout, timeout.Error);
    Equal(I2SessionState.Failed, timeoutRunner.State);
    Equal(1, timeoutTransport.ConnectCallCount);
    Equal(1, timeoutTransport.CloseCallCount);

    ScriptedTransport connectFailureTransport = new(Array.Empty<byte[]>());
    connectFailureTransport.ConnectFailure = new IOException();
    I2SessionRunner connectFailureRunner = new(connectFailureTransport);
    I2Result connectFailure = connectFailureRunner.StartAsync(
            Configuration(),
            CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(I2ErrorCode.ConnectionFailed, connectFailure.Error);
    Equal(I2SessionState.Failed, connectFailureRunner.State);
    Equal(1, connectFailureTransport.CloseCallCount);

    ScriptedTransport writeFailureTransport = new(Array.Empty<byte[]>());
    writeFailureTransport.WriteFailure = new IOException();
    I2SessionRunner writeFailureRunner = new(writeFailureTransport);
    I2Result writeFailure = writeFailureRunner.StartAsync(
            Configuration(),
            CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(I2ErrorCode.SendFailed, writeFailure.Error);
    Equal(I2SessionState.Failed, writeFailureRunner.State);
    Equal(1, writeFailureTransport.CloseCallCount);

    ScriptedTransport readFailureTransport = new(Array.Empty<byte[]>());
    I2SessionRunner readFailureRunner = NewStartedRunner(readFailureTransport);
    readFailureTransport.ReadFailure = new IOException();
    I2PumpResult readFailure = readFailureRunner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(I2ErrorCode.ConnectionFailed, readFailure.Error);
    Equal(I2SessionState.Failed, readFailureRunner.State);
    Equal(1, readFailureTransport.CloseCallCount);

    ScriptedTransport remoteCloseTransport = new(Array.Empty<byte[]>());
    I2SessionRunner remoteCloseRunner = NewStartedRunner(remoteCloseTransport);
    I2PumpResult remoteClose = remoteCloseRunner
        .PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(I2ErrorCode.RemoteClosed, remoteClose.Error);
    Equal(I2SessionState.Failed, remoteCloseRunner.State);
    Equal(1, remoteCloseTransport.CloseCallCount);
}

static void TestExplicitLeaveLifecycle()
{
    ScriptedTransport rejectedTransport = new(Array.Empty<byte[]>());
    I2SessionRunner rejectedRunner = NewStartedRunner(rejectedTransport);
    rejectedTransport.Enqueue(
        StocFrame(
            StocPacketType.ErrorMsg,
            PacketPayloadCodec.EncodeStocErrorMessage(
                new JoinErrorPayload(JoinErrorCode.Password))));
    Equal(
        I2ErrorCode.JoinRejected,
        rejectedRunner.PumpReadAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult()
            .Error);
    int rejectedWrites = rejectedTransport.Writes.Count;
    Equal(
        I2ErrorCode.InvalidStateTransition,
        rejectedRunner.LeaveAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult()
            .Error);
    Equal(rejectedWrites, rejectedTransport.Writes.Count);

    ScriptedTransport transport = new(Array.Empty<byte[]>());
    I2SessionRunner runner = NewStartedRunner(transport);
    I2Result leave = runner.LeaveAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    True(leave.IsSuccess);
    Equal(I2SessionState.Closed, runner.State);
    Equal(2, transport.Writes.Count);
    Equal(1, transport.CloseCallCount);
    int reads = transport.ReadCallCount;
    Equal(
        I2ErrorCode.InvalidStateTransition,
        runner.PumpReadAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult()
            .Error);
    Equal(reads, transport.ReadCallCount);
    Equal(1, transport.CloseCallCount);
}

static void TestBufferedInputAndSerializedCancellation()
{
    ScriptedTransport notStartedTransport = new(Array.Empty<byte[]>());
    I2SessionRunner notStartedRunner = new(notStartedTransport);
    I2PumpResult notStarted = notStartedRunner
        .PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(I2ErrorCode.InvalidStateTransition, notStarted.Error);
    Equal(0, notStartedTransport.ReadCallCount);
    Equal(I2SessionState.Failed, notStartedRunner.State);

    ScriptedTransport bufferedTransport = new(Array.Empty<byte[]>());
    I2SessionRunner bufferedRunner = NewStartedRunner(bufferedTransport);
    bufferedTransport.Enqueue(
        Concat(
            StocFrame(StocPacketType.JoinGame, ValidHostInfoPayload()),
            StocFrame(
                StocPacketType.TypeChange,
                PacketPayloadCodec.EncodeStocTypeChange(
                    new StocTypeChangePayload(0x10))),
            StocFrame(
                StocPacketType.HsPlayerEnter,
                PacketPayloadCodec.EncodeStocHsPlayerEnter(
                    new StocHsPlayerEnterPayload("Ignis", 0))),
            StocFrame(
                StocPacketType.HsPlayerEnter,
                PacketPayloadCodec.EncodeStocHsPlayerEnter(
                    new StocHsPlayerEnterPayload("Opponent", 1)))));
    True(bufferedRunner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    True(bufferedRunner.SubmitDeckAsync(
            new PrevalidatedProtocolDeck(
                new uint[] { 1 },
                Array.Empty<uint>()),
            CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    True(bufferedRunner.RequestReadyAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    bufferedTransport.Enqueue(
        Concat(
            StocFrame(
                StocPacketType.HsPlayerChange,
                PacketPayloadCodec.EncodeStocHsPlayerChange(
                    new StocHsPlayerChangePayload(0x09))),
            StocFrame(
                StocPacketType.HsPlayerChange,
                PacketPayloadCodec.EncodeStocHsPlayerChange(
                    new StocHsPlayerChangePayload(0x19)))));
    True(bufferedRunner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    True(bufferedRunner.RequestDuelStartAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    int writesBeforeCausalBoundary = bufferedTransport.Writes.Count;

    bufferedTransport.Enqueue(
        Concat(
            StocFrame(StocPacketType.DuelStart, Array.Empty<byte>()),
            StocFrame(StocPacketType.SelectHand, Array.Empty<byte>()),
            StocFrame(
                StocPacketType.HandResult,
                PacketPayloadCodec.EncodeStocHandResult(
                    new StocHandResultPayload(1, 3)))));
    I2PumpResult choice = bufferedRunner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    False(choice.IsSuccess);
    Equal(I2ErrorCode.UnexpectedPacketForState, choice.Error);
    Equal(I2SessionState.Failed, bufferedRunner.State);
    Equal(1, bufferedRunner.Events.Count(@event =>
        @event.Kind == I2EventKind.RpsRequested));
    Equal(writesBeforeCausalBoundary, bufferedTransport.Writes.Count);

    BlockingTransport gateTransport = new();
    I2SessionRunner gateRunner = NewStartedRunner(gateTransport);
    using CancellationTokenSource readCancellation = new();
    Task<I2PumpResult> pendingRead = gateRunner
        .PumpReadAsync(readCancellation.Token)
        .AsTask();
    Equal(1, gateTransport.ReadCallCount);

    using CancellationTokenSource canceledRequest = new();
    canceledRequest.Cancel();
    I2Result canceled = gateRunner
        .RequestReadyAsync(canceledRequest.Token)
        .GetAwaiter()
        .GetResult();
    Equal(I2ErrorCode.Cancelled, canceled.Error);
    Equal(I2SessionState.JoinRequestSent, gateRunner.State);

    readCancellation.Cancel();
    Equal(I2ErrorCode.Cancelled, pendingRead.GetAwaiter().GetResult().Error);
    Equal(I2SessionState.Closed, gateRunner.State);
    Equal(1, gateTransport.CloseCallCount);
}

static void TestI2RemediationBarriers()
{
    ScriptedTransport joinOnlyTransport = new(Array.Empty<byte[]>());
    I2SessionRunner joinOnlyRunner = NewStartedRunner(joinOnlyTransport);
    joinOnlyTransport.Enqueue(
        StocFrame(StocPacketType.JoinGame, ValidHostInfoPayload()));
    I2PumpResult joinOnly = joinOnlyRunner
        .PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    True(joinOnly.IsSuccess);
    Equal(I2SessionState.JoinAccepted, joinOnlyRunner.State);
    Equal(
        I2ErrorCode.InvalidStateTransition,
        joinOnlyRunner.SubmitDeckAsync(
                new PrevalidatedProtocolDeck(
                    new uint[] { 1 },
                    Array.Empty<uint>()),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult()
            .Error);
    Equal(2, joinOnlyTransport.Writes.Count);

    byte[] joinFrame = StocFrame(
        StocPacketType.JoinGame,
        ValidHostInfoPayload());
    byte[] observerFrame = StocFrame(
        StocPacketType.TypeChange,
        PacketPayloadCodec.EncodeStocTypeChange(
            new StocTypeChangePayload(7)));
    foreach (byte[][] chunks in new[]
    {
        new[] { Concat(joinFrame, observerFrame) },
        new[] { joinFrame, observerFrame }
    })
    {
        ScriptedTransport observerTransport = new(Array.Empty<byte[]>());
        I2SessionRunner observerRunner = NewStartedRunner(observerTransport);
        observerTransport.Enqueue(chunks);
        I2PumpResult observer = observerRunner
            .PumpReadAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        if (observer.IsSuccess)
        {
            Equal(I2SessionState.JoinAccepted, observerRunner.State);
            Equal(2, observerTransport.Writes.Count);
            observer = observerRunner
                .PumpReadAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }
        False(observer.IsSuccess);
        Equal(I2ErrorCode.UnsupportedRoomTopology, observer.Error);
        Equal(2, observerTransport.Writes.Count);
    }

    (I2ErrorCode WholeError, I2ErrorCode ByteError, string WholeWrites, string ByteWrites) race =
        RunReadyNotReadyRace();
    Equal(race.WholeError, race.ByteError);
    Equal(race.WholeWrites, race.ByteWrites);

    ScriptedTransport tpTransport = new(Array.Empty<byte[]>());
    I2SessionRunner tpRunner = NewRunnerAtTpRequest(tpTransport);
    byte[] earlyGameMessage = StocFrame(
        StocPacketType.GameMsg,
        new byte[] { 0xde, 0xad });
    tpTransport.Enqueue(
        Concat(
            StocFrame(StocPacketType.SelectTp, Array.Empty<byte>()),
            earlyGameMessage));
    I2PumpResult earlyGame = tpRunner
        .PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    False(earlyGame.IsSuccess);
    Equal(I2ErrorCode.UnexpectedPacketForState, earlyGame.Error);
    Equal(I2SessionState.Failed, tpRunner.State);
    Equal(
        0,
        tpTransport.Writes.Count(frame =>
            ReadCtos(frame).Type == CtosPacketType.TpResult));

    ScriptedTransport startingTransport = new(Array.Empty<byte[]>());
    I2SessionRunner startingRunner = NewReadyRunner(startingTransport);
    True(startingRunner.RequestDuelStartAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    startingTransport.Enqueue(
        StocFrame(
            StocPacketType.HsPlayerChange,
            PacketPayloadCodec.EncodeStocHsPlayerChange(
                new StocHsPlayerChangePayload(0x1a))));
    I2PumpResult invalidated = startingRunner
        .PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    True(invalidated.IsSuccess);
    Equal(I2SessionState.Ready, startingRunner.State);
    startingTransport.Enqueue(
        StocFrame(
            StocPacketType.HsPlayerChange,
            PacketPayloadCodec.EncodeStocHsPlayerChange(
                new StocHsPlayerChangePayload(0x19))));
    True(startingRunner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    True(startingRunner.RequestDuelStartAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    Equal(I2SessionState.Starting, startingRunner.State);

    ScriptedTransport leaveTransport = new(Array.Empty<byte[]>());
    I2SessionRunner leaveRunner = NewReadyRunner(leaveTransport);
    True(leaveRunner.RequestDuelStartAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    leaveTransport.Enqueue(
        StocFrame(
            StocPacketType.HsPlayerChange,
            PacketPayloadCodec.EncodeStocHsPlayerChange(
                new StocHsPlayerChangePayload(0x1b))));
    True(leaveRunner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    Equal(I2SessionState.Ready, leaveRunner.State);
    False(leaveRunner.Lobby.SnapshotPlayers().Any(player =>
        player.Position == ClientContractV1.SecondDuelistPosition &&
        player.IsOccupied));

    ScriptedTransport observeTransport = new(Array.Empty<byte[]>());
    I2SessionRunner observeRunner = NewReadyRunner(observeTransport);
    True(observeRunner.RequestDuelStartAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    observeTransport.Enqueue(
        StocFrame(
            StocPacketType.HsPlayerChange,
            PacketPayloadCodec.EncodeStocHsPlayerChange(
                new StocHsPlayerChangePayload(0x18))));
    True(observeRunner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    Equal(I2SessionState.Ready, observeRunner.State);
    False(observeRunner.Lobby.SnapshotPlayers().Any(player =>
        player.Position == ClientContractV1.SecondDuelistPosition &&
        player.IsOccupied));
}

static (I2ErrorCode WholeError, I2ErrorCode ByteError, string WholeWrites, string ByteWrites)
    RunReadyNotReadyRace()
{
    static (I2ErrorCode Error, string Writes) Run(bool oneByte)
    {
        ScriptedTransport transport = new(Array.Empty<byte[]>());
        I2SessionRunner runner = NewReadyRunner(transport);
        byte[] notReady = StocFrame(
            StocPacketType.HsPlayerChange,
            PacketPayloadCodec.EncodeStocHsPlayerChange(
                new StocHsPlayerChangePayload(0x1a)));
        transport.Enqueue(oneByte
            ? notReady.Select(value => new[] { value }).ToArray()
            : new[] { notReady });
        True(runner.PumpReadAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult()
            .IsSuccess);
        I2Result start = runner.RequestDuelStartAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        return (
            start.Error,
            string.Join("|", transport.Writes.Select(Convert.ToHexString)));
    }

    (I2ErrorCode wholeError, string wholeWrites) = Run(false);
    (I2ErrorCode byteError, string byteWrites) = Run(true);
    return (wholeError, byteError, wholeWrites, byteWrites);
}

static I2SessionRunner NewReadyRunner(ScriptedTransport transport)
{
    I2SessionRunner runner = NewStartedRunner(transport);
    transport.Enqueue(
        Concat(
            StocFrame(StocPacketType.JoinGame, ValidHostInfoPayload()),
            StocFrame(
                StocPacketType.TypeChange,
                PacketPayloadCodec.EncodeStocTypeChange(
                    new StocTypeChangePayload(0x10))),
            StocFrame(
                StocPacketType.HsPlayerEnter,
                PacketPayloadCodec.EncodeStocHsPlayerEnter(
                    new StocHsPlayerEnterPayload("Ignis", 0))),
            StocFrame(
                StocPacketType.HsPlayerEnter,
                PacketPayloadCodec.EncodeStocHsPlayerEnter(
                    new StocHsPlayerEnterPayload("Opponent", 1)))));
    True(runner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    True(runner.SubmitDeckAsync(
            new PrevalidatedProtocolDeck(
                new uint[] { 1 },
                Array.Empty<uint>()),
            CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    True(runner.RequestReadyAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    transport.Enqueue(
        Concat(
            StocFrame(
                StocPacketType.HsPlayerChange,
                PacketPayloadCodec.EncodeStocHsPlayerChange(
                    new StocHsPlayerChangePayload(0x09))),
            StocFrame(
                StocPacketType.HsPlayerChange,
                PacketPayloadCodec.EncodeStocHsPlayerChange(
                    new StocHsPlayerChangePayload(0x19)))));
    True(runner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    Equal(I2SessionState.Ready, runner.State);
    return runner;
}

static I2SessionRunner NewRunnerAtTpRequest(ScriptedTransport transport)
{
    I2SessionRunner runner = NewReadyRunner(transport);
    True(runner.RequestDuelStartAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    transport.Enqueue(
        Concat(
            StocFrame(StocPacketType.DuelStart, Array.Empty<byte>()),
            StocFrame(StocPacketType.SelectHand, Array.Empty<byte>())));
    I2PumpResult handRequest = runner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    True(handRequest.IsSuccess);
    True(runner.SubmitChoiceAsync(
            handRequest.ChoiceRequest!.Token,
            1,
            CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    transport.Enqueue(
        StocFrame(
            StocPacketType.HandResult,
            PacketPayloadCodec.EncodeStocHandResult(
                new StocHandResultPayload(1, 3))));
    True(runner.PumpReadAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    Equal(I2SessionState.WaitingForTpRequest, runner.State);
    return runner;
}

static (I2SessionState State, string Events, string Writes, byte[] Pending)
    RunChunkedWinner(ChunkingMode mode)
{
    ScriptedTransport transport = new(Array.Empty<byte[]>());
    I2SessionRunner runner = NewStartedRunner(transport);
    PumpStage(
        runner,
        transport,
        ChunkStage(
            mode,
            StocFrame(StocPacketType.JoinGame, ValidHostInfoPayload()),
            StocFrame(
                StocPacketType.TypeChange,
                PacketPayloadCodec.EncodeStocTypeChange(
                    new StocTypeChangePayload(0x10))),
            StocFrame(
                StocPacketType.HsPlayerEnter,
                PacketPayloadCodec.EncodeStocHsPlayerEnter(
                    new StocHsPlayerEnterPayload("Ignis", 0))),
            StocFrame(
                StocPacketType.HsPlayerEnter,
                PacketPayloadCodec.EncodeStocHsPlayerEnter(
                    new StocHsPlayerEnterPayload("Opponent", 1)))),
        static current => current.State == I2SessionState.LobbyJoined &&
            current.Lobby.SnapshotPlayers().Count == 2);
    True(runner.SubmitDeckAsync(
            new PrevalidatedProtocolDeck(
                new uint[] { 0x11223344 },
                new uint[] { 0xaabbccdd }),
            CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    True(runner.RequestReadyAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    PumpStage(
        runner,
        transport,
        ChunkStage(
            mode,
            StocFrame(
                StocPacketType.HsPlayerChange,
                PacketPayloadCodec.EncodeStocHsPlayerChange(
                    new StocHsPlayerChangePayload(0x09))),
            StocFrame(
                StocPacketType.HsPlayerChange,
                PacketPayloadCodec.EncodeStocHsPlayerChange(
                    new StocHsPlayerChangePayload(0x19)))),
        static current => current.State == I2SessionState.Ready &&
            current.Lobby.SnapshotPlayers().Count == 2 &&
            current.Lobby.SnapshotPlayers().All(player => player.IsReady));
    True(runner.RequestDuelStartAsync(CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    PumpStage(
        runner,
        transport,
        ChunkStage(
            mode,
            StocFrame(StocPacketType.DuelStart, Array.Empty<byte>()),
            StocFrame(StocPacketType.SelectHand, Array.Empty<byte>())),
        static current => current.PendingChoice is not null);
    PreDuelChoiceTokenV1 handToken = runner.PendingChoice!.Token;
    True(runner.SubmitChoiceAsync(handToken, 1, CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    PumpStage(
        runner,
        transport,
        ChunkStage(
            mode,
            StocFrame(
                StocPacketType.HandResult,
                PacketPayloadCodec.EncodeStocHandResult(
                    new StocHandResultPayload(1, 3)))),
        static current => current.State == I2SessionState.WaitingForTpRequest);

    PumpStage(
        runner,
        transport,
        ChunkStage(
            mode,
            StocFrame(StocPacketType.SelectTp, Array.Empty<byte>())),
        static current => current.PendingChoice is not null);
    PreDuelChoiceTokenV1 tpToken = runner.PendingChoice!.Token;
    True(runner.SubmitChoiceAsync(tpToken, 0, CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    True(runner.RuntimeHandoff is not null);
    return (
        runner.State,
        string.Join("|", runner.Events.Select(@event => @event.ToString())),
        string.Join("|", transport.Writes.Select(Convert.ToHexString)),
        runner.RuntimeHandoff!.PendingBytes.ToArray());
}

static void PumpStage(
    I2SessionRunner runner,
    ScriptedTransport transport,
    byte[][] chunks,
    Func<I2SessionRunner, bool> completed)
{
    ArgumentNullException.ThrowIfNull(completed);
    transport.Enqueue(chunks);
    int maximumAttempts = Math.Max(chunks.Length + 1, 1);
    for (int attempt = 0; attempt < maximumAttempts; attempt++)
    {
        I2PumpResult result = runner.PumpReadAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        True(result.IsSuccess);
        if (completed(runner))
        {
            return;
        }
    }

    throw new InvalidOperationException("The scripted stage did not complete.");
}

static byte[][] ChunkStage(ChunkingMode mode, params byte[][] frames)
{
    byte[] combined = Concat(frames);
    return mode switch
    {
        ChunkingMode.AllCoalesced => new[] { combined },
        ChunkingMode.OneFrame => frames.Select(frame => frame.ToArray()).ToArray(),
        ChunkingMode.OneByte => combined
            .Select(value => new[] { value })
            .ToArray(),
        ChunkingMode.Irregular => ChunkBytes(combined, new[] { 1, 2, 5, 3, 8, 13, 21 }),
        _ => throw new InvalidOperationException("Unknown chunking mode.")
    };
}

static byte[][] ChunkBytes(byte[] bytes, int[] pattern)
{
    List<byte[]> chunks = new();
    int offset = 0;
    int patternIndex = 0;
    while (offset < bytes.Length)
    {
        int count = Math.Min(pattern[patternIndex % pattern.Length], bytes.Length - offset);
        chunks.Add(bytes.AsSpan(offset, count).ToArray());
        offset += count;
        patternIndex++;
    }

    return chunks.ToArray();
}

static PreDuelStateMachine NewJoinRequestMachine()
{
    PreDuelStateMachine machine = new();
    True(machine.BeginConnection().IsSuccess);
    True(machine.MarkTransportConnected().IsSuccess);
    True(machine.MarkPlayerInfoSent().IsSuccess);
    True(machine.MarkJoinRequestSent().IsSuccess);
    return machine;
}

static I2SessionRunner NewStartedRunner(IByteTransport transport)
{
    I2SessionRunner runner = new(transport);
    True(runner.StartAsync(
            Configuration(),
            CancellationToken.None)
        .GetAwaiter()
        .GetResult()
        .IsSuccess);
    return runner;
}

static ConnectionConfigurationV1 Configuration() =>
    new(
        "localhost",
        7911,
        "Ignis",
        7,
        RoomPasswordV1.Create("synthetic-pass"),
        TimeSpan.FromSeconds(5));

static PreDuelStateMachine NewRpsMachine()
{
    PreDuelStateMachine machine = NewReadyMachine();
    True(machine.MarkDuelStartRequested().IsSuccess);
    True(machine.ApplyPacket(
        ValidatedStoc(StocPacketType.DuelStart, Array.Empty<byte>())).IsSuccess);
    return machine;
}

static PreDuelStateMachine NewReadyMachine()
{
    PreDuelStateMachine machine = NewJoinRequestMachine();
    True(machine.ApplyPacket(
        ValidatedStoc(StocPacketType.JoinGame, ValidHostInfoPayload())).IsSuccess);
    True(machine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.TypeChange,
            PacketPayloadCodec.EncodeStocTypeChange(
                new StocTypeChangePayload(0x10)))).IsSuccess);
    True(machine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsPlayerEnter,
            PacketPayloadCodec.EncodeStocHsPlayerEnter(
                new StocHsPlayerEnterPayload("Ignis", 0)))).IsSuccess);
    True(machine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsPlayerEnter,
            PacketPayloadCodec.EncodeStocHsPlayerEnter(
                new StocHsPlayerEnterPayload("Opponent", 1)))).IsSuccess);
    True(machine.MarkDeckSubmitted().IsSuccess);
    True(machine.MarkReadyRequested().IsSuccess);
    True(machine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsPlayerChange,
            PacketPayloadCodec.EncodeStocHsPlayerChange(
                new StocHsPlayerChangePayload(0x09)))).IsSuccess);
    True(machine.ApplyPacket(
        ValidatedStoc(
            StocPacketType.HsPlayerChange,
            PacketPayloadCodec.EncodeStocHsPlayerChange(
                new StocHsPlayerChangePayload(0x19)))).IsSuccess);
    return machine;
}

static ValidatedStocPacket ValidatedStoc(
    StocPacketType type,
    byte[] payload)
{
    FrameReadResult<ValidatedStocPacket> result =
        PacketPayloadValidator.TryReadValidatedStoc(
            WireFrameCodec.EncodeStoc(type, payload));
    if (result.Status != FrameReadStatus.Success || result.Frame is null)
    {
        throw new InvalidOperationException(
            $"Expected a validated STOC packet, got {result.Status}/{result.Error}.");
    }

    return result.Frame;
}

static byte[] ValidHostInfoPayload()
{
    return PacketPayloadCodec.EncodeStocJoinGame(ValidHostInfo());
}

static HostInfoPayload ValidHostInfo() =>
    new(
            0,
            5,
            0,
            0,
            0,
            0,
            8000,
            5,
            1,
            0,
            0,
            ClientContractV1.ExpectedServerHandshake,
            new ProtocolClientVersion(41, 0, 11, 0),
            1,
            1,
            1,
            0,
            0,
            0,
            new DeckSizeLimits(40, 60),
            new DeckSizeLimits(0, 15),
            new DeckSizeLimits(0, 15));

static byte[] StocFrame(StocPacketType type, byte[] payload) =>
    WireFrameCodec.EncodeStoc(type, payload);

static CtosFrame ReadCtos(byte[] frame)
{
    FrameReadResult<CtosFrame> result = WireFrameCodec.TryReadCtos(frame);
    if (result.Status != FrameReadStatus.Success || result.Frame is null)
    {
        throw new InvalidOperationException(
            $"Expected a valid CTOS frame, got {result.Status}/{result.Error}.");
    }

    return result.Frame;
}

static byte[] Concat(params byte[][] parts)
{
    int length = parts.Sum(part => part.Length);
    byte[] result = new byte[length];
    int offset = 0;
    foreach (byte[] part in parts)
    {
        part.CopyTo(result, offset);
        offset += part.Length;
    }

    return result;
}

static void AssertConfigurationFailure(
    string host,
    int port,
    string playerName,
    RoomPasswordV1 password,
    TimeSpan timeout,
    I2ErrorCode expectedCode)
{
    ClientConfigurationException exception = AssertThrows<ClientConfigurationException>(
        () => new ConnectionConfigurationV1(
            host,
            port,
            playerName,
            7,
            password,
            timeout));
    Equal(expectedCode, exception.Code);
}

static void SequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException("Expected sequences to be equal.");
    }
}

static void BytesEqual(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException(
            $"Expected bytes {Convert.ToHexString(expected)}, got {Convert.ToHexString(actual)}.");
    }
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void False(bool value)
{
    if (value)
    {
        throw new InvalidOperationException("Expected false.");
    }
}

static void True(bool value)
{
    if (!value)
    {
        throw new InvalidOperationException("Expected true.");
    }
}

static void NotNull<T>(T? value)
    where T : class
{
    if (value is null)
    {
        throw new InvalidOperationException("Expected a non-null value.");
    }
}

static TException AssertThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException exception)
    {
        return exception;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
}

enum ChunkingMode : byte
{
    OneByte,
    OneFrame,
    AllCoalesced,
    Irregular
}
