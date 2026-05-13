namespace AlongJourney.Entities.Characters.AICharacters.Enemies;

using Godot;
using AlongJourney.Components;
using AlongJourney.Interfaces;
using AlongJourney.Entities.States;

public partial class Ghost : AICharacter
{
    // --- 配置 ---
    [Export] public int DamagePerTick = 10;
    [Export] public float DamageInterval = 1.0f;

    // --- 组件引用 ---
    [Export] private Area2D _detectionArea;
    [Export] private HitboxComponent _hitbox;

    private float _damageTimer = 0f;
    private ChaseTargetMovement _chase;

    public override void _Ready()
    {
        base._Ready();

        _chase = GetNodeOrNull<ChaseTargetMovement>("ChaseTargetMovement");

        if (_detectionArea != null)
        {
            _detectionArea.BodyEntered += OnBodyEnteredDetection;
            _detectionArea.BodyExited += OnBodyExitedDetection;
        }

        // 自动查找组件
        if (_detectionArea == null)
        {
            _detectionArea = GetNodeOrNull<Area2D>("DetectionArea");
        }

        if (_hitbox == null)
        {
            _hitbox = GetNodeOrNull<HitboxComponent>("Hitbox");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        UpdateAnimation();
        ProcessContactDamage(delta);
    }

    // --- 动画优化 ---

    private void UpdateAnimation()
    {
        if (AnimationController != null)
        {
            AnimationController.UpdateAnimation();
        }
    }

    private void ProcessContactDamage(double delta)
    {
        if (_hitbox == null || !_hitbox.HasOverlappingAreas())
        {
            _damageTimer = 0;
            return;
        }

        _damageTimer -= (float)delta;
        if (_damageTimer <= 0)
        {
            foreach (var area in _hitbox.GetOverlappingAreas())
            {
                if (area is IDamageable damageable)
                {
                    damageable.TakeDamage(DamagePerTick, GlobalPosition);
                    _damageTimer = DamageInterval;
                    break;
                }
            }
        }
    }

    private void OnBodyEnteredDetection(Node2D body)
    {
        if (_chase == null)
        {
            return;
        }

        if (body is ITargetable targetable && targetable.IsAlive)
        {
            _chase.SetTarget(targetable);
        }
    }

    private void OnBodyExitedDetection(Node2D body)
    {
        if (_chase == null)
        {
            return;
        }

        if (_chase.Target != null && !GodotObject.IsInstanceValid(_chase.Target as GodotObject))
        {
            _chase.SetTarget(null);
            return;
        }

        if (body is ITargetable targetable && targetable == _chase.Target)
        {
            _chase.SetTarget(null);
        }
    }

    protected override void OnNpcHealthChanged(int currentHp, int maxHp, Vector2 sourcePosition)
    {
        if (!IsAlive)
        {
            return;
        }

        bool hasSource = !float.IsNaN(sourcePosition.X) && !float.IsNaN(sourcePosition.Y);
        if (hasSource)
        {
            SetBlackboardValue(Actor.BlackboardKeys.HitSource, sourcePosition);
            if (KnockbackComponent != null)
            {
                KnockbackComponent.ApplyKnockback(sourcePosition);
            }
        }
        else
        {
            SetBlackboardValue(Actor.BlackboardKeys.HitSource, HealthComponent.NoSourcePosition);
        }

        SetBlackboardValue(Actor.BlackboardKeys.HitPending, true);
        RequestStateChange<StaggerState>();
    }
}
