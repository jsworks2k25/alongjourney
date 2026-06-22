namespace AlongJourney.Core;

using System.Collections.Generic;
using Godot;

/// <summary>
/// 等距网格放置：奇数占地对齐格心，偶数占地对齐网格交叉点。
/// </summary>
public static class GridPlacement
{
    public static Vector2 WorldToGrid(
        Vector2 worldPos,
        Vector2 groundOrigin,
        float tileWidth,
        float tileHeight)
    {
        float halfW = tileWidth / 2f;
        float halfH = tileHeight / 2f;
        Vector2 localPos = worldPos - groundOrigin;

        float x = (localPos.X / halfW + localPos.Y / halfH) / 2f;
        float y = (localPos.Y / halfH - localPos.X / halfW) / 2f;

        return new Vector2(x, y);
    }

    public static Vector2 GridToWorld(
        Vector2 gridPos,
        Vector2 groundOrigin,
        float tileWidth,
        float tileHeight)
    {
        float halfW = tileWidth / 2f;
        float halfH = tileHeight / 2f;

        float x = (gridPos.X - gridPos.Y) * halfW;
        float y = (gridPos.X + gridPos.Y) * halfH;

        return new Vector2(x, y) + groundOrigin;
    }

    /// <summary>
    /// 1×1 → 整数格坐标（格心）；2×2 等偶数占地 → n+0.5（四格交叉点）。
    /// </summary>
    public static Vector2 SnapForFootprint(Vector2 gridPos, Vector2I footprintTiles)
    {
        float snapX = IsEven(footprintTiles.X)
            ? Mathf.Round(gridPos.X - 0.5f) + 0.5f
            : Mathf.Round(gridPos.X);

        float snapY = IsEven(footprintTiles.Y)
            ? Mathf.Round(gridPos.Y - 0.5f) + 0.5f
            : Mathf.Round(gridPos.Y);

        return new Vector2(snapX, snapY);
    }

    /// <summary>
    /// 放置重叠检测半径（世界像素），按占地从格心/交叉点向四隅估算。
    /// </summary>
    public static float GetPlacementBlockRadius(
        Vector2I footprintTiles,
        float tileWidth,
        float tileHeight)
    {
        float halfW = tileWidth / 2f;
        float halfH = tileHeight / 2f;
        float extentX = footprintTiles.X / 2f;
        float extentY = footprintTiles.Y / 2f;

        float cornerX = (extentX - extentY) * halfW;
        float cornerY = (extentX + extentY) * halfH;

        return new Vector2(cornerX, cornerY).Length() * 0.9f;
    }

    private static bool IsEven(int value) => value > 0 && value % 2 == 0;

    public static bool IsFootprintEven(Vector2I footprintTiles) =>
        IsEven(footprintTiles.X) || IsEven(footprintTiles.Y);

    /// <summary>
    /// 从吸附锚点（格心或 n+0.5 交叉点）展开占用的逻辑格。
    /// </summary>
    public static void GetOccupiedCells(
        Vector2 anchorGrid,
        Vector2I footprintTiles,
        HashSet<Vector2I> destination)
    {
        int startX = Mathf.FloorToInt(anchorGrid.X - (footprintTiles.X - 1) / 2f);
        int startY = Mathf.FloorToInt(anchorGrid.Y - (footprintTiles.Y - 1) / 2f);

        for (int dx = 0; dx < footprintTiles.X; dx++)
        {
            for (int dy = 0; dy < footprintTiles.Y; dy++)
            {
                destination.Add(new Vector2I(startX + dx, startY + dy));
            }
        }
    }

    /// <summary>
    /// 网格四向步进 → MovementComponent 可用的 MoveDirection（配合 IsometricVector.Y）。
    /// </summary>
    public static Vector2 GridStepToMoveDirection(
        Vector2I gridStep,
        float tileWidth = 32f,
        float tileHeight = 16f,
        float isometricScaleY = 0.5f)
    {
        float halfW = tileWidth / 2f;
        float halfH = tileHeight / 2f;
        Vector2 worldStep = new Vector2(
            (gridStep.X - gridStep.Y) * halfW,
            (gridStep.X + gridStep.Y) * halfH);

        if (worldStep.LengthSquared() < 0.001f)
        {
            return Vector2.Zero;
        }

        return new Vector2(worldStep.X, worldStep.Y / isometricScaleY).Normalized();
    }
}
