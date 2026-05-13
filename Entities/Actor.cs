namespace AlongJourney.Entities;

using Godot;
using Godot.Collections;
using AlongJourney.Interfaces;
using AlongJourney.Core;
using AlongJourney.Components;

public partial class Actor : CharacterBody2D, ITargetable
{
    // ==========================================
    // 1. 信号定义
    // ==========================================
    [Signal]
    public delegate void StateChangedEventHandler(string newStateName);

    [Signal]
    public delegate void BlackboardChangedEventHandler(string key, Variant value);

    // ==========================================
    // 2. Blackboard 键定义 (优化为 StringName)
    // ==========================================
    // 使用 StringName 在 Godot 中进行字典查找性能更佳
    public static class BlackboardKeys
    {
        public static readonly StringName InputVector = "input_vector";
        public static readonly StringName MoveDirection = "move_direction";
        public static readonly StringName MoveSpeed = "move_speed";
        public static readonly StringName IsDead = "is_dead";
        public static readonly StringName IsAttacking = "is_attacking";
        public static readonly StringName State = "state";
        public static readonly StringName DamagePending = "damage_pending";
        public static readonly StringName DamageAmount = "damage_amount";
        public static readonly StringName DamageSource = "damage_source";
        public static readonly StringName HitPending = "hit_pending";
        public static readonly StringName HitSource = "hit_source";
        public static readonly StringName Velocity = "velocity";
        public static readonly StringName CurrentHealth = "current_health";
        public static readonly StringName MaxHealth = "max_health";
    }

    // ==========================================
    // 3. 组件引用 (使用 Export 替代 GetNode)
    // ==========================================
    [ExportGroup("Core Components")]
    [Export] public StateMachine StateMachine { get; private set; }
    [Export] public HealthComponent HealthComponent { get; private set; }
    [Export] public AnimationController AnimationController { get; private set; }
    [Export] public HitEffectComponent HitEffectComponent { get; private set; }
    [Export] public KnockbackComponent KnockbackComponent { get; private set; }
    [Export] public HurtboxComponent HurtboxComponent { get; private set; }

    // 碰撞体通常是固定的，可以用 GetNode，或者也 Export
    [Export] public CollisionShape2D CollisionShape { get; private set; }

    // ==========================================
    // 4. 数据存储
    // ==========================================
    // 使用 StringName 作为 Key
    public Dictionary<StringName, Variant> Blackboard { get; } = new();

    /// <summary>
    /// 获取当前状态名称（用于兼容性检查）
    /// </summary>
    public string CurrentStateName => StateMachine?.CurrentState?.Name ?? "None";
    
    /// <summary>
    /// 检查是否处于指定状态
    /// </summary>
    public bool IsInState<T>() where T : State
    {
        return StateMachine?.CurrentState is T;
    }

    /// <summary>
    /// 检查是否存活
    /// </summary>
    public bool IsAlive => !GetBlackboardBool(BlackboardKeys.IsDead, false);

    public override void _EnterTree()
    {
        InitializeBlackboardDefaults();
    }

    public override void _Ready()
    {
        // 🛡️ 架构检查：确保必要的组件已连接
        if (StateMachine == null) GD.PushError($"{Name}: StateMachine is not assigned in Inspector!");
        if (HealthComponent == null) GD.PushWarning($"{Name}: HealthComponent is missing!");

        if (HurtboxComponent == null)
        {
            HurtboxComponent = GetNodeOrNull<HurtboxComponent>("Hurtbox");
        }

        if (HurtboxComponent == null)
        {
            GD.PushWarning($"{Name}: HurtboxComponent is missing (Inspector export or child named Hurtbox).");
        }

        // 绑定事件
        if (StateMachine != null)
        {
            StateMachine.StateChanged += OnStateMachineStateChanged;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsAlive) return;
        // 移动由 Movement/Knockback 组件驱动，避免依赖 Actor 本体处理顺序
    }

