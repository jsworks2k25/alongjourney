using Godot;
using AlongJourney.Core;

public partial class BuildingSystem : Node2D
{
    [Signal] public delegate void BuildingModeChangedEventHandler(bool isBuildingMode);
    [Signal] public delegate void ObjectPlacedEventHandler(PackedScene scene, Vector2 position);

    [ExportCategory("Config")]

    [Export] public TileMapLayer GroundLayer;
    [Export] public Node2D ObjectLayer;
    [Export] public float TileWidth = 32f;
    [Export] public float TileHeight = 16f;

    [ExportCategory("Debug")]
    [Export] public PackedScene ObjectToPlace;

    private Node2D _previewIllusion;
    private bool _isBuildingMode = false;
    private int _activePlayerId = GameConstants.InvalidPlayerId;
    private Vector2I _activeFootprint = Vector2I.One;
    private readonly WorldMutationService _worldMutations = new();
    public bool IsBuildingMode => _isBuildingMode;
    public PackedScene CurrentBuildingTarget => ObjectToPlace;

    public override void _Ready()
    {
        if (ObjectToPlace != null)
        {
            SetBuildingTarget(ObjectToPlace);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isBuildingMode || _previewIllusion == null) return;

        if (@event is InputEventMouseMotion)
        {
            UpdateIllusionPosition();
        }

        if (@event.IsActionPressed("mouse_left"))
        {
            if (PlaceObject())
            {
                GetViewport().SetInputAsHandled();
            }
        }

        if (@event.IsActionPressed("mouse_right"))
        {
            CancelBuildingMode();
            GetViewport().SetInputAsHandled();
        }
    }

    public void SetBuildingTarget(PackedScene scene)
    {
        SetBuildingTargetForPlayer(scene, GameConstants.InvalidPlayerId);
    }

    public void SetBuildingTargetForPlayer(PackedScene scene, int playerId)
    {
        if (scene == null)
        {
            CancelBuildingMode();
            return;
        }

        if (_previewIllusion != null)
        {
            _previewIllusion.QueueFree();
        }

        ObjectToPlace = scene;
        _activePlayerId = playerId;
        _activeFootprint = BuildingFootprintReader.GetFootprintTiles(scene);
        Node instance = scene.Instantiate();

        if (instance is Node2D node2d)
        {
            _previewIllusion = node2d;
            _previewIllusion.Modulate = new Color(1, 1, 1, 0.5f);
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
        _activePlayerId = GameConstants.InvalidPlayerId;
        _activeFootprint = Vector2I.One;

        if (_previewIllusion != null)
        {
            _previewIllusion.QueueFree();
            _previewIllusion = null;
        }

        EmitSignal(SignalName.BuildingModeChanged, _isBuildingMode);
    }

    /// <summary>
    /// 根据建筑占地计算吸附后的世界坐标（供编辑器手动摆放或建造预览复用）。
    /// </summary>
    public Vector2 SnapWorldPosition(Vector2 worldPos, Vector2I footprintTiles)
    {
        Vector2 gridPos = GridPlacement.WorldToGrid(
            worldPos,
            GroundLayer.GlobalPosition,
            TileWidth,
            TileHeight);
        Vector2 snappedGrid = GridPlacement.SnapForFootprint(gridPos, footprintTiles);
        return GridToWorld(snappedGrid);
    }

    public Vector2 SnapWorldPositionForScene(Vector2 worldPos, PackedScene scene)
    {
        return SnapWorldPosition(worldPos, BuildingFootprintReader.GetFootprintTiles(scene));
    }

    private void UpdateIllusionPosition()
    {
        _previewIllusion.GlobalPosition = SnapWorldPosition(GetGlobalMousePosition(), _activeFootprint);
        _previewIllusion.ZIndex = 1;
    }

    private bool PlaceObject()
    {
        if (ObjectToPlace == null)
        {
            return false;
        }

        var request = new PlaceObjectRequest(
            ObjectToPlace,
            ObjectLayer,
            _previewIllusion.GlobalPosition,
            _activePlayerId,
            _activeFootprint,
            TileWidth,
            TileHeight);

        if (!_worldMutations.TryPlaceObject(request, out var newBuilding))
        {
            return false;
        }

        EmitSignal(SignalName.ObjectPlaced, ObjectToPlace, newBuilding.GlobalPosition);
        return true;
    }

    private Vector2 GridToWorld(Vector2 gridPos) =>
        GridPlacement.GridToWorld(gridPos, GroundLayer.GlobalPosition, TileWidth, TileHeight);

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
