namespace OCGForge.Ignis.Protocol;

public enum PayloadContractKind : byte
{
    ExactEmpty = 0,
    ExactTypedLayout = 1,
    Opaque = 2
}

public sealed class ValidatedCtosPacket
{
    internal ValidatedCtosPacket(
        CtosPacketType type,
        PayloadContractKind payloadContract,
        object? payload)
    {
        Type = type;
        PayloadContract = payloadContract;
        Payload = payload;
    }

    public CtosPacketType Type { get; }

    public PayloadContractKind PayloadContract { get; }

    public object? Payload { get; }
}

public sealed class ValidatedStocPacket
{
    internal ValidatedStocPacket(
        StocPacketType type,
        PayloadContractKind payloadContract,
        object? payload)
    {
        Type = type;
        PayloadContract = payloadContract;
        Payload = payload;
    }

    public StocPacketType Type { get; }

    public PayloadContractKind PayloadContract { get; }

    public object? Payload { get; }
}
