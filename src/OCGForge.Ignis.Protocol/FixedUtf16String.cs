using System.Buffers.Binary;
using System.Text;

namespace OCGForge.Ignis.Protocol;

public static class FixedUtf16String
{
    private static readonly UnicodeEncoding LittleEndianUtf16 =
        new(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);

    public static byte[] Encode(string? value, int codeUnits)
    {
        if (value is null || codeUnits <= 0 || value.Contains('\0'))
        {
            throw new ProtocolCodecException(
                ProtocolErrorCode.InvalidFixedString,
                "The fixed UTF-16 value is null, empty-width, or contains a terminator.");
        }

        if (codeUnits > int.MaxValue / 2)
        {
            throw new ProtocolCodecException(
                ProtocolErrorCode.IntegerOverflow,
                "The fixed UTF-16 width cannot be represented safely.");
        }

        if (codeUnits > ProtocolContractV1.MaxPayloadLength / 2)
        {
            throw new ProtocolCodecException(
                ProtocolErrorCode.OversizedPacket,
                "The fixed UTF-16 width cannot fit in a V1 payload.");
        }

        byte[] encoded;
        try
        {
            encoded = LittleEndianUtf16.GetBytes(value);
        }
        catch (EncoderFallbackException exception)
        {
            throw new ProtocolCodecException(
                ProtocolErrorCode.InvalidFixedString,
                "The fixed UTF-16 value contains an invalid surrogate sequence.",
                exception);
        }

        int maxContentBytes = checked((codeUnits - 1) * 2);
        if (encoded.Length > maxContentBytes)
        {
            throw new ProtocolCodecException(
                ProtocolErrorCode.InvalidFixedString,
                "The fixed UTF-16 value cannot fit with a terminating code unit.");
        }

        byte[] result = new byte[checked(codeUnits * 2)];
        encoded.CopyTo(result, 0);
        return result;
    }

    public static PayloadDecodeResult<string> Decode(
        ReadOnlySpan<byte> payload,
        int codeUnits)
    {
        if (codeUnits <= 0 || codeUnits > int.MaxValue / 2)
        {
            return PayloadDecodeResults.Failure<string>(
                ProtocolErrorCode.IntegerOverflow);
        }

        int expectedLength = codeUnits * 2;
        if (payload.Length != expectedLength)
        {
            return PayloadDecodeResults.Failure<string>(
                payload.Length > expectedLength
                    ? ProtocolErrorCode.TrailingPayloadBytes
                    : ProtocolErrorCode.PayloadLengthMismatch);
        }

        int terminatorOffset = -1;
        for (int offset = 0; offset < expectedLength; offset += 2)
        {
            if (BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(offset, 2)) == 0)
            {
                terminatorOffset = offset;
                break;
            }
        }

        if (terminatorOffset < 0)
        {
            return PayloadDecodeResults.Failure<string>(
                ProtocolErrorCode.InvalidFixedString);
        }

        string decoded;
        try
        {
            decoded = LittleEndianUtf16.GetString(payload[..terminatorOffset]);
        }
        catch (DecoderFallbackException)
        {
            return PayloadDecodeResults.Failure<string>(ProtocolErrorCode.InvalidFixedString);
        }

        return PayloadDecodeResults.Success(decoded);
    }
}
