namespace OCGForge.Ignis.Client;

public enum I2SessionState : byte
{
    Created = 0,
    Connecting = 1,
    TransportConnected = 2,
    PlayerInfoSent = 3,
    JoinRequestSent = 4,
    JoinAccepted = 5,
    LobbyJoined = 6,
    DeckSubmitted = 7,
    ReadyRequested = 8,
    Ready = 9,
    NotReadyRequested = 10,
    Starting = 11,
    DuelStarted = 12,
    WaitingForHandChoice = 13,
    WaitingForHandResult = 14,
    WaitingForTpRequest = 15,
    WaitingForTpChoice = 16,
    HandedOff = 17,
    Closed = 18,
    Failed = 19
}
