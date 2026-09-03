namespace OCGForge.Ignis.Protocol;

public static class PacketPayloadValidator
{
    public static FrameReadResult<ValidatedCtosPacket> TryReadValidatedCtos(
        ReadOnlySpan<byte> buffer)
    {
        FrameReadResult<CtosFrame> raw = WireFrameCodec.TryReadCtos(buffer);
        if (raw.Status == FrameReadStatus.NeedMoreData)
        {
            return FrameReadResults.NeedMoreData<ValidatedCtosPacket>();
        }

        if (raw.Status == FrameReadStatus.Invalid)
        {
            return FrameReadResults.Invalid<ValidatedCtosPacket>(raw.Error);
        }

        PayloadDecodeResult<ValidatedCtosPacket> validated =
            ValidateCtos(raw.Frame!);
        return validated.IsSuccess
            ? FrameReadResults.Success(raw.ConsumedBytes, validated.Value)
            : FrameReadResults.Invalid<ValidatedCtosPacket>(validated.Error);
    }

    public static FrameReadResult<ValidatedStocPacket> TryReadValidatedStoc(
        ReadOnlySpan<byte> buffer)
    {
        FrameReadResult<StocFrame> raw = WireFrameCodec.TryReadStoc(buffer);
        if (raw.Status == FrameReadStatus.NeedMoreData)
        {
            return FrameReadResults.NeedMoreData<ValidatedStocPacket>();
        }

        if (raw.Status == FrameReadStatus.Invalid)
        {
            return FrameReadResults.Invalid<ValidatedStocPacket>(raw.Error);
        }

        PayloadDecodeResult<ValidatedStocPacket> validated =
            ValidateStoc(raw.Frame!);
        return validated.IsSuccess
            ? FrameReadResults.Success(raw.ConsumedBytes, validated.Value)
            : FrameReadResults.Invalid<ValidatedStocPacket>(validated.Error);
    }

    public static PayloadDecodeResult<ValidatedCtosPacket> ValidateCtos(
        CtosFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        return frame.Type switch
        {
            CtosPacketType.Response => Typed(
                frame.Type,
                PayloadContractKind.Opaque,
                PacketPayloadCodec.DecodeCtosResponse(frame.Payload.Span)),
            CtosPacketType.UpdateDeck => Typed(
                frame.Type,
                PayloadContractKind.ExactTypedLayout,
                PacketPayloadCodec.DecodeUpdateDeck(frame.Payload.Span)),
            CtosPacketType.HandResult => Typed(
                frame.Type,
                PayloadContractKind.ExactTypedLayout,
                PacketPayloadCodec.DecodeCtosHandResult(frame.Payload.Span)),
            CtosPacketType.TpResult => Typed(
                frame.Type,
                PayloadContractKind.ExactTypedLayout,
                PacketPayloadCodec.DecodeCtosTpResult(frame.Payload.Span)),
            CtosPacketType.PlayerInfo => Typed(
                frame.Type,
                PayloadContractKind.ExactTypedLayout,
                PacketPayloadCodec.DecodePlayerInfo(frame.Payload.Span)),
            CtosPacketType.JoinGame => Typed(
                frame.Type,
                PayloadContractKind.ExactTypedLayout,
                PacketPayloadCodec.DecodeJoinGame(frame.Payload.Span)),
            CtosPacketType.LeaveGame or
            CtosPacketType.Surrender or
            CtosPacketType.TimeConfirm or
            CtosPacketType.HsReady or
            CtosPacketType.HsNotReady or
            CtosPacketType.HsStart => ExactEmptyCtos(frame),
            _ => PayloadDecodeResults.Failure<ValidatedCtosPacket>(
                ProtocolErrorCode.UnknownPacketType)
        };
    }

