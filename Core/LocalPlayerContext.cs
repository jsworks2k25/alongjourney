namespace AlongJourney.Core;

using System;
using Godot;
using AlongJourney.Entities.Player;

/// <summary>
/// The local presentation context: UI, camera, and local input bind to this
/// player. It is separate from the registry so future clients can each choose
/// their own local player while the host owns the world simulation.
/// </summary>
public sealed class LocalPlayerContext
{
    public event Action<Player> LocalPlayerChanged;

    public int LocalPlayerId { get; private set; } = GameConstants.DefaultLocalPlayerId;
    public Player CurrentPlayer { get; private set; }

    public bool HasValidPlayer =>
        CurrentPlayer != null &&
        GodotObject.IsInstanceValid(CurrentPlayer) &&
        CurrentPlayer.IsInsideTree();

    public void Bind(Player player)
    {
        if (player == null)
        {
            Clear();
            return;
        }

        LocalPlayerId = player.PlayerId;
        CurrentPlayer = player;
        LocalPlayerChanged?.Invoke(CurrentPlayer);
    }

    public void ClearIf(Player player)
    {
        if (CurrentPlayer == player)
        {
            Clear();
        }
    }

    public void Clear()
    {
        if (CurrentPlayer == null)
        {
            return;
        }

        CurrentPlayer = null;
        LocalPlayerChanged?.Invoke(null);
    }
}
