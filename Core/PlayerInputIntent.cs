namespace AlongJourney.Core;

using Godot;

/// <summary>
/// Local input intent for one player. Future multiplayer code can serialize this
/// instead of exposing raw Godot input state to the simulation.
/// </summary>
public readonly struct PlayerInputIntent
{
    public static readonly PlayerInputIntent Empty = new(Vector2.Zero);

    public PlayerInputIntent(Vector2 moveDirection)
    {
        MoveDirection = moveDirection;
    }

    public Vector2 MoveDirection { get; }
}
