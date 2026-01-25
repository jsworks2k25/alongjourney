using Godot;
using System;

public partial class Ghost : Enemy
{
    // --- 配置 ---
    [Export] public int DamagePerTick = 10;
    [Export] public float DamageInterval = 1.0f;

    // --- 组件引用 ---
    [Export] private MovementSmoothingComponent _movementSmoothing;
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
        if (_movementSmoothing == null)
            _movementSmoothing = GetNodeOrNull<MovementSmoothingComponent>("MovementSmoothing");
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
                // [新增] 惯性刹车：如果没有目标，就慢慢停下来，而不是瞬间静止
                Velocity = ApplyBrake(Velocity, (float)delta);
                break;

            case GhostState.Chase:
                if (_target != null && !GodotObject.IsInstanceValid(_target as GodotObject))
                    _target = null;
                if (_target != null && _target.IsAlive)
                {
                    ChaseTarget(delta);
                }
                else
                {
                    Velocity = ApplyBrake(Velocity, (float)delta);
                }
                break;
        }

        UpdateAnimation();
        ProcessContactDamage(delta);
    }

    // --- 核心移动逻辑改动 ---

    private void ChaseTarget(double delta)
    {
        var targetPos = GetTargetPosition();
        if (!targetPos.HasValue) return;

        Vector2 direction = (targetPos.Value - GlobalPosition).Normalized();
        
        // [修改] 不再直接赋值 Velocity = direction * Speed
        // 而是使用 MoveToward 平滑地从"当前速度"过渡到"目标速度"
        
        Velocity = ApplyAcceleration(Velocity, direction * Speed * _isoVec, (float)delta);
        

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

    private Vector2 ApplyAcceleration(Vector2 currentVelocity, Vector2 targetVelocity, float delta)
    {
        return _movementSmoothing != null
            ? _movementSmoothing.Accelerate(currentVelocity, targetVelocity, delta)
            : targetVelocity;
    }

    private Vector2 ApplyBrake(Vector2 currentVelocity, float delta)
    {
        return _movementSmoothing != null
            ? _movementSmoothing.Brake(currentVelocity, delta)
            : Vector2.Zero;
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