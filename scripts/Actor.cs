using Godot;
using System;

public partial class Actor : CharacterBody2D, ITargetable
{
    [Signal]
    public delegate void StateChangedEventHandler(string newStateName);

    [Signal]
    public delegate void BlackboardChangedEventHandler(string key, Variant value);

    public const string KeyInputVector = "input_vector"; // 保留兼容性
    public const string KeyMoveDirection = "move_direction"; // 通用移动方向（归一化向量）
    public const string KeyMoveSpeed = "move_speed"; // 移动速度（由 MovementComponent 读取）
    public const string KeyIsDead = "is_dead";
    public const string KeyIsAttacking = "is_attacking";
    public const string KeyState = "state";
    public const string KeyDamagePending = "damage_pending";
    public const string KeyDamageAmount = "damage_amount";
    public const string KeyDamageSource = "damage_source";
    public const string KeyHitPending = "hit_pending";
    public const string KeyHitSource = "hit_source";
    public const string KeyVelocity = "velocity";
    public const string KeyCurrentHealth = "current_health";
    public const string KeyMaxHealth = "max_health";
    public Godot.Collections.Dictionary<string, Variant> Blackboard { get; } = new();

    private StateMachine _stateMachine;

    /// <summary>
    /// 获取当前状态名称（用于兼容性检查）
    /// </summary>
    public string CurrentStateName => _stateMachine?.CurrentState?.Name ?? "None";
    
    /// <summary>
    /// 检查是否处于指定状态
    /// </summary>
    public bool IsInState<T>() where T : State
    {
        return _stateMachine?.CurrentState is T;
    }

    /// <summary>
    /// 检查是否存活
    /// </summary>
    public bool IsAlive => !GetBlackboardBool(KeyIsDead, false);

    protected HealthComponent _healthComponent;
    protected AnimationController _animationController;
    protected HitEffectComponent _hitEffectComponent;
    protected KnockbackComponent _knockbackComponent;
    protected HurtboxComponent _hurtboxComponent;
    protected CollisionShape2D _collisionShape;

    public override void _EnterTree()
    {
        InitializeBlackboardDefaults();
    }

    public override void _Ready()
    {
        // 查找状态机
        _stateMachine = GetNodeOrNull<StateMachine>("StateMachine");
        if (_stateMachine != null)
        {
            _stateMachine.StateChanged += OnStateMachineStateChanged;
        }
        else
        {
            GD.PushWarning($"{Name}: Actor should have a StateMachine child node.");
        }

        _healthComponent = GetNodeOrNull<HealthComponent>("CoreComponents/HealthComponent")
            ?? GetNodeOrNull<HealthComponent>("HealthComponent");
        _animationController = GetNodeOrNull<AnimationController>("CoreComponents/AnimationController")
            ?? GetNodeOrNull<AnimationController>("AnimationController");
        _hitEffectComponent = GetNodeOrNull<HitEffectComponent>("CoreComponents/HitEffectComponent")
            ?? GetNodeOrNull<HitEffectComponent>("HitEffectComponent");
        _knockbackComponent = GetNodeOrNull<KnockbackComponent>("CoreComponents/KnockbackComponent")
            ?? GetNodeOrNull<KnockbackComponent>("KnockbackComponent");
        _hurtboxComponent = GetNodeOrNull<HurtboxComponent>("CoreComponents/HurtboxComponent")
            ?? GetNodeOrNull<HurtboxComponent>("HurtboxComponent")
            ?? GetNodeOrNull<HurtboxComponent>("Hurtbox");
        _collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        if (_healthComponent != null)
        {
            _healthComponent.Died += HandleDied;
            _healthComponent.HealthChanged += HandleHealthChanged;
        }
    }

    private void OnStateMachineStateChanged(string newStateName)
    {
        EmitSignal(SignalName.StateChanged, newStateName);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAlive)
        {
            return;
        }

        // Actor 只负责调用 MoveAndSlide，所有速度计算由组件负责：
        // - Normal 状态：MovementComponent 计算速度
        // - Stagger 状态：KnockbackComponent 计算速度
        // - Attack 状态：MovementComponent 不更新速度，保持当前速度（或由其他组件处理）

