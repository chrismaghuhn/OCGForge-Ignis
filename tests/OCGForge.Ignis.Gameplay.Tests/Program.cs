using System.Buffers.Binary;
using System.Reflection;
using OCGForge.Ignis.Client;
using OCGForge.Ignis.Gameplay;
using OCGForge.Ignis.Protocol;

var tests = new (string Name, Action Body)[]
{
    ("MSG_START establishes SelfIsPlayer0", TestStartSelfIsPlayer0),
    ("MSG_START establishes SelfIsPlayer1", TestStartSelfIsPlayer1),
    ("MSG_START exact length and no inner NeedMoreData", TestStartLength),
    ("observer and invalid roles fail closed", TestRoleRejection),
    ("duplicate and conflicting MSG_START fail closed", TestDuplicateAndConflict),
    ("perspective-dependent and unknown messages fail closed", TestUnsupportedMessages),
    ("modern loc_info is explicit little endian", TestModernLocInfo),
    ("handoff claims exactly once", TestHandoffClaimExactlyOnce),
    ("pending bytes are processed before reads", TestPendingBytesFirst),
    ("partial pending frame continues through I1", TestPartialPendingFrame),
    ("session drains pending before live transport", TestSessionPendingReadFirst),
    ("pump and dispose share lifecycle ownership", TestPumpDisposeLifecycle),
    ("outer chunking has identical semantic output", TestChunkingDeterminism),
    ("pending suffix transfers unchanged", TestPendingSuffixTransfer),
    ("failure closes transport exactly once", TestFailureCloseExactlyOnce),
    ("short inner message fails through complete outer frame", TestShortInnerMessage),
    ("malformed outer frame fails closed", TestMalformedOuterFrame),
    ("privacy values exclude control metadata", TestPrivacyBoundary),
    ("fresh decoder values are immutable by construction", TestValueOwnership),
    ("I3B query union decodes every admitted flag", TestQueryUnion),
    ("I3B query failures are strict and atomic", TestQueryFailures),
    ("I3B mirror initializes perspective and authoritative turn state", TestMirrorInitialization),
    ("I3B mirror applies movement and relations transactionally", TestMirrorMovementAndRelations),
    ("I3B face-down transitions destroy stale card facts", TestFaceDownTransition),
    ("I3B draw LP and terminal state are fail closed", TestDrawLpAndTerminal),
    ("I3B update data preserves wire query order", TestUpdateDataWireOrder),
    ("I3B stream chunking preserves mirror semantics", TestMirrorChunking)
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

static void TestStartSelfIsPlayer0()
{
    GameplayMessageDecoderV1 decoder = new();
    GameplayMessageDecodeResult result = decoder.Decode(
        new StocGameMessagePayload(CreateStartBytes(0x00)));

    True(result.IsSuccess);
    Equal(GameplayErrorCode.None, result.Error);
    NotNull(result.Message);
    NotNull(result.Perspective);
    Equal(GameplayPerspectiveKind.SelfIsPlayer0, result.Perspective!.Kind);
    Equal((byte)0x00, result.Message!.Start.PlayerType);
    Equal((byte)4, GameplayMessageV1.MessageId);
    Equal(8000u, result.Message.Start.LifePoints0);
    Equal(7000u, result.Message.Start.LifePoints1);
    Equal((ushort)40, result.Message.Start.DeckCount0);
    Equal((ushort)15, result.Message.Start.ExtraCount0);
    Equal((ushort)41, result.Message.Start.DeckCount1);
    Equal((ushort)16, result.Message.Start.ExtraCount1);
}

static void TestStartSelfIsPlayer1()
{
    GameplayMessageDecoderV1 decoder = new();
    GameplayMessageDecodeResult result = decoder.Decode(
        new StocGameMessagePayload(CreateStartBytes(0x01)));

    True(result.IsSuccess);
    Equal(GameplayPerspectiveKind.SelfIsPlayer1, result.Perspective!.Kind);
    Equal(GameplayPerspectiveKind.SelfIsPlayer1, decoder.Perspective!.Kind);
}

static void TestStartLength()
{
    byte[] valid = CreateStartBytes(0x00);
    for (int length = 0; length < valid.Length; length++)
    {
        GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(
            new StocGameMessagePayload(valid.AsSpan(0, length)));
        False(result.IsSuccess);
        Equal(GameplayErrorCode.MalformedGameMessage, result.Error);
    }

    byte[] trailing = new byte[valid.Length + 1];
    valid.CopyTo(trailing, 0);
    trailing[^1] = 0xaa;
    GameplayMessageDecodeResult withTrailing = new GameplayMessageDecoderV1().Decode(
        new StocGameMessagePayload(trailing));
    False(withTrailing.IsSuccess);
    Equal(GameplayErrorCode.MalformedGameMessage, withTrailing.Error);

    True(!Enum.GetNames<GameplayErrorCode>().Contains("NeedMoreData", StringComparer.Ordinal));
}

static void TestRoleRejection()
{
    foreach (byte observer in new byte[] { 0x10, 0x11 })
    {
        GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(
            new StocGameMessagePayload(CreateStartBytes(observer)));
        False(result.IsSuccess);
        Equal(GameplayErrorCode.UnsupportedPerspective, result.Error);
    }

    foreach (byte invalid in new byte[] { 0x02, 0xff })
    {
        GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(
            new StocGameMessagePayload(CreateStartBytes(invalid)));
        False(result.IsSuccess);
        Equal(GameplayErrorCode.InvalidPerspectiveRole, result.Error);
    }
}

static void TestDuplicateAndConflict()
{
    GameplayMessageDecoderV1 decoder = new();
    True(decoder.Decode(new StocGameMessagePayload(CreateStartBytes(0x00))).IsSuccess);

    GameplayMessageDecodeResult duplicate = decoder.Decode(
        new StocGameMessagePayload(CreateStartBytes(0x00)));
    False(duplicate.IsSuccess);
    Equal(GameplayErrorCode.DuplicatePerspective, duplicate.Error);

    GameplayMessageDecodeResult conflict = decoder.Decode(
        new StocGameMessagePayload(CreateStartBytes(0x01)));
    False(conflict.IsSuccess);
    Equal(GameplayErrorCode.ConflictingPerspective, conflict.Error);
    Equal(GameplayPerspectiveKind.SelfIsPlayer0, decoder.Perspective!.Kind);
}

static void TestUnsupportedMessages()
{
    GameplayMessageDecoderV1 decoder = new();
    GameplayMessageDecodeResult dependent = decoder.Decode(
        new StocGameMessagePayload(new byte[] { 6, 0, 0 }));
    False(dependent.IsSuccess);
    Equal(GameplayErrorCode.PerspectiveNotEstablished, dependent.Error);
    Null(decoder.Perspective);

    GameplayMessageDecodeResult tooLate = decoder.Decode(
        new StocGameMessagePayload(CreateStartBytes(0x00)));
    False(tooLate.IsSuccess);
    Equal(GameplayErrorCode.PerspectiveEstablishmentTooLate, tooLate.Error);

    GameplayMessageDecodeResult unknown = new GameplayMessageDecoderV1().Decode(
        new StocGameMessagePayload(new byte[] { 0xff }));
    False(unknown.IsSuccess);
    Equal(GameplayErrorCode.UnknownMessageId, unknown.Error);

    GameplayMessageDecodeResult unsupported = new GameplayMessageDecoderV1().Decode(
        new StocGameMessagePayload(new byte[] { 3 }));
    False(unsupported.IsSuccess);
    Equal(GameplayErrorCode.UnsupportedMessage, unsupported.Error);
}

static void TestModernLocInfo()
{
    byte[] bytes = new byte[10];
    bytes[0] = 1;
    bytes[1] = 0x80;
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2, 4), 0x11223344);
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(6, 4), 0x55667788);

    True(GameplayWirePrimitivesV1.TryDecodeModernLocInfo(
        bytes,
        out ModernLocInfoV1 value,
        out GameplayErrorCode error));
    Equal(GameplayErrorCode.None, error);
    Equal((byte)1, value.Controller);
    Equal((byte)0x80, value.Location);
    Equal(0x11223344u, value.Sequence);
    Equal(0x55667788u, value.Position);

    False(GameplayWirePrimitivesV1.TryDecodeModernLocInfo(
        bytes.AsSpan(0, 9),
        out _,
        out error));
    Equal(GameplayErrorCode.MalformedGameMessage, error);
}

