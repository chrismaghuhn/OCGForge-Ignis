using System.Buffers.Binary;

namespace OCGForge.Ignis.Protocol;

public sealed class CtosFrame
{
    private readonly byte[] payload;

    internal CtosFrame(CtosPacketType type, ReadOnlySpan<byte> payload)
    {
        Type = type;
        this.payload = payload.ToArray();
    }

    public CtosPacketType Type { get; }

    public ReadOnlyMemory<byte> Payload => payload;
}

public sealed class StocFrame
{
    private readonly byte[] payload;

    internal StocFrame(StocPacketType type, ReadOnlySpan<byte> payload)
    {
        Type = type;
        this.payload = payload.ToArray();
    }

    public StocPacketType Type { get; }

    public ReadOnlyMemory<byte> Payload => payload;
}

public static class WireFrameCodec
{
    public static byte[] EncodeCtos(CtosPacketType type, ReadOnlySpan<byte> payload)
    {
        EnsureSupportedCtos(type);
        return Encode((byte)type, payload);
    }

    public static byte[] EncodeStoc(StocPacketType type, ReadOnlySpan<byte> payload)
    {
        PacketTypeDisposition disposition = PacketTypeCatalog.ClassifyStoc((byte)type);
        if (disposition == PacketTypeDisposition.ExplicitlyUnsupported)
        {
            throw new ProtocolCodecException(
                ProtocolErrorCode.UnsupportedPacketType,
                "The STOC packet type is explicitly unsupported in V1.");
        }

        if (disposition != PacketTypeDisposition.Supported)
        {
            throw new ProtocolCodecException(
                ProtocolErrorCode.UnknownPacketType,
                "The STOC packet type is not part of the frozen V1 set.");
        }

        return Encode((byte)type, payload);
    }

    // This method returns a raw frame after structural direction/type checks.
    // Use PacketPayloadValidator for the type-aware validated packet boundary.
    public static FrameReadResult<CtosFrame> TryReadCtos(ReadOnlySpan<byte> buffer) =>
        TryReadCtosCore(buffer);

    // This method returns a raw frame after structural direction/type checks.
    // Use PacketPayloadValidator for the type-aware validated packet boundary.
    public static FrameReadResult<StocFrame> TryReadStoc(ReadOnlySpan<byte> buffer) =>
        TryReadStocCore(buffer);

    public static ProtocolErrorCode EndOfStream(ReadOnlySpan<byte> pendingBytes) =>
        pendingBytes.IsEmpty
            ? ProtocolErrorCode.None
            : ProtocolErrorCode.TruncatedFrame;

    private static byte[] Encode(byte type, ReadOnlySpan<byte> payload)
    {
        if (payload.Length > ProtocolContractV1.MaxPayloadLength)
        {
            throw new ProtocolCodecException(
                ProtocolErrorCode.OversizedPacket,
                "The payload exceeds the representable V1 frame capacity.");
        }

        int packetLength = checked(ProtocolContractV1.PacketTypeSize + payload.Length);
        byte[] frame = new byte[checked(ProtocolContractV1.LengthPrefixSize + packetLength)];
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(0, 2), checked((ushort)packetLength));
        frame[2] = type;
        payload.CopyTo(frame.AsSpan(3));
        return frame;
    }

    private static FrameReadResult<CtosFrame> TryReadCtosCore(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < ProtocolContractV1.LengthPrefixSize)
        {
            return FrameReadResults.NeedMoreData<CtosFrame>();
        }

        ushort packetLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer[..2]);
        if (packetLength == 0)
        {
            return FrameReadResults.Invalid<CtosFrame>(ProtocolErrorCode.InvalidPacketLength);
        }

        int totalLength = checked(ProtocolContractV1.LengthPrefixSize + packetLength);
        if (buffer.Length < totalLength)
        {
            return FrameReadResults.NeedMoreData<CtosFrame>();
        }

        byte rawType = buffer[2];
        if (PacketTypeCatalog.ClassifyCtos(rawType) != PacketTypeDisposition.Supported)
        {
            return FrameReadResults.Invalid<CtosFrame>(ProtocolErrorCode.UnknownPacketType);
        }

        int payloadLength = packetLength - ProtocolContractV1.PacketTypeSize;
        CtosFrame frame = new((CtosPacketType)rawType, buffer.Slice(3, payloadLength));
        return FrameReadResults.Success(totalLength, frame);
    }

    private static FrameReadResult<StocFrame> TryReadStocCore(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < ProtocolContractV1.LengthPrefixSize)
        {
            return FrameReadResults.NeedMoreData<StocFrame>();
        }

        ushort packetLength = BinaryPrimitives.ReadUInt16LittleEndian(buffer[..2]);
        if (packetLength == 0)
        {
            return FrameReadResults.Invalid<StocFrame>(ProtocolErrorCode.InvalidPacketLength);
        }

        int totalLength = checked(ProtocolContractV1.LengthPrefixSize + packetLength);
        if (buffer.Length < totalLength)
        {
            return FrameReadResults.NeedMoreData<StocFrame>();
        }

        byte rawType = buffer[2];
        PacketTypeDisposition disposition = PacketTypeCatalog.ClassifyStoc(rawType);
        if (disposition == PacketTypeDisposition.ExplicitlyUnsupported)
        {
            return FrameReadResults.Invalid<StocFrame>(ProtocolErrorCode.UnsupportedPacketType);
        }

        if (disposition != PacketTypeDisposition.Supported)
        {
            return FrameReadResults.Invalid<StocFrame>(ProtocolErrorCode.UnknownPacketType);
        }

        int payloadLength = packetLength - ProtocolContractV1.PacketTypeSize;
        StocFrame frame = new((StocPacketType)rawType, buffer.Slice(3, payloadLength));
        return FrameReadResults.Success(totalLength, frame);
    }

    private static void EnsureSupportedCtos(CtosPacketType type)
    {
        if (!PacketTypeCatalog.IsSupported(type))
        {
            throw new ProtocolCodecException(
                ProtocolErrorCode.UnknownPacketType,
                "The CTOS packet type is not part of the frozen V1 set.");
        }
    }
}
