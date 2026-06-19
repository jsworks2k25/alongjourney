namespace AlongJourney.Components;

using Godot;
using AlongJourney.Core;

/// <summary>
/// 可放置建筑的占地元数据。节点原点即放置锚点：
/// 1×1 → 格心；2×2 等偶数 → 四格交叉点（占地中心）。
/// </summary>
public partial class BuildingFootprint : Node
{
    [Export]
    public Vector2I Tiles = Vector2I.One;

    public Vector2 SnapGrid(Vector2 gridPos) => GridPlacement.SnapForFootprint(gridPos, Tiles);

    public float GetBlockRadius(float tileWidth, float tileHeight) =>
        GridPlacement.GetPlacementBlockRadius(Tiles, tileWidth, tileHeight);
}
