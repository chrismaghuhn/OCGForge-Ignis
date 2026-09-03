namespace OCGForge.Ignis.Client;

public enum I2EventKind : byte
{
    TransportConnected = 0,
    PlayerInfoSent = 1,
    JoinRequestSent = 2,
    LobbyJoined = 3,
    OwnTypeChanged = 4,
    PlayerEntered = 5,
    PlayerStatusChanged = 6,
    WatcherCountChanged = 7,
    DeckSubmitted = 8,
    ReadyRequested = 9,
    NotReadyRequested = 10,
    DuelStartRequested = 11,
    DuelStarted = 12,
    PlayerMoved = 13,
    RpsRequested = 14,
    RpsChoiceSent = 15,
    RpsResultReceived = 16,
    TurnPreferenceRequested = 17,
    TurnPreferenceSent = 18,
    HandedOff = 19,
    Failed = 20,
    Closed = 21
}

public sealed record I2Event(
    I2EventKind Kind,
    byte? Position = null,
    string? Name = null,
    byte? Status = null,
    byte? Value = null,
    PreDuelChoiceTokenV1? ChoiceToken = null,
    I2ErrorCode Error = I2ErrorCode.None,
    ushort? WatcherCount = null);
