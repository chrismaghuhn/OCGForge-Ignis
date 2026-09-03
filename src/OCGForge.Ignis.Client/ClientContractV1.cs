using System.Collections.ObjectModel;

namespace OCGForge.Ignis.Client;

public static class ClientContractV1
{
    public const string Id = "ocgforge-ignis.client.preduel.v1";
    public const uint DuelRelayFlag = 0x00000080;
    public const int BestOfRequired = 1;
    public const uint ExpectedServerHandshake = 4043399681u;
    public const int ExpectedTeam1Size = 1;
    public const int ExpectedTeam2Size = 1;
    public const byte FirstDuelistPosition = 0;
    public const byte SecondDuelistPosition = 1;
    public const byte ObserverPosition = 7;
    public const int MaxReceiveBufferBytes =
        Protocol.ProtocolContractV1.MaxPacketLength +
        Protocol.ProtocolContractV1.LengthPrefixSize;

    private static readonly ReadOnlyCollection<byte> rpsValues =
        Array.AsReadOnly(new byte[] { 1, 2, 3 });
    private static readonly ReadOnlyCollection<byte> turnPreferenceValues =
        Array.AsReadOnly(new byte[] { 0, 1 });

    public static IReadOnlyList<byte> RpsValues => rpsValues;

    public static IReadOnlyList<byte> TurnPreferenceValues => turnPreferenceValues;
}
