namespace OCGForge.Ignis.Client;

public enum I2SessionState : byte
{
    Created = 0,
    Connecting = 1,
    TransportConnected = 2,
    PlayerInfoSent = 3,
    JoinRequestSent = 4,
    LobbyJoined = 5,
    DeckSubmitted = 6,
    ReadyRequested = 7,
    Ready = 8,
    NotReadyRequested = 9,
    Starting = 10,
    DuelStarted = 11,
    WaitingForHandChoice = 12,
    WaitingForHandResult = 13,
    WaitingForTpRequest = 14,
    WaitingForTpChoice = 15,
    HandedOff = 16,
    Closed = 17,
    Failed = 18
}
