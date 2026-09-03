namespace OCGForge.Ignis.Protocol;

public enum PacketDirection : byte
{
    Ctos = 0,
    Stoc = 1
}

public enum CtosPacketType : byte
{
    Response = 0x01,
    UpdateDeck = 0x02,
    HandResult = 0x03,
    TpResult = 0x04,
    PlayerInfo = 0x10,
    JoinGame = 0x12,
    LeaveGame = 0x13,
    Surrender = 0x14,
    TimeConfirm = 0x15,
    HsReady = 0x22,
    HsNotReady = 0x23,
    HsStart = 0x25
}

public enum StocPacketType : byte
{
    GameMsg = 0x01,
    ErrorMsg = 0x02,
    SelectHand = 0x03,
    SelectTp = 0x04,
    HandResult = 0x05,
    TpResult = 0x06,
    JoinGame = 0x12,
    TypeChange = 0x13,
    LeaveGame = 0x14,
    DuelStart = 0x15,
    DuelEnd = 0x16,
    TimeLimit = 0x18,
    HsPlayerEnter = 0x20,
    HsPlayerChange = 0x21,
    HsWatchChange = 0x22,
    Catchup = 0xf0,
    Rematch = 0xf1,
    WaitingRematch = 0xf2
}

public enum PacketTypeDisposition : byte
{
    Supported = 0,
    ExplicitlyUnsupported = 1,
    Unknown = 2
}

public static class PacketTypeCatalog
{
    public static PacketTypeDisposition ClassifyCtos(byte rawType) =>
        rawType switch
        {
            (byte)CtosPacketType.Response or
            (byte)CtosPacketType.UpdateDeck or
            (byte)CtosPacketType.HandResult or
            (byte)CtosPacketType.TpResult or
            (byte)CtosPacketType.PlayerInfo or
            (byte)CtosPacketType.JoinGame or
            (byte)CtosPacketType.LeaveGame or
            (byte)CtosPacketType.Surrender or
            (byte)CtosPacketType.TimeConfirm or
            (byte)CtosPacketType.HsReady or
            (byte)CtosPacketType.HsNotReady or
            (byte)CtosPacketType.HsStart => PacketTypeDisposition.Supported,
            _ => PacketTypeDisposition.Unknown
        };

    public static PacketTypeDisposition ClassifyStoc(byte rawType) =>
        rawType switch
        {
            (byte)StocPacketType.GameMsg or
            (byte)StocPacketType.ErrorMsg or
            (byte)StocPacketType.SelectHand or
            (byte)StocPacketType.SelectTp or
            (byte)StocPacketType.HandResult or
            (byte)StocPacketType.TpResult or
            (byte)StocPacketType.JoinGame or
            (byte)StocPacketType.TypeChange or
            (byte)StocPacketType.LeaveGame or
            (byte)StocPacketType.DuelStart or
            (byte)StocPacketType.DuelEnd or
            (byte)StocPacketType.TimeLimit or
            (byte)StocPacketType.HsPlayerEnter or
            (byte)StocPacketType.HsPlayerChange or
            (byte)StocPacketType.HsWatchChange => PacketTypeDisposition.Supported,
            (byte)StocPacketType.Catchup or
            (byte)StocPacketType.Rematch or
            (byte)StocPacketType.WaitingRematch => PacketTypeDisposition.ExplicitlyUnsupported,
            _ => PacketTypeDisposition.Unknown
        };

    public static bool IsSupported(CtosPacketType type) =>
        ClassifyCtos((byte)type) == PacketTypeDisposition.Supported;

    public static bool IsSupported(StocPacketType type) =>
        ClassifyStoc((byte)type) == PacketTypeDisposition.Supported;
}