    public static PayloadDecodeResult<ValidatedStocPacket> ValidateStoc(
        StocFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        return frame.Type switch
        {
            StocPacketType.GameMsg => Typed(
                frame.Type,
                PayloadContractKind.Opaque,
                PacketPayloadCodec.DecodeStocGameMessage(frame.Payload.Span)),
            StocPacketType.ErrorMsg => Typed(
                frame.Type,
                PayloadContractKind.ExactTypedLayout,
                PacketPayloadCodec.DecodeStocErrorMessage(frame.Payload.Span)),
            StocPacketType.SelectHand or
            StocPacketType.SelectTp or
            StocPacketType.TpResult or
            StocPacketType.LeaveGame or
            StocPacketType.DuelStart or
            StocPacketType.DuelEnd => ExactEmptyStoc(frame),
            StocPacketType.HandResult => Typed(
                frame.Type,
                PayloadContractKind.ExactTypedLayout,
                PacketPayloadCodec.DecodeStocHandResult(frame.Payload.Span)),
            StocPacketType.JoinGame => Typed(
                frame.Type,
                PayloadContractKind.ExactTypedLayout,
                PacketPayloadCodec.DecodeStocJoinGame(frame.Payload.Span)),
            StocPacketType.TypeChange => Typed(
                frame.Type,
                PayloadContractKind.ExactTypedLayout,
                PacketPayloadCodec.DecodeStocTypeChange(frame.Payload.Span)),
            StocPacketType.TimeLimit => Typed(
                frame.Type,
                PayloadContractKind.ExactTypedLayout,
                PacketPayloadCodec.DecodeStocTimeLimit(frame.Payload.Span)),
            StocPacketType.HsPlayerEnter => Typed(
                frame.Type,
                PayloadContractKind.ExactTypedLayout,
                PacketPayloadCodec.DecodeStocHsPlayerEnter(frame.Payload.Span)),
            StocPacketType.HsPlayerChange => Typed(
                frame.Type,
                PayloadContractKind.ExactTypedLayout,
                PacketPayloadCodec.DecodeStocHsPlayerChange(frame.Payload.Span)),
            StocPacketType.HsWatchChange => Typed(
                frame.Type,
                PayloadContractKind.ExactTypedLayout,
                PacketPayloadCodec.DecodeStocHsWatchChange(frame.Payload.Span)),
            _ => PayloadDecodeResults.Failure<ValidatedStocPacket>(
                PacketTypeCatalog.ClassifyStoc((byte)frame.Type) ==
                    PacketTypeDisposition.ExplicitlyUnsupported
                    ? ProtocolErrorCode.UnsupportedPacketType
                    : ProtocolErrorCode.UnknownPacketType)
        };
    }

    private static PayloadDecodeResult<ValidatedCtosPacket> ExactEmptyCtos(
        CtosFrame frame) =>
        frame.Payload.Length == 0
            ? PayloadDecodeResults.Success(
                new ValidatedCtosPacket(
                    frame.Type,
                    PayloadContractKind.ExactEmpty,
                    null))
            : PayloadDecodeResults.Failure<ValidatedCtosPacket>(
                ProtocolErrorCode.TrailingPayloadBytes);

    private static PayloadDecodeResult<ValidatedStocPacket> ExactEmptyStoc(
        StocFrame frame) =>
        frame.Payload.Length == 0
            ? PayloadDecodeResults.Success(
                new ValidatedStocPacket(
                    frame.Type,
                    PayloadContractKind.ExactEmpty,
                    null))
            : PayloadDecodeResults.Failure<ValidatedStocPacket>(
                ProtocolErrorCode.TrailingPayloadBytes);

    private static PayloadDecodeResult<ValidatedCtosPacket> Typed<T>(
        CtosPacketType type,
        PayloadContractKind contract,
        PayloadDecodeResult<T> decoded) =>
        decoded.IsSuccess
            ? PayloadDecodeResults.Success(
                new ValidatedCtosPacket(type, contract, decoded.Value))
            : PayloadDecodeResults.Failure<ValidatedCtosPacket>(decoded.Error);

    private static PayloadDecodeResult<ValidatedStocPacket> Typed<T>(
        StocPacketType type,
        PayloadContractKind contract,
        PayloadDecodeResult<T> decoded) =>
        decoded.IsSuccess
            ? PayloadDecodeResults.Success(
                new ValidatedStocPacket(type, contract, decoded.Value))
            : PayloadDecodeResults.Failure<ValidatedStocPacket>(decoded.Error);
}
