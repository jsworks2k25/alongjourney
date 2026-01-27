using Godot;

public partial class Enemy : Actor
{
    [Export] public float Speed = 50f;
    protected ITargetable _target;

    public override void _Ready()
    {
        base._Ready();

        // 订阅 HealthComponent 信号
        if (HealthComponent != null)
        {
            HealthComponent.Died += HandleDied;
            HealthComponent.HealthChanged += HandleHealthChanged;
        }

        // 等距向量现在由 MovementComponent 的 Export 属性管理

        // 将 Speed 写入黑板，供 MovementComponent 使用
        SetBlackboardValue(Actor.BlackboardKeys.MoveSpeed, Speed);

        // 查找目标（玩家）
        FindTarget();
    }

    /// <summary>
    /// 查找目标（使用 Group 或接口）
    /// </summary>
    protected virtual void FindTarget()
    {
        if (_target != null && GodotObject.IsInstanceValid(_target as GodotObject) && _target.IsAlive)
            return;
        _target = null;

        string groupName = GameConfig.GetPlayerGroupName();
        var players = GetTree().GetNodesInGroup(groupName);

        foreach (var player in players)
        {
            if (player is ITargetable targetable && targetable.IsAlive)
            {
                _target = targetable;
                break;
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
            // 写入移动意图到黑板，由 MovementComponent 处理
            // 状态机会根据移动方向自动在 Idle 和 Run 之间切换
            SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, direction);
        }
        else
        {
            // 没有目标时停止移动
            SetBlackboardValue(Actor.BlackboardKeys.MoveDirection, Vector2.Zero);
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

    private void HandleHealthChanged(int currentHp, int maxHp, Vector2 sourcePosition)
    {
        if (GetBlackboardBool(Actor.BlackboardKeys.IsDead, false))
        {
            return;
        }

        bool hasSource = !float.IsNaN(sourcePosition.X) && !float.IsNaN(sourcePosition.Y);
        if (hasSource)
        {
            SetBlackboardValue(Actor.BlackboardKeys.HitSource, sourcePosition);
            RequestStateChange<StaggerState>();
            SetBlackboardValue(Actor.BlackboardKeys.HitPending, true);
        }
        else
        {
            SetBlackboardValue(Actor.BlackboardKeys.HitSource, HealthComponent.NoSourcePosition);
        }
    }
}