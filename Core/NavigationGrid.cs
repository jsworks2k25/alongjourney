namespace AlongJourney.Core;

using System;
using System.Collections.Generic;
using Godot;
using AlongJourney.Components;

/// <summary>
/// 维护等距逻辑格的可行走 / 阻挡信息，供 AI 四向寻路使用。
/// </summary>
public partial class NavigationGrid : Node2D
{
    [ExportCategory("References")]
    [Export] public TileMapLayer GroundLayer;
    [Export] public Node2D ObjectLayer;

    [ExportCategory("Grid")]
    [Export] public float TileWidth = 32f;
    [Export] public float TileHeight = 16f;
    [Export] public int BoundsPadding = 1;

    [ExportCategory("Debug")]
    [Export] public bool ShowDebugOverlay = true;
    [Export] public Color BlockedOverlayColor = new(1f, 0.2f, 0.2f, 0.45f);

    private readonly HashSet<Vector2I> _walkableCells = new();
    private readonly HashSet<Vector2I> _blockedCells = new();

    public IReadOnlyCollection<Vector2I> WalkableCells => _walkableCells;
    public IReadOnlyCollection<Vector2I> BlockedCells => _blockedCells;

    public override void _Ready()
    {
        AddToGroup(GameConstants.NavigationGridGroupName);
        CallDeferred(MethodName.Initialize);
    }

    public override void _Draw()
    {
        if (!ShowDebugOverlay || GroundLayer == null)
        {
            return;
        }

        Vector2 groundOrigin = GroundLayer.GlobalPosition;
        foreach (Vector2I cell in _blockedCells)
        {
            DrawCellDiamond(cell, groundOrigin, BlockedOverlayColor);
        }
    }

    public static NavigationGrid Resolve(Node from)
    {
        if (from == null)
        {
            return null;
        }

        return from.GetTree()?.GetFirstNodeInGroup(GameConstants.NavigationGridGroupName) as NavigationGrid;
    }

    public void Rebuild()
    {
        _walkableCells.Clear();
        _blockedCells.Clear();

        if (GroundLayer == null)
        {
            GD.PushError($"{Name}: GroundLayer is not assigned.");
            QueueRedraw();
            return;
        }

        BuildWalkableRegion();
        ScanStaticObstacles();

        if (ShowDebugOverlay)
        {
            GD.Print($"{Name}: walkable={_walkableCells.Count}, blocked={_blockedCells.Count}");
        }

        QueueRedraw();
    }

    public bool IsWalkable(Vector2I cell) =>
        _walkableCells.Contains(cell) && !_blockedCells.Contains(cell);

    public List<Vector2I> FindPath(Vector2I start, Vector2I goal) =>
        GridPathfinder.FindPath(start, goal, IsWalkable);

    public Vector2I WorldToCell(Vector2 worldPosition)
    {
        Vector2 grid = GridPlacement.WorldToGrid(
            worldPosition,
            GroundLayer.GlobalPosition,
            TileWidth,
            TileHeight);
        return new Vector2I(Mathf.RoundToInt(grid.X), Mathf.RoundToInt(grid.Y));
    }

    public Vector2 CellToWorld(Vector2I cell) =>
        GridPlacement.GridToWorld(
            new Vector2(cell.X, cell.Y),
            GroundLayer.GlobalPosition,
            TileWidth,
            TileHeight);

    public void MarkBlockedAtWorld(Vector2 worldPosition, Vector2I footprintTiles)
    {
        Vector2 grid = GridPlacement.WorldToGrid(
            worldPosition,
            GroundLayer.GlobalPosition,
            TileWidth,
            TileHeight);
        Vector2 anchor = GridPlacement.SnapForFootprint(grid, footprintTiles);
        MarkFootprintBlocked(anchor, footprintTiles);
        QueueRedraw();
    }

    private void Initialize()
    {
        BuildingSystem buildingSystem = GetParent()?.GetNodeOrNull<BuildingSystem>("BuildingSystem");
        if (buildingSystem != null)
        {
            buildingSystem.ObjectPlaced += OnObjectPlaced;
        }
        else
        {
            GD.PushWarning($"{Name}: BuildingSystem not found; dynamic placement updates disabled.");
        }

        Rebuild();
    }

    private void OnObjectPlaced(PackedScene scene, Vector2 position)
    {
        if (TryRegisterPlacedObstacle(position))
        {
            QueueRedraw();
        }
        else
        {
            Vector2I footprint = BuildingFootprintReader.GetFootprintTiles(scene);
            MarkBlockedAtWorld(position, footprint);
        }

        if (ShowDebugOverlay)
        {
            GD.Print($"{Name}: marked blocked at {position} (total blocked={_blockedCells.Count})");
        }
    }

