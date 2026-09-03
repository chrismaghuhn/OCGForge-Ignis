using System.Collections.ObjectModel;
using OCGForge.Ignis.Protocol;

namespace OCGForge.Ignis.Client;

public sealed class PreDuelSessionV1
{
    private readonly I2Event[] events;
    private readonly ReadOnlyCollection<I2Event> eventsView;

    public PreDuelSessionV1(
        HostInfoPayload hostInfo,
        byte preDuelLobbyPosition,
        bool isHost,
        PreDuelOutcome outcome,
        IEnumerable<I2Event> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        this.events = events.ToArray();
        eventsView = Array.AsReadOnly(this.events);
        HostInfo = hostInfo;
        PreDuelLobbyPosition = preDuelLobbyPosition;
        IsHost = isHost;
        Outcome = outcome;
    }

    public HostInfoPayload HostInfo { get; }

    public byte PreDuelLobbyPosition { get; }

    public bool IsHost { get; }

    public PreDuelOutcome Outcome { get; }

    public IReadOnlyList<I2Event> Events => eventsView;

    public override string ToString() =>
        $"PreDuelSessionV1(Position={PreDuelLobbyPosition}, " +
        $"IsHost={IsHost}, Outcome={Outcome}, Events={Events.Count})";
}
