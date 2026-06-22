namespace AlongJourney.Entities.Characters.AICharacters;

using System.Collections.Generic;
using Godot;
using AlongJourney.Core;
using AlongJourney.Entities;
using AlongJourney.Interfaces;

/// <summary>
/// 四向网格寻路追击：写入 <see cref="Actor.BlackboardKeys.MoveDirection"/>。
/// </summary>
public partial class GridChaseMovement : Node
{
    [Export] public bool UseBonfireGroup = true;
    [Export] public float RepathInterval = 0.35f;
    [Export] public float WaypointReachDistance = 8f;

    private Actor _owner;
    private NavigationGrid _navigationGrid;
    private ITargetable _target;
    private List<Vector2I> _path;
    private int _pathIndex;
    private Vector2I _lastStartCell;
    private Vector2I _lastGoalCell;
    private Vector2I _resolvedGoalCell;
    private float _repathTimer;
    private bool _pathDirty = true;

    public ITargetable Target => _target;

    public override void _Ready()
    {
        _owner = GetParent<Actor>();
        if (_owner == null)
        {
            GD.PushError($"{Name}: parent must be Actor.");
        }

        CallDeferred(MethodName.BindNavigationGrid);
    }

    public void SetTarget(ITargetable target)
    {
        _target = target;
        _pathDirty = true;
    }

    public void PhysicsTick(double delta)
    {
        if (_owner == null || _navigationGrid == null)
        {
            return;
        }

        EnsureTarget();
        if (_target == null || !_target.IsAlive)
        {
            StopMoving();
            return;
        }

        Vector2I startCell = _navigationGrid.WorldToCell(_owner.GlobalPosition);
        Vector2I goalCell = _navigationGrid.WorldToCell(_target.GlobalPosition);

        if (IsOnTargetApproachCell(startCell, goalCell))
        {
            StopMoving();
            return;
        }

        _repathTimer -= (float)delta;
        if (_pathDirty
            || _path == null
            || _pathIndex >= _path.Count
            || _repathTimer <= 0f
            || startCell != _lastStartCell
            || goalCell != _lastGoalCell)
        {
            Repath(startCell, goalCell);
        }

        if (_path == null || _path.Count == 0)
        {
            MoveTowardApproachCell(startCell, goalCell);
            return;
        }

        if (_pathIndex >= _path.Count)
        {
            MoveTowardApproachCell(startCell, goalCell);
            return;
        }

        Vector2I waypoint = _path[_pathIndex];
        Vector2 waypointWorld = _navigationGrid.CellToWorld(waypoint);
        if (_owner.GlobalPosition.DistanceTo(waypointWorld) <= WaypointReachDistance)
        {
            _pathIndex++;
            if (_pathIndex >= _path.Count)
            {
                MoveTowardApproachCell(startCell, goalCell);
                return;
            }

            waypoint = _path[_pathIndex];
            waypointWorld = _navigationGrid.CellToWorld(waypoint);
        }

        Vector2I gridStep = GetCardinalStepToward(startCell, waypoint);
        if (gridStep == Vector2I.Zero
            && _owner.GlobalPosition.DistanceTo(waypointWorld) > WaypointReachDistance)
        {
            _pathDirty = true;
            return;
        }

        if (gridStep == Vector2I.Zero)
        {
            return;
        }

        ApplyMoveDirection(gridStep);
    }

    private void MoveTowardApproachCell(Vector2I startCell, Vector2I goalCell)
    {
        Vector2I approachCell = _resolvedGoalCell != InvalidCell
            ? _resolvedGoalCell
            : FindNearestApproachCell(startCell, goalCell);
        if (approachCell == InvalidCell)
        {
            StopMoving();
            return;
        }

        if (IsOnTargetApproachCell(startCell, goalCell))
        {
            StopMoving();
            return;
        }

        Vector2I gridStep = GetCardinalStepToward(startCell, approachCell);
        if (gridStep == Vector2I.Zero)
        {
            _pathDirty = true;
            return;
        }

        ApplyMoveDirection(gridStep);
    }