static void TestHandoffClaimExactlyOnce()
{
    TestTransport transport = new(Array.Empty<byte[]>());
    GameplayHandoffOfferV1 handoff = CreateHandoff(transport, Array.Empty<byte>());

    GameplayHandoffAcquireResult first = GameplayHandoffConsumerV1.TryCreate(handoff);
    True(first.IsSuccess);
    NotNull(first.Consumer);

    GameplayHandoffAcquireResult second = GameplayHandoffConsumerV1.TryCreate(handoff);
    False(second.IsSuccess);
    Equal(GameplayErrorCode.HandoffAlreadyClaimed, second.Error);
    Null(second.Consumer);

    first.Consumer!.DisposeAsync().GetAwaiter().GetResult();
    Equal(1, transport.CloseCallCount);
}

static void TestPendingBytesFirst()
{
    byte[] frame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x00));
    TestTransport transport = new(new[] { new byte[] { 0xaa } });
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, frame));
    True(acquired.IsSuccess);

    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    True(result.IsSuccess);
    Equal(GameplayPerspectiveKind.SelfIsPlayer0, result.Perspective!.Kind);
    Equal(0, transport.ReadCallCount);
    Equal(0, transport.CloseCallCount);

    GameplaySessionV1 session = result.Session!;
    acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
    Equal(0, transport.CloseCallCount);
    session.CloseOwnedTransportAsync().GetAwaiter().GetResult();
    session.CloseOwnedTransportAsync().GetAwaiter().GetResult();
    Equal(1, transport.CloseCallCount);
}

static void TestPartialPendingFrame()
{
    byte[] frame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x00));
    const int pendingLength = 3;
    TestTransport transport = new(Split(
        frame.AsSpan(pendingLength).ToArray(),
        new[] { 1, 2, 3, 5 }));
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, frame.AsSpan(0, pendingLength)));

    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    True(result.IsSuccess);
    Equal(GameplayPerspectiveKind.SelfIsPlayer0, result.Perspective!.Kind);
    True(transport.ReadCallCount > 0);
    acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
    result.Session!.CloseOwnedTransportAsync().GetAwaiter().GetResult();
}

static void TestSessionPendingReadFirst()
{
    byte[] startFrame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x00));
    byte[] futureFrame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        new byte[] { 3 });
    byte[] combined = new byte[startFrame.Length + futureFrame.Length];
    startFrame.CopyTo(combined, 0);
    futureFrame.CopyTo(combined, startFrame.Length);

    TestTransport transport = new(new[] { combined });
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, Array.Empty<byte>()));
    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    True(result.IsSuccess);

    byte[] destination = new byte[futureFrame.Length];
    int readsBeforeSession = transport.ReadCallCount;
    int pendingCount = result.Session!.ReadAsync(
            destination,
            CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(futureFrame.Length, pendingCount);
    BytesEqual(futureFrame, destination);
    Equal(readsBeforeSession, transport.ReadCallCount);

    transport.Enqueue(new byte[] { 0xa1 });
    int liveCount = result.Session.ReadAsync(
            destination.AsMemory(0, 1),
            CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    Equal(1, liveCount);
    Equal(readsBeforeSession + 1, transport.ReadCallCount);

    acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
    result.Session.CloseOwnedTransportAsync().GetAwaiter().GetResult();
}

static void TestPumpDisposeLifecycle()
{
    byte[] frame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x00));
    const int pendingLength = 3;
    LifecycleRaceTransport transport = new(frame.AsSpan(pendingLength).ToArray());
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, frame.AsSpan(0, pendingLength)));
    True(acquired.IsSuccess);

    Task<GameplayPumpResult> pumpTask = Task.Run(async () =>
        await acquired.Consumer!.PumpAsync(CancellationToken.None));
    True(transport.ReadStarted.Wait(TimeSpan.FromSeconds(5)));

    Task disposeTask = acquired.Consumer!.DisposeAsync().AsTask();
    transport.Release();

    GameplayPumpResult result = pumpTask.GetAwaiter().GetResult();
    disposeTask.GetAwaiter().GetResult();
    True(result.IsSuccess);
    Equal(0, transport.CloseCallCount);

    result.Session!.CloseOwnedTransportAsync().GetAwaiter().GetResult();
    Equal(1, transport.CloseCallCount);
}

static void TestChunkingDeterminism()
{
    byte[] frame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x01));
    string whole = RunChunking(new[] { frame }, out _);
    string oneByte = RunChunking(
        frame.Select(value => new[] { value }).ToArray(),
        out _);
    string irregular = RunChunking(
        Split(frame, new[] { 1, 2, 5, 3, 7 }),
        out _);

    Equal(whole, oneByte);
    Equal(whole, irregular);
}

static void TestPendingSuffixTransfer()
{
    byte[] startFrame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x00));
    byte[] futureFrame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        new byte[] { 3 });
    byte[] combined = new byte[startFrame.Length + futureFrame.Length];
    startFrame.CopyTo(combined, 0);
    futureFrame.CopyTo(combined, startFrame.Length);

    TestTransport transport = new(new[] { combined });
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, Array.Empty<byte>()));
    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();

    True(result.IsSuccess);
    BytesEqual(futureFrame, result.Session!.PendingBytes.Span);
    acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
    result.Session.CloseOwnedTransportAsync().GetAwaiter().GetResult();
}

static void TestFailureCloseExactlyOnce()
{
    byte[] observerFrame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x10));
    TestTransport transport = new(Array.Empty<byte[]>());
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, observerFrame));

    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    False(result.IsSuccess);
    Equal(GameplayErrorCode.UnsupportedPerspective, result.Error);
    Equal(1, transport.CloseCallCount);
    int readsAfterFailure = transport.ReadCallCount;

    GameplayPumpResult repeated = acquired.Consumer.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    False(repeated.IsSuccess);
    Equal(readsAfterFailure, transport.ReadCallCount);
    acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
    Equal(1, transport.CloseCallCount);
}

static void TestMalformedOuterFrame()
{
    TestTransport transport = new(Array.Empty<byte[]>());
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, new byte[] { 1, 0, 0xff }));

    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    False(result.IsSuccess);
    Equal(GameplayErrorCode.MalformedOuterFrame, result.Error);
    Equal(1, transport.CloseCallCount);
}

static void TestShortInnerMessage()
{
    byte[] frame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        new byte[] { GameplayMessageV1.MessageId, 0x00 });
    TestTransport transport = new(Array.Empty<byte[]>());
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, frame));

    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    False(result.IsSuccess);
    Equal(GameplayErrorCode.MalformedGameMessage, result.Error);
    Equal(1, transport.CloseCallCount);
}