    private bool TryRegisterPlacedObstacle(Vector2 position)
    {
        if (ObjectLayer == null)
        {
            return false;
        }

        foreach (Node child in ObjectLayer.GetChildren())
        {
            if (child is StaticBody2D body
                && body.GlobalPosition.DistanceSquaredTo(position) < 1f)
            {
                RegisterObstacle(body);
                return true;
            }
        }

        return false;
    }

    private void BuildWalkableRegion()
    {
        Godot.Collections.Array<Vector2I> usedCells = GroundLayer.GetUsedCells();
        if (usedCells.Count == 0)
        {
            for (int x = -8; x <= 8; x++)
            {
                for (int y = -8; y <= 8; y++)
                {
                    _walkableCells.Add(new Vector2I(x, y));
                }
            }

            return;
        }

        Vector2 groundOrigin = GroundLayer.GlobalPosition;
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        foreach (Vector2I tileCoord in usedCells)
        {
            Vector2 world = GroundLayer.ToGlobal(GroundLayer.MapToLocal(tileCoord));
            Vector2 grid = GridPlacement.WorldToGrid(world, groundOrigin, TileWidth, TileHeight);
            int gx = Mathf.RoundToInt(grid.X);
            int gy = Mathf.RoundToInt(grid.Y);
            minX = Mathf.Min(minX, gx);
            minY = Mathf.Min(minY, gy);
            maxX = Mathf.Max(maxX, gx);
            maxY = Mathf.Max(maxY, gy);
        }

        minX -= BoundsPadding;
        minY -= BoundsPadding;
        maxX += BoundsPadding;
        maxY += BoundsPadding;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                _walkableCells.Add(new Vector2I(x, y));
            }
        }
    }

    private void ScanStaticObstacles()
    {
        if (ObjectLayer == null)
        {
            GD.PushWarning($"{Name}: ObjectLayer is not assigned.");
            return;
        }

        foreach (Node child in ObjectLayer.GetChildren())
        {
            if (child is StaticBody2D body && HasEnabledCollision(body))
            {
                RegisterObstacle(body);
            }
        }
    }

    private void RegisterObstacle(StaticBody2D body)
    {
        Vector2I footprintTiles = BuildingFootprintReader.ReadFootprintTiles(body);
        Vector2 grid = GridPlacement.WorldToGrid(
            GetObstacleGridAnchor(body),
            GroundLayer.GlobalPosition,
            TileWidth,
            TileHeight);
        Vector2 anchor = GridPlacement.SnapForFootprint(grid, footprintTiles);
        GridPlacement.GetOccupiedCells(anchor, footprintTiles, _blockedCells);
    }

    private static Vector2 GetObstacleGridAnchor(StaticBody2D body)
    {
        CollisionShape2D shape = body.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (shape != null && !shape.Disabled && shape.Shape != null)
        {
            return shape.GlobalTransform.Origin;
        }

        foreach (Node child in body.GetChildren())
        {
            if (child is CollisionShape2D collisionShape
                && !collisionShape.Disabled
                && collisionShape.Shape != null)
            {
                return collisionShape.GlobalTransform.Origin;
            }
        }

        return body.GlobalPosition;
    }

    private void MarkFootprintBlocked(Vector2 anchorGrid, Vector2I footprintTiles)
    {
        GridPlacement.GetOccupiedCells(anchorGrid, footprintTiles, _blockedCells);
    }

    private static bool HasEnabledCollision(StaticBody2D body)
    {
        foreach (Node child in body.GetChildren())
        {
            if (child is CollisionShape2D shape && !shape.Disabled && shape.Shape != null)
            {
                return true;
            }

            if (child is CollisionPolygon2D polygon && !polygon.Disabled)
            {
                return true;
            }
        }

        return false;
    }

    private void DrawCellDiamond(Vector2I cell, Vector2 groundOrigin, Color color)
    {
        Vector2 center = ToLocal(CellToWorld(cell));
        Vector2[] points =
        {
            ToLocal(GridPlacement.GridToWorld(new Vector2(cell.X + 0.5f, cell.Y), groundOrigin, TileWidth, TileHeight)),
            ToLocal(GridPlacement.GridToWorld(new Vector2(cell.X, cell.Y + 0.5f), groundOrigin, TileWidth, TileHeight)),
            ToLocal(GridPlacement.GridToWorld(new Vector2(cell.X - 0.5f, cell.Y), groundOrigin, TileWidth, TileHeight)),
            ToLocal(GridPlacement.GridToWorld(new Vector2(cell.X, cell.Y - 0.5f), groundOrigin, TileWidth, TileHeight)),
        };

        DrawColoredPolygon(points, color);
        DrawPolyline(points, new Color(color, 1f), 1f, true);
    }
}
