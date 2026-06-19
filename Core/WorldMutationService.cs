namespace AlongJourney.Core;

using Godot;

public readonly struct PlaceObjectRequest
{
    public PlaceObjectRequest(
        PackedScene scene,
        Node2D parent,
        Vector2 position,
        int requestedByPlayerId,
        Vector2I footprintTiles,
        float tileWidth,
        float tileHeight)
    {
        Scene = scene;
        Parent = parent;
        Position = position;
        RequestedByPlayerId = requestedByPlayerId;
        FootprintTiles = footprintTiles;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
    }

    public PackedScene Scene { get; }
    public Node2D Parent { get; }
    public Vector2 Position { get; }
    public int RequestedByPlayerId { get; }
    public Vector2I FootprintTiles { get; }
    public float TileWidth { get; }
    public float TileHeight { get; }
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

        float blockRadius = GridPlacement.GetPlacementBlockRadius(
            request.FootprintTiles,
            request.TileWidth,
            request.TileHeight);

        if (IsPlacementBlocked(request.Parent, request.Position, blockRadius))
        {
            return false;
        }

        placedObject = request.Scene.Instantiate<Node2D>();
        placedObject.GlobalPosition = request.Position;
        request.Parent.AddChild(placedObject);
        return true;
    }

    private static bool IsPlacementBlocked(Node2D objectLayer, Vector2 globalPosition, float radius)
    {
        World2D world = objectLayer.GetWorld2D();
        if (world == null)
        {
            return false;
        }

        var shape = new CircleShape2D { Radius = radius };
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape,
            Transform = new Transform2D(0f, globalPosition),
            CollideWithAreas = false,
            CollideWithBodies = true,
            CollisionMask = uint.MaxValue,
        };

        foreach (Godot.Collections.Dictionary result in world.DirectSpaceState.IntersectShape(query))
        {
            if (!result.TryGetValue("collider", out Variant colliderVariant))
            {
                continue;
            }

            if (colliderVariant.AsGodotObject() is StaticBody2D body
                && IsDescendantOf(body, objectLayer))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDescendantOf(Node node, Node ancestor)
    {
        Node current = node;
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }

            current = current.GetParent();
        }

        return false;
    }
}