static void TestPrivacyBoundary()
{
    GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(
        new StocGameMessagePayload(CreateStartBytes(0x00)));
    True(result.IsSuccess);

    string[] forbidden =
    {
        "socket", "endpoint", "password", "credential", "pid", "thread",
        "timestamp", "wall", "pointer", "object", "runtime", "engine",
        "locator", "CoreHost", "hidden"
    };
    AssertDoesNotContainForbidden(result.ToString(), forbidden);
    AssertDoesNotContainForbidden(result.Message!.ToString(), forbidden);
    AssertDoesNotContainForbidden(result.Perspective!.ToString(), forbidden);
    False(typeof(MirrorEntityIdV1).IsPublic);
    Null(typeof(MirrorCardSnapshotV1).GetProperty(
        "EntityId",
        BindingFlags.Instance | BindingFlags.Public));
    Null(typeof(MirrorSnapshotV1).GetProperty(
        "PendingChain",
        BindingFlags.Instance | BindingFlags.Public));
    Null(typeof(MirrorSnapshotV1).GetProperty(
        "TargetRelations",
        BindingFlags.Instance | BindingFlags.Public));
}

static void TestValueOwnership()
{
    byte[] bytes = CreateStartBytes(0x00);
    StocGameMessagePayload payload = new(bytes);
    bytes[1] = 0x01;
    GameplayMessageDecodeResult result = new GameplayMessageDecoderV1().Decode(payload);
    True(result.IsSuccess);
    Equal((byte)0x00, result.Message!.Start.PlayerType);
}

static void TestQueryUnion()
{
    byte[] query = Join(
        QueryRecord(QueryFlagV1.Code, U32(0x11223344)),
        QueryRecord(QueryFlagV1.Position, U32(0x55667788)),
        QueryRecord(QueryFlagV1.Alias, U32(0x01020304)),
        QueryRecord(QueryFlagV1.Type, U32(0x05060708)),
        QueryRecord(QueryFlagV1.Level, U32(0x090a0b0c)),
        QueryRecord(QueryFlagV1.Rank, U32(0x0d0e0f10)),
        QueryRecord(QueryFlagV1.Attribute, U32(0x11121314)),
        QueryRecord(QueryFlagV1.Race, U64(0x1122334455667788)),
        QueryRecord(QueryFlagV1.Attack, I32(-100)),
        QueryRecord(QueryFlagV1.Defense, I32(2100)),
        QueryRecord(QueryFlagV1.BaseAttack, I32(1900)),
        QueryRecord(QueryFlagV1.BaseDefense, I32(1600)),
        QueryRecord(QueryFlagV1.Reason, U32(0x15161718)),
        QueryRecord(QueryFlagV1.ReasonCard, LocInfo(0, 0x10, 2, 0x1)),
        QueryRecord(QueryFlagV1.EquipCard, LocInfo(1, 0x04, 3, 0x4)),
        QueryRecord(
            QueryFlagV1.TargetCard,
            Join(U32(1), LocInfo(0, 0x04, 0, 0x1))),
        QueryRecord(
            QueryFlagV1.OverlayCard,
            Join(U32(1), U32(0x21222324))),
        QueryRecord(
            QueryFlagV1.Counters,
            Join(U32(1), U32(0x25262728))),
        QueryRecord(QueryFlagV1.Owner, new byte[] { 1 }),
        QueryRecord(QueryFlagV1.Status, U32(0x29303132)),
        QueryRecord(QueryFlagV1.IsPublic, new byte[] { 1 }),
        QueryRecord(QueryFlagV1.LScale, U32(0x33343536)),
        QueryRecord(QueryFlagV1.RScale, U32(0x37383940)),
        QueryRecord(QueryFlagV1.Link, Join(U32(2), U32(0x41424344))),
        QueryRecord(QueryFlagV1.IsHidden, new byte[] { 0 }),
        QueryRecord(QueryFlagV1.Cover, U32(0x45464748)),
        QueryEnd());

    ModernQueryDecodeResult decoded = ModernQueryDecoderV1.Decode(query);
    True(decoded.IsSuccess, decoded.Error.ToString());
    NotNull(decoded.Value);
    Equal(26, decoded.Value!.Fields.Count);
    Equal(QueryFlagV1.Code, decoded.Value.Fields[0].Flag);
    Equal(QueryFlagV1.Cover, decoded.Value.Fields[^1].Flag);
    Equal(
        0x11223344u,
        ((ModernQueryUInt32PayloadV1)decoded.Value.Fields[0].Payload).Value);
    Equal(
        -100,
        ((ModernQueryInt32PayloadV1)decoded.Value.Fields[8].Payload).Value);
    Equal(
        0x1122334455667788ul,
        ((ModernQueryUInt64PayloadV1)decoded.Value.Fields[7].Payload).Value);
    Equal(
        1,
        ((ModernQueryLocInfoVectorPayloadV1)decoded.Value.Fields[15].Payload)
            .Values.Count);
    Equal(
        0x41424344u,
        ((ModernQueryLinkPayloadV1)decoded.Value.Fields[23].Payload).LinkMarker);

    byte[] streamBody = Join(new byte[] { 0, 0 }, query, query);
    ModernQueryStreamDecodeResult stream = ModernQueryDecoderV1.DecodeStream(
        Join(U32((uint)streamBody.Length), streamBody));
    True(stream.IsSuccess, stream.Error.ToString());
    Equal(3, stream.Values.Count);
    True(stream.Values[0].IsOnFieldSkipped);
    Equal(stream.Values[1], stream.Values[2]);
}

static void TestQueryFailures()
{
    ModernQueryDecodeResult duplicateScalar = ModernQueryDecoderV1.Decode(
        Join(
            QueryRecord(QueryFlagV1.Code, U32(1)),
            QueryRecord(QueryFlagV1.Code, U32(2)),
            QueryEnd()));
    False(duplicateScalar.IsSuccess);
    Equal(GameplayErrorCode.DuplicateQueryFlag, duplicateScalar.Error);

    ModernQueryDecodeResult duplicateVector = ModernQueryDecoderV1.Decode(
        Join(
            QueryRecord(QueryFlagV1.OverlayCard, Join(U32(1), U32(3))),
            QueryRecord(QueryFlagV1.OverlayCard, Join(U32(1), U32(4))),
            QueryEnd()));
    False(duplicateVector.IsSuccess);
    Equal(GameplayErrorCode.DuplicateQueryFlag, duplicateVector.Error);

    byte[] oneQuery = Join(QueryRecord(QueryFlagV1.Code, U32(7)), QueryEnd());
    ModernQueryStreamDecodeResult repeatedAcrossQueries =
        ModernQueryDecoderV1.DecodeStream(
            Join(U32((uint)(oneQuery.Length * 2)), oneQuery, oneQuery));
    True(repeatedAcrossQueries.IsSuccess, repeatedAcrossQueries.Error.ToString());
    Equal(2, repeatedAcrossQueries.Values.Count);

    foreach (uint invalidFlag in new uint[] { 0, 3, 0x04000000 })
    {
        ModernQueryDecodeResult invalid = ModernQueryDecoderV1.Decode(
            Join(QueryRecordRaw(invalidFlag, U32(1)), QueryEnd()));
        False(invalid.IsSuccess);
        Equal(GameplayErrorCode.UnsupportedQueryFlag, invalid.Error);
    }

    ModernQueryDecodeResult shortFlag = ModernQueryDecoderV1.Decode(
        new byte[] { 8, 0, 1 });
    False(shortFlag.IsSuccess);
    Equal(GameplayErrorCode.MalformedQuery, shortFlag.Error);

    ModernQueryDecodeResult shortVector = ModernQueryDecoderV1.Decode(
        Join(
            QueryRecord(QueryFlagV1.TargetCard, U32(1)),
            QueryEnd()));
    False(shortVector.IsSuccess);
    Equal(GameplayErrorCode.QueryLengthMismatch, shortVector.Error);

    ModernQueryDecodeResult countOverflow = ModernQueryDecoderV1.Decode(
        Join(
            QueryRecord(QueryFlagV1.TargetCard, U32(uint.MaxValue)),
            QueryEnd()));
    False(countOverflow.IsSuccess);
    Equal(GameplayErrorCode.QueryCountOverflow, countOverflow.Error);

    ModernQueryDecodeResult trailing = ModernQueryDecoderV1.Decode(
        Join(
            new byte[] { 9, 0, 1, 0, 0, 0, 0x78, 0x56, 0x34, 0x12, 0xaa },
            QueryEnd()));
    False(trailing.IsSuccess);
    Equal(GameplayErrorCode.QueryLengthMismatch, trailing.Error);

    ModernQueryDecodeResult missingTerminator = ModernQueryDecoderV1.Decode(
        QueryRecord(QueryFlagV1.Code, U32(1)));
    False(missingTerminator.IsSuccess);
    Equal(GameplayErrorCode.MalformedQuery, missingTerminator.Error);
}

