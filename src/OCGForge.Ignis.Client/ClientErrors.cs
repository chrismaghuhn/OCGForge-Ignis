namespace OCGForge.Ignis.Client;

public enum I2ErrorCode : byte
{
    None = 0,
    InvalidConfiguration = 1,
    InvalidStateTransition = 2,
    ConnectionFailed = 3,
    ConnectionTimeout = 4,
    Cancelled = 5,
    RemoteClosed = 6,
    TruncatedStream = 7,
    ProtocolFailure = 8,
    UnsupportedPacket = 9,
    UnexpectedPacketForState = 10,
    VersionMismatch = 11,
    ServerHandshakeMismatch = 12,
    UnsupportedRoomTopology = 13,
    JoinRejected = 14,
    DeckRejected = 15,
    SideFlowUnsupported = 16,
    ChoiceNotPending = 17,
    StaleChoice = 18,
    InvalidChoice = 19,
    SendFailed = 20,
    ReceiveBufferOverflow = 21,
    TransportOwnershipError = 22,
    ServerLeft = 23,
    UnsupportedLobbyPositionMove = 24,
    ChoiceOrdinalOverflow = 25
}

public readonly record struct I2Result(
    bool IsSuccess,
    I2ErrorCode Error)
{
    public static I2Result Success() => new(true, I2ErrorCode.None);

    public static I2Result Failure(I2ErrorCode error) =>
        new(false, error);
}

public sealed class ClientConfigurationException : Exception
{
    public ClientConfigurationException(I2ErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public ClientConfigurationException(
        I2ErrorCode code,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    public I2ErrorCode Code { get; }
}
