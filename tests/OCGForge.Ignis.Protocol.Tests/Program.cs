using System.Buffers.Binary;

using OCGForge.Ignis.Protocol;

var tests = new (string Name, Action Body)[]
{
    ("contract constants", TestContractConstants),
    ("direction-specific packet identities", TestPacketIdentities),
    ("frame encoding and decoding", TestFrameEncodingAndDecoding),
    ("frame status and malformed headers", TestFrameStatuses),
    ("maximum frame and outgoing capacity", TestFrameCapacity),
    ("fragmentation at every byte boundary", TestFragmentation),
    ("coalesced frames and exact consumption", TestCoalescing),
    ("end-of-stream classification", TestEndOfStream),
    ("fixed UTF-16 strings", TestFixedUtf16Strings),
    ("fixed UTF-16 tail semantics", TestFixedUtf16TailSemantics),
    ("padding is non-semantic", TestPaddingIsNonSemantic),
    ("typed payload golden vectors", TestTypedPayloads),
    ("version recognition", TestVersionRecognition),
    ("negative typed payloads", TestNegativePayloads),
    ("unsupported outgoing packet types", TestUnsupportedOutgoingTypes),
    ("repeated encode/decode determinism", TestDeterminism),
    ("type-aware packet validation surface", TestTypeAwareValidation),
    ("discriminated error payloads", TestErrorPayloads),
    ("DeckError semantic projection", TestDeckErrorSemanticProjection),
    ("validated V1 version compatibility", TestValidatedVersionCompatibility)
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

static void TestContractConstants()
{
    Equal("ocgforge-ignis.protocol.wire.v1", ProtocolContractV1.Id);
    Equal(2, ProtocolContractV1.LengthPrefixSize);
    Equal(1, ProtocolContractV1.PacketTypeSize);
    Equal(65535, ProtocolContractV1.MaxPacketLength);
    Equal(65534, ProtocolContractV1.MaxPayloadLength);
    Equal((ushort)0x1354, ProtocolContractV1.ExpectedProVersion);
    Equal((byte)41, ProtocolContractV1.ExpectedEdoproVersionMajor);
    Equal((byte)0, ProtocolContractV1.ExpectedEdoproVersionMinor);
    Equal((byte)2, ProtocolContractV1.ExpectedEdoproVersionPatch);
    Equal((byte)11, ProtocolContractV1.ExpectedOcgVersionMajor);
    Equal((byte)0, ProtocolContractV1.ExpectedOcgVersionMinor);
    Equal(
        new ProtocolClientVersion(41, 0, 11, 0),
        ProtocolContractV1.ExpectedClientVersion);
}

static void TestPacketIdentities()
{
    var ctos = new (CtosPacketType Type, byte Value)[]
    {
        (CtosPacketType.Response, 0x01),
        (CtosPacketType.UpdateDeck, 0x02),
        (CtosPacketType.HandResult, 0x03),
        (CtosPacketType.TpResult, 0x04),
        (CtosPacketType.PlayerInfo, 0x10),
        (CtosPacketType.JoinGame, 0x12),
        (CtosPacketType.LeaveGame, 0x13),
        (CtosPacketType.Surrender, 0x14),
        (CtosPacketType.TimeConfirm, 0x15),
        (CtosPacketType.HsReady, 0x22),
        (CtosPacketType.HsNotReady, 0x23),
        (CtosPacketType.HsStart, 0x25)
    };

    foreach ((CtosPacketType type, byte value) in ctos)
    {
        Equal(value, (byte)type);
        Equal(PacketTypeDisposition.Supported, PacketTypeCatalog.ClassifyCtos(value));
    }

    var stoc = new (StocPacketType Type, byte Value)[]
    {
        (StocPacketType.GameMsg, 0x01),
        (StocPacketType.ErrorMsg, 0x02),
        (StocPacketType.SelectHand, 0x03),
        (StocPacketType.SelectTp, 0x04),
        (StocPacketType.HandResult, 0x05),
        (StocPacketType.TpResult, 0x06),
        (StocPacketType.JoinGame, 0x12),
        (StocPacketType.TypeChange, 0x13),
        (StocPacketType.LeaveGame, 0x14),
        (StocPacketType.DuelStart, 0x15),
        (StocPacketType.DuelEnd, 0x16),
        (StocPacketType.TimeLimit, 0x18),
        (StocPacketType.HsPlayerEnter, 0x20),
        (StocPacketType.HsPlayerChange, 0x21),
        (StocPacketType.HsWatchChange, 0x22)
    };

    foreach ((StocPacketType type, byte value) in stoc)
    {
        Equal(value, (byte)type);
        Equal(PacketTypeDisposition.Supported, PacketTypeCatalog.ClassifyStoc(value));
    }

    Equal(
        PacketTypeDisposition.ExplicitlyUnsupported,
        PacketTypeCatalog.ClassifyStoc((byte)StocPacketType.Catchup));
    Equal(
        PacketTypeDisposition.ExplicitlyUnsupported,
        PacketTypeCatalog.ClassifyStoc((byte)StocPacketType.Rematch));
    Equal(
        PacketTypeDisposition.ExplicitlyUnsupported,
        PacketTypeCatalog.ClassifyStoc((byte)StocPacketType.WaitingRematch));
    Equal(PacketTypeDisposition.Unknown, PacketTypeCatalog.ClassifyCtos(0x11));
    Equal(PacketTypeDisposition.Unknown, PacketTypeCatalog.ClassifyStoc(0x11));
    Equal(PacketTypeDisposition.Unknown, PacketTypeCatalog.ClassifyStoc(0x99));
}

static void TestFrameEncodingAndDecoding()
{
    byte[] empty = WireFrameCodec.EncodeCtos(
        CtosPacketType.Response,
        ReadOnlySpan<byte>.Empty);
    BytesEqual(Hex("01 00 01"), empty);

    byte[] payload = Hex("de ad be");
    byte[] encoded = WireFrameCodec.EncodeStoc(StocPacketType.GameMsg, payload);
    BytesEqual(Hex("04 00 01 de ad be"), encoded);

    FrameReadResult<StocFrame> decoded = WireFrameCodec.TryReadStoc(encoded);
    Equal(FrameReadStatus.Success, decoded.Status);
    Equal(encoded.Length, decoded.ConsumedBytes);
    Equal(StocPacketType.GameMsg, decoded.Frame!.Type);
    BytesEqual(payload, decoded.Frame.Payload.Span);
}

static void TestFrameStatuses()
{
    AssertNeedMore(WireFrameCodec.TryReadCtos(ReadOnlySpan<byte>.Empty));
    AssertNeedMore(WireFrameCodec.TryReadCtos(new byte[] { 0x01 }));
    AssertNeedMore(WireFrameCodec.TryReadCtos(new byte[] { 0x01, 0x00 }));

    FrameReadResult<CtosFrame> zeroLength =
        WireFrameCodec.TryReadCtos(new byte[] { 0x00, 0x00 });
    Equal(FrameReadStatus.Invalid, zeroLength.Status);
    Equal(ProtocolErrorCode.InvalidPacketLength, zeroLength.Error);
    Equal(0, zeroLength.ConsumedBytes);
    Equal<CtosFrame?>(null, zeroLength.Frame);

    FrameReadResult<CtosFrame> unknown =
        WireFrameCodec.TryReadCtos(new byte[] { 0x01, 0x00, 0x11 });
    Equal(FrameReadStatus.Invalid, unknown.Status);
    Equal(ProtocolErrorCode.UnknownPacketType, unknown.Error);

    FrameReadResult<StocFrame> unsupported =
        WireFrameCodec.TryReadStoc(new byte[] { 0x01, 0x00, 0xf0 });
    Equal(FrameReadStatus.Invalid, unsupported.Status);
    Equal(ProtocolErrorCode.UnsupportedPacketType, unsupported.Error);

    byte[] frame = WireFrameCodec.EncodeCtos(
        CtosPacketType.Response,
        Hex("42"));
    byte[] withTrailing = Concat(frame, Hex("99"));
    FrameReadResult<CtosFrame> first = WireFrameCodec.TryReadCtos(withTrailing);
    Equal(FrameReadStatus.Success, first.Status);
    Equal(frame.Length, first.ConsumedBytes);
    BytesEqual(Hex("42"), first.Frame!.Payload.Span);
}

static void TestFrameCapacity()
{
    byte[] maximumPayload = new byte[ProtocolContractV1.MaxPayloadLength];
    maximumPayload[0] = 0x11;
    maximumPayload[^1] = 0xee;
    byte[] maximumFrame = WireFrameCodec.EncodeCtos(
        CtosPacketType.Response,
        maximumPayload);
    Equal(65537, maximumFrame.Length);
    BytesEqual(Hex("ff ff 01"), maximumFrame.AsSpan(0, 3));
    Equal(0x11, maximumFrame[3]);
    Equal(0xee, maximumFrame[^1]);

    FrameReadResult<CtosFrame> decoded = WireFrameCodec.TryReadCtos(maximumFrame);
    Equal(FrameReadStatus.Success, decoded.Status);
    Equal(maximumFrame.Length, decoded.ConsumedBytes);
    Equal(ProtocolContractV1.MaxPayloadLength, decoded.Frame!.Payload.Length);

    byte[] oversized = new byte[ProtocolContractV1.MaxPayloadLength + 1];
    ProtocolCodecException exception = AssertThrows<ProtocolCodecException>(
        () => WireFrameCodec.EncodeCtos(CtosPacketType.Response, oversized));
    Equal(ProtocolErrorCode.OversizedPacket, exception.Code);
}

static void TestFragmentation()
{
    byte[] ctosPlayerInfo = WireFrameCodec.EncodeCtos(
        CtosPacketType.PlayerInfo,
        PacketPayloadCodec.EncodePlayerInfo(new CtosPlayerInfoPayload("Ignis")));
    byte[] ctosOpaque = WireFrameCodec.EncodeCtos(
        CtosPacketType.Response,
        Hex("00 10 ff"));
    byte[] stocOpaque = WireFrameCodec.EncodeStoc(
        StocPacketType.GameMsg,
        Hex("aa bb cc dd"));

    CheckEveryCtosSplit(ctosPlayerInfo);
    CheckEveryCtosSplit(ctosOpaque);
    CheckEveryStocSplit(stocOpaque);
}

static void CheckEveryCtosSplit(byte[] frame)
{
    for (int split = 0; split <= frame.Length; split++)
    {
        var accumulated = new List<byte>();
        if (split > 0)
        {
            accumulated.AddRange(frame.AsSpan(0, split).ToArray());
            FrameReadResult<CtosFrame> partial =
                WireFrameCodec.TryReadCtos(accumulated.ToArray());
            if (split < frame.Length)
            {
                Equal(FrameReadStatus.NeedMoreData, partial.Status);
                Equal(0, partial.ConsumedBytes);
            }
            else
            {
                AssertCtosSuccess(partial, frame.Length);
                continue;
            }
        }

        accumulated.AddRange(frame.AsSpan(split).ToArray());
        AssertCtosSuccess(
            WireFrameCodec.TryReadCtos(accumulated.ToArray()),
            frame.Length);
    }
}

static void CheckEveryStocSplit(byte[] frame)
{
    for (int split = 0; split <= frame.Length; split++)
    {
        var accumulated = new List<byte>();
        if (split > 0)
        {
            accumulated.AddRange(frame.AsSpan(0, split).ToArray());
            FrameReadResult<StocFrame> partial =
                WireFrameCodec.TryReadStoc(accumulated.ToArray());
            if (split < frame.Length)
            {
                Equal(FrameReadStatus.NeedMoreData, partial.Status);
                Equal(0, partial.ConsumedBytes);
            }
            else
            {
                AssertStocSuccess(partial, frame.Length);
                continue;
            }
        }

        accumulated.AddRange(frame.AsSpan(split).ToArray());
        AssertStocSuccess(
            WireFrameCodec.TryReadStoc(accumulated.ToArray()),
            frame.Length);
    }
}

static void AssertCtosSuccess(FrameReadResult<CtosFrame> result, int length)
{
    Equal(FrameReadStatus.Success, result.Status);
    Equal(length, result.ConsumedBytes);
    NotNull(result.Frame);
}

static void AssertStocSuccess(FrameReadResult<StocFrame> result, int length)
{
    Equal(FrameReadStatus.Success, result.Status);
    Equal(length, result.ConsumedBytes);
    NotNull(result.Frame);
}

static void TestCoalescing()
{
    byte[] first = WireFrameCodec.EncodeCtos(
        CtosPacketType.Response,
        Hex("01"));
    byte[] second = WireFrameCodec.EncodeCtos(
        CtosPacketType.HandResult,
        Hex("02"));
    byte[] third = WireFrameCodec.EncodeCtos(
        CtosPacketType.TpResult,
        Hex("03"));
    byte[] coalesced = Concat(first, second, third);

    FrameReadResult<CtosFrame> readFirst = WireFrameCodec.TryReadCtos(coalesced);
    AssertCtosSuccess(readFirst, first.Length);
    Equal(CtosPacketType.Response, readFirst.Frame!.Type);

    ReadOnlySpan<byte> afterFirst = coalesced.AsSpan(readFirst.ConsumedBytes);
    FrameReadResult<CtosFrame> readSecond = WireFrameCodec.TryReadCtos(afterFirst);
    AssertCtosSuccess(readSecond, second.Length);
    Equal(CtosPacketType.HandResult, readSecond.Frame!.Type);

    ReadOnlySpan<byte> afterSecond = afterFirst[readSecond.ConsumedBytes..];
    FrameReadResult<CtosFrame> readThird = WireFrameCodec.TryReadCtos(afterSecond);
    AssertCtosSuccess(readThird, third.Length);
    Equal(CtosPacketType.TpResult, readThird.Frame!.Type);

    byte[] partial = Concat(first, second, third.AsSpan(0, 2).ToArray());
    FrameReadResult<CtosFrame> partialThird =
        WireFrameCodec.TryReadCtos(partial.AsSpan(first.Length + second.Length));
    Equal(FrameReadStatus.NeedMoreData, partialThird.Status);
    Equal(0, partialThird.ConsumedBytes);
}

static void TestEndOfStream()
{
    Equal(
        ProtocolErrorCode.None,
        WireFrameCodec.EndOfStream(ReadOnlySpan<byte>.Empty));
    Equal(
        ProtocolErrorCode.TruncatedFrame,
        WireFrameCodec.EndOfStream(new byte[] { 0x01 }));
    Equal(
        ProtocolErrorCode.TruncatedFrame,
        WireFrameCodec.EndOfStream(new byte[] { 0x05, 0x00, 0x01 }));
}

static void TestFixedUtf16Strings()
{
    byte[] encoded = FixedUtf16String.Encode("AΩ", 4);
    BytesEqual(Hex("41 00 a9 03 00 00 00 00"), encoded);
    PayloadDecodeResult<string> decoded = FixedUtf16String.Decode(encoded, 4);
    True(decoded.IsSuccess);
    Equal("AΩ", decoded.Value);

    AssertThrows<ProtocolCodecException>(
        () => FixedUtf16String.Encode(new string('x', 20), 20));
    ProtocolCodecException embeddedNull = AssertThrows<ProtocolCodecException>(
        () => FixedUtf16String.Encode("a\0b", 20));
    Equal(ProtocolErrorCode.InvalidFixedString, embeddedNull.Code);
    AssertThrows<ProtocolCodecException>(
        () => FixedUtf16String.Encode("\ud800", 20));
    ProtocolCodecException invalidWidth = AssertThrows<ProtocolCodecException>(
        () => FixedUtf16String.Encode(string.Empty, int.MaxValue));
    Equal(ProtocolErrorCode.IntegerOverflow, invalidWidth.Code);
    ProtocolCodecException oversizedWidth = AssertThrows<ProtocolCodecException>(
        () => FixedUtf16String.Encode(
            string.Empty,
            ProtocolContractV1.MaxPayloadLength / 2 + 1));
    Equal(ProtocolErrorCode.OversizedPacket, oversizedWidth.Code);

    byte[] nonZeroAfterTerminator = new byte[8];
    nonZeroAfterTerminator[0] = (byte)'A';
    nonZeroAfterTerminator[4] = (byte)'B';
    PayloadDecodeResult<string> malformed =
        FixedUtf16String.Decode(nonZeroAfterTerminator, 4);
    True(malformed.IsSuccess);
    Equal("A", malformed.Value);
}

static void TestFixedUtf16TailSemantics()
{
    byte[] validTail = new byte[8];
    validTail[0] = (byte)'A';
    validTail[4] = (byte)'B';
    PayloadDecodeResult<string> valid =
        FixedUtf16String.Decode(validTail, 4);
    True(valid.IsSuccess);
    Equal("A", valid.Value);

    byte[] invalidSurrogateTail = new byte[8];
    invalidSurrogateTail[0] = (byte)'A';
    invalidSurrogateTail[5] = 0xd8;
    PayloadDecodeResult<string> invalidSurrogate =
        FixedUtf16String.Decode(invalidSurrogateTail, 4);
    True(invalidSurrogate.IsSuccess);
    Equal("A", invalidSurrogate.Value);

    byte[] noTerminator = new byte[8];
    noTerminator[0] = (byte)'A';
    noTerminator[2] = (byte)'B';
    noTerminator[4] = (byte)'C';
    noTerminator[6] = (byte)'D';
    Equal(
        ProtocolErrorCode.InvalidFixedString,
        FixedUtf16String.Decode(noTerminator, 4).Error);
}

static void TestPaddingIsNonSemantic()
{
    byte[] timeA = Hex("02 00 34 12");
    byte[] timeB = Hex("02 7f 34 12");
    PayloadDecodeResult<StocTimeLimitPayload> decodedTimeA =
        PacketPayloadCodec.DecodeStocTimeLimit(timeA);
    PayloadDecodeResult<StocTimeLimitPayload> decodedTimeB =
        PacketPayloadCodec.DecodeStocTimeLimit(timeB);
    True(decodedTimeA.IsSuccess);
    True(decodedTimeB.IsSuccess);
    Equal(decodedTimeA.Value, decodedTimeB.Value);
    BytesEqual(timeA, PacketPayloadCodec.EncodeStocTimeLimit(decodedTimeB.Value));

    byte[] joinA = Hex(
        "54 13 00 00 44 33 22 11 " +
        "72 00 6f 00 6f 00 6d 00 2d 00 73 00 65 00 63 00 72 00 65 00 74 00 " +
        "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
        "29 00 0b 00");
    byte[] joinB = joinA.ToArray();
    joinB[2] = 0xaa;
    joinB[3] = 0xbb;
    PayloadDecodeResult<CtosJoinGamePayload> decodedJoinA =
        PacketPayloadCodec.DecodeJoinGame(joinA);
    PayloadDecodeResult<CtosJoinGamePayload> decodedJoinB =
        PacketPayloadCodec.DecodeJoinGame(joinB);
    True(decodedJoinA.IsSuccess);
    True(decodedJoinB.IsSuccess);
    Equal(decodedJoinA.Value, decodedJoinB.Value);
    BytesEqual(joinA, PacketPayloadCodec.EncodeJoinGame(decodedJoinB.Value));

    byte[] errorA = Hex(
        "02 00 00 00 05 00 00 00 " +
        "00 00 00 00 00 00 00 00 00 00 00 00 " +
        "44 33 22 11");
    byte[] errorB = errorA.ToArray();
    errorB[1] = 0xaa;
    errorB[2] = 0xbb;
    errorB[3] = 0xcc;
    PayloadDecodeResult<StocErrorPayload> decodedErrorA =
        PacketPayloadCodec.DecodeStocErrorMessage(errorA);
    PayloadDecodeResult<StocErrorPayload> decodedErrorB =
        PacketPayloadCodec.DecodeStocErrorMessage(errorB);
    True(decodedErrorA.IsSuccess);
    True(decodedErrorB.IsSuccess);
    Equal(decodedErrorA.Value, decodedErrorB.Value);
    BytesEqual(
        errorA,
        PacketPayloadCodec.EncodeStocErrorMessage(decodedErrorB.Value));

    byte[] hostA = HostGoldenPayload();
    byte[] hostB = hostA.ToArray();
    hostB[9] = 0xaa;
    hostB[10] = 0xbb;
    hostB[11] = 0xcc;
    hostB[66] = 0xdd;
    hostB[67] = 0xee;
    PayloadDecodeResult<HostInfoPayload> decodedHostA =
        PacketPayloadCodec.DecodeStocJoinGame(hostA);
    PayloadDecodeResult<HostInfoPayload> decodedHostB =
        PacketPayloadCodec.DecodeStocJoinGame(hostB);
    True(decodedHostA.IsSuccess);
    True(decodedHostB.IsSuccess);
    Equal(decodedHostA.Value, decodedHostB.Value);
    BytesEqual(
        hostA,
        PacketPayloadCodec.EncodeStocJoinGame(decodedHostB.Value));

    byte[] playerA = new byte[42];
    playerA[0] = (byte)'P';
    playerA[2] = (byte)'1';
    playerA[40] = 0x03;
    byte[] playerB = playerA.ToArray();
    playerB[41] = 0xcc;
    PayloadDecodeResult<StocHsPlayerEnterPayload> decodedPlayerA =
        PacketPayloadCodec.DecodeStocHsPlayerEnter(playerA);
    PayloadDecodeResult<StocHsPlayerEnterPayload> decodedPlayerB =
        PacketPayloadCodec.DecodeStocHsPlayerEnter(playerB);
    True(decodedPlayerA.IsSuccess);
    True(decodedPlayerB.IsSuccess);
    Equal(decodedPlayerA.Value, decodedPlayerB.Value);
    BytesEqual(
        playerA,
        PacketPayloadCodec.EncodeStocHsPlayerEnter(decodedPlayerB.Value));
}

static void TestTypedPayloads()
{
    CtosPlayerInfoPayload player = new("Ignis");
    byte[] playerBytes = PacketPayloadCodec.EncodePlayerInfo(player);
    byte[] playerExpected = new byte[40];
    playerExpected[0] = 0x49;
    playerExpected[2] = 0x67;
    playerExpected[4] = 0x6e;
    playerExpected[6] = 0x69;
    playerExpected[8] = 0x73;
    BytesEqual(playerExpected, playerBytes);
    PayloadDecodeResult<CtosPlayerInfoPayload> playerDecoded =
        PacketPayloadCodec.DecodePlayerInfo(playerBytes);
    True(playerDecoded.IsSuccess);
    Equal(player, playerDecoded.Value);
    BytesEqual(playerBytes, PacketPayloadCodec.EncodePlayerInfo(playerDecoded.Value));

    CtosJoinGamePayload join = new(
        0x1354,
        0x11223344,
        "room-secret",
        new ProtocolClientVersion(41, 0, 11, 0));
    byte[] joinBytes = PacketPayloadCodec.EncodeJoinGame(join);
    BytesEqual(
        Hex(
            "54 13 00 00 44 33 22 11 " +
            "72 00 6f 00 6f 00 6d 00 2d 00 73 00 65 00 63 00 72 00 65 00 74 00 " +
            "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
            "29 00 0b 00"),
        joinBytes);
    PayloadDecodeResult<CtosJoinGamePayload> joinDecoded =
        PacketPayloadCodec.DecodeJoinGame(joinBytes);
    True(joinDecoded.IsSuccess);
    Equal(join, joinDecoded.Value);
    BytesEqual(joinBytes, PacketPayloadCodec.EncodeJoinGame(joinDecoded.Value));

    CtosUpdateDeckPayload deck = new(
        new uint[] { 0x11223344, 0x55667788 },
        new uint[] { 0xaabbccdd });
    byte[] deckBytes = PacketPayloadCodec.EncodeUpdateDeck(deck);
    BytesEqual(
        Hex(
            "02 00 00 00 01 00 00 00 " +
            "44 33 22 11 88 77 66 55 dd cc bb aa"),
        deckBytes);
    PayloadDecodeResult<CtosUpdateDeckPayload> deckDecoded =
        PacketPayloadCodec.DecodeUpdateDeck(deckBytes);
    True(deckDecoded.IsSuccess);
    SequenceEqual(deck.MainAndExtraCards, deckDecoded.Value.MainAndExtraCards);
    SequenceEqual(deck.SideCards, deckDecoded.Value.SideCards);
    BytesEqual(
        deckBytes,
        PacketPayloadCodec.EncodeUpdateDeck(deckDecoded.Value));

    CtosHandResultPayload hand = new(0x7f);
    BytesEqual(Hex("7f"), PacketPayloadCodec.EncodeCtosHandResult(hand));
    Equal(
        hand,
        PacketPayloadCodec.DecodeCtosHandResult(Hex("7f")).Value);

    CtosTpResultPayload tp = new(0x01);
    BytesEqual(Hex("01"), PacketPayloadCodec.EncodeCtosTpResult(tp));
    Equal(tp, PacketPayloadCodec.DecodeCtosTpResult(Hex("01")).Value);

    StocErrorPayload error = new DeckErrorCardCodePayload(
        DeckErrorCode.CardCount,
        0x11223344);
    byte[] errorBytes = PacketPayloadCodec.EncodeStocErrorMessage(error);
    BytesEqual(
        Hex(
            "02 00 00 00 05 00 00 00 " +
            "00 00 00 00 00 00 00 00 00 00 00 00 " +
            "44 33 22 11"),
        errorBytes);
    PayloadDecodeResult<StocErrorPayload> errorDecoded =
        PacketPayloadCodec.DecodeStocErrorMessage(errorBytes);
    True(errorDecoded.IsSuccess);
    Equal(error, errorDecoded.Value);
    BytesEqual(
        errorBytes,
        PacketPayloadCodec.EncodeStocErrorMessage(errorDecoded.Value));

    HostInfoPayload host = new(
        0x01020304,
        0x05,
        0x06,
        0x07,
        0x08,
        0x09,
        0x0d0e0f10,
        0x11,
        0x12,
        0x1314,
        0x15161718,
        0x191a1b1c,
        new ProtocolClientVersion(0x1d, 0x1e, 0x1f, 0x20),
        0x21222324,
        0x25262728,
        0x292a2b2c,
        0x2d2e2f30,
        0x31323334,
        0x3536,
        new DeckSizeLimits(0x3738, 0x393a),
        new DeckSizeLimits(0x3b3c, 0x3d3e),
        new DeckSizeLimits(0x3f40, 0x4142));
    byte[] hostBytes = PacketPayloadCodec.EncodeStocJoinGame(host);
    BytesEqual(
        Hex(
            "04 03 02 01 05 06 07 08 09 00 00 00 " +
            "10 0f 0e 0d 11 12 14 13 18 17 16 15 " +
            "1c 1b 1a 19 1d 1e 1f 20 " +
            "24 23 22 21 28 27 26 25 2c 2b 2a 29 " +
            "30 2f 2e 2d 34 33 32 31 36 35 " +
            "38 37 3a 39 3c 3b 3e 3d 40 3f 42 41 00 00"),
        hostBytes);
    PayloadDecodeResult<HostInfoPayload> hostDecoded =
        PacketPayloadCodec.DecodeStocJoinGame(hostBytes);
    True(hostDecoded.IsSuccess);
    Equal(host, hostDecoded.Value);
    BytesEqual(hostBytes, PacketPayloadCodec.EncodeStocJoinGame(hostDecoded.Value));

    StocHandResultPayload stocHand = new(0x01, 0x02);
    BytesEqual(Hex("01 02"), PacketPayloadCodec.EncodeStocHandResult(stocHand));
    Equal(
        stocHand,
        PacketPayloadCodec.DecodeStocHandResult(Hex("01 02")).Value);

    StocTypeChangePayload typeChange = new(0xa5);
    BytesEqual(Hex("a5"), PacketPayloadCodec.EncodeStocTypeChange(typeChange));
    Equal(
        typeChange,
        PacketPayloadCodec.DecodeStocTypeChange(Hex("a5")).Value);

    StocTimeLimitPayload time = new(0x02, 0x1234);
    BytesEqual(
        Hex("02 00 34 12"),
        PacketPayloadCodec.EncodeStocTimeLimit(time));
    Equal(
        time,
        PacketPayloadCodec.DecodeStocTimeLimit(Hex("02 00 34 12")).Value);

    StocHsPlayerEnterPayload playerEnter = new("P1", 0x03);
    byte[] playerEnterBytes = PacketPayloadCodec.EncodeStocHsPlayerEnter(playerEnter);
    byte[] playerEnterExpected = new byte[42];
    playerEnterExpected[0] = 0x50;
    playerEnterExpected[2] = 0x31;
    playerEnterExpected[40] = 0x03;
    BytesEqual(playerEnterExpected, playerEnterBytes);
    Equal(
        playerEnter,
        PacketPayloadCodec.DecodeStocHsPlayerEnter(playerEnterBytes).Value);

    StocHsPlayerChangePayload playerChange = new(0xa1);
    BytesEqual(
        Hex("a1"),
        PacketPayloadCodec.EncodeStocHsPlayerChange(playerChange));
    Equal(
        playerChange,
        PacketPayloadCodec.DecodeStocHsPlayerChange(Hex("a1")).Value);

    StocHsWatchChangePayload watch = new(0x1234);
    BytesEqual(
        Hex("34 12"),
        PacketPayloadCodec.EncodeStocHsWatchChange(watch));
    Equal(
        watch,
        PacketPayloadCodec.DecodeStocHsWatchChange(Hex("34 12")).Value);

    CtosResponsePayload response = new(Hex("00 ff 10"));
    BytesEqual(
        Hex("00 ff 10"),
        PacketPayloadCodec.EncodeCtosResponse(response));
    BytesEqual(
        Hex("00 ff 10"),
        PacketPayloadCodec.DecodeCtosResponse(Hex("00 ff 10")).Value.Bytes.Span);

    StocGameMessagePayload gameMessage = new(Hex("12 34 56"));
    BytesEqual(
        Hex("12 34 56"),
        PacketPayloadCodec.EncodeStocGameMessage(gameMessage));
    BytesEqual(
        Hex("12 34 56"),
        PacketPayloadCodec.DecodeStocGameMessage(Hex("12 34 56")).Value.Bytes.Span);

    FrameReadResult<CtosFrame> emptyFrame = WireFrameCodec.TryReadCtos(
        WireFrameCodec.EncodeCtos(CtosPacketType.HsReady, ReadOnlySpan<byte>.Empty));
    AssertCtosSuccess(emptyFrame, 3);
}

static void TestVersionRecognition()
{
    ProtocolClientVersion expected = new(41, 0, 11, 0);
    Equal(
        ProtocolErrorCode.None,
        VersionRecognition.ValidateV1(0x1354, expected));
    Equal(
        ProtocolErrorCode.UnsupportedVersion,
        VersionRecognition.ValidateV1(0x1355, expected));
    Equal(
        ProtocolErrorCode.UnsupportedVersion,
        VersionRecognition.ValidateV1(0x1354, new ProtocolClientVersion(40, 0, 11, 0)));
}

static void TestNegativePayloads()
{
    Equal(
        ProtocolErrorCode.PayloadLengthMismatch,
        PacketPayloadCodec.DecodePlayerInfo(new byte[39]).Error);
    Equal(
        ProtocolErrorCode.TrailingPayloadBytes,
        PacketPayloadCodec.DecodePlayerInfo(new byte[41]).Error);
    Equal(
        ProtocolErrorCode.PayloadLengthMismatch,
        PacketPayloadCodec.DecodeJoinGame(new byte[51]).Error);
    Equal(
        ProtocolErrorCode.TrailingPayloadBytes,
        PacketPayloadCodec.DecodeJoinGame(new byte[53]).Error);
    Equal(
        ProtocolErrorCode.PayloadLengthMismatch,
        PacketPayloadCodec.DecodeCtosHandResult(ReadOnlySpan<byte>.Empty).Error);
    Equal(
        ProtocolErrorCode.TrailingPayloadBytes,
        PacketPayloadCodec.DecodeStocTimeLimit(new byte[5]).Error);
    Equal(
        ProtocolErrorCode.PayloadLengthMismatch,
        PacketPayloadCodec.DecodeStocErrorMessage(
            Hex("01 00 00 00 01 00 00")).Error);
    Equal(
        ProtocolErrorCode.UnknownErrorType,
        PacketPayloadCodec.DecodeStocErrorMessage(
            Hex("99 00 00 00 00 00 00 00")).Error);
    byte[] updateCountMismatch = Hex("01 00 00 00 00 00 00 00");
    Equal(
        ProtocolErrorCode.PayloadLengthMismatch,
        PacketPayloadCodec.DecodeUpdateDeck(updateCountMismatch).Error);
    byte[] updateTrailing = Hex("00 00 00 00 00 00 00 00 aa");
    Equal(
        ProtocolErrorCode.TrailingPayloadBytes,
        PacketPayloadCodec.DecodeUpdateDeck(updateTrailing).Error);
    Equal(
        ProtocolErrorCode.IntegerOverflow,
        PacketPayloadCodec.DecodeUpdateDeck(
            Hex("ff ff ff ff 00 00 00 00")).Error);

    byte[] stringTrailing = new byte[40];
    for (int offset = 0; offset < stringTrailing.Length; offset += 2)
    {
        stringTrailing[offset] = (byte)'A';
    }
    Equal(
        ProtocolErrorCode.InvalidFixedString,
        PacketPayloadCodec.DecodePlayerInfo(stringTrailing).Error);
}

static void TestUnsupportedOutgoingTypes()
{
    ProtocolCodecException unsupported = AssertThrows<ProtocolCodecException>(
        () => WireFrameCodec.EncodeStoc(
            StocPacketType.Catchup,
            ReadOnlySpan<byte>.Empty));
    Equal(ProtocolErrorCode.UnsupportedPacketType, unsupported.Code);

    ProtocolCodecException unknown = AssertThrows<ProtocolCodecException>(
        () => WireFrameCodec.EncodeCtos(
            (CtosPacketType)0x11,
            ReadOnlySpan<byte>.Empty));
    Equal(ProtocolErrorCode.UnknownPacketType, unknown.Code);
}

static void TestDeterminism()
{
    byte[] payload = Hex("00 10 ff aa");
    byte[] expected = WireFrameCodec.EncodeCtos(
        CtosPacketType.Response,
        payload);

    for (int iteration = 0; iteration < 100; iteration++)
    {
        BytesEqual(
            expected,
            WireFrameCodec.EncodeCtos(CtosPacketType.Response, payload));
        FrameReadResult<CtosFrame> result = WireFrameCodec.TryReadCtos(expected);
        AssertCtosSuccess(result, expected.Length);
        BytesEqual(payload, result.Frame!.Payload.Span);
    }
}

static void TestTypeAwareValidation()
{
    byte[] playerInfo = PacketPayloadCodec.EncodePlayerInfo(
        new CtosPlayerInfoPayload("Ignis"));
    byte[] joinGame = PacketPayloadCodec.EncodeJoinGame(
        new CtosJoinGamePayload(
            0x1354,
            0x11223344,
            "room-secret",
            new ProtocolClientVersion(41, 0, 11, 0)));
    byte[] updateDeck = PacketPayloadCodec.EncodeUpdateDeck(
        new CtosUpdateDeckPayload(new uint[] { 1 }, Array.Empty<uint>()));

    var ctosCases = new (CtosPacketType Type, byte[] Payload, PayloadContractKind Contract)[]
    {
        (CtosPacketType.Response, Hex("aa"), PayloadContractKind.Opaque),
        (CtosPacketType.UpdateDeck, updateDeck, PayloadContractKind.ExactTypedLayout),
        (CtosPacketType.HandResult, Hex("01"), PayloadContractKind.ExactTypedLayout),
        (CtosPacketType.TpResult, Hex("01"), PayloadContractKind.ExactTypedLayout),
        (CtosPacketType.PlayerInfo, playerInfo, PayloadContractKind.ExactTypedLayout),
        (CtosPacketType.JoinGame, joinGame, PayloadContractKind.ExactTypedLayout),
        (CtosPacketType.LeaveGame, Array.Empty<byte>(), PayloadContractKind.ExactEmpty),
        (CtosPacketType.Surrender, Array.Empty<byte>(), PayloadContractKind.ExactEmpty),
        (CtosPacketType.TimeConfirm, Array.Empty<byte>(), PayloadContractKind.ExactEmpty),
        (CtosPacketType.HsReady, Array.Empty<byte>(), PayloadContractKind.ExactEmpty),
        (CtosPacketType.HsNotReady, Array.Empty<byte>(), PayloadContractKind.ExactEmpty),
        (CtosPacketType.HsStart, Array.Empty<byte>(), PayloadContractKind.ExactEmpty)
    };
    Equal(12, ctosCases.Length);
    foreach ((CtosPacketType type, byte[] payload, PayloadContractKind contract) in ctosCases)
    {
        byte[] frame = WireFrameCodec.EncodeCtos(type, payload);
        FrameReadResult<ValidatedCtosPacket> result =
            PacketPayloadValidator.TryReadValidatedCtos(frame);
        AssertValidatedCtos(result, frame.Length, type, contract);
    }

    foreach (CtosPacketType type in new[]
    {
        CtosPacketType.LeaveGame,
        CtosPacketType.Surrender,
        CtosPacketType.TimeConfirm,
        CtosPacketType.HsReady,
        CtosPacketType.HsNotReady,
        CtosPacketType.HsStart
    })
    {
        FrameReadResult<ValidatedCtosPacket> invalid =
            PacketPayloadValidator.TryReadValidatedCtos(
                WireFrameCodec.EncodeCtos(type, Hex("ff")));
        Equal(FrameReadStatus.Invalid, invalid.Status);
        Equal(ProtocolErrorCode.TrailingPayloadBytes, invalid.Error);
    }

    byte[] error = PacketPayloadCodec.EncodeStocErrorMessage(
        new DeckErrorCardCodePayload(
            DeckErrorCode.CardCount,
            0x11223344));
    byte[] time = PacketPayloadCodec.EncodeStocTimeLimit(
        new StocTimeLimitPayload(0x02, 0x1234));
    byte[] playerEnter = PacketPayloadCodec.EncodeStocHsPlayerEnter(
        new StocHsPlayerEnterPayload("P1", 0x03));
    byte[] watch = PacketPayloadCodec.EncodeStocHsWatchChange(
        new StocHsWatchChangePayload(0x1234));

    var stocCases = new (StocPacketType Type, byte[] Payload, PayloadContractKind Contract)[]
    {
        (StocPacketType.GameMsg, Hex("aa"), PayloadContractKind.Opaque),
        (StocPacketType.ErrorMsg, error, PayloadContractKind.ExactTypedLayout),
        (StocPacketType.SelectHand, Array.Empty<byte>(), PayloadContractKind.ExactEmpty),
        (StocPacketType.SelectTp, Array.Empty<byte>(), PayloadContractKind.ExactEmpty),
        (StocPacketType.HandResult, Hex("01 02"), PayloadContractKind.ExactTypedLayout),
        (StocPacketType.TpResult, Array.Empty<byte>(), PayloadContractKind.ExactEmpty),
        (StocPacketType.JoinGame, HostV1GoldenPayload(), PayloadContractKind.ExactTypedLayout),
        (StocPacketType.TypeChange, Hex("a5"), PayloadContractKind.ExactTypedLayout),
        (StocPacketType.LeaveGame, Array.Empty<byte>(), PayloadContractKind.ExactEmpty),
        (StocPacketType.DuelStart, Array.Empty<byte>(), PayloadContractKind.ExactEmpty),
        (StocPacketType.DuelEnd, Array.Empty<byte>(), PayloadContractKind.ExactEmpty),
        (StocPacketType.TimeLimit, time, PayloadContractKind.ExactTypedLayout),
        (StocPacketType.HsPlayerEnter, playerEnter, PayloadContractKind.ExactTypedLayout),
        (StocPacketType.HsPlayerChange, Hex("a1"), PayloadContractKind.ExactTypedLayout),
        (StocPacketType.HsWatchChange, watch, PayloadContractKind.ExactTypedLayout)
    };
    Equal(15, stocCases.Length);
    foreach ((StocPacketType type, byte[] payload, PayloadContractKind contract) in stocCases)
    {
        byte[] frame = WireFrameCodec.EncodeStoc(type, payload);
        FrameReadResult<ValidatedStocPacket> result =
            PacketPayloadValidator.TryReadValidatedStoc(frame);
        AssertValidatedStoc(result, frame.Length, type, contract);
    }

    foreach (StocPacketType type in new[]
    {
        StocPacketType.SelectHand,
        StocPacketType.SelectTp,
        StocPacketType.TpResult,
        StocPacketType.LeaveGame,
        StocPacketType.DuelStart,
        StocPacketType.DuelEnd
    })
    {
        FrameReadResult<ValidatedStocPacket> invalid =
            PacketPayloadValidator.TryReadValidatedStoc(
                WireFrameCodec.EncodeStoc(type, Hex("ff")));
        Equal(FrameReadStatus.Invalid, invalid.Status);
        Equal(ProtocolErrorCode.TrailingPayloadBytes, invalid.Error);
    }

    FrameReadResult<ValidatedStocPacket> unknownError =
        PacketPayloadValidator.TryReadValidatedStoc(
            WireFrameCodec.EncodeStoc(
                StocPacketType.ErrorMsg,
                Hex("99 00 00 00 00 00 00 00")));
    Equal(FrameReadStatus.Invalid, unknownError.Status);
    Equal(ProtocolErrorCode.UnknownErrorType, unknownError.Error);
}

static void TestErrorPayloads()
{
    var cases = new (StocErrorPayload Payload, string Expected)[]
    {
        (
            new JoinErrorPayload(JoinErrorCode.Password),
            "01 00 00 00 01 00 00 00"),
        (
            new DeckErrorCardCodePayload(DeckErrorCode.CardCount, 0x11223344),
            "02 00 00 00 05 00 00 00 " +
            "00 00 00 00 00 00 00 00 00 00 00 00 " +
            "44 33 22 11"),
        (
            new DeckErrorCountPayload(DeckErrorCode.MainCount, 6, 40, 60),
            "02 00 00 00 06 00 00 00 " +
            "06 00 00 00 28 00 00 00 3c 00 00 00 " +
            "00 00 00 00"),
        (
            new DeckErrorTypeOnlyPayload(DeckErrorCode.ExtraCount),
            "02 00 00 00 07 00 00 00 " +
            "00 00 00 00 00 00 00 00 00 00 00 00 " +
            "00 00 00 00"),
        (
            new SideErrorPayload(0x11223344),
            "03 00 00 00 44 33 22 11"),
        (
            new LegacyVersionErrorPayload(0x11223344),
            "04 00 00 00 44 33 22 11"),
        (
            new VersionError2Payload(new ProtocolClientVersion(41, 0, 11, 0)),
            "05 00 00 00 29 00 0b 00")
    };

    foreach ((StocErrorPayload payload, string expected) in cases)
    {
        byte[] encoded = PacketPayloadCodec.EncodeStocErrorMessage(payload);
        BytesEqual(Hex(expected), encoded);
        PayloadDecodeResult<StocErrorPayload> decoded =
            PacketPayloadCodec.DecodeStocErrorMessage(encoded);
        True(decoded.IsSuccess);
        Equal(payload, decoded.Value);
        BytesEqual(encoded, PacketPayloadCodec.EncodeStocErrorMessage(decoded.Value));
    }

    var malformed = new (byte[] Payload, ProtocolErrorCode Error)[]
    {
        (Hex("01 00 00 00 01 00 00"), ProtocolErrorCode.PayloadLengthMismatch),
        (Hex("01 00 00 00 01 00 00 00 00"), ProtocolErrorCode.TrailingPayloadBytes),
        (Hex("02 00 00 00 05 00 00 00"), ProtocolErrorCode.PayloadLengthMismatch),
        (
            Hex(
                "02 00 00 00 05 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 44 33 22 11 aa"),
            ProtocolErrorCode.TrailingPayloadBytes),
        (Hex("03 00 00 00 44 33 22"), ProtocolErrorCode.PayloadLengthMismatch),
        (Hex("03 00 00 00 44 33 22 11 00"), ProtocolErrorCode.TrailingPayloadBytes),
        (Hex("04 00 00 00 44 33 22"), ProtocolErrorCode.PayloadLengthMismatch),
        (Hex("04 00 00 00 44 33 22 11 00"), ProtocolErrorCode.TrailingPayloadBytes),
        (Hex("05 00 00 00 29 00 0b"), ProtocolErrorCode.PayloadLengthMismatch),
        (Hex("05 00 00 00 29 00 0b 00 00"), ProtocolErrorCode.TrailingPayloadBytes)
    };

    foreach ((byte[] payload, ProtocolErrorCode error) in malformed)
    {
        Equal(error, PacketPayloadCodec.DecodeStocErrorMessage(payload).Error);
    }

    Equal(
        ProtocolErrorCode.UnknownErrorType,
        PacketPayloadCodec.DecodeStocErrorMessage(
            Hex("99 00 00 00 00 00 00 00")).Error);

    Equal(
        ProtocolErrorCode.UnknownErrorCode,
        PacketPayloadCodec.DecodeStocErrorMessage(
            Hex("01 00 00 00 ff ff ff ff")).Error);
    Equal(
        ProtocolErrorCode.UnknownErrorCode,
        PacketPayloadCodec.DecodeStocErrorMessage(
            Hex(
                "02 00 00 00 ff ff ff ff 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 44 33 22 11")).Error);

    Equal(
        ProtocolErrorCode.UnknownErrorCode,
        PacketPayloadCodec.DecodeStocErrorMessage(
            DeckErrorRaw(DeckErrorCode.None)).Error);
    ProtocolCodecException none = AssertThrows<ProtocolCodecException>(
        () => PacketPayloadCodec.EncodeStocErrorMessage(
            new DeckErrorTypeOnlyPayload(DeckErrorCode.None)));
    Equal(ProtocolErrorCode.UnknownErrorCode, none.Code);

    FrameReadResult<ValidatedStocPacket> malformedDeck =
        PacketPayloadValidator.TryReadValidatedStoc(
            WireFrameCodec.EncodeStoc(
                StocPacketType.ErrorMsg,
                Hex("02 00 00 00 05 00 00 00")));
    Equal(FrameReadStatus.Invalid, malformedDeck.Status);
    Equal(ProtocolErrorCode.PayloadLengthMismatch, malformedDeck.Error);
}

static void TestDeckErrorSemanticProjection()
{
    foreach (DeckErrorCode error in new[]
    {
        DeckErrorCode.Lflist,
        DeckErrorCode.OcgOnly,
        DeckErrorCode.TcgOnly,
        DeckErrorCode.UnknownCard,
        DeckErrorCode.CardCount,
        DeckErrorCode.UnofficialCard
    })
    {
        byte[] rawA = DeckErrorRaw(error, 6, 40, 60, 0x11223344);
        byte[] rawB = rawA.ToArray();
        for (int offset = 8; offset < 20; offset++)
        {
            rawB[offset] = (byte)(0xa0 + offset);
        }

        DeckErrorCardCodePayload cardA =
            DecodeDeckError<DeckErrorCardCodePayload>(rawA);
        DeckErrorCardCodePayload cardB =
            DecodeDeckError<DeckErrorCardCodePayload>(rawB);
        Equal(cardA, cardB);
        Equal(cardA.GetHashCode(), cardB.GetHashCode());
        BytesEqual(
            DeckErrorRaw(error, 0, 0, 0, 0x11223344),
            PacketPayloadCodec.EncodeStocErrorMessage(cardB));
    }

    foreach (DeckErrorCode error in new[]
    {
        DeckErrorCode.MainCount,
        DeckErrorCode.SideCount
    })
    {
        byte[] rawA = DeckErrorRaw(error, 6, 40, 60, 0x11223344);
        byte[] rawB = DeckErrorRaw(error, 6, 40, 60, 0xaabbccdd);
        DeckErrorCountPayload countA =
            DecodeDeckError<DeckErrorCountPayload>(rawA);
        DeckErrorCountPayload countB =
            DecodeDeckError<DeckErrorCountPayload>(rawB);
        Equal(countA, countB);
        Equal(countA.GetHashCode(), countB.GetHashCode());
        BytesEqual(
            DeckErrorRaw(error, 6, 40, 60, 0),
            PacketPayloadCodec.EncodeStocErrorMessage(countB));
    }

    foreach (DeckErrorCode error in new[]
    {
        DeckErrorCode.ExtraCount,
        DeckErrorCode.ForbiddenType,
        DeckErrorCode.InvalidSize,
        DeckErrorCode.TooManyLegends,
        DeckErrorCode.TooManySkills
    })
    {
        byte[] rawA = DeckErrorRaw(error, 6, 40, 60, 0x11223344);
        byte[] rawB = DeckErrorRaw(error, 1, 2, 3, 0xaabbccdd);
        DeckErrorTypeOnlyPayload typeOnlyA =
            DecodeDeckError<DeckErrorTypeOnlyPayload>(rawA);
        DeckErrorTypeOnlyPayload typeOnlyB =
            DecodeDeckError<DeckErrorTypeOnlyPayload>(rawB);
        Equal(typeOnlyA, typeOnlyB);
        Equal(typeOnlyA.GetHashCode(), typeOnlyB.GetHashCode());
        BytesEqual(
            DeckErrorRaw(error),
            PacketPayloadCodec.EncodeStocErrorMessage(typeOnlyB));
    }
}

static void TestValidatedVersionCompatibility()
{
    byte[] validJoin = PacketPayloadCodec.EncodeJoinGame(
        new CtosJoinGamePayload(
            0x1354,
            0x11223344,
            "room-secret",
            new ProtocolClientVersion(41, 0, 11, 0)));
    FrameReadResult<ValidatedCtosPacket> valid =
        PacketPayloadValidator.TryReadValidatedCtos(
            WireFrameCodec.EncodeCtos(CtosPacketType.JoinGame, validJoin));
    Equal(FrameReadStatus.Success, valid.Status);

    byte[] wrongProtocol = validJoin.ToArray();
    wrongProtocol[0] = 0x55;
    wrongProtocol[1] = 0x13;
    FrameReadResult<CtosFrame> rawWrongProtocol =
        WireFrameCodec.TryReadCtos(
            WireFrameCodec.EncodeCtos(CtosPacketType.JoinGame, wrongProtocol));
    Equal(FrameReadStatus.Success, rawWrongProtocol.Status);
    AssertUnsupportedJoinVersion(wrongProtocol);

    byte[] wrongClient = validJoin.ToArray();
    wrongClient[48] = 40;
    AssertUnsupportedJoinVersion(wrongClient);

    byte[] wrongCore = validJoin.ToArray();
    wrongCore[50] = 10;
    AssertUnsupportedJoinVersion(wrongCore);

    FrameReadResult<ValidatedStocPacket> validHost =
        PacketPayloadValidator.TryReadValidatedStoc(
            WireFrameCodec.EncodeStoc(
                StocPacketType.JoinGame,
                HostV1GoldenPayload()));
    Equal(FrameReadStatus.Success, validHost.Status);

    byte[] wrongHostClient = HostV1GoldenPayload();
    wrongHostClient[28] = 40;
    FrameReadResult<StocFrame> rawWrongHostClient =
        WireFrameCodec.TryReadStoc(
            WireFrameCodec.EncodeStoc(StocPacketType.JoinGame, wrongHostClient));
    Equal(FrameReadStatus.Success, rawWrongHostClient.Status);
    AssertUnsupportedHostVersion(wrongHostClient);

    byte[] wrongHostCore = HostV1GoldenPayload();
    wrongHostCore[30] = 10;
    AssertUnsupportedHostVersion(wrongHostCore);
}

static void AssertUnsupportedHostVersion(byte[] payload)
{
    FrameReadResult<ValidatedStocPacket> result =
        PacketPayloadValidator.TryReadValidatedStoc(
            WireFrameCodec.EncodeStoc(
                StocPacketType.JoinGame,
                payload));
    Equal(FrameReadStatus.Invalid, result.Status);
    Equal(ProtocolErrorCode.UnsupportedVersion, result.Error);
}

static void AssertUnsupportedJoinVersion(byte[] payload)
{
    FrameReadResult<ValidatedCtosPacket> result =
        PacketPayloadValidator.TryReadValidatedCtos(
            WireFrameCodec.EncodeCtos(CtosPacketType.JoinGame, payload));
    Equal(FrameReadStatus.Invalid, result.Status);
    Equal(ProtocolErrorCode.UnsupportedVersion, result.Error);
}

static void AssertValidatedCtos(
    FrameReadResult<ValidatedCtosPacket> result,
    int consumedBytes,
    CtosPacketType type,
    PayloadContractKind contract)
{
    Equal(FrameReadStatus.Success, result.Status);
    Equal(consumedBytes, result.ConsumedBytes);
    NotNull(result.Frame);
    Equal(type, result.Frame!.Type);
    Equal(contract, result.Frame.PayloadContract);
    if (contract == PayloadContractKind.ExactEmpty)
    {
        Equal<object?>(null, result.Frame.Payload);
    }
    else
    {
        NotNull(result.Frame.Payload);
    }
}

static void AssertValidatedStoc(
    FrameReadResult<ValidatedStocPacket> result,
    int consumedBytes,
    StocPacketType type,
    PayloadContractKind contract)
{
    Equal(FrameReadStatus.Success, result.Status);
    Equal(consumedBytes, result.ConsumedBytes);
    NotNull(result.Frame);
    Equal(type, result.Frame!.Type);
    Equal(contract, result.Frame.PayloadContract);
    if (contract == PayloadContractKind.ExactEmpty)
    {
        Equal<object?>(null, result.Frame.Payload);
    }
    else
    {
        NotNull(result.Frame.Payload);
    }
}

static void AssertNeedMore<T>(FrameReadResult<T> result)
    where T : class
{
    Equal(FrameReadStatus.NeedMoreData, result.Status);
    Equal(0, result.ConsumedBytes);
    Equal(ProtocolErrorCode.None, result.Error);
    Equal<T?>(null, result.Frame);
}

static byte[] Hex(string text)
{
    string compact = string.Concat(text.Where(character => !char.IsWhiteSpace(character)));
    if (compact.Length % 2 != 0)
    {
        throw new InvalidOperationException("Hex string has odd length.");
    }

    byte[] result = new byte[compact.Length / 2];
    for (int index = 0; index < result.Length; index++)
    {
        result[index] = Convert.ToByte(compact.Substring(index * 2, 2), 16);
    }

    return result;
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

static T DecodeDeckError<T>(byte[] payload)
    where T : StocErrorPayload
{
    PayloadDecodeResult<StocErrorPayload> decoded =
        PacketPayloadCodec.DecodeStocErrorMessage(payload);
    True(decoded.IsSuccess);
    if (decoded.Value is not T result)
    {
        throw new InvalidOperationException(
            $"Expected {typeof(T).Name}, got {decoded.Value.GetType().Name}.");
    }

    return result;
}

static byte[] DeckErrorRaw(
    DeckErrorCode error,
    uint current = 0,
    uint minimum = 0,
    uint maximum = 0,
    uint cardCode = 0)
{
    byte[] payload = new byte[PacketPayloadCodec.DeckErrorPayloadLength];
    payload[0] = (byte)ErrorType.DeckError;
    BinaryPrimitives.WriteUInt32LittleEndian(
        payload.AsSpan(4, 4),
        (uint)error);
    BinaryPrimitives.WriteUInt32LittleEndian(
        payload.AsSpan(8, 4),
        current);
    BinaryPrimitives.WriteUInt32LittleEndian(
        payload.AsSpan(12, 4),
        minimum);
    BinaryPrimitives.WriteUInt32LittleEndian(
        payload.AsSpan(16, 4),
        maximum);
    BinaryPrimitives.WriteUInt32LittleEndian(
        payload.AsSpan(20, 4),
        cardCode);
    return payload;
}

static byte[] HostGoldenPayload() =>
    Hex(
        "04 03 02 01 05 06 07 08 09 00 00 00 " +
        "10 0f 0e 0d 11 12 14 13 18 17 16 15 " +
        "1c 1b 1a 19 1d 1e 1f 20 " +
        "24 23 22 21 28 27 26 25 2c 2b 2a 29 " +
        "30 2f 2e 2d 34 33 32 31 36 35 " +
        "38 37 3a 39 3c 3b 3e 3d 40 3f 42 41 00 00");

static byte[] HostV1GoldenPayload()
{
    byte[] payload = HostGoldenPayload();
    payload[28] = 0x29;
    payload[29] = 0x00;
    payload[30] = 0x0b;
    payload[31] = 0x00;
    return payload;
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

    throw new InvalidOperationException(
        $"Expected exception {typeof(TException).Name}.");
}

static void SequenceEqual<T>(
    IReadOnlyList<T> expected,
    IReadOnlyList<T> actual)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException("Expected sequences to be equal.");
    }
}