static void TestMirrorInitialization()
{
    GameplayMessageDecoderV1 decoder = new();
    GameplayMessageDecodeResult start = decoder.Decode(
        new StocGameMessagePayload(CreateStartBytes(0x01)));
    True(start.IsSuccess);

    MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
        start.Message!,
        start.Perspective!);
    True(created.IsSuccess, created.Error.ToString());
    PerspectiveStateMirrorV1 mirror = created.Mirror!;
    MirrorSnapshotV1 snapshot = mirror.Snapshot;

    Equal(2, snapshot.Participants.Count);
    Equal(MirrorParticipantRoleV1.Self, snapshot.Participants[0].Role);
    Equal(MirrorParticipantRoleV1.Opponent, snapshot.Participants[1].Role);
    Equal(7000u, snapshot.Participants[0].LifePoints.Value);
    Equal(8000u, snapshot.Participants[1].LifePoints.Value);
    Equal(
        41u,
        snapshot.GetParticipant(MirrorParticipantRoleV1.Self)
            .GetZone(MirrorZoneV1.MainDeck).Count.Value);
    Equal(
        16u,
        snapshot.GetParticipant(MirrorParticipantRoleV1.Self)
            .GetZone(MirrorZoneV1.ExtraDeck).Count.Value);
    Equal(0ul, snapshot.TurnCount);
    False(snapshot.TurnPlayer.IsKnown);
    False(snapshot.Phase.IsKnown);
    False(snapshot.Terminal.IsTerminal);
    Equal(
        MirrorProvenanceV1.PublicProtocolFact,
        snapshot.Participants[0].LifePoints.Provenance);

    GameplayMessageV1 turn = DecodeMessage(decoder, new byte[] { 40, 1 });
    MirrorApplyResult turnResult = mirror.Apply(turn);
    True(turnResult.IsSuccess, turnResult.Error.ToString());
    Equal(1ul, mirror.Snapshot.TurnCount);
    Equal(MirrorParticipantRoleV1.Self, mirror.Snapshot.TurnPlayer.Value);

    GameplayMessageV1 phase = DecodeMessage(decoder, new byte[] { 41, 0x04, 0x00 });
    MirrorApplyResult phaseResult = mirror.Apply(phase);
    True(phaseResult.IsSuccess, phaseResult.Error.ToString());
    Equal((ushort)4, mirror.Snapshot.Phase.Value);
    AssertDoesNotContainForbidden(
        mirror.Snapshot.ToString(),
        new[] { "socket", "endpoint", "password", "pid", "timestamp", "thread" });
}

