namespace AlongJourney.Core;

using Godot;
using AlongJourney.Components;

public static class BuildingFootprintReader
{
    public static Vector2I GetFootprintTiles(PackedScene scene)
    {
        if (scene == null)
        {
            return Vector2I.One;
        }

        Node instance = scene.Instantiate();
        Vector2I tiles = ReadFootprintTiles(instance);
        instance.QueueFree();
        return tiles;
    }

    public static Vector2I ReadFootprintTiles(Node node)
    {
        BuildingFootprint footprint = FindFootprint(node);
        return footprint?.Tiles ?? Vector2I.One;
    }

    public static BuildingFootprint FindFootprint(Node node)
    {
        if (node == null)
        {
            return null;
        }

        if (node is BuildingFootprint component)
        {
            return component;
        }

        if (node.GetNodeOrNull<BuildingFootprint>("BuildingFootprint") is BuildingFootprint named)
        {
            return named;
        }

        foreach (Node child in node.GetChildren())
        {
            BuildingFootprint found = FindFootprint(child);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
