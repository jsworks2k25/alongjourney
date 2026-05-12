namespace AlongJourney.Entities.NPC;

using Godot;
using AlongJourney.Entities;
using AlongJourney.Interfaces;
using AlongJourney.Core;
using AlongJourney.Components;
using AlongJourney.Entities.NPC.States;

public partial class NPC : Actor
{
    [Export] public float Speed = 50f;
    protected ITargetable _target;

    public override void _Ready()
    {
        base._Ready();

        if (HealthComponent != null)
        {
            HealthComponent.Died += OnHealthDied;
            HealthComponent.HealthChanged += OnHealthChanged;
        }

        SetBlackboardValue(Actor.BlackboardKeys.MoveSpeed, Speed);
        FindTarget();
    }

    protected virtual void FindTarget()
    {
        if (_target != null && GodotObject.IsInstanceValid(_target as GodotObject) && _target.IsAlive)
            return;
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
                float distanceSquared = GlobalPosition.DistanceSquaredTo(targetable.GlobalPosition);
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    _target = targetable;
                }
            }
        }
    }

    /// <summary>
    /// 获取目标位置，子类可以重写
    /// </summary>
    protected virtual Vector2? GetTargetPosition()
    {
        if (_target == null || !GodotObject.IsInstanceValid(_target as GodotObject)) return null;
        if (!_target.IsAlive) return null;
        return _target.GlobalPosition;
    }

    // 子类可以重写这个方法来实现不同的 AI
    public override void _PhysicsProcess(double delta)
    {
        if (_target != null && !GodotObject.IsInstanceValid(_target as GodotObject))
            _target = null;
        if (_target == null || !_target.IsAlive)
            FindTarget();

        // AI 逻辑：根据目标位置决定移动方向
        // 状态机会根据移动方向自动在 Idle 和 Run 之间切换
        UpdateAIMovement(delta);
        base._PhysicsProcess(delta);
    }

    /// <summary>
    /// 更新 AI 移动逻辑：根据目标位置设置移动方向
    /// </summary>
    protected virtual void UpdateAIMovement(double delta)
    {
        var targetPos = GetTargetPosition();
        if (targetPos.HasValue)
        {
            Vector2 direction = (targetPos.Value - GlobalPosition).Normalized();
            SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, direction);
        }
        else
        {
            SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
        }
    }

    private void OnHealthDied() => OnNpcHealthDied();

    private void OnHealthChanged(int currentHp, int maxHp, Vector2 sourcePosition) =>
        OnNpcHealthChanged(currentHp, maxHp, sourcePosition);

    /// <summary>
    /// Override for custom death teardown. Default: dead state + delayed free.
    /// </summary>
    protected virtual void OnNpcHealthDied()
    {
        if (GetBlackboardBool(Actor.BlackboardKeys.IsDead, false))
        {
            return;
        }

        Velocity = Vector2.Zero;
        SetBlackboardValue(Actor.BlackboardKeys.IsDead, true);
        RequestStateChange<DeadState>();
        GetTree().CreateTimer(0.5f).Timeout += QueueFree;
    }

    /// <summary>
    /// Override to react to HP changes (e.g. stagger, VFX). Base does nothing.
    /// </summary>
    protected virtual void OnNpcHealthChanged(int currentHp, int maxHp, Vector2 sourcePosition)
    {
    }
}
