using OCGForge.Ignis.Protocol;

namespace OCGForge.Ignis.Client;

public sealed class RoomPasswordV1
{
    private readonly string value;

    private RoomPasswordV1(string value)
    {
        this.value = value;
    }

    public static RoomPasswordV1 Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        try
        {
            FixedUtf16String.Encode(value, ProtocolContractV1.FixedTextCodeUnits);
        }
        catch (ProtocolCodecException exception)
        {
            throw new ClientConfigurationException(
                I2ErrorCode.InvalidConfiguration,
                "The room password is not representable by the V1 protocol field.",
                exception);
        }

        return new RoomPasswordV1(value);
    }

    internal string Value => value;

    public override string ToString() => "[REDACTED]";
}

public sealed class ConnectionConfigurationV1
{
    private static readonly TimeSpan MaximumConnectionTimeout =
        TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    public ConnectionConfigurationV1(
        string host,
        int port,
        string playerName,
        uint gameId,
        RoomPasswordV1 roomPassword,
        TimeSpan connectionTimeout)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(playerName);
        ArgumentNullException.ThrowIfNull(roomPassword);

        if (string.IsNullOrWhiteSpace(host) || port is < 1 or > 65535)
        {
            throw Invalid("The host or port is invalid.");
        }

        try
        {
            FixedUtf16String.Encode(
                playerName,
                ProtocolContractV1.FixedTextCodeUnits);
        }
        catch (ProtocolCodecException exception)
        {
            throw new ClientConfigurationException(
                I2ErrorCode.InvalidConfiguration,
                "The player name is not representable by the V1 protocol field.",
                exception);
        }

        if (connectionTimeout <= TimeSpan.Zero ||
            connectionTimeout > MaximumConnectionTimeout)
        {
            throw Invalid("The connection timeout must be positive and finite.");
        }

        Host = host;
        Port = port;
        PlayerName = playerName;
        GameId = gameId;
        RoomPassword = roomPassword;
        ConnectionTimeout = connectionTimeout;
    }

    public string Host { get; }

    public int Port { get; }

    public string PlayerName { get; }

    public uint GameId { get; }

    public RoomPasswordV1 RoomPassword { get; }

    public TimeSpan ConnectionTimeout { get; }

    public override string ToString() =>
        $"ConnectionConfigurationV1(Host={Host}, Port={Port}, " +
        $"PlayerName={PlayerName}, GameId={GameId}, " +
        "RoomPassword=[REDACTED], " +
        $"ConnectionTimeout={ConnectionTimeout})";

    private static ClientConfigurationException Invalid(string message) =>
        new(I2ErrorCode.InvalidConfiguration, message);
}
