using Godot;
using System;

public partial class BuildingSystem : Node2D
{
    [Signal] public delegate void BuildingModeChangedEventHandler(bool isBuildingMode);
    [Signal] public delegate void ObjectPlacedEventHandler(PackedScene scene, Vector2 position);

    [ExportCategory("Config")]

    [Export] public TileMapLayer GroundLayer;
    [Export] public Node2D ObjectLayer;
    [Export] public int Subdivisions = 2; // 子网格细分等级：2 = 半格对齐，4 = 1/4格对齐
    [Export] public float TileWidth = 32f;
    [Export] public float TileHeight = 16f;

    [ExportCategory("Debug")]
    [Export] public PackedScene ObjectToPlace; // 拖入你想放置的家具/建筑预制体

    private Node2D _previewIllusion; // 预览的虚影
    private bool _isBuildingMode = false;
    public bool IsBuildingMode => _isBuildingMode;
    public PackedScene CurrentBuildingTarget => ObjectToPlace;

    public override void _Ready()
    {
        // 初始化：如果已经在编辑器里预设了要放置的物体，先生成一个 Illusion
        if (ObjectToPlace != null)
        {
            SetBuildingTarget(ObjectToPlace);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isBuildingMode || _previewIllusion == null) return;

        // 鼠标移动时更新位置
        if (@event is InputEventMouseMotion)
        {
            UpdateIllusionPosition();
        }

        // 点击左键放置
        if (@event.IsActionPressed("mouse_left")) // 确保你在 InputMap 里设置了 "mouse_left"
        {
            if (PlaceObject())
            {
                GetViewport().SetInputAsHandled();
            }
        }
        
        // 右键取消/旋转（这里先写取消）
        if (@event.IsActionPressed("mouse_right"))
        {
            CancelBuildingMode();
            GetViewport().SetInputAsHandled();
        }
    }

    // 核心方法：设置当前要建造的物体
    public void SetBuildingTarget(PackedScene scene)
    {
        if (scene == null)
        {
            CancelBuildingMode();
            return;
        }

        // 清理旧 Ghost
        if (_previewIllusion != null)
        {
            _previewIllusion.QueueFree();
        }

        ObjectToPlace = scene;
        Node instance = scene.Instantiate();
        
        if (instance is Node2D node2d)
        {
            _previewIllusion = node2d;
            // 设为半透明，以此作为“预览”
            _previewIllusion.Modulate = new Color(1, 1, 1, 0.5f);
            // 关掉碰撞，防止预览时卡住玩家
            DisableCollisionsRecursively(_previewIllusion); 
            
            AddChild(_previewIllusion);
            _isBuildingMode = true;
            UpdateIllusionPosition();
            EmitSignal(SignalName.BuildingModeChanged, _isBuildingMode);
        }
        else
        {
            GD.PrintErr("BuildingSystem: Trying to place a non-Node2D object!");
            instance.QueueFree();
        }
    }

    public void CancelBuildingMode()
    {
        _isBuildingMode = false;
        ObjectToPlace = null;

        if (_previewIllusion != null)
        {
            _previewIllusion.QueueFree();
            _previewIllusion = null;
        }

        EmitSignal(SignalName.BuildingModeChanged, _isBuildingMode);
    }

    private void UpdateIllusionPosition()
    {
        Vector2 mousePos = GetGlobalMousePosition();
        Vector2 snappedPos = CalculateSnappedPosition(mousePos);
        
        // 更新 Illusion 位置
        _previewIllusion.GlobalPosition = snappedPos;
        
        // 简单的 Z-Sorting (Y-Sort) 预览
        // 实际上在 Y-Sort 节点下会自动处理，但为了虚影层级正确，有时需要手动调整 ZIndex
        _previewIllusion.ZIndex = 1; 
    }

    private bool PlaceObject()
    {
        if (ObjectToPlace == null)
        {
            return false;
        }

        // 真正的实例化
        Node2D newBuilding = ObjectToPlace.Instantiate<Node2D>();
        
        // 获取刚才计算好的吸附位置
        newBuilding.GlobalPosition = _previewIllusion.GlobalPosition;
        
        // 将物体添加到场景中（通常添加到 Y-Sort 节点下，而不是 BuildingSystem 下）
        // 这里假设 GroundLayer 的父节点是主要的 Y-Sort 容器
        ObjectLayer.AddChild(newBuilding);
        EmitSignal(SignalName.ObjectPlaced, ObjectToPlace, newBuilding.GlobalPosition);

        // 可选：放置后是否退出建造模式？
        // _isBuildingMode = false; 
        // _previewIllusion.QueueFree();
        // _previewIllusion = null;
        return true;
    }

    // --- 核心数学逻辑 ---

    // 将世界坐标转为等距网格坐标 (Float)
    private Vector2 WorldToGrid(Vector2 worldPos)
    {
        // 这里的公式基于标准的 Isometric 投影
        // x_grid = (x / (W/2) + y / (H/2)) / 2
        // y_grid = (y / (H/2) - x / (W/2)) / 2
        
        float halfW = TileWidth / 2f;
        float halfH = TileHeight / 2f;

        // 注意：这取决于你的 TileMap 原点设置。
        // Godot 默认 TileMap 原点在菱形中心，不需要额外 Offset。
        // 如果你的鼠标对不准，可能需要减去 GroundLayer.GlobalPosition
        Vector2 localPos = worldPos - GroundLayer.GlobalPosition;

        float x = (localPos.X / halfW + localPos.Y / halfH) / 2f;
        float y = (localPos.Y / halfH - localPos.X / halfW) / 2f;

        return new Vector2(x, y);
    }

    // 将等距网格坐标转回世界坐标
    private Vector2 GridToWorld(Vector2 gridPos)
    {
        float halfW = TileWidth / 2f;
        float halfH = TileHeight / 2f;

        float x = (gridPos.X - gridPos.Y) * halfW;
        float y = (gridPos.X + gridPos.Y) * halfH;

        return new Vector2(x, y) + GroundLayer.GlobalPosition;
    }

    private Vector2 CalculateSnappedPosition(Vector2 mousePos)
    {
        // 1. 转为 Grid 坐标
        Vector2 gridPos = WorldToGrid(mousePos);

        // 2. 进行子网格吸附
        // Math.Round(val * n) / n 是最经典的吸附算法
        float snappedX = (float)Math.Round(gridPos.X * Subdivisions) / Subdivisions;
        float snappedY = (float)Math.Round(gridPos.Y * Subdivisions) / Subdivisions;

        // 3. 转回世界坐标
        return GridToWorld(new Vector2(snappedX, snappedY));
    }

    // 辅助工具：递归禁用预览物体的碰撞体
    private void DisableCollisionsRecursively(Node node)
    {
        if (node is CollisionShape2D shape)
        {
            shape.Disabled = true;
        }
        if (node is CollisionPolygon2D poly)
        {
            poly.Disabled = true;
        }
        foreach (Node child in node.GetChildren())
        {
            DisableCollisionsRecursively(child);
        }
    }
}