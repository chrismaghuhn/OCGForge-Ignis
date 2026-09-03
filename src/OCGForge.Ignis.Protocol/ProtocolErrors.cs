namespace OCGForge.Ignis.Protocol;

public enum ProtocolErrorCode : byte
{
    None = 0,
    TruncatedFrame = 1,
    InvalidPacketLength = 2,
    OversizedPacket = 3,
    UnknownPacketType = 4,
    UnsupportedPacketType = 5,
    PayloadLengthMismatch = 6,
    InvalidFixedString = 7,
    UnsupportedVersion = 8,
    TrailingPayloadBytes = 9,
    IntegerOverflow = 10
}

public enum FrameReadStatus : byte
{
    Success = 0,
    NeedMoreData = 1,
    Invalid = 2
}

public readonly record struct FrameReadResult<T>(
    FrameReadStatus Status,
    int ConsumedBytes,
    T? Frame,
    ProtocolErrorCode Error)
    where T : class
{
}

public readonly record struct PayloadDecodeResult<T>(
    bool IsSuccess,
    T Value,
    ProtocolErrorCode Error)
{
}

public static class FrameReadResults
{
    public static FrameReadResult<T> NeedMoreData<T>()
        where T : class =>
        new(FrameReadStatus.NeedMoreData, 0, null, ProtocolErrorCode.None);

    public static FrameReadResult<T> Invalid<T>(ProtocolErrorCode error)
        where T : class =>
        new(FrameReadStatus.Invalid, 0, null, error);

    public static FrameReadResult<T> Success<T>(int consumedBytes, T frame)
        where T : class =>
        new(FrameReadStatus.Success, consumedBytes, frame, ProtocolErrorCode.None);
}

public static class PayloadDecodeResults
{
    public static PayloadDecodeResult<T> Success<T>(T value) =>
        new(true, value, ProtocolErrorCode.None);

    public static PayloadDecodeResult<T> Failure<T>(ProtocolErrorCode error) =>
        new(false, default!, error);
}

public sealed class ProtocolCodecException : Exception
{
    public ProtocolCodecException(ProtocolErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public ProtocolCodecException(
        ProtocolErrorCode code,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    public ProtocolErrorCode Code { get; }
}