    private Vector2I FindNearestApproachCell(Vector2I startCell, Vector2I targetCell)
    {
        Vector2I best = InvalidCell;
        int bestPathLength = int.MaxValue;

        foreach (Vector2I neighbor in CardinalNeighbors)
        {
            Vector2I approachCell = targetCell + neighbor;
            if (!_navigationGrid.IsWalkable(approachCell))
            {
                continue;
            }

            List<Vector2I> path = _navigationGrid.FindPath(startCell, approachCell);
            if (path == null || path.Count == 0)
            {
                continue;
            }

            if (path.Count < bestPathLength)
            {
                bestPathLength = path.Count;
                best = approachCell;
            }
        }

        return best;
    }

    private bool IsOnTargetApproachCell(Vector2I currentCell, Vector2I targetCell)
    {
        if (!_navigationGrid.IsWalkable(currentCell))
        {
            return false;
        }

        return Mathf.Abs(currentCell.X - targetCell.X) + Mathf.Abs(currentCell.Y - targetCell.Y) == 1;
    }

    private static Vector2I GetCardinalStepToward(Vector2I from, Vector2I to)
    {
        Vector2I delta = to - from;
        if (delta == Vector2I.Zero)
        {
            return Vector2I.Zero;
        }

        if (Mathf.Abs(delta.X) >= Mathf.Abs(delta.Y))
        {
            return new Vector2I(Mathf.Clamp(delta.X, -1, 1), 0);
        }

        return new Vector2I(0, Mathf.Clamp(delta.Y, -1, 1));
    }

    private void ApplyMoveDirection(Vector2I gridStep)
    {
        Vector2 moveDir = GridPlacement.GridStepToMoveDirection(
            gridStep,
            _navigationGrid.TileWidth,
            _navigationGrid.TileHeight);
        _owner.SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, moveDir);
    }

    private static readonly Vector2I[] CardinalNeighbors =
    {
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1),
    };

    private static Vector2I InvalidCell => new(int.MinValue, int.MinValue);

    private void BindNavigationGrid()
    {
        _navigationGrid = NavigationGrid.Resolve(this);
        if (_navigationGrid == null)
        {
            GD.PushError($"{Name}: NavigationGrid not found.");
            return;
        }

        BuildingSystem buildingSystem = GetTree().CurrentScene?.GetNodeOrNull<BuildingSystem>("BuildingSystem");
        if (buildingSystem != null)
        {
            buildingSystem.ObjectPlaced += OnWorldChanged;
        }
    }

    private void OnWorldChanged(PackedScene scene, Vector2 position)
    {
        _pathDirty = true;
    }

    private void EnsureTarget()
    {
        if (_target != null && GodotObject.IsInstanceValid(_target as GodotObject) && _target.IsAlive)
        {
            return;
        }

        _target = null;
        if (!UseBonfireGroup)
        {
            return;
        }

        float bestDistanceSquared = float.MaxValue;
        foreach (Node node in GetTree().GetNodesInGroup(GameConstants.BonfireGroupName))
        {
            if (node is not ITargetable targetable || !targetable.IsAlive)
            {
                continue;
            }

            float distanceSquared = _owner.GlobalPosition.DistanceSquaredTo(targetable.GlobalPosition);
            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                _target = targetable;
            }
        }
    }

    private void Repath(Vector2I startCell, Vector2I goalCell)
    {
        _pathDirty = false;
        _repathTimer = RepathInterval;
        _lastStartCell = startCell;
        _lastGoalCell = goalCell;
        _resolvedGoalCell = FindNearestApproachCell(startCell, goalCell);
        if (_resolvedGoalCell == InvalidCell)
        {
            _path = null;
            return;
        }

        _path = _navigationGrid.FindPath(startCell, _resolvedGoalCell);
        _pathIndex = 0;

        if (_path == null || _path.Count == 0)
        {
            return;
        }

        if (_path[0] == startCell && _path.Count > 1)
        {
            _pathIndex = 1;
        }
        else if (_path.Count > 0)
        {
            _resolvedGoalCell = _path[^1];
        }
    }

    private void StopMoving()
    {
        _owner.SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
    }
}
