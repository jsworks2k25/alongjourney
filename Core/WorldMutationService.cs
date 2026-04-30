namespace AlongJourney.Core;

using Godot;

public readonly struct PlaceObjectRequest
{
    public PlaceObjectRequest(PackedScene scene, Node2D parent, Vector2 position, int requestedByPlayerId)
    {
        Scene = scene;
        Parent = parent;
        Position = position;
        RequestedByPlayerId = requestedByPlayerId;
    }

    public PackedScene Scene { get; }
    public Node2D Parent { get; }
    public Vector2 Position { get; }
    public int RequestedByPlayerId { get; }
}

/// <summary>
/// Single entry point for world mutations. It is local and immediate today, but
/// the call boundary matches future host-authoritative validation.
/// </summary>
public sealed class WorldMutationService
{
    public bool TryPlaceObject(PlaceObjectRequest request, out Node2D placedObject)
    {
        placedObject = null;

        if (request.Scene == null || request.Parent == null)
        {
            return false;
        }

        placedObject = request.Scene.Instantiate<Node2D>();
        placedObject.GlobalPosition = request.Position;
        request.Parent.AddChild(placedObject);
        return true;
    }
}
