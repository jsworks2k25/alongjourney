using Godot;
using System;

public partial class Actor : CharacterBody2D, ITargetable
{
    [Signal]
    public delegate void StateChangedEventHandler(int newState);

    [Signal]
    public delegate void BlackboardChangedEventHandler(string key, Variant value);

    public enum ActorState
    {
        Normal,
        Attack,
        Stagger,
        Dead
    }

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

    public ActorState CurrentState { get; private set; } = ActorState.Normal;
    public bool IsAlive => CurrentState != ActorState.Dead;

    public event Action<ActorState> StateChangedTyped;

    protected HealthComponent _healthComponent;
    protected AnimationController _animationController;
    protected HitEffectComponent _hitEffectComponent;
    protected HurtboxComponent _hurtboxComponent;
    protected CollisionShape2D _collisionShape;

    public override void _EnterTree()
    {
        InitializeBlackboardDefaults();
    }

    public override void _Ready()
    {
        _healthComponent = GetNodeOrNull<HealthComponent>("CoreComponents/HealthComponent")
            ?? GetNodeOrNull<HealthComponent>("HealthComponent");
        _animationController = GetNodeOrNull<AnimationController>("CoreComponents/AnimationController")
            ?? GetNodeOrNull<AnimationController>("AnimationController");
        _hitEffectComponent = GetNodeOrNull<HitEffectComponent>("CoreComponents/HitEffectComponent")
            ?? GetNodeOrNull<HitEffectComponent>("HitEffectComponent");
        _hurtboxComponent = GetNodeOrNull<HurtboxComponent>("CoreComponents/HurtboxComponent")
            ?? GetNodeOrNull<HurtboxComponent>("HurtboxComponent")
            ?? GetNodeOrNull<HurtboxComponent>("Hurtbox");
        _collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        if (_healthComponent != null)
        {
            _healthComponent.Died += HandleDied;
            _healthComponent.HealthChanged += HandleHealthChanged;
        }

        if (_hitEffectComponent != null)
        {
            _hitEffectComponent.OnStaggerEnded += OnStaggerEnded;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAlive)
        {
            return;
        }

        // Attack 状态时自动减速（由 Actor 统一处理，不依赖 MovementComponent）
        if (CurrentState == ActorState.Attack)
        {
            float friction = GameConfig.Instance != null ? GameConfig.Instance.KnockbackFriction : 600f;
            Velocity = Velocity.MoveToward(Vector2.Zero, friction * (float)delta);
        }

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
        Blackboard[KeyState] = (int)CurrentState;
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

    public void SetState(ActorState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        var oldState = CurrentState;
        CurrentState = newState;

        SetBlackboardValue(KeyState, (int)newState);
        SetBlackboardValue(KeyIsDead, newState == ActorState.Dead);
        SetBlackboardValue(KeyIsAttacking, newState == ActorState.Attack);

        EmitSignal(SignalName.StateChanged, (int)newState);
        StateChangedTyped?.Invoke(newState);
        OnStateChanged(oldState, newState);
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

    protected virtual void OnStateChanged(ActorState oldState, ActorState newState)
    {
    }

    protected virtual void HandleHealthChanged(int currentHp, int maxHp, Vector2 sourcePosition)
    {
        if (CurrentState == ActorState.Dead)
        {
            return;
        }

        bool hasSource = !float.IsNaN(sourcePosition.X) && !float.IsNaN(sourcePosition.Y);
        if (hasSource)
        {
            SetState(ActorState.Stagger);
            SetBlackboardValue(KeyHitSource, sourcePosition);
        }
        else
        {
            SetBlackboardValue(KeyHitSource, HealthComponent.NoSourcePosition);
        }

        SetBlackboardValue(KeyHitPending, true);
    }

    protected virtual void HandleDied()
    {
        if (CurrentState == ActorState.Dead)
        {
            return;
        }

        Velocity = Vector2.Zero;
        SetState(ActorState.Dead);
        SetCollisionEnabled(false);
        SetHurtboxEnabled(false);
    }

    protected void SetCollisionEnabled(bool enabled)
    {
        if (_collisionShape != null)
        {
            _collisionShape.SetDeferred("disabled", !enabled);
        }
    }

    protected void SetHurtboxEnabled(bool enabled)
    {
        if (_hurtboxComponent != null)
        {
            _hurtboxComponent.SetDeferred("monitoring", enabled);
            _hurtboxComponent.SetDeferred("monitorable", enabled);
        }
    }

    private void OnStaggerEnded()
    {
        if (CurrentState == ActorState.Stagger)
        {
            SetState(ActorState.Normal);
        }
    }
}
