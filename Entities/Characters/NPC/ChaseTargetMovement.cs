namespace AlongJourney.Entities.Characters.NPC;

using Godot;
using AlongJourney.Core;
using AlongJourney.Entities;
using AlongJourney.Interfaces;

/// <summary>
/// Optional child of an <see cref="Actor"/>: picks a target and writes <see cref="Actor.BlackboardKeys.MoveDirection"/>.
/// Call <see cref="PhysicsTick"/> from the parent's physics step (NPC does this when this node exists).
/// </summary>
public partial class ChaseTargetMovement : Node
{
    [Export] public bool UsePlayerRegistry = true;

    private Actor _owner;
    private ITargetable _target;

    public ITargetable Target => _target;

    public override void _Ready()
    {
        _owner = GetParent<Actor>();
        if (_owner == null)
        {
            GD.PushError($"{Name}: parent must be Actor.");
        }
    }

    public void SetTarget(ITargetable target)
    {
        _target = target;
    }

    public void PhysicsTick(double delta)
    {
        if (_owner == null)
        {
            return;
        }

        if (_target != null && !GodotObject.IsInstanceValid(_target as GodotObject))
        {
            _target = null;
        }

        if (_target == null || !_target.IsAlive)
        {
            if (UsePlayerRegistry)
            {
                TryPickNearestPlayer();
            }
            else
            {
                _target = null;
            }
        }

        var targetPos = GetTargetPosition();
        if (targetPos.HasValue)
        {
            Vector2 direction = (targetPos.Value - _owner.GlobalPosition).Normalized();
            _owner.SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, direction);
        }
        else
        {
            _owner.SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
        }
    }

    private Vector2? GetTargetPosition()
    {
        if (_target == null || !GodotObject.IsInstanceValid(_target as GodotObject))
        {
            return null;
        }

        if (!_target.IsAlive)
        {
            return null;
        }

        return _target.GlobalPosition;
    }

    private void TryPickNearestPlayer()
    {
        if (_target != null && GodotObject.IsInstanceValid(_target as GodotObject) && _target.IsAlive)
        {
            return;
        }

        _target = null;

        var players = GameManager.Instance?.PlayerRegistry.GetPlayers();
        if (players == null || players.Count == 0)
        {
            return;
        }

        float bestDistanceSquared = float.MaxValue;
        foreach (var player in players)
        {
            if (player is ITargetable targetable && targetable.IsAlive)
            {
                float distanceSquared = _owner.GlobalPosition.DistanceSquaredTo(targetable.GlobalPosition);
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    _target = targetable;
                }
            }
        }
    }
}
