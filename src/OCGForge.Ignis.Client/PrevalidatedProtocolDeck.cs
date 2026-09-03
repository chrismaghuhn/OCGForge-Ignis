using System.Collections.ObjectModel;
using OCGForge.Ignis.Protocol;

namespace OCGForge.Ignis.Client;

public sealed class PrevalidatedProtocolDeck
{
    private readonly uint[] mainAndExtraCards;
    private readonly uint[] sideCards;
    private readonly ReadOnlyCollection<uint> mainAndExtraView;
    private readonly ReadOnlyCollection<uint> sideView;

    public PrevalidatedProtocolDeck(
        IEnumerable<uint> mainAndExtraCards,
        IEnumerable<uint> sideCards)
    {
        ArgumentNullException.ThrowIfNull(mainAndExtraCards);
        ArgumentNullException.ThrowIfNull(sideCards);

        this.mainAndExtraCards = mainAndExtraCards.ToArray();
        this.sideCards = sideCards.ToArray();
        mainAndExtraView = Array.AsReadOnly(this.mainAndExtraCards);
        sideView = Array.AsReadOnly(this.sideCards);

        ulong payloadLength = 8UL +
            ((ulong)this.mainAndExtraCards.Length + (ulong)this.sideCards.Length) *
            sizeof(uint);
        if (payloadLength > ProtocolContractV1.MaxPayloadLength)
        {
            throw new ArgumentException(
                "The ordered deck sequences exceed the V1 payload capacity.",
                nameof(mainAndExtraCards));
        }
    }

    public IReadOnlyList<uint> MainAndExtraCards => mainAndExtraView;

    public IReadOnlyList<uint> SideCards => sideView;

    internal ReadOnlySpan<uint> MainAndExtraSpan => mainAndExtraCards;

    internal ReadOnlySpan<uint> SideSpan => sideCards;
}
