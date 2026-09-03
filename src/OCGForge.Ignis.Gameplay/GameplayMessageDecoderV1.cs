using System.Buffers.Binary;
using OCGForge.Ignis.Protocol;

namespace OCGForge.Ignis.Gameplay;

public static class GameplayWirePrimitivesV1
{
    public const int ModernLocInfoByteLength = 10;

    public static bool TryDecodeModernLocInfo(
        ReadOnlySpan<byte> bytes,
        out ModernLocInfoV1 value,
        out GameplayErrorCode error)
    {
        if (bytes.Length != ModernLocInfoByteLength)
        {
            value = default;
            error = GameplayErrorCode.MalformedGameMessage;
            return false;
        }

        value = new ModernLocInfoV1(
            bytes[0],
            bytes[1],
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(2, 4)),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(6, 4)));
        error = GameplayErrorCode.None;
        return true;
    }
}

public sealed class GameplayMessageDecoderV1
{
    private const byte MsgUpdateData = 6;
    private const byte MsgUpdateCard = 7;

    private GameplayPerspectiveV1? perspective;
    private bool perspectiveDependentProcessingStarted;

    public GameplayPerspectiveV1? Perspective => perspective;

    public GameplayMessageDecodeResult Decode(
        StocGameMessagePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ReadOnlySpan<byte> bytes = payload.Bytes.Span;
        if (bytes.IsEmpty)
        {
            return GameplayMessageDecodeResult.Failure(
                GameplayErrorCode.MalformedGameMessage,
                perspective);
        }

        byte messageId = bytes[0];
        if (messageId != GameplayMessageV1.MessageId)
        {
            return DecodeNonStart(messageId);
        }

        if (bytes.Length != 18)
        {
            return GameplayMessageDecodeResult.Failure(
                GameplayErrorCode.MalformedGameMessage,
                perspective);
        }

        byte playerType = bytes[1];
        if (playerType is 0x10 or 0x11)
        {
            return GameplayMessageDecodeResult.Failure(
                GameplayErrorCode.UnsupportedPerspective,
                perspective);
        }

        if (playerType is not 0x00 and not 0x01)
        {
            return GameplayMessageDecodeResult.Failure(
                GameplayErrorCode.InvalidPerspectiveRole,
                perspective);
        }

        if (perspectiveDependentProcessingStarted && perspective is null)
        {
            return GameplayMessageDecodeResult.Failure(
                GameplayErrorCode.PerspectiveEstablishmentTooLate,
                perspective);
        }

        if (perspective is not null)
        {
            return GameplayMessageDecodeResult.Failure(
                perspective.PlayerType == playerType
                    ? GameplayErrorCode.DuplicatePerspective
                    : GameplayErrorCode.ConflictingPerspective,
                perspective);
        }

        if (!GameplayPerspectiveV1.TryCreate(playerType, out GameplayPerspectiveV1 newPerspective))
        {
            return GameplayMessageDecodeResult.Failure(
                GameplayErrorCode.InvalidPerspectiveRole,
                perspective);
        }

        GameplayStartPayloadV1 start = new(
            playerType,
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(2, 4)),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(6, 4)),
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(10, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(12, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(14, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(16, 2)));

        perspective = newPerspective;
        return GameplayMessageDecodeResult.Success(
            GameplayMessageV1.FromStart(start),
            newPerspective);
    }

    private GameplayMessageDecodeResult DecodeNonStart(byte messageId)
    {
        if (messageId is MsgUpdateData or MsgUpdateCard)
        {
            perspectiveDependentProcessingStarted = true;
            return GameplayMessageDecodeResult.Failure(
                perspective is null
                    ? GameplayErrorCode.PerspectiveNotEstablished
                    : GameplayErrorCode.UnsupportedMessage,
                perspective);
        }

        return GameplayMessageDecodeResult.Failure(
            messageId == 3
                ? GameplayErrorCode.UnsupportedMessage
                : GameplayErrorCode.UnknownMessageId,
            perspective);
    }
}