static void TestMirrorMovementAndRelations()
{
    GameplayMessageDecoderV1 decoder = new();
    (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 activeDecoder) =
        CreateMirror(0x00);

    ModernLocInfoV1 empty = new(0, 0, 0, 0);
    ModernLocInfoV1 hand0 = new(0, 0x02, 0, 0x08);
    GameplayMessageV1 createFirst = DecodeMessage(
        activeDecoder,
        MoveMessage(0x11223344, empty, hand0, 0));
    MirrorApplyResult createResult = mirror.Apply(createFirst);
    True(createResult.IsSuccess, createResult.Error.ToString());
    Equal(1u, mirror.Snapshot.GetZone(MirrorParticipantRoleV1.Self, MirrorZoneV1.Hand).Count.Value);
    Equal(2u, mirror.Snapshot.GetZone(MirrorParticipantRoleV1.Self, MirrorZoneV1.MainDeck).Count.Value);
    string beforeInvalidPileSequence = mirror.Snapshot.ToDeterministicString();
    MirrorApplyResult invalidPileSequence = mirror.Apply(DecodeMessage(
        activeDecoder,
        MoveMessage(
            0x99999999,
            empty,
            new ModernLocInfoV1(0, 0x02, 9, 0x08),
            0)));
    False(invalidPileSequence.IsSuccess);
    Equal(GameplayErrorCode.StateCapacityExceeded, invalidPileSequence.Error);
    Equal(beforeInvalidPileSequence, mirror.Snapshot.ToDeterministicString());

    ModernLocInfoV1 monster0 = new(0, 0x04, 0, 0x01);
    MirrorApplyResult moveResult = mirror.Apply(
        DecodeMessage(activeDecoder, MoveMessage(0, hand0, monster0, 0)));
    True(moveResult.IsSuccess, moveResult.Error.ToString());

    ModernLocInfoV1 handA = new(0, 0x02, 0, 0x08);
    ModernLocInfoV1 handB = new(0, 0x02, 1, 0x08);
    True(mirror.Apply(DecodeMessage(
        activeDecoder,
        MoveMessage(0xaaaabbbb, empty, handA, 0))).IsSuccess);
    True(mirror.Apply(DecodeMessage(
        activeDecoder,
        MoveMessage(0xccccdddd, empty, handB, 0))).IsSuccess);
    True(mirror.Apply(DecodeMessage(
        activeDecoder,
        MoveMessage(
            0,
            handA,
            new ModernLocInfoV1(0, 0x10, 0, 0x04),
            0))).IsSuccess);
    MirrorCardSnapshotV1 shiftedHand = mirror.Snapshot.Cards.Single(
        card => card.Zone == MirrorZoneV1.Hand);
    Equal(0xccccddddu, shiftedHand.CardCode.Value);
    Equal(0u, shiftedHand.Sequence);
    ModernLocInfoV1 handC = new(0, 0x02, 1, 0x08);
    True(mirror.Apply(DecodeMessage(
        activeDecoder,
        MoveMessage(0xeeeeffff, empty, handC, 0))).IsSuccess);
    True(mirror.Apply(DecodeMessage(
        activeDecoder,
        MoveMessage(
            0,
            handC,
            new ModernLocInfoV1(0, 0x02, 0, 0x08),
            0))).IsSuccess);
    MirrorCardSnapshotV1 reorderedFirst = mirror.Snapshot.Cards.Single(
        card => card.Zone == MirrorZoneV1.Hand && card.Sequence == 0);
    Equal(0xeeeeffffu, reorderedFirst.CardCode.Value);

    MirrorApplyResult spellTrapSeven = mirror.Apply(DecodeMessage(
        activeDecoder,
        MoveMessage(
            0x12345678,
            empty,
            new ModernLocInfoV1(0, 0x08, 7, 0x04),
            0)));
    True(spellTrapSeven.IsSuccess, spellTrapSeven.Error.ToString());
    MirrorApplyResult monsterSix = mirror.Apply(DecodeMessage(
        activeDecoder,
        MoveMessage(
            0x23456789,
            empty,
            new ModernLocInfoV1(0, 0x04, 6, 0x04),
            0)));
    True(monsterSix.IsSuccess, monsterSix.Error.ToString());
    string beforeInvalidSpellTrap = mirror.Snapshot.ToDeterministicString();
    MirrorApplyResult invalidSpellTrap = mirror.Apply(DecodeMessage(
        activeDecoder,
        MoveMessage(
            0x87654321,
            empty,
            new ModernLocInfoV1(0, 0x08, 8, 0x04),
            0)));
    False(invalidSpellTrap.IsSuccess);
    Equal(GameplayErrorCode.StateCapacityExceeded, invalidSpellTrap.Error);
    Equal(beforeInvalidSpellTrap, mirror.Snapshot.ToDeterministicString());

    ModernQueryV1 updateQuery = DecodeQuery(
        QueryRecord(QueryFlagV1.Code, U32(0x55667788)),
        QueryRecord(QueryFlagV1.Position, U32(0x04)),
        QueryRecord(QueryFlagV1.Owner, new byte[] { 0 }),
        QueryEnd());
    MirrorApplyResult updateResult = mirror.Apply(
        DecodeMessage(activeDecoder, UpdateCardMessage(0, 0x04, 0, updateQuery)));
    True(updateResult.IsSuccess, updateResult.Error.ToString());
    MirrorCardSnapshotV1 updatedCard = mirror.Snapshot.Cards.Single(
        card => card.Zone == MirrorZoneV1.MonsterZone && card.Sequence == 0);
    Equal(0x55667788u, updatedCard.CardCode.Value);
    Equal((uint)0x04, updatedCard.Position.Value);
    Equal(MirrorParticipantRoleV1.Self, updatedCard.Owner.Value);

    ModernQueryV1 invalidOwnerQuery = DecodeQuery(
        QueryRecord(QueryFlagV1.Code, U32(0xdeadbeef)),
        QueryRecord(QueryFlagV1.Owner, new byte[] { 2 }),
        QueryEnd());
    string beforeInvalidQuery = mirror.Snapshot.ToDeterministicString();
    MirrorApplyResult invalidQuery = mirror.Apply(
        DecodeMessage(activeDecoder, UpdateCardMessage(0, 0x04, 0, invalidOwnerQuery)));
    False(invalidQuery.IsSuccess);
    Equal(GameplayErrorCode.InvalidParticipant, invalidQuery.Error);
    Equal(beforeInvalidQuery, mirror.Snapshot.ToDeterministicString());

    string beforeInvalidSequence = mirror.Snapshot.ToDeterministicString();
    MirrorApplyResult invalidSequence = mirror.Apply(
        DecodeMessage(activeDecoder, UpdateCardMessage(0, 0x04, 7, updateQuery)));
    False(invalidSequence.IsSuccess);
    Equal(GameplayErrorCode.StateCapacityExceeded, invalidSequence.Error);
    Equal(beforeInvalidSequence, mirror.Snapshot.ToDeterministicString());

    ModernLocInfoV1 monster1 = new(0, 0x04, 1, 0x04);
    MirrorApplyResult createSecondResult = mirror.Apply(
        DecodeMessage(activeDecoder, MoveMessage(0x99aabbcc, empty, monster1, 0)));
    True(createSecondResult.IsSuccess, createSecondResult.Error.ToString());
    Equal(7, mirror.Snapshot.Cards.Count);

    string beforeConflict = mirror.Snapshot.ToDeterministicString();
    MirrorApplyResult conflict = mirror.Apply(
        DecodeMessage(activeDecoder, MoveMessage(0xabcdef01, empty, monster0, 0)));
    False(conflict.IsSuccess);
    Equal(GameplayErrorCode.ConflictingSlotOccupancy, conflict.Error);
    Equal(beforeConflict, mirror.Snapshot.ToDeterministicString());

    MirrorApplyResult swapResult = mirror.Apply(
        DecodeMessage(activeDecoder, SwapMessage(monster0, monster1)));
    True(swapResult.IsSuccess, swapResult.Error.ToString());
    MirrorCardSnapshotV1 atSlot0 = mirror.Snapshot.Cards.Single(
        card => card.Zone == MirrorZoneV1.MonsterZone && card.Sequence == 0);
    Equal(0x99aabbccu, atSlot0.CardCode.Value);

    MirrorApplyResult positionResult = mirror.Apply(
        DecodeMessage(
            activeDecoder,
            PosChangeMessage(0, 0x04, 0, 0x04, 0x08)));
    True(positionResult.IsSuccess, positionResult.Error.ToString());

    string beforeSet = mirror.Snapshot.ToDeterministicString();
    MirrorApplyResult setResult = mirror.Apply(
        DecodeMessage(activeDecoder, SetMessage(0x10203040, monster0)));
    True(setResult.IsSuccess, setResult.Error.ToString());
    Equal(beforeSet, mirror.Snapshot.ToDeterministicString());

    MirrorApplyResult targetResult = mirror.Apply(
        DecodeMessage(activeDecoder, CardTargetMessage(monster0, monster1)));
    True(targetResult.IsSuccess, targetResult.Error.ToString());
    Equal(1, mirror.Snapshot.TargetRelations.Count);
    MirrorApplyResult cancelResult = mirror.Apply(
        DecodeMessage(activeDecoder, CardTargetMessage(monster0, monster1, true)));
    True(cancelResult.IsSuccess, cancelResult.Error.ToString());
    Equal(0, mirror.Snapshot.TargetRelations.Count);

    MirrorApplyResult equipResult = mirror.Apply(
        DecodeMessage(activeDecoder, EquipMessage(monster0, monster1)));
    True(equipResult.IsSuccess, equipResult.Error.ToString());
    Equal(1, mirror.Snapshot.EquipmentRelations.Count);
    ModernLocInfoV1 monster2 = new(0, 0x04, 2, 0x04);
    True(mirror.Apply(DecodeMessage(
        activeDecoder,
        MoveMessage(0x55660011, empty, monster2, 0))).IsSuccess);
    MirrorApplyResult retargetResult = mirror.Apply(
        DecodeMessage(activeDecoder, EquipMessage(monster0, monster2)));
    True(retargetResult.IsSuccess, retargetResult.Error.ToString());
    Equal(1, mirror.Snapshot.EquipmentRelations.Count);
    MirrorApplyResult unequipResult = mirror.Apply(
        DecodeMessage(activeDecoder, UnequipMessage(monster0)));
    True(unequipResult.IsSuccess, unequipResult.Error.ToString());
    Equal(0, mirror.Snapshot.EquipmentRelations.Count);

    MirrorApplyResult chaining = mirror.Apply(
        DecodeMessage(activeDecoder, ChainingMessage(monster0, 1, 0)));
    True(chaining.IsSuccess, chaining.Error.ToString());
    NotEqual(beforeSet, mirror.Snapshot.ToDeterministicString());
    True(mirror.Snapshot.PendingChain.IsKnown);
    MirrorApplyResult chained = mirror.Apply(
        DecodeMessage(activeDecoder, new byte[] { 71, 1 }));
    True(chained.IsSuccess, chained.Error.ToString());
    False(mirror.Snapshot.Chains[0].CardCode.IsKnown);
    MirrorApplyResult target = mirror.Apply(
        DecodeMessage(activeDecoder, BecomeTargetMessage(monster1)));
    True(target.IsSuccess, target.Error.ToString());
    Equal(1, mirror.Snapshot.ChainTargetRelations.Count);
    True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 72, 1 })).IsSuccess);
    True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 75, 1 })).IsSuccess);
    False(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 75, 1 })).IsSuccess);
    True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 74 })).IsSuccess);
    Equal(0, mirror.Snapshot.Chains.Count);

    MirrorApplyResult secondChaining = mirror.Apply(
        DecodeMessage(activeDecoder, ChainingMessage(monster1, 1, 0)));
    True(secondChaining.IsSuccess, secondChaining.Error.ToString());
    True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 71, 1 })).IsSuccess);
    True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 72, 1 })).IsSuccess);
    True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 76, 1 })).IsSuccess);
    True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 73, 1 })).IsSuccess);
    True(mirror.Apply(DecodeMessage(activeDecoder, new byte[] { 74 })).IsSuccess);

    True(mirror.Apply(DecodeMessage(
        activeDecoder,
        MoveMessage(
            0,
            empty,
            new ModernLocInfoV1(0, 0x84, 0, 0),
            0))).IsSuccess);
    MirrorApplyResult overlayParentSwap = mirror.Apply(
        DecodeMessage(activeDecoder, SwapMessage(monster0, monster1)));
    False(overlayParentSwap.IsSuccess);
    Equal(GameplayErrorCode.InvalidRelation, overlayParentSwap.Error);
}

