using Godot;

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

        // 订阅 HealthComponent 信号
        if (HealthComponent != null)
        {
            HealthComponent.Died += HandleDied;
            HealthComponent.HealthChanged += HandleHealthChanged;
        }
        
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
                SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
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
                    SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
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
            SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
            return;
        }

        Vector2 direction = (targetPos.Value - GlobalPosition).Normalized();
        // 写入移动意图到黑板，由 MovementComponent 处理
        SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, direction);
    }

    // --- 动画优化 ---

    private void UpdateAnimation()
    {
		if (AnimationController != null)
		{
			// 使用统一方法，根据 velocity.Y 自动判断方向，自动翻转
			AnimationController.UpdateAnimation(Velocity);
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

    private void HandleDied()
    {
        if (GetBlackboardBool(Actor.BlackboardKeys.IsDead, false))
        {
            return;
        }

        Velocity = Vector2.Zero;
        SetBlackboardValue(Actor.BlackboardKeys.IsDead, true);
        RequestStateChange<DeadState>();

        // 延迟销毁，让动画播放完
        GetTree().CreateTimer(0.5f).Timeout += QueueFree;
    }

    /// <summary>
    /// 重写受击处理：Ghost 受击时不会进入 Stagger 状态，可以继续移动
    /// </summary>
    private void HandleHealthChanged(int currentHp, int maxHp, Vector2 sourcePosition)
    {
        // Ghost 受击时只触发受击效果（闪烁等），但不进入 Stagger 状态，可以继续移动
        // 使用 IsAlive 属性检查是否死亡（这是 Actor 基类的属性）
        if (!IsAlive)
        {
            return;
        }

        // 设置受击源位置，让 HitEffectComponent 可以播放闪烁效果
        bool hasSource = !float.IsNaN(sourcePosition.X) && !float.IsNaN(sourcePosition.Y);
        if (hasSource)
        {
            SetBlackboardValue(Actor.BlackboardKeys.HitSource, sourcePosition);
        }
        else
        {
            SetBlackboardValue(Actor.BlackboardKeys.HitSource, HealthComponent.NoSourcePosition);
        }

        SetBlackboardValue(Actor.BlackboardKeys.HitPending, true);
        // 注意：Ghost 不进入 Stagger 状态，可以继续移动
    }
}