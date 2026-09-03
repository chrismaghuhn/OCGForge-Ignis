using OCGForge.Ignis.Protocol;

namespace OCGForge.Ignis.Client;

public readonly record struct LobbyPlayerSnapshot(
    byte Position,
    string Name,
    byte Status,
    bool IsOccupied)
{
    public bool IsReady => Status == 0x09;
}

public sealed class LobbyState
{
    private readonly SortedDictionary<byte, LobbyPlayerSnapshot> players = new();

    public HostInfoPayload? HostInfo { get; internal set; }

    public byte? PreDuelLobbyPosition { get; internal set; }

    public bool IsHost { get; internal set; }

    public ushort WatcherCount { get; internal set; }

    internal void ApplyPlayerEntered(byte position, string name)
    {
        players[position] = new LobbyPlayerSnapshot(position, name, 0, true);
    }

    internal void ApplyPlayerStatus(byte position, byte status)
    {
        if (players.TryGetValue(position, out LobbyPlayerSnapshot player))
        {
            players[position] = player with { Status = status };
        }
        else
        {
            players[position] = new LobbyPlayerSnapshot(
                position,
                string.Empty,
                status,
                false);
        }
    }

    internal void ApplyPlayerMoved(byte oldPosition, byte newPosition)
    {
        if (players.TryGetValue(oldPosition, out LobbyPlayerSnapshot player))
        {
            players.Remove(oldPosition);
            players[newPosition] = player with { Position = newPosition };
        }
    }

    internal void RemovePlayer(byte position) => players.Remove(position);

    internal bool HasOccupiedPlayer(byte position) =>
        players.TryGetValue(position, out LobbyPlayerSnapshot player) &&
        player.IsOccupied;

    internal bool TryGetPlayer(
        byte position,
        out LobbyPlayerSnapshot player) =>
        players.TryGetValue(position, out player);

    public IReadOnlyList<LobbyPlayerSnapshot> SnapshotPlayers() =>
        players.Values.ToArray();
}