static void TestDrawLpAndTerminal()
{
    (PerspectiveStateMirrorV1 knownDeckMirror, GameplayMessageDecoderV1 knownDeckDecoder) =
        CreateMirror(0x00, deckCount0: 2, deckCount1: 2);
    ModernLocInfoV1 empty = new(0, 0, 0, 0);
    True(knownDeckMirror.Apply(DecodeMessage(
        knownDeckDecoder,
        MoveMessage(
            0x01020304,
            empty,
            new ModernLocInfoV1(0, 0x01, 0, 0x01),
            0))).IsSuccess);
    string beforeKnownDeckDraw = knownDeckMirror.Snapshot.ToDeterministicString();
    MirrorApplyResult knownDeckDraw = knownDeckMirror.Apply(DecodeMessage(
        knownDeckDecoder,
        DrawMessage(0, (0x11223344u, 0x00000004u))));
    False(knownDeckDraw.IsSuccess);
    Equal(GameplayErrorCode.UnknownMirrorReference, knownDeckDraw.Error);
    Equal(beforeKnownDeckDraw, knownDeckMirror.Snapshot.ToDeterministicString());

    (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
        CreateMirror(0x00, deckCount0: 3, deckCount1: 3);

    GameplayMessageV1 draw = DecodeMessage(
        decoder,
        DrawMessage(0, (0x11223344u, 0x00000004u)));
    MirrorApplyResult drawResult = mirror.Apply(draw);
    True(drawResult.IsSuccess, drawResult.Error.ToString());
    Equal(2u, mirror.Snapshot.GetZone(MirrorParticipantRoleV1.Self, MirrorZoneV1.MainDeck).Count.Value);
    Equal(1u, mirror.Snapshot.GetZone(MirrorParticipantRoleV1.Self, MirrorZoneV1.Hand).Count.Value);
    Equal(0x11223344u, mirror.Snapshot.Cards.Single().CardCode.Value);

    GameplayMessageDecodeResult twoDraws = decoder.Decode(
        new StocGameMessagePayload(
            DrawMessage(
                0,
                (0x11223344u, 0x00000004u),
                (0xaabbccddu, 0x00000008u))));
    True(twoDraws.IsSuccess, twoDraws.Error.ToString());
    Equal(2, twoDraws.Message!.Draw!.Cards.Count);
    Equal(0x11223344u, twoDraws.Message.Draw.Cards[0].CardCode);
    Equal(0x00000004u, twoDraws.Message.Draw.Cards[0].Position);
    Equal(0xaabbccddu, twoDraws.Message.Draw.Cards[1].CardCode);
    Equal(0x00000008u, twoDraws.Message.Draw.Cards[1].Position);

    True(mirror.Apply(DecodeMessage(decoder, new byte[] { 91, 0, 0xf4, 0x01, 0, 0 })).IsSuccess);
    Equal(7500u, mirror.Snapshot.Participants[0].LifePoints.Value);
    True(mirror.Apply(DecodeMessage(decoder, new byte[] { 92, 0, 0xfa, 0, 0, 0 })).IsSuccess);
    Equal(7750u, mirror.Snapshot.Participants[0].LifePoints.Value);
    True(mirror.Apply(DecodeMessage(decoder, new byte[] { 94, 1, 0x70, 0x17, 0, 0 })).IsSuccess);
    Equal(6000u, mirror.Snapshot.Participants[1].LifePoints.Value);
    True(mirror.Apply(DecodeMessage(decoder, new byte[] { 100, 1, 0xf4, 0x01, 0, 0 })).IsSuccess);
    Equal(5500u, mirror.Snapshot.Participants[1].LifePoints.Value);

    string beforeLpOverflow = mirror.Snapshot.ToDeterministicString();
    MirrorApplyResult lpOverflow = mirror.Apply(DecodeMessage(
        decoder,
        new byte[] { 92, 0, 0xff, 0xff, 0xff, 0xff }));
    False(lpOverflow.IsSuccess);
    Equal(GameplayErrorCode.ArithmeticFailure, lpOverflow.Error);
    Equal(beforeLpOverflow, mirror.Snapshot.ToDeterministicString());

    GameplayMessageDecodeResult zeroDraw = decoder.Decode(
        new StocGameMessagePayload(DrawMessage(0)));
    False(zeroDraw.IsSuccess);
    Equal(GameplayErrorCode.InvalidDrawCount, zeroDraw.Error);

    GameplayMessageV1 terminal = DecodeMessage(decoder, new byte[] { 5, 2, 0x07 });
    MirrorApplyResult terminalResult = mirror.Apply(terminal);
    True(terminalResult.IsSuccess, terminalResult.Error.ToString());
    True(mirror.Snapshot.Terminal.IsTerminal);
    Null(mirror.Snapshot.Terminal.Winner);
    Equal((byte)0x07, mirror.Snapshot.Terminal.WinType);

    MirrorApplyResult duplicateTerminal = mirror.Apply(
        DecodeMessage(decoder, new byte[] { 5, 2, 0x07 }));
    False(duplicateTerminal.IsSuccess);
    Equal(GameplayErrorCode.TerminalStateMutation, duplicateTerminal.Error);
    MirrorApplyResult afterTerminal = mirror.Apply(
        DecodeMessage(decoder, new byte[] { 40, 0 }));
    False(afterTerminal.IsSuccess);
    Equal(GameplayErrorCode.TerminalStateMutation, afterTerminal.Error);
}

static void TestFaceDownTransition()
{
    (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
        CreateMirror(0x00);
    ModernLocInfoV1 empty = new(0, 0, 0, 0);
    ModernLocInfoV1 first = new(0, 0x04, 0, 0x04);
    ModernLocInfoV1 second = new(0, 0x04, 1, 0x04);
    True(mirror.Apply(DecodeMessage(
        decoder,
        MoveMessage(0x11223344, empty, first, 0))).IsSuccess);
    True(mirror.Apply(DecodeMessage(
        decoder,
        MoveMessage(0x55667788, empty, second, 0))).IsSuccess);
    True(mirror.Apply(DecodeMessage(
        decoder,
        CardTargetMessage(first, second))).IsSuccess);
    ModernQueryV1 query = DecodeQuery(
        QueryRecord(QueryFlagV1.Code, U32(0x11223344)),
        QueryRecord(QueryFlagV1.Type, U32(0x01)),
        QueryEnd());
    True(mirror.Apply(DecodeMessage(
        decoder,
        UpdateCardMessage(0, 0x04, 0, query))).IsSuccess);

    MirrorApplyResult faceDown = mirror.Apply(DecodeMessage(
        decoder,
        PosChangeMessage(0, 0x04, 0, 0x04, 0x08)));
    True(faceDown.IsSuccess, faceDown.Error.ToString());
    Equal(0, mirror.Snapshot.TargetRelations.Count);
    MirrorCardSnapshotV1 hidden = mirror.Snapshot.Cards.Single(
        card => card.Zone == MirrorZoneV1.MonsterZone && card.Sequence == 0);
    False(hidden.CardCode.IsKnown);
    Equal(0, hidden.QueryFields.Count);
}

static void TestUpdateDataWireOrder()
{
    (PerspectiveStateMirrorV1 mirror, GameplayMessageDecoderV1 decoder) =
        CreateMirror(0x00);
    ModernLocInfoV1 empty = new(0, 0, 0, 0);
    True(mirror.Apply(DecodeMessage(
        decoder,
        MoveMessage(0x100, empty, new ModernLocInfoV1(0, 0x02, 0, 0x08), 0))).IsSuccess);
    True(mirror.Apply(DecodeMessage(
        decoder,
        MoveMessage(0x200, empty, new ModernLocInfoV1(0, 0x02, 1, 0x08), 0))).IsSuccess);

    ModernQueryV1 orderedQuery = DecodeQuery(
        QueryRecord(QueryFlagV1.Position, U32(0x04)),
        QueryRecord(QueryFlagV1.Code, U32(0xabcdef01)),
        QueryEnd());
    byte[] emptyQuery = QueryEnd();
    GameplayMessageV1 updateData = DecodeMessage(
        decoder,
        UpdateDataMessage(0, 0x02, Join(orderedQuery.RawBytes.ToArray(), emptyQuery)));
    MirrorApplyResult result = mirror.Apply(updateData);
    True(result.IsSuccess, result.Error.ToString());
    MirrorCardSnapshotV1 first = mirror.Snapshot.Cards.Single(
        card => card.Zone == MirrorZoneV1.Hand && card.Sequence == 0);
    Equal(0xabcdef01u, first.CardCode.Value);
    Equal(2, first.QueryFields.Count);
    Equal(QueryFlagV1.Position, first.QueryFields[0].Flag);
    Equal(QueryFlagV1.Code, first.QueryFields[1].Flag);
}

static void TestMirrorChunking()
{
    byte[] startFrame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        CreateStartBytes(0x00));
    byte[] moveFrame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        MoveMessage(
            0x11223344,
            new ModernLocInfoV1(0, 0, 0, 0),
            new ModernLocInfoV1(0, 0x02, 0, 0x08),
            0));
    byte[] turnFrame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        new byte[] { 40, 0 });
    byte[] phaseFrame = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        new byte[] { 41, 4, 0 });
    byte[] transcript = Join(startFrame, moveFrame, turnFrame, phaseFrame);

    string whole = RunMirrorTranscript(new[] { transcript });
    string oneByte = RunMirrorTranscript(
        transcript.Select(value => new[] { value }).ToArray());
    string irregular = RunMirrorTranscript(
        Split(transcript, new[] { 1, 2, 7, 3, 11 }));
    Equal(whole, oneByte);
    Equal(whole, irregular);
}

