namespace OCGForge.Ignis.Protocol;

public static class ProtocolContractV1
{
    public const string Id = "ocgforge-ignis.protocol.wire.v1";
    public const int LengthPrefixSize = 2;
    public const int PacketTypeSize = 1;
    public const int MaxPacketLength = ushort.MaxValue;
    public const int MaxPayloadLength = MaxPacketLength - PacketTypeSize;
    public const ushort ExpectedProVersion = 0x1354;
    public const byte ExpectedEdoproVersionMajor = 41;
    public const byte ExpectedEdoproVersionMinor = 0;
    public const byte ExpectedEdoproVersionPatch = 2;
    public const byte ExpectedOcgVersionMajor = 11;
    public const byte ExpectedOcgVersionMinor = 0;
    public const int FixedTextCodeUnits = 20;

    public static ProtocolClientVersion ExpectedClientVersion =>
        new(
            ExpectedEdoproVersionMajor,
            ExpectedEdoproVersionMinor,
            ExpectedOcgVersionMajor,
            ExpectedOcgVersionMinor);
}
