namespace AlongJourney.Core;

using System.Collections.Generic;
using Godot;
using AlongJourney.Entities.Player;

/// <summary>
/// Tracks players by stable local ids. This is intentionally network-free; peer
/// ids and device routing can be layered on top later.
/// </summary>
public sealed class PlayerRegistry
{
    private readonly Dictionary<int, Player> _playersById = new();
    private int _nextGeneratedId = GameConstants.DefaultLocalPlayerId;

    public IReadOnlyDictionary<int, Player> PlayersById => _playersById;

    public int Register(Player player)
    {
        if (player == null)
        {
            return GameConstants.InvalidPlayerId;
        }

        int playerId = player.PlayerId > GameConstants.InvalidPlayerId
            ? player.PlayerId
            : AllocatePlayerId();

        _playersById[playerId] = player;
        player.ConfigureMultiplayerIdentity(playerId, player.IsLocalPlayer, player.InputDeviceId);
        _nextGeneratedId = Mathf.Max(_nextGeneratedId, playerId + 1);
        return playerId;
    }

    public void Unregister(Player player)
    {
        if (player == null)
        {
            return;
        }

        if (_playersById.TryGetValue(player.PlayerId, out var registered) && registered == player)
        {
            _playersById.Remove(player.PlayerId);
        }
    }

    public Player GetPlayer(int playerId)
    {
        return _playersById.TryGetValue(playerId, out var player) && IsPlayerValid(player)
            ? player
            : null;
    }

    public Player GetFirstLocalPlayer()
    {
        foreach (var player in GetPlayers())
        {
            if (player.IsLocalPlayer)
            {
                return player;
            }
        }

        return null;
    }

    public List<Player> GetPlayers()
    {
        var players = new List<Player>(_playersById.Count);
        foreach (var pair in _playersById)
        {
            if (IsPlayerValid(pair.Value))
            {
                players.Add(pair.Value);
            }
        }

        return players;
    }

    public void Clear()
    {
        _playersById.Clear();
        _nextGeneratedId = GameConstants.DefaultLocalPlayerId;
    }

    private int AllocatePlayerId()
    {
        while (_playersById.ContainsKey(_nextGeneratedId))
        {
            _nextGeneratedId++;
        }

        return _nextGeneratedId++;
    }

    private static bool IsPlayerValid(Player player)
    {
        return player != null && GodotObject.IsInstanceValid(player) && player.IsInsideTree();
    }
}