static string RunChunking(byte[][] chunks, out TestTransport transport)
{
    transport = new TestTransport(chunks);
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, Array.Empty<byte>()));
    True(acquired.IsSuccess);
    GameplayPumpResult result = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    True(result.IsSuccess);
    string semantic = $"{result.Perspective!.Kind}|{result.Message!.Start.PlayerType}|" +
        $"{result.Message.Start.LifePoints0}|{result.Message.Start.LifePoints1}|" +
        $"{result.Session!.PendingBytes.Length}";
    acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
    result.Session.CloseOwnedTransportAsync().GetAwaiter().GetResult();
    return semantic;
}

static GameplayHandoffOfferV1 CreateHandoff(
    IGameplayTransportV1 transport,
    ReadOnlySpan<byte> pendingBytes)
{
    PreDuelSessionV1 publicSession = new(
        default,
        0,
        false,
        PreDuelOutcome.RpsLoss,
        Array.Empty<I2Event>());
    return new GameplayHandoffOfferV1(transport, publicSession, pendingBytes);
}

static byte[] CreateStartBytes(
    byte playerType,
    ushort deckCount0 = 40,
    ushort extraCount0 = 15,
    ushort deckCount1 = 41,
    ushort extraCount1 = 16)
{
    byte[] bytes = new byte[18];
    bytes[0] = 4;
    bytes[1] = playerType;
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2, 4), 8000);
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(6, 4), 7000);
    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(10, 2), deckCount0);
    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12, 2), extraCount0);
    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(14, 2), deckCount1);
    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(16, 2), extraCount1);
    return bytes;
}

static GameplayMessageV1 DecodeMessage(
    GameplayMessageDecoderV1 decoder,
    byte[] bytes)
{
    GameplayMessageDecodeResult result = decoder.Decode(
        new StocGameMessagePayload(bytes));
    True(result.IsSuccess, result.Error.ToString());
    NotNull(result.Message);
    return result.Message!;
}

static ModernQueryV1 DecodeQuery(params byte[][] records)
{
    ModernQueryDecodeResult result = ModernQueryDecoderV1.Decode(
        Join(records));
    True(result.IsSuccess, result.Error.ToString());
    NotNull(result.Value);
    return result.Value!;
}

static (PerspectiveStateMirrorV1 Mirror, GameplayMessageDecoderV1 Decoder)
    CreateMirror(
        byte playerType,
        ushort deckCount0 = 2,
        ushort extraCount0 = 1,
        ushort deckCount1 = 2,
        ushort extraCount1 = 1)
{
    GameplayMessageDecoderV1 decoder = new();
    GameplayMessageDecodeResult start = decoder.Decode(
        new StocGameMessagePayload(
            CreateStartBytes(
                playerType,
                deckCount0,
                extraCount0,
                deckCount1,
                extraCount1)));
    True(start.IsSuccess, start.Error.ToString());
    MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
        start.Message!,
        start.Perspective!);
    True(created.IsSuccess, created.Error.ToString());
    return (created.Mirror!, decoder);
}

static byte[] QueryRecord(QueryFlagV1 flag, byte[] payload) =>
    QueryRecordRaw((uint)flag, payload);

static byte[] QueryRecordRaw(uint flag, byte[] payload)
{
    byte[] record = new byte[2 + 4 + payload.Length];
    BinaryPrimitives.WriteUInt16LittleEndian(
        record.AsSpan(0, 2), checked((ushort)(4 + payload.Length)));
    BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(2, 4), flag);
    payload.CopyTo(record, 6);
    return record;
}

static byte[] QueryEnd() => new byte[] { 4, 0, 0, 0, 0, 0x80 };

static byte[] U32(uint value)
{
    byte[] bytes = new byte[4];
    BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
    return bytes;
}

static byte[] I32(int value)
{
    byte[] bytes = new byte[4];
    BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
    return bytes;
}

static byte[] U64(ulong value)
{
    byte[] bytes = new byte[8];
    BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
    return bytes;
}

static byte[] LocInfo(byte controller, byte location, uint sequence, uint position)
{
    byte[] bytes = new byte[10];
    bytes[0] = controller;
    bytes[1] = location;
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2, 4), sequence);
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(6, 4), position);
    return bytes;
}

static byte[] UpdateCardMessage(
    byte player,
    byte location,
    byte sequence,
    ModernQueryV1 query) =>
    Join(
        new byte[] { 7, player, location, sequence },
        query.RawBytes.ToArray());

static byte[] UpdateDataMessage(
    byte player,
    byte location,
    byte[] queryBody) =>
    Join(new byte[] { 6, player, location }, U32((uint)queryBody.Length), queryBody);

static byte[] MoveMessage(
    uint cardCode,
    ModernLocInfoV1 previous,
    ModernLocInfoV1 current,
    uint reason) =>
    Join(
        new byte[] { 50 },
        U32(cardCode),
        LocInfo(previous.Controller, previous.Location, previous.Sequence, previous.Position),
        LocInfo(current.Controller, current.Location, current.Sequence, current.Position),
        U32(reason));

static byte[] PosChangeMessage(
    byte controller,
    byte location,
    byte sequence,
    byte previousPosition,
    byte currentPosition) =>
    Join(
        new byte[] { 53 },
        U32(0),
        new byte[] { controller, location, sequence, previousPosition, currentPosition });

static byte[] SetMessage(uint cardCode, ModernLocInfoV1 location) =>
    Join(
        new byte[] { 54 },
        U32(cardCode),
        LocInfo(location.Controller, location.Location, location.Sequence, location.Position));

