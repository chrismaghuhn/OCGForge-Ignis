using System.Collections.ObjectModel;

namespace OCGForge.Ignis.Protocol;

public readonly record struct ProtocolClientVersion(
    byte ClientMajor,
    byte ClientMinor,
    byte CoreMajor,
    byte CoreMinor);

public static class VersionRecognition
{
    public static ProtocolErrorCode ValidateV1(
        ushort proVersion,
        ProtocolClientVersion clientVersion) =>
        proVersion == ProtocolContractV1.ExpectedProVersion &&
        clientVersion == ProtocolContractV1.ExpectedClientVersion
            ? ProtocolErrorCode.None
            : ProtocolErrorCode.UnsupportedVersion;
}

public enum ErrorType : byte
{
    JoinError = 0x01,
    DeckError = 0x02,
    SideError = 0x03,
    VersionError = 0x04,
    VersionError2 = 0x05
}

public readonly record struct DeckSizeLimits(ushort Min, ushort Max);

public readonly record struct CtosPlayerInfoPayload(string Name);

public readonly record struct CtosHandResultPayload(byte Result);

public readonly record struct CtosTpResultPayload(byte Result);

public readonly record struct CtosJoinGamePayload(
    ushort ProtocolVersion,
    byte Reserved0,
    byte Reserved1,
    uint GameId,
    string Password,
    ProtocolClientVersion ClientVersion);

public sealed class CtosUpdateDeckPayload
{
    private readonly uint[] mainAndExtraCards;
    private readonly uint[] sideCards;
    private readonly ReadOnlyCollection<uint> mainAndExtraView;
    private readonly ReadOnlyCollection<uint> sideView;

    public CtosUpdateDeckPayload(
        IEnumerable<uint> mainAndExtraCards,
        IEnumerable<uint> sideCards)
    {
        ArgumentNullException.ThrowIfNull(mainAndExtraCards);
        ArgumentNullException.ThrowIfNull(sideCards);

        this.mainAndExtraCards = mainAndExtraCards.ToArray();
        this.sideCards = sideCards.ToArray();
        mainAndExtraView = Array.AsReadOnly(this.mainAndExtraCards);
        sideView = Array.AsReadOnly(this.sideCards);
    }

    public IReadOnlyList<uint> MainAndExtraCards => mainAndExtraView;

    public IReadOnlyList<uint> SideCards => sideView;

    internal ReadOnlySpan<uint> MainAndExtraSpan => mainAndExtraCards;

    internal ReadOnlySpan<uint> SideSpan => sideCards;
}

public class OpaquePayload
{
    private readonly byte[] bytes;

    public OpaquePayload(ReadOnlySpan<byte> bytes)
    {
        this.bytes = bytes.ToArray();
    }

    public int Length => bytes.Length;

    public ReadOnlyMemory<byte> Bytes => bytes;

    internal ReadOnlySpan<byte> AsSpan() => bytes;
}

public sealed class CtosResponsePayload : OpaquePayload
{
    public CtosResponsePayload(ReadOnlySpan<byte> bytes)
        : base(bytes)
    {
    }
}

public sealed class StocGameMessagePayload : OpaquePayload
{
    public StocGameMessagePayload(ReadOnlySpan<byte> bytes)
        : base(bytes)
    {
    }
}

public sealed class StocErrorMessagePayload
{
    public StocErrorMessagePayload(
        ErrorType type,
        byte reserved0,
        byte reserved1,
        byte reserved2,
        uint code,
        OpaquePayload additionalPayload)
    {
        ArgumentNullException.ThrowIfNull(additionalPayload);
        Type = type;
        Reserved0 = reserved0;
        Reserved1 = reserved1;
        Reserved2 = reserved2;
        Code = code;
        AdditionalPayload = additionalPayload;
    }

    public ErrorType Type { get; }

    public byte Reserved0 { get; }

    public byte Reserved1 { get; }

    public byte Reserved2 { get; }

    public uint Code { get; }

    public OpaquePayload AdditionalPayload { get; }
}

public readonly record struct HostInfoPayload(
    uint BanlistId,
    byte Rule,
    byte Mode,
    byte DuelRule,
    byte NoCheckDeckContent,
    byte NoShuffleDeck,
    byte Reserved0,
    byte Reserved1,
    byte Reserved2,
    uint StartLp,
    byte StartHand,
    byte DrawCount,
    ushort TimeLimit,
    uint DuelFlagHigh,
    uint Handshake,
    ProtocolClientVersion Version,
    int Team1,
    int Team2,
    int BestOf,
    uint DuelFlagLow,
    uint ForbiddenTypes,
    ushort ExtraRules,
    DeckSizeLimits MainDeck,
    DeckSizeLimits ExtraDeck,
    DeckSizeLimits SideDeck,
    byte TrailingReserved0,
    byte TrailingReserved1);

public readonly record struct StocHandResultPayload(byte Result1, byte Result2);

public readonly record struct StocTypeChangePayload(byte Type);

public readonly record struct StocTimeLimitPayload(
    byte Player,
    byte Reserved,
    ushort LeftTime);

public readonly record struct StocHsPlayerEnterPayload(
    string Name,
    byte Position,
    byte Reserved);

public readonly record struct StocHsPlayerChangePayload(byte Status);

public readonly record struct StocHsWatchChangePayload(ushort WatchCount);