        MoveAndSlide();
        SetBlackboardValueIfChanged(KeyVelocity, Velocity);
    }

    private void InitializeBlackboardDefaults()
    {
        Blackboard[KeyInputVector] = Vector2.Zero;
        Blackboard[KeyMoveDirection] = Vector2.Zero;
        Blackboard[KeyMoveSpeed] = 0f; // 0 表示使用 MovementComponent 的默认值
        Blackboard[KeyIsDead] = false;
        Blackboard[KeyIsAttacking] = false;
        Blackboard[KeyState] = "None";
        Blackboard[KeyDamagePending] = false;
        Blackboard[KeyDamageAmount] = 0;
        Blackboard[KeyDamageSource] = HealthComponent.NoSourcePosition;
        Blackboard[KeyHitPending] = false;
        Blackboard[KeyHitSource] = HealthComponent.NoSourcePosition;
        Blackboard[KeyVelocity] = Vector2.Zero;
    }

    public void SetBlackboardValue(string key, Variant value)
    {
        Blackboard[key] = value;
        EmitSignal(SignalName.BlackboardChanged, key, value);
    }

    public void SetBlackboardValueIfChanged(string key, Variant value)
    {
        if (Blackboard.TryGetValue(key, out var existing) && existing.Equals(value))
        {
            return;
        }

        SetBlackboardValue(key, value);
    }

    public bool TryGetBlackboardValue(string key, out Variant value)
    {
        return Blackboard.TryGetValue(key, out value);
    }

    public Vector2 GetBlackboardVector(string key, Vector2 defaultValue)
    {
        return Blackboard.TryGetValue(key, out var value) ? value.AsVector2() : defaultValue;
    }

    public bool GetBlackboardBool(string key, bool defaultValue)
    {
        return Blackboard.TryGetValue(key, out var value) ? value.AsBool() : defaultValue;
    }

    public int GetBlackboardInt(string key, int defaultValue)
    {
        return Blackboard.TryGetValue(key, out var value) ? value.AsInt32() : defaultValue;
    }

    public float GetBlackboardFloat(string key, float defaultValue)
    {
        return Blackboard.TryGetValue(key, out var value) ? value.AsSingle() : defaultValue;
    }

    /// <summary>
    /// 请求切换到指定状态（通过状态机）
    /// </summary>
    public void RequestStateChange<T>() where T : State
    {
        _stateMachine?.ChangeStateByType<T>();
    }

    /// <summary>
    /// 请求切换到指定状态（通过名称）
    /// </summary>
    public void RequestStateChangeByName(string stateName)
    {
        _stateMachine?.ChangeStateByName(stateName);
    }

    public void RequestDamage(int amount, Vector2? sourcePosition = null)
    {
        if (amount <= 0)
        {
            return;
        }

        int existing = 0;
        if (GetBlackboardBool(KeyDamagePending, false))
        {
            existing = GetBlackboardInt(KeyDamageAmount, 0);
        }

        SetBlackboardValue(KeyDamageAmount, existing + amount);
        SetBlackboardValue(KeyDamageSource, sourcePosition ?? HealthComponent.NoSourcePosition);
        SetBlackboardValue(KeyDamagePending, true);
    }

    protected virtual void HandleHealthChanged(int currentHp, int maxHp, Vector2 sourcePosition)
    {
        if (GetBlackboardBool(KeyIsDead, false))
        {
            return;
        }

        bool hasSource = !float.IsNaN(sourcePosition.X) && !float.IsNaN(sourcePosition.Y);
        if (hasSource)
        {
            SetBlackboardValue(KeyHitSource, sourcePosition);
            SetBlackboardValue(KeyHitPending, true);
            
            // 状态机会在状态更新时检查 KeyHitPending 并转换到 StaggerState
            // StaggerState 会处理击退逻辑
        }
        else
        {
            SetBlackboardValue(KeyHitSource, HealthComponent.NoSourcePosition);
        }
    }

    protected virtual void HandleDied()
    {
        if (GetBlackboardBool(KeyIsDead, false))
        {
            return;
        }

        Velocity = Vector2.Zero;
        SetBlackboardValue(KeyIsDead, true);
        
        // 直接请求转换到死亡状态
        RequestStateChange<DeadState>();
    }

    public void SetCollisionEnabled(bool enabled)
    {
        if (_collisionShape != null)
        {
            _collisionShape.SetDeferred("disabled", !enabled);
        }
    }

    public void SetHurtboxEnabled(bool enabled)
    {
        if (_hurtboxComponent != null)
        {
            _hurtboxComponent.SetDeferred("monitoring", enabled);
            _hurtboxComponent.SetDeferred("monitorable", enabled);
        }
    }

}