static byte[] SwapMessage(ModernLocInfoV1 first, ModernLocInfoV1 second) =>
    Join(
        new byte[] { 55 },
        U32(0),
        LocInfo(first.Controller, first.Location, first.Sequence, first.Position),
        U32(0),
        LocInfo(second.Controller, second.Location, second.Sequence, second.Position));

static byte[] CardTargetMessage(
    ModernLocInfoV1 source,
    ModernLocInfoV1 target,
    bool cancel = false) =>
    Join(
        new byte[] { (byte)(cancel ? 97 : 96) },
        LocInfo(source.Controller, source.Location, source.Sequence, source.Position),
        LocInfo(target.Controller, target.Location, target.Sequence, target.Position));

static byte[] EquipMessage(ModernLocInfoV1 card, ModernLocInfoV1 target) =>
    Join(
        new byte[] { 93 },
        LocInfo(card.Controller, card.Location, card.Sequence, card.Position),
        LocInfo(target.Controller, target.Location, target.Sequence, target.Position));

static byte[] UnequipMessage(ModernLocInfoV1 card) =>
    Join(
        new byte[] { 95 },
        LocInfo(card.Controller, card.Location, card.Sequence, card.Position));

static byte[] ChainingMessage(
    ModernLocInfoV1 card,
    uint chainSize,
    uint cardCode = 0x11223344) =>
    Join(
        new byte[] { 70 },
        U32(cardCode),
        LocInfo(card.Controller, card.Location, card.Sequence, card.Position),
        new byte[] { card.Controller, card.Location },
        U32(card.Sequence),
        U64(0x0102030405060708),
        U32(chainSize));

static byte[] BecomeTargetMessage(params ModernLocInfoV1[] targets)
{
    List<byte[]> parts = new() { new byte[] { 83 }, U32((uint)targets.Length) };
    parts.AddRange(
        targets.Select(target =>
            LocInfo(target.Controller, target.Location, target.Sequence, target.Position)));
    return Join(parts.ToArray());
}

static byte[] DrawMessage(
    byte player,
    params (uint Code, uint Position)[] cards)
{
    List<byte[]> parts = new() { new byte[] { 90, player }, U32((uint)cards.Length) };
    parts.AddRange(cards.Select(card => Join(U32(card.Code), U32(card.Position))));
    return Join(parts.ToArray());
}

static string RunMirrorTranscript(byte[][] chunks)
{
    TestTransport transport = new(chunks);
    GameplayHandoffAcquireResult acquired = GameplayHandoffConsumerV1.TryCreate(
        CreateHandoff(transport, Array.Empty<byte>()));
    True(acquired.IsSuccess);
    GameplayPumpResult start = acquired.Consumer!.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    True(start.IsSuccess, start.Error.ToString());
    MirrorCreateResult created = PerspectiveStateMirrorV1.TryCreate(
        start.Message!,
        start.Perspective!);
    True(created.IsSuccess, created.Error.ToString());

    GameplayMirrorSessionV1 session = new(start.Session!, created.Mirror!);
    GameplayMirrorPumpResult move = session.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    True(move.IsSuccess, move.Error.ToString());
    GameplayMirrorPumpResult turn = session.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    True(turn.IsSuccess, turn.Error.ToString());
    GameplayMirrorPumpResult phase = session.PumpAsync(
        CancellationToken.None).GetAwaiter().GetResult();
    True(phase.IsSuccess, phase.Error.ToString());
    string result = session.Mirror.Snapshot.ToDeterministicString();
    session.DisposeAsync().GetAwaiter().GetResult();
    acquired.Consumer.DisposeAsync().GetAwaiter().GetResult();
    return result;
}

static byte[] Join(params byte[][] parts)
{
    int length = 0;
    foreach (byte[] part in parts)
    {
        length = checked(length + part.Length);
    }

    byte[] result = new byte[length];
    int offset = 0;
    foreach (byte[] part in parts)
    {
        part.CopyTo(result, offset);
        offset += part.Length;
    }

    return result;
}

static byte[][] Split(byte[] bytes, int[] sizes)
{
    List<byte[]> chunks = new();
    int offset = 0;
    int sizeIndex = 0;
    while (offset < bytes.Length)
    {
        int count = Math.Min(sizes[sizeIndex % sizes.Length], bytes.Length - offset);
        chunks.Add(bytes.AsSpan(offset, count).ToArray());
        offset += count;
        sizeIndex++;
    }

    return chunks.ToArray();
}

static void AssertDoesNotContainForbidden(string? value, IEnumerable<string> forbidden)
{
    string text = value ?? string.Empty;
    foreach (string term in forbidden)
    {
        False(text.Contains(term, StringComparison.OrdinalIgnoreCase),
            $"forbidden value '{term}' appeared");
    }
}

static void True(bool condition, string message = "assertion was false")
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void False(bool condition, string message = "assertion was true") =>
    True(!condition, message);

static void Null(object? value) => True(value is null, "expected null");

static void NotNull(object? value) => True(value is not null, "expected non-null");

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"expected {expected}; actual {actual}");
    }
}

static void NotEqual<T>(T first, T second)
{
    if (EqualityComparer<T>.Default.Equals(first, second))
    {
        throw new InvalidOperationException($"values unexpectedly equal: {first}");
    }
}

static void BytesEqual(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
{
    True(expected.SequenceEqual(actual),
        $"expected {Convert.ToHexString(expected)}; actual {Convert.ToHexString(actual)}");
}

internal sealed class TestTransport : IByteTransport, IGameplayTransportV1
{
    private readonly Queue<byte[]> chunks;
    private byte[]? current;
    private int currentOffset;
    private bool closed;

    internal TestTransport(IEnumerable<byte[]> chunks)
    {
        this.chunks = new Queue<byte[]>(chunks.Select(chunk => chunk.ToArray()));
    }

    internal int ReadCallCount { get; private set; }

    internal int CloseCallCount { get; private set; }

    internal void Enqueue(params byte[][] additionalChunks)
    {
        foreach (byte[] chunk in additionalChunks)
        {
            chunks.Enqueue(chunk.ToArray());
        }
    }

    public ValueTask ConnectAsync(
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public ValueTask<int> ReadAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadCallCount++;
        while (current is null || currentOffset == current.Length)
        {
            if (chunks.Count == 0)
            {
                return ValueTask.FromResult(0);
            }

            current = chunks.Dequeue();
            currentOffset = 0;
        }

        int count = Math.Min(destination.Length, current.Length - currentOffset);
        current.AsMemory(currentOffset, count).CopyTo(destination);
        currentOffset += count;
        return ValueTask.FromResult(count);
    }

    public ValueTask WriteAsync(
        ReadOnlyMemory<byte> source,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("I3A never writes transport bytes");
    }

    public ValueTask CloseAsync()
    {
        if (!closed)
        {
            closed = true;
            CloseCallCount++;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => CloseAsync();
}

internal sealed class LifecycleRaceTransport : IGameplayTransportV1
{
    private readonly byte[] remainder;
    private readonly TaskCompletionSource<bool> readStarted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> release =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int closeCallCount;
    private bool closed;

    internal LifecycleRaceTransport(byte[] remainder)
    {
        this.remainder = remainder.ToArray();
    }

    internal Task ReadStarted => readStarted.Task;

    internal int CloseCallCount => closeCallCount;

    internal void Release() => release.TrySetResult(true);

    public async ValueTask<int> ReadAsync(
        Memory<byte> destination,
        CancellationToken cancellationToken)
    {
        readStarted.TrySetResult(true);
        await release.Task.WaitAsync(cancellationToken);
        remainder.CopyTo(destination);
        return remainder.Length;
    }

    public ValueTask CloseAsync()
    {
        if (!closed)
        {
            closed = true;
            Interlocked.Increment(ref closeCallCount);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => CloseAsync();
}
