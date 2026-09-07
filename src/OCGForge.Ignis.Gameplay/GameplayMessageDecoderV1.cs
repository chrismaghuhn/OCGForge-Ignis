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
    private const byte MsgWin = 5;
    private const byte MsgUpdateData = 6;
    private const byte MsgUpdateCard = 7;
    private const byte MsgNewTurn = 40;
    private const byte MsgNewPhase = 41;
    private const byte MsgMove = 50;
    private const byte MsgPosChange = 53;
    private const byte MsgSet = 54;
    private const byte MsgSwap = 55;
    private const byte MsgChaining = 70;
    private const byte MsgChained = 71;
    private const byte MsgChainSolving = 72;
    private const byte MsgChainSolved = 73;
    private const byte MsgChainEnd = 74;
    private const byte MsgChainNegated = 75;
    private const byte MsgChainDisabled = 76;
    private const byte MsgBecomeTarget = 83;
    private const byte MsgDraw = 90;
    private const byte MsgDamage = 91;
    private const byte MsgRecover = 92;
    private const byte MsgEquip = 93;
    private const byte MsgLpUpdate = 94;
    private const byte MsgUnequip = 95;
    private const byte MsgCardTarget = 96;
    private const byte MsgCancelTarget = 97;
    private const byte MsgPayLpCost = 100;
    private const byte MsgAddCounter = 101;
    private const byte MsgRemoveCounter = 102;
    private const byte MsgConfirmDeckTop = 30;
    private const byte MsgConfirmCards = 31;
    private const byte MsgShuffleDeck = 32;
    private const byte MsgShuffleHand = 33;
    private const byte MsgSwapGraveDeck = 35;
    private const byte MsgShuffleSetCard = 36;
    private const byte MsgReverseDeck = 37;
    private const byte MsgShuffleExtra = 39;
    private const byte MsgConfirmExtraTop = 42;
    private const byte MsgSummoning = 60;
    private const byte MsgSummoned = 61;
    private const byte MsgSpecialSummoning = 62;
    private const byte MsgSpecialSummoned = 63;
    private const byte MsgFlipSummoning = 64;
    private const byte MsgFlipSummoned = 65;

    private GameplayPerspectiveV1? perspective;
    private bool perspectiveDependentProcessingStarted;

    public GameplayMessageDecoderV1()
    {
    }

    public GameplayMessageDecoderV1(GameplayPerspectiveV1 establishedPerspective)
    {
        perspective = establishedPerspective ??
            throw new ArgumentNullException(nameof(establishedPerspective));
    }

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

        return bytes[0] == GameplayMessageV1.MessageId
            ? DecodeStart(bytes)
            : DecodeNonStart(bytes);
    }

    private GameplayMessageDecodeResult DecodeStart(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 18)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        byte playerType = bytes[1];
        if (playerType is 0x10 or 0x11)
        {
            return Failure(GameplayErrorCode.UnsupportedPerspective);
        }

        if (playerType is not 0x00 and not 0x01)
        {
            return Failure(GameplayErrorCode.InvalidPerspectiveRole);
        }

        if (perspectiveDependentProcessingStarted && perspective is null)
        {
            return Failure(GameplayErrorCode.PerspectiveEstablishmentTooLate);
        }

        if (perspective is not null)
        {
            return Failure(
                perspective.PlayerType == playerType
                    ? GameplayErrorCode.DuplicatePerspective
                    : GameplayErrorCode.ConflictingPerspective);
        }

        if (!GameplayPerspectiveV1.TryCreate(
                playerType,
                out GameplayPerspectiveV1 newPerspective))
        {
            return Failure(GameplayErrorCode.InvalidPerspectiveRole);
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

    private GameplayMessageDecodeResult DecodeNonStart(ReadOnlySpan<byte> bytes)
    {
        byte messageId = bytes[0];
        if (!IsI3BMessage(messageId))
        {
            return Failure(
                messageId == 3
                    ? GameplayErrorCode.UnsupportedMessage
                    : GameplayErrorCode.UnknownMessageId);
        }

        perspectiveDependentProcessingStarted = true;
        if (perspective is null)
        {
            return Failure(GameplayErrorCode.PerspectiveNotEstablished);
        }

        GameplayMessageDecodeResult result = messageId switch
        {
            MsgWin => DecodeWin(bytes),
            MsgUpdateData => DecodeUpdateData(bytes),
            MsgUpdateCard => DecodeUpdateCard(bytes),
            MsgNewTurn => DecodeNewTurn(bytes),
            MsgNewPhase => DecodeNewPhase(bytes),
            MsgMove => DecodeMove(bytes),
            MsgPosChange => DecodePositionChange(bytes),
            MsgSet => DecodeSet(bytes),
            MsgSwap => DecodeSwap(bytes),
            MsgSwapGraveDeck => DecodeSwapGraveDeck(bytes),
            MsgChaining => DecodeChaining(bytes),
            MsgChained => DecodeChainSize(bytes, GameplayMessageKindV1.Chained),
            MsgChainSolving => DecodeChainSize(bytes, GameplayMessageKindV1.ChainSolving),
            MsgChainSolved => DecodeChainSize(bytes, GameplayMessageKindV1.ChainSolved),
            MsgChainEnd => DecodeEmptyChain(bytes),
            MsgChainNegated => DecodeChainSize(bytes, GameplayMessageKindV1.ChainNegated),
            MsgChainDisabled => DecodeChainSize(bytes, GameplayMessageKindV1.ChainDisabled),
            MsgBecomeTarget => DecodeBecomeTarget(bytes),
            MsgDraw => DecodeDraw(bytes),
            MsgDamage => DecodeLifePoints(bytes, MsgDamage, GameplayMessageKindV1.Damage),
            MsgRecover => DecodeLifePoints(bytes, MsgRecover, GameplayMessageKindV1.Recover),
            MsgEquip => DecodeEquip(bytes),
            MsgLpUpdate => DecodeLifePoints(bytes, MsgLpUpdate, GameplayMessageKindV1.LpUpdate),
            MsgUnequip => DecodeUnequip(bytes),
            MsgCardTarget => DecodeCardTarget(bytes, MsgCardTarget, GameplayMessageKindV1.CardTarget),
            MsgCancelTarget => DecodeCardTarget(bytes, MsgCancelTarget, GameplayMessageKindV1.CancelTarget),
            MsgPayLpCost => DecodeLifePoints(bytes, MsgPayLpCost, GameplayMessageKindV1.PayLpCost),
            MsgSummoning => DecodeSummoning(bytes, MsgSummoning, GameplayMessageKindV1.Summoning),
            MsgSummoned => DecodeSummoned(bytes, MsgSummoned, GameplayMessageKindV1.Summoned),
            MsgSpecialSummoning => DecodeSummoning(
                bytes,
                MsgSpecialSummoning,
                GameplayMessageKindV1.SpecialSummoning),
            MsgSpecialSummoned => DecodeSummoned(
                bytes,
                MsgSpecialSummoned,
                GameplayMessageKindV1.SpecialSummoned),
            MsgFlipSummoning => DecodeSummoning(
                bytes,
                MsgFlipSummoning,
                GameplayMessageKindV1.FlipSummoning),
            MsgFlipSummoned => DecodeSummoned(bytes, MsgFlipSummoned, GameplayMessageKindV1.FlipSummoned),
            MsgConfirmCards => DecodeConfirm(
                bytes,
                MsgConfirmCards,
                GameplayMessageKindV1.ConfirmCards),
            MsgConfirmDeckTop => DecodeConfirm(
                bytes,
                MsgConfirmDeckTop,
                GameplayMessageKindV1.ConfirmDeckTop),
            MsgConfirmExtraTop => DecodeConfirm(
                bytes,
                MsgConfirmExtraTop,
                GameplayMessageKindV1.ConfirmExtraTop),
            MsgShuffleDeck or MsgShuffleHand or MsgShuffleExtra or
                MsgShuffleSetCard or MsgReverseDeck => DecodeShuffle(bytes, messageId),
            MsgAddCounter => DecodeCounter(
                bytes,
                MsgAddCounter,
                GameplayMessageKindV1.AddCounter),
            MsgRemoveCounter => DecodeCounter(
                bytes,
                MsgRemoveCounter,
                GameplayMessageKindV1.RemoveCounter),
            _ => Failure(GameplayErrorCode.UnknownMessageId)
        };

        return result;
    }

    private GameplayMessageDecodeResult DecodeWin(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 3)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        return bytes[1] <= 2
            ? Success(GameplayMessageV1.FromWin(new GameplayWinPayloadV1(bytes[1], bytes[2])))
            : Failure(GameplayErrorCode.InvalidParticipant);
    }

    private GameplayMessageDecodeResult DecodeUpdateData(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 7)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        GameplayErrorCode playerError = ValidatePlayer(bytes[1]);
        if (playerError != GameplayErrorCode.None)
        {
            return Failure(playerError);
        }

        ModernQueryStreamDecodeResult query = ModernQueryDecoderV1.DecodeStream(bytes[3..]);
        if (!query.IsSuccess)
        {
            return Failure(query.Error);
        }

        return Success(GameplayMessageV1.FromUpdateData(
            new GameplayUpdateDataPayloadV1(bytes[1], bytes[2], query.Values)));
    }

    private GameplayMessageDecodeResult DecodeUpdateCard(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 6)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        GameplayErrorCode playerError = ValidatePlayer(bytes[1]);
        if (playerError != GameplayErrorCode.None)
        {
            return Failure(playerError);
        }

        ModernQueryDecodeResult query = ModernQueryDecoderV1.Decode(bytes[4..]);
        if (!query.IsSuccess)
        {
            return Failure(query.Error);
        }

        return Success(GameplayMessageV1.FromUpdateCard(
            new GameplayUpdateCardPayloadV1(bytes[1], bytes[2], bytes[3], query.Value!)));
    }

    private GameplayMessageDecodeResult DecodeNewTurn(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 2)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        GameplayErrorCode error = ValidatePlayer(bytes[1]);
        return error == GameplayErrorCode.None
            ? Success(GameplayMessageV1.FromNewTurn(new GameplayNewTurnPayloadV1(bytes[1])))
            : Failure(error);
    }

    private GameplayMessageDecodeResult DecodeNewPhase(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length == 3
            ? Success(GameplayMessageV1.FromNewPhase(
                new GameplayNewPhasePayloadV1(
                    BinaryPrimitives.ReadUInt16LittleEndian(bytes[1..3]))))
            : Failure(GameplayErrorCode.MalformedGameMessage);
    }

    private GameplayMessageDecodeResult DecodeMove(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 29)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        if (!TryReadLoc(bytes, 5, out ModernLocInfoV1 previous, out GameplayErrorCode error) ||
            !TryReadLoc(bytes, 15, out ModernLocInfoV1 current, out error))
        {
            return Failure(error);
        }

        return Success(GameplayMessageV1.FromMove(new GameplayMovePayloadV1(
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[1..5]),
            previous,
            current,
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[25..29]))));
    }

    private GameplayMessageDecodeResult DecodePositionChange(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 10)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        GameplayErrorCode error = ValidatePlayer(bytes[5]);
        return error == GameplayErrorCode.None
            ? Success(GameplayMessageV1.FromPositionChange(
                new GameplayPositionChangePayloadV1(
                    BinaryPrimitives.ReadUInt32LittleEndian(bytes[1..5]),
                    bytes[5],
                    bytes[6],
                    bytes[7],
                    bytes[8],
                    bytes[9])))
            : Failure(error);
    }

    private GameplayMessageDecodeResult DecodeSet(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 15)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        if (!TryReadLoc(bytes, 5, out ModernLocInfoV1 location, out GameplayErrorCode error))
        {
            return Failure(error);
        }

        return Success(GameplayMessageV1.FromSet(new GameplaySetPayloadV1(
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[1..5]),
            location)));
    }

    private GameplayMessageDecodeResult DecodeSwap(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 29)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        if (!TryReadLoc(bytes, 5, out ModernLocInfoV1 location0, out GameplayErrorCode error) ||
            !TryReadLoc(bytes, 19, out ModernLocInfoV1 location1, out error))
        {
            return Failure(error);
        }

        return Success(GameplayMessageV1.FromSwap(new GameplaySwapPayloadV1(
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[1..5]),
            location0,
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[15..19]),
            location1)));
    }

    private GameplayMessageDecodeResult DecodeSwapGraveDeck(
        ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 10)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        GameplayErrorCode playerError = ValidatePlayer(bytes[1]);
        if (playerError != GameplayErrorCode.None)
        {
            return Failure(playerError);
        }

        uint reportedExtraCount =
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[2..6]);
        uint maskLength = BinaryPrimitives.ReadUInt32LittleEndian(bytes[6..10]);
        if (maskLength > int.MaxValue ||
            (ulong)bytes.Length != 10UL + maskLength)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        return Success(GameplayMessageV1.FromSwapGraveDeck(
            new GameplaySwapGraveDeckPayloadV1(
                bytes[1],
                reportedExtraCount,
                bytes.Slice(10, (int)maskLength).ToArray())));
    }

    private GameplayMessageDecodeResult DecodeChaining(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 33)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        if (!TryReadLoc(bytes, 5, out ModernLocInfoV1 location, out GameplayErrorCode error))
        {
            return Failure(error);
        }

        error = ValidatePlayer(bytes[15]);
        uint chainSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes[29..33]);
        if (error != GameplayErrorCode.None || chainSize == 0)
        {
            if (error == GameplayErrorCode.None && chainSize == 0)
            {
                return Failure(GameplayErrorCode.InvalidChainState);
            }

            return Failure(error);
        }

        return Success(GameplayMessageV1.FromChaining(new GameplayChainingPayloadV1(
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[1..5]),
            location,
            bytes[15],
            bytes[16],
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[17..21]),
            BinaryPrimitives.ReadUInt64LittleEndian(bytes[21..29]),
            chainSize)));
    }

    private GameplayMessageDecodeResult DecodeChainSize(
        ReadOnlySpan<byte> bytes,
        GameplayMessageKindV1 kind)
    {
        return bytes.Length == 2 && bytes[1] > 0
            ? Success(GameplayMessageV1.FromChain(
                bytes[0],
                kind,
                new GameplayChainSizePayloadV1(bytes[1])))
            : Failure(GameplayErrorCode.InvalidChainState);
    }

    private GameplayMessageDecodeResult DecodeEmptyChain(ReadOnlySpan<byte> bytes) =>
        bytes.Length == 1
            ? Success(GameplayMessageV1.FromChain(
                bytes[0],
                GameplayMessageKindV1.ChainEnd,
                default))
            : Failure(GameplayErrorCode.MalformedGameMessage);

    private GameplayMessageDecodeResult DecodeBecomeTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 5)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        uint count = BinaryPrimitives.ReadUInt32LittleEndian(bytes[1..5]);
        ulong required = 5ul + ((ulong)count * GameplayWirePrimitivesV1.ModernLocInfoByteLength);
        if (required > int.MaxValue || required != (uint)bytes.Length)
        {
            return Failure(GameplayErrorCode.QueryLengthMismatch);
        }

        List<ModernLocInfoV1> targets = new((int)count);
        int offset = 5;
        for (uint index = 0; index < count; index++)
        {
            if (!TryReadLoc(bytes, offset, out ModernLocInfoV1 target, out GameplayErrorCode error))
            {
                return Failure(error);
            }

            targets.Add(target);
            offset += GameplayWirePrimitivesV1.ModernLocInfoByteLength;
        }

        return Success(GameplayMessageV1.FromBecomeTarget(
            new GameplayBecomeTargetPayloadV1(targets)));
    }

    private GameplayMessageDecodeResult DecodeDraw(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 6)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        GameplayErrorCode playerError = ValidatePlayer(bytes[1]);
        if (playerError != GameplayErrorCode.None)
        {
            return Failure(playerError);
        }

        uint count = BinaryPrimitives.ReadUInt32LittleEndian(bytes[2..6]);
        if (count == 0)
        {
            return Failure(GameplayErrorCode.InvalidDrawCount);
        }

        ulong required = 6ul + ((ulong)count * 8ul);
        if (required > int.MaxValue || required != (uint)bytes.Length)
        {
            return Failure(GameplayErrorCode.QueryLengthMismatch);
        }

        List<GameplayDrawCardRecordV1> cards = new((int)count);
        int offset = 6;
        for (uint index = 0; index < count; index++)
        {
            cards.Add(new GameplayDrawCardRecordV1(
                BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset + 4, 4))));
            offset += 8;
        }

        return Success(GameplayMessageV1.FromDraw(
            new GameplayDrawPayloadV1(bytes[1], cards)));
    }

    private GameplayMessageDecodeResult DecodeLifePoints(
        ReadOnlySpan<byte> bytes,
        byte messageId,
        GameplayMessageKindV1 kind)
    {
        if (bytes.Length != 6)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        GameplayErrorCode error = ValidatePlayer(bytes[1]);
        return error == GameplayErrorCode.None
            ? Success(GameplayMessageV1.FromLifePoints(
                messageId,
                kind,
                new GameplayLifePointPayloadV1(
                    bytes[1],
                    BinaryPrimitives.ReadUInt32LittleEndian(bytes[2..6]))))
            : Failure(error);
    }

    private GameplayMessageDecodeResult DecodeEquip(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 21)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        if (!TryReadLoc(bytes, 1, out ModernLocInfoV1 card, out GameplayErrorCode error) ||
            !TryReadLoc(bytes, 11, out ModernLocInfoV1 target, out error))
        {
            return Failure(error);
        }

        return Success(GameplayMessageV1.FromEquip(
            new GameplayEquipPayloadV1(card, target)));
    }

    private GameplayMessageDecodeResult DecodeUnequip(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 11)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        return TryReadLoc(bytes, 1, out ModernLocInfoV1 card, out GameplayErrorCode error)
            ? Success(GameplayMessageV1.FromUnequip(new GameplayUnequipPayloadV1(card)))
            : Failure(error);
    }

    private GameplayMessageDecodeResult DecodeCardTarget(
        ReadOnlySpan<byte> bytes,
        byte messageId,
        GameplayMessageKindV1 kind)
    {
        if (bytes.Length != 21)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        if (!TryReadLoc(bytes, 1, out ModernLocInfoV1 source, out GameplayErrorCode error) ||
            !TryReadLoc(bytes, 11, out ModernLocInfoV1 target, out error))
        {
            return Failure(error);
        }

        return Success(GameplayMessageV1.FromCardTarget(
            messageId,
            kind,
            new GameplayCardTargetPayloadV1(source, target)));
    }

    private GameplayMessageDecodeResult DecodeSummoning(
        ReadOnlySpan<byte> bytes,
        byte messageId,
        GameplayMessageKindV1 kind)
    {
        if (bytes.Length != 15)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        if (!TryReadLoc(bytes, 5, out ModernLocInfoV1 location, out GameplayErrorCode error))
        {
            return Failure(error);
        }

        if (location.Location == 0)
        {
            return Failure(GameplayErrorCode.InvalidLocation);
        }

        return Success(GameplayMessageV1.FromSummoning(
            messageId,
            kind,
            new GameplaySummoningPayloadV1(
                BinaryPrimitives.ReadUInt32LittleEndian(bytes[1..5]),
                location)));
    }

    private GameplayMessageDecodeResult DecodeSummoned(
        ReadOnlySpan<byte> bytes,
        byte messageId,
        GameplayMessageKindV1 kind) =>
        bytes.Length == 1
            ? Success(GameplayMessageV1.FromSummoned(messageId, kind))
            : Failure(GameplayErrorCode.MalformedGameMessage);

    private GameplayMessageDecodeResult DecodeConfirm(
        ReadOnlySpan<byte> bytes,
        byte messageId,
        GameplayMessageKindV1 kind)
    {
        const int headerLength = 6;
        const int compactEntryLength = 10;
        const int extendedEntryLength = 14;
        if (bytes.Length < headerLength)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        GameplayErrorCode playerError = ValidatePlayer(bytes[1]);
        if (playerError != GameplayErrorCode.None)
        {
            return Failure(playerError);
        }

        uint count = BinaryPrimitives.ReadUInt32LittleEndian(bytes[2..6]);
        ulong bodyLength = (ulong)bytes.Length - headerLength;
        int entryLength;
        if (bodyLength == (ulong)count * compactEntryLength)
        {
            entryLength = compactEntryLength;
        }
        else if (bodyLength == (ulong)count * extendedEntryLength)
        {
            entryLength = extendedEntryLength;
        }
        else
        {
            return Failure(GameplayErrorCode.QueryLengthMismatch);
        }

        if (count > int.MaxValue)
        {
            return Failure(GameplayErrorCode.QueryCountOverflow);
        }

        List<GameplayConfirmCardRecordV1> cards = new((int)count);
        int offset = headerLength;
        for (uint index = 0; index < count; index++)
        {
            uint cardCode = BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(offset, sizeof(uint)));
            offset += sizeof(uint);

            ModernLocInfoV1 location;
            if (entryLength == compactEntryLength)
            {
                if (!TryReadCompactLoc(
                        bytes,
                        offset,
                        out location,
                        out GameplayErrorCode error))
                {
                    return Failure(error);
                }

                offset += 6;
            }
            else
            {
                if (!TryReadLoc(
                        bytes,
                        offset,
                        out location,
                        out GameplayErrorCode error))
                {
                    return Failure(error);
                }

                offset += GameplayWirePrimitivesV1.ModernLocInfoByteLength;
            }

            if (location.Location == 0)
            {
                return Failure(GameplayErrorCode.InvalidLocation);
            }

            cards.Add(new GameplayConfirmCardRecordV1(cardCode, location));
        }

        return Success(GameplayMessageV1.FromConfirm(
            messageId,
            kind,
            new GameplayConfirmPayloadV1(bytes[1], cards)));
    }

    private GameplayMessageDecodeResult DecodeShuffle(
        ReadOnlySpan<byte> bytes,
        byte messageId)
    {
        if (messageId == MsgReverseDeck)
        {
            return bytes.Length == 1
                ? Success(GameplayMessageV1.FromShuffle(
                    messageId,
                    new GameplayShufflePayloadV1(
                        GameplayShuffleKindV1.ReverseDeck,
                        null,
                        0)))
                : Failure(GameplayErrorCode.MalformedGameMessage);
        }

        if (messageId == MsgShuffleDeck)
        {
            return DecodePlayerShuffle(
                bytes,
                messageId,
                GameplayShuffleKindV1.Deck,
                location: 0x01);
        }

        if (messageId == MsgShuffleHand || messageId == MsgShuffleExtra)
        {
            if (bytes.Length < 6)
            {
                return Failure(GameplayErrorCode.MalformedGameMessage);
            }

            GameplayErrorCode playerError = ValidatePlayer(bytes[1]);
            if (playerError != GameplayErrorCode.None)
            {
                return Failure(playerError);
            }

            uint count = BinaryPrimitives.ReadUInt32LittleEndian(bytes[2..6]);
            ulong required = 6ul + ((ulong)count * sizeof(uint));
            if (required > int.MaxValue || required != (ulong)bytes.Length)
            {
                return Failure(GameplayErrorCode.QueryLengthMismatch);
            }

            List<uint> cardCodes = new((int)count);
            int offset = 6;
            for (uint index = 0; index < count; index++)
            {
                cardCodes.Add(BinaryPrimitives.ReadUInt32LittleEndian(
                    bytes.Slice(offset, sizeof(uint))));
                offset += sizeof(uint);
            }

            return Success(GameplayMessageV1.FromShuffle(
                messageId,
                new GameplayShufflePayloadV1(
                    messageId == MsgShuffleHand
                        ? GameplayShuffleKindV1.Hand
                        : GameplayShuffleKindV1.Extra,
                    bytes[1],
                    messageId == MsgShuffleHand ? (byte)0x02 : (byte)0x40,
                    cardCodes)));
        }

        if (messageId != MsgShuffleSetCard || bytes.Length < 3)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        byte location = bytes[1];
        byte countByte = bytes[2];
        if (location is not 0x04 and not 0x08 || countByte == 0)
        {
            return Failure(GameplayErrorCode.InvalidLocation);
        }

        ulong requiredLength = 3ul +
                               ((ulong)countByte *
                                GameplayWirePrimitivesV1.ModernLocInfoByteLength *
                                2ul);
        if (requiredLength != (ulong)bytes.Length)
        {
            return Failure(GameplayErrorCode.QueryLengthMismatch);
        }

        List<ModernLocInfoV1> previous = new(countByte);
        int offsetForPrevious = 3;
        byte? player = null;
        for (int index = 0; index < countByte; index++)
        {
            if (!TryReadLoc(
                    bytes,
                    offsetForPrevious,
                    out ModernLocInfoV1 value,
                    out GameplayErrorCode error))
            {
                return Failure(error);
            }

            if (value.Location != location)
            {
                return Failure(GameplayErrorCode.InvalidLocation);
            }

            if (!player.HasValue)
            {
                player = value.Controller;
            }
            else if (player.Value != value.Controller)
            {
                return Failure(GameplayErrorCode.InvalidParticipant);
            }

            previous.Add(value);
            offsetForPrevious += GameplayWirePrimitivesV1.ModernLocInfoByteLength;
        }

        if (!player.HasValue)
        {
            return Failure(GameplayErrorCode.InvalidParticipant);
        }

        byte shufflePlayer = player.Value;
        List<ModernLocInfoV1> current = new(countByte);
        for (int index = 0; index < countByte; index++)
        {
            if (!TryReadLoc(
                    bytes,
                    offsetForPrevious,
                    out ModernLocInfoV1 value,
                    out GameplayErrorCode error))
            {
                return Failure(error);
            }

            bool isZero = value.Controller == 0 &&
                          value.Location == 0 &&
                          value.Sequence == 0 &&
                          value.Position == 0;
            if (!isZero &&
                (value.Controller != shufflePlayer || value.Location != location))
            {
                return Failure(GameplayErrorCode.InvalidStateTransition);
            }

            current.Add(value);
            offsetForPrevious += GameplayWirePrimitivesV1.ModernLocInfoByteLength;
        }

        return Success(GameplayMessageV1.FromShuffle(
            messageId,
            new GameplayShufflePayloadV1(
                GameplayShuffleKindV1.SetCard,
                player,
                location,
                previous: previous,
                current: current)));
    }

    private GameplayMessageDecodeResult DecodePlayerShuffle(
        ReadOnlySpan<byte> bytes,
        byte messageId,
        GameplayShuffleKindV1 kind,
        byte location)
    {
        if (bytes.Length != 2)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        GameplayErrorCode playerError = ValidatePlayer(bytes[1]);
        return playerError == GameplayErrorCode.None
            ? Success(GameplayMessageV1.FromShuffle(
                messageId,
                new GameplayShufflePayloadV1(kind, bytes[1], location)))
            : Failure(playerError);
    }

    private GameplayMessageDecodeResult DecodeCounter(
        ReadOnlySpan<byte> bytes,
        byte messageId,
        GameplayMessageKindV1 kind)
    {
        if (bytes.Length != 8)
        {
            return Failure(GameplayErrorCode.MalformedGameMessage);
        }

        if (bytes[4] == 0)
        {
            return Failure(GameplayErrorCode.InvalidLocation);
        }

        GameplayErrorCode playerError = ValidatePlayer(bytes[3]);
        return playerError == GameplayErrorCode.None
            ? Success(GameplayMessageV1.FromCounter(
                messageId,
                kind,
                new GameplayCounterPayloadV1(
                    BinaryPrimitives.ReadUInt16LittleEndian(bytes[1..3]),
                    bytes[3],
                    bytes[4],
                    bytes[5],
                    BinaryPrimitives.ReadUInt16LittleEndian(bytes[6..8]))))
            : Failure(playerError);
    }

    private static bool IsI3BMessage(byte messageId) =>
        messageId is MsgWin or
            MsgUpdateData or
            MsgUpdateCard or
            MsgNewTurn or
            MsgNewPhase or
            MsgMove or
            MsgPosChange or
            MsgSet or
            MsgSwap or
            MsgSwapGraveDeck or
            MsgChaining or
            MsgChained or
            MsgChainSolving or
            MsgChainSolved or
            MsgChainEnd or
            MsgChainNegated or
            MsgChainDisabled or
            MsgBecomeTarget or
            MsgDraw or
            MsgDamage or
            MsgRecover or
            MsgEquip or
            MsgLpUpdate or
            MsgUnequip or
            MsgCardTarget or
            MsgCancelTarget or
            MsgPayLpCost or
            MsgSummoning or
            MsgSummoned or
            MsgSpecialSummoning or
            MsgSpecialSummoned or
            MsgFlipSummoning or
            MsgFlipSummoned or
            MsgConfirmCards or
            MsgConfirmDeckTop or
            MsgConfirmExtraTop or
            MsgShuffleDeck or
            MsgShuffleHand or
            MsgShuffleExtra or
            MsgShuffleSetCard or
            MsgReverseDeck or
            MsgAddCounter or
            MsgRemoveCounter;

    private static GameplayErrorCode ValidatePlayer(byte player) =>
        player <= 1
            ? GameplayErrorCode.None
            : GameplayErrorCode.InvalidParticipant;

    private static bool TryReadLoc(
        ReadOnlySpan<byte> bytes,
        int offset,
        out ModernLocInfoV1 value,
        out GameplayErrorCode error)
    {
        value = default;
        error = GameplayErrorCode.None;
        if (offset < 0 || bytes.Length - offset < GameplayWirePrimitivesV1.ModernLocInfoByteLength)
        {
            error = GameplayErrorCode.MalformedGameMessage;
            return false;
        }

        if (!GameplayWirePrimitivesV1.TryDecodeModernLocInfo(
                bytes.Slice(offset, GameplayWirePrimitivesV1.ModernLocInfoByteLength),
                out value,
                out error))
        {
            return false;
        }

        error = ValidatePlayer(value.Controller);
        return error == GameplayErrorCode.None;
    }

    private static bool TryReadCompactLoc(
        ReadOnlySpan<byte> bytes,
        int offset,
        out ModernLocInfoV1 value,
        out GameplayErrorCode error)
    {
        value = default;
        error = GameplayErrorCode.None;
        if (offset < 0 || bytes.Length - offset < 6)
        {
            error = GameplayErrorCode.MalformedGameMessage;
            return false;
        }

        value = new ModernLocInfoV1(
            bytes[offset],
            bytes[offset + 1],
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset + 2, 4)),
            0x05);
        error = ValidatePlayer(value.Controller);
        return error == GameplayErrorCode.None;
    }

    private GameplayMessageDecodeResult Success(GameplayMessageV1 message) =>
        GameplayMessageDecodeResult.Success(message, perspective!);

    private GameplayMessageDecodeResult Failure(GameplayErrorCode error) =>
        GameplayMessageDecodeResult.Failure(error, perspective);
}