    // ==========================================
    // 5. Blackboard 操作封装
    // ==========================================
    private void InitializeBlackboardDefaults()
    {
        Blackboard[BlackboardKeys.InputVector] = Vector2.Zero;
        Blackboard[BlackboardKeys.MoveDirection] = Vector2.Zero;
        Blackboard[BlackboardKeys.MoveSpeed] = 0f; // 0 表示使用 MovementComponent 的默认值
        Blackboard[BlackboardKeys.IsDead] = false;
        Blackboard[BlackboardKeys.IsAttacking] = false;
        Blackboard[BlackboardKeys.State] = "None";
        Blackboard[BlackboardKeys.DamagePending] = false;
        Blackboard[BlackboardKeys.DamageAmount] = 0;
        Blackboard[BlackboardKeys.DamageSource] = HealthComponent.NoSourcePosition;
        Blackboard[BlackboardKeys.HitPending] = false;
        Blackboard[BlackboardKeys.HitSource] = HealthComponent.NoSourcePosition;
        Blackboard[BlackboardKeys.Velocity] = Vector2.Zero;
    }

    public void SetBlackboardValue(StringName key, Variant value)
    {
        Blackboard[key] = value;
        EmitSignal(SignalName.BlackboardChanged, key.ToString(), value); 
    }

    public void SetBlackboardValueIfChanged(StringName key, Variant value)
    {
        if (Blackboard.TryGetValue(key, out var existing) && existing.Equals(value)) return;
        SetBlackboardValue(key, value);
    }

    public bool TryGetBlackboardValue(StringName key, out Variant value)
    {
        return Blackboard.TryGetValue(key, out value);
    }

    public Vector2 GetBlackboardVector(StringName key, Vector2 defaultValue = default)
    {
        return Blackboard.TryGetValue(key, out var val) ? val.AsVector2() : defaultValue;
    }
    
    public bool GetBlackboardBool(StringName key, bool defaultValue = false)
    {
        return Blackboard.TryGetValue(key, out var val) ? val.AsBool() : defaultValue;
    }
    
    public int GetBlackboardInt(StringName key, int defaultValue = 0)
    {
        return Blackboard.TryGetValue(key, out var val) ? val.AsInt32() : defaultValue;
    }

    public float GetBlackboardFloat(StringName key, float defaultValue = 0f)
    {
        return Blackboard.TryGetValue(key, out var val) ? val.AsSingle() : defaultValue;
    }

    // ==========================================
    // 6. 状态机操作封装
    // ==========================================
    
    /// <summary>
    /// 请求切换到指定状态（通过状态机，类型安全）
    /// </summary>
    public void RequestStateChange<T>() where T : State => StateMachine?.ChangeStateByType<T>();

    // ==========================================
    // 7. 数据请求方法（不包含业务逻辑）
    // ==========================================
    
    /// <summary>
    /// 请求伤害处理（仅设置 Blackboard 数据，不包含业务逻辑）
    /// </summary>
    public void RequestDamage(int amount, Vector2? sourcePosition = null)
    {
        if (amount <= 0) return;

        int existing = GetBlackboardBool(BlackboardKeys.DamagePending, false) 
            ? GetBlackboardInt(BlackboardKeys.DamageAmount, 0) 
            : 0;

        SetBlackboardValue(BlackboardKeys.DamageAmount, existing + amount);
        SetBlackboardValue(BlackboardKeys.DamageSource, sourcePosition ?? HealthComponent.NoSourcePosition);
        SetBlackboardValue(BlackboardKeys.DamagePending, true);
    }

    // ==========================================
    // 8. 事件处理（仅转发信号，不包含业务逻辑）
    // ==========================================
    
    private void OnStateMachineStateChanged(string newStateName)
    {
        SetBlackboardValue(BlackboardKeys.State, newStateName); // 同步状态回 Blackboard
        EmitSignal(SignalName.StateChanged, newStateName);
    }

    // ==========================================
    // 9. 组件操作工具方法（简单的组件封装）
    // ==========================================
    
    public void SetCollisionEnabled(bool enabled)
    {
        if (CollisionShape != null)
        {
            CollisionShape.SetDeferred("disabled", !enabled);
        }
    }

    public void SetHurtboxEnabled(bool enabled)
    {
        if (HurtboxComponent != null)
        {
            HurtboxComponent.SetDeferred("monitoring", enabled);
            HurtboxComponent.SetDeferred("monitorable", enabled);
        }
    }

    public void ApplyMovement()
    {
        if (!IsAlive) return;
        MoveAndSlide();
        SetBlackboardValueIfChanged(BlackboardKeys.Velocity, Velocity);
    }
}
