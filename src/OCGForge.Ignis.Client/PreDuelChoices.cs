using System.Collections.ObjectModel;

namespace OCGForge.Ignis.Client;

public enum PreDuelChoiceKind : byte
{
    Rps = 0,
    TurnPreference = 1
}

public readonly record struct PreDuelChoiceTokenV1(ulong Ordinal);

public sealed class PreDuelChoiceRequest
{
    private readonly byte[] legalValues;
    private readonly ReadOnlyCollection<byte> legalValuesView;

    public PreDuelChoiceRequest(
        PreDuelChoiceKind kind,
        PreDuelChoiceTokenV1 token,
        IEnumerable<byte> legalValues)
    {
        ArgumentNullException.ThrowIfNull(legalValues);
        this.legalValues = legalValues.ToArray();
        legalValuesView = Array.AsReadOnly(this.legalValues);
        Kind = kind;
        Token = token;
    }

    public PreDuelChoiceKind Kind { get; }

    public PreDuelChoiceTokenV1 Token { get; }

    public IReadOnlyList<byte> LegalValues => legalValuesView;
}

public enum PreDuelOutcome : byte
{
    RpsWin = 0,
    RpsLoss = 1
}
