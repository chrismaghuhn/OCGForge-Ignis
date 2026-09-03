using System.Buffers.Binary;

namespace OCGForge.Ignis.Protocol;

public static class PacketPayloadCodec
{
    public const int PlayerInfoPayloadLength = ProtocolContractV1.FixedTextCodeUnits * 2;
    public const int JoinGamePayloadLength = 52;
    public const int TimeLimitPayloadLength = 4;
    public const int HsPlayerEnterPayloadLength = 42;
    public const int HostInfoPayloadLength = 68;

    public static byte[] EncodePlayerInfo(CtosPlayerInfoPayload value) =>
        FixedUtf16String.Encode(value.Name, ProtocolContractV1.FixedTextCodeUnits);

    public static PayloadDecodeResult<CtosPlayerInfoPayload> DecodePlayerInfo(
        ReadOnlySpan<byte> payload)
    {
        PayloadDecodeResult<string> name = FixedUtf16String.Decode(
            payload,
            ProtocolContractV1.FixedTextCodeUnits);
        return name.IsSuccess
            ? PayloadDecodeResults.Success(
                new CtosPlayerInfoPayload(name.Value))
            : PayloadDecodeResults.Failure<CtosPlayerInfoPayload>(name.Error);
    }

    public static byte[] EncodeJoinGame(CtosJoinGamePayload value)
    {
        byte[] payload = new byte[JoinGamePayloadLength];
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(0, 2), value.ProtocolVersion);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4, 4), value.GameId);
        FixedUtf16String.Encode(
                value.Password,
                ProtocolContractV1.FixedTextCodeUnits)
            .CopyTo(payload, 8);
        WriteClientVersion(payload, 48, value.ClientVersion);
        return payload;
    }

    public static PayloadDecodeResult<CtosJoinGamePayload> DecodeJoinGame(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != JoinGamePayloadLength)
        {
            return PayloadDecodeResults.Failure<CtosJoinGamePayload>(
                ExactLengthError(payload.Length, JoinGamePayloadLength));
        }

        PayloadDecodeResult<string> password = FixedUtf16String.Decode(
            payload.Slice(8, 40),
            ProtocolContractV1.FixedTextCodeUnits);
        return password.IsSuccess
            ? PayloadDecodeResults.Success(
                new CtosJoinGamePayload(
                    BinaryPrimitives.ReadUInt16LittleEndian(payload[..2]),
                    BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(4, 4)),
                    password.Value,
                    ReadClientVersion(payload, 48)))
            : PayloadDecodeResults.Failure<CtosJoinGamePayload>(password.Error);
    }

    public static byte[] EncodeUpdateDeck(CtosUpdateDeckPayload value)
    {
        ArgumentNullException.ThrowIfNull(value);

        int totalCards;
        int payloadLength;
        try
        {
            totalCards = checked(value.MainAndExtraCards.Count + value.SideCards.Count);
            payloadLength = checked(8 + checked(totalCards * sizeof(uint)));
        }
        catch (OverflowException exception)
        {
            throw new ProtocolCodecException(
                ProtocolErrorCode.IntegerOverflow,
                "The update-deck payload length overflowed an integer.",
                exception);
        }

        EnsurePayloadLength(payloadLength);
        byte[] payload = new byte[payloadLength];
        BinaryPrimitives.WriteUInt32LittleEndian(
            payload.AsSpan(0, 4),
            checked((uint)value.MainAndExtraCards.Count));
        BinaryPrimitives.WriteUInt32LittleEndian(
            payload.AsSpan(4, 4),
            checked((uint)value.SideCards.Count));

        int offset = 8;
        foreach (uint code in value.MainAndExtraSpan)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(offset, 4), code);
            offset += 4;
        }

        foreach (uint code in value.SideSpan)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(offset, 4), code);
            offset += 4;
        }

        return payload;
    }

    public static PayloadDecodeResult<CtosUpdateDeckPayload> DecodeUpdateDeck(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 8)
        {
            return PayloadDecodeResults.Failure<CtosUpdateDeckPayload>(
                ProtocolErrorCode.PayloadLengthMismatch);
        }

        ulong mainCount = BinaryPrimitives.ReadUInt32LittleEndian(payload[..4]);
        ulong sideCount = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(4, 4));
        ulong expectedLength = 8UL + ((mainCount + sideCount) * sizeof(uint));

        if (expectedLength > int.MaxValue)
        {
            return PayloadDecodeResults.Failure<CtosUpdateDeckPayload>(
                ProtocolErrorCode.IntegerOverflow);
        }

        if (expectedLength != (ulong)payload.Length)
        {
            return PayloadDecodeResults.Failure<CtosUpdateDeckPayload>(
                expectedLength < (ulong)payload.Length
                    ? ProtocolErrorCode.TrailingPayloadBytes
                    : ProtocolErrorCode.PayloadLengthMismatch);
        }

        int mainLength = checked((int)mainCount);
        int sideLength = checked((int)sideCount);
        uint[] mainAndExtra = new uint[mainLength];
        uint[] side = new uint[sideLength];
        int offset = 8;

        for (int index = 0; index < mainLength; index++)
        {
            mainAndExtra[index] = BinaryPrimitives.ReadUInt32LittleEndian(
                payload.Slice(offset, 4));
            offset += 4;
        }

        for (int index = 0; index < sideLength; index++)
        {
            side[index] = BinaryPrimitives.ReadUInt32LittleEndian(
                payload.Slice(offset, 4));
            offset += 4;
        }

        return PayloadDecodeResults.Success(
            new CtosUpdateDeckPayload(mainAndExtra, side));
    }

    public static byte[] EncodeCtosHandResult(CtosHandResultPayload value) =>
        new[] { value.Result };

    public static PayloadDecodeResult<CtosHandResultPayload> DecodeCtosHandResult(
        ReadOnlySpan<byte> payload) =>
        payload.Length == 1
            ? PayloadDecodeResults.Success(
                new CtosHandResultPayload(payload[0]))
            : PayloadDecodeResults.Failure<CtosHandResultPayload>(
                ExactLengthError(payload.Length, 1));

    public static byte[] EncodeCtosTpResult(CtosTpResultPayload value) =>
        new[] { value.Result };

    public static PayloadDecodeResult<CtosTpResultPayload> DecodeCtosTpResult(
        ReadOnlySpan<byte> payload) =>
        payload.Length == 1
            ? PayloadDecodeResults.Success(
                new CtosTpResultPayload(payload[0]))
            : PayloadDecodeResults.Failure<CtosTpResultPayload>(
                ExactLengthError(payload.Length, 1));

    public static byte[] EncodeStocErrorMessage(StocErrorMessagePayload value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!IsKnownErrorType((byte)value.Type))
        {
            throw new ProtocolCodecException(
                ProtocolErrorCode.UnknownErrorType,
                "The error-message type is not part of the frozen V1 set.");
        }

        int payloadLength;
        try
        {
            payloadLength = checked(8 + value.AdditionalPayload.Length);
        }
        catch (OverflowException exception)
        {
            throw new ProtocolCodecException(
                ProtocolErrorCode.IntegerOverflow,
                "The error-message payload length overflowed an integer.",
                exception);
        }

        EnsurePayloadLength(payloadLength);
        byte[] payload = new byte[payloadLength];
        payload[0] = (byte)value.Type;
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4, 4), value.Code);
        value.AdditionalPayload.AsSpan().CopyTo(payload.AsSpan(8));
        return payload;
    }

    public static PayloadDecodeResult<StocErrorMessagePayload> DecodeStocErrorMessage(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 8)
        {
            return PayloadDecodeResults.Failure<StocErrorMessagePayload>(
                ProtocolErrorCode.PayloadLengthMismatch);
        }

        if (!IsKnownErrorType(payload[0]))
        {
            return PayloadDecodeResults.Failure<StocErrorMessagePayload>(
                ProtocolErrorCode.UnknownErrorType);
        }

        return PayloadDecodeResults.Success(
            new StocErrorMessagePayload(
                (ErrorType)payload[0],
                BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(4, 4)),
                new OpaquePayload(payload[8..])));
    }

    public static byte[] EncodeStocJoinGame(HostInfoPayload value)
    {
        byte[] payload = new byte[HostInfoPayloadLength];
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(0, 4), value.BanlistId);
        payload[4] = value.Rule;
        payload[5] = value.Mode;
        payload[6] = value.DuelRule;
        payload[7] = value.NoCheckDeckContent;
        payload[8] = value.NoShuffleDeck;
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(12, 4), value.StartLp);
        payload[16] = value.StartHand;
        payload[17] = value.DrawCount;
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(18, 2), value.TimeLimit);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(20, 4), value.DuelFlagHigh);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(24, 4), value.Handshake);
        WriteClientVersion(payload, 28, value.Version);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(32, 4), value.Team1);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(36, 4), value.Team2);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(40, 4), value.BestOf);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(44, 4), value.DuelFlagLow);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(48, 4), value.ForbiddenTypes);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(52, 2), value.ExtraRules);
        WriteDeckSize(payload, 54, value.MainDeck);
        WriteDeckSize(payload, 58, value.ExtraDeck);
        WriteDeckSize(payload, 62, value.SideDeck);
        return payload;
    }

    public static PayloadDecodeResult<HostInfoPayload> DecodeStocJoinGame(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != HostInfoPayloadLength)
        {
            return PayloadDecodeResults.Failure<HostInfoPayload>(
                ExactLengthError(payload.Length, HostInfoPayloadLength));
        }

        return PayloadDecodeResults.Success(
            new HostInfoPayload(
                BinaryPrimitives.ReadUInt32LittleEndian(payload[..4]),
                payload[4],
                payload[5],
                payload[6],
                payload[7],
                payload[8],
                BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(12, 4)),
                payload[16],
                payload[17],
                BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(18, 2)),
                BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(20, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(24, 4)),
                ReadClientVersion(payload, 28),
                BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(32, 4)),
                BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(36, 4)),
                BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(40, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(44, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(48, 4)),
                BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(52, 2)),
                ReadDeckSize(payload, 54),
                ReadDeckSize(payload, 58),
                ReadDeckSize(payload, 62)));
    }

    public static byte[] EncodeStocHandResult(StocHandResultPayload value) =>
        new[] { value.Result1, value.Result2 };

    public static PayloadDecodeResult<StocHandResultPayload> DecodeStocHandResult(
        ReadOnlySpan<byte> payload) =>
        payload.Length == 2
            ? PayloadDecodeResults.Success(
                new StocHandResultPayload(payload[0], payload[1]))
            : PayloadDecodeResults.Failure<StocHandResultPayload>(
                ExactLengthError(payload.Length, 2));

    public static byte[] EncodeStocTypeChange(StocTypeChangePayload value) =>
        new[] { value.Type };

    public static PayloadDecodeResult<StocTypeChangePayload> DecodeStocTypeChange(
        ReadOnlySpan<byte> payload) =>
        payload.Length == 1
            ? PayloadDecodeResults.Success(
                new StocTypeChangePayload(payload[0]))
            : PayloadDecodeResults.Failure<StocTypeChangePayload>(
                ExactLengthError(payload.Length, 1));

    public static byte[] EncodeStocTimeLimit(StocTimeLimitPayload value)
    {
        byte[] payload = new byte[TimeLimitPayloadLength];
        payload[0] = value.Player;
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2, 2), value.LeftTime);
        return payload;
    }

    public static PayloadDecodeResult<StocTimeLimitPayload> DecodeStocTimeLimit(
        ReadOnlySpan<byte> payload) =>
        payload.Length == TimeLimitPayloadLength
            ? PayloadDecodeResults.Success(
                new StocTimeLimitPayload(
                    payload[0],
                    BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2, 2))))
            : PayloadDecodeResults.Failure<StocTimeLimitPayload>(
                ExactLengthError(payload.Length, TimeLimitPayloadLength));

    public static byte[] EncodeStocHsPlayerEnter(StocHsPlayerEnterPayload value)
    {
        byte[] payload = new byte[HsPlayerEnterPayloadLength];
        FixedUtf16String.Encode(
                value.Name,
                ProtocolContractV1.FixedTextCodeUnits)
            .CopyTo(payload, 0);
        payload[40] = value.Position;
        return payload;
    }

    public static PayloadDecodeResult<StocHsPlayerEnterPayload> DecodeStocHsPlayerEnter(
        ReadOnlySpan<byte> payload)
    {
        if (payload.Length != HsPlayerEnterPayloadLength)
        {
            return PayloadDecodeResults.Failure<StocHsPlayerEnterPayload>(
                ExactLengthError(payload.Length, HsPlayerEnterPayloadLength));
        }

        PayloadDecodeResult<string> name = FixedUtf16String.Decode(
            payload[..40],
            ProtocolContractV1.FixedTextCodeUnits);
        return name.IsSuccess
            ? PayloadDecodeResults.Success(
                new StocHsPlayerEnterPayload(name.Value, payload[40]))
            : PayloadDecodeResults.Failure<StocHsPlayerEnterPayload>(name.Error);
    }

    public static byte[] EncodeStocHsPlayerChange(StocHsPlayerChangePayload value) =>
        new[] { value.Status };

    public static PayloadDecodeResult<StocHsPlayerChangePayload>
        DecodeStocHsPlayerChange(ReadOnlySpan<byte> payload) =>
        payload.Length == 1
            ? PayloadDecodeResults.Success(
                new StocHsPlayerChangePayload(payload[0]))
            : PayloadDecodeResults.Failure<StocHsPlayerChangePayload>(
                ExactLengthError(payload.Length, 1));

    public static byte[] EncodeStocHsWatchChange(StocHsWatchChangePayload value)
    {
        byte[] payload = new byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, value.WatchCount);
        return payload;
    }

    public static PayloadDecodeResult<StocHsWatchChangePayload>
        DecodeStocHsWatchChange(ReadOnlySpan<byte> payload) =>
        payload.Length == 2
            ? PayloadDecodeResults.Success(
                new StocHsWatchChangePayload(
                    BinaryPrimitives.ReadUInt16LittleEndian(payload)))
            : PayloadDecodeResults.Failure<StocHsWatchChangePayload>(
                ExactLengthError(payload.Length, 2));

    public static byte[] EncodeCtosResponse(CtosResponsePayload value)
    {
        ArgumentNullException.ThrowIfNull(value);
        EnsurePayloadLength(value.Length);
        return value.Bytes.ToArray();
    }

    public static PayloadDecodeResult<CtosResponsePayload> DecodeCtosResponse(
        ReadOnlySpan<byte> payload) =>
        PayloadDecodeResults.Success(
            new CtosResponsePayload(payload));

    public static byte[] EncodeStocGameMessage(StocGameMessagePayload value)
    {
        ArgumentNullException.ThrowIfNull(value);
        EnsurePayloadLength(value.Length);
        return value.Bytes.ToArray();
    }

    public static PayloadDecodeResult<StocGameMessagePayload> DecodeStocGameMessage(
        ReadOnlySpan<byte> payload) =>
        PayloadDecodeResults.Success(
            new StocGameMessagePayload(payload));

    private static void EnsurePayloadLength(int payloadLength)
    {
        if (payloadLength < 0 || payloadLength > ProtocolContractV1.MaxPayloadLength)
        {
            throw new ProtocolCodecException(
                ProtocolErrorCode.OversizedPacket,
                "The typed payload exceeds the representable V1 frame capacity.");
        }
    }

    private static ProtocolErrorCode ExactLengthError(int actual, int expected) =>
        actual > expected
            ? ProtocolErrorCode.TrailingPayloadBytes
            : ProtocolErrorCode.PayloadLengthMismatch;

    private static bool IsKnownErrorType(byte rawType) =>
        rawType >= (byte)ErrorType.JoinError &&
        rawType <= (byte)ErrorType.VersionError2;

    private static void WriteClientVersion(
        Span<byte> destination,
        int offset,
        ProtocolClientVersion version)
    {
        destination[offset] = version.ClientMajor;
        destination[offset + 1] = version.ClientMinor;
        destination[offset + 2] = version.CoreMajor;
        destination[offset + 3] = version.CoreMinor;
    }

    private static ProtocolClientVersion ReadClientVersion(
        ReadOnlySpan<byte> source,
        int offset) =>
        new(
            source[offset],
            source[offset + 1],
            source[offset + 2],
            source[offset + 3]);

    private static void WriteDeckSize(
        Span<byte> destination,
        int offset,
        DeckSizeLimits limits)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset, 2), limits.Min);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset + 2, 2), limits.Max);
    }

    private static DeckSizeLimits ReadDeckSize(
        ReadOnlySpan<byte> source,
        int offset) =>
        new(
            BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(offset, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(offset + 2, 2)));
}
