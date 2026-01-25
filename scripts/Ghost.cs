using Godot;
using System;

public partial class Ghost : Enemy
{
    // --- 配置 ---
    [Export] public int DamagePerTick = 10;
    [Export] public float DamageInterval = 1.0f;

    // --- 组件引用 ---
    [Export] private Area2D _detectionArea;
    [Export] private HitboxComponent _hitbox;

    // --- 状态机 ---
    private enum GhostState { Idle, Chase }
    private GhostState _currentState = GhostState.Idle;

    private float _damageTimer = 0f;

    public override void _Ready()
    {
        base._Ready(); // 调用基类初始化
        
        if (_detectionArea != null)
        {
            _detectionArea.BodyEntered += OnBodyEnteredDetection;
            _detectionArea.BodyExited += OnBodyExitedDetection;
        }

        // 自动查找组件
        if (_detectionArea == null)
            _detectionArea = GetNodeOrNull<Area2D>("DetectionArea");
        if (_hitbox == null)
            _hitbox = GetNodeOrNull<HitboxComponent>("Hitbox");
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta); // 调用基类的移动逻辑
        
        switch (_currentState)
        {
            case GhostState.Idle:
                // 空闲时停止移动（MovementComponent 会自动处理刹车）
                SetBlackboardValue(Actor.KeyMoveDirection, Vector2.Zero);
                break;

            case GhostState.Chase:
                if (_target != null && !GodotObject.IsInstanceValid(_target as GodotObject))
                    _target = null;
                if (_target != null && _target.IsAlive)
                {
                    ChaseTarget();
                }
                else
                {
                    SetBlackboardValue(Actor.KeyMoveDirection, Vector2.Zero);
                }
                break;
        }

        UpdateAnimation();
        ProcessContactDamage(delta);
    }

    // --- 核心移动逻辑改动 ---

    private void ChaseTarget()
    {
        var targetPos = GetTargetPosition();
        if (!targetPos.HasValue)
        {
            SetBlackboardValue(Actor.KeyMoveDirection, Vector2.Zero);
            return;
        }

        Vector2 direction = (targetPos.Value - GlobalPosition).Normalized();
        // 写入移动意图到黑板，由 MovementComponent 处理
        SetBlackboardValue(Actor.KeyMoveDirection, direction);
    }

    // --- 动画优化 ---

    private void UpdateAnimation()
    {
		if (_animationController != null)
		{
			// 使用统一方法，根据 velocity.Y 自动判断方向，自动翻转
			_animationController.UpdateAnimation(Velocity);
		}
    }


    // --- 其他逻辑保持不变 ---
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
                    // 传递攻击者的位置（Ghost的全局位置）
                    damageable.TakeDamage(DamagePerTick, GlobalPosition);
                    _damageTimer = DamageInterval; 
                    break; // 一次只伤害一个目标
                }
            }
        }
    }

    private void OnBodyEnteredDetection(Node2D body)
    {
        if (body is ITargetable targetable && targetable.IsAlive)
        {
            _target = targetable;
            _currentState = GhostState.Chase;
        }
    }

    private void OnBodyExitedDetection(Node2D body)
    {
        if (_target != null && !GodotObject.IsInstanceValid(_target as GodotObject))
            _target = null;
        if (body is ITargetable targetable && targetable == _target)
        {
            _target = null;
            _currentState = GhostState.Idle;
        }
    }
}